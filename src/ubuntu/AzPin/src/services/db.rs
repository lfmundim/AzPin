use rusqlite::{params, Connection, Result};
use std::path::PathBuf;
use crate::models::persistence::{CachedToken, PinnedResourceGroup, PinnedResource};
use std::sync::Mutex;

pub struct Db {
    conn: Mutex<Connection>,
}

impl Db {
    pub fn new() -> Result<Self> {
        let db_path = Self::get_db_path();
        if let Some(parent) = db_path.parent() {
            std::fs::create_dir_all(parent).unwrap_or_default();
        }
        let conn = Connection::open(&db_path)?;
        let db = Self { conn: Mutex::new(conn) };
        db.init()?;
        Ok(db)
    }

    pub fn new_in_memory() -> Result<Self> {
        let conn = Connection::open_in_memory()?;
        let db = Self { conn: Mutex::new(conn) };
        db.init()?;
        Ok(db)
    }

    fn get_db_path() -> PathBuf {
        let data_dir = dirs::data_dir().unwrap_or_else(|| {
            PathBuf::from(std::env::var("HOME").unwrap_or_else(|_| String::from("~"))).join(".local/share")
        });
        data_dir.join("azpin").join("azpin.db")
    }

    fn init(&self) -> Result<()> {
        let conn = self.conn.lock().unwrap();
        conn.execute(
            "CREATE TABLE IF NOT EXISTS tokens (
                subscription_id TEXT PRIMARY KEY,
                tenant_id TEXT NOT NULL,
                access_token TEXT NOT NULL,
                expires_on TEXT NOT NULL
            )",
            [],
        )?;

        conn.execute(
            "CREATE TABLE IF NOT EXISTS pinned_resource_groups (
                id TEXT PRIMARY KEY,
                subscription_id TEXT NOT NULL,
                subscription_display_name TEXT,
                name TEXT NOT NULL,
                display_order INTEGER NOT NULL
            )",
            [],
        )?;

        // Migrate existing installs that lack the subscription_display_name column
        let _ = conn.execute(
            "ALTER TABLE pinned_resource_groups ADD COLUMN subscription_display_name TEXT",
            [],
        );

        conn.execute(
            "CREATE TABLE IF NOT EXISTS pinned_resources (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                type TEXT NOT NULL,
                resource_group TEXT NOT NULL,
                subscription_id TEXT NOT NULL,
                location TEXT NOT NULL,
                display_order INTEGER NOT NULL,
                group_id TEXT NOT NULL,
                FOREIGN KEY(group_id) REFERENCES pinned_resource_groups(id) ON DELETE CASCADE
            )",
            [],
        )?;

        conn.execute(
            "CREATE TABLE IF NOT EXISTS hidden_subscriptions (
                id TEXT PRIMARY KEY
            )",
            [],
        )?;

        Ok(())
    }

    // --- Token Operations ---

    pub fn save_token(&self, token: &CachedToken) -> Result<()> {
        let conn = self.conn.lock().unwrap();
        conn.execute(
            "INSERT OR REPLACE INTO tokens (subscription_id, tenant_id, access_token, expires_on)
             VALUES (?1, ?2, ?3, ?4)",
            params![token.subscription_id, token.tenant_id, token.access_token, token.expires_on],
        )?;
        Ok(())
    }

    pub fn get_token(&self, subscription_id: &str) -> Result<Option<CachedToken>> {
        let conn = self.conn.lock().unwrap();
        let mut stmt = conn.prepare("SELECT subscription_id, tenant_id, access_token, expires_on FROM tokens WHERE subscription_id = ?1")?;
        let mut rows = stmt.query(params![subscription_id])?;

        if let Some(row) = rows.next()? {
            Ok(Some(CachedToken {
                subscription_id: row.get(0)?,
                tenant_id: row.get(1)?,
                access_token: row.get(2)?,
                expires_on: row.get(3)?,
            }))
        } else {
            Ok(None)
        }
    }

    // --- Pinned Resource Operations ---

    pub fn save_pinned_group(&self, group: &PinnedResourceGroup) -> Result<()> {
        {
            let conn = self.conn.lock().unwrap();
            conn.execute(
                "INSERT OR REPLACE INTO pinned_resource_groups (id, subscription_id, subscription_display_name, name, display_order)
                 VALUES (?1, ?2, ?3, ?4, ?5)",
                params![group.id, group.subscription_id, group.subscription_display_name, group.name, group.display_order],
            )?;
        }

        // For simplicity, we can also manage resources here or separately
        for res in &group.resources {
            self.save_pinned_resource(res, &group.id)?;
        }
        Ok(())
    }

    pub fn ensure_implicit_group(
        &self,
        id: &str,
        subscription_id: &str,
        name: &str,
        subscription_display_name: Option<&str>,
    ) -> Result<()> {
        let conn = self.conn.lock().unwrap();
        conn.execute(
            "INSERT OR IGNORE INTO pinned_resource_groups (id, subscription_id, subscription_display_name, name, display_order)
             VALUES (?1, ?2, ?3, ?4, -1)",
            params![id, subscription_id, subscription_display_name, name],
        )?;
        Ok(())
    }

    pub fn save_pinned_resource(&self, resource: &PinnedResource, group_id: &str) -> Result<()> {
        let conn = self.conn.lock().unwrap();
        conn.execute(
            "INSERT OR REPLACE INTO pinned_resources (id, name, type, resource_group, subscription_id, location, display_order, group_id)
             VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8)",
            params![
                resource.id, resource.name, resource.type_, resource.resource_group,
                resource.subscription_id, resource.location, resource.display_order, group_id
            ],
        )?;
        Ok(())
    }

    pub fn get_pinned_groups(&self) -> Result<Vec<PinnedResourceGroup>> {
        let conn = self.conn.lock().unwrap();
        let mut stmt = conn.prepare("SELECT id, subscription_id, subscription_display_name, name, display_order FROM pinned_resource_groups WHERE display_order >= 0 ORDER BY display_order ASC")?;

        let group_iter = stmt.query_map([], |row| {
            let id: String = row.get(0)?;
            Ok(PinnedResourceGroup {
                id: id.clone(),
                subscription_id: row.get(1)?,
                subscription_display_name: row.get(2)?,
                name: row.get(3)?,
                display_order: row.get(4)?,
                resources: Vec::new(),
            })
        })?;

        let mut groups = Vec::new();
        for group in group_iter {
            groups.push(group?);
        }
        
        drop(stmt);
        drop(conn);

        for group in &mut groups {
            group.resources = self.get_pinned_resources(&group.id).unwrap_or_default();
        }

        Ok(groups)
    }

    pub fn get_orphan_resources(&self) -> Result<Vec<PinnedResource>> {
        let conn = self.conn.lock().unwrap();
        let mut stmt = conn.prepare("SELECT r.id, r.name, r.type, r.resource_group, r.subscription_id, r.location, r.display_order FROM pinned_resources r JOIN pinned_resource_groups g ON r.group_id = g.id WHERE g.display_order < 0 ORDER BY r.display_order ASC")?;
        let res_iter = stmt.query_map([], |row| {
            Ok(PinnedResource {
                id: row.get(0)?,
                name: row.get(1)?,
                type_: row.get(2)?,
                resource_group: row.get(3)?,
                subscription_id: row.get(4)?,
                location: row.get(5)?,
                display_order: row.get(6)?,
            })
        })?;

        let mut resources = Vec::new();
        for res in res_iter {
            resources.push(res?);
        }
        Ok(resources)
    }

    pub fn get_pinned_resources(&self, group_id: &str) -> Result<Vec<PinnedResource>> {
        let conn = self.conn.lock().unwrap();
        let mut stmt = conn.prepare("SELECT id, name, type, resource_group, subscription_id, location, display_order FROM pinned_resources WHERE group_id = ?1 ORDER BY display_order ASC")?;
        let res_iter = stmt.query_map(params![group_id], |row| {
            Ok(PinnedResource {
                id: row.get(0)?,
                name: row.get(1)?,
                type_: row.get(2)?,
                resource_group: row.get(3)?,
                subscription_id: row.get(4)?,
                location: row.get(5)?,
                display_order: row.get(6)?,
            })
        })?;

        let mut resources = Vec::new();
        for res in res_iter {
            resources.push(res?);
        }
        Ok(resources)
    }

    pub fn delete_pinned_group(&self, id: &str) -> Result<()> {
        let conn = self.conn.lock().unwrap();
        // Check if it has resources
        let count: i64 = conn.query_row("SELECT COUNT(*) FROM pinned_resources WHERE group_id = ?1", params![id], |row| row.get(0))?;
        if count > 0 {
            // Keep it but mark as implicit
            conn.execute("UPDATE pinned_resource_groups SET display_order = -1 WHERE id = ?1", params![id])?;
        } else {
            conn.execute("DELETE FROM pinned_resource_groups WHERE id = ?1", params![id])?;
        }
        Ok(())
    }

    pub fn delete_pinned_resource(&self, id: &str) -> Result<()> {
        let conn = self.conn.lock().unwrap();
        // get group id before delete
        let group_id: Result<String> = conn.query_row("SELECT group_id FROM pinned_resources WHERE id = ?1", params![id], |row| row.get(0));
        conn.execute("DELETE FROM pinned_resources WHERE id = ?1", params![id])?;
        
        if let Ok(gid) = group_id {
            let count: i64 = conn.query_row("SELECT COUNT(*) FROM pinned_resources WHERE group_id = ?1", params![gid], |row| row.get(0)).unwrap_or(0);
            let display_order: i32 = conn.query_row("SELECT display_order FROM pinned_resource_groups WHERE id = ?1", params![gid], |row| row.get(0)).unwrap_or(0);
            if count == 0 && display_order < 0 {
                conn.execute("DELETE FROM pinned_resource_groups WHERE id = ?1", params![gid])?;
            }
        }
        Ok(())
    }

    // --- Settings Operations ---
    
    pub fn hide_subscription(&self, id: &str) -> Result<()> {
        let conn = self.conn.lock().unwrap();
        conn.execute("INSERT OR IGNORE INTO hidden_subscriptions (id) VALUES (?1)", params![id])?;
        Ok(())
    }

    pub fn show_subscription(&self, id: &str) -> Result<()> {
        let conn = self.conn.lock().unwrap();
        conn.execute("DELETE FROM hidden_subscriptions WHERE id = ?1", params![id])?;
        Ok(())
    }

    pub fn get_hidden_subscriptions(&self) -> Result<Vec<String>> {
        let conn = self.conn.lock().unwrap();
        let mut stmt = conn.prepare("SELECT id FROM hidden_subscriptions")?;
        let rows = stmt.query_map([], |row| row.get(0))?;
        
        let mut subs = Vec::new();
        for row in rows {
            subs.push(row?);
        }
        Ok(subs)
    }
}

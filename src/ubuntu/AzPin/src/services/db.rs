use rusqlite::{params, Connection, Result};
use std::path::PathBuf;
use crate::models::persistence::{CachedToken, PinnedResourceGroup, PinnedResource};

pub struct Db {
    conn: Connection,
}

impl Db {
    pub fn new() -> Result<Self> {
        let db_path = Self::get_db_path();
        if let Some(parent) = db_path.parent() {
            std::fs::create_dir_all(parent).unwrap_or_default();
        }
        let conn = Connection::open(&db_path)?;
        let db = Self { conn };
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
        self.conn.execute(
            "CREATE TABLE IF NOT EXISTS tokens (
                subscription_id TEXT PRIMARY KEY,
                tenant_id TEXT NOT NULL,
                access_token TEXT NOT NULL,
                expires_on TEXT NOT NULL
            )",
            [],
        )?;

        self.conn.execute(
            "CREATE TABLE IF NOT EXISTS pinned_resource_groups (
                id TEXT PRIMARY KEY,
                subscription_id TEXT NOT NULL,
                name TEXT NOT NULL,
                display_order INTEGER NOT NULL
            )",
            [],
        )?;

        self.conn.execute(
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

        Ok(())
    }

    // --- Token Operations ---

    pub fn save_token(&self, token: &CachedToken) -> Result<()> {
        self.conn.execute(
            "INSERT OR REPLACE INTO tokens (subscription_id, tenant_id, access_token, expires_on)
             VALUES (?1, ?2, ?3, ?4)",
            params![token.subscription_id, token.tenant_id, token.access_token, token.expires_on],
        )?;
        Ok(())
    }

    pub fn get_token(&self, subscription_id: &str) -> Result<Option<CachedToken>> {
        let mut stmt = self.conn.prepare("SELECT subscription_id, tenant_id, access_token, expires_on FROM tokens WHERE subscription_id = ?1")?;
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

    // Add more CRUD operations for PinnedResourceGroup and PinnedResource as needed
}

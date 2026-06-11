use crate::models::persistence::CachedToken;
use crate::services::az_cli::AzCliService;
use crate::services::db::Db;
use chrono::{DateTime, Duration, Utc, TimeZone};
use std::sync::Arc;

pub struct TokenCache {
    db: Arc<Db>,
}

impl TokenCache {
    pub fn new(db: Arc<Db>) -> Self {
        Self { db }
    }

    pub fn get_valid_token(&self, subscription_id: &str) -> Result<String, String> {
        if let Ok(Some(token)) = self.db.get_token(subscription_id) {
            if self.is_token_valid(&token.expires_on) {
                return Ok(token.access_token);
            }
        }

        // Token missing or expiring soon, fetch new one
        let (access_token, expires_on, tenant_id) = AzCliService::get_access_token(subscription_id)?;

        let cached_token = CachedToken {
            subscription_id: subscription_id.to_string(),
            tenant_id,
            access_token: access_token.clone(),
            expires_on,
        };

        if let Err(e) = self.db.save_token(&cached_token) {
            eprintln!("Failed to save token to DB: {}", e);
        }

        Ok(access_token)
    }

    fn is_token_valid(&self, expires_on: &str) -> bool {
        let now = Utc::now();
        let buffer = Duration::minutes(5);

        // Azure CLI sometimes returns RFC3339, sometimes a naive string like "2026-06-10 12:34:56.123456"
        if let Ok(dt) = DateTime::parse_from_rfc3339(expires_on) {
            return dt.with_timezone(&Utc) > now + buffer;
        }

        if let Ok(naive) = chrono::NaiveDateTime::parse_from_str(expires_on, "%Y-%m-%d %H:%M:%S.%f") {
            if let chrono::LocalResult::Single(dt) = chrono::Local.from_local_datetime(&naive) {
                return dt.with_timezone(&Utc) > now + buffer;
            }
        }
        
        if let Ok(naive) = chrono::NaiveDateTime::parse_from_str(expires_on, "%Y-%m-%d %H:%M:%S") {
            if let chrono::LocalResult::Single(dt) = chrono::Local.from_local_datetime(&naive) {
                return dt.with_timezone(&Utc) > now + buffer;
            }
        }

        // If we can't parse it, consider it invalid so we fetch a new one
        false
    }
}

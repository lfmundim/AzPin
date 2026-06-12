use crate::models::persistence::CachedToken;
use crate::services::az_cli::AzCliService;
use crate::services::db::Db;
use chrono::{DateTime, Duration, Utc};
use std::sync::Arc;

pub struct TokenCache {
    db: Arc<Db>,
}

impl TokenCache {
    pub fn new(db: Arc<Db>) -> Self {
        Self { db }
    }

    pub async fn get_valid_token(&self, subscription_id: &str) -> Result<String, String> {
        if let Ok(Some(token)) = self.db.get_token(subscription_id) {
            if self.is_token_valid(&token.expires_on) {
                return Ok(token.access_token);
            }
        }

        let (access_token, expires_on, tenant_id) =
            AzCliService::get_access_token(subscription_id).await?;

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

        if let Ok(dt) = DateTime::parse_from_rfc3339(expires_on) {
            return dt.with_timezone(&Utc) > now + buffer;
        }
        false
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::services::db::Db;
    use chrono::Duration;
    use std::sync::Arc;

    fn make_cache() -> TokenCache {
        let db = Arc::new(Db::new_in_memory().expect("in-memory DB"));
        TokenCache::new(db)
    }

    #[test]
    fn is_token_valid_not_expired() {
        let cache = make_cache();
        let future = (Utc::now() + Duration::hours(2)).to_rfc3339();
        assert!(cache.is_token_valid(&future));
    }

    #[test]
    fn is_token_valid_expired() {
        let cache = make_cache();
        let past = (Utc::now() - Duration::hours(1)).to_rfc3339();
        assert!(!cache.is_token_valid(&past));
    }

    #[test]
    fn is_token_valid_within_buffer() {
        let cache = make_cache();
        let soon = (Utc::now() + Duration::minutes(3)).to_rfc3339();
        assert!(!cache.is_token_valid(&soon));
    }

    #[test]
    fn is_token_valid_garbage_returns_false() {
        let cache = make_cache();
        assert!(!cache.is_token_valid("not-a-date"));
    }
}

use crate::services::token_cache::TokenCache;
use reqwest::Client;
use std::collections::HashMap;
use std::sync::{Arc, Mutex};

const ARM_BASE_URL: &str = "https://management.azure.com";
const PERMISSIONS_API_VERSION: &str = "2022-04-01";

const START_ACTION: &str = "microsoft.web/sites/start/action";
const STOP_ACTION: &str = "microsoft.web/sites/stop/action";

pub struct PermissionsService {
    client: Client,
    token_cache: Arc<TokenCache>,
    cache: Mutex<HashMap<String, bool>>,
}

impl PermissionsService {
    pub fn new(token_cache: Arc<TokenCache>) -> Self {
        Self {
            client: Client::new(),
            token_cache,
            cache: Mutex::new(HashMap::new()),
        }
    }

    pub async fn can_manage(&self, subscription_id: &str, resource_id: &str) -> bool {
        if let Ok(cache) = self.cache.lock() {
            if let Some(&cached) = cache.get(resource_id) {
                return cached;
            }
        }

        let result = self.check_access(subscription_id, resource_id).await;

        if let Ok(mut cache) = self.cache.lock() {
            cache.insert(resource_id.to_string(), result);
        }

        result
    }

    async fn check_access(&self, subscription_id: &str, resource_id: &str) -> bool {
        let token = match self.token_cache.get_valid_token(subscription_id).await {
            Ok(t) => t,
            Err(_) => return false,
        };

        let url = format!(
            "{}{}/providers/Microsoft.Authorization/permissions?api-version={}",
            ARM_BASE_URL, resource_id, PERMISSIONS_API_VERSION
        );

        let res = match self
            .client
            .get(&url)
            .header("Authorization", format!("Bearer {}", token))
            .send()
            .await
        {
            Ok(r) => r,
            Err(_) => return false,
        };

        if !res.status().is_success() {
            return false;
        }

        let body: serde_json::Value = match res.json().await {
            Ok(v) => v,
            Err(_) => return false,
        };

        let entries = match body.get("value").and_then(|v| v.as_array()) {
            Some(e) => e,
            None => return false,
        };

        for entry in entries {
            let actions: Vec<&str> = entry
                .get("actions")
                .and_then(|a| a.as_array())
                .map(|a| a.iter().filter_map(|v| v.as_str()).collect())
                .unwrap_or_default();

            let not_actions: Vec<&str> = entry
                .get("notActions")
                .and_then(|a| a.as_array())
                .map(|a| a.iter().filter_map(|v| v.as_str()).collect())
                .unwrap_or_default();

            let grants = actions
                .iter()
                .any(|p| arm_pattern_matches(p, START_ACTION) || arm_pattern_matches(p, STOP_ACTION));

            let denied = not_actions
                .iter()
                .any(|p| arm_pattern_matches(p, START_ACTION) || arm_pattern_matches(p, STOP_ACTION));

            if grants && !denied {
                return true;
            }
        }

        false
    }
}

fn arm_pattern_matches(pattern: &str, action: &str) -> bool {
    let p = pattern.to_lowercase();
    let a = action.to_lowercase();
    if p == "*" || p == a {
        return true;
    }
    if let Some(prefix) = p.strip_suffix("/*") {
        return a.starts_with(&format!("{}/", prefix)) || a == prefix;
    }
    false
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn arm_pattern_exact_match() {
        assert!(arm_pattern_matches(
            "microsoft.web/sites/start/action",
            "microsoft.web/sites/start/action"
        ));
    }

    #[test]
    fn arm_pattern_full_wildcard() {
        assert!(arm_pattern_matches("*", "microsoft.web/sites/start/action"));
        assert!(arm_pattern_matches("*", "microsoft.web/sites/stop/action"));
    }

    #[test]
    fn arm_pattern_trailing_wildcard() {
        assert!(arm_pattern_matches(
            "microsoft.web/sites/*",
            "microsoft.web/sites/start/action"
        ));
        assert!(arm_pattern_matches(
            "microsoft.web/*",
            "microsoft.web/sites/start/action"
        ));
        assert!(arm_pattern_matches(
            "Microsoft.Web/sites/*",
            "microsoft.web/sites/stop/action"
        ));
    }

    #[test]
    fn arm_pattern_no_match() {
        assert!(!arm_pattern_matches(
            "microsoft.storage/*",
            "microsoft.web/sites/start/action"
        ));
        assert!(!arm_pattern_matches(
            "microsoft.web/sites/read",
            "microsoft.web/sites/start/action"
        ));
    }
}

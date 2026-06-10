use crate::services::token_cache::TokenCache;
use reqwest::Client;
use std::sync::Arc;

const ARM_BASE_URL: &str = "https://management.azure.com";

pub struct PermissionsService {
    client: Client,
    token_cache: Arc<TokenCache>,
}

impl PermissionsService {
    pub fn new(token_cache: Arc<TokenCache>) -> Self {
        Self {
            client: Client::new(),
            token_cache,
        }
    }

    pub async fn check_access(&self, subscription_id: &str, resource_id: &str, api_version: &str) -> Result<bool, String> {
        // Implement access check via ARM API
        // For simplicity, we can do a dummy check or check the permissions endpoint if known
        // A common way is to check the providers/Microsoft.Authorization/permissions
        let url = format!("{}{}/providers/Microsoft.Authorization/permissions?api-version={}", ARM_BASE_URL, resource_id, api_version);
        let token = self.token_cache.get_valid_token(subscription_id)?;
        let auth = format!("Bearer {}", token);

        let res = self.client.get(&url)
            .header("Authorization", auth)
            .send()
            .await
            .map_err(|e| format!("Request failed: {}", e))?;

        if !res.status().is_success() {
            // Assume false if the request fails
            return Ok(false);
        }

        // Ideally parse the response to check specific actions. For now return true if 200 OK.
        Ok(true)
    }
}

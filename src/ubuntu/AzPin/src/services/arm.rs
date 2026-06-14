use crate::models::arm::{ArmResource, ArmResourceGroup};
use crate::services::token_cache::TokenCache;
use crate::utils::resource_type::ResourceState;
use reqwest::Client;
use std::sync::Arc;

const ARM_BASE_URL: &str = "https://management.azure.com";

pub struct ArmService {
    client: Client,
    token_cache: Arc<TokenCache>,
}

impl ArmService {
    pub fn new(token_cache: Arc<TokenCache>) -> Self {
        Self {
            client: Client::new(),
            token_cache,
        }
    }

    async fn get_auth_header(&self, subscription_id: &str) -> Result<String, String> {
        let token = self.token_cache.get_valid_token(subscription_id).await?;
        Ok(format!("Bearer {}", token.trim()))
    }

    pub async fn fetch_resource_groups(
        &self,
        subscription_id: &str,
    ) -> Result<Vec<ArmResourceGroup>, String> {
        let url = format!(
            "{}/subscriptions/{}/resourcegroups?api-version=2021-04-01",
            ARM_BASE_URL, subscription_id
        );
        let auth = self.get_auth_header(subscription_id).await?;

        let res = self
            .client
            .get(&url)
            .header("Authorization", auth)
            .send()
            .await
            .map_err(|e| format!("Request failed: {}", e))?;

        if !res.status().is_success() {
            let status = res.status();
            let err_body = res.text().await.unwrap_or_default();
            return Err(format!("ARM API error: {} - {}", status, err_body));
        }

        #[derive(serde::Deserialize)]
        struct ArmResponse {
            value: Vec<ArmResourceGroup>,
        }

        let body: ArmResponse = res
            .json()
            .await
            .map_err(|e| format!("Failed to parse response: {}", e))?;
        Ok(body.value)
    }

    pub async fn fetch_resources(
        &self,
        subscription_id: &str,
        resource_group: &str,
    ) -> Result<Vec<ArmResource>, String> {
        let url = format!(
            "{}/subscriptions/{}/resourceGroups/{}/resources?api-version=2021-04-01",
            ARM_BASE_URL, subscription_id, resource_group
        );
        let auth = self.get_auth_header(subscription_id).await?;

        let res = self
            .client
            .get(&url)
            .header("Authorization", auth)
            .send()
            .await
            .map_err(|e| format!("Request failed: {}", e))?;

        if !res.status().is_success() {
            let status = res.status();
            let err_body = res.text().await.unwrap_or_default();
            return Err(format!("ARM API error: {} - {}", status, err_body));
        }

        #[derive(serde::Deserialize)]
        struct ArmResponse {
            value: Vec<ArmResource>,
        }

        let body: ArmResponse = res
            .json()
            .await
            .map_err(|e| format!("Failed to parse response: {}", e))?;
        Ok(body.value)
    }

    fn get_api_version(resource_id: &str) -> &'static str {
        let id_lower = resource_id.to_lowercase();
        if id_lower.contains("microsoft.app/containerapps") {
            "2023-05-01"
        } else if id_lower.contains("microsoft.logic/workflows") {
            "2019-05-01"
        } else if id_lower.contains("microsoft.compute/virtualmachines") {
            "2023-09-01"
        } else {
            "2023-01-01"
        }
    }

    pub async fn get_resource_state(
        &self,
        subscription_id: &str,
        resource_id: &str,
    ) -> Result<ResourceState, String> {
        let is_vm = resource_id
            .to_lowercase()
            .contains("microsoft.compute/virtualmachines");
        let api_version = Self::get_api_version(resource_id);

        let url = if is_vm {
            format!(
                "{}{}?api-version={}&$expand=instanceView",
                ARM_BASE_URL, resource_id, api_version
            )
        } else {
            format!("{}{}?api-version={}", ARM_BASE_URL, resource_id, api_version)
        };

        let auth = self.get_auth_header(subscription_id).await?;

        let res = self
            .client
            .get(&url)
            .header("Authorization", auth)
            .send()
            .await
            .map_err(|e| format!("Request failed: {}", e))?;

        if !res.status().is_success() {
            let status = res.status();
            let err_body = res.text().await.unwrap_or_default();
            return Err(format!("ARM API error: {} - {}", status, err_body));
        }

        let body: serde_json::Value = res
            .json()
            .await
            .map_err(|e| format!("Failed to parse response: {}", e))?;

        if is_vm {
            if let Some(statuses) = body
                .pointer("/properties/instanceView/statuses")
                .and_then(|s| s.as_array())
            {
                for status in statuses {
                    if let Some(code) = status.get("code").and_then(|c| c.as_str()) {
                        if let Some(power) = code.strip_prefix("PowerState/") {
                            return Ok(ResourceState::from_str(power));
                        }
                    }
                }
            }
        }

        if let Some(props) = body.get("properties") {
            for field in &["state", "runningStatus", "runningState", "powerState", "provisioningState"] {
                if let Some(s) = props.get(field).and_then(|v| v.as_str()) {
                    return Ok(ResourceState::from_str(s));
                }
            }
        }

        Ok(ResourceState::Unknown)
    }

    fn get_action_url(resource_id: &str, action: &str) -> String {
        let api_version = Self::get_api_version(resource_id);
        let id_lower = resource_id.to_lowercase();

        let mapped_action = if id_lower.contains("microsoft.logic/workflows") {
            match action {
                "start" => "enable",
                "stop" => "disable",
                _ => action,
            }
        } else if id_lower.contains("microsoft.compute/virtualmachines") && action == "stop" {
            "powerOff"
        } else {
            action
        };

        format!(
            "{}{}/{}?api-version={}",
            ARM_BASE_URL, resource_id, mapped_action, api_version
        )
    }

    pub async fn start_resource(&self, subscription_id: &str, resource_id: &str) -> Result<(), String> {
        let url = Self::get_action_url(resource_id, "start");
        let auth = self.get_auth_header(subscription_id).await?;
        self.post_action(&url, &auth).await
    }

    pub async fn stop_resource(&self, subscription_id: &str, resource_id: &str) -> Result<(), String> {
        let url = Self::get_action_url(resource_id, "stop");
        let auth = self.get_auth_header(subscription_id).await?;
        self.post_action(&url, &auth).await
    }

    pub async fn restart_resource(
        &self,
        subscription_id: &str,
        resource_id: &str,
    ) -> Result<(), String> {
        if resource_id
            .to_lowercase()
            .contains("microsoft.app/containerapps")
        {
            self.stop_resource(subscription_id, resource_id).await?;
            return self.start_resource(subscription_id, resource_id).await;
        }

        let url = Self::get_action_url(resource_id, "restart");
        let auth = self.get_auth_header(subscription_id).await?;
        self.post_action(&url, &auth).await
    }

    async fn post_action(&self, url: &str, auth: &str) -> Result<(), String> {
        let res = self
            .client
            .post(url)
            .header("Authorization", auth)
            .header("Content-Length", "0")
            .send()
            .await
            .map_err(|e| format!("Request failed: {}", e))?;

        if !res.status().is_success() {
            let status = res.status();
            let err_body = res.text().await.unwrap_or_default();
            return Err(format!("ARM API error: {} - {}", status, err_body));
        }

        Ok(())
    }
}

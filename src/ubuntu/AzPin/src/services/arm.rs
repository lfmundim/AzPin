use crate::models::arm::{ArmResource, ArmResourceGroup};
use crate::services::token_cache::TokenCache;
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

    fn get_auth_header(&self, subscription_id: &str) -> Result<String, String> {
        let token = self.token_cache.get_valid_token(subscription_id)?;
        Ok(format!("Bearer {}", token))
    }

    pub async fn fetch_resource_groups(&self, subscription_id: &str) -> Result<Vec<ArmResourceGroup>, String> {
        let url = format!("{}/subscriptions/{}/resourcegroups?api-version=2021-04-01", ARM_BASE_URL, subscription_id);
        let auth = self.get_auth_header(subscription_id)?;

        let res = self.client.get(&url)
            .header("Authorization", auth)
            .send()
            .await
            .map_err(|e| format!("Request failed: {}", e))?;

        if !res.status().is_success() {
            return Err(format!("ARM API error: {}", res.status()));
        }

        #[derive(serde::Deserialize)]
        struct ArmResponse {
            value: Vec<ArmResourceGroup>,
        }

        let body: ArmResponse = res.json().await.map_err(|e| format!("Failed to parse response: {}", e))?;
        Ok(body.value)
    }

    pub async fn fetch_resources(&self, subscription_id: &str, resource_group: &str) -> Result<Vec<ArmResource>, String> {
        let url = format!("{}/subscriptions/{}/resourceGroups/{}/resources?api-version=2021-04-01", ARM_BASE_URL, subscription_id, resource_group);
        let auth = self.get_auth_header(subscription_id)?;

        let res = self.client.get(&url)
            .header("Authorization", auth)
            .send()
            .await
            .map_err(|e| format!("Request failed: {}", e))?;

        if !res.status().is_success() {
            return Err(format!("ARM API error: {}", res.status()));
        }

        #[derive(serde::Deserialize)]
        struct ArmResponse {
            value: Vec<ArmResource>,
        }

        let body: ArmResponse = res.json().await.map_err(|e| format!("Failed to parse response: {}", e))?;
        Ok(body.value)
    }

    pub async fn get_resource_state(&self, subscription_id: &str, resource_id: &str, api_version: &str) -> Result<String, String> {
        let is_vm = resource_id.to_lowercase().contains("microsoft.compute/virtualmachines");
        let url = if is_vm {
            format!("{}{}?api-version={}&$expand=instanceView", ARM_BASE_URL, resource_id, api_version)
        } else {
            format!("{}{}?api-version={}", ARM_BASE_URL, resource_id, api_version)
        };
        
        let auth = self.get_auth_header(subscription_id)?;

        let res = self.client.get(&url)
            .header("Authorization", auth)
            .send()
            .await
            .map_err(|e| format!("Request failed: {}", e))?;

        if !res.status().is_success() {
            return Err(format!("ARM API error: {}", res.status()));
        }

        let body: serde_json::Value = res.json().await.map_err(|e| format!("Failed to parse response: {}", e))?;
        
        if is_vm {
            if let Some(instance_view) = body.get("properties").and_then(|p| p.get("instanceView")) {
                if let Some(statuses) = instance_view.get("statuses").and_then(|s| s.as_array()) {
                    for status in statuses {
                        if let Some(code) = status.get("code").and_then(|c| c.as_str()) {
                            if code.starts_with("PowerState/") {
                                return Ok(code.replace("PowerState/", ""));
                            }
                        }
                    }
                }
            }
        }
        
        if let Some(props) = body.get("properties") {
            if let Some(state) = props.get("state").and_then(|v| v.as_str()) {
                return Ok(state.to_string());
            }
            if let Some(state) = props.get("runningState").and_then(|v| v.as_str()) {
                return Ok(state.to_string());
            }
            if let Some(state) = props.get("powerState").and_then(|v| v.as_str()) {
                return Ok(state.to_string());
            }
            if let Some(state) = props.get("provisioningState").and_then(|v| v.as_str()) {
                return Ok(state.to_string());
            }
        }
        
        Ok("Unknown".to_string())
    }

    pub async fn start_resource(&self, subscription_id: &str, resource_id: &str, api_version: &str) -> Result<(), String> {
        let url = format!("{}{}/start?api-version={}", ARM_BASE_URL, resource_id, api_version);
        let auth = self.get_auth_header(subscription_id)?;

        let res = self.client.post(&url)
            .header("Authorization", auth)
            .send()
            .await
            .map_err(|e| format!("Request failed: {}", e))?;

        if !res.status().is_success() {
            return Err(format!("ARM API error: {}", res.status()));
        }

        Ok(())
    }

    pub async fn stop_resource(&self, subscription_id: &str, resource_id: &str, api_version: &str) -> Result<(), String> {
        let action = if resource_id.to_lowercase().contains("microsoft.compute/virtualmachines") {
            "powerOff"
        } else {
            "stop"
        };
        let url = format!("{}{}/{}?api-version={}", ARM_BASE_URL, resource_id, action, api_version);
        let auth = self.get_auth_header(subscription_id)?;

        let res = self.client.post(&url)
            .header("Authorization", auth)
            .send()
            .await
            .map_err(|e| format!("Request failed: {}", e))?;

        if !res.status().is_success() {
            return Err(format!("ARM API error: {}", res.status()));
        }

        Ok(())
    }

    pub async fn restart_resource(&self, subscription_id: &str, resource_id: &str, api_version: &str) -> Result<(), String> {
        let url = format!("{}{}/restart?api-version={}", ARM_BASE_URL, resource_id, api_version);
        let auth = self.get_auth_header(subscription_id)?;

        let res = self.client.post(&url)
            .header("Authorization", auth)
            .send()
            .await
            .map_err(|e| format!("Request failed: {}", e))?;

        if !res.status().is_success() {
            return Err(format!("ARM API error: {}", res.status()));
        }

        Ok(())
    }
}

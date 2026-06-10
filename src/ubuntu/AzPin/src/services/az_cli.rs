use std::process::Command;
use serde::Deserialize;

#[derive(Deserialize)]
struct AzTokenResponse {
    #[serde(rename = "accessToken")]
    pub access_token: String,
    #[serde(rename = "expiresOn")]
    pub expires_on: String,
    pub tenant: String,
}

#[derive(Deserialize, Debug)]
pub struct AzSubscription {
    pub id: String,
    pub name: String,
    #[serde(rename = "tenantId")]
    pub tenant_id: String,
    #[serde(rename = "isDefault")]
    pub is_default: bool,
    pub state: String,
}

pub struct AzCliService;

impl AzCliService {
    pub fn get_access_token(subscription_id: &str) -> Result<(String, String, String), String> {
        let output = Command::new("az")
            .args(["account", "get-access-token", "--subscription", subscription_id, "--output", "json"])
            .output()
            .map_err(|e| format!("Failed to execute az cli: {}", e))?;

        if !output.status.success() {
            let err_msg = String::from_utf8_lossy(&output.stderr);
            return Err(format!("az cli error: {}", err_msg));
        }

        let resp: AzTokenResponse = serde_json::from_slice(&output.stdout)
            .map_err(|e| format!("Failed to parse az output: {}", e))?;

        Ok((resp.access_token, resp.expires_on, resp.tenant))
    }

    pub fn get_default_subscription() -> Result<AzSubscription, String> {
        let output = Command::new("az")
            .args(["account", "show", "--output", "json"])
            .output()
            .map_err(|e| format!("Failed to execute az cli: {}", e))?;

        if !output.status.success() {
            let err_msg = String::from_utf8_lossy(&output.stderr);
            return Err(format!("az cli error: {}", err_msg));
        }

        let resp: AzSubscription = serde_json::from_slice(&output.stdout)
            .map_err(|e| format!("Failed to parse az output: {}", e))?;

        Ok(resp)
    }

    pub fn list_subscriptions() -> Result<Vec<AzSubscription>, String> {
        let output = Command::new("az")
            .args(["account", "list", "--output", "json"])
            .output()
            .map_err(|e| format!("Failed to execute az cli: {}", e))?;

        if !output.status.success() {
            let err_msg = String::from_utf8_lossy(&output.stderr);
            return Err(format!("az cli error: {}", err_msg));
        }

        let resp: Vec<AzSubscription> = serde_json::from_slice(&output.stdout)
            .map_err(|e| format!("Failed to parse az output: {}", e))?;

        Ok(resp)
    }
}

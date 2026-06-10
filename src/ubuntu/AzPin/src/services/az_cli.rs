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
}

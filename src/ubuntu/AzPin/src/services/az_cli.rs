use tokio::process::Command;
use serde::Deserialize;
use chrono::{DateTime, Utc};

#[derive(Deserialize)]
struct AzTokenResponse {
    #[serde(rename = "accessToken")]
    pub access_token: String,
    #[serde(rename = "expiresOn")]
    pub expires_on: String,
    pub tenant: String,
}

#[derive(Deserialize, Debug, Clone)]
pub struct AzUser {
    pub name: String,
}

#[derive(Deserialize, Debug, Clone)]
pub struct AzSubscription {
    pub id: String,
    pub name: String,
    #[serde(rename = "tenantId")]
    pub tenant_id: String,
    #[serde(rename = "isDefault")]
    pub is_default: bool,
    pub state: String,
    #[serde(default)]
    pub user: Option<AzUser>,
}

pub struct AzCliService;

fn resolve_az_path() -> &'static str {
    for path in &["/usr/bin/az", "/usr/local/bin/az", "/snap/bin/az"] {
        if std::path::Path::new(path).exists() {
            return path;
        }
    }
    "az"
}

fn parse_az_expiry(s: &str) -> DateTime<Utc> {
    if let Ok(dt) = chrono::NaiveDateTime::parse_from_str(s, "%Y-%m-%d %H:%M:%S%.f") {
        return DateTime::from_naive_utc_and_offset(dt, Utc);
    }
    if let Ok(dt) = DateTime::parse_from_rfc3339(s) {
        return dt.with_timezone(&Utc);
    }
    Utc::now() + chrono::Duration::hours(1)
}

impl AzCliService {
    pub async fn get_access_token(subscription_id: &str) -> Result<(String, String, String), String> {
        let output = Command::new(resolve_az_path())
            .args([
                "account",
                "get-access-token",
                "--subscription",
                subscription_id,
                "--resource",
                "https://management.azure.com/",
                "--output",
                "json",
            ])
            .output()
            .await
            .map_err(|e| format!("Failed to execute az cli: {}", e))?;

        if !output.status.success() {
            let err_msg = String::from_utf8_lossy(&output.stderr);
            return Err(format!("az cli error: {}", err_msg));
        }

        let resp: AzTokenResponse = serde_json::from_slice(&output.stdout)
            .map_err(|e| format!("Failed to parse az output: {}", e))?;

        let expires_on_rfc = parse_az_expiry(&resp.expires_on).to_rfc3339();
        Ok((resp.access_token, expires_on_rfc, resp.tenant))
    }

    pub async fn get_default_subscription() -> Result<AzSubscription, String> {
        let output = Command::new(resolve_az_path())
            .args(["account", "show", "--output", "json"])
            .output()
            .await
            .map_err(|e| format!("Failed to execute az cli: {}", e))?;

        if !output.status.success() {
            let err_msg = String::from_utf8_lossy(&output.stderr);
            return Err(format!("az cli error: {}", err_msg));
        }

        let resp: AzSubscription = serde_json::from_slice(&output.stdout)
            .map_err(|e| format!("Failed to parse az output: {}", e))?;

        Ok(resp)
    }

    pub async fn list_subscriptions() -> Result<Vec<AzSubscription>, String> {
        let output = Command::new(resolve_az_path())
            .args(["account", "list", "--output", "json"])
            .output()
            .await
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

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn parse_az_expiry_az_cli_format() {
        let dt = parse_az_expiry("2026-06-12 15:30:00.123456");
        assert_eq!(dt.format("%Y-%m-%d %H:%M:%S").to_string(), "2026-06-12 15:30:00");
    }

    #[test]
    fn parse_az_expiry_rfc3339_fallback() {
        let dt = parse_az_expiry("2026-06-12T15:30:00Z");
        assert_eq!(dt.format("%Y-%m-%d %H:%M:%S").to_string(), "2026-06-12 15:30:00");
    }

    #[test]
    fn parse_az_expiry_garbage_returns_future() {
        let before = Utc::now();
        let dt = parse_az_expiry("not-a-date");
        assert!(dt > before);
        assert!(dt <= Utc::now() + chrono::Duration::hours(2));
    }
}

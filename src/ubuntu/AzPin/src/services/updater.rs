use serde::Deserialize;
use std::env;
use reqwest::Client;

#[derive(Deserialize)]
struct GitHubRelease {
    tag_name: String,
    html_url: String,
}

#[derive(Debug, Clone, PartialEq)]
pub enum UpdateCheckState {
    Idle,
    Checking,
    UpToDate { version: String },
    UpdateAvailable { current: String, latest: String, release_url: String },
    Failed(String),
}

pub struct UpdaterService {
    client: Client,
}

impl UpdaterService {
    pub fn new() -> Self {
        Self {
            client: Client::new(),
        }
    }

    pub async fn check_for_updates(&self) -> UpdateCheckState {
        let current_version = env!("CARGO_PKG_VERSION");
        
        let url = "https://api.github.com/repos/lfmundim/AzPin/releases/latest";
        let res = match self.client.get(url)
            .header("User-Agent", "AzPin")
            .header("Accept", "application/vnd.github+json")
            .send()
            .await 
        {
            Ok(r) => r,
            Err(e) => return UpdateCheckState::Failed(e.to_string()),
        };

        if !res.status().is_success() {
            return UpdateCheckState::Failed(format!("GitHub API error: {}", res.status()));
        }

        let release: GitHubRelease = match res.json().await {
            Ok(r) => r,
            Err(e) => return UpdateCheckState::Failed(format!("Failed to parse release: {}", e)),
        };

        let latest_version = release.tag_name.trim_start_matches('v').trim_start_matches('V');
        
        if Self::is_newer(latest_version, current_version) {
            UpdateCheckState::UpdateAvailable {
                current: current_version.to_string(),
                latest: latest_version.to_string(),
                release_url: release.html_url,
            }
        } else {
            UpdateCheckState::UpToDate { version: current_version.to_string() }
        }
    }

    fn is_newer(latest: &str, current: &str) -> bool {
        let parse = |v: &str| -> Vec<u32> {
            v.split('.').filter_map(|s| s.parse().ok()).collect()
        };
        let l = parse(latest);
        let c = parse(current);
        
        let max_len = std::cmp::max(l.len(), c.len());
        for i in 0..max_len {
            let lv = l.get(i).copied().unwrap_or(0);
            let cv = c.get(i).copied().unwrap_or(0);
            if lv != cv {
                return lv > cv;
            }
        }
        false
    }
}

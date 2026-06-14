use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct PinnedResourceGroup {
    pub id: String,
    pub subscription_id: String,
    pub subscription_display_name: Option<String>,
    pub name: String,
    pub display_order: i32,
    pub resources: Vec<PinnedResource>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct PinnedResource {
    pub id: String,
    pub name: String,
    pub type_: String,
    pub resource_group: String,
    pub subscription_id: String,
    pub location: String,
    pub display_order: i32,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CachedToken {
    pub subscription_id: String,
    pub tenant_id: String,
    pub access_token: String,
    pub expires_on: String,
}

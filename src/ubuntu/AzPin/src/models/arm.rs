use serde::Deserialize;
use std::collections::HashMap;

#[derive(Debug, Clone, Deserialize)]
pub struct ArmResourceGroup {
    pub id: String,
    pub name: String,
    pub location: String,
}

#[derive(Debug, Clone, Deserialize)]
pub struct ArmResource {
    pub id: String,
    pub name: String,
    #[serde(rename = "type")]
    pub type_: String,
    pub location: String,
    #[serde(default)]
    pub tags: Option<HashMap<String, String>>,
}

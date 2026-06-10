use serde::Deserialize;

#[derive(Debug, Clone, Deserialize)]
pub struct ArmResourceGroup {
    pub id: String,
    pub name: String,
    pub location: String,
    // Add other fields mapped from Azure JSON as needed
}

#[derive(Debug, Clone, Deserialize)]
pub struct ArmResource {
    pub id: String,
    pub name: String,
    #[serde(rename = "type")]
    pub type_: String,
    pub location: String,
    // Add other fields mapped from Azure JSON as needed
}

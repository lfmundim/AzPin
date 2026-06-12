pub fn get_icon_for_type(resource_type: &str) -> &'static str {
    match resource_type.to_lowercase().as_str() {
        "microsoft.compute/virtualmachines" => "computer-symbolic",
        "microsoft.sql/servers"
        | "microsoft.documentdb/databaseaccounts"
        | "microsoft.sql/managedinstances" => "drive-harddisk-symbolic",
        "microsoft.web/sites" | "microsoft.web/sites/slots" => "applications-internet-symbolic",
        "microsoft.storage/storageaccounts" => "folder-symbolic",
        "microsoft.network/virtualnetworks" | "microsoft.network/loadbalancers" => {
            "network-workgroup-symbolic"
        }
        "microsoft.app/containerapps" => "package-x-generic-symbolic",
        "microsoft.keyvault/vaults" => "dialog-password-symbolic",
        "microsoft.servicebus/namespaces" => "mail-send-symbolic",
        "microsoft.logic/workflows" => "system-run-symbolic",
        _ => "emblem-system-symbolic",
    }
}

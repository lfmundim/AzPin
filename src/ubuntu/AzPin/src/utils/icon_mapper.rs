pub fn get_gnome_icon(sf_symbol: &str) -> &'static str {
    match sf_symbol {
        // Core shapes and cloud
        "cloud.fill" => "weather-overcast-symbolic",
        "cloud" => "weather-overcast",
        
        // Operations
        "play.fill" => "media-playback-start-symbolic",
        "stop.fill" => "media-playback-stop-symbolic",
        "arrow.clockwise" => "view-refresh-symbolic",
        
        // Resources
        "folder.fill" => "folder-symbolic",
        "desktopcomputer" => "computer-symbolic",
        "server.rack" => "network-server-symbolic",
        "database" => "drive-harddisk-symbolic",
        
        // Fallback
        _ => "emblem-system-symbolic",
    }
}

pub fn get_icon_for_type(resource_type: &str) -> &'static str {
    let lower = resource_type.to_lowercase();
    if lower.contains("virtualmachine") || lower.contains("compute") {
        "computer-symbolic"
    } else if lower.contains("database") || lower.contains("sql") {
        "drive-harddisk-symbolic"
    } else if lower.contains("web/sites") {
        "applications-internet-symbolic"
    } else if lower.contains("storage") {
        "folder-symbolic"
    } else if lower.contains("network") {
        "network-workgroup-symbolic"
    } else {
        "emblem-system-symbolic"
    }
}

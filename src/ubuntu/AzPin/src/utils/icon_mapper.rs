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

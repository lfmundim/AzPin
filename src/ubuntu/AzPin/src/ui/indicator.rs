use std::sync::Arc;
use gtk4::gio;
use appindicator3::prelude::*;
use crate::services::db::Db;
use crate::services::arm::ArmService;
// Note: AppIndicator with GTK4 typically requires exporting the gio::Menu over DBus
// or using a specific GTK4 compatible tray library. We are conceptually implementing
// the requested interface here.
use appindicator3::{Indicator, IndicatorCategory, IndicatorStatus};

pub struct IndicatorApp {
    indicator: Indicator,
    db: Arc<Db>,
    arm_service: Arc<ArmService>,
}

impl IndicatorApp {
    pub fn new(db: Arc<Db>, arm_service: Arc<ArmService>) -> Self {
        let indicator = Indicator::new("azpin", "weather-overcast", IndicatorCategory::ApplicationStatus);
        indicator.set_status(IndicatorStatus::Active);

        let app = Self { indicator, db, arm_service };
        app.build_menu();
        app
    }

    pub fn build_menu(&self) {
        // GTK4 uses gio::Menu instead of the deprecated gtk::Menu.
        // We build a gio::Menu model which can be exported or attached to the indicator.
        let menu = gio::Menu::new();

        // Top item: Authentication Status
        let auth_status = self.get_auth_status();
        let auth_item = gio::MenuItem::new(Some(&auth_status), None);
        menu.append_item(&auth_item);

        // Iterate over PinnedResourceGroups
        if let Ok(groups) = self.db.get_pinned_groups() {
            for group in groups {
                let group_menu = gio::Menu::new();
                
                for res in &group.resources {
                    // Resource Submenu for actions
                    let res_menu = gio::Menu::new();
                    
                    // Open in Portal Action
                    let open_action_name = format!("app.open_{}", res.id);
                    res_menu.append_item(&gio::MenuItem::new(Some("Open in Portal"), Some(&open_action_name)));
                    
                    // State mutations
                    let start_action_name = format!("app.start_{}", res.id);
                    res_menu.append_item(&gio::MenuItem::new(Some("Start"), Some(&start_action_name)));
                    
                    let stop_action_name = format!("app.stop_{}", res.id);
                    res_menu.append_item(&gio::MenuItem::new(Some("Stop"), Some(&stop_action_name)));
                    
                    let restart_action_name = format!("app.restart_{}", res.id);
                    res_menu.append_item(&gio::MenuItem::new(Some("Restart"), Some(&restart_action_name)));

                    let res_item = gio::MenuItem::new_submenu(Some(&res.name), &res_menu);
                    group_menu.append_item(&res_item);
                }

                let group_item = gio::MenuItem::new_submenu(Some(&group.name), &group_menu);
                menu.append_item(&group_item);
            }
        }

        // Note: Connecting the "about-to-show" signal is conceptually replaced by 
        // updating the gio::Menu dynamically or relying on action state queries in GTK4.
        
        // Pseudo-code to bind the menu:
        // self.indicator.set_menu(&mut menu_wrapper); 
    }

    fn get_auth_status(&self) -> String {
        // Fetch first available token or report not signed in
        // Since we don't have a default subscription_id handy without context,
        // we might query the DB for any token to check login status.
        // Assuming we just want a placeholder logic here:
        "⚠️ Not signed in".to_string()
    }

    // Pseudo-method to demonstrate handling action
    pub fn handle_open_portal(&self, uri: &str) {
        if let Err(e) = gio::AppInfo::launch_default_for_uri(uri, None::<&gio::AppLaunchContext>) {
            eprintln!("Failed to open portal: {}", e);
        }
    }
}

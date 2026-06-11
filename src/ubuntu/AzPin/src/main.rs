use adw::prelude::*;
use adw::Application;
use gtk4 as gtk;
use gtk::gio;

mod models;
mod services;
mod ui;
mod utils;

use std::sync::{Arc, RwLock};
use std::collections::HashMap;
use crate::models::persistence::{PinnedGroup, PinnedResource};

#[tokio::main]
async fn main() {
    // Initialize standard adw::Application
    let app = Application::builder()
        .application_id("com.lfmundim.azpin")
        // Use HANDLES_COMMAND_LINE or simple flags so the main window doesn't open immediately
        .flags(gio::ApplicationFlags::HANDLES_COMMAND_LINE)
        .build();

    // Setup action for command line
    app.connect_command_line(move |app, _cli| {
        // Initialize services
        let db = std::sync::Arc::new(crate::services::db::Db::new().expect("Failed to init DB"));
        let token_cache = std::sync::Arc::new(crate::services::token_cache::TokenCache::new(db.clone()));
        let arm_service = std::sync::Arc::new(crate::services::arm::ArmService::new(token_cache));
        
        let (open_tx, open_rx) = gtk::glib::MainContext::channel(gtk::glib::Priority::DEFAULT);
        let app_clone = app.clone();
        open_rx.attach(None, move |_| {
            if let Some(win) = app_clone.active_window() {
                win.present();
            } else if let Some(win) = app_clone.windows().first() {
                win.present();
            }
            gtk::glib::ControlFlow::Continue
        });

        let (settings_tx, settings_rx) = gtk::glib::MainContext::channel(gtk::glib::Priority::DEFAULT);
        let app_clone2 = app.clone();
        let db_clone = db.clone();
        settings_rx.attach(None, move |_| {
            let settings = crate::ui::settings::SettingsWindow::new(&app_clone2, db_clone.clone());
            settings.present();
            gtk::glib::ControlFlow::Continue
        });

        let (pin_changed_tx, pin_changed_rx) = gtk::glib::MainContext::channel(gtk::glib::Priority::DEFAULT);

        // Initialize Tray (without GTK3 linkage)
        let tokio_handle = tokio::runtime::Handle::current();
        let state_cache: Arc<RwLock<HashMap<String, String>>> = Arc::new(RwLock::new(HashMap::new()));
        let rg_resources_cache: Arc<RwLock<HashMap<String, Vec<PinnedResource>>>> = Arc::new(RwLock::new(HashMap::new()));
        
        let tray = crate::ui::indicator::AzPinTray { 
            db: db.clone(), 
            arm_service: arm_service.clone(), 
            open_tx, 
            settings_tx, 
            pin_changed_tx: pin_changed_tx.clone(),
            tokio_handle: tokio_handle.clone(),
            state_cache: state_cache.clone(),
            rg_resources_cache: rg_resources_cache.clone(),
        };

        let tray_service = ksni::TrayService::new(tray);
        let tray_handle = tray_service.handle();
        tray_service.spawn();

        let state_tray_handle = tray_handle.clone();

        // Spawn a background task to periodically update the state of runnable pinned resources
        let state_db = db.clone();
        let state_arm = arm_service.clone();
        let state_cache_clone = state_cache.clone();
        let state_rg_cache = rg_resources_cache.clone();
        tokio_handle.spawn(async move {
            loop {
                let mut runnables = Vec::new();
                let mut updated = false;
                
                // Get from groups
                if let Ok(groups) = state_db.get_pinned_groups() {
                    for g in groups {
                        if let Ok(arm_resources) = state_arm.fetch_resources(&g.subscription_id, &g.name).await {
                            let mut p_res = Vec::new();
                            for r in arm_resources {
                                let p = PinnedResource {
                                    id: r.id.clone(),
                                    name: r.name,
                                    type_: r.type_,
                                    resource_group: g.name.clone(),
                                    subscription_id: g.subscription_id.clone(),
                                    location: r.location,
                                    display_order: 0,
                                };
                                p_res.push(p.clone());
                                
                                if p.type_.eq_ignore_ascii_case("Microsoft.Web/sites") || 
                                   p.type_.eq_ignore_ascii_case("Microsoft.App/containerApps") || 
                                   p.type_.eq_ignore_ascii_case("Microsoft.Compute/virtualMachines") ||
                                   p.type_.eq_ignore_ascii_case("Microsoft.Logic/workflows") {
                                    runnables.push(p);
                                }
                            }
                            if let Ok(mut cache) = state_rg_cache.write() {
                                cache.insert(g.id.clone(), p_res);
                                updated = true;
                            }
                        }
                    }
                }

                // Get from orphans
                if let Ok(orphans) = state_db.get_orphan_resources() {
                    for r in orphans {
                        if r.type_.eq_ignore_ascii_case("Microsoft.Web/sites") || 
                           r.type_.eq_ignore_ascii_case("Microsoft.App/containerApps") || 
                           r.type_.eq_ignore_ascii_case("Microsoft.Compute/virtualMachines") ||
                           r.type_.eq_ignore_ascii_case("Microsoft.Logic/workflows") {
                            runnables.push(r);
                        }
                    }
                }

                let mut updated = false;

                // Fetch states
                for r in runnables {
                    if let Ok(state) = state_arm.get_resource_state(&r.subscription_id, &r.id).await {
                        if let Ok(mut cache) = state_cache_clone.write() {
                            cache.insert(r.id.clone(), state);
                            updated = true;
                        }
                    }
                }

                if updated {
                    state_tray_handle.update(|_| {});
                }

                tokio::time::sleep(tokio::time::Duration::from_secs(30)).await;
            }
        });

        // Present main window for testing as well
        let window = crate::ui::main_window::MainWindow::new(app, db, arm_service, tray_handle, pin_changed_rx);
        window.present();
        
        // This is a minimal hook to prevent immediate exit
        app.hold();
        0
    });

    // Run the application
    app.run();
}

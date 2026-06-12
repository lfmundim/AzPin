use adw::prelude::*;
use adw::Application;
use gtk4 as gtk;
use gtk::gio;

mod models;
mod services;
mod ui;
mod utils;

use std::collections::HashMap;
use std::sync::{Arc, RwLock};

use crate::models::persistence::PinnedResource;
use crate::services::az_cli::AzCliService;
use crate::services::permissions::PermissionsService;
use crate::utils::resource_type::{is_runnable, ResourceState};

#[tokio::main]
async fn main() {
    let app = Application::builder()
        .application_id("com.kimdim.azpin")
        .flags(gio::ApplicationFlags::HANDLES_COMMAND_LINE)
        .build();

    app.connect_command_line(move |app, _cli| {
        let db = Arc::new(
            crate::services::db::Db::new().expect("Failed to init DB"),
        );
        let token_cache = Arc::new(crate::services::token_cache::TokenCache::new(db.clone()));
        let arm_service = Arc::new(crate::services::arm::ArmService::new(token_cache.clone()));
        let permissions_service = Arc::new(PermissionsService::new(token_cache));

        // --- Channels ---
        let (open_tx, open_rx) = gtk::glib::MainContext::channel(gtk::glib::Priority::DEFAULT);
        let app_open = app.clone();
        open_rx.attach(None, move |_| {
            if let Some(win) = app_open.active_window() {
                win.present();
            } else if let Some(win) = app_open.windows().first() {
                win.present();
            }
            gtk::glib::ControlFlow::Continue
        });

        let (settings_tx, settings_rx) = gtk::glib::MainContext::channel(gtk::glib::Priority::DEFAULT);
        let app_settings = app.clone();
        let db_settings = db.clone();
        settings_rx.attach(None, move |_| {
            let settings =
                crate::ui::settings::SettingsWindow::new(&app_settings, db_settings.clone());
            settings.present();
            gtk::glib::ControlFlow::Continue
        });

        // Issue 7: proper quit via GTK lifecycle — no std::process::exit
        let (quit_tx, quit_rx) = gtk::glib::MainContext::channel(gtk::glib::Priority::DEFAULT);
        let app_quit = app.clone();
        quit_rx.attach(None, move |_| {
            app_quit.quit();
            gtk::glib::ControlFlow::Continue
        });

        let (pin_changed_tx, pin_changed_rx) =
            gtk::glib::MainContext::channel(gtk::glib::Priority::DEFAULT);

        let tokio_handle = tokio::runtime::Handle::current();

        let state_cache: Arc<RwLock<HashMap<String, ResourceState>>> =
            Arc::new(RwLock::new(HashMap::new()));
        let permissions_cache: Arc<RwLock<HashMap<String, bool>>> =
            Arc::new(RwLock::new(HashMap::new()));
        let rg_resources_cache: Arc<RwLock<HashMap<String, Vec<PinnedResource>>>> =
            Arc::new(RwLock::new(HashMap::new()));
        let account_cache: Arc<RwLock<Option<crate::services::az_cli::AzSubscription>>> =
            Arc::new(RwLock::new(None));

        let tray = crate::ui::indicator::AzPinTray {
            db: db.clone(),
            arm_service: arm_service.clone(),
            permissions_service: permissions_service.clone(),
            open_tx,
            settings_tx,
            quit_tx,
            pin_changed_tx: pin_changed_tx.clone(),
            tokio_handle: tokio_handle.clone(),
            state_cache: state_cache.clone(),
            permissions_cache: permissions_cache.clone(),
            rg_resources_cache: rg_resources_cache.clone(),
            account_cache: account_cache.clone(),
        };

        let tray_service = ksni::TrayService::new(tray);
        let tray_handle = tray_service.handle();
        tray_service.spawn();

        let state_tray_handle = tray_handle.clone();

        // Background polling loop
        let poll_db = db.clone();
        let poll_arm = arm_service.clone();
        let poll_permissions = permissions_service.clone();
        let poll_state_cache = state_cache.clone();
        let poll_permissions_cache = permissions_cache.clone();
        let poll_rg_cache = rg_resources_cache.clone();
        let poll_account_cache = account_cache.clone();

        tokio_handle.spawn(async move {
            loop {
                // Issue 6: single updated flag for the entire loop body
                let mut updated = false;

                // Issue 5: fetch account info once per poll cycle
                if let Ok(sub) = AzCliService::get_default_subscription().await {
                    if let Ok(mut cache) = poll_account_cache.write() {
                        *cache = Some(sub);
                        updated = true;
                    }
                }

                let mut runnables: Vec<PinnedResource> = Vec::new();

                // Populate rg_resources_cache from pinned groups
                if let Ok(groups) = poll_db.get_pinned_groups() {
                    for g in groups {
                        if let Ok(arm_resources) =
                            poll_arm.fetch_resources(&g.subscription_id, &g.name).await
                        {
                            let mut p_res = Vec::new();
                            for r in arm_resources {
                                let p = PinnedResource {
                                    id: r.id.clone(),
                                    name: r.name,
                                    type_: r.type_.clone(),
                                    resource_group: g.name.clone(),
                                    subscription_id: g.subscription_id.clone(),
                                    location: r.location,
                                    display_order: 0,
                                };
                                if is_runnable(&r.type_) {
                                    runnables.push(p.clone());
                                }
                                p_res.push(p);
                            }
                            if let Ok(mut cache) = poll_rg_cache.write() {
                                cache.insert(g.id.clone(), p_res);
                                updated = true;
                            }
                        }
                    }
                }

                // Collect orphan runnables
                if let Ok(orphans) = poll_db.get_orphan_resources() {
                    for r in orphans {
                        if is_runnable(&r.type_) {
                            runnables.push(r);
                        }
                    }
                }

                // Fetch state + permissions for each runnable
                for r in &runnables {
                    if let Ok(state) = poll_arm.get_resource_state(&r.subscription_id, &r.id).await {
                        if let Ok(mut cache) = poll_state_cache.write() {
                            cache.insert(r.id.clone(), state);
                            updated = true;
                        }
                    }

                    let can_manage = poll_permissions.can_manage(&r.subscription_id, &r.id).await;
                    if let Ok(mut cache) = poll_permissions_cache.write() {
                        if cache.get(&r.id) != Some(&can_manage) {
                            cache.insert(r.id.clone(), can_manage);
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

        let window = crate::ui::main_window::MainWindow::new(
            app,
            db,
            arm_service,
            tray_handle,
            pin_changed_rx,
        );
        window.present();

        app.hold();
        0
    });

    app.run();
}

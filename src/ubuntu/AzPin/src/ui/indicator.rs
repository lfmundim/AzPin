use std::sync::Arc;
use ksni::{Tray, MenuItem, menu};
use gtk4 as gtk;
use crate::services::db::Db;
use crate::services::arm::ArmService;

pub struct AzPinTray {
    pub db: Arc<Db>,
    pub arm_service: Arc<ArmService>,
    pub open_tx: gtk::glib::Sender<()>,
    pub settings_tx: gtk::glib::Sender<()>,
    pub pin_changed_tx: gtk::glib::Sender<()>,
    pub tokio_handle: tokio::runtime::Handle,
    pub state_cache: Arc<std::sync::RwLock<std::collections::HashMap<String, String>>>,
}

impl Tray for AzPinTray {
    fn icon_name(&self) -> String {
        "weather-overcast".into()
    }
    
    fn title(&self) -> String {
        "AzPin".into()
    }
    
    fn id(&self) -> String {
        "com.lfmundim.azpin".into()
    }

    fn menu(&self) -> Vec<MenuItem<Self>> {
        let mut items = Vec::new();

        // 1. Account info
        match crate::services::az_cli::AzCliService::get_default_subscription() {
            Ok(sub) => {
                items.push(menu::StandardItem {
                    label: format!("✅ {}", sub.name),
                    enabled: false,
                    ..Default::default()
                }.into());
            },
            Err(_) => {
                items.push(menu::StandardItem {
                    label: "⚠️ Not signed in".into(),
                    enabled: false,
                    ..Default::default()
                }.into());
            }
        }

        items.push(menu::MenuItem::Separator);

        // 2. Pinned Groups
        if let Ok(groups) = self.db.get_pinned_groups() {
            let arm_svc = self.arm_service.clone();
            let db_ref = self.db.clone();
            
            for group in groups {
                let mut group_submenu = Vec::new();
                
                // Fetch resources for this group
                if let Ok(resources) = db_ref.get_pinned_resources(&group.id) {
                    for res in resources {
                        let res_id_portal = res.id.clone();
                        
                        let mut submenu = vec![
                            menu::StandardItem {
                                label: "Open in Portal".into(),
                                activate: Box::new(move |_| {
                                    let uri = format!("https://portal.azure.com/#resource{}", res_id_portal);
                                    let _ = gtk::gio::AppInfo::launch_default_for_uri(&uri, None::<&gtk::gio::AppLaunchContext>);
                                }),
                                ..Default::default()
                            }.into(),
                        ];

                        let is_runnable = res.type_.eq_ignore_ascii_case("Microsoft.Web/sites") || 
                                          res.type_.eq_ignore_ascii_case("Microsoft.App/containerApps") || 
                                          res.type_.eq_ignore_ascii_case("Microsoft.Compute/virtualMachines");

                        if is_runnable {
                            let state = {
                                if let Ok(cache) = self.state_cache.read() {
                                    cache.get(&res.id).cloned().unwrap_or_else(|| "Unknown".to_string())
                                } else {
                                    "Unknown".to_string()
                                }
                            };

                            let is_running = state.eq_ignore_ascii_case("Running") || state.eq_ignore_ascii_case("Succeeded");
                            let is_stopped = state.eq_ignore_ascii_case("Stopped") || state.eq_ignore_ascii_case("Deallocated") || state.eq_ignore_ascii_case("Stopped (Deallocated)");
                            
                            let is_transitioning = state.eq_ignore_ascii_case("Starting") || state.eq_ignore_ascii_case("Stopping") || state.eq_ignore_ascii_case("Restarting");

                            submenu.push(menu::MenuItem::Separator);
                            
                            if !is_running && !is_transitioning {
                                let r_id_start = res.id.clone();
                                let sub_start = res.subscription_id.clone();
                                let a_svc_start = arm_svc.clone();
                                let tokio_handle = self.tokio_handle.clone();
                                let cache_clone = self.state_cache.clone();
                                submenu.push(menu::StandardItem {
                                    label: "Start".into(),
                                    activate: Box::new(move |_| {
                                        if let Ok(mut c) = cache_clone.write() { c.insert(r_id_start.clone(), "Starting".to_string()); }
                                        let a_svc = a_svc_start.clone();
                                        let sid = sub_start.clone();
                                        let rid = r_id_start.clone();
                                        tokio_handle.spawn(async move {
                                            let _ = a_svc.start_resource(&sid, &rid, "2021-04-01").await;
                                        });
                                    }),
                                    ..Default::default()
                                }.into());
                            }

                            if is_running && !is_transitioning {
                                let r_id_stop = res.id.clone();
                                let sub_stop = res.subscription_id.clone();
                                let a_svc_stop = arm_svc.clone();
                                let tokio_handle = self.tokio_handle.clone();
                                let cache_clone = self.state_cache.clone();
                                submenu.push(menu::StandardItem {
                                    label: "Stop".into(),
                                    activate: Box::new(move |_| {
                                        if let Ok(mut c) = cache_clone.write() { c.insert(r_id_stop.clone(), "Stopping".to_string()); }
                                        let a_svc = a_svc_stop.clone();
                                        let sid = sub_stop.clone();
                                        let rid = r_id_stop.clone();
                                        tokio_handle.spawn(async move {
                                            let _ = a_svc.stop_resource(&sid, &rid, "2021-04-01").await;
                                        });
                                    }),
                                    ..Default::default()
                                }.into());

                                let r_id_restart = res.id.clone();
                                let sub_restart = res.subscription_id.clone();
                                let a_svc_restart = arm_svc.clone();
                                let tokio_handle = self.tokio_handle.clone();
                                let cache_clone = self.state_cache.clone();
                                submenu.push(menu::StandardItem {
                                    label: "Restart".into(),
                                    activate: Box::new(move |_| {
                                        if let Ok(mut c) = cache_clone.write() { c.insert(r_id_restart.clone(), "Restarting".to_string()); }
                                        let a_svc = a_svc_restart.clone();
                                        let sid = sub_restart.clone();
                                        let rid = r_id_restart.clone();
                                        tokio_handle.spawn(async move {
                                            let _ = a_svc.restart_resource(&sid, &rid, "2021-04-01").await;
                                        });
                                    }),
                                    ..Default::default()
                                }.into());
                            }
                        }

                        // We can either add it as a submenu (if runnable) or standard item
                        if is_runnable {
                            let state = {
                                if let Ok(cache) = self.state_cache.read() {
                                    cache.get(&res.id).cloned().unwrap_or_else(|| "Unknown".to_string())
                                } else {
                                    "Unknown".to_string()
                                }
                            };
                            let status_indicator = if state.eq_ignore_ascii_case("Running") || state.eq_ignore_ascii_case("Succeeded") {
                                "🟢"
                            } else if state.eq_ignore_ascii_case("Stopped") || state.eq_ignore_ascii_case("Deallocated") || state.eq_ignore_ascii_case("Stopped (Deallocated)") {
                                "🔴"
                            } else {
                                "⚪"
                            };

                            group_submenu.push(menu::SubMenu {
                                label: format!("{} {}", status_indicator, res.name.clone()),
                                submenu,
                                ..Default::default()
                            }.into());
                        } else {
                            // If it doesn't have actions, just make it clickable to open portal directly
                            let r_id = res.id.clone();
                            group_submenu.push(menu::StandardItem {
                                label: res.name.clone(),
                                activate: Box::new(move |_| {
                                    let uri = format!("https://portal.azure.com/#resource{}", r_id);
                                    let _ = gtk::gio::AppInfo::launch_default_for_uri(&uri, None::<&gtk::gio::AppLaunchContext>);
                                }),
                                ..Default::default()
                            }.into());
                        }
                    }
                }
                
                // Add bottom options for the group
                if !group_submenu.is_empty() {
                    group_submenu.push(menu::MenuItem::Separator);
                }
                
                let g_id_portal = group.id.clone();
                group_submenu.push(menu::StandardItem {
                    label: "Open Resource Group in Portal".into(),
                    activate: Box::new(move |_| {
                        let uri = format!("https://portal.azure.com/#resource{}", g_id_portal);
                        let _ = gtk::gio::AppInfo::launch_default_for_uri(&uri, None::<&gtk::gio::AppLaunchContext>);
                    }),
                    ..Default::default()
                }.into());
                
                let g_id_unpin = group.id.clone();
                let db_unpin = self.db.clone();
                group_submenu.push(menu::StandardItem {
                    label: "Unpin".into(),
                    activate: Box::new(move |tray: &mut AzPinTray| {
                        let _ = db_unpin.delete_pinned_group(&g_id_unpin);
                        let _ = tray.pin_changed_tx.send(());
                    }),
                    ..Default::default()
                }.into());
                
                items.push(menu::SubMenu {
                    label: group.name,
                    submenu: group_submenu,
                    ..Default::default()
                }.into());
            }
        }

        // 3. Pinned Individual Resources (that are NOT part of a pinned group)
        if let Ok(orphans) = self.db.get_orphan_resources() {
            if !orphans.is_empty() {
                items.push(menu::MenuItem::Separator);
                
                for res in orphans {
                    let is_runnable = res.type_.eq_ignore_ascii_case("Microsoft.Web/sites") || 
                                      res.type_.eq_ignore_ascii_case("Microsoft.App/containerApps") || 
                                      res.type_.eq_ignore_ascii_case("Microsoft.Compute/virtualMachines");

                    if is_runnable {
                        let mut submenu = vec![
                            menu::StandardItem {
                                label: "Open in Portal".into(),
                                activate: Box::new({
                                    let r_id = res.id.clone();
                                    move |_| {
                                        let uri = format!("https://portal.azure.com/#resource{}", r_id);
                                        let _ = gtk::gio::AppInfo::launch_default_for_uri(&uri, None::<&gtk::gio::AppLaunchContext>);
                                    }
                                }),
                                ..Default::default()
                            }.into(),
                            menu::MenuItem::Separator,
                        ];
                        
                        let state = {
                            if let Ok(cache) = self.state_cache.read() {
                                cache.get(&res.id).cloned().unwrap_or_else(|| "Unknown".to_string())
                            } else {
                                "Unknown".to_string()
                            }
                        };

                        let is_running = state.eq_ignore_ascii_case("Running") || state.eq_ignore_ascii_case("Succeeded");
                        let is_stopped = state.eq_ignore_ascii_case("Stopped") || state.eq_ignore_ascii_case("Deallocated") || state.eq_ignore_ascii_case("Stopped (Deallocated)");
                        let is_transitioning = state.eq_ignore_ascii_case("Starting") || state.eq_ignore_ascii_case("Stopping") || state.eq_ignore_ascii_case("Restarting");

                        if !is_running && !is_transitioning {
                            let r_id_start = res.id.clone();
                            let sub_start = res.subscription_id.clone();
                            let a_svc_start = self.arm_service.clone();
                            let tokio_handle = self.tokio_handle.clone();
                            let cache_clone = self.state_cache.clone();
                            submenu.push(menu::StandardItem {
                                label: "Start".into(),
                                activate: Box::new(move |_| {
                                    if let Ok(mut c) = cache_clone.write() { c.insert(r_id_start.clone(), "Starting".to_string()); }
                                    let a_svc = a_svc_start.clone();
                                    let sid = sub_start.clone();
                                    let rid = r_id_start.clone();
                                    tokio_handle.spawn(async move {
                                        let _ = a_svc.start_resource(&sid, &rid, "2021-04-01").await;
                                    });
                                }),
                                ..Default::default()
                            }.into());
                        }

                        if is_running && !is_transitioning {
                            let r_id_stop = res.id.clone();
                            let sub_stop = res.subscription_id.clone();
                            let a_svc_stop = self.arm_service.clone();
                            let tokio_handle = self.tokio_handle.clone();
                            let cache_clone = self.state_cache.clone();
                            submenu.push(menu::StandardItem {
                                label: "Stop".into(),
                                activate: Box::new(move |_| {
                                    if let Ok(mut c) = cache_clone.write() { c.insert(r_id_stop.clone(), "Stopping".to_string()); }
                                    let a_svc = a_svc_stop.clone();
                                    let sid = sub_stop.clone();
                                    let rid = r_id_stop.clone();
                                    tokio_handle.spawn(async move {
                                        let _ = a_svc.stop_resource(&sid, &rid, "2021-04-01").await;
                                    });
                                }),
                                ..Default::default()
                            }.into());

                            let r_id_restart = res.id.clone();
                            let sub_restart = res.subscription_id.clone();
                            let a_svc_restart = self.arm_service.clone();
                            let tokio_handle = self.tokio_handle.clone();
                            let cache_clone = self.state_cache.clone();
                            submenu.push(menu::StandardItem {
                                label: "Restart".into(),
                                activate: Box::new(move |_| {
                                    if let Ok(mut c) = cache_clone.write() { c.insert(r_id_restart.clone(), "Restarting".to_string()); }
                                    let a_svc = a_svc_restart.clone();
                                    let sid = sub_restart.clone();
                                    let rid = r_id_restart.clone();
                                    tokio_handle.spawn(async move {
                                        let _ = a_svc.restart_resource(&sid, &rid, "2021-04-01").await;
                                    });
                                }),
                                ..Default::default()
                            }.into());
                        }

                        submenu.push(menu::MenuItem::Separator);
                        
                        let r_id_unpin = res.id.clone();
                        let db_unpin = self.db.clone();
                        submenu.push(menu::StandardItem {
                            label: "Unpin".into(),
                            activate: Box::new(move |tray: &mut AzPinTray| {
                                let _ = db_unpin.delete_pinned_resource(&r_id_unpin);
                                let _ = tray.pin_changed_tx.send(());
                            }),
                            ..Default::default()
                        }.into());

                        let status_indicator = if state.eq_ignore_ascii_case("Running") || state.eq_ignore_ascii_case("Succeeded") {
                            "🟢"
                        } else if state.eq_ignore_ascii_case("Stopped") || state.eq_ignore_ascii_case("Deallocated") || state.eq_ignore_ascii_case("Stopped (Deallocated)") {
                            "🔴"
                        } else {
                            "⚪"
                        };

                        items.push(menu::SubMenu {
                            label: format!("{} {}", status_indicator, res.name.clone()),
                            submenu,
                            ..Default::default()
                        }.into());
                    } else {
                        let r_id = res.id.clone();
                        items.push(menu::StandardItem {
                            label: res.name.clone(),
                            activate: Box::new(move |_| {
                                let uri = format!("https://portal.azure.com/#resource{}", r_id);
                                let _ = gtk::gio::AppInfo::launch_default_for_uri(&uri, None::<&gtk::gio::AppLaunchContext>);
                            }),
                            ..Default::default()
                        }.into());
                    }
                }
            }
        }

        items.push(menu::MenuItem::Separator);

        let tx = self.open_tx.clone();
        items.push(menu::StandardItem {
            label: "Open AzPin...".into(),
            activate: Box::new(move |_| {
                let _ = tx.send(());
            }),
            ..Default::default()
        }.into());

        let settings_tx = self.settings_tx.clone();
        items.push(menu::StandardItem {
            label: "Settings...".into(),
            activate: Box::new(move |_| {
                let _ = settings_tx.send(());
            }),
            ..Default::default()
        }.into());

        items.push(menu::StandardItem {
            label: "Quit AzPin".into(),
            activate: Box::new(|_| {
                std::process::exit(0);
            }),
            ..Default::default()
        }.into());

        items
    }
}

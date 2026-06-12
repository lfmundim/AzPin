use std::collections::HashMap;
use std::sync::{Arc, RwLock};

use gtk4 as gtk;
use ksni::{menu, MenuItem, Tray};

use crate::models::persistence::PinnedResource;
use crate::services::arm::ArmService;
use crate::services::az_cli::AzSubscription;
use crate::services::db::Db;
use crate::services::permissions::PermissionsService;
use crate::utils::portal_url;
use crate::utils::resource_type::{is_runnable, ResourceState};

pub struct AzPinTray {
    pub db: Arc<Db>,
    pub arm_service: Arc<ArmService>,
    pub permissions_service: Arc<PermissionsService>,
    pub open_tx: gtk::glib::Sender<()>,
    pub settings_tx: gtk::glib::Sender<()>,
    pub quit_tx: gtk::glib::Sender<()>,
    pub pin_changed_tx: gtk::glib::Sender<()>,
    pub tokio_handle: tokio::runtime::Handle,
    pub state_cache: Arc<RwLock<HashMap<String, ResourceState>>>,
    pub permissions_cache: Arc<RwLock<HashMap<String, bool>>>,
    pub rg_resources_cache: Arc<RwLock<HashMap<String, Vec<PinnedResource>>>>,
    pub account_cache: Arc<RwLock<Option<AzSubscription>>>,
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

        // 1. Account info — read from cache, never call AzCliService here
        let account_label = if let Ok(cache) = self.account_cache.read() {
            match cache.as_ref() {
                Some(sub) => format!("● {}", sub.name),
                None => "Not signed in".to_string(),
            }
        } else {
            "Not signed in".to_string()
        };

        items.push(
            menu::StandardItem {
                label: account_label,
                enabled: false,
                ..Default::default()
            }
            .into(),
        );

        items.push(menu::MenuItem::Separator);

        // 2. Pinned Groups
        if let Ok(groups) = self.db.get_pinned_groups() {
            let arm_svc = self.arm_service.clone();

            for group in groups {
                let mut group_submenu = Vec::new();

                let resources = if let Ok(cache) = self.rg_resources_cache.read() {
                    cache.get(&group.id).cloned().unwrap_or_default()
                } else {
                    Vec::new()
                };

                if !resources.is_empty() {
                    for res in resources {
                        let runnable = is_runnable(&res.type_);

                        let state = if runnable {
                            if let Ok(cache) = self.state_cache.read() {
                                cache.get(&res.id).cloned().unwrap_or(ResourceState::Unknown)
                            } else {
                                ResourceState::Unknown
                            }
                        } else {
                            ResourceState::Unknown
                        };

                        let can_manage = if runnable {
                            if let Ok(cache) = self.permissions_cache.read() {
                                cache.get(&res.id).copied()
                            } else {
                                None
                            }
                        } else {
                            None
                        };

                        let res_id_portal = res.id.clone();
                        let mut submenu: Vec<MenuItem<Self>> = vec![menu::StandardItem {
                            label: "Open in Portal".into(),
                            activate: Box::new(move |_| {
                                let uri = portal_url::resource_url(&res_id_portal);
                                let _ = gtk::gio::AppInfo::launch_default_for_uri(
                                    &uri,
                                    None::<&gtk::gio::AppLaunchContext>,
                                );
                            }),
                            ..Default::default()
                        }
                        .into()];

                        // Only show action buttons when: runnable + permissions confirmed
                        if runnable && can_manage == Some(true) {
                            submenu.push(menu::MenuItem::Separator);
                            build_action_items(
                                &mut submenu,
                                &res,
                                &state,
                                &arm_svc,
                                &self.tokio_handle,
                                &self.state_cache,
                            );
                        } else if runnable && can_manage.is_none() {
                            // Permissions not yet loaded — fail safe, show nothing
                        } else if runnable {
                            // can_manage == Some(false) — no buttons
                        }

                        let label = res.name.clone();

                        if runnable {
                            group_submenu.push(
                                menu::SubMenu {
                                    label,
                                    submenu,
                                    ..Default::default()
                                }
                                .into(),
                            );
                        } else {
                            let r_id = res.id.clone();
                            group_submenu.push(
                                menu::StandardItem {
                                    label: res.name.clone(),
                                    activate: Box::new(move |_| {
                                        let uri = portal_url::resource_url(&r_id);
                                        let _ = gtk::gio::AppInfo::launch_default_for_uri(
                                            &uri,
                                            None::<&gtk::gio::AppLaunchContext>,
                                        );
                                    }),
                                    ..Default::default()
                                }
                                .into(),
                            );
                        }
                    }
                } else {
                    group_submenu.push(
                        menu::StandardItem {
                            label: "Loading resources...".into(),
                            enabled: false,
                            ..Default::default()
                        }
                        .into(),
                    );
                }

                if !group_submenu.is_empty() {
                    group_submenu.push(menu::MenuItem::Separator);
                }

                let g_sub_id = group.subscription_id.clone();
                let g_name = group.name.clone();
                group_submenu.push(
                    menu::StandardItem {
                        label: "Open Resource Group in Portal".into(),
                        activate: Box::new(move |_| {
                            let uri = portal_url::resource_group_url(&g_sub_id, &g_name);
                            let _ = gtk::gio::AppInfo::launch_default_for_uri(
                                &uri,
                                None::<&gtk::gio::AppLaunchContext>,
                            );
                        }),
                        ..Default::default()
                    }
                    .into(),
                );

                let g_id_unpin = group.id.clone();
                let db_unpin = self.db.clone();
                group_submenu.push(
                    menu::StandardItem {
                        label: "Unpin".into(),
                        activate: Box::new(move |tray: &mut AzPinTray| {
                            let _ = db_unpin.delete_pinned_group(&g_id_unpin);
                            let _ = tray.pin_changed_tx.send(());
                        }),
                        ..Default::default()
                    }
                    .into(),
                );

                items.push(
                    menu::SubMenu {
                        label: group.name,
                        submenu: group_submenu,
                        ..Default::default()
                    }
                    .into(),
                );
            }
        }

        // 3. Pinned Individual Resources (orphans not part of a pinned group)
        if let Ok(orphans) = self.db.get_orphan_resources() {
            if !orphans.is_empty() {
                items.push(menu::MenuItem::Separator);

                for res in orphans {
                    let runnable = is_runnable(&res.type_);

                    if runnable {
                        let state = if let Ok(cache) = self.state_cache.read() {
                            cache.get(&res.id).cloned().unwrap_or(ResourceState::Unknown)
                        } else {
                            ResourceState::Unknown
                        };

                        let can_manage = if let Ok(cache) = self.permissions_cache.read() {
                            cache.get(&res.id).copied()
                        } else {
                            None
                        };

                        let res_id_portal = res.id.clone();
                        let mut submenu: Vec<MenuItem<Self>> = vec![
                            menu::StandardItem {
                                label: "Open in Portal".into(),
                                activate: Box::new(move |_| {
                                    let uri = portal_url::resource_url(&res_id_portal);
                                    let _ = gtk::gio::AppInfo::launch_default_for_uri(
                                        &uri,
                                        None::<&gtk::gio::AppLaunchContext>,
                                    );
                                }),
                                ..Default::default()
                            }
                            .into(),
                        ];

                        if can_manage == Some(true) {
                            submenu.push(menu::MenuItem::Separator);
                            build_action_items(
                                &mut submenu,
                                &res,
                                &state,
                                &self.arm_service,
                                &self.tokio_handle,
                                &self.state_cache,
                            );
                        }

                        submenu.push(menu::MenuItem::Separator);

                        let r_id_unpin = res.id.clone();
                        let db_unpin = self.db.clone();
                        submenu.push(
                            menu::StandardItem {
                                label: "Unpin".into(),
                                activate: Box::new(move |tray: &mut AzPinTray| {
                                    let _ = db_unpin.delete_pinned_resource(&r_id_unpin);
                                    let _ = tray.pin_changed_tx.send(());
                                }),
                                ..Default::default()
                            }
                            .into(),
                        );

                        items.push(
                            menu::SubMenu {
                                label: res.name.clone(),
                                submenu,
                                ..Default::default()
                            }
                            .into(),
                        );
                    } else {
                        let r_id = res.id.clone();
                        items.push(
                            menu::StandardItem {
                                label: res.name.clone(),
                                activate: Box::new(move |_| {
                                    let uri = portal_url::resource_url(&r_id);
                                    let _ = gtk::gio::AppInfo::launch_default_for_uri(
                                        &uri,
                                        None::<&gtk::gio::AppLaunchContext>,
                                    );
                                }),
                                ..Default::default()
                            }
                            .into(),
                        );
                    }
                }
            }
        }

        items.push(menu::MenuItem::Separator);

        let tx = self.open_tx.clone();
        items.push(
            menu::StandardItem {
                label: "Open AzPin...".into(),
                activate: Box::new(move |_| {
                    let _ = tx.send(());
                }),
                ..Default::default()
            }
            .into(),
        );

        let settings_tx = self.settings_tx.clone();
        items.push(
            menu::StandardItem {
                label: "Settings...".into(),
                activate: Box::new(move |_| {
                    let _ = settings_tx.send(());
                }),
                ..Default::default()
            }
            .into(),
        );

        let quit_tx = self.quit_tx.clone();
        items.push(
            menu::StandardItem {
                label: "Quit AzPin".into(),
                activate: Box::new(move |_| {
                    let _ = quit_tx.send(());
                }),
                ..Default::default()
            }
            .into(),
        );

        items
    }
}

fn build_action_items(
    submenu: &mut Vec<MenuItem<AzPinTray>>,
    res: &PinnedResource,
    state: &ResourceState,
    arm_svc: &Arc<ArmService>,
    tokio_handle: &tokio::runtime::Handle,
    state_cache: &Arc<RwLock<HashMap<String, ResourceState>>>,
) {
    if state.is_transitioning() {
        let label = format!("… {}", state.display_label());
        submenu.push(
            menu::StandardItem {
                label,
                enabled: false,
                ..Default::default()
            }
            .into(),
        );
        return;
    }

    if state.is_stopped() {
        let r_id = res.id.clone();
        let sub = res.subscription_id.clone();
        let svc = arm_svc.clone();
        let handle = tokio_handle.clone();
        let cache = state_cache.clone();
        submenu.push(
            menu::StandardItem {
                label: "▶ Start".into(),
                activate: Box::new(move |_| {
                    if let Ok(mut c) = cache.write() {
                        c.insert(r_id.clone(), ResourceState::Starting);
                    }
                    let s = svc.clone();
                    let sid = sub.clone();
                    let rid = r_id.clone();
                    handle.spawn(async move {
                        let _ = s.start_resource(&sid, &rid).await;
                    });
                }),
                ..Default::default()
            }
            .into(),
        );
    } else if state.is_running() {
        let r_id_stop = res.id.clone();
        let sub_stop = res.subscription_id.clone();
        let svc_stop = arm_svc.clone();
        let handle_stop = tokio_handle.clone();
        let cache_stop = state_cache.clone();
        submenu.push(
            menu::StandardItem {
                label: "■ Stop".into(),
                activate: Box::new(move |_| {
                    if let Ok(mut c) = cache_stop.write() {
                        c.insert(r_id_stop.clone(), ResourceState::Stopping);
                    }
                    let s = svc_stop.clone();
                    let sid = sub_stop.clone();
                    let rid = r_id_stop.clone();
                    handle_stop.spawn(async move {
                        let _ = s.stop_resource(&sid, &rid).await;
                    });
                }),
                ..Default::default()
            }
            .into(),
        );

        let r_id_restart = res.id.clone();
        let sub_restart = res.subscription_id.clone();
        let svc_restart = arm_svc.clone();
        let handle_restart = tokio_handle.clone();
        let cache_restart = state_cache.clone();
        submenu.push(
            menu::StandardItem {
                label: "⟳ Restart".into(),
                activate: Box::new(move |_| {
                    if let Ok(mut c) = cache_restart.write() {
                        c.insert(r_id_restart.clone(), ResourceState::Restarting);
                    }
                    let s = svc_restart.clone();
                    let sid = sub_restart.clone();
                    let rid = r_id_restart.clone();
                    handle_restart.spawn(async move {
                        let _ = s.restart_resource(&sid, &rid).await;
                    });
                }),
                ..Default::default()
            }
            .into(),
        );
    } else {
        submenu.push(
            menu::StandardItem {
                label: "Loading...".into(),
                enabled: false,
                ..Default::default()
            }
            .into(),
        );
    }
}

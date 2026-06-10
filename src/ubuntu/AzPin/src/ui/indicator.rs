use std::sync::Arc;
use ksni::{Tray, MenuItem, menu};
use gtk4 as gtk;
use crate::services::db::Db;
use crate::services::arm::ArmService;

pub struct AzPinTray {
    pub db: Arc<Db>,
    pub arm_service: Arc<ArmService>,
    pub open_tx: gtk::glib::Sender<()>,
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

        let tx = self.open_tx.clone();
        items.push(menu::StandardItem {
            label: "Open AzPin".into(),
            activate: Box::new(move |_| {
                let _ = tx.send(());
            }),
            ..Default::default()
        }.into());
        items.push(menu::MenuItem::Separator);

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

        // Add pinned groups
        if let Ok(groups) = self.db.get_pinned_groups() {
            let arm_svc = self.arm_service.clone();
            
            for group in groups {
                let mut group_submenu = Vec::new();
                for res in group.resources {
                    let res_id_portal = res.id.clone();
                    
                    let mut submenu = vec![
                        menu::StandardItem {
                            label: "Open in Portal".into(),
                            activate: Box::new(move |_| {
                                let uri = format!("https://portal.azure.com/#resource{}", res_id_portal);
                                let launcher = gtk::UriLauncher::new(&uri);
                                launcher.launch(None::<&gtk::Window>, gtk::gio::Cancellable::NONE, |_| {});
                            }),
                            ..Default::default()
                        }.into(),
                    ];

                    let is_runnable = res.type_.eq_ignore_ascii_case("Microsoft.Web/sites") || 
                                      res.type_.eq_ignore_ascii_case("Microsoft.App/containerApps") || 
                                      res.type_.eq_ignore_ascii_case("Microsoft.Compute/virtualMachines");

                    if is_runnable {
                        submenu.push(menu::MenuItem::Separator);
                        
                        let r_id_start = res.id.clone();
                        let sub_start = res.subscription_id.clone();
                        let a_svc_start = arm_svc.clone();
                        submenu.push(menu::StandardItem {
                            label: "Start".into(),
                            activate: Box::new(move |_| {
                                let a_svc = a_svc_start.clone();
                                let sid = sub_start.clone();
                                let rid = r_id_start.clone();
                                gtk::glib::spawn_future_local(async move {
                                    let _ = a_svc.start_resource(&sid, &rid, "2021-04-01").await;
                                });
                            }),
                            ..Default::default()
                        }.into());

                        let r_id_stop = res.id.clone();
                        let sub_stop = res.subscription_id.clone();
                        let a_svc_stop = arm_svc.clone();
                        submenu.push(menu::StandardItem {
                            label: "Stop".into(),
                            activate: Box::new(move |_| {
                                let a_svc = a_svc_stop.clone();
                                let sid = sub_stop.clone();
                                let rid = r_id_stop.clone();
                                gtk::glib::spawn_future_local(async move {
                                    let _ = a_svc.stop_resource(&sid, &rid, "2021-04-01").await;
                                });
                            }),
                            ..Default::default()
                        }.into());

                        let r_id_restart = res.id.clone();
                        let sub_restart = res.subscription_id.clone();
                        let a_svc_restart = arm_svc.clone();
                        submenu.push(menu::StandardItem {
                            label: "Restart".into(),
                            activate: Box::new(move |_| {
                                let a_svc = a_svc_restart.clone();
                                let sid = sub_restart.clone();
                                let rid = r_id_restart.clone();
                                gtk::glib::spawn_future_local(async move {
                                    let _ = a_svc.restart_resource(&sid, &rid, "2021-04-01").await;
                                });
                            }),
                            ..Default::default()
                        }.into());
                    }

                    group_submenu.push(menu::SubMenu {
                        label: res.name.clone(),
                        submenu,
                        ..Default::default()
                    }.into());
                }
                
                items.push(menu::SubMenu {
                    label: group.name,
                    submenu: group_submenu,
                    ..Default::default()
                }.into());
            }
        }

        items
    }
}

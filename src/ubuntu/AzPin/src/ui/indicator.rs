use std::sync::Arc;
use ksni::{Tray, MenuItem, menu};
use crate::services::db::Db;
use crate::services::arm::ArmService;

pub struct AzPinTray {
    pub db: Arc<Db>,
    pub arm_service: Arc<ArmService>,
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
        let mut items = vec![
            menu::StandardItem {
                label: "⚠️ Not signed in".into(),
                enabled: false,
                ..Default::default()
            }.into(),
            menu::MenuItem::Separator,
        ];

        // Add pinned groups
        if let Ok(groups) = self.db.get_pinned_groups() {
            for group in groups {
                let mut group_submenu = Vec::new();
                for res in group.resources {
                    let res_id = res.id.clone();
                    group_submenu.push(menu::SubMenu {
                        label: res.name.clone(),
                        submenu: vec![
                            menu::StandardItem {
                                label: "Open in Portal".into(),
                                activate: Box::new(move |_| {
                                    // Portal integration here
                                }),
                                ..Default::default()
                            }.into(),
                        ],
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

use gtk4::prelude::*;
use adw::prelude::*;
use gtk4 as gtk;
use std::sync::Arc;
use crate::services::db::Db;
use crate::services::az_cli::AzCliService;

pub struct SettingsWindow {
    window: adw::PreferencesWindow,
}

impl SettingsWindow {
    pub fn new(app: &adw::Application, db: Arc<Db>) -> Self {
        let window = adw::PreferencesWindow::builder()
            .application(app)
            .title("AzPin Settings")
            .build();

        // Account Page
        let account_page = adw::PreferencesPage::builder()
            .title("Account")
            .icon_name("avatar-default-symbolic")
            .build();

        let account_group = adw::PreferencesGroup::builder()
            .title("Identity")
            .build();

        let identity_row = adw::ActionRow::builder()
            .title("Current Tenant")
            .subtitle("Loading...")
            .build();

        let id_row_clone = identity_row.clone();
        gtk::glib::spawn_future_local(async move {
            if let Ok(sub) = AzCliService::get_default_subscription() {
                id_row_clone.set_subtitle(&sub.tenant_id);
            } else {
                id_row_clone.set_subtitle("Not signed in");
            }
        });

        account_group.add(&identity_row);
        account_page.add(&account_group);

        // Subscriptions Page
        let subs_page = adw::PreferencesPage::builder()
            .title("Subscriptions")
            .icon_name("view-list-symbolic")
            .build();

        let subs_group = adw::PreferencesGroup::builder()
            .title("Active Subscriptions")
            .build();

        let subs_group_clone = subs_group.clone();
        let db_clone = db.clone();
        gtk::glib::spawn_future_local(async move {
            let hidden_subs = db_clone.get_hidden_subscriptions().unwrap_or_default();
            
            if let Ok(subs) = AzCliService::list_subscriptions() {
                for sub in subs {
                    let is_hidden = hidden_subs.contains(&sub.id);
                    
                    let sub_toggle = gtk::Switch::new();
                    sub_toggle.set_active(!is_hidden);
                    
                    let sub_row = adw::ActionRow::builder()
                        .title(&sub.name)
                        .subtitle(&sub.id)
                        .build();
                    sub_row.add_suffix(&sub_toggle);
                    
                    let db_ref = db_clone.clone();
                    let sub_id = sub.id.clone();
                    sub_toggle.connect_active_notify(move |switch| {
                        if switch.is_active() {
                            let _ = db_ref.show_subscription(&sub_id);
                        } else {
                            let _ = db_ref.hide_subscription(&sub_id);
                        }
                    });
                    
                    subs_group_clone.add(&sub_row);
                }
            }
        });

        subs_page.add(&subs_group);

        window.add(&account_page);
        window.add(&subs_page);

        Self { window }
    }

    pub fn present(&self) {
        self.window.present();
    }
}

use gtk4::prelude::*;
use adw::prelude::*;
use gtk4 as gtk;
use gtk::prelude::*;
use std::sync::Arc;
use crate::services::db::Db;

pub struct SettingsWindow {
    window: adw::PreferencesWindow,
}

impl SettingsWindow {
    pub fn new(app: &adw::Application, _db: Arc<Db>) -> Self {
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

        // Display current tenant (dummy placeholder for UI setup)
        let identity_row = adw::ActionRow::builder()
            .title("Current Tenant")
            .subtitle("Not signed in")
            .build();

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

        // Dummy subscription toggle
        let sub_toggle = gtk::Switch::new();
        let sub_row = adw::ActionRow::builder()
            .title("Production Sub")
            .subtitle("sub-1234-5678")
            .build();
        sub_row.add_suffix(&sub_toggle);
        
        subs_group.add(&sub_row);
        subs_page.add(&subs_group);

        window.add(&account_page);
        window.add(&subs_page);

        Self { window }
    }

    pub fn present(&self) {
        self.window.present();
    }
}

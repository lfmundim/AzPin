use gtk4::prelude::*;
use adw::prelude::*;
use gtk4 as gtk;
use std::sync::Arc;
use crate::services::db::Db;
use crate::services::az_cli::AzCliService;
use crate::services::updater::{UpdaterService, UpdateCheckState};

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
            if let Ok(sub) = AzCliService::get_default_subscription().await {
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
            
            if let Ok(subs) = AzCliService::list_subscriptions().await {
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

        // Updates Page
        let updates_page = adw::PreferencesPage::builder()
            .title("Updates")
            .icon_name("software-update-available-symbolic")
            .build();

        let updates_group = adw::PreferencesGroup::builder()
            .title("Application Updates")
            .build();

        let current_version = env!("CARGO_PKG_VERSION");
        let version_row = adw::ActionRow::builder()
            .title("Current Version")
            .subtitle(current_version)
            .build();

        let check_btn = gtk::Button::builder()
            .label("Check for Updates")
            .margin_top(10)
            .margin_bottom(10)
            .css_classes(["suggested-action"])
            .build();

        let status_label = gtk::Label::builder()
            .label("")
            .wrap(true)
            .margin_top(10)
            .build();

        let download_btn = gtk::Button::builder()
            .label("Download Update")
            .margin_top(10)
            .margin_bottom(10)
            .visible(false)
            .css_classes(["suggested-action"])
            .build();

        let updates_vbox = gtk::Box::builder()
            .orientation(gtk::Orientation::Vertical)
            .spacing(6)
            .margin_top(12)
            .margin_bottom(12)
            .margin_start(12)
            .margin_end(12)
            .build();

        updates_vbox.append(&check_btn);
        updates_vbox.append(&status_label);
        updates_vbox.append(&download_btn);

        let updates_container_row = adw::ActionRow::new();
        updates_container_row.set_child(Some(&updates_vbox));

        updates_group.add(&version_row);
        updates_group.add(&updates_container_row);
        updates_page.add(&updates_group);
        window.add(&updates_page);

        let status_label_clone = status_label.clone();
        let download_btn_clone = download_btn.clone();
        let check_btn_clone = check_btn.clone();

        check_btn.connect_clicked(move |_| {
            let status = status_label_clone.clone();
            let download = download_btn_clone.clone();
            let btn = check_btn_clone.clone();

            btn.set_sensitive(false);
            status.set_label("Checking for updates...");
            download.set_visible(false);

            gtk::glib::spawn_future_local(async move {
                let updater = UpdaterService::new();
                match updater.check_for_updates().await {
                    UpdateCheckState::UpToDate { version } => {
                        status.set_label(&format!("You are up to date! (v{})", version));
                    }
                    UpdateCheckState::UpdateAvailable { latest, release_url, .. } => {
                        status.set_label(&format!("Update available! v{}", latest));
                        download.set_visible(true);
                        
                        let url = release_url.clone();
                        download.connect_clicked(move |_| {
                            let _ = gtk::gio::AppInfo::launch_default_for_uri(&url, None::<&gtk::gio::AppLaunchContext>);
                        });
                    }
                    UpdateCheckState::Failed(err) => {
                        status.set_label(&format!("Failed to check for updates: {}", err));
                    }
                    _ => {}
                }
                btn.set_sensitive(true);
            });
        });

        Self { window }
    }

    pub fn present(&self) {
        self.window.present();
    }
}

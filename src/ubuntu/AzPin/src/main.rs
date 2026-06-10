use adw::prelude::*;
use adw::Application;
use gtk4 as gtk;
use gtk::gio;

mod models;
mod services;
mod ui;
mod utils;

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
        let tray = crate::ui::indicator::AzPinTray { db: db.clone(), arm_service: arm_service.clone(), open_tx, settings_tx, pin_changed_tx };
        let tray_service = ksni::TrayService::new(tray);
        let tray_handle = tray_service.handle();
        tray_service.spawn();

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

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
        
        // Initialize Indicator
        // We leak the indicator or store it somewhere so it doesn't get dropped
        let indicator = crate::ui::indicator::IndicatorApp::new(db.clone(), arm_service.clone());
        Box::leak(Box::new(indicator));

        // Present main window for testing as well
        let window = crate::ui::main_window::MainWindow::new(app, db, arm_service);
        window.present();
        
        // This is a minimal hook to prevent immediate exit
        app.hold();
        0
    });

    // Run the application
    app.run();
}

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
        // Here we will eventually start the app indicator and background services
        // We do not present a window here by default
        
        // This is a minimal hook to prevent immediate exit while we run the indicator
        app.hold();
        0
    });

    // Run the application
    app.run();
}

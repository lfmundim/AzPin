use adw::prelude::*;
use gtk4 as gtk;
use std::sync::Arc;
use crate::services::db::Db;
use crate::services::arm::ArmService;
use crate::ui::settings::SettingsWindow;

pub struct MainWindow {
    window: adw::ApplicationWindow,
}

impl MainWindow {
    pub fn new(app: &adw::Application, db: Arc<Db>, arm_service: Arc<ArmService>) -> Self {
        let split_view = adw::OverlaySplitView::new();

        // --- Sidebar (Resource Groups) ---
        let sidebar_box = gtk::Box::new(gtk::Orientation::Vertical, 0);
        let sidebar_header = adw::HeaderBar::new();
        sidebar_header.set_title_widget(Some(&gtk::Label::new(Some("Resource Groups"))));
        
        // Settings Button
        let settings_btn = gtk::Button::builder()
            .icon_name("emblem-system-symbolic")
            .build();
        let app_clone = app.clone();
        let db_clone = db.clone();
        settings_btn.connect_clicked(move |_| {
            let settings = SettingsWindow::new(&app_clone, db_clone.clone());
            settings.present();
        });
        sidebar_header.pack_end(&settings_btn);

        sidebar_box.append(&sidebar_header);

        let rg_listbox = gtk::ListBox::new();
        rg_listbox.add_css_class("navigation-sidebar");
        
        // Load pinned groups into listbox
        if let Ok(groups) = db.get_pinned_groups() {
            for group in groups {
                let row = gtk::ListBoxRow::new();
                let label = gtk::Label::new(Some(&group.name));
                label.set_halign(gtk::Align::Start);
                label.set_margin_start(12);
                label.set_margin_end(12);
                label.set_margin_top(8);
                label.set_margin_bottom(8);
                row.set_child(Some(&label));
                rg_listbox.append(&row);
            }
        }
        
        let scrolled_sidebar = gtk::ScrolledWindow::builder()
            .hscrollbar_policy(gtk::PolicyType::Never)
            .child(&rg_listbox)
            .vexpand(true)
            .build();
        sidebar_box.append(&scrolled_sidebar);

        split_view.set_sidebar(Some(&sidebar_box));

        // --- Detail View (ViewStack with Tabs) ---
        let view_stack = adw::ViewStack::new();

        // 1. Pinned Tab
        let pinned_box = gtk::Box::new(gtk::Orientation::Vertical, 0);
        let pinned_list = gtk::ListBox::new();
        pinned_list.add_css_class("boxed-list");
        pinned_list.set_margin_top(16);
        pinned_list.set_margin_bottom(16);
        pinned_list.set_margin_start(16);
        pinned_list.set_margin_end(16);
        pinned_box.append(&pinned_list);
        
        view_stack.add_titled(&pinned_box, Some("pinned"), "Pinned");

        // 2. Browse Tab
        let browse_box = gtk::Box::new(gtk::Orientation::Vertical, 0);
        let search_entry = gtk::SearchEntry::new();
        search_entry.set_margin_top(16);
        search_entry.set_margin_bottom(16);
        search_entry.set_margin_start(16);
        search_entry.set_margin_end(16);
        browse_box.append(&search_entry);

        let browse_list = gtk::ListBox::new();
        browse_list.add_css_class("boxed-list");
        browse_list.set_margin_start(16);
        browse_list.set_margin_end(16);
        browse_list.set_margin_bottom(16);
        browse_box.append(&browse_list);
        
        view_stack.add_titled(&browse_box, Some("browse"), "Browse");

        // 3. All Subscriptions Tab
        let subs_box = gtk::Box::new(gtk::Orientation::Vertical, 0);
        let subs_label = gtk::Label::new(Some("Select a subscription..."));
        subs_box.append(&subs_label);
        view_stack.add_titled(&subs_box, Some("subscriptions"), "Subscriptions");

        // Setup ViewSwitcher in Detail Header
        let detail_box = gtk::Box::new(gtk::Orientation::Vertical, 0);
        let detail_header = adw::HeaderBar::new();
        
        let view_switcher = adw::ViewSwitcher::builder()
            .stack(&view_stack)
            .policy(adw::ViewSwitcherPolicy::Wide)
            .build();
        
        let switcher_title = adw::ViewSwitcherTitle::builder()
            .stack(&view_stack)
            .title("Details")
            .build();
        
        detail_header.set_title_widget(Some(&switcher_title));
        detail_box.append(&detail_header);
        detail_box.append(&view_stack);
        
        split_view.set_content(Some(&detail_box));

        // --- Create ApplicationWindow ---
        let window = adw::ApplicationWindow::builder()
            .application(app)
            .title("AzPin")
            .default_width(900)
            .default_height(600)
            .content(&split_view)
            .build();

        Self { window }
    }

    pub fn present(&self) {
        self.window.present();
    }
}

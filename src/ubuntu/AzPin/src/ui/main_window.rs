use adw::prelude::*;
use gtk4 as gtk;
use std::sync::Arc;
use crate::services::db::Db;
use crate::services::arm::ArmService;
use crate::services::az_cli::AzCliService;
use crate::ui::settings::SettingsWindow;

pub struct MainWindow {
    window: adw::ApplicationWindow,
}

impl MainWindow {
    pub fn new(app: &adw::Application, db: Arc<Db>, arm_service: Arc<ArmService>) -> Self {
        let root_stack = gtk::Stack::new();
        root_stack.set_transition_type(gtk::StackTransitionType::Crossfade);

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
        
        let is_logged_in_res = AzCliService::get_default_subscription();
        let subscription_id = is_logged_in_res.ok().map(|s| s.id).unwrap_or_default();

        let arm_svc = arm_service.clone();
        let sub_id = subscription_id.clone();
        let rg_listbox_clone = rg_listbox.clone();

        if !sub_id.is_empty() {
            gtk::glib::spawn_future_local(async move {
                if let Ok(groups) = arm_svc.fetch_resource_groups(&sub_id).await {
                    for group in groups {
                        let row = gtk::ListBoxRow::new();
                        let label = gtk::Label::new(Some(&group.name));
                        label.set_halign(gtk::Align::Start);
                        label.set_margin_start(12);
                        label.set_margin_end(12);
                        label.set_margin_top(8);
                        label.set_margin_bottom(8);
                        row.set_child(Some(&label));
                        row.set_widget_name(&group.name);
                        rg_listbox_clone.append(&row);
                    }
                }
            });
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

        let browse_list_clone = browse_list.clone();
        let arm_svc_browse = arm_service.clone();
        let sub_id_browse = subscription_id.clone();
        let db_clone_for_pin = db.clone();
        
        rg_listbox.connect_row_selected(move |_listbox, row_opt| {
            if let Some(row) = row_opt {
                let group_name = row.widget_name().to_string();
                let b_list = browse_list_clone.clone();
                let a_svc = arm_svc_browse.clone();
                let sub = sub_id_browse.clone();
                let db_ref = db_clone_for_pin.clone();
                
                while let Some(child) = b_list.first_child() {
                    b_list.remove(&child);
                }

                gtk::glib::spawn_future_local(async move {
                    if let Ok(resources) = a_svc.fetch_resources(&sub, &group_name).await {
                        for res in resources {
                            let res_row = gtk::ListBoxRow::new();
                            let box_ = gtk::Box::new(gtk::Orientation::Horizontal, 8);
                            box_.set_margin_start(12);
                            box_.set_margin_end(12);
                            box_.set_margin_top(8);
                            box_.set_margin_bottom(8);
                            
                            let label = gtk::Label::new(Some(&res.name));
                            label.set_halign(gtk::Align::Start);
                            label.set_hexpand(true);
                            
                            let pin_btn = gtk::Button::builder()
                                .icon_name("bookmark-new-symbolic")
                                .css_classes(vec!["flat".to_string()])
                                .build();
                                
                            // PINNING LOGIC
                            let res_clone = res.clone();
                            let db_clone2 = db_ref.clone();
                            let grp_name_clone = group_name.clone();
                            let sub_clone = sub.clone();
                            pin_btn.connect_clicked(move |_| {
                                use crate::models::persistence::{PinnedResource, PinnedResourceGroup};
                                let _ = db_clone2.save_pinned_group(&PinnedResourceGroup {
                                    id: grp_name_clone.clone(),
                                    subscription_id: sub_clone.clone(),
                                    name: grp_name_clone.clone(),
                                    display_order: 0,
                                    resources: vec![],
                                });
                                let _ = db_clone2.save_pinned_resource(&PinnedResource {
                                    id: res_clone.id.clone(),
                                    name: res_clone.name.clone(),
                                    type_: res_clone.type_.clone(),
                                    resource_group: grp_name_clone.clone(),
                                    subscription_id: sub_clone.clone(),
                                    location: res_clone.location.clone(),
                                    display_order: 0,
                                }, &grp_name_clone);
                            });

                            box_.append(&label);
                            box_.append(&pin_btn);
                            res_row.set_child(Some(&box_));
                            b_list.append(&res_row);
                        }
                    }
                });
            }
        });
        
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
        root_stack.add_named(&split_view, Some("main"));

        // --- Onboarding View ---
        let status_page = adw::StatusPage::builder()
            .title("Welcome to AzPin")
            .description("Please sign in to your Azure account to view and pin your resources.")
            .icon_name("network-server-symbolic")
            .build();

        let sign_in_btn = gtk::Button::builder()
            .label("Sign In to Azure")
            .css_classes(vec!["suggested-action".to_string(), "pill".to_string()])
            .halign(gtk::Align::Center)
            .margin_bottom(32)
            .build();

        status_page.set_child(Some(&sign_in_btn));
        root_stack.add_named(&status_page, Some("onboarding"));

        // --- Logic ---
        let is_logged_in = !subscription_id.is_empty();
        if is_logged_in {
            root_stack.set_visible_child_name("main");
        } else {
            root_stack.set_visible_child_name("onboarding");
        }

        let root_stack_clone = root_stack.clone();
        sign_in_btn.connect_clicked(move |_| {
            let root_stack_clone = root_stack_clone.clone();
            std::thread::spawn(move || {
                let _ = std::process::Command::new("az").arg("login").output();
                gtk::glib::idle_add_local(move || {
                    root_stack_clone.set_visible_child_name("main");
                    gtk::glib::ControlFlow::Break
                });
            });
        });

        // --- Create ApplicationWindow ---
        let window = adw::ApplicationWindow::builder()
            .application(app)
            .title("AzPin")
            .default_width(900)
            .default_height(600)
            .content(&root_stack)
            .build();

        Self { window }
    }

    pub fn present(&self) {
        self.window.present();
    }
}

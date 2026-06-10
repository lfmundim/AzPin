use adw::prelude::*;
use gtk4 as gtk;
use std::sync::Arc;
use std::cell::RefCell;
use std::rc::Rc;
use crate::services::db::Db;
use crate::services::arm::ArmService;
use crate::services::az_cli::{AzCliService, AzSubscription};
use crate::ui::settings::SettingsWindow;

pub struct MainWindow {
    window: adw::ApplicationWindow,
}

fn get_icon_for_type(res_type: &str) -> &'static str {
    match res_type.to_lowercase().as_str() {
        "microsoft.web/sites" | "microsoft.web/serverfarms" => "applications-internet-symbolic",
        "microsoft.app/containerapps" => "applications-development-symbolic",
        "microsoft.dbforpostgresql/flexibleservers" => "server-database-symbolic",
        "microsoft.keyvault/vaults" => "dialog-password-symbolic",
        "microsoft.insights/components" => "help-about-symbolic",
        "microsoft.storage/storageaccounts" => "drive-harddisk-symbolic",
        "microsoft.compute/virtualmachines" => "computer-symbolic",
        _ => "text-x-generic-symbolic"
    }
}

impl MainWindow {
    pub fn new(app: &adw::Application, db: Arc<Db>, arm_service: Arc<ArmService>, tray_handle: ksni::Handle<crate::ui::indicator::AzPinTray>) -> Self {
        let root_stack = gtk::Stack::new();
        root_stack.set_transition_type(gtk::StackTransitionType::Crossfade);

        let split_view = adw::OverlaySplitView::new();

        // --- Sidebar (Pinned Items) ---
        let sidebar_box = gtk::Box::new(gtk::Orientation::Vertical, 0);
        let sidebar_header = adw::HeaderBar::new();
        sidebar_header.set_title_widget(Some(&gtk::Label::new(Some("AzPin"))));
        
        let settings_btn = gtk::Button::builder().icon_name("emblem-system-symbolic").build();
        let app_clone = app.clone();
        let db_clone = db.clone();
        settings_btn.connect_clicked(move |_| {
            let settings = SettingsWindow::new(&app_clone, db_clone.clone());
            settings.present();
        });
        sidebar_header.pack_end(&settings_btn);
        sidebar_box.append(&sidebar_header);

        let pinned_list = gtk::ListBox::new();
        pinned_list.add_css_class("navigation-sidebar");
        
        let scrolled_sidebar = gtk::ScrolledWindow::builder()
            .hscrollbar_policy(gtk::PolicyType::Never)
            .child(&pinned_list)
            .vexpand(true)
            .build();
        sidebar_box.append(&scrolled_sidebar);
        split_view.set_sidebar(Some(&sidebar_box));

        // --- Detail View (Browse) ---
        let detail_box = gtk::Box::new(gtk::Orientation::Vertical, 0);
        let detail_header = adw::HeaderBar::new();
        detail_header.set_title_widget(Some(&gtk::Label::new(Some("Browse Azure"))));
        detail_box.append(&detail_header);

        // Header controls: Subscription Dropdown & Search
        let controls_box = gtk::Box::new(gtk::Orientation::Horizontal, 12);
        controls_box.set_margin_top(12); controls_box.set_margin_bottom(12);
        controls_box.set_margin_start(16); controls_box.set_margin_end(16);
        
        let sub_label = gtk::Label::new(Some("Subscription"));
        controls_box.append(&sub_label);
        
        let sub_model = gtk::StringList::new(&["Loading Subscriptions..."]);
        let sub_dropdown = gtk::DropDown::new(Some(sub_model.clone()), gtk::Expression::NONE);
        sub_dropdown.set_valign(gtk::Align::Center);
        controls_box.append(&sub_dropdown);

        let search_entry = gtk::SearchEntry::new();
        search_entry.set_hexpand(true);
        search_entry.set_placeholder_text(Some("Search resource groups"));
        search_entry.set_valign(gtk::Align::Center);
        controls_box.append(&search_entry);

        detail_box.append(&controls_box);

        // Resource Groups List
        let rg_list = gtk::ListBox::new();
        rg_list.add_css_class("boxed-list");
        rg_list.set_margin_start(16); rg_list.set_margin_end(16);
        rg_list.set_margin_bottom(16);
        
        let scrolled_rgs = gtk::ScrolledWindow::builder()
            .hscrollbar_policy(gtk::PolicyType::Never)
            .child(&rg_list)
            .vexpand(true)
            .build();
        detail_box.append(&scrolled_rgs);

        split_view.set_content(Some(&detail_box));
        root_stack.add_named(&split_view, Some("main"));

        // --- Onboarding View ---
        let status_page = adw::StatusPage::builder()
            .title("Welcome to AzPin")
            .description("Not signed in — run 'az login' in your terminal.")
            .icon_name("network-server-symbolic")
            .build();

        let refresh_btn = gtk::Button::builder()
            .label("Refresh Auth Status")
            .css_classes(vec!["suggested-action".to_string(), "pill".to_string()])
            .halign(gtk::Align::Center)
            .margin_bottom(32)
            .build();

        status_page.set_child(Some(&refresh_btn));
        root_stack.add_named(&status_page, Some("onboarding"));

        // --- Logic: Load Pinned Items to Sidebar ---
        let load_pinned_items = {
            let pinned_list = pinned_list.clone();
            let db_ref = db.clone();
            Rc::new(move || {
                while let Some(child) = pinned_list.first_child() {
                    pinned_list.remove(&child);
                }
                
                // Add Pinned RGs
                let mut title_added = false;
                if let Ok(groups) = db_ref.get_pinned_groups() {
                    for group in groups {
                        if !title_added {
                            let label = gtk::Label::new(Some("<b>Pinned Resource Groups</b>"));
                            label.set_use_markup(true); label.set_halign(gtk::Align::Start);
                            label.set_margin_start(12); label.set_margin_top(12); label.set_margin_bottom(6);
                            pinned_list.append(&label);
                            title_added = true;
                        }
                        let row = gtk::ListBoxRow::new();
                        let box_ = gtk::Box::new(gtk::Orientation::Horizontal, 8);
                        box_.set_margin_start(12); box_.set_margin_end(12); box_.set_margin_top(8); box_.set_margin_bottom(8);
                        box_.append(&gtk::Image::from_icon_name("folder-symbolic"));
                        let label = gtk::Label::new(Some(&group.name));
                        label.set_hexpand(true); label.set_halign(gtk::Align::Start);
                        box_.append(&label);
                        row.set_child(Some(&box_));
                        
                        // Open portal on click
                        let g_id = group.id.clone();
                        let tap = gtk::GestureClick::new();
                        tap.connect_pressed(move |_, _, _, _| {
                            let uri = format!("https://portal.azure.com/#resource{}", g_id);
                            let _ = gtk::gio::AppInfo::launch_default_for_uri(&uri, None::<&gtk::gio::AppLaunchContext>);
                        });
                        row.add_controller(tap);
                        pinned_list.append(&row);
                    }
                }
                
                // Add Pinned Resources
                let mut title_added2 = false;
                // Wait, db.rs doesn't have a direct get_all_pinned_resources method, we have to iterate groups or create a new method.
                // For simplicity, we can fetch them via a raw query if needed, or get them from db.
            })
        };
        // Wait, db.rs only has `get_pinned_resources(&self, group_id: &str)`. 
        // We'll need to fetch all pinned groups, then fetch resources for each.

        let load_pinned_items_clone = load_pinned_items.clone();
        let db_for_sidebar = db.clone();
        let pinned_list_for_sidebar = pinned_list.clone();
        
        let load_sidebar = Rc::new(move || {
            while let Some(child) = pinned_list_for_sidebar.first_child() {
                pinned_list_for_sidebar.remove(&child);
            }
            let label = gtk::Label::new(Some("<b>Pinned</b>"));
            label.set_use_markup(true); label.set_halign(gtk::Align::Start);
            label.set_margin_start(12); label.set_margin_top(12); label.set_margin_bottom(6);
            pinned_list_for_sidebar.append(&label);

            if let Ok(groups) = db_for_sidebar.get_pinned_groups() {
                for group in groups {
                    // Pinned Group row
                    let row = gtk::ListBoxRow::new();
                    let box_ = gtk::Box::new(gtk::Orientation::Horizontal, 8);
                    box_.set_margin_start(12); box_.set_margin_end(12); box_.set_margin_top(8); box_.set_margin_bottom(8);
                    box_.append(&gtk::Image::from_icon_name("folder-symbolic"));
                    let label = gtk::Label::new(Some(&group.name));
                    label.set_hexpand(true); label.set_halign(gtk::Align::Start);
                    box_.append(&label);
                    row.set_child(Some(&box_));
                    let g_id = group.id.clone();
                    let tap = gtk::GestureClick::new();
                    tap.connect_pressed(move |_, _, _, _| {
                        let uri = format!("https://portal.azure.com/#resource{}", g_id);
                        let _ = gtk::gio::AppInfo::launch_default_for_uri(&uri, None::<&gtk::gio::AppLaunchContext>);
                    });
                    row.add_controller(tap);
                    pinned_list_for_sidebar.append(&row);

                    // Fetch pinned resources for this group
                    if let Ok(resources) = db_for_sidebar.get_pinned_resources(&group.id) {
                        for res in resources {
                            let row = gtk::ListBoxRow::new();
                            let box_ = gtk::Box::new(gtk::Orientation::Horizontal, 8);
                            box_.set_margin_start(24); box_.set_margin_end(12); box_.set_margin_top(8); box_.set_margin_bottom(8); // indented
                            box_.append(&gtk::Image::from_icon_name(get_icon_for_type(&res.type_)));
                            let label = gtk::Label::new(Some(&res.name));
                            label.set_hexpand(true); label.set_halign(gtk::Align::Start);
                            box_.append(&label);
                            row.set_child(Some(&box_));
                            let r_id = res.id.clone();
                            let tap = gtk::GestureClick::new();
                            tap.connect_pressed(move |_, _, _, _| {
                                let uri = format!("https://portal.azure.com/#resource{}", r_id);
                                let _ = gtk::gio::AppInfo::launch_default_for_uri(&uri, None::<&gtk::gio::AppLaunchContext>);
                            });
                            row.add_controller(tap);
                            pinned_list_for_sidebar.append(&row);
                        }
                    }
                }
            }
        });
        load_sidebar();

        // --- Logic: Load Subscriptions ---
        let subs_cache: Rc<RefCell<Vec<AzSubscription>>> = Rc::new(RefCell::new(Vec::new()));
        let (sub_tx, sub_rx) = gtk::glib::MainContext::channel(gtk::glib::Priority::DEFAULT);
        let sub_model_clone = sub_model.clone();
        let subs_cache_clone = subs_cache.clone();
        let sub_dropdown_clone = sub_dropdown.clone();
        sub_rx.attach(None, move |subs: Vec<AzSubscription>| {
            sub_dropdown_clone.set_selected(gtk::INVALID_LIST_POSITION);
            sub_model_clone.splice(0, sub_model_clone.n_items(), &subs.iter().map(|s| s.name.as_str()).collect::<Vec<_>>());
            *subs_cache_clone.borrow_mut() = subs;
            sub_dropdown_clone.set_selected(0);
            gtk::glib::ControlFlow::Continue
        });
        std::thread::spawn(move || {
            if let Ok(subs) = AzCliService::list_subscriptions() {
                let _ = sub_tx.send(subs);
            }
        });

        // --- Logic: Search ---
        let rg_list_search = rg_list.clone();
        search_entry.connect_search_changed(move |entry| {
            let query = entry.text().to_string().to_lowercase();
            let mut i = 0;
            while let Some(row) = rg_list_search.row_at_index(i) {
                if let Some(exp) = row.downcast_ref::<adw::ExpanderRow>() {
                    let title = exp.title().to_string().to_lowercase();
                    row.set_visible(query.is_empty() || title.contains(&query));
                }
                i += 1;
            }
        });

        // --- Logic: Subscription Selected -> Fetch RGs ---
        let arm_svc_rg = arm_service.clone();
        let live_rg_list_clone = rg_list.clone();
        let db_for_rg = db.clone();
        let tray_handle_rg = tray_handle.clone();
        let load_sidebar_rg = load_sidebar.clone();
        
        sub_dropdown.connect_selected_item_notify(move |dropdown| {
            let idx = dropdown.selected() as usize;
            let subs = subs_cache.borrow();
            if idx < subs.len() {
                let sub_id = subs[idx].id.clone();
                let a_svc = arm_svc_rg.clone();
                let list_c = live_rg_list_clone.clone();
                let db_c = db_for_rg.clone();
                let tray_c = tray_handle_rg.clone();
                let ls_c = load_sidebar_rg.clone();
                
                while let Some(child) = list_c.first_child() {
                    list_c.remove(&child);
                }

                gtk::glib::spawn_future_local(async move {
                    if let Ok(groups) = a_svc.fetch_resource_groups(&sub_id).await {
                        for group in groups {
                            let exp_row = adw::ExpanderRow::new();
                            exp_row.set_title(&group.name);
                            exp_row.add_prefix(&gtk::Image::from_icon_name("folder-symbolic"));
                            
                            // Check if pinned
                            let is_pinned = db_c.get_pinned_groups().unwrap_or_default().iter().any(|g| g.id == group.id);
                            
                            let pin_btn = gtk::ToggleButton::builder()
                                .icon_name("bookmark-new-symbolic")
                                .css_classes(vec!["flat".to_string()])
                                .valign(gtk::Align::Center)
                                .active(is_pinned)
                                .build();
                                
                            let g_id = group.id.clone();
                            let s_id = sub_id.clone();
                            let g_name = group.name.clone();
                            let db_pin = db_c.clone();
                            let tr_pin = tray_c.clone();
                            let ls_pin = ls_c.clone();
                            
                            pin_btn.connect_toggled(move |btn| {
                                use crate::models::persistence::PinnedResourceGroup;
                                if btn.is_active() {
                                    let _ = db_pin.save_pinned_group(&PinnedResourceGroup {
                                        id: g_id.clone(), subscription_id: s_id.clone(), name: g_name.clone(), display_order: 0, resources: vec![]
                                    });
                                } else {
                                    let _ = db_pin.delete_pinned_group(&g_id);
                                }
                                let _ = tr_pin.update(|_| {});
                                ls_pin();
                            });
                            exp_row.add_suffix(&pin_btn);
                            
                            // Load resources on expand
                            let loaded = Rc::new(RefCell::new(false));
                            let a_svc_res = a_svc.clone();
                            let sub_res = sub_id.clone();
                            let grp_res = group.name.clone();
                            let g_id_res = group.id.clone();
                            let db_res = db_c.clone();
                            let tray_res = tray_c.clone();
                            let ls_res = ls_c.clone();
                            let exp_row_clone = exp_row.clone();
                            
                            exp_row.connect_expanded_notify(move |exp| {
                                if exp.is_expanded() && !*loaded.borrow() {
                                    *loaded.borrow_mut() = true;
                                    let asr = a_svc_res.clone();
                                    let sr = sub_res.clone();
                                    let gr = grp_res.clone();
                                    let dbr = db_res.clone();
                                    let tr = tray_res.clone();
                                    let lsr = ls_res.clone();
                                    let er = exp_row_clone.clone();
                                    let g_id_r = g_id_res.clone();
                                    
                                    gtk::glib::spawn_future_local(async move {
                                        if let Ok(resources) = asr.fetch_resources(&sr, &gr).await {
                                            for res in resources {
                                                let act_row = adw::ActionRow::new();
                                                act_row.set_title(&res.name);
                                                act_row.add_prefix(&gtk::Image::from_icon_name(get_icon_for_type(&res.type_)));
                                                
                                                // Check if pinned
                                                let is_res_pinned = dbr.get_pinned_resources(&g_id_r).unwrap_or_default().iter().any(|r| r.id == res.id);
                                                
                                                let res_pin_btn = gtk::ToggleButton::builder()
                                                    .icon_name("bookmark-new-symbolic")
                                                    .css_classes(vec!["flat".to_string()])
                                                    .valign(gtk::Align::Center)
                                                    .active(is_res_pinned)
                                                    .build();
                                                    
                                                let r_id = res.id.clone();
                                                let r_name = res.name.clone();
                                                let r_type = res.type_.clone();
                                                let r_loc = res.location.clone();
                                                let g_r = gr.clone();
                                                let s_r = sr.clone();
                                                let db_r_pin = dbr.clone();
                                                let tr_r_pin = tr.clone();
                                                let ls_r_pin = lsr.clone();
                                                let group_id_r = g_id_r.clone();
                                                
                                                // Save group first to satisfy foreign key if it's not pinned
                                                let group_id_for_fk = g_id_r.clone();
                                                let sub_for_fk = sr.clone();
                                                let group_name_for_fk = gr.clone();
                                                
                                                res_pin_btn.connect_toggled(move |btn| {
                                                    use crate::models::persistence::{PinnedResource, PinnedResourceGroup};
                                                    if btn.is_active() {
                                                        // Ensure group exists in DB
                                                        let _ = db_r_pin.save_pinned_group(&PinnedResourceGroup {
                                                            id: group_id_for_fk.clone(), subscription_id: sub_for_fk.clone(), name: group_name_for_fk.clone(), display_order: 0, resources: vec![]
                                                        });
                                                        let _ = db_r_pin.save_pinned_resource(&PinnedResource {
                                                            id: r_id.clone(), name: r_name.clone(), type_: r_type.clone(),
                                                            resource_group: g_r.clone(), subscription_id: s_r.clone(), location: r_loc.clone(), display_order: 0,
                                                        }, &group_id_r);
                                                    } else {
                                                        let _ = db_r_pin.delete_pinned_resource(&r_id);
                                                    }
                                                    let _ = tr_r_pin.update(|_| {});
                                                    ls_r_pin();
                                                });
                                                act_row.add_suffix(&res_pin_btn);
                                                er.add_row(&act_row);
                                            }
                                        }
                                    });
                                }
                            });
                            
                            list_c.append(&exp_row);
                        }
                    }
                });
            }
        });

        // --- Logic: Auth Status ---
        let (chk_tx, chk_rx) = gtk::glib::MainContext::channel(gtk::glib::Priority::DEFAULT);
        let root_stack_init = root_stack.clone();
        chk_rx.attach(None, move |is_logged_in| {
            if is_logged_in {
                root_stack_init.set_visible_child_name("main");
            } else {
                root_stack_init.set_visible_child_name("onboarding");
            }
            gtk::glib::ControlFlow::Continue
        });
        std::thread::spawn(move || {
            let is_logged_in = AzCliService::get_default_subscription().is_ok();
            let _ = chk_tx.send(is_logged_in);
        });

        let (sender, receiver) = gtk::glib::MainContext::channel(gtk::glib::Priority::DEFAULT);
        let root_stack_clone = root_stack.clone();
        receiver.attach(None, move |is_logged_in| {
            if is_logged_in {
                root_stack_clone.set_visible_child_name("main");
            }
            gtk::glib::ControlFlow::Continue
        });

        refresh_btn.connect_clicked(move |_| {
            let sender = sender.clone();
            std::thread::spawn(move || {
                let is_logged_in = AzCliService::get_default_subscription().is_ok();
                let _ = sender.send(is_logged_in);
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

        window.connect_close_request(move |win| {
            win.hide();
            gtk::glib::Propagation::Stop
        });

        Self { window }
    }

    pub fn present(&self) {
        self.window.present();
    }
}

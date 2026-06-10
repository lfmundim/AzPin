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

impl MainWindow {
    pub fn new(app: &adw::Application, db: Arc<Db>, arm_service: Arc<ArmService>, tray_handle: ksni::Handle<crate::ui::indicator::AzPinTray>) -> Self {
        let root_stack = gtk::Stack::new();
        root_stack.set_transition_type(gtk::StackTransitionType::Crossfade);

        let split_view = adw::OverlaySplitView::new();

        // --- Sidebar ---
        let sidebar_box = gtk::Box::new(gtk::Orientation::Vertical, 0);
        let sidebar_header = adw::HeaderBar::new();
        sidebar_header.set_title_widget(Some(&gtk::Label::new(Some("AzPin"))));
        
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

        let sidebar_list = gtk::ListBox::new();
        sidebar_list.add_css_class("navigation-sidebar");
        
        // 1. Static "Browse Azure" Row
        let browse_row = gtk::ListBoxRow::new();
        let browse_box = gtk::Box::new(gtk::Orientation::Horizontal, 8);
        browse_box.set_margin_start(12); browse_box.set_margin_end(12);
        browse_box.set_margin_top(8); browse_box.set_margin_bottom(8);
        let browse_icon = gtk::Image::from_icon_name("network-server-symbolic");
        let browse_label = gtk::Label::new(Some("Browse Azure"));
        browse_label.set_hexpand(true); browse_label.set_halign(gtk::Align::Start);
        browse_box.append(&browse_icon);
        browse_box.append(&browse_label);
        browse_row.set_child(Some(&browse_box));
        browse_row.set_widget_name("BROWSE_AZURE");
        sidebar_list.append(&browse_row);

        // 2. Pinned RGs from DB
        let load_pinned_rgs = {
            let sidebar_list = sidebar_list.clone();
            let db_ref = db.clone();
            let tray_handle_clone = tray_handle.clone();
            Rc::new(move || {
                // Clear existing pinned rows
                let mut child = sidebar_list.first_child();
                while let Some(c) = child {
                    let next = c.next_sibling();
                    if let Some(row) = c.downcast_ref::<gtk::ListBoxRow>() {
                        if row.widget_name().as_str() != "BROWSE_AZURE" {
                            sidebar_list.remove(row);
                        }
                    }
                    child = next;
                }
                
                if let Ok(groups) = db_ref.get_pinned_groups() {
                    for group in groups {
                        let row = gtk::ListBoxRow::new();
                        let box_ = gtk::Box::new(gtk::Orientation::Horizontal, 8);
                        box_.set_margin_start(12); box_.set_margin_end(12);
                        box_.set_margin_top(8); box_.set_margin_bottom(8);
                        
                        let icon = gtk::Image::from_icon_name("folder-symbolic");
                        let label = gtk::Label::new(Some(&group.name));
                        label.set_hexpand(true); label.set_halign(gtk::Align::Start);
                        
                        let unpin_btn = gtk::Button::builder()
                            .icon_name("user-trash-symbolic")
                            .css_classes(vec!["flat".to_string()])
                            .build();
                            
                        let db_unpin = db_ref.clone();
                        let tray_unpin = tray_handle_clone.clone();
                        let grp_id = group.id.clone();
                        // We will need a way to reload this list after unpin, but doing it safely is tricky without a signal.
                        // For now, we will just remove the row visually and from DB.
                        let row_ref = row.clone();
                        let list_ref = sidebar_list.clone();
                        unpin_btn.connect_clicked(move |_| {
                            let _ = db_unpin.delete_pinned_group(&grp_id);
                            list_ref.remove(&row_ref);
                            let _ = tray_unpin.update(|_| {});
                        });
                        
                        box_.append(&icon);
                        box_.append(&label);
                        box_.append(&unpin_btn);
                        
                        row.set_child(Some(&box_));
                        row.set_widget_name(&format!("RG:{}|{}", group.subscription_id, group.name));
                        sidebar_list.append(&row);
                    }
                }
            })
        };
        load_pinned_rgs();

        let scrolled_sidebar = gtk::ScrolledWindow::builder()
            .hscrollbar_policy(gtk::PolicyType::Never)
            .child(&sidebar_list)
            .vexpand(true)
            .build();
        sidebar_box.append(&scrolled_sidebar);
        split_view.set_sidebar(Some(&sidebar_box));

        // --- Detail View (Stack) ---
        let detail_stack = gtk::Stack::new();
        detail_stack.set_transition_type(gtk::StackTransitionType::Crossfade);
        
        let detail_header = adw::HeaderBar::new();
        let detail_box = gtk::Box::new(gtk::Orientation::Vertical, 0);
        detail_box.append(&detail_header);
        detail_box.append(&detail_stack);

        // -- Page 1: Browse Azure --
        let browse_azure_page = gtk::Box::new(gtk::Orientation::Vertical, 0);
        
        let sub_model = gtk::StringList::new(&["Loading Subscriptions..."]);
        let sub_dropdown = gtk::DropDown::new(Some(sub_model.clone()), gtk::Expression::NONE);
        sub_dropdown.set_margin_top(16); sub_dropdown.set_margin_bottom(16);
        sub_dropdown.set_margin_start(16); sub_dropdown.set_margin_end(16);
        browse_azure_page.append(&sub_dropdown);

        let search_entry = gtk::SearchEntry::new();
        search_entry.set_margin_bottom(16);
        search_entry.set_margin_start(16); search_entry.set_margin_end(16);
        browse_azure_page.append(&search_entry);

        let live_rg_list = gtk::ListBox::new();
        live_rg_list.add_css_class("boxed-list");
        live_rg_list.set_margin_start(16); live_rg_list.set_margin_end(16);
        
        let scrolled_live_rgs = gtk::ScrolledWindow::builder()
            .hscrollbar_policy(gtk::PolicyType::Never)
            .child(&live_rg_list)
            .vexpand(true)
            .build();
        browse_azure_page.append(&scrolled_live_rgs);

        detail_stack.add_named(&browse_azure_page, Some("browse_azure"));

        // -- Page 2: Pinned RG View (with Tabs) --
        let rg_view_stack = adw::ViewStack::new();
        
        let pinned_tab_box = gtk::Box::new(gtk::Orientation::Vertical, 0);
        let pinned_res_list = gtk::ListBox::new();
        let pinned_res_list_clone = pinned_res_list.clone();
        pinned_res_list.add_css_class("boxed-list");
        pinned_res_list.set_margin_top(16); pinned_res_list.set_margin_bottom(16);
        pinned_res_list.set_margin_start(16); pinned_res_list.set_margin_end(16);
        
        let scrolled_pinned_res = gtk::ScrolledWindow::builder()
            .hscrollbar_policy(gtk::PolicyType::Never)
            .child(&pinned_res_list)
            .vexpand(true)
            .build();
        pinned_tab_box.append(&scrolled_pinned_res);
        rg_view_stack.add_titled(&pinned_tab_box, Some("pinned"), "Pinned");

        let browse_tab_box = gtk::Box::new(gtk::Orientation::Vertical, 0);
        let browse_res_list = gtk::ListBox::new();
        browse_res_list.add_css_class("boxed-list");
        browse_res_list.set_margin_top(16); browse_res_list.set_margin_bottom(16);
        browse_res_list.set_margin_start(16); browse_res_list.set_margin_end(16);
        
        let scrolled_browse_res = gtk::ScrolledWindow::builder()
            .hscrollbar_policy(gtk::PolicyType::Never)
            .child(&browse_res_list)
            .vexpand(true)
            .build();
        browse_tab_box.append(&scrolled_browse_res);
        rg_view_stack.add_titled(&browse_tab_box, Some("browse"), "Browse");

        detail_stack.add_named(&rg_view_stack, Some("rg_view"));

        // ViewSwitcher in header (only shown when in rg_view)
        let switcher_title = adw::ViewSwitcherTitle::builder()
            .stack(&rg_view_stack)
            .title("Resource Group")
            .build();
        detail_header.set_title_widget(Some(&switcher_title));

        split_view.set_content(Some(&detail_box));
        root_stack.add_named(&split_view, Some("main"));

        // Contexts for logic
        let subs_cache: Rc<RefCell<Vec<AzSubscription>>> = Rc::new(RefCell::new(Vec::new()));

        // 1. Load Subscriptions
        let (sub_tx, sub_rx) = gtk::glib::MainContext::channel(gtk::glib::Priority::DEFAULT);
        let sub_model_clone = sub_model.clone();
        let subs_cache_clone = subs_cache.clone();
        let sub_dropdown_clone = sub_dropdown.clone();
        sub_rx.attach(None, move |subs: Vec<AzSubscription>| {
            sub_model_clone.splice(0, sub_model_clone.n_items(), &subs.iter().map(|s| s.name.as_str()).collect::<Vec<_>>());
            *subs_cache_clone.borrow_mut() = subs;
            sub_dropdown_clone.notify("selected-item");
            gtk::glib::ControlFlow::Continue
        });
        std::thread::spawn(move || {
            if let Ok(subs) = AzCliService::list_subscriptions() {
                let _ = sub_tx.send(subs);
            }
        });

        // 2. Sub Dropdown Selection -> Load Live RGs
        let arm_svc_rg = arm_service.clone();
        let live_rg_list_clone = live_rg_list.clone();
        let db_for_pin_rg = db.clone();
        let tray_for_pin_rg = tray_handle.clone();
        let load_pinned_rgs_clone = load_pinned_rgs.clone();
        let subs_cache_for_sel = subs_cache.clone();
        
        sub_dropdown.connect_selected_item_notify(move |dropdown| {
            let idx = dropdown.selected() as usize;
            let subs = subs_cache_for_sel.borrow();
            if idx < subs.len() {
                let sub_id = subs[idx].id.clone();
                let a_svc = arm_svc_rg.clone();
                let rg_list = live_rg_list_clone.clone();
                let db_pin = db_for_pin_rg.clone();
                let tray_pin = tray_for_pin_rg.clone();
                let reload_rgs = load_pinned_rgs_clone.clone();
                
                while let Some(child) = rg_list.first_child() {
                    rg_list.remove(&child);
                }

                gtk::glib::spawn_future_local(async move {
                    if let Ok(groups) = a_svc.fetch_resource_groups(&sub_id).await {
                        for group in groups {
                            let row = gtk::ListBoxRow::new();
                            let box_ = gtk::Box::new(gtk::Orientation::Horizontal, 8);
                            box_.set_margin_start(12); box_.set_margin_end(12);
                            box_.set_margin_top(8); box_.set_margin_bottom(8);
                            
                            let label = gtk::Label::new(Some(&group.name));
                            label.set_halign(gtk::Align::Start); label.set_hexpand(true);
                            
                            let pin_btn = gtk::Button::builder()
                                .icon_name("bookmark-new-symbolic")
                                .css_classes(vec!["flat".to_string()])
                                .build();
                            
                            let db_c = db_pin.clone();
                            let s_id = sub_id.clone();
                            let g_name = group.name.clone();
                            let tray_c = tray_pin.clone();
                            let reload_c = reload_rgs.clone();
                            
                            pin_btn.connect_clicked(move |_| {
                                use crate::models::persistence::PinnedResourceGroup;
                                let _ = db_c.save_pinned_group(&PinnedResourceGroup {
                                    id: g_name.clone(),
                                    subscription_id: s_id.clone(),
                                    name: g_name.clone(),
                                    display_order: 0,
                                    resources: vec![],
                                });
                                let _ = tray_c.update(|_| {});
                                reload_c(); // refresh sidebar
                            });
                            
                            box_.append(&label);
                            box_.append(&pin_btn);
                            row.set_child(Some(&box_));
                            rg_list.append(&row);
                        }
                    }
                });
            }
        });

        // 3. Sidebar Selection Logic
        let arm_svc_browse = arm_service.clone();
        let browse_res_list_clone = browse_res_list.clone();
        let db_for_pin = db.clone();
        let tray_handle_pin = tray_handle.clone();
        let header_bar_clone = header_bar.clone();
        let switcher_title_clone = switcher_title.clone();
        
        sidebar_list.connect_row_selected(move |_listbox, row_opt| {
            if let Some(row) = row_opt {
                let name = row.widget_name().to_string();
                if name == "BROWSE_AZURE" {
                    detail_stack.set_visible_child_name("browse_azure");
                    let title = adw::WindowTitle::new("Browse Azure", "");
                    header_bar_clone.set_title_widget(Some(&title));
                } else if name.starts_with("RG:") {
                    detail_stack.set_visible_child_name("rg_view");
                    header_bar_clone.set_title_widget(Some(&switcher_title_clone));
                    let parts: Vec<&str> = name.split('|').collect();
                    if parts.len() == 2 {
                        let sub_id = parts[0].replace("RG:", "");
                        let rg_name = parts[1].to_string();
                        switcher_title_clone.set_title(&rg_name);
                        
                        // Clear browse list
                        let b_list = browse_res_list_clone.clone();
                        while let Some(child) = b_list.first_child() {
                            b_list.remove(&child);
                        }
                        
                        // Load Live Resources
                        let a_svc = arm_svc_browse.clone();
                        let sub = sub_id.clone();
                        let grp = rg_name.clone();
                        let db_ref = db_for_pin.clone();
                        let tray_ref = tray_handle_pin.clone();
                        
                        gtk::glib::spawn_future_local(async move {
                            if let Ok(resources) = a_svc.fetch_resources(&sub, &grp).await {
                                for res in resources {
                                    let res_row = gtk::ListBoxRow::new();
                                    let box_ = gtk::Box::new(gtk::Orientation::Horizontal, 8);
                                    box_.set_margin_start(12); box_.set_margin_end(12);
                                    box_.set_margin_top(8); box_.set_margin_bottom(8);
                                    
                                    let label = gtk::Label::new(Some(&res.name));
                                    label.set_halign(gtk::Align::Start); label.set_hexpand(true);
                                    
                                    let pin_btn = gtk::Button::builder()
                                        .icon_name("bookmark-new-symbolic")
                                        .css_classes(vec!["flat".to_string()])
                                        .build();
                                        
                                    let res_clone = res.clone();
                                    let db_c = db_ref.clone();
                                    let g_c = grp.clone();
                                    let s_c = sub.clone();
                                    let tray_c = tray_ref.clone();
                                    
                                    pin_btn.connect_clicked(move |_| {
                                        use crate::models::persistence::{PinnedResource, PinnedResourceGroup};
                                        let _ = db_c.save_pinned_group(&PinnedResourceGroup {
                                            id: g_c.clone(), subscription_id: s_c.clone(), name: g_c.clone(), display_order: 0, resources: vec![]
                                        });
                                        let _ = db_c.save_pinned_resource(&PinnedResource {
                                            id: res_clone.id.clone(), name: res_clone.name.clone(), type_: res_clone.type_.clone(),
                                            resource_group: g_c.clone(), subscription_id: s_c.clone(), location: res_clone.location.clone(), display_order: 0,
                                        }, &g_c);
                                        let _ = tray_c.update(|_| {});
                                    });

                                    let portal_btn = gtk::Button::builder()
                                        .icon_name("external-link-symbolic")
                                        .css_classes(vec!["flat".to_string()])
                                        .build();
                                    
                                    let res_id_clone = res.id.clone();
                                    portal_btn.connect_clicked(move |_| {
                                        let uri = format!("https://portal.azure.com/#resource{}", res_id_clone);
                                        let _ = gtk::gio::AppInfo::launch_default_for_uri(&uri, None::<&gtk::gio::AppLaunchContext>);
                                    });

                                    box_.append(&label);
                                    box_.append(&portal_btn);
                                    box_.append(&pin_btn);
                                    res_row.set_child(Some(&box_));
                                    b_list.append(&res_row);
                                }
                            }
                        });
                        
                        // Clear pinned list
                        let p_list = pinned_res_list_clone.clone();
                        while let Some(child) = p_list.first_child() {
                            p_list.remove(&child);
                        }
                        
                        // Load Pinned Resources
                        let p_list_clone = p_list.clone();
                        let db_p = db_for_pin.clone();
                        let g_id = rg_name.clone();
                        let tray_p = tray_handle_pin.clone();
                        
                        gtk::glib::spawn_future_local(async move {
                            if let Ok(pinned_res) = db_p.get_pinned_resources(&g_id) {
                                for res in pinned_res {
                                    let res_row = gtk::ListBoxRow::new();
                                    let box_ = gtk::Box::new(gtk::Orientation::Horizontal, 8);
                                    box_.set_margin_start(12); box_.set_margin_end(12);
                                    box_.set_margin_top(8); box_.set_margin_bottom(8);
                                    
                                    let label = gtk::Label::new(Some(&res.name));
                                    label.set_halign(gtk::Align::Start); label.set_hexpand(true);
                                    
                                    let portal_btn = gtk::Button::builder()
                                        .icon_name("external-link-symbolic")
                                        .css_classes(vec!["flat".to_string()])
                                        .build();
                                        
                                    let res_id_clone = res.id.clone();
                                    portal_btn.connect_clicked(move |_| {
                                        let uri = format!("https://portal.azure.com/#resource{}", res_id_clone);
                                        let _ = gtk::gio::AppInfo::launch_default_for_uri(&uri, None::<&gtk::gio::AppLaunchContext>);
                                    });
                                    
                                    let unpin_btn = gtk::Button::builder()
                                        .icon_name("user-trash-symbolic")
                                        .css_classes(vec!["flat".to_string()])
                                        .build();
                                    
                                    let db_unpin = db_p.clone();
                                    let row_ref = res_row.clone();
                                    let list_ref = p_list_clone.clone();
                                    let tray_unpin = tray_p.clone();
                                    let unpin_id = res.id.clone();
                                    
                                    // We don't have delete_pinned_resource yet, so we will need to add it to db.rs
                                    unpin_btn.connect_clicked(move |_| {
                                        let _ = db_unpin.delete_pinned_resource(&unpin_id);
                                        list_ref.remove(&row_ref);
                                        let _ = tray_unpin.update(|_| {});
                                    });
                                    
                                    box_.append(&label);
                                    box_.append(&portal_btn);
                                    box_.append(&unpin_btn);
                                    res_row.set_child(Some(&box_));
                                    p_list_clone.append(&res_row);
                                }
                            }
                        });
                    }
                }
            }
        });

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

        // --- Logic ---
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

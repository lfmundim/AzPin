# Task 4: AppIndicator (Menubar)

## Objective
Create the top-panel indicator and its dynamic menu using GNOME/Ubuntu APIs.

## Context
Ubuntu does not use native macOS-style MenuBars or Windows-style System Trays. It uses AppIndicators.

## Steps
1. **Initialize AppIndicator:**
   - Use `ayatana-appindicator` (or a suitable Rust wrapper like `ayatana-appindicator-rs`).
   - Set the indicator icon to a bundled SVG representing the `cloud.fill` icon.
   - Ensure the indicator has `IndicatorCategory::ApplicationStatus`.

2. **Build the Menu (GTK Menu):**
   - Create a `gtk::Menu`.
   - Top item: Authentication Status (e.g., `user@tenant (subscription name)` or `⚠️ Not signed in`).
   - Iterate over `PinnedResourceGroup` list. For each:
     - Create a `gtk::MenuItem` or a `gtk::Menu` submenu if you want a drawer. (Note: AppIndicators do not support custom widgets in menus very well, so rely on standard Submenus).
     - Inside the group submenu, list the `PinnedResource` items.
   - For runnable resources, you need action buttons. Since standard GTK Menus in AppIndicators only support text and an icon, you might need to map actions to sub-items (e.g., Resource Name -> [Start, Stop, Restart]).

3. **Menu Interaction:**
   - Fetch real-time statuses (Running/Stopped) *when the menu opens* using the `about-to-show` signal of the `gtk::Menu`.
   - Connect menu item clicks to open the browser: Use `gio::AppInfo::launch_default_for_uri` to open the portal URL.
   - Connect action clicks to `ArmService` mutations, updating the UI to a loading state while the request processes.

## Constraints
- Do not use background polling timers. Fetch on menu open.
- Gracefully handle `az` not being logged in (show a disabled menu item).

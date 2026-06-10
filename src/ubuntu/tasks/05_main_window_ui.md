# Task 5: Main Window UI

## Objective
Build the main management interface using `libadwaita`.

## Context
This window is opened via the "Open AzPin..." menu item in the AppIndicator.

## Steps
1. **Window Setup:**
   - Use `adw::ApplicationWindow`.
   - Use `adw::OverlaySplitView` for the layout.

2. **Sidebar:**
   - Display a list of pinned Resource Groups.
   - Use `gtk::ListBox`.
   - Implement drag-and-drop or simple up/down buttons to reorder them.

3. **Detail View:**
   - Use `adw::ViewStack` or `gtk::Notebook` to create tabs: "Pinned", "Browse", "All Subscriptions".
   - **Pinned Tab:** List individually pinned resources for the selected RG.
   - **Browse Tab:** Live ARM browser. Display search bar and list of live resources from ARM. Add "Pin" toggle buttons next to each.

4. **Settings Dialog:**
   - Use `adw::PreferencesWindow`.
   - Show account info (current identity, tenant).
   - Show subscription toggles.

## Constraints
- Apply `libadwaita` classes (e.g., `.linked`, `.suggested-action`) to match the GNOME HIG.
- Icons should map to standard GNOME Adwaita icons (e.g., `folder-symbolic`, `system-run-symbolic`, `media-playback-start-symbolic`). Map the SF Symbols defined in `AZPIN_SPEC.md` to these equivalents in a `utils/icon_mapper.rs` file.

# Changelog

All notable changes to AzPin are documented in this file.

- Keep the same version in this changelog while changes are still being made on the same branch.
- Do not change the released version until the branch is ready for release.
- Use GitVersioning to derive the app version; do not hardcode version strings in source files.

## [Unreleased]

### CI

- Bumped version to 1.0.x.
- Unified macOS and Windows release assets into a single GitHub release per version (one "latest" flag covers both platforms).
- Added `create-release` job to `release.yml` and `prerelease.yml`; both build jobs now depend on it for version and tag, then upload assets independently.
- Windows MSI renamed to `AzPin-Windows-{version}-Installer.msi` (release) / `AzPin-Windows-{version}-beta-Installer.msi` (beta).
- Windows beta tag unified to `beta-v{version}` (was `beta-win-v{version}`).
- Winget manifest URL updated to match new MSI filename.
- Added full automated Ubuntu builds for both `amd64` and `arm64` architectures.
- Ubuntu artifacts renamed to universal format: `AzPin-Ubuntu-x64-v{version}.deb` and `AzPin-Ubuntu-arm64-v{version}.deb`.

### General

- Added update checker: "Check for Updates" queries the GitHub Releases API (`api.github.com/repos/lfmundim/AzPin/releases/latest`), compares the latest tag against the running version, and shows platform-specific upgrade instructions (`brew upgrade azpin` on macOS, `winget upgrade lfmundim.AzPin` on Windows, or `.deb` direct download on Ubuntu) with a direct link to the release page.

### Ubuntu

- Introduced native GTK4 Linux port built with Rust and `libadwaita`.
- Added dynamic background polling to maintain live azure state in the system tray.
- Settings: Implemented an "Updates" tab using `reqwest` to query the GitHub Releases API and allow one-click browser-based `.deb` updates.
- **Security**: `PermissionsService` now performs real ARM `GET .../providers/Microsoft.Authorization/permissions` checks with wildcard pattern matching (`*`, trailing `/*`, exact). Action buttons (Start/Stop/Restart) only appear for users with confirmed write permissions. Fail-safe: no buttons shown until permissions are verified.
- **Fix**: Token expiry parsing now handles the `az` CLI 2.x datetime format (`"YYYY-MM-DD HH:MM:SS.ffffff"`); tokens are cached for their actual TTL rather than a hardcoded 1-hour fallback.
- **Fix**: All `az` CLI invocations converted to `tokio::process::Command` (async), eliminating blocking I/O on Tokio worker threads.
- **Fix**: `menu()` no longer performs any I/O; account info, permissions, and resource states are all fetched in the async polling loop and read from caches in the synchronous render path.
- **Fix**: Removed shadowed `updated` variable in polling loop — tray now correctly refreshes when resource group contents load, not only when runnable resource state changes.
- **Fix**: Subscriptions never loaded in the main window — `Handle::current()` panicked inside spawned OS threads (no Tokio context), silently killing the loader thread. The Tokio runtime handle is now captured once in `MainWindow::new` and cloned into each thread closure.
- **Fix**: Quit action now uses GTK `app.quit()` via channel instead of `std::process::exit(0)`, ensuring SQLite WAL flush and GTK lifecycle hooks execute cleanly.
- **Fix**: Replaced emoji indicators (`✅`, `⚠️`, `🟢`, `🔴`, `⚪`) with Unicode geometric symbols (`▶`, `■`, `…`, `○`) and plain text per spec.
- **Refactor**: `is_runnable` extracted to `utils/resource_type.rs`; now covers 5 resource types including `microsoft.web/sites/slots`. All inline type-string checks removed.
- **Refactor**: `ResourceState` typed enum replaces raw `String` state storage; `state_cache` is now type-safe end-to-end.
- **Refactor**: Portal URL construction centralized in `utils/portal_url.rs`; no inline `portal.azure.com` strings remain in the UI layer.
- **Refactor**: `icon_mapper.rs` uses exact resource type matching instead of `contains()` substring matching.
- **Model**: `ArmResource` gains optional `tags: Option<HashMap<String, String>>` field.
- **Model**: `PinnedResourceGroup` gains `subscription_display_name: Option<String>`; DB column added automatically via migration on startup.
- **Tests**: Unit tests added for `resource_type`, `portal_url`, `az_cli` expiry parsing, `permissions` wildcard matching, and `token_cache` validity logic.
- **UI**: Tray menu state glyphs moved off resource names onto the action items themselves (`▶ Start`, `■ Stop`, `⟳ Restart`, `… Starting/Stopping`); resource names now render plain.
- **UI**: Pinning a resource group hides the per-resource pin buttons inside it (resources are covered by the group pin); unpinning the group shows them again.
- **UI**: Pin buttons now use the GNOME `view-pin-symbolic` icon instead of `bookmark-new-symbolic`.

### macOS

- Build phase script injects the git tag into `CFBundleShortVersionString` at build time, fixing the update checker always reporting `v0.0.0` as the installed version.
- `UpdateCheckService` (new): `@Observable` service that hits the GitHub Releases API and exposes `.idle / .checking / .upToDate / .updateAvailable / .failed` state.
- Preferences Settings tab: shows current version, "Check for Updates" button, and inline result (up-to-date indicator, update banner with `brew upgrade azpin` command and GitHub release link, or error message with retry).
- Menubar: shows "Update Available: v{latest}" item (opens release URL) whenever `UpdateCheckService.state` is `.updateAvailable`.

### Windows

- `IUpdateCheckService` / `UpdateCheckService` (new): queries GitHub Releases API via named `HttpClient("github")`; returns `UpdateCheckResult` with state, versions, and release URL.
- `SettingsViewModel`: injected with `IUpdateCheckService`; exposes `CheckForUpdatesCommand`, observable update state properties, and current version from the running assembly.
- Settings → Preferences tab: current version row, "Check for Updates" button, inline result panel (up-to-date, update available with `winget upgrade lfmundim.AzPin` and GitHub link, or error).
- Tray menu: "Check for Updates" button; shows result inline (winget command + GitHub link when update available).

### General

- Refreshed app icon (all sizes 16–1024) and menubar/tray icon across macOS and Windows.

### Windows

- Extracted service layer (`Services/`, `Utilities/`, `Models/`, `Data/`, `ViewModels/AuthViewModel`) into `AzPin.Windows.Core` class library; test project now references Core only, eliminating WinUI build target transitive dependency that blocked CI test execution.
- Test CI step re-enabled; `dotnet test` now runs on every push.
- Added `AzPin.Windows.Core`: `ResourceTypeMapper`, `PortalUrl`, `BrowseViewModel`, `ResourceGroupItemViewModel`, `ResourceItemViewModel`, `PinnedResourceItemViewModel`, `IPinService`/`PinService`.
- `TrayMenuViewModel` refactored into Core: quit and open-window actions injected as `Action` delegates, removing WinUI type dependency.
- `ArmResource` now decodes optional `tags` dictionary from ARM responses.
- Main window redesigned: WinUI 3 `NavigationView` (left pane), Mica backdrop, 960×640 minimum.
- Window close button hides the window instead of exiting; tray icon persists.
- BrowsePage: subscription `ComboBox`, live resource group list from ARM, expand/collapse per RG, inline resource list, portal hyperlinks for RGs and resources.
- Pin button on each resource: saves/removes `PinnedResource` in SQLite; state persists across restarts.
- Tray popup: shows flat list of individually-pinned resources (sorted by `DisplayOrder`); clicking opens Azure Portal.
- "Open AzPin..." tray button shows/activates main window (singleton, never recreated).
- `PinService` uses `IDbContextFactory<AzPinDbContext>` for singleton-safe DB access.
- Added tests: `BrowseViewModelTests`, `ResourceGroupItemViewModelTests`, `ResourceItemViewModelTests`, `PinServiceTests`, `PortalUrlTests`, `ResourceTypeMapperTests`, `TrayMenuViewModelTests`.
- Fixed: tray icon unresponsive on startup — removed `TrayHostWindow`, moved `TaskbarIcon` into `MainWindow`, activating main window off-screen (`-32000,-32000`) so shell registers the tray icon without a visible flash.
- Fixed: main window appeared tiny (1×1) on startup — eliminated the dedicated `TrayHostWindow`; main window now opens centered at 960×640 on first show.
- Fixed: right-click context menu never appeared — switched from `TrayPopup` (broken in H.NotifyIcon WinUI v2) to `ContextFlyout` + `MenuFlyout`; menu rebuilds reactively on every `PropertyChanged` event.
- Fixed: `DbUpdateException` on token fetch — `TokenCache` was a Singleton holding a scoped `DbContext` forever; switched to `IDbContextFactory<AzPinDbContext>` so each call opens and disposes its own context.
- Fixed: `SQLite Error 19 NOT NULL constraint failed: CachedTokens.TenantId` — `az account get-access-token` on some environments omits `tenantId`; `TokenCache` now falls back to `string.Empty` instead of passing null to SQLite.
- Fixed: pinning a resource did not update the tray icon list until next app restart — `IPinService` now raises `PinsChanged` after every pin/unpin; `TrayMenuViewModel` subscribes and reloads the pinned list immediately.
- Added: pin button on resource group header rows; clicking pins/unpins the entire RG and reflects the pinned state in the tray.
- Fixed: BrowsePage toolbar (subscription picker + search box) was overlapped by system caption buttons — removed `ExtendsContentIntoTitleBar` so the window uses a standard title bar.
- Fixed: Settings navigation item did nothing — added `SettingsPage` and wired up the nav handler.
- Fixed: tray icon showed the default Windows app icon — regenerated `tray.ico` from the app's branded PNG assets (16×16, 32×32, 256×256).
- Fixed: tray context menu showed no pinned items after pin/unpin — `PropertyChanged` fired on a background thread; context menu rebuild now dispatched to UI thread via `DispatcherQueue.TryEnqueue`.
- Fixed: pinned resource groups were not shown in the tray context menu — `TrayMenuViewModel` now loads both pinned RGs and individual resources; RGs appear above resources in the menu as portal links.
- Fixed: app window and taskbar icon showed the default Windows icon — `AppWindow.SetIcon` now called on startup with the branded `tray.ico`.
- Pinned RGs in tray context menu now show as cascading submenus: live resources (fetched from ARM on startup/pin change) listed inside; runnable resources get a nested Stop/Start/Restart/Open in Portal submenu; "Open Resource Group in Portal" and "Unpin" at the bottom.
- Added `TrayRgViewModel`, `TrayResourceViewModel`, `AppRunningState` to Core; `ResourceTypeMapper.IsRunnable()` helper added.
- Fixed: tray context menu icons not rendering — replaced `FontIcon` + Segoe Fluent Icons (unreliable in WinUI popup context) with `SymbolIcon` (Segoe MDL2 Assets, always available); per-type mapping covers all known resource types.
- Settings page now has three tabs matching the macOS version: **Account** (User, Tenant ID, Active Subscription Name/ID, Refresh Token button), **Subscriptions** (per-subscription visibility toggles; hidden subs excluded from Browse on next load), **Preferences** (Open at Login toggle via registry `HKCU\…\Run`).
- Added `HiddenSubscription` entity, `ISubscriptionSettingsService` / `SubscriptionSettingsService`; `BrowseViewModel` filters hidden subscriptions on load.
- Added `SettingsViewModel`, `SubscriptionItemViewModel` to Core.
- `AuthViewModel` now exposes `ActiveSubscriptionId`.
- DB forward-compat: `HiddenSubscriptions` table created via `CREATE TABLE IF NOT EXISTS` on startup so existing databases upgrade without data loss.

### Menubar

- Runnable resources (App Services, Function Apps, Container Apps, Logic Apps) now show a native chevron submenu containing Stop/Start, Restart, and Open in Portal with SF Symbol glyphs; non-runnable resources remain plain portal-opening buttons.
- Fixed action buttons not appearing for Contributor-role users: replaced `checkAccess` POST (requires Owner-level `Microsoft.Authorization/*/read`) with `GET .../providers/Microsoft.Authorization/permissions`, accessible to Contributors. Correctly handles wildcard actions and `notActions` denial.
- Fixed `.unknown` running state showing a spinner; spinner now only appears during transitional states (starting/stopping/restarting).
- Fixed stale ARM error persisting in menubar RG drawer after a transient failure: error now clears on the next successful resource fetch for that RG.
- Fixed permissions check returning false for custom roles that grant actions via wildcard patterns (e.g. `Microsoft.Web/sites/*`, `Microsoft.Web/*`); ARM RBAC wildcard matching now evaluated correctly.
- Individually-pinned resources (not part of a pinned RG) now show the same submenu as RG resources: runnable+permitted resources get Start/Stop/Restart and Open in Portal; others remain plain portal-open buttons. States and permissions are fetched at startup alongside pinned RG resources.
- RG resource list no longer gets stuck on "Loading..." after pinning a new RG mid-session.

### Main Window

- Fixed: browse view no longer shifts layout when the search field appears; subscription picker and search field now share one toolbar row (picker left, search right), so the content area stays anchored to the top at all times.
- App icon (all macOS sizes 16–1024) added to Assets catalog; AzPin now shows its icon in Finder, the app switcher, and the Dock.
- Dock presence is now dynamic: the Dock icon appears when the main window opens and disappears when it closes (menubar-only sessions show no Dock icon).
- Fixed: clicking "Settings..." in the menubar now brings the Settings window to front.
- Fixed: after completing onboarding (e.g. installing az CLI mid-session), the main browse view now reloads automatically instead of staying stuck on the "CLI not found" error.
- Fixed: Container Apps now use the correct ARM API version (`2023-05-01`) and read `.properties.runningStatus` instead of `.properties.state`. Logic Apps use `2019-05-01` and map "Enabled"/"Disabled" to running/stopped. Container App and Logic App restart is implemented as sequential stop → start (no native restart endpoint exists).
- Added "Open at Login" toggle in Settings → Preferences.
- Added Settings → Subscriptions tab: list all accessible subscriptions with visibility toggles; hidden subscriptions are excluded from the Browse picker on the next load.
- Fixed: hiding the currently-selected subscription now resets the selection to the first visible subscription on the next reload (previously the hidden subscription remained selected and continued loading resource groups).
- Added "Open Terminal..." button in the menubar when not signed in, so `az login` can be run without searching for a terminal.
- On first launch, AzPin automatically opens the main window (and shows the onboarding sheet) when the menubar icon is first clicked, without requiring "Open AzPin..." to be found first.
- Search box at the top of the browse panel filters resource groups by name (case-insensitive, clears on subscription change).
- Resource groups always sorted alphabetically (case-insensitive).
- Resources within a group always sorted by type name (case-insensitive) for consistent ordering across all RGs.
- Browse tab within a selected sidebar RG now shows that RG's live resources (with pin buttons), not the full subscription browser. The subscription browser is shown when no RG is selected.
- Two pinned RGs sharing the same name across different subscriptions show a subscription disambiguator suffix in the menubar label (e.g. "rg-shared · Production").
- `AzureResource` now decodes the optional `tags` dictionary from ARM responses (not displayed yet; foundation for future filtering).

### Windows pre-release feature set (tasks 3.1–3.11)

- First-run onboarding dialog polls every 2 s: CLI installed → signed in → subscription accessible; "Get Started" enables when all three pass. Steps show ProgressRing (checking), green checkmark (pass), or red warning (fail).
- Re-run Setup button in Settings → Preferences resets the onboarding flag and re-shows the onboarding dialog.
- Pinned resource groups stored in SQLite; tray context menu builds cascading RG submenus with live resources fetched from ARM on menu open.
- Running state (Running/Stopped/Unknown) fetched per runnable resource after the resource list loads; transitional states (Fetching/Starting/Stopping/Restarting) show a spinner in the submenu.
- Permissions checked via `GET .../providers/Microsoft.Authorization/permissions` before showing Start/Stop/Restart buttons; fail-safe: no buttons shown if check fails or returns an error.
- Container Apps use API version `2023-05-01` and read `.properties.runningStatus`; Logic Apps use `2019-05-01` and map "Enabled"/"Disabled" to Running/Stopped; restart for both is stop → start sequence.
- Logic App start/stop mapped to ARM "enable"/"disable" actions.
- Main window sidebar shows pinned RGs in a reorderable `ListView` (CanDrag/CanReorder); drag-completed persists `DisplayOrder` via `UpdateDisplayOrderAsync`.
- Two pinned RGs with the same name across different subscriptions show a subscription disambiguator suffix in the sidebar.
- Detail page shows "Pinned" and "Browse" tabs (`SelectorBar`) when a sidebar RG is selected; no-selection placeholder shown otherwise.
- Pinned Resources tab: reorderable list of individually-pinned resources with inline unpin button; drag-completed persists order.
- Browse tab within a selected sidebar RG: live resources fetched from ARM with inline pin/unpin buttons.
- Start/Stop/Restart commands revert state optimistically on ARM failure and surface a 4-second inline error message.
- Action error auto-clears after 4 s via fire-and-forget async method.
- `InternalsVisibleTo` on Core assembly allows tests to call `ArmService.ApiVersion()` and `ArmService.MapAction()` directly.
- Tray context menu disambiguates RG labels when two pinned RGs share the same name across different subscriptions (appends first 8 chars of subscription ID, e.g. "shared-rg · sub-aaaa"). Matches macOS menubar behavior.
- Individually-pinned resources whose parent RG is also pinned are excluded from the flat tray list; they appear inside the RG submenu only, preventing duplicates.
- Added 63 new tests: `ArmServiceTypeAwareTests` (API version selection, action mapping, state parsing per resource type), `TrayResourceViewModelTests` (show-button gating, transitional states, action error/revert), `TrayRgViewModelTests` (error tracking, Fetching→final state flow, permissions propagation), `MainWindowViewModelTests` (sort order, name disambiguation, unpin collection/selection), `PinnedResourcesViewModelTests` (subscription+RG filter, case-insensitive, unpin). All 111 tests pass.
- Resource and resource group names behave as hyperlinks: pointer cursor on hover, underline on hover, click opens in Azure Portal.
- Resource group rows show a rotating chevron for the drawer toggle; chevron click toggles, name click opens Portal.
- RG expand/collapse animates smoothly (switched from List/NSTableView to ScrollView+LazyVStack).
- Loading spinner moved to an inline overlay on the RG row header, eliminating layout shift during animation.
- RG pin button toggles: clicking pin.fill unpins the resource group immediately.
- Resource pin button toggles: clicking pin.fill unpins the individual resource.
- PinnedResourcesView rows show a pin.fill button for inline unpin in addition to the context menu.
- Resource pin button hides immediately when the parent RG is pinned (reactive via @Query, no reload needed).

### Bug Fixes

- Fixed two startup warnings ("Set a .modelContext in view's environment to use Query") caused by MenuBarExtra @Query properties firing before scene-level modelContainer propagated.

---

### Pre-release feature set (tasks 3.1–3.11)

- First-run onboarding sheet polls every 2s for CLI installed → signed in → subscription accessible; "Get Started" enables when all three pass.
- Resource groups can be pinned whole; pinned RG identity stored in SwiftData, resources fetched live from ARM on each menu open.
- MenuBarViewModel introduced: coordinates parallel ARM fan-out for pinned RGs, tracks running state per resource, and per-resource permissions.
- Menubar RG drawer: clicking a pinned RG expands an inline list of its live resources; clicking again collapses it.
- Running state (running/stopped/unknown) shown per runnable resource via ARM property fetch after resource list loads.
- Permissions checked before showing action buttons; fail-safe defaults to no buttons if check fails.
- Start/Stop/Restart: transitional states (starting/stopping/restarting) show spinner; state reverts optimistically on ARM failure.
- AppRunningState extended with .starting, .stopping, .restarting transitional cases.
- Main window sidebar shows pinned RGs; drag-to-reorder persists via SwiftData displayOrder; right-click → Unpin.
- Detail view shows Pinned/Browse tabs when a sidebar RG is selected; no-selection placeholder shown otherwise.
- Pinned Resources tab shows individually-pinned resources for the selected RG, with drag-to-reorder and swipe/context-menu unpin.
- Account Settings tab shows current az identity with Refresh Token and Re-run Setup actions.
- Error indicators shown for RGs whose ARM resource fetch failed; errors tracked per RG, not globally.
- RG context menu ("Open in Portal") on menubar RG button; "Install Azure CLI..." button added to .cliNotInstalled state.
- TokenCacheTests, PermissionsServiceTests, and MenuBarViewModelTests added with in-memory SwiftData and MockURLProtocol stubs.

### MVP (tasks 2.1–2.6)

- Subscription list sorted default-first then by tenantId; isDefault decoded from az account list output.
- modelContainer wired into all SwiftUI scenes; fixes pins not surviving window close and not appearing in menubar.
- Switched to --subscription flag for az account get-access-token for correct cross-tenant/MSA token resolution.
- ShellError conforms to LocalizedError so az CLI stderr surfaces as the error message.
- Pinned resources appear in the menubar sorted by displayOrder with correct SF Symbol icons; clicking opens in Portal.
- ResourceRow pin button saves to SwiftData with duplicate guard; state persists across restarts.
- BrowseView loads real Azure subscriptions, resource groups, and resources inline with SF Symbol icons.
- MenuBarView shows live az CLI auth status.

### POC (tasks 1.1–1.6)

- AuthViewModel with AuthState enum coordinates az CLI auth state for views.
- Services (AzCLIService, TokenCache, ARMService, PermissionsService) and SwiftData container wired into the SwiftUI environment.
- AzJSONDecoder added to parse az CLI date format ("yyyy-MM-dd HH:mm:ss.SSSSSS").
- Added MIT LICENSE, version.json (Nerdbank.GitVersioning), ExportOptions.plist, and release.yml workflow.

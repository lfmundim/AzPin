# Changelog

All notable changes to AzPin are documented in this file.

- Keep the same version in this changelog while changes are still being made on the same branch.
- Do not change the released version until the branch is ready for release.
- Use GitVersioning to derive the app version; do not hardcode version strings in source files.

## [Unreleased]

### Menubar

- Runnable resources (App Services, Function Apps, Container Apps, Logic Apps) now show a native chevron submenu containing Stop/Start, Restart, and Open in Portal with SF Symbol glyphs; non-runnable resources remain plain portal-opening buttons.
- Fixed action buttons not appearing for Contributor-role users: replaced `checkAccess` POST (requires Owner-level `Microsoft.Authorization/*/read`) with `GET .../providers/Microsoft.Authorization/permissions`, accessible to Contributors. Correctly handles wildcard actions and `notActions` denial.
- Fixed `.unknown` running state showing a spinner; spinner now only appears during transitional states (starting/stopping/restarting).
- Fixed stale ARM error persisting in menubar RG drawer after a transient failure: error now clears on the next successful resource fetch for that RG.
- Fixed permissions check returning false for custom roles that grant actions via wildcard patterns (e.g. `Microsoft.Web/sites/*`, `Microsoft.Web/*`); ARM RBAC wildcard matching now evaluated correctly.
- Individually-pinned resources are plain buttons that open in Portal; unpin is done from the main window only.
- RG resource list no longer gets stuck on "Loading..." after pinning a new RG mid-session.

### Main Window

- Fixed: clicking "Settings..." in the menubar now brings the Settings window to front.
- Fixed: after completing onboarding (e.g. installing az CLI mid-session), the main browse view now reloads automatically instead of staying stuck on the "CLI not found" error.
- Fixed: Container Apps now use the correct ARM API version (`2023-05-01`) and read `.properties.runningStatus` instead of `.properties.state`. Logic Apps use `2019-05-01` and map "Enabled"/"Disabled" to running/stopped. Container App and Logic App restart is implemented as sequential stop → start (no native restart endpoint exists).
- Added "Open at Login" toggle in Settings → Preferences.
- Added Settings → Subscriptions tab: list all accessible subscriptions with visibility toggles; hidden subscriptions are excluded from the Browse picker on the next load.
- Added "Open Terminal..." button in the menubar when not signed in, so `az login` can be run without searching for a terminal.
- On first launch, AzPin automatically opens the main window (and shows the onboarding sheet) when the menubar icon is first clicked, without requiring "Open AzPin..." to be found first.
- Search box at the top of the browse panel filters resource groups by name (case-insensitive, clears on subscription change).
- Resource groups always sorted alphabetically (case-insensitive).
- Resources within a group always sorted by type name (case-insensitive) for consistent ordering across all RGs.
- Browse tab within a selected sidebar RG now shows that RG's live resources (with pin buttons), not the full subscription browser. The subscription browser is shown when no RG is selected.
- Two pinned RGs sharing the same name across different subscriptions show a subscription disambiguator suffix in the menubar label (e.g. "rg-shared · Production").
- `AzureResource` now decodes the optional `tags` dictionary from ARM responses (not displayed yet; foundation for future filtering).
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

# Changelog

All notable changes to AzPin are documented in this file.

- Keep the same version in this changelog while changes are still being made on the same branch.
- Do not change the released version until the branch is ready for release.
- Use GitVersioning to derive the app version; do not hardcode version strings in source files.

## [Unreleased]

- Subscription list shows all subscriptions sorted default-first then by tenantId; isDefault field decoded from az account list output.
- modelContainer now wired into all SwiftUI scenes so @Query and modelContext resolve to the persistent store; fixes pins not surviving window close and not appearing in the menubar.
- Resource group rows now toggle: tap expands, tap again collapses.
- Error state clears at the start of each load so a failed subscription does not bleed its error into a subsequently selected working subscription.
- Switched to --subscription flag for az account get-access-token (not --tenant) so cross-tenant guest accounts and MSAs resolve tokens correctly via az CLI internal cache.
- ShellError now conforms to LocalizedError so stderr from az CLI is surfaced as the error message instead of a generic enum description.
- Threaded tenantId from AzureSubscription through BrowseViewModel, ARMService, TokenCache, and AzCLIService for correct multi-tenant token acquisition.
- NSApp.activate added alongside openWindow so "Open AzPin..." brings the main window to the foreground.
- Pinned resources appear in the menubar dropdown, sorted by displayOrder, with correct SF Symbol icons. Clicking opens the resource in Azure Portal via NSWorkspace.
- ResourceRow now has a pin button that saves a PinnedResource to SwiftData. Duplicate check via FetchDescriptor before insert; button shows pin.fill and disables once pinned. State persists across app restarts.
- Tapping a resource group in BrowseView loads its resources inline with correct SF Symbol icons via ResourceTypeMapper. Switching subscription clears stale RG selection and resource list. ResourceRow introduced.
- BrowseView shows resource groups for the selected subscription; selecting a different subscription reloads the list. DetailView now renders BrowseView (temporary scaffolding until task 3.8 tabs). ResourceGroupRow introduced.
- BrowseView now loads real Azure subscriptions via AzCLIService, auto-selects the first, and shows loading/error/empty states. BrowseViewModel introduced as the @Observable backing for BrowseView.
- MenuBarView now shows live az CLI auth status: signed-in account name, "run az login" warning, or CLI not installed.
- AuthViewModel added with AuthState enum — coordinates az CLI auth state for views.
- Services (AzCLIService, TokenCache, ARMService, PermissionsService) and SwiftData container wired into SwiftUI environment at app startup.
- AzJSONDecoder added to correctly parse az CLI date format ("yyyy-MM-dd HH:mm:ss.SSSSSS") — fixes silent decoding failure on token fetch.
- Initial changelog created.
- Added MIT LICENSE.
- Added `version.json` (Nerdbank.GitVersioning config, major=0, minor starts at 1).
- Added `ExportOptions.plist` for Developer ID archive export.
- Added `.github/workflows/release.yml` for tag-triggered build, notarization, and DMG release.
- Added phased development task files (POC / MVP / Pre-release) to `src/macos/tasks/` (gitignored).
- Added `.claude/skills/changelog-update.md` skill for PR-driven changelog updates.

# Changelog

All notable changes to AzPin are documented in this file.

- Keep the same version in this changelog while changes are still being made on the same branch.
- Do not change the released version until the branch is ready for release.
- Use GitVersioning to derive the app version; do not hardcode version strings in source files.

## [Unreleased]

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

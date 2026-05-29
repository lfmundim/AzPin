# Changelog

All notable changes to AzPin are documented in this file.

- Keep the same version in this changelog while changes are still being made on the same branch.
- Do not change the released version until the branch is ready for release.
- Use GitVersioning to derive the app version; do not hardcode version strings in source files.

## [Unreleased]

- Initial changelog created.
- Added MIT LICENSE.
- Added `version.json` (Nerdbank.GitVersioning config, major=0, minor starts at 1).
- Added `ExportOptions.plist` for Developer ID archive export.
- Added `.github/workflows/release.yml` for tag-triggered build, notarization, and DMG release.

# Skill: Release Process

Use this file when cutting a release, updating the Homebrew cask, or debugging the GitHub Actions release workflow. Follow these steps in order — do not skip notarization.

---

## Prerequisites

The following must be set up once and are not repeated per release:

- Apple Developer Program membership (for Developer ID certificate)
- Developer ID Application certificate installed in keychain
- App-specific password created at appleid.apple.com for notarytool
- GitHub repository secrets configured (see below)

### Required GitHub Secrets

| Secret | Value |
|---|---|
| `APPLE_DEVELOPER_ID` | Developer ID Application: Your Name (TEAMID) |
| `APPLE_TEAM_ID` | 10-character Apple Team ID |
| `APPLE_ID` | Apple ID email address |
| `APPLE_APP_SPECIFIC_PASSWORD` | App-specific password from appleid.apple.com |
| `CERTIFICATE_BASE64` | Base64-encoded .p12 export of Developer ID cert |
| `CERTIFICATE_PASSWORD` | Password used when exporting the .p12 |

---

## Versioning

AzPin uses semantic versioning: `MAJOR.MINOR.PATCH`

- `MAJOR` — breaking change to pinned data format or auth model
- `MINOR` — new features (v1.1 multi-sub, v2 environments)
- `PATCH` — bug fixes, ARM API version bumps, UI polish

Version lives in two places — keep them in sync:
1. `AzPin.xcodeproj` → target → General → Version (`MARKETING_VERSION`)
2. The git tag (`v1.0.0`)

Before tagging, bump `MARKETING_VERSION` in the project, commit, then tag.

```bash
# Bump version in Xcode project (or edit project.pbxproj directly)
# Then:
git add AzPin.xcodeproj/project.pbxproj
git commit -m "chore: bump version to 1.0.1"
git tag v1.0.1
git push origin main --tags
```

Pushing the tag triggers the release workflow.

---

## GitHub Actions Workflow

File: `.github/workflows/release.yml`

```yaml
name: Release

on:
  push:
    tags:
      - 'v*.*.*'

jobs:
  build:
    runs-on: macos-15
    steps:
      - uses: actions/checkout@v4

      - name: Import Developer ID Certificate
        run: |
          echo "${{ secrets.CERTIFICATE_BASE64 }}" | base64 --decode > cert.p12
          security create-keychain -p "" build.keychain
          security import cert.p12 -k build.keychain -P "${{ secrets.CERTIFICATE_PASSWORD }}" -T /usr/bin/codesign
          security list-keychains -s build.keychain
          security default-keychain -s build.keychain
          security unlock-keychain -p "" build.keychain
          security set-key-partition-list -S apple-tool:,apple: -s -k "" build.keychain

      - name: Build and Archive
        run: |
          xcodebuild -scheme AzPin \
            -configuration Release \
            -archivePath build/AzPin.xcarchive \
            CODE_SIGN_IDENTITY="${{ secrets.APPLE_DEVELOPER_ID }}" \
            DEVELOPMENT_TEAM="${{ secrets.APPLE_TEAM_ID }}" \
            archive | xcbeautify

      - name: Export App
        run: |
          xcodebuild -exportArchive \
            -archivePath build/AzPin.xcarchive \
            -exportPath build/export \
            -exportOptionsPlist ExportOptions.plist

      - name: Create DMG
        run: |
          brew install create-dmg
          VERSION="${GITHUB_REF_NAME#v}"
          create-dmg \
            --volname "AzPin" \
            --window-pos 200 120 \
            --window-size 600 400 \
            --icon-size 128 \
            --icon "AzPin.app" 150 185 \
            --hide-extension "AzPin.app" \
            --app-drop-link 450 185 \
            --background "Resources/dmg-background.png" \
            "AzPin-${VERSION}.dmg" \
            "build/export/"

      - name: Notarize
        run: |
          VERSION="${GITHUB_REF_NAME#v}"
          xcrun notarytool submit "AzPin-${VERSION}.dmg" \
            --apple-id "${{ secrets.APPLE_ID }}" \
            --password "${{ secrets.APPLE_APP_SPECIFIC_PASSWORD }}" \
            --team-id "${{ secrets.APPLE_TEAM_ID }}" \
            --wait

      - name: Staple
        run: |
          VERSION="${GITHUB_REF_NAME#v}"
          xcrun stapler staple "AzPin-${VERSION}.dmg"

      - name: Upload Release Asset
        uses: softprops/action-gh-release@v2
        with:
          files: "AzPin-*.dmg"
          generate_release_notes: true
```

---

## ExportOptions.plist

Required by `xcodebuild -exportArchive`. Commit this file to the repo root.

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>method</key>
    <string>developer-id</string>
    <key>teamID</key>
    <string>YOUR_TEAM_ID</string>
    <key>signingStyle</key>
    <string>automatic</string>
</dict>
</plist>
```

Replace `YOUR_TEAM_ID` with the actual value. Do not commit real secrets here — Team ID is not a secret.

---

## Updating the Homebrew Cask

After a successful release, the cask SHA256 must be updated manually (or via a follow-up workflow step).

### Manual Steps

```bash
# Download the new DMG
curl -L -o AzPin-1.0.1.dmg \
  https://github.com/{owner}/azpin/releases/download/v1.0.1/AzPin-1.0.1.dmg

# Compute SHA256
shasum -a 256 AzPin-1.0.1.dmg
```

Update `homebrew-tap/Casks/azpin.rb`:
```ruby
cask "azpin" do
  version "1.0.1"                          # ← bump
  sha256 "abc123..."                        # ← new hash

  url "https://github.com/{owner}/azpin/releases/download/v#{version}/AzPin-#{version}.dmg"
  ...
end
```

Commit and push to the `homebrew-tap` repo. Homebrew picks up the change automatically for users who run `brew upgrade`.

### Automating Cask Updates (optional, future)

Add a second job to the release workflow that:
1. Computes SHA256 of the uploaded DMG
2. Clones `homebrew-tap`
3. Patches `azpin.rb` with `sed`
4. Commits and pushes via a GitHub token

Not implemented in v1 — manual update is fine for low release cadence.

---

## Local Release Build (without CI)

For testing the full release pipeline locally:

```bash
# Archive
xcodebuild -scheme AzPin -configuration Release \
  -archivePath build/AzPin.xcarchive archive

# Export
xcodebuild -exportArchive \
  -archivePath build/AzPin.xcarchive \
  -exportPath build/export \
  -exportOptionsPlist ExportOptions.plist

# Create DMG
create-dmg \
  --volname "AzPin" \
  --window-size 600 400 \
  --icon-size 128 \
  --icon "AzPin.app" 150 185 \
  --app-drop-link 450 185 \
  "AzPin-local.dmg" \
  "build/export/"

# Notarize
xcrun notarytool submit AzPin-local.dmg \
  --apple-id "your@email.com" \
  --password "xxxx-xxxx-xxxx-xxxx" \
  --team-id "YOURTEAMID" \
  --wait

# Staple
xcrun stapler staple AzPin-local.dmg
```

---

## Checklist Before Tagging

- [ ] Version bumped in `MARKETING_VERSION` and committed
- [ ] `CHANGELOG.md` updated (if maintained)
- [ ] All tests passing locally (`xcodebuild test`)
- [ ] No SwiftFormat violations (`swiftformat --lint .`)
- [ ] Tested on a clean Tahoe install (or VM snapshot)
- [ ] `ExportOptions.plist` is committed and correct
- [ ] `Resources/dmg-background.png` is present in repo

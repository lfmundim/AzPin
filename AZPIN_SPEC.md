# AzPin — Project Specification

> **Handoff document for Claude Code.**
> This is a reference spec, not a prompt. Read it fully before writing any code.
> All decisions documented here are final unless explicitly marked as open.

---

## Overview

**AzPin** is a native macOS menubar application that reads the user's existing `az` CLI session and provides fast, pinnable access to Azure resources. The primary workflow is: open menubar → see pinned resource groups and their resources → click to open in browser, or act on runnable resources (start/stop/restart) without leaving the desktop.

It is a **personal developer tool**, open source, free, with no paid dependencies of any kind.

---

## License

**MIT License.**

Rationale: simpler than Apache 2.0, no patent clauses, maximum compatibility with other open source tooling, and standard for developer tools of this nature. Include a `LICENSE` file at the repo root with the standard MIT text. Every source file does not need a header — a root `LICENSE` file is sufficient.

**Hard constraint: zero paid libraries, frameworks, SDKs, or services.** This includes no Azure SDK for Swift (use raw ARM REST API via `URLSession`), no paid fonts, no paid icon sets, and no paid CI/CD beyond what GitHub provides for free.

---

## Platform Requirements

| Property | Value |
|---|---|
| Minimum macOS | **26.0 (Tahoe)** |
| Target macOS | 26.5+ |
| Architecture | Universal binary (arm64 + x86_64) |
| Xcode version | 26+ (required for Liquid Glass / Tahoe APIs) |
| Swift version | 6.x |
| No `#available` guards needed | Tahoe-only, no backward compat |

Tahoe is the correct floor. No Sequoia fallback. No `#available(macOS 26, *)` guards — the entire codebase assumes Tahoe.

---

## Development Environment

The developer uses **VSCode** for day-to-day Swift editing (Swift extension + SourceKit-LSP) and **Xcode CLI tools** for building, archiving, and notarization. Full Xcode.app is only needed for asset catalog editing.

Recommended shell tools:
```bash
xcode-select --install   # CLI tools
brew install xcbeautify  # prettier xcodebuild output
brew install swiftformat # code formatting
```

The project must be buildable entirely from the command line:
```bash
xcodebuild -scheme AzPin -configuration Release -arch arm64 -arch x86_64 archive
```

---

## App Architecture

### App Mode

AzPin is a **menubar-only app** with a secondary full window for management. It has no Dock icon.

In `Info.plist`:
```xml
<key>LSUIElement</key>
<true/>
```

In the SwiftUI app entry point:
```swift
@main
struct AzPinApp: App {
    var body: some Scene {
        MenuBarExtra("AzPin", systemImage: "cloud.fill") {
            MenuBarView()
        }
        .menuBarExtraStyle(.menu)

        Window("AzPin", id: "main") {
            MainAppView()
        }
        .windowStyle(.titleBar)
        .defaultSize(width: 900, height: 600)

        Settings {
            SettingsView()
        }
    }
}
```

The menubar icon uses `.renderingMode(.template)` so it adapts correctly to light/dark mode and the Tahoe transparent menu bar.

### Scene Summary

| Scene | Type | Purpose |
|---|---|---|
| `MenuBarExtra` | `.menu` style | Primary interaction surface |
| `Window("main")` | Standard window | Browse + manage pinned resources |
| `Settings` | Settings scene | Account info, preferences |

---

## Design System

### Principle

The app must feel like it was made by Apple. Every visual and interaction decision should defer to the platform. No custom chrome, no custom colors, no custom animations beyond what SwiftUI provides.

### Liquid Glass

Recompiling with Xcode 26 gives Liquid Glass automatically to: toolbar, sidebar, menu bar, window controls, `NSPopover`, sheets. No manual `.glassEffect()` calls are needed for standard navigation structure.

Apply `.glassEffect()` only to:
- Floating action buttons (if any)
- Custom popovers outside standard SwiftUI sheet flow

Never apply `.glassEffect()` to:
- List rows
- Content cards
- Scrollable areas
- Backgrounds

### Colors

Zero hardcoded hex colors. Only semantic SwiftUI colors:
- `.primary`, `.secondary` for text
- `Color.accentColor` for interactive elements
- `Color.green` for running state
- `Color.red` for stopped state / destructive
- `Color.orange` for loading / transitional state

These adapt automatically to light mode, dark mode, and user accent color preferences.

### Typography

System font only. Use `.font(.body)`, `.font(.caption)`, `.font(.headline)` etc. Never specify a custom font family.

### Icons: SF Symbols

All icons are SF Symbols. No external icon library. No emoji.

#### Resource Type Icons

| Resource Type | SF Symbol |
|---|---|
| Resource Group | `folder.fill` |
| Function App | `bolt.fill` |
| App Service | `globe` |
| App Insights | `lightbulb.fill` |
| Storage Account | `externaldrive.fill` |
| Service Bus | `arrow.triangle.branch` |
| Key Vault | `key.fill` |
| API Management | `antenna.radiowaves.left.and.right` |
| SQL / CosmosDB | `cylinder.fill` |
| Container App | `shippingbox.fill` |
| Unknown / Other | `cloud.fill` |

#### Action Icons

| Action | SF Symbol | Color tint |
|---|---|---|
| Start | `play.fill` | `.green` |
| Stop | `stop.fill` | `.red` |
| Restart | `arrow.clockwise` | `.primary` |
| Loading / transitioning | `progress.indicator` (animated) | `.orange` |

#### App / Menubar Icon

`cloud.fill` as the menubar `systemImage`. Final app icon to be designed separately in Icon Composer (Xcode 26 tool) — this is not blocking development.

---

## Data Model (SwiftData)

Use SwiftData for persistence. No CoreData, no SQLite directly, no UserDefaults for structured data.

### Models

```swift
@Model
class PinnedResourceGroup {
    var id: String            // ARM resource group ID
    var subscriptionId: String
    var name: String
    var displayOrder: Int
    var resources: [PinnedResource]
}

@Model
class PinnedResource {
    var id: String            // Full ARM resource ID
    var name: String
    var type: String          // e.g. "Microsoft.Web/sites"
    var resourceGroup: String
    var subscriptionId: String
    var location: String
    var displayOrder: Int
}

@Model
class CachedToken {
    var subscriptionId: String
    var tenantId: String
    var accessToken: String
    var expiresOn: Date
}
```

`PinnedResourceGroup` contains an ordered list of `PinnedResource`. Both are reorderable by the user.

---

## Authentication

### Strategy

AzPin piggybacks on the existing `az` CLI session. No separate login UI, no OAuth flow, no browser redirect. If the user is logged in with `az login`, AzPin works. If not, it tells them to run `az login` in terminal.

### Token Acquisition

Shell out to the `az` CLI:

```swift
func fetchToken(subscription: String) async throws -> AzureToken {
    let result = try await shell("az account get-access-token --subscription \(subscription) --output json")
    let decoded = try JSONDecoder().decode(AzureTokenResponse.self, from: result)
    return decoded
}
```

The `az` binary is typically at `/usr/local/bin/az` or `/opt/homebrew/bin/az`. Resolve it dynamically:
```swift
func resolveAzPath() -> String {
    for path in ["/usr/local/bin/az", "/opt/homebrew/bin/az", "/usr/bin/az"] {
        if FileManager.default.fileExists(atPath: path) { return path }
    }
    return "az" // fallback to PATH
}
```

### Token Caching

- Store token in SwiftData (`CachedToken` model)
- On each ARM API call, check if `expiresOn > Date.now + 5 minutes`
- If valid, use cached token
- If expired or missing, re-fetch via `az` CLI and update cache
- One `CachedToken` per subscription

### Auth State in Menubar

If no valid token can be obtained:
- Show `⚠️ Not signed in` at top of menu
- Show `Run 'az login' in Terminal` as a disabled menu item
- Optionally: a `Open Terminal` menu item that launches Terminal.app

---

## Azure ARM API Integration

### Base URL

```
https://management.azure.com
```

All requests use `URLSession` with `Bearer {token}` in the `Authorization` header. No Azure SDK. No third-party HTTP libraries.

### Endpoints Used

#### List Subscriptions
```
GET /subscriptions?api-version=2022-12-01
```

#### List Resource Groups
```
GET /subscriptions/{subscriptionId}/resourcegroups?api-version=2021-04-01
```

#### List Resources in a Resource Group
```
GET /subscriptions/{subscriptionId}/resourceGroups/{rgName}/resources?api-version=2021-04-01
```

Response includes `id`, `name`, `type`, `location`, `tags`.

#### Get App Service / Function App State
```
GET /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Web/sites/{name}?api-version=2023-01-01
```

Response `.properties.state` is `"Running"` or `"Stopped"`.

#### Start App Service / Function App
```
POST /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Web/sites/{name}/start?api-version=2023-01-01
```

#### Stop App Service / Function App
```
POST /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Web/sites/{name}/stop?api-version=2023-01-01
```

#### Restart App Service / Function App
```
POST /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Web/sites/{name}/restart?api-version=2023-01-01
```

#### Check User Permissions on a Resource
```
POST /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Authorization/permissions?api-version=2022-04-01
```

Or use the `checkAccess` endpoint to verify if the current identity has `Microsoft.Web/sites/start/action`, `stop/action`, `restart/action` on a given resource. Only show action buttons if this returns true.

### Runnable Resource Types

Only these types get start/stop/restart buttons:

```swift
let runnableTypes: Set<String> = [
    "microsoft.web/sites",             // App Service & Function Apps
    "microsoft.web/sites/slots",       // Deployment slots
    "microsoft.app/containerapps",     // Container Apps
    "microsoft.logic/workflows"        // Logic Apps (Standard)
]
```

Comparison should be case-insensitive (ARM returns mixed casing).

---

## Portal URL Construction

Every Azure resource has a deterministic portal URL. Construct it from the ARM resource ID:

```swift
func portalURL(for resourceId: String) -> URL {
    let encoded = resourceId.addingPercentEncoding(withAllowedCharacters: .urlPathAllowed) ?? resourceId
    return URL(string: "https://portal.azure.com/#resource\(encoded)")!
}
```

For resource groups:
```swift
func portalURLForResourceGroup(subscriptionId: String, rgName: String) -> URL {
    let path = "/subscriptions/\(subscriptionId)/resourceGroups/\(rgName)"
    return URL(string: "https://portal.azure.com/#resource\(path)")!
}
```

Opening in browser:
```swift
NSWorkspace.shared.open(url)
```

---

## Menubar Menu Structure

The menubar menu is built with native SwiftUI menu content inside `MenuBarExtra`. Structure:

```
[cloud.fill icon] ▼
─────────────────────────────────────
[auth status row]
  ✅ user@tenant (subscription name)    ← if signed in
  ⚠️ Not signed in — run 'az login'    ← if not
─────────────────────────────────────
📁 rg-production
    ⚡ func-licensing-api   [▶ or ⏹]  [↺]
    🌐 app-licensing-portal [▶ or ⏹]  [↺]
    🗄️ st-licensingdata
    🔑 kv-shared-keys
─────────────────────────────────────
📁 rg-shared
    📡 apim-gs1us
    💡 ai-insights-prod
─────────────────────────────────────
Pin Resource Group...
─────────────────────────────────────
Open AzPin...
Quit AzPin
```

### Menu Item Behavior

- Clicking a resource name → open portal URL in default browser
- Clicking a resource group name → toggles the drawer open/closed. The entire RG row is a single tap target for disclosure; there is no separate navigation action on the row itself.
- Navigating to a resource group in the portal is exposed two ways:
  1. **"Open Resource Group" item** — a dedicated menu item at the bottom of the drawer, separated by a `Divider()`, with a `arrow.up.forward` SF Symbol. Always present when the drawer is open.
  2. **Right-click / secondary click context menu** on the RG row — shows "Open in Portal" as a `.contextMenu` modifier action. This is the power-user shortcut and works whether the drawer is open or closed.
- Both trigger the same portal URL: `portal.azure.com/#resource/subscriptions/{sub}/resourceGroups/{name}`
- Action buttons (▶ ⏹ ↺) are inline, right-aligned in the row
- Action buttons only shown if: resource is a runnable type AND user has permission
- State (Running/Stopped) is fetched **when the menu opens**, not on a background timer
- During state fetch, show `progress.indicator` in place of action buttons
- After an action (e.g. stop), immediately switch button to loading state, then update to new state once ARM confirms

### State Machine for Action Buttons

```
Unknown → [menu opens] → Fetching → Running | Stopped
Running → [tap ⏹] → Stopping → Stopped
Running → [tap ↺] → Restarting → Running
Stopped → [tap ▶] → Starting → Running
Any transitional state → show progress.indicator, disable buttons
```

---

## Full App Window

Opened via "Open AzPin..." menu item, or `@Environment(\.openWindow)` with id `"main"`.

### Layout

`NavigationSplitView` with:
- **Sidebar**: list of pinned resource groups (reorderable)
- **Detail**: resources within selected resource group

### Tabs / Sections in Detail

Use a `TabView` or segmented `Picker` in the detail area:

1. **Pinned** — shows currently pinned resources for the selected RG, reorderable, removable
2. **Browse** — search/browse all resources in the selected RG from ARM live data, with Pin buttons
3. **All Subscriptions** — top-level browse to add new resource groups to pin

### Browse Flow (All Subscriptions tab)

```
[Subscription picker ▾]
[Search field: filter by name]
─────────────────────────────
📁 rg-production          [Pin RG]
   ⚡ func-licensing-api  [Pin Resource]
   🌐 app-portal          [Pin Resource]
📁 rg-dev
   ...
```

Pinning a whole RG auto-pins all its resources and keeps them live (new resources in that RG appear automatically on next refresh).

Pinning an individual resource adds only that resource, even if its parent RG is not pinned.

### Settings Window

Standard macOS Settings scene (`@Environment(\.openSettings)`).

Sections:
- **Account**: displays current `az` identity, tenant, active subscription; button to refresh token
- **Subscriptions**: list of accessible subscriptions, select which one(s) to show in Browse
- **Preferences**: (v1 stubs, not functional) polling interval toggle (off by default)

---

## Resource Group Pinning vs Resource Pinning

Two distinct pin modes:

| Mode | Behavior |
|---|---|
| Pin entire RG | All current + future resources in that RG appear in the menu. ARM is queried for resources on each menu open. |
| Pin individual resource | Only that specific resource appears. Persisted by ARM resource ID. |

Both can coexist. A resource that is individually pinned and whose parent RG is also pinned should only appear once in the menu (deduplication by resource ID).

Display order in menu: pinned RGs first (in user-defined order), then individually pinned resources not belonging to a pinned RG grouped under a separator.

---

## Refresh / Data Fetching Strategy

- **On menu open**: fetch resource states (Running/Stopped) for all runnable resources in pinned RGs. Fetch resource list for RG-pinned groups (to catch new resources).
- **No background polling** in v1.
- **Token refresh**: checked before every ARM call, re-fetched from `az` CLI if within 5 minutes of expiry.
- **Errors**: if an ARM call fails (network, permissions), show the resource name with a `⚠️` suffix and a tooltip/secondary text explaining the issue. Do not crash or hide the resource.

---

## Permissions Check

Before showing action buttons, verify the user has the necessary permissions. Use ARM `checkAccess`:

```
POST https://management.azure.com/{resourceId}/providers/Microsoft.Authorization/checkAccess?api-version=2022-04-01
Body: { "actions": ["Microsoft.Web/sites/start/action", "Microsoft.Web/sites/stop/action"] }
```

Cache permissions check result per resource per session (only re-check when token is refreshed). If the permissions call itself fails, default to **not showing** the action buttons (fail safe).

---

## Project Structure

```
AzPin/
├── AzPin.xcodeproj/
├── AzPin/
│   ├── AzPinApp.swift              # App entry point, scene declarations
│   ├── Info.plist                  # LSUIElement = true
│   │
│   ├── MenuBar/
│   │   ├── MenuBarView.swift       # Root MenuBarExtra content
│   │   ├── ResourceGroupMenuItem.swift
│   │   ├── ResourceMenuItem.swift
│   │   └── AuthStatusView.swift
│   │
│   ├── MainWindow/
│   │   ├── MainAppView.swift       # NavigationSplitView root
│   │   ├── SidebarView.swift       # Pinned RG list
│   │   ├── DetailView.swift        # Tabbed detail area
│   │   ├── BrowseView.swift        # Live ARM browser
│   │   └── PinnedResourcesView.swift
│   │
│   ├── Settings/
│   │   └── SettingsView.swift
│   │
│   ├── Services/
│   │   ├── AzCLIService.swift      # Shells out to az CLI
│   │   ├── ARMService.swift        # URLSession ARM REST calls
│   │   ├── TokenCache.swift        # Token cache logic
│   │   └── PermissionsService.swift
│   │
│   ├── Models/
│   │   ├── AzureResource.swift     # Decodable ARM response structs
│   │   ├── PinnedResourceGroup.swift  # SwiftData model
│   │   ├── PinnedResource.swift       # SwiftData model
│   │   └── CachedToken.swift          # SwiftData model
│   │
│   └── Utilities/
│       ├── PortalURL.swift         # portal.azure.com URL builder
│       ├── ResourceTypeMapper.swift # type string → SF Symbol name
│       └── ShellRunner.swift       # async Process wrapper
│
├── LICENSE                         # MIT
├── README.md
└── .github/
    └── workflows/
        └── release.yml             # Build, notarize, publish DMG
```

---

## Distribution

### GitHub Releases

Build a universal DMG on every tagged release.

GitHub Actions workflow (`.github/workflows/release.yml`) triggered by `push` to tags matching `v*.*.*`:

Steps:
1. `xcodebuild archive` with `CODE_SIGN_IDENTITY` and `DEVELOPMENT_TEAM` from GitHub secrets
2. `xcodebuild -exportArchive` to produce `.app`
3. Create universal binary via `-arch arm64 -arch x86_64` (or use `lipo` post-build)
4. Package `.app` into `.dmg` using `hdiutil`
5. Submit to Apple notarization: `xcrun notarytool submit` with Apple ID credentials from GitHub secrets
6. Staple: `xcrun stapler staple`
7. Upload `.dmg` as GitHub release asset

Required GitHub secrets:
- `APPLE_DEVELOPER_ID` — Developer ID certificate identity string
- `APPLE_TEAM_ID`
- `APPLE_ID` — Apple ID email for notarytool
- `APPLE_APP_SPECIFIC_PASSWORD` — App-specific password for notarytool

### Homebrew Cask (own tap)

Repo: `{owner}/homebrew-tap`

Cask file `Casks/azpin.rb`:
```ruby
cask "azpin" do
  version "1.0.0"
  sha256 "REPLACE_WITH_SHA256_OF_DMG"

  url "https://github.com/{owner}/azpin/releases/download/v#{version}/AzPin-#{version}.dmg"

  name "AzPin"
  desc "Azure resource launcher for the macOS menubar"
  homepage "https://github.com/{owner}/azpin"

  app "AzPin.app"

  zap trash: [
    "~/Library/Application Support/AzPin",
    "~/Library/Containers/com.{owner}.azpin",
  ]
end
```

Install instructions for users:
```bash
brew tap {owner}/tap
brew install --cask azpin
```

### Notarization Requirement

Required for Gatekeeper on all macOS versions. Without notarization, users get the "unverified developer" warning and must manually override via System Settings. An Apple Developer Program membership ($99/yr) is required for the Developer ID certificate used in signing and notarization.

**App Sandbox: OFF.** The app must shell out to `az` CLI, which is incompatible with the sandbox. Since it is not distributed via the Mac App Store, sandbox is not required.

---

## README Minimum Content

The `README.md` should cover:
- What AzPin is (one paragraph)
- Screenshot or GIF of menubar in action
- Prerequisites: macOS 26 Tahoe, Azure CLI installed and `az login` completed
- Install via Homebrew (primary method)
- Install via DMG (manual fallback)
- How pinning works (RG vs individual resource)
- Building from source instructions
- Contributing section
- License (MIT)

---

## First-Run Onboarding

On first launch (gated by `hasCompletedOnboarding: Bool` in `UserDefaults`), present a sheet over the main window (or as a standalone window if no main window has been opened yet) that walks the user through the three prerequisites in order.

### Onboarding Steps

```
Step 1: Azure CLI detected        ⏳ → ✅
Step 2: Signed in (az login)      ⏳ → ✅
Step 3: Subscription accessible   ⏳ → ✅

[Get Started]  ← enabled only when all three are ✅
```

Each step is checked **actively while the sheet is open**, polling every 2–3 seconds via a `Timer` or `AsyncStream`. The user does not need to tap "check" — steps resolve automatically as they complete actions in their terminal.

### Step Logic

| Step | Check | Pass condition |
|---|---|---|
| 1. CLI detected | `FileManager.default.fileExists` at known `az` paths | Any path resolves |
| 2. Signed in | `az account show --output json` exits 0 | Non-empty JSON with `user` field |
| 3. Subscription accessible | `az account list --output json` exits 0 | At least one subscription in list |

If a step fails (e.g. CLI not found), show an inline help text under that step with the action required:
- CLI not found → "Install the Azure CLI: [aka.ms/installazureclimacos]"
- Not signed in → "Run `az login` in your terminal"
- No subscriptions → "Ensure your account has access to at least one Azure subscription"

### Completion

Once all three steps are ✅, the "Get Started" button activates. Tapping it sets `hasCompletedOnboarding = true` in UserDefaults, dismisses the sheet, and opens the Browse tab so the user can immediately pin their first resource group.

The onboarding sheet is never shown again after completion. It can be re-triggered manually from Settings → Account → "Re-run setup" for troubleshooting.

---

## DMG User Experience

### Tooling

Use `create-dmg` (open source, `brew install create-dmg`) instead of raw `hdiutil`. It handles window sizing, background image, icon layout, and symlink to `/Applications` cleanly.

Basic invocation in the release workflow:
```bash
create-dmg \
  --volname "AzPin" \
  --volicon "AzPin/Assets.xcassets/AppIcon.appiconset/icon_512x512.png" \
  --window-pos 200 120 \
  --window-size 600 400 \
  --icon-size 128 \
  --icon "AzPin.app" 150 185 \
  --hide-extension "AzPin.app" \
  --app-drop-link 450 185 \
  --background "Resources/dmg-background.png" \
  "AzPin-$VERSION.dmg" \
  "build/export/"
```

Include a `Resources/dmg-background.png` (1200×800px @2x) with a simple drag arrow graphic. This is a static asset in the repo.

### Post-Install Eject Prompt

After the user drags `AzPin.app` to `/Applications` and closes the DMG window, prompt them to eject and delete the DMG file.

This is implemented as an **AppleScript** embedded in the DMG via a background app or `DS_Store` hook. The standard approach with `create-dmg` is to include a small background AppleScript application that fires on window close:

```applescript
on run
    tell application "Finder"
        set dmgVolume to (every disk whose name is "AzPin")
        if (count of dmgVolume) > 0 then
            set result to display dialog "AzPin has been installed. Would you like to eject and delete the installer?" buttons {"Keep", "Eject & Delete"} default button "Eject & Delete"
            if button returned of result is "Eject & Delete" then
                eject item 1 of dmgVolume
                -- deletion of the .dmg source file requires knowing its path
                -- instruct the user to move to trash manually if path is unknown
                display dialog "You can now delete the AzPin .dmg file from your Downloads folder." buttons {"OK"} default button "OK"
            end if
        end if
    end tell
end run
```

**Note:** Automatically deleting the source `.dmg` from disk is not reliably possible from within the DMG itself (the script doesn't know where the user stored it). The prompt should offer to eject the mounted volume and remind the user to delete the `.dmg` from Downloads (or wherever they saved it). This is standard practice for DMG installers.

---

## Open Decisions (to resolve during development)

1. **Multi-subscription display**: menubar shows RG names only (flat list, user-defined order). No subscription prefix in labels. Exception: if two pinned RGs share the same name across different subscriptions, append a short disambiguator as secondary text (e.g. the subscription display name in `.caption` size below the RG name). This case is rare but must not silently break.

2. **Resource Group live-sync scope**: when a whole RG is pinned, resources are fetched on menu open. If a resource was deleted in Azure since last open, it should be silently removed from the menu (ARM returns 404 or it's absent from the list response). No user action needed.

3. **Container App start/stop API**: the ARM API path for Container Apps is different from App Service. Verify the correct endpoint during implementation — may require a separate method in `ARMService`.

4. **App Insights actions**: App Insights has no start/stop concept. It should appear in the menu with a click-to-open behavior only, no action buttons, regardless of permissions.

5. **`az` CLI not found**: if `az` is not installed at all, show a persistent warning in the menubar menu with a link to `https://aka.ms/installazureclimacos`. Do not silently fail.

---

## Future Versions (Not In Scope for v1)

### v1.1 — Multi-Subscription Pinning

Allow pinning RGs from different subscriptions simultaneously without switching the active `az` context.

**How it works:** `az account get-access-token --subscription {id}` accepts any subscription ID the logged-in identity has access to, without modifying the active context. Each subscription gets its own cached token (already modeled in `CachedToken.subscriptionId`).

**Data model addition:**
```swift
// Add to PinnedResourceGroup:
var subscriptionDisplayName: String  // resolved once at pin time, stored
```

**Token resolution:** `ARMService.token(for subscriptionId:)` — already the natural shape given per-subscription token cache.

**ARM calls:** fan out in parallel via `TaskGroup` across all unique subscription IDs represented in the pinned list.

**Menu display:** flat list, RG name only. Subscription name only shown as secondary `.caption` text if two pinned RGs share the same name (collision detection by name equality across different `subscriptionId` values).

### v2 — Multi-Environment / Multi-Tenant

Support for multiple Azure environments (e.g. "Work" and "Personal") each backed by a different tenant.

**How it works:** `az login --tenant {tenantId}` supports multiple concurrent tenant sessions. `az account get-access-token --tenant {tenantId}` fetches a token for a specific tenant without affecting the active session.

**Concept:** Named "Environments" (user-defined labels like "Work" or "Personal"), each with:
- A tenant ID
- A default subscription list
- Their own set of pinned resource groups

**UX:** A switcher in the menubar (top of the dropdown, above pinned resources) or a persistent split. Switching environment reloads the pinned resources for that environment. Pinned data is stored per-environment.

**Scope note:** This is a meaningful data model change (environments as a first-class entity wrapping pinned RGs). Design the v1.1 data model with a nullable `environmentId` foreign key on `PinnedResourceGroup` so v2 migration is additive rather than destructive.

---

## Out of Scope for v1

- Resource metrics, charts, or monitoring
- Log streaming
- Creating, deleting, or modifying resource configuration
- AKS cluster management
- Virtual machine start/stop (deliberate — too high-risk for a menubar button)
- Notifications or alerts
- Multiple concurrent subscriptions (v1.1)
- Multiple tenants / environments (v2)
- Background polling (stubbed in Settings but not implemented)
- App Store distribution
- iOS / iPadOS version

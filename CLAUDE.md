# AzPin — Claude Code Context

AzPin reads the user's existing `az` CLI session and provides fast, pinnable access to Azure resources. Click to open in browser, start/stop/restart runnable resources inline. No Azure SDK. No paid dependencies.

Two native implementations live in this repo:
- **macOS** — Swift/SwiftUI menubar app (`AzPin/`)
- **Windows** — WinUI 3 tray app (`src/windows/`)

Full spec: `AZPIN_SPEC.md`

---

## Platform

### macOS

- **macOS 26.0 (Tahoe) minimum. No exceptions.**
- No `#available` guards. The entire codebase assumes Tahoe.
- Universal binary (arm64 + x86_64).
- Swift 6.x, Xcode 26+.

### Windows

- **Windows 11 22H2 (build 10.0.22621.0) minimum.**
- WinUI 3 (Windows App SDK 1.6+), **unpackaged** — no MSIX, no Store packaging.
- .NET 9, C#, target framework `net9.0-windows10.0.22621.0`.
- x64 and arm64.

---

## Build Commands

### macOS

```bash
# Debug build
xcodebuild -scheme AzPin -configuration Debug build | xcbeautify

# Release archive
xcodebuild -scheme AzPin -configuration Release \
  -archivePath build/AzPin.xcarchive archive | xcbeautify

# Export .app from archive
xcodebuild -exportArchive \
  -archivePath build/AzPin.xcarchive \
  -exportPath build/export \
  -exportOptionsPlist ExportOptions.plist

# Run tests
xcodebuild -scheme AzPin -configuration Debug test | xcbeautify

# Format
swiftformat .
```

### Windows

```powershell
# Restore
dotnet restore src/windows/AzPin.Windows/AzPin.Windows.csproj -p:Platform=x64

# Debug build
dotnet build src/windows/AzPin.Windows/AzPin.Windows.csproj -c Debug -p:Platform=x64

# Run tests
dotnet test src/windows/AzPin.Windows.sln

# Self-contained release bundle
dotnet publish src/windows/AzPin.Windows/AzPin.Windows.csproj `
  -c Release -r win-x64 -p:Platform=x64 `
  -p:WindowsAppSDKSelfContained=true --self-contained true `
  -o build/publish/win-x64
```

---

## Hard Constraints

These are non-negotiable for both platforms unless noted.

### Shared

- **No Azure SDK.** All ARM calls via `URLSession` (macOS) or `HttpClient` (Windows) only.
- **No paid dependencies.** Zero. No SPM or NuGet packages with commercial licenses.
- **No hardcoded colors.** Platform-semantic colors only (see per-platform rules below).
- **No custom fonts.** System font only.
- **No emoji in UI.** SF Symbols (macOS) or Segoe Fluent Icons / WinUI built-ins (Windows) only.
- **Every testable unit of code must ship with tests.** All happy paths and all mapped error/sad paths. No exceptions for service layer code. See `.claude/skills/testing-approach.md`.

### macOS-only

- **No `#available` guards.** Tahoe is the floor.
- **App Sandbox is OFF.** Required for `az` CLI shell access.
- **No `DispatchQueue.main.async`.** Use `@MainActor` instead.
- **Semantic SwiftUI colors only:** `.primary`, `.secondary`, `Color.green`, `Color.red`, `Color.orange`, `Color.accentColor`.

### Windows-only

- **No MSIX / no AppxManifest-gated APIs.** App is unpackaged.
- **No taskbar button** while the main window is hidden.
- **MVVM throughout.** `CommunityToolkit.Mvvm` for all ViewModels.
- **`ResourceDictionary` theme brushes only.** No hardcoded hex colors. Use `Application.Current.RequestedTheme` for theme detection.
- **Segoe UI Variable only.** Windows 11 system font, used by default — do not specify a font family.
- **No Moq or paid test frameworks.** Hand-write test doubles.

---

## Architecture Rules

### macOS Service Boundaries

- `ShellRunner.swift` is the **only** place that instantiates `Process`. Nothing else shells out directly.
- `TokenCache.swift` is the **only** place that handles token expiry and refresh. Callers call `token(for:)` and get a valid token back or an error — they never check expiry themselves.
- `ARMService.swift` is the **only** place that makes `URLSession` calls to `management.azure.com`. Views and ViewModels never call ARM directly.
- `PermissionsService.swift` is the **only** place that calls the ARM `checkAccess` endpoint.
- `PortalURL.swift` is the **only** place that constructs `portal.azure.com` URLs.

### Windows Service Boundaries

- `AzCliService.cs` is the **only** place that spawns `az` processes.
- `TokenCache.cs` is the **only** class that reads/writes `CachedToken` in the DB and handles expiry logic. Callers call `GetTokenAsync` and get a valid token or an exception.
- `ArmService.cs` is the **only** class that calls `management.azure.com` via `HttpClient`. No View, ViewModel, or other service calls ARM directly.
- `PermissionsService.cs` is the **only** class that calls the ARM `checkAccess` endpoint.
- `PortalUrl.cs` is the **only** place that constructs `portal.azure.com` URLs.

### macOS State and Data

- **SwiftData** for persistence: pinned RGs, pinned resources, cached tokens only.
- **Never** persist ARM resource lists in SwiftData. Live resource data is always fetched fresh on menu open.
- **Never** put `@State` in a view for data that belongs to the service layer. Views observe `@Observable` service objects or receive data via the environment.
- Token cache is keyed by `subscriptionId`. One `CachedToken` per subscription.

### Windows State and Data

- **EF Core + SQLite** for persistence: pinned RGs, pinned resources, cached tokens only.
- **Never** persist ARM resource lists in the DB. Live resource data is always fetched fresh on menu open.
- ViewModels are the only consumers of service interfaces. Views bind to ViewModel properties only.
- DB file location: `%LOCALAPPDATA%\AzPin\azpin.db`.
- Token cache is keyed by `subscriptionId`. One `CachedToken` row per subscription.

### macOS Concurrency

- Prefer `async/await` over callbacks everywhere.
- Use `TaskGroup` for parallel ARM fan-out across multiple subscriptions or resources.
- All UI updates on `@MainActor`.
- Avoid `Task { @MainActor in }` inside views — push that to the ViewModel/service layer.

### Windows Concurrency

- All async methods use `async`/`await` and `CancellationToken`.
- UI updates via `DispatcherQueue.TryEnqueue` or `[RelayCommand]` on the UI thread.
- Parallel ARM fan-out via `Task.WhenAll` or `Parallel.ForEachAsync`.

---

## Known Sharp Edges

### macOS

**`MenuBarExtra` with `.menu` style** only supports a limited SwiftUI subset in menu items. Stick to `Button`, `Divider`, `Label`, and `Text`. Complex custom views inside a `.menu` style `MenuBarExtra` will silently not render or behave unexpectedly. If richer layout is needed, switch that specific item to a `.window` style popover instead.

**`DisclosureGroup` in menu context** behaves differently from in a regular window. Document any workaround used and leave a `// NOTE:` comment explaining why the standard API couldn't be used.

**`az` binary path varies.** Check `/opt/homebrew/bin/az` (Apple silicon), `/usr/local/bin/az` (Intel), then fall back to `az` on PATH. `AzCLIService` resolves this — never hardcode a path.

**Portal URL construction.** Resource IDs from ARM already start with `/subscriptions/...`. Do not double-prefix. See `PortalURL.swift`.

### Windows

**Unpackaged WinUI 3 requires manual COM init.** `WinRT.ComWrappersSupport.InitializeComWrappers()` must be called before `Application.Start` in `Program.cs`. Missing this causes a silent crash.

**`AppWindow.IsShownInSwitchers`.** Set to `false` when main window is hidden/closed; `true` when shown. Failure to reset causes a phantom taskbar button.

**H.NotifyIcon context menus must be built on the UI thread.** Updating `TaskbarIcon` menu items from a background thread causes marshalling exceptions.

### Shared

**ARM resource type casing is inconsistent.** The same resource type can come back as `Microsoft.Web/sites`, `microsoft.web/sites`, or `MICROSOFT.WEB/SITES` depending on the endpoint. Always lowercase before comparing. `ResourceTypeMapper` (macOS) / `ResourceTypeHelper` (Windows) handles this — do not compare type strings anywhere else.

**ARM permissions check can fail silently.** If `checkAccess` returns an error or unexpected shape, default to NOT showing action buttons. Fail safe, not fail open.

---

## Naming Conventions

### macOS (Swift)

| Thing | Convention | Example |
|---|---|---|
| ARM response structs | `*Response` suffix | `ResourceListResponse` |
| SwiftData models | No suffix | `PinnedResourceGroup` |
| SF Symbol names | Defined in `ResourceTypeMapper` | Never inline symbol strings in views |
| Async service methods | Verb + noun | `fetchResources(in:)`, `startApp(_:)` |
| Boolean flags | `is*` or `has*` | `isRunning`, `hasCompletedOnboarding` |

### Windows (C#)

| Thing | Convention | Example |
|---|---|---|
| ARM response records | `Arm*` prefix | `ArmResource`, `ArmSubscription` |
| EF Core entities | No suffix | `PinnedResourceGroup` |
| Service interfaces | `I*` prefix | `IArmService`, `ITokenCache` |
| Async service methods | Verb + noun + `Async` | `FetchResourcesAsync`, `StartResourceAsync` |
| Boolean flags | `Is*` or `Has*` | `IsRunning`, `HasCompletedOnboarding` |
| ViewModels | `*ViewModel` suffix | `TrayIconViewModel` |

---

## File Map

### macOS

```
AzPin/
├── MenuBar/          # MenuBarExtra views only. No business logic.
├── MainWindow/       # Full app window: browse, pinned management.
├── Settings/         # Settings scene.
├── Services/         # All side-effectful logic lives here.
│   ├── AzCLIService        # az CLI invocations
│   ├── ARMService          # URLSession → management.azure.com
│   ├── TokenCache          # Token storage and refresh
│   └── PermissionsService  # checkAccess calls
├── Models/           # SwiftData models + ARM Decodable structs
└── Utilities/        # Pure functions, no side effects
    ├── PortalURL           # URL construction only
    ├── ResourceTypeMapper  # type string → SF Symbol
    └── ShellRunner         # Process wrapper
```

### Windows

```
src/windows/
├── AzPin.Windows.sln
├── AzPin.Windows/           # Main app project
│   ├── Program.cs           # Unpackaged WinUI 3 entry point
│   ├── App.xaml / App.xaml.cs
│   ├── TrayIcon/            # H.NotifyIcon tray icon and context menu
│   ├── MainWindow/          # Main app window (browse, pinned management)
│   ├── Services/            # All side-effectful logic lives here
│   │   ├── AzCliService     # az CLI invocations
│   │   ├── ArmService       # HttpClient → management.azure.com
│   │   ├── TokenCache       # EF Core token storage and refresh
│   │   └── PermissionsService  # checkAccess calls
│   ├── Models/
│   │   ├── Entities/        # EF Core persistence models
│   │   └── Arm/             # ARM response records (System.Text.Json)
│   ├── Data/
│   │   └── AzPinDbContext.cs
│   └── Utilities/           # Pure functions, no side effects
│       ├── PortalUrl        # URL construction only
│       └── ResourceTypeHelper  # type string → icon mapping
├── AzPin.Windows.Core/      # (if extracted) shared logic
└── AzPin.Windows.Tests/     # xUnit tests
    tasks/                   # Per-task implementation specs
```

---

## Dependencies

### macOS (SPM)

None currently. Before adding any package:
1. Confirm it is MIT or Apache 2.0 licensed.
2. Confirm it has no paid tier or commercial restriction.
3. Add it to this section with license noted.

### Windows (NuGet)

| Package | License | Purpose |
|---|---|---|
| `Microsoft.WindowsAppSDK` | MIT | WinUI 3 runtime |
| `H.NotifyIcon.WinUI` | MIT | Tray icon |
| `CommunityToolkit.Mvvm` | MIT | MVVM / RelayCommand |
| `Microsoft.EntityFrameworkCore.Sqlite` | MIT | Persistence |
| `Microsoft.Extensions.DependencyInjection` | MIT | DI container |
| `Microsoft.Extensions.Hosting` | MIT | App lifecycle |
| `xunit` + `xunit.runner.visualstudio` | Apache 2.0 | Tests |
| `Microsoft.EntityFrameworkCore.InMemory` | MIT | In-memory DB for tests |

Before adding any new package: confirm MIT or Apache 2.0, no paid tier, add to this table.

---

## Release & Versioning

- **Always update `CHANGELOG.md` whenever code or behavior changes.** Every meaningful change must be captured in the changelog.
- **Do not bump the version in the changelog while still on the same branch.** Keep the same version entry for all changes in-flight on that branch until the release is finalized.
- **Use GitVersioning for app versioning.** Version numbers must be derived from Git history/metadata, not manually hardcoded or manually incremented in source.
- `CHANGELOG.md` is the canonical change history; `GitVersioning` is the canonical app version source.
- macOS release branches: `release/v*` → produces signed, notarized DMG + Homebrew cask update.
- Windows release branches: `release/win/v*` → produces self-contained `win-x64` zip, attached to a GitHub pre-release.

See `RELEASE_PROCESS.md` for the release workflow and changelog guidance.

---

## What Not To Do

### Shared

- Do not call `az` outside of `AzCLIService` (either platform).
- Do not construct portal URLs outside of `PortalURL.swift` (macOS) or `PortalUrl.cs` (Windows).
- Do not compare ARM type strings without lowercasing first.
- Do not show action buttons (start/stop/restart) without first confirming permissions via `PermissionsService`.
- Do not crash or hide resources on ARM errors — show the resource with a warning indicator.
- Do not use background polling timers — explicitly out of scope for v1.

### macOS-only

- Do not add `Codable` conformance to SwiftData models — keep ARM response structs and persistence models separate.

### Windows-only

- Do not add JSON serialization attributes to EF Core entities — keep ARM response records and persistence entities separate.
- Do not use `Dispatcher.InvokeAsync` — use `DispatcherQueue.TryEnqueue` for WinUI 3.
- Do not reference `App.Services` from inside a ViewModel constructor — use constructor injection via DI.

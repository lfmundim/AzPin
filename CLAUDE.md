# AzPin — Claude Code Context

AzPin is a native macOS menubar app that reads the user's existing `az` CLI session and provides fast, pinnable access to Azure resources. Click to open in browser, start/stop/restart runnable resources inline. No Azure SDK. No App Store. No sandbox.

Full spec: `AZPIN_SPEC.md`

---

## Platform

- **macOS 26.0 (Tahoe) minimum. No exceptions.**
- No `#available` guards. The entire codebase assumes Tahoe.
- Universal binary (arm64 + x86_64).
- Swift 6.x, Xcode 26+.

---

## Build Commands

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

---

## Hard Constraints

These are non-negotiable. Do not work around them.

- **No Azure SDK.** All ARM calls via `URLSession` only. See `ARMService.swift`.
- **No paid dependencies.** Zero. No SPM packages with commercial licenses.
- **No hardcoded colors.** Semantic SwiftUI colors only: `.primary`, `.secondary`, `Color.green`, `Color.red`, `Color.orange`, `Color.accentColor`.
- **No custom fonts.** System font (`.font(.body)` etc.) only.
- **No `#available` guards.** Tahoe is the floor.
- **App Sandbox is OFF.** Required for `az` CLI shell access.
- **No `DispatchQueue.main.async`.** Use `@MainActor` instead.
- **No emoji in UI.** SF Symbols only.

---

## Architecture Rules

### Service Boundaries

- `ShellRunner.swift` is the **only** place that instantiates `Process`. Nothing else shells out directly.
- `TokenCache.swift` is the **only** place that handles token expiry and refresh. Callers call `token(for:)` and get a valid token back or an error — they never check expiry themselves.
- `ARMService.swift` is the **only** place that makes `URLSession` calls to `management.azure.com`. Views and ViewModels never call ARM directly.
- `PermissionsService.swift` is the **only** place that calls the ARM `checkAccess` endpoint.
- `PortalURL.swift` is the **only** place that constructs `portal.azure.com` URLs.

### State and Data

- **SwiftData** for persistence: pinned RGs, pinned resources, cached tokens only.
- **Never** persist ARM resource lists in SwiftData. Live resource data is always fetched fresh on menu open.
- **Never** put `@State` in a view for data that belongs to the service layer. Views observe `@Observable` service objects or receive data via the environment.
- Token cache is keyed by `subscriptionId`. One `CachedToken` per subscription.

### Concurrency

- Prefer `async/await` over callbacks everywhere.
- Use `TaskGroup` for parallel ARM fan-out across multiple subscriptions or resources.
- All UI updates on `@MainActor`.
- Avoid `Task { @MainActor in }` inside views — push that to the ViewModel/service layer.

---

## Known Sharp Edges

**`MenuBarExtra` with `.menu` style** only supports a limited SwiftUI subset in menu items. Stick to `Button`, `Divider`, `Label`, and `Text`. Complex custom views inside a `.menu` style `MenuBarExtra` will silently not render or behave unexpectedly. If richer layout is needed, switch that specific item to a `.window` style popover instead.

**`DisclosureGroup` in menu context** behaves differently from in a regular window. Document any workaround used and leave a `// NOTE:` comment explaining why the standard API couldn't be used.

**ARM resource type casing is inconsistent.** The same resource type can come back as `Microsoft.Web/sites`, `microsoft.web/sites`, or `MICROSOFT.WEB/SITES` depending on the endpoint. Always `.lowercased()` before comparing. `ResourceTypeMapper` handles this — do not compare type strings anywhere else.

**`az` binary path varies.** Check `/opt/homebrew/bin/az` (Apple silicon), `/usr/local/bin/az` (Intel), then fall back to `az` on PATH. `AzCLIService` resolves this — never hardcode a path.

**ARM permissions check can fail silently.** If `checkAccess` returns an error or unexpected shape, default to NOT showing action buttons. Fail safe, not fail open.

**Portal URL construction.** Resource IDs from ARM already start with `/subscriptions/...`. Do not double-prefix. See `PortalURL.swift`.

---

## Naming Conventions

| Thing | Convention | Example |
|---|---|---|
| ARM response structs | `*Response` suffix | `ResourceListResponse` |
| SwiftData models | No suffix | `PinnedResourceGroup` |
| SF Symbol names | Defined in `ResourceTypeMapper` | Never inline symbol strings in views |
| Async service methods | Verb + noun | `fetchResources(in:)`, `startApp(_:)` |
| Boolean flags | `is*` or `has*` | `isRunning`, `hasCompletedOnboarding` |

---

## File Map

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

---

## Dependencies (SPM)

None currently. Before adding any package:
1. Confirm it is MIT or Apache 2.0 licensed.
2. Confirm it has no paid tier or commercial restriction.
3. Add it to this section with license noted.

---

## Release & Versioning

- **Always update `CHANGELOG.md` whenever code or behavior changes.** Every meaningful change must be captured in the changelog.
- **Do not bump the version in the changelog while still on the same branch.** Keep the same version entry for all changes in-flight on that branch until the release is finalized.
- **Use GitVersioning for app versioning.** Version numbers must be derived from Git history/metadata, not manually hardcoded or manually incremented in source.
- `CHANGELOG.md` is the canonical change history; `GitVersioning` is the canonical app version source.

See `RELEASE_PROCESS.md` for the release workflow and changelog guidance.

---

## What Not To Do

- Do not call `az` outside of `AzCLIService`.
- Do not construct portal URLs outside of `PortalURL.swift`.
- Do not compare ARM type strings without `.lowercased()`.
- Do not add `Codable` conformance to SwiftData models — keep ARM response structs and persistence models separate.
- Do not use `Timer` for background polling — it is explicitly out of scope for v1.
- Do not show action buttons (start/stop/restart) without first confirming permissions via `PermissionsService`.
- Do not crash or hide resources on ARM errors — show the resource with a warning indicator.

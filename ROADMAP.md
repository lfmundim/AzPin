# AzPin Roadmap

Planned features, ordered by likely release. Nothing here is committed to a timeline.

---

## v1.0 — Required Before Release

### Auto-Update (Sparkle)

Integrate [Sparkle](https://sparkle-project.org) (MIT) for in-app update checks and silent background downloads.

**Why:** Direct-distribution app with no App Store delivery mechanism. Users need a reliable way to receive updates without manually checking GitHub.

**How it works:**
- Add Sparkle via SPM.
- Wire `SPUStandardUpdaterController` into `AppDelegate` / app entry point.
- Set `SUFeedURL` in `Info.plist` pointing at appcast hosted on GitHub Releases.
- Generate EdDSA signing key once with Sparkle's `generate_keys` tool; store private key in secrets, embed public key in app bundle.
- Release workflow: export `.app` → zip → run `generate_appcast` → push appcast + zip asset to GitHub Release. GitVersioning provides the version string.
- Update check triggers on launch (with user-configurable interval); Sparkle handles download, verification, and relaunch.

**Scope:** No custom update UI needed — Sparkle's standard sheet is sufficient for v1.0.

---

## v1.1 — Action Notifications + Copy Endpoint

### Action Completion Notifications

After tapping Start, Stop, or Restart in the menubar, AzPin polls ARM until the resource reaches its expected state, then fires a native macOS notification confirming the outcome.

**Why:** Start/stop operations on App Services and Container Apps can take 15–60 seconds. Currently the transitional spinner runs until the state resolves, but only if the menu stays open. With notifications, the user can dismiss the menu and be told when the operation completes.

**How it works:**
- On action dispatch (start/stop/restart), record the resource ID, target state, and a deadline (e.g. 5 minutes).
- Spawn a background `Task` that calls `ARMService.fetchAppState` every 5 seconds.
- When the observed state matches the target state (or the deadline expires), send a `UNUserNotificationCenter` notification:
  - Success: `"{resource name} is now Running."` / `"… is now Stopped."`
  - Timeout: `"{resource name}: operation timed out. Check the portal."`
- Request notification permission on first action if not already granted (`UNUserNotificationCenter.current().requestAuthorization`).
- The polling task is cancelled immediately if the user manually triggers another action on the same resource.

**Scope note:** This is per-action polling only — triggered by a user gesture, not a background timer. It does not require a persistent background process or launch agent.

### Copy Endpoint / Hostname

One-click copy of the primary endpoint for runnable resources directly from the menubar.

**Why:** Developers reference App Service URLs and Container App FQDNs constantly. Currently requires opening the portal.

**How it works:**
- For `Microsoft.Web/sites`: copy `defaultHostName` from ARM properties (`https://{name}.azurewebsites.net`).
- For `Microsoft.App/containerApps`: copy `configuration.ingress.fqdn` from ARM properties.
- Button appears in the resource row alongside Open in Portal and action buttons, only when a resolvable endpoint exists.
- Copies to `NSPasteboard.general` with `https://` prefix. No browser open.

---

## v1.2 — Background Polling (Periodic Refresh)

Periodically re-fetch running state for all pinned runnable resources while the app is running, without the user opening the menu.

**Why:** Running state shown in the menubar can go stale if a resource is stopped externally (deployment, auto-scaling, another user). Background polling keeps the indicators accurate.

**How it works:**
- Settings toggle: "Auto-refresh running state" — off by default, configurable interval (1 min / 5 min / 15 min).
- Uses `DispatchSource.makeTimerSource` (or a Swift `AsyncStream`-based ticker) — no `Timer`.
- On each tick, fan out `fetchAppState` calls in parallel via `TaskGroup` for all pinned runnable resources.
- Updates `MenuBarViewModel.appStates` on `@MainActor`; no UI flicker if state is unchanged.
- Polling pauses when the machine is sleeping (`NSWorkspace.willSleepNotification`) and resumes on wake.
- Does not re-fetch permissions or resource lists — only running state.

---

## v1.3 — Multi-Subscription Pinning

Pin resource groups from different subscriptions simultaneously without switching the active `az` subscription context.

**Why:** Developers with resources spread across dev/staging/prod subscriptions currently have to re-pin after switching subscriptions in BrowseView.

**How it works:**
- `az account get-access-token --subscription {id}` works for any subscription the identity has access to, without modifying the active context.
- Each subscription gets its own `CachedToken` (already modelled by `CachedToken.subscriptionId`).
- ARM fan-out in `MenuBarViewModel` groups by unique subscription ID and fetches tokens in parallel.
- Menu display: flat RG list, subscription name shown as secondary `.caption` text only when two pinned RGs share the same name across different subscriptions.

**Data model addition:**
```swift
// Add to PinnedResourceGroup:
var subscriptionDisplayName: String  // resolved once at pin time, stored
```

---

## v1.4 — Windows Version

Native WinUI 3 app with feature parity. Task list in `src/windows/tasks/`.

### Auto-Update (Windows)

Use **MSIX + AppInstaller** for update delivery — OS-native, zero extra dependencies, hosts `.appinstaller` file on GitHub Releases same as macOS appcast.

**Velopack note:** [Velopack](https://velopack.io) (MIT) is a cross-platform alternative that would unify macOS + Windows release scripting under one tool. Currently sticking with Sparkle (macOS) + MSIX AppInstaller (Windows) as the more mature path. Revisit Velopack before Windows release work begins — if it has matured significantly, switching both platforms to Velopack may be worth the migration.

---

## v2 — Multi-Tenant / Multi-Environment

Support multiple Azure tenants (e.g. "Work" and "Personal") each with their own pinned resource groups, selectable from the menubar.

**Why:** Users with a personal Azure subscription and a work tenant need to switch context manually today.

**How it works:**
- Named "Environments" (user-defined labels), each with a tenant ID, default subscription list, and their own pinned RG set.
- `az login --tenant {tenantId}` supports concurrent tenant sessions.
- Switcher at the top of the menubar dropdown: shows current environment, tap to cycle or pick.
- Pinned data stored per-environment; switching environment reloads the pinned resources for that environment.

**Data model addition:** `Environment` as a first-class `@Model`, with `PinnedResourceGroup.environmentId` (nullable for migration compatibility with v1.x data).

---

## v0.3 — Next Up

- **Copy resource ID**: right-click on a resource → "Copy Resource ID" copies the ARM ID to clipboard.

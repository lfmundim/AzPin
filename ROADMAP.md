# AzPin Roadmap

Planned features, ordered by likely release. Nothing here is committed to a timeline.

---

## v1.0 — Required Before Release

_(No blocking items currently. Sparkle / auto-update moved to unversioned consideration below. Waiting on WinGet for v1.0 release)_

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

## v1.2 — Multi-Subscription Pinning

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

## Unversioned Considerations

### Auto-Update (macOS — Sparkle)

[Sparkle](https://sparkle-project.org) (MIT) provides in-app update checks and silent background downloads for direct-distribution macOS apps.

Users can already check for updates manually via the Check for Updates option in the app. Sparkle would add background/scheduled checks and in-app download + relaunch.

**Consider adding if:** manual update checks prove insufficient (e.g. users miss releases frequently, or Homebrew distribution is dropped).

**How it would work:**
- Add Sparkle via SPM.
- Wire `SPUStandardUpdaterController` into the app entry point.
- Set `SUFeedURL` in `Info.plist` pointing at an appcast hosted on GitHub Releases.
- Generate EdDSA signing key once; store private key in secrets, embed public key in app bundle.
- Release workflow: export `.app` → zip → run `generate_appcast` → push appcast + zip asset to GitHub Release.

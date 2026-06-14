# Azure Interactions

This document is an **exhaustive list of every interaction AzPin performs against Azure**, for ease of auditing. If you want to know exactly what AzPin reads, calls, or changes in your Azure environment, it is all here.

Key facts up front:

- **AzPin performs no authentication of its own.** There is no login screen, no client secret, no app registration, no device-code flow. The app simply asks the locally installed `az` CLI for an access token, reusing whatever session you established with `az login`. If you are not logged in, AzPin shows an onboarding screen telling you to run `az login` — it never prompts for credentials.
- **No Azure SDK is used.** Every ARM call is a plain HTTPS request (`URLSession` on macOS, `HttpClient` on Windows, `reqwest` on Ubuntu) against `https://management.azure.com`. What you see below is byte-for-byte what goes over the wire.
- **Tokens are cached locally, per subscription**, only until their natural expiry (SwiftData on macOS, SQLite on Windows/Ubuntu). They are never transmitted anywhere except in the `Authorization` header of ARM requests.
- **Write operations are limited to start / stop / restart** of runnable resources (App Services, slots, Container Apps, Logic Apps, VMs on Ubuntu). Everything else is read-only.
- **Action buttons are permission-gated.** Start/Stop/Restart only appear after an RBAC permissions check confirms your account can perform them. On any error or unexpected response the app fails safe: no buttons.
- The only non-Azure network call the app makes is to the **GitHub Releases API** (`api.github.com`) when you explicitly click "Check for Updates". That is out of scope for this document.

Conventions used below:

- `{placeholders}` mark values substituted at runtime.
- `{resourceId}` is a full ARM resource ID as returned by ARM itself, e.g. `/subscriptions/{subId}/resourceGroups/{rg}/providers/Microsoft.Web/sites/{name}`.
- Platform links go to the single file where each platform is allowed to make that call (service-boundary rule — see `CLAUDE.md`).

---

## 1. Azure CLI invocations

All `az` invocations are read-only queries against the local CLI session. AzPin never runs `az login`, `az logout`, or any `az` command that mutates state.

### 1.1 Show current account

```sh
az account show --output json
```

**Why:** Detect whether the user is signed in, and display the signed-in user and default subscription in the tray/menubar. A failure here flips the UI to the "run `az login`" onboarding screen.

| Platform | Where |
|---|---|
| macOS | [`AzCLIService.swift`](src/macos/AzPin/Services/AzCLIService.swift) (`currentAccount`) |
| Windows | [`AzCliService.cs`](src/windows/AzPin.Windows.Core/Services/AzCliService.cs) (`GetCurrentAccountAsync`) |
| Ubuntu | [`az_cli.rs`](src/ubuntu/AzPin/src/services/az_cli.rs) (`get_default_subscription`) |

### 1.2 List subscriptions

```sh
az account list --output json
```

**Why:** Populate the subscription dropdown in the main window so the user can browse resource groups per subscription.

| Platform | Where |
|---|---|
| macOS | [`AzCLIService.swift`](src/macos/AzPin/Services/AzCLIService.swift) (`listSubscriptions`) |
| Windows | [`AzCliService.cs`](src/windows/AzPin.Windows.Core/Services/AzCliService.cs) (`ListSubscriptionsAsync`) |
| Ubuntu | [`az_cli.rs`](src/ubuntu/AzPin/src/services/az_cli.rs) (`list_subscriptions`) |

### 1.3 Get access token

macOS / Windows:

```sh
az account get-access-token --subscription {subscriptionId} --output json
```

Ubuntu (explicit ARM audience, same default the CLI uses):

```sh
az account get-access-token --subscription {subscriptionId} --resource https://management.azure.com/ --output json
```

**Why:** This is the **only** way AzPin obtains credentials. The returned bearer token (scoped to the ARM audience) authorizes every HTTP call in section 2. The token and its expiry are cached locally keyed by subscription ID; a new token is requested only when the cached one has expired. The token cache layer is the sole caller:

| Platform | CLI invocation | Cache layer (sole caller) |
|---|---|---|
| macOS | [`AzCLIService.swift`](src/macos/AzPin/Services/AzCLIService.swift) (`fetchToken`) | [`TokenCache.swift`](src/macos/AzPin/Services/TokenCache.swift) |
| Windows | [`AzCliService.cs`](src/windows/AzPin.Windows.Core/Services/AzCliService.cs) (`GetAccessTokenAsync`) | [`TokenCache.cs`](src/windows/AzPin.Windows.Core/Services/TokenCache.cs) |
| Ubuntu | [`az_cli.rs`](src/ubuntu/AzPin/src/services/az_cli.rs) (`get_access_token`) | [`token_cache.rs`](src/ubuntu/AzPin/src/services/token_cache.rs) |

---

## 2. ARM REST API calls

All calls go to `https://management.azure.com` with a single header:

```http
Authorization: Bearer {token from section 1.3}
```

ARM is the **only** host these services ever contact.

### 2.1 List subscriptions (ARM)

```http
GET /subscriptions?api-version=2022-12-01
```

**Why:** ARM-side counterpart to `az account list`.

| Platform | Where |
|---|---|
| Windows only | [`ArmService.cs`](src/windows/AzPin.Windows.Core/Services/ArmService.cs) (`FetchSubscriptionsAsync`) |

macOS and Ubuntu list subscriptions exclusively via the CLI (section 1.2).

### 2.2 List resource groups

```http
GET /subscriptions/{subscriptionId}/resourcegroups?api-version=2021-04-01
```

**Why:** Populate the browse list in the main window for the selected subscription. Read-only.

| Platform | Where |
|---|---|
| macOS | [`ARMService.swift`](src/macos/AzPin/Services/ARMService.swift) (`fetchResourceGroups`) |
| Windows | [`ArmService.cs`](src/windows/AzPin.Windows.Core/Services/ArmService.cs) (`FetchResourceGroupsAsync`) |
| Ubuntu | [`arm.rs`](src/ubuntu/AzPin/src/services/arm.rs) (`fetch_resource_groups`) |

### 2.3 List resources in a resource group

```http
GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/resources?api-version=2021-04-01
```

**Why:** Show the resources inside a resource group — when expanding a group in the browse list, and when refreshing the contents of a pinned group for the tray/menubar menu. Live data is always fetched fresh; resource lists are never persisted.

| Platform | Where |
|---|---|
| macOS | [`ARMService.swift`](src/macos/AzPin/Services/ARMService.swift) (`fetchResources`) |
| Windows | [`ArmService.cs`](src/windows/AzPin.Windows.Core/Services/ArmService.cs) (`FetchResourcesAsync`) |
| Ubuntu | [`arm.rs`](src/ubuntu/AzPin/src/services/arm.rs) (`fetch_resources`) |

### 2.4 Read a resource's running state

```http
GET {resourceId}?api-version={apiVersion}
```

Ubuntu additionally expands the instance view for virtual machines, because a VM's power state only appears there:

```http
GET {resourceId}?api-version=2023-09-01&$expand=instanceView
```

**Why:** Decide whether a pinned runnable resource is Running, Stopped, or transitioning so the tray menu can offer the correct action (Start vs. Stop/Restart). Read-only.

`{apiVersion}` per resource type:

| Resource type | api-version | State field read |
|---|---|---|
| `microsoft.web/sites`, `microsoft.web/sites/slots` (default) | `2023-01-01` | `properties.state` |
| `microsoft.app/containerapps` | `2023-05-01` | `properties.runningStatus` |
| `microsoft.logic/workflows` | `2019-05-01` | `properties.state` (`Enabled`/`Disabled`) |
| `microsoft.compute/virtualmachines` (Ubuntu only) | `2023-09-01` | `properties.instanceView.statuses[].code` (`PowerState/*`) |

| Platform | Where |
|---|---|
| macOS | [`ARMService.swift`](src/macos/AzPin/Services/ARMService.swift) (`fetchAppState`) |
| Windows | [`ArmService.cs`](src/windows/AzPin.Windows.Core/Services/ArmService.cs) (`FetchRunningStateAsync`) |
| Ubuntu | [`arm.rs`](src/ubuntu/AzPin/src/services/arm.rs) (`get_resource_state`) |

### 2.5 Start / Stop / Restart a resource

**The only write operations AzPin ever performs.** Empty-body POSTs, fired exclusively by an explicit user click on an action button, and only after the permissions check (2.6) has confirmed access.

```http
POST {resourceId}/{action}?api-version={apiVersion}
```

`{action}` is `start`, `stop`, or `restart`, with these per-type mappings:

| Resource type | Start | Stop | Restart |
|---|---|---|---|
| App Service / slot (default) | `start` | `stop` | `restart` |
| Container Apps | `start` | `stop` | `stop` then `start` (no restart endpoint) |
| Logic Apps | `enable` | `disable` | `stop`+`start` on Windows; `restart` on macOS/Ubuntu |
| Virtual machines (Ubuntu only) | `start` | `powerOff` | `restart` |

`{apiVersion}` follows the same table as section 2.4.

| Platform | Where |
|---|---|
| macOS | [`ARMService.swift`](src/macos/AzPin/Services/ARMService.swift) (`startApp` / `stopApp` / `restartApp` → `performAction`) |
| Windows | [`ArmService.cs`](src/windows/AzPin.Windows.Core/Services/ArmService.cs) (`StartResourceAsync` / `StopResourceAsync` / `RestartResourceAsync` → `PostActionAsync`) |
| Ubuntu | [`arm.rs`](src/ubuntu/AzPin/src/services/arm.rs) (`start_resource` / `stop_resource` / `restart_resource` → `post_action`) |

> Note: `powerOff` (Ubuntu VMs) stops the VM but **does not deallocate it** — compute billing continues. AzPin never calls `deallocate` or `delete` on anything.

### 2.6 Check RBAC permissions on a resource

```http
GET {resourceId}/providers/Microsoft.Authorization/permissions?api-version=2022-04-01
```

**Why:** Before showing Start/Stop/Restart buttons, AzPin verifies the signed-in account actually holds the corresponding RBAC actions (e.g. `Microsoft.Web/sites/start/action`). The response's `actions` / `notActions` patterns are evaluated locally with wildcard support (`*`, `Microsoft.Web/sites/*`). This endpoint is readable by Contributors (unlike the `checkAccess` POST, which needs Owner-level rights — a deliberate choice). **Fail-safe:** any error, non-2xx, or unexpected shape results in no action buttons. Results are cached in memory per resource. Read-only.

| Platform | Where |
|---|---|
| macOS | [`PermissionsService.swift`](src/macos/AzPin/Services/PermissionsService.swift) (`checkAccess`) |
| Windows | [`PermissionsService.cs`](src/windows/AzPin.Windows.Core/Services/PermissionsService.cs) (`CheckAccessAsync`) |
| Ubuntu | [`permissions.rs`](src/ubuntu/AzPin/src/services/permissions.rs) (`check_access`) |

---

## 3. Azure Portal links (browser only)

Not API calls — clicking a pinned item opens the default browser at the Azure Portal. No data leaves the machine beyond the navigation itself; the portal authenticates the user with its own session.

```text
https://portal.azure.com/#resource{resourceId}
https://portal.azure.com/#resource/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}
```

| Platform | Where (sole constructor of portal URLs) |
|---|---|
| macOS | [`PortalURL.swift`](src/macos/AzPin/Utilities/PortalURL.swift) |
| Windows | [`PortalUrl.cs`](src/windows/AzPin.Windows.Core/Utilities/PortalUrl.cs) |
| Ubuntu | [`portal_url.rs`](src/ubuntu/AzPin/src/utils/portal_url.rs) |

---

## Keeping this document current

Any change that adds, removes, or alters an Azure interaction (new `az` invocation, new ARM endpoint, changed api-version, new action mapping) **must** be reflected here in the same change. This rule is enforced via `CLAUDE.md`.

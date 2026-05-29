# Skill: ARM API Reference

Use this file when writing or modifying anything in `ARMService.swift`, adding new ARM endpoints, or debugging API responses. All endpoint URLs, API versions, and response shapes used in AzPin are documented here. Do not guess API versions — use the versions listed.

---

## Base URL

```
https://management.azure.com
```

All requests require:
```
Authorization: Bearer {token}
Content-Type: application/json
```

---

## Endpoints

### List Subscriptions

```
GET /subscriptions?api-version=2022-12-01
```

Response shape:
```json
{
  "value": [
    {
      "id": "/subscriptions/{subscriptionId}",
      "subscriptionId": "...",
      "displayName": "My Subscription",
      "tenantId": "...",
      "state": "Enabled"
    }
  ]
}
```

---

### List Resource Groups

```
GET /subscriptions/{subscriptionId}/resourcegroups?api-version=2021-04-01
```

Response shape:
```json
{
  "value": [
    {
      "id": "/subscriptions/{sub}/resourceGroups/{name}",
      "name": "rg-production",
      "location": "westeurope",
      "properties": {
        "provisioningState": "Succeeded"
      }
    }
  ]
}
```

---

### List Resources in a Resource Group

```
GET /subscriptions/{subscriptionId}/resourceGroups/{rgName}/resources?api-version=2021-04-01
```

Response shape:
```json
{
  "value": [
    {
      "id": "/subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Web/sites/{name}",
      "name": "func-my-app",
      "type": "Microsoft.Web/sites",
      "location": "westeurope",
      "kind": "functionapp",
      "tags": {}
    }
  ]
}
```

**Important:** `type` casing is inconsistent across endpoints. Always `.lowercased()` before any comparison.

**Important:** Function Apps and App Services share the type `Microsoft.Web/sites`. Distinguish them via the `kind` field:
- `kind: "functionapp"` → Function App
- `kind: "app"` → App Service
- `kind: "functionapp,linux"` → Linux Function App
- `kind: "app,linux"` → Linux App Service

---

### Get App Service / Function App Detail

```
GET /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Web/sites/{name}?api-version=2023-01-01
```

Key response fields:
```json
{
  "properties": {
    "state": "Running",
    "hostNames": ["myapp.azurewebsites.net"],
    "kind": "functionapp"
  }
}
```

`properties.state` values: `"Running"`, `"Stopped"`, `"Starting"`, `"Stopping"`, `"Unknown"`

---

### Start App Service / Function App

```
POST /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Web/sites/{name}/start?api-version=2023-01-01
```

Body: empty (`""` or omit)
Success: HTTP 200, empty body

---

### Stop App Service / Function App

```
POST /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Web/sites/{name}/stop?api-version=2023-01-01
```

Body: empty
Success: HTTP 200, empty body

---

### Restart App Service / Function App

```
POST /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Web/sites/{name}/restart?api-version=2023-01-01
```

Optional query param: `?softRestart=false` (default false = full restart)
Body: empty
Success: HTTP 200, empty body

---

### Get Container App Detail

```
GET /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.App/containerApps/{name}?api-version=2023-05-01
```

Key response fields:
```json
{
  "properties": {
    "runningStatus": "Running"
  }
}
```

`properties.runningStatus` values: `"Running"`, `"Stopped"`, `"Unknown"`

**Note:** Container Apps use a different provider path (`Microsoft.App`) and different API version from App Service. Do not share the same ARM method.

---

### Start Container App

```
POST /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.App/containerApps/{name}/start?api-version=2023-05-01
```

Success: HTTP 200 or HTTP 202 (async operation — poll `Location` header if 202)

---

### Stop Container App

```
POST /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.App/containerApps/{name}/stop?api-version=2023-05-01
```

Success: HTTP 200 or HTTP 202

---

### Check Permissions (checkAccess)

```
POST /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Authorization/permissions?api-version=2022-04-01
```

Body:
```json
{
  "actions": [
    "Microsoft.Web/sites/start/action",
    "Microsoft.Web/sites/stop/action",
    "Microsoft.Web/sites/restart/action"
  ]
}
```

Response:
```json
{
  "value": [
    {
      "actions": ["Microsoft.Web/sites/start/action"],
      "notActions": []
    }
  ]
}
```

If the returned `actions` array contains the action, the user has permission. If the call fails (403, 404, network error), default to NOT showing action buttons.

---

## Resource Type Reference

Always compare lowercased. These are the types AzPin handles specially:

| Lowercased Type | Display Name | Runnable |
|---|---|---|
| `microsoft.web/sites` | Function App or App Service (check `kind`) | ✅ |
| `microsoft.web/sites/slots` | Deployment Slot | ✅ |
| `microsoft.app/containerapps` | Container App | ✅ |
| `microsoft.logic/workflows` | Logic App | ✅ |
| `microsoft.insights/components` | App Insights | ❌ |
| `microsoft.storage/storageaccounts` | Storage Account | ❌ |
| `microsoft.servicebus/namespaces` | Service Bus | ❌ |
| `microsoft.keyvault/vaults` | Key Vault | ❌ |
| `microsoft.apimanagement/service` | API Management | ❌ |
| `microsoft.sql/servers/databases` | SQL Database | ❌ |
| `microsoft.documentdb/databaseaccounts` | CosmosDB | ❌ |

Anything not in this table maps to `cloud.fill` and is not runnable.

---

## Error Handling

| HTTP Status | Meaning | Action |
|---|---|---|
| 200 | Success | Parse response |
| 202 | Async operation accepted | Poll `Location` header (Container Apps only in v1) |
| 400 | Bad request | Log, show warning on resource |
| 401 | Token expired | Refresh token via `TokenCache`, retry once |
| 403 | Insufficient permissions | Hide action buttons, show resource as read-only |
| 404 | Resource not found | Silently remove from menu if it was a fetched resource; show error if it was a direct action |
| 429 | Rate limited | Back off, show warning |
| 5xx | Azure-side error | Show warning, do not retry automatically |

On 401, retry exactly once after token refresh. If still 401 after retry, treat as an auth error and prompt re-login.

---

## Token Shape

Response from `az account get-access-token --output json`:

```json
{
  "accessToken": "eyJ...",
  "expiresOn": "2026-05-29 14:30:00.000000",
  "subscription": "00000000-0000-0000-0000-000000000000",
  "tenant": "00000000-0000-0000-0000-000000000000",
  "tokenType": "Bearer"
}
```

`expiresOn` is a string in `"yyyy-MM-dd HH:mm:ss.SSSSSS"` format. Parse carefully — it is not ISO 8601.

Refresh threshold: if `expiresOn - now < 5 minutes`, treat as expired and re-fetch before making any ARM call.

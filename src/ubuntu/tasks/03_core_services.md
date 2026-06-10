# Task 3: Core Services

## Objective
Implement the service layer to interact with the Azure CLI and Azure Resource Manager (ARM).

## Context
These services form the backbone of AzPin. They must adhere strictly to the boundaries defined in `CLAUDE.md`.

## Steps
1. **`AzCliService`:**
   - **Responsibility:** The *only* place that shells out to `az`.
   - Use `std::process::Command` to execute `az`.
   - Implement path resolution: check `/usr/bin/az` or rely on the `PATH` environment variable.
   - Implement `get_access_token(subscription_id: &str) -> Result<String, Error>` by running `az account get-access-token --subscription <id> --output json`.

2. **`TokenCache`:**
   - **Responsibility:** Manage token lifecycles.
   - Implement `get_valid_token(subscription_id: &str) -> Result<String, Error>`.
   - Check the DB/cache. If the token is missing or expires in < 5 minutes, call `AzCliService::get_access_token`, save to DB, and return it.

3. **`ArmService`:**
   - **Responsibility:** The *only* place that calls `https://management.azure.com` via HTTP.
   - Use `reqwest` client.
   - Implement `fetch_resource_groups`, `fetch_resources`, `get_resource_state` (Running/Stopped), `start_resource`, `stop_resource`, `restart_resource`.
   - All requests must include the `Authorization: Bearer <token>` header, getting the token exclusively from `TokenCache`.

4. **`PermissionsService`:**
   - Implement `check_access` against the ARM API to verify if the user has start/stop/restart permissions.

## Constraints
- Do NOT use any Azure SDK. Only raw `reqwest` REST calls.
- Handle network errors gracefully; return `Result` types. Do not panic.
- Ensure parallel execution for multiple requests using `tokio::task::JoinSet` or `futures::future::join_all`.

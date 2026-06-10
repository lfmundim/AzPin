# Task 2: Data Models & Persistence

## Objective
Implement the data models and persistence layer using the XDG Base Directory Specification.

## Context
AzPin requires caching for two things: the user's pinned resource groups (and resources) and the Azure access tokens.

## Steps
1. **Define Models (in `src/models/`):**
   - Create `PinnedResourceGroup`: `id`, `subscription_id`, `name`, `display_order`, `resources` (Vec of `PinnedResource`).
   - Create `PinnedResource`: `id`, `name`, `type_`, `resource_group`, `subscription_id`, `location`, `display_order`.
   - Create `CachedToken`: `subscription_id`, `tenant_id`, `access_token`, `expires_on`.
   - Create ARM response structs mapping to Azure's JSON responses (e.g., `ArmResource`, `ArmResourceGroup`). Add `#[derive(Deserialize)]` to them.
   - **Crucial Rule:** Keep persistence models distinct from ARM response structs! Do not cross-pollinate `Deserialize` attributes onto database entities.

2. **Implement Persistence:**
   - Use `rusqlite` to manage an SQLite database, or `serde_json` to manage JSON files.
   - **Path:** Resolve the data directory using `std::env::var("XDG_DATA_HOME")` (falling back to `~/.local/share/`). The database should live at `~/.local/share/azpin/azpin.db`.
   - Write CRUD operations for tokens and pinned items.

## Constraints
- Ensure DB initialization runs at app startup and creates the tables if they don't exist.
- Tokens must be keyed by `subscription_id` (one token per subscription).

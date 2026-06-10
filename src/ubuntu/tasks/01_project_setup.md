# Task 1: Project Scaffolding & Setup

## Objective
Initialize the Rust project and configure the GTK4 + libadwaita environment for AzPin's Ubuntu native port.

## Context
AzPin on Ubuntu must follow the exact same strict boundaries as the macOS and Windows versions (see `CLAUDE.md` and `AZPIN_SPEC.md`):
- 100% native UI (GTK4 + libadwaita).
- Native core (Rust).
- Zero paid dependencies.
- No Azure SDKs.

## Steps
1. **Initialize Project:**
   - Run `cargo init --bin` inside `src/ubuntu/AzPin`.
   - Update `Cargo.toml` with the necessary dependencies:
     - `gtk4` and `libadwaita` for UI.
     - `tokio` (with full features) for async runtime.
     - `reqwest` (with `json` feature) for REST API calls.
     - `serde` and `serde_json` for JSON parsing.
     - `rusqlite` for SQLite persistence (or `serde_json` for simpler file-based JSON storage per XDG spec).
     - `ayatana-appindicator` or `libappindicator` bindings (via `libappindicator-sys` or `ayatana-appindicator-rs`) for the top panel menu.

2. **Setup App Entry Point:**
   - In `src/main.rs`, initialize a standard `adw::Application` (libadwaita).
   - Set the application ID to `com.lfmundim.azpin`.
   - Ensure the application runs without keeping a main window open by default (since it's an indicator app). You might need to use `gio::ApplicationFlags::HANDLES_COMMAND_LINE` or simply not present a window on activation.

3. **Configure Project Structure:**
   Create the following module hierarchy in `src/`:
   - `models/`: Persistence and ARM response structs.
   - `services/`: AzCli, ARM REST, TokenCache.
   - `ui/`: Menubar/indicator and MainWindow components.
   - `utils/`: Portal URL builder, icon mappers.

## Constraints
- All UI must use standard GTK/libadwaita widgets.
- No custom hex colors; use Adwaita semantic colors.
- Follow Rust standard practices (`Result` for error handling, `async/await` with `tokio`).

# Task 6: Packaging & Distribution

## Objective
Prepare AzPin to be published via `apt-get` for Ubuntu users.

## Context
AzPin distributes standalone binaries. For Ubuntu, we need a Debian package (`.deb`).

## Steps
1. **Desktop Integration Files:**
   - Create `assets/linux/com.lfmundim.azpin.desktop` file. Ensure `Exec=/usr/bin/azpin` and `Icon=com.lfmundim.azpin`.
   - Create SVG icons for the app launcher and indicator. Place them in standard resolutions under an `assets/linux/icons/` directory.

2. **Debian Control File (`DEBIAN/control`):**
   - Package name: `azpin`.
   - Architecture: `amd64` (and `arm64`).
   - Depends: `libgtk-4-1, libadwaita-1-0, libappindicator3-1`.
   - Recommends: `azure-cli`.
   - Maintainer and description fields.

3. **Build Script / GitHub Action:**
   - Add a step in `.github/workflows/release.yml` for Ubuntu.
   - Run `cargo build --release --target x86_64-unknown-linux-gnu`.
   - Use a tool like `cargo-deb` (highly recommended for Rust) to automatically bundle the binary, `.desktop` file, icons, and generate the `.deb` package.
     - Add `[package.metadata.deb]` section in `Cargo.toml` to configure paths.
   - Upload the resulting `.deb` to GitHub Releases.

4. **APT Repository (Optional/Future):**
   - Document how to set up an APT repository using GitHub Pages so users can run `apt-get install azpin`. This matches the Homebrew tap experience.

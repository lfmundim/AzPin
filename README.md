# AzPin

<p align="center">
  <img src="assets/iconset/original.svg" alt="Logo" width="30%" />
</p>

AzPin is a native macOS menubar app that reads your existing `az` CLI session and gives you fast, pinnable access to Azure resources. Open the menubar, see your pinned resource groups and their live resources, click to open in the portal, or start/stop/restart runnable resources without leaving the desktop.

There is also an in-progress WinUI 3 Windows port under `src/windows/`. Windows builds are currently distributed as pre-release zip bundles from GitHub Releases rather than an installer.

No Azure SDK. No App Store. No sandbox. Requires macOS 26 Tahoe.

---

## Prerequisites

- **macOS 26 Tahoe** or later
- **Azure CLI** installed ([aka.ms/installazureclimacos](https://aka.ms/installazureclimacos))
- Signed in: `az login`

---

## Install

### Homebrew (recommended)

```bash
brew tap lfmundim/tap
brew install --cask azpin
```

### DMG (manual)

Download the latest `.dmg` from [Releases](../../releases), drag `AzPin.app` to `/Applications`.

### Windows Preview (manual)

Download the latest Windows pre-release zip from [Releases](../../releases), extract it on a Windows 11 VM, and run `AzPin.Windows.exe`.

The Windows release workflow publishes a self-contained unpackaged bundle, so no separate Windows App SDK runtime installer should be required for that artifact.

---

## Running the Unsigned App

Until the first signed release, pre-built DMGs are **unsigned and unnotarized**. To bypass Gatekeeper:

1. Right-click (or Control-click) `AzPin.app` → **Open**.
2. Click **Open** on the warning dialog.

You only need to do this once.

---

## How Pinning Works

**Pin an entire resource group** — all current and future resources in that RG appear in the menubar on every open. New resources show up automatically.

![Resource Group pinning](/docs/Pin_RG.png)

**Pin individual resources** — only those specific resources appear, even if their parent RG is not pinned.

![Individual Resource pinning](/docs/Pin_Resource.png)

Both modes coexist. If a resource is individually pinned and its parent RG is also pinned, it appears once (deduplication by resource ID). Pinned RGs are reorderable by drag in the main window.

![Resource Group reordering](/docs/Pin_Reorder.png)

---

## Menubar

Click the `☁` icon in the menubar to see:

1. Signed-in account and active subscription
2. Pinned resource groups, each expandable to show live resources
3. Runnable resources (App Services, Function Apps, Container Apps, Logic Apps) with a submenu for Start / Stop / Restart
4. Individually-pinned resources not belonging to a pinned RG — runnable ones show the same Start/Stop/Restart submenu as RG resources
5. Quick access to open the main window or quit

![Menu Bar, pinned RG](/docs/MenuBar_RG.png)

---

## Main Window

Open via **Open AzPin...** in the menubar or `⌘Space → AzPin`.

- **Sidebar**: pinned RGs, drag to reorder, right-click to unpin or open in Portal
- **Pinned tab**: individually-pinned resources for the selected RG, reorderable
- **Browse tab** (RG selected): live resources within that specific RG, sorted by type, with pin buttons
- **Browse view** (nothing selected): subscription picker and search field share a single toolbar row — browse all RGs and resources, pin whole RGs or individual resources
- **Settings → Subscriptions**: hide subscriptions you don't want cluttering the Browse picker

---

## Build from Source

Requires Xcode 26+ and the Xcode command-line tools.

```bash
# Debug build
xcodebuild -scheme AzPin -configuration Debug build | xcbeautify

# Run tests
xcodebuild -scheme AzPin -configuration Debug test | xcbeautify

# Release archive
xcodebuild -scheme AzPin -configuration Release \
  -archivePath build/AzPin.xcarchive archive | xcbeautify
```

---

## Contributing

1. Fork and clone.
2. Create a feature branch off `main`.
3. Follow the rules in `CLAUDE.md` — especially: no Azure SDK, no hardcoded colors, no paid dependencies, tests for every service method.
4. Open a PR against `main`.

---

## Key Docs

| File | Purpose |
|---|---|
| `CLAUDE.md` | Architecture rules and hard constraints |
| `AZPIN_SPEC.md` | Full product specification |
| `CHANGELOG.md` | Release history |
| `ROADMAP.md` | Planned future features |
| `RELEASE_PROCESS.md` | How to cut a release |

---

## License

[MIT](LICENSE)

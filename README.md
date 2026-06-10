# AzPin
![version](https://img.shields.io/badge/dynamic/regex?url=https://raw.githubusercontent.com/lfmundim/homebrew-tap/main/Casks/azpin.rb&search=version%20%22(.%2B)%22&label=brew)

<p align="center">
  <img src="assets/iconset/original.svg" alt="Logo" width="30%" />
</p>

AzPin is a native macOS menubar app that reads your existing `az` CLI session and gives you fast, pinnable access to Azure resources. Open the menubar, see your pinned resource groups and their live resources, click to open in the portal, or start/stop/restart runnable resources without leaving the desktop.

There is also a WinUI 3 Windows port under `src/windows/`, distributed as a self-contained zip from GitHub Releases.

No Azure SDK. No App Store. No sandbox. Requires macOS 26 Tahoe.

---

## Prerequisites

- **macOS 26 Tahoe** or later | **Windows 11** or later
- **[Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest)** installed
- Signed in: `az login`

---

## Install

### macOS - Homebrew (recommended)

```bash
brew tap lfmundim/tap
brew install --cask azpin
```

### macOS - DMG (manual)

Download the latest `.dmg` from [Releases](../../releases), drag `AzPin.app` to `/Applications`.

### Windows (manual)

Download the latest `AzPin-Windows-win-x64-*.zip` from [Releases](../../releases), extract, and run `AzPin.Windows.exe`.

No separate Windows App SDK runtime installer is required — the bundle is self-contained.

---

## How Pinning Works

**Pin an entire resource group** — all current and future resources in that RG appear in the menubar on every open. New resources show up automatically.

![Resource Group pinning](/docs/Pin_RG.png)
![Resource Group pinning on Windows](/docs/Pin_RG_Win.png)

**Pin individual resources** — only those specific resources appear, even if their parent RG is not pinned.

![Individual Resource pinning](/docs/Pin_Resource.png)
![Individual Resource pinning on Windows](/docs/Pin_Resource_Win.png)

Both modes coexist. If a resource is individually pinned and its parent RG is also pinned, it appears once (deduplication by resource ID). 

---

## MacOS Menubar / Windows TaskBar

Click the `☁` icon in the menubar to see:

1. Signed-in account and active subscription
2. Pinned resource groups, each expandable to show live resources
3. Runnable resources (App Services, Function Apps, Container Apps, Logic Apps) with a submenu for Start / Stop / Restart
4. Individually-pinned resources not belonging to a pinned RG — runnable ones show the same Start/Stop/Restart submenu as RG resources
5. Quick access to open the main window or quit

![Menu Bar, pinned RG](/docs/MenuBar_RG.png)
![Tray Icon, pinned RG](/docs/TrayIcon_RG.png)

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

### macOS

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

### Windows

Requires .NET 10 SDK and the Windows App SDK workload.

```powershell
# Restore
dotnet restore src/windows/AzPin.Windows/AzPin.Windows.csproj -p:Platform=x64

# Debug build
dotnet build src/windows/AzPin.Windows/AzPin.Windows.csproj -c Debug -p:Platform=x64

# Run tests
dotnet test src/windows/AzPin.Windows.sln

# Self-contained release bundle
dotnet publish src/windows/AzPin.Windows/AzPin.Windows.csproj `
  -c Release -r win-x64 -p:Platform=x64 `
  -p:WindowsAppSDKSelfContained=true --self-contained true `
  -o build/publish/win-x64
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

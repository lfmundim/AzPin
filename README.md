# AzPin

AzPin is a native macOS menubar app that reads the user's existing `az` CLI session and provides fast, pinnable access to Azure resources.

## Key Docs

- `CLAUDE.md` — code context, architecture rules, and hard constraints.
- `CHANGELOG.md` — human-readable release history. Update this for every meaningful change.
- `RELEASE_PROCESS.md` — release workflow and GitVersioning guidance.
- `AZPIN_SPEC.md` — full product spec.

## Versioning

- App versioning is managed by `GitVersioning`.
- Keep major version at `0` for now.
- Start with minor version `1` on the first feature branch.

## Running the Unsigned App

Pre-built DMGs are attached to each [GitHub Release](../../releases). Until basic functionality is complete, releases are **unsigned and unnotarized** — Apple Gatekeeper will block the app by default. To run it anyway:

1. Download the `.dmg` from the release and mount it.
2. Drag `AzPin.app` to `/Applications`.
3. **Do not double-click.** Right-click (or Control-click) `AzPin.app` → **Open**.
4. macOS will warn that the app is from an unidentified developer. Click **Open** to proceed.

You only need to do this once. After the first manual open, macOS remembers the exception and the app launches normally.

> **Why unsigned?** Code signing and notarization require a paid Apple Developer account ($99/yr). AzPin will be signed once core functionality is stable. In the meantime, the right-click workaround is the standard way to run unsigned macOS apps from developers you trust.

## Build

```bash
xcodebuild -scheme AzPin -configuration Debug build | xcbeautify
```

## Testing

```bash
xcodebuild -scheme AzPin -configuration Debug test | xcbeautify
```

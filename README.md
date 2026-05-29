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

## Build

```bash
xcodebuild -scheme AzPin -configuration Debug build | xcbeautify
```

## Testing

```bash
xcodebuild -scheme AzPin -configuration Debug test | xcbeautify
```

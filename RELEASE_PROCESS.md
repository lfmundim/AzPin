# Release Process and Versioning

## Changelog Rules

- Every code change must be reflected in `CHANGELOG.md`.
- Keep entries concise and factual.
- Do not update the version line in `CHANGELOG.md` while the branch is still in progress.
- If the branch contains multiple related changes, group them under the same unreleased or version entry.

## GitVersioning

- App versioning is managed by `GitVersioning`.
- Do not manually set the app version in source or project files.
- Let `GitVersioning` derive the version from Git tags, branch names, and commit history.
- Keep major version at `0` for now. Major only increments when the user explicitly specifies it.
- Start with `minor` version `1` on the first feature branch.
- The changelog documents what changed; GitVersioning documents which commit/version is built.

## Branch Workflow

- Work on a feature or fix in a branch without bumping the changelog version.
- Add changelog entries as the branch evolves.
- When the branch is ready for release, align the changelog version with the GitVersioning release version.
- Release tooling/CI should use GitVersioning to stamp the build.
- All release branches use `release/v*`.
- The pipeline detects changed paths and builds only the affected platform(s): macOS if `src/macos/**` changed, Windows if `src/windows/**` changed, both if needed.

## Windows Deployment

- Windows CI runs on GitHub-hosted Windows runners and is the verification path for the WinUI project.
- Pushing to `release/v*` with changes under `src/windows/**` triggers the Windows build job.
- That workflow builds, publishes a self-contained `win-x64` unpackaged bundle, packages it as a zip, and builds a signed MSI installer via WiX v4.
- Both the zip and the MSI are uploaded as workflow artifacts and attached to a GitHub pre-release tagged `win-v*`.
- After the release is created, the pipeline automatically submits a PR to `microsoft/winget-pkgs` via `wingetcreate update`, updating the `KimDim.AzPin` manifest to the new version.
- To verify on a Windows VM: download the MSI and install it, or download the zip, extract, and run `AzPin.Windows.exe` directly.

## winget — Initial Setup (one-time)

The automated winget PR step requires the package to already exist in `microsoft/winget-pkgs`. This is a one-time manual step:

1. Ensure a release with the MSI exists on GitHub.
2. Hand-write the three manifest files (or use `wingetcreate new` on Windows) and open a PR to `microsoft/winget-pkgs` under `manifests/k/KimDim/AzPin/<version>/`:
   - `KimDim.AzPin.yaml`
   - `KimDim.AzPin.installer.yaml`
   - `KimDim.AzPin.locale.en-US.yaml`
3. Wait for Microsoft review and merge.
4. All subsequent releases are handled automatically by the pipeline.

The pipeline uses a `WINGET_TOKEN` GitHub secret (PAT with `public_repo` scope) to open PRs on `microsoft/winget-pkgs`.

## Practical Guidance

- If you need a new release version, create a Git tag or otherwise follow the configured GitVersioning workflow.
- Do not treat `CHANGELOG.md` as the version source. It is a human-readable history document.
- `GitVersioning` is the single source of truth for the app version.

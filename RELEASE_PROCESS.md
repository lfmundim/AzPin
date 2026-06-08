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
- macOS release branches use `release/v*`.
- Windows preview release branches use `release/win/v*`.

## Windows Preview Deployment

- Windows CI runs on GitHub-hosted Windows runners and is the verification path for the WinUI project.
- Pushing to `release/win/v*` triggers the Windows release workflow.
- That workflow builds, tests, publishes a self-contained `win-x64` unpackaged bundle, uploads it as a workflow artifact, and attaches the zip to a GitHub pre-release tagged `win-v*`.
- To verify on a Windows VM, download the release zip, extract it, and run `AzPin.Windows.exe`.

## Practical Guidance

- If you need a new release version, create a Git tag or otherwise follow the configured GitVersioning workflow.
- Do not treat `CHANGELOG.md` as the version source. It is a human-readable history document.
- `GitVersioning` is the single source of truth for the app version.

# Skill: changelog-update

Invoked via `/changelog-update`. Updates `CHANGELOG.md` based on completed tasks and the current PR diff.

---

## Steps

### Step 1: Ask which tasks were completed

Ask the user:

> "Which tasks did you complete on this PR? List the task file names or numbers (e.g., 1.2, 1.3, 1.4) or describe what was done."

Wait for the user's response before proceeding.

### Step 2: Fetch the PR diff

Determine the current branch and find the associated open PR:

```bash
git branch --show-current
gh pr list --state open --head "$(git branch --show-current)" --json number,title --jq '.[0]'
```

If a PR number is found, fetch its diff:
```bash
gh pr diff {PR_NUMBER}
```

If no PR is open yet, use the local diff against main:
```bash
git diff main...HEAD
```

### Step 3: Read the task files

For each task number the user mentioned, read the corresponding file from `src/macos/tasks/`. For example, if the user says "1.2 and 1.4", read:
- `src/macos/tasks/1.2_wire_app_entry_point.md`
- `src/macos/tasks/1.4_auth_view_model.md`

### Step 4: Cross-reference tasks and diff

Compare what the task files say should be built against what the diff shows was actually changed. The goal is to write accurate changelog entries — not copy-paste the task goals, but describe what was actually done based on the code.

For each task completed, identify:
- What new files were added
- What existing files were modified and how
- What behavior changed from the user's perspective

### Step 5: Update CHANGELOG.md

Read `CHANGELOG.md` and `RELEASE_PROCESS.md` first to understand the current format and rules.

Add entries under the `## [Unreleased]` section. Follow these rules from `RELEASE_PROCESS.md`:
- Keep entries concise and factual
- Do not update the version line — keep it as `[Unreleased]`
- Do not repeat entries already in the file
- Group related changes under a single bullet if appropriate
- Describe behavior changes from the user's perspective, not code mechanics

Example good entries:
```markdown
- MenuBarView now shows live az CLI auth status (signed in / not signed in).
- AzureTokenResponse date decoding fixed for az CLI date string format.
- Services wired into SwiftUI environment via AzPinApp entry point.
```

Example bad entries:
```markdown
- Modified AzPinApp.swift to add ModelContainer and inject services.
- Fixed DateFormatter in fetchToken function.
```

### Step 6: Confirm with the user

Show the proposed changelog additions and ask: "Does this look right? I'll update CHANGELOG.md if you confirm."

Make the edit only after confirmation.

---

## Notes

- Never bump the version number in CHANGELOG.md. That happens only at release time.
- If the diff shows code that doesn't match any task the user mentioned, ask about it before adding a changelog entry.
- If a task was only partially completed, describe only what was actually done.
- The `gh` CLI must be authenticated (`gh auth status`) for PR diff fetching to work.

# Repository Recommendations

This document records safe cleanup and repo-management recommendations for the `submitted/` project so they can be revised after each step.

## Current Status

- `submitted/` is a nested Git repository with its own remote: `VisualEducationSystem.git`
- the nested repo is synced with `origin/main`
- local documentation changes exist and should be reviewed before commit/push
- large Unity-generated cache/build folders were already removed safely

## Keep

Keep these because they are source, submission, or editable project files:

- `submitted/README.md`
- `submitted/docs/`
- `submitted/VisualEducationSystem/Assets/`
- `submitted/VisualEducationSystem/Packages/`
- `submitted/VisualEducationSystem/ProjectSettings/`
- `submitted/.git/` while `submitted/` is still managed as its own repo
- editable deliverables such as `.md`, `.pdf`, `.pptx`, and any media you still need

## Safe To Remove When Needed

These are usually generated outputs and can be deleted if disk space is needed:

- `submitted/VisualEducationSystem/Library/`
- `submitted/VisualEducationSystem/Temp/`
- `submitted/VisualEducationSystem/Logs/`
- `submitted/VisualEducationSystem/profile-*.app`
- `submitted/VisualEducationSystem/*BurstDebugInformation_DoNotShip`
- `.DS_Store`

Expected effect:

- Unity will regenerate needed cache/build files next time the project opens
- the first reopen/rebuild may be slower
- source code and assets are not harmed

## Use Caution

Review before deleting these:

- `submitted/VES-Memory-Palace-Phase-2-Mar-11-Demo.mov`
- `submitted/docs/Storyboard-Concept-Slides.pptx`
- `submitted/docs/Meeting-2-Docs.zip`
- generated export PDFs if they are the only final copies you need

These files are not required for Unity to run, but they may still matter for submission, presentation, or archival use.

## Recommended Process

1. Check nested repo status with `git -C submitted status --short --branch`
2. Remove only generated folders/files first
3. Recheck size and project structure
4. Open Unity only if needed and allow cache regeneration
5. Update this document with what was removed and why
6. Commit/push nested repo changes only after confirming submission files are correct

## Change Log

### 2026-03-25

- Removed generated Unity folders and outputs:
  - `Library/`
  - `Logs/`
  - most of `Temp/`
  - `profile-1.app`
  - `VisualEducationSystem_BurstDebugInformation_DoNotShip`
- Kept source folders, docs, media, and nested Git metadata
- Confirmed `submitted/.git` is a real nested repository and should remain for now
- Removed local Finder metadata files:
  - `docs/.DS_Store`
  - `VisualEducationSystem/.DS_Store`
- Current size snapshot after cleanup:
  - `submitted/`: about `389M`
  - `submitted/docs/`: about `26M`
  - `submitted/VisualEducationSystem/`: about `1.2M`
- Current nested repo state still includes local documentation changes and one Unity settings change to review before commit

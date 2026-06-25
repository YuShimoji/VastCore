# VastCore Remote Resume Handoff - 2026-06-25

This file is intentionally ASCII-safe so another terminal can read it through
Windows PowerShell without encoding loss.

## Resume Target

| item | value |
| --- | --- |
| repository | `YuShimoji/VastCore` |
| remote | `origin` = `https://github.com/YuShimoji/VastCore.git` |
| worktree used for diagnostics | `C:\Users\PLANNER007\VastCore\VastCore-origin-main-parent-20260625` |
| branch to resume | `codex/vc-rst-2e-upm-root-cause` |
| base before local report commit | `39f790c4651718c03a8e64628fcbd3dd1def0b44` |
| handoff worktree | `C:\Users\PLANNER007\VastCore\VastCore` |
| handoff branch | `codex/vc-rst-remote-handoff-20260622` |
| stale worktree to avoid | `C:\Users\PLANNER007\VastCore\VastCore-origin-main-compile` |

Use the diagnostic parent worktree for UPM/package-manager continuation. Do not
run Unity/package diagnostics from the handoff worktree or the stale compile
worktree.

## Current Blocker

Unity Package Manager fails before C# compile with:

```text
Failed to resolve packages: The "path" argument must be of type string. Received undefined.
No packages loaded.
```

The failure reproduces in both VastCore and a clean control project, so current
evidence points away from VastCore package contents and toward user-level Unity
state, Unity Package Manager state, or Unity installation/global state.

## Files Preserving Context

| file | role |
| --- | --- |
| `docs/restart/VC_ORIGIN_MAIN_PARENT_BOOTSTRAP_REPORT.md` | Parent worktree/bootstrap context |
| `docs/restart/VC_PACKAGE_MANAGER_RESTORATION_REPORT.md` | Earlier package restoration attempts and reverted candidates |
| `docs/restart/VC_PACKAGE_MANAGER_ROOT_CAUSE_REPORT.md` | T+2e root-cause diagnostics |
| `docs/restart/VC_UPM_ENVIRONMENT_ISOLATION_REPORT.md` | T+2f control-project and environment isolation |
| `docs/restart/VC_UPM_ACL_REGENERATION_REPORT.md` | T+2g ACL regeneration gate; blocked by active Unity |
| `docs/restart/VC_REMOTE_RESUME_HANDOFF_20260625.md` | This remote resume handoff |

## Diagnostic Artifacts

Some artifact/log paths may be git-ignored locally. The reports above preserve
the important conclusions. Local artifact paths used during the investigation:

| path | purpose |
| --- | --- |
| `artifacts/restart/t2f-acl-backup-ledger.json` | ACL XML/etag backup ledger |
| `artifacts/restart/t2f-machine-backups/` | Backup copies of Unity package ACL XML/etag |
| `artifacts/restart/t2g-process-safety-check.json` | Redacted process snapshot; `aclRenameSafe=false` |
| `artifacts/logs/t2f-*` | Control/VastCore UPM logs from environment isolation |

Do not expose raw Unity Hub or Editor access tokens. The T+2g process snapshot
was rewritten with the token redacted.

## Safety State

At the T+2g gate, ACL rename/regeneration was not performed because these
processes were active for another project:

| process | pid | project/path hint | decision |
| --- | ---: | --- | --- |
| `Unity.exe` | 40112 | `C:\Users\PLANNER007\Desktop\Soft\Kakuninnyou\galsurvival` | blocks ACL rename |
| `UnityPackageManager.exe` | 37512 | server process for PID `40112` | blocks ACL rename |
| `Unity.exe` AssetImportWorker1 | 38444 | `galsurvival` | blocks ACL rename |
| `Unity.exe` AssetImportWorker2 | 3448 | `galsurvival` | blocks ACL rename |

Before attempting ACL regeneration again, re-check active Unity/UPM processes.
If any active Unity/UPM process remains for a user project, do not rename ACL.

## What Was Not Changed

| area | state |
| --- | --- |
| `Packages/manifest.json` | no final diff |
| `Packages/packages-lock.json` | no final diff |
| major `ProjectSettings/` files | no final diff |
| runtime/editor C# | not modified |
| scenes/prefabs/materials/textures/meshes/imported assets | not modified |
| machine-level Unity ACL files | not renamed or deleted in T+2g |
| handoff branch worktree | left clean |

## Recommended Next Move

Default next slice while Unity remains active:

```text
VC-RST-2h-upm-editor-install-or-global-state-repair-decision
```

If the user closes the `galsurvival` Unity Editor first, rerun the ACL gate:

1. Verify no active Unity Editor / UnityPackageManager / AssetImportWorker
   process remains.
2. Backup `packageAccessControlList.xml` and `.etag` again.
3. Rename them reversibly, for example with a `.t2g-disabled` or newer suffix.
4. Open/retest the clean control project first.
5. Retest VastCore only if the control project improves.
6. If UPM reaches C# compile, stop package work and start the C# compile gate.

## Do Not Start

Do not start these until UPM resolves packages:

| area | reason |
| --- | --- |
| C# compile fixes | C# compile is not reached |
| terrain architecture | outside this restoration slice |
| DualGrid / mining / CSG / EasyRoads | explicitly out of scope |
| Simulator split / Trail / player controller | explicitly out of scope |
| product package removals | control project shows failure is not VastCore package-specific |
| system-wide Unity installation edits | requires a separate repair decision |

## Quick Verification Commands

Run from `C:\Users\PLANNER007\VastCore\VastCore-origin-main-parent-20260625`:

```powershell
git status --short --branch --untracked-files=all
git rev-parse HEAD
git diff --name-status -- Packages ProjectSettings Assets
```

Run from `C:\Users\PLANNER007\VastCore\VastCore`:

```powershell
git status --short --branch
```

Expected before the next diagnostic action:

| check | expected |
| --- | --- |
| parent branch | `codex/vc-rst-2e-upm-root-cause` |
| parent package/project diffs | none |
| handoff branch | clean |
| active Unity/UPM | none before ACL rename |


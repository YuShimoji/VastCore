# VC UPM Environment Isolation Report

Route:
`VastCore | VC-RST-2f-upm-environment-or-version-isolation | target: YuShimoji/VastCore@origin-main-parent-20260625`

Last updated: 2026-06-25

Review Card: not emitted.
reason: this slice is UPM environment isolation, not a user-facing artifact review.
next_nonredundant_axis: environment repair decision because UPM still does not resolve.

## 1. Current State

| item | value |
| --- | --- |
| Worktree path | `C:\Users\PLANNER007\VastCore\VastCore-origin-main-parent-20260625` |
| Branch / HEAD | `codex/vc-rst-2e-upm-root-cause` at `39f790c4651718c03a8e64628fcbd3dd1def0b44` |
| Unity versions used | `6000.3.6f1`, `6000.4.9f1` |
| Starting diff | known docs-only dirty state; no starting diff in `Packages/` or required `ProjectSettings/` |
| Active blocker | Unity Package Manager fails before package resolution with `The "path" argument must be of type string. Received undefined` |
| C# compile | not reached |
| Handoff branch untouched | yes |

T+2f moved the diagnosis from "maybe VastCore package files" to "machine/user Unity state or Unity installation behavior." A brand-new control project with an empty manifest fails with the same Package Manager error.

## 2. Machine UPM State Snapshot

| item | path/value | finding | risk | action |
| ---- | ---------- | ------- | ---- | ------ |
| User profile | `C:\Users\PLANNER007` | Current user profile used for Unity cache/license files | medium | Snapshot recorded |
| Unity editors | `6000.0.32f1`, `6000.0.59f2`, `6000.2.2f1`, `6000.3.0b2`, `6000.3.3f1`, `6000.3.6f1`, `6000.4.9f1` | Multiple Unity 6000 installs available | medium | Tested `6000.3.6f1` and `6000.4.9f1` |
| UPM cache | `C:\Users\PLANNER007\AppData\Local\Unity\cache\upm` | Exists; only `db` child visible in snapshot | medium | Read-only snapshot; process-local `UPM_CACHE_ROOT` test run |
| Package ACL | `C:\Users\PLANNER007\AppData\Local\Unity\licenses\packages\packageAccessControlList.xml` | Exists; 41 package ids; one empty `<Package Id=""/>` | high | Backed up; rename not run while another Unity editor is active |
| Package ACL etag | `C:\Users\PLANNER007\AppData\Local\Unity\licenses\packages\packageAccessControlList.etag` | Exists | medium | Backed up |
| UPM config files | `.upmconfig.toml`, `.npmrc`, AppData Unity config candidates | Not present | low | No action |
| Environment variables | `UPM_*`, proxy, node/npm cache vars | Unset at baseline | low | Process-local cache env tested |
| Active Unity session | `C:\Users\PLANNER007\Desktop\Soft\Kakuninnyou\galsurvival` | User-facing Unity editor and UPM process are running | high | Blocked global ACL/cache rename tests to avoid side effects |

Snapshot artifact:

- `artifacts/restart/t2f-machine-state-snapshot.json`

## 3. Control Project Test

| control project | Unity version | manifest shape | result | reached stage | log |
| --------------- | ------------- | -------------- | ------ | ------------- | --- |
| `C:\Users\PLANNER007\VastCore\_upm-control\control-6000_3_6f1-20260625-173452` | `6000.3.6f1` | Created by Unity; `Packages/manifest.json` contains empty `dependencies` object | `-createProject` returned exit 1 | AssetDatabase initial refresh, then UPM `project:update-dependencies --> 500` | `artifacts/logs/t2f-control-create-6000_3_6f1/create-project.log` |
| Same control project | `6000.3.6f1` | Empty dependencies | exit 1 | AssetDatabase initial refresh, then Package Manager manifest update failure | `artifacts/logs/t2f-control-open-6000_3_6f1/control-open.log` |
| Same control project | `6000.3.6f1` | Empty dependencies; process env `UPM_NPM_CACHE_PATH` and ignored `UPM_CACHE_PATH` | exit 1 | Same Package Manager manifest update failure | `artifacts/logs/t2f-control-open-clean-env-cache/control-open-clean-env-cache.log` |
| Same control project | `6000.3.6f1` | Empty dependencies; process env `UPM_CACHE_ROOT` set to clean folder | exit 1 | Same Package Manager manifest update failure | `artifacts/logs/t2f-control-open-upm-cache-root/control-open-upm-cache-root.log` |
| Same control project | `6000.4.9f1` | Empty dependencies | exit 1 | `project:list-packages --> 500`, `project:update-dependencies --> 500` | `artifacts/logs/t2f-control-open-6000_4_9f1/control-open-6000_4_9f1.log` |

Interpretation: the failure is not VastCore-specific. It reproduces on a clean control project with an empty manifest.

## 4. ACL / Cache Regeneration Tests

| target | backup path | action | result | restored/retained | conclusion |
| ------ | ----------- | ------ | ------ | ----------------- | ---------- |
| `packageAccessControlList.xml` | `artifacts/restart/t2f-machine-backups/20260625-173840-packageAccessControlList.xml` | Backup only | Backup length matches source | Original retained | Rename/regeneration not run because active user Unity/UPM session could be affected |
| `packageAccessControlList.etag` | `artifacts/restart/t2f-machine-backups/20260625-173840-packageAccessControlList.etag` | Backup only | Backup length matches source | Original retained | Safe rollback material exists for next slice |
| UPM cache root | process-local `C:\Users\PLANNER007\VastCore\_upm-control\cache-root-test` | Set `UPM_CACHE_ROOT` only for control Unity process | Same error | No machine-level file changed | Normal UPM package cache root is unlikely to be the sole cause |
| VastCore UPM cache root | process-local `C:\Users\PLANNER007\VastCore\_upm-control\vastcore-cache-root-test` | Set `UPM_CACHE_ROOT` only for VastCore check | Same error | No machine-level file changed | Confirms clean cache root does not unblock VastCore |

The explicitly authorized machine-level rename test was not executed because another Unity Editor session and its UPM process were active for a different project. Touching global Unity license/cache state while that session is live would risk side effects outside this slice.

## 5. VastCore Retest Results

| candidate | action | result | reached stage | conclusion |
| --------- | ------ | ------ | ------------- | ---------- |
| T+2f VastCore clean cache root | Ran `scripts/check-compile.ps1` with process-local `UPM_CACHE_ROOT` | exit 1 with same `Failed to resolve packages: The "path" argument... No packages loaded.` | UPM `project:resolve-packages --> 500` | VastCore remains blocked; UPM cache root is not enough |
| Existing T+2e baseline | Original package files | exit 1 with same failure | UPM resolution only | Still valid baseline |

No C# compiler errors were reached.

## 6. Project Path / Project State Isolation

| test | result | conclusion | next |
| ---- | ------ | ---------- | ---- |
| Clean control project under `C:\Users\PLANNER007\VastCore\_upm-control\...` | Same `path undefined` failure | VastCore-specific assets, package files, and ProjectSettings are not required to reproduce | Move to environment repair |
| Empty manifest control | Same failure | Package group and package manifest contents are not sufficient root cause | Do not continue package bisection |
| Unity `6000.4.9f1` control | Same failure with `project:list-packages` / `project:update-dependencies` 500 | Not unique to `6000.3.6f1` | Consider Unity Hub/license/cache repair rather than one project edit |
| Active external Unity session | Found `galsurvival` editor and UPM running | Machine-level ACL rename would not be isolated | Close/stop external Unity before ACL regeneration test |

## 7. Hypothesis Update

| hypothesis | prior status | T+2f evidence | updated result | next |
| ---------- | ------------ | ------------- | -------------- | ---- |
| H4 Unity version/install behavior | possible | Both `6000.3.6f1` and `6000.4.9f1` fail on control project | More likely install/user-state class than single-version bug | Run repair decision after safe ACL test |
| H5 local generated/cache state | unlikely inside VastCore | Clean `UPM_CACHE_ROOT` still fails for control and VastCore | Unlikely as normal UPM cache root issue | Do not repeat local cache cleanup |
| H6 ProjectSettings / PackageManagerSettings | weakened | Clean control project with default settings also fails | Very unlikely as VastCore ProjectSettings cause | Stop ProjectSettings edits |
| H8 project path / machine environment | likely | New project path also fails | Machine/user environment strongly implicated | Proceed to environment repair |
| H9 packageAccessControlList / package ACL corruption | suspicious | ACL has empty Package Id; control project fails before package import; backup succeeded | Strong remaining candidate, not proven | Close active Unity then rename/regenerate ACL |
| H10 Unity Editor install / UPM binary issue | possible | UPM fails across versions, but licensing resolves and UPM binary runs to 500 | Possible if ACL test fails | Consider Unity Hub/Editor repair or reinstall decision |

## 8. Final State

- Changed files added:
  - `docs/restart/VC_UPM_ENVIRONMENT_ISOLATION_REPORT.md`
- Existing docs from prior slices still dirty/untracked:
  - `docs/restart/VC_PACKAGE_MANAGER_RESTORATION_REPORT.md`
  - `docs/restart/VC_ORIGIN_MAIN_PARENT_BOOTSTRAP_REPORT.md`
  - `docs/restart/VC_PACKAGE_MANAGER_ROOT_CAUSE_REPORT.md`
- Machine-level files:
  - `packageAccessControlList.xml` backed up and left in place.
  - `packageAccessControlList.etag` backed up and left in place.
  - No machine-level files renamed or deleted.
- Control project:
  - Created under `C:\Users\PLANNER007\VastCore\_upm-control\control-6000_3_6f1-20260625-173452`.
- Package/ProjectSettings state:
  - No final diff in `Packages/manifest.json`.
  - No final diff in `Packages/packages-lock.json`.
  - No final diff in major `ProjectSettings`.
- UPM reached:
  - UPM IPC request handling.
  - Control: `project:update-dependencies --> 500`.
  - VastCore: `project:resolve-packages --> 500`.
- UPM did not reach:
  - successful package resolution.
  - loaded packages.
  - C# compile.
- Handoff branch remained untouched.

## 9. Environment Repair Card

| field | value |
| --- | --- |
| status | required |
| target | Unity user-level package ACL / licensing package access state |
| why | Clean control project with empty manifest fails the same way as VastCore; normal UPM cache root override does not help; ACL file contains one empty package id |
| safest action | Close all Unity Editor instances, then rename `C:\Users\PLANNER007\AppData\Local\Unity\licenses\packages\packageAccessControlList.xml` and `.etag` after backup, launch control project to let Unity regenerate package access state |
| rollback | Restore backed-up XML/etag from `artifacts/restart/t2f-machine-backups/` if the regenerated state does not improve UPM |
| user_work | Close the active Unity project `C:\Users\PLANNER007\Desktop\Soft\Kakuninnyou\galsurvival` or explicitly authorize stopping those Unity/UPM processes |
| confidence | medium |

If ACL regeneration does not improve the control project, the next repair decision should move to Unity Hub/license client state or Editor/UPM installation repair.

## 10. Decision Packet

Recommended default:

- Do not modify VastCore package files.
- Do not start C# or terrain work.
- Close active Unity sessions, then run a scoped ACL rename/regeneration test.
- If ACL regeneration succeeds on control project, retest VastCore and stop at first C# compile blocker.

Alternatives:

| option | benefit | cost | when to choose |
| --- | --- | --- | --- |
| ACL regeneration after closing Unity | Tests strongest remaining causal signal | Requires user to close active Unity | Best next move |
| Unity Hub/license repair | Addresses licensing/package access state broadly | More invasive and user-visible | Choose if ACL regeneration fails |
| Unity Editor repair/reinstall | Addresses UPM binary/install corruption | Slow and broader system change | Choose after user-state repair fails |
| New Windows user profile control | Cleanly separates user AppData state | High setup overhead | Choose if repair risk must be minimized |

Rejected options:

- Package removals or package bisection.
- Runtime/editor C# fixes.
- Terrain/architecture work.
- Machine-level rename while another Unity Editor/UPM is active.
- System-wide Unity installation file edits.

Confidence:

- High that this is not VastCore package/project content.
- High that normal UPM cache root is not the sole cause.
- Medium that package ACL/licensing user state is the next best target.
- Medium-low on exact repair until ACL regeneration is tested with Unity closed.

Remaining unknowns:

- Whether the empty `<Package Id=""/>` is causal.
- Whether the active Unity session is holding stale package/license state.
- Whether Unity Hub/license client repair is needed after ACL regeneration.

## Completion Matrix / Done Gates

| gate | done | total | unknown | meter | missing |
| --- | ---: | ---: | ---: | --- | --- |
| Worktree verified | 5 | 5 | 0 | `[#####] 5/5` | none |
| Machine UPM snapshot | 6 | 6 | 0 | `[######] 6/6` | none |
| Control project test | 5 | 5 | 0 | `[#####] 5/5` | none |
| ACL/cache test | 4 | 6 | 1 | `[####--] 4/6` | ACL rename/regenerate blocked by active Unity |
| Path/project isolation | 5 | 5 | 0 | `[#####] 5/5` | none |
| Validation | 3 | 5 | 0 | `[###--] 3/5` | UPM resolution, C# compile/tests |
| Report hygiene | 8 | 8 | 0 | `[########] 8/8` | none |

## Review Card / Review Debt

Review Card: not emitted.

Reason: this slice is UPM environment isolation, not a user-facing artifact review.

Review debt:

- ACL rename/regeneration is prepared but not executed because an unrelated Unity Editor session is active.
- Control project remains in `_upm-control` for repeatable retests.

## Command / Action Ledger

| action | result |
| --- | --- |
| Verified parent branch/head/status | valid branch `codex/vc-rst-2e-upm-root-cause`, HEAD `39f790c` |
| Reviewed T+2e root-cause report | confirmed prior package-file conclusions |
| Created machine-state snapshot | `artifacts/restart/t2f-machine-state-snapshot.json` |
| Created clean control project | directory created; Unity returned exit 1 with UPM path error |
| Opened control project with `6000.3.6f1` | exit 1, same error |
| Opened control project with clean env cache using old vars | exit 1; UPM says `UPM_CACHE_PATH` ignored |
| Opened control project with `UPM_CACHE_ROOT` | exit 1, same error |
| Opened control project with `6000.4.9f1` | exit 1, same error |
| Backed up package ACL XML and etag | backup succeeded |
| Checked active Unity/UPM processes | active unrelated `galsurvival` Unity session found |
| Retested VastCore with clean `UPM_CACHE_ROOT` | exit 1, same error |

## User-Side Work

Close the active Unity Editor project at `C:\Users\PLANNER007\Desktop\Soft\Kakuninnyou\galsurvival`, or explicitly authorize the next agent to stop those Unity/UPM processes before the ACL rename/regeneration test.

No product package decision is needed.

## Agent-Side Work

Next agent-side entry points:

| entry | reduces | enables |
| --- | --- | --- |
| ACL regeneration test | uncertainty around empty package ACL id | determine whether user-level Unity package access state is causal |
| Control retest after ACL regeneration | noise from VastCore project state | decide if UPM environment is repaired |
| VastCore retest after control passes | transition from environment layer to repo layer | reach C# compile gate |
| Editor/license repair decision | remaining install/user-state uncertainty | decide Unity Hub repair/reinstall only if needed |

## Goal Stack

| horizon | state |
| --- | --- |
| Immediate | Control project proves machine/user state involvement |
| Short-term | ACL regeneration is next blocked action |
| Mid-term | Make UPM resolve packages |
| Long-term | Move to `VC-RST-3-csharp-compile-restoration-gate` only after UPM resolves |

## Turn Calendar

| turn | expected move |
| --- | --- |
| T+2f | Completed environment isolation; stopped before unsafe global rename |
| T+2g | ACL regeneration or Unity environment repair decision |
| T+3 | C# compile restoration if UPM resolves |

## Visual Summary

```text
Clean control project     [FAIL] same UPM path error
VastCore project          [FAIL] same UPM path error
UPM_CACHE_ROOT clean      [FAIL] same UPM path error
Unity 6000.4.9f1 control  [FAIL] same UPM path error
ACL backup                [ OK ] ready
ACL rename/regenerate     [WAIT] active Unity session
C# compile                [----] not reached
```

## Continuation State

Continue from:

- `C:\Users\PLANNER007\VastCore\VastCore-origin-main-parent-20260625`
- branch `codex/vc-rst-2e-upm-root-cause`
- control project `C:\Users\PLANNER007\VastCore\_upm-control\control-6000_3_6f1-20260625-173452`

Recommended next slice:

- `VC-RST-2g-upm-environment-repair-or-editor-reinstall-decision`

Do not continue into:

- C# fixes.
- terrain architecture.
- package removal.


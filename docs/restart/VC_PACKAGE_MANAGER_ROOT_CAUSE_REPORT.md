# VC Package Manager Root Cause Report

Route:
`VastCore | VC-RST-2e-package-manager-root-cause-fix | target: YuShimoji/VastCore@origin-main-parent-20260625`

Last updated: 2026-06-25

Review Card: not emitted.
reason: this slice is UPM root-cause diagnostics, not a user-facing artifact review.
next_nonredundant_axis: environment/version isolation if UPM still fails, C# compile restoration only if UPM resolves.

## 1. Current State

| Field | Value |
| --- | --- |
| Worktree path | `C:\Users\PLANNER007\VastCore\VastCore-origin-main-parent-20260625` |
| HEAD | `39f790c4651718c03a8e64628fcbd3dd1def0b44` |
| Branch state | local diagnostic branch `codex/vc-rst-2e-upm-root-cause`, based exactly on `origin/main` `39f790c4651718c03a8e64628fcbd3dd1def0b44` |
| Unity version used for baseline | `6000.3.6f1 (bbb010bdb8a3)` |
| Additional Unity version diagnostic | `6000.4.9f1 (f7258d6eebbe)` with minimal manifest only |
| Starting diff classification | known docs-only dirty state from prior slices; no starting diff in `Packages/` or required `ProjectSettings/` files |
| Previous T+2d blocker | `Failed to resolve packages: The "path" argument must be of type string. Received undefined. No packages loaded.` |
| Current blocker after T+2e | Same UPM path error persists with original package files, built-in-only manifest, missing `PackageManagerSettings.asset`, targeted generated-state cleanup, and Unity `6000.4.9f1` minimal-manifest diagnostic |
| C# compile stage | not reached |
| Handoff branch | not used for Unity/package diagnostics |

The blocker is now classified as a pre-C# Unity Package Manager failure that is not explained by the known git packages, the lockfile alone, ProjectSettings Package Manager registry state alone, or worktree-local generated state alone.

## 2. Deep Log Evidence

| source | path | finding | relation to `path undefined` |
| ------ | ---- | ------- | ---------------------------- |
| Baseline Unity log | `artifacts/logs/t2e-baseline/compile-check.log` | Unity `6000.3.6f1` starts the project, reaches `Application.AssetDatabase Initial Refresh Start`, then reports `Done resolving packages with errors in 0.05 seconds` and the exact `path` error | Confirms current parent still fails before package resolution succeeds |
| Baseline UPM log | `artifacts/restart/t2e-baseline-upm.log` | `project:resolve-packages --> 500 (27 ms)` with no stack trace | Confirms the failure originates inside UPM resolver request handling |
| Minimal built-in manifest log | `artifacts/logs/t2e-minimal-builtin/compile-check.log` | Built-in modules only, no lockfile, still reports the same `path` error | Strongly weakens package-group and git-package hypotheses |
| Generated-state cleanup log | `artifacts/logs/t2e-minimal-generated-clean/compile-check.log` | After deleting `Library/ScriptAssemblies` with no `Library/PackageCache` or `Library/PackageManager` present, same error remains | Weakens worktree-local generated-state hypothesis |
| No PackageManagerSettings log | `artifacts/logs/t2e-minimal-no-pmsettings/compile-check.log` | Removing `ProjectSettings/PackageManagerSettings.asset` temporarily still yields same error | Weakens scoped registry / registry settings hypothesis |
| Unity `6000.4.9f1` minimal log | `artifacts/logs/t2e-minimal-unity6000_4_9f1/compile-check.log` | Same `path` error appears as `Failed to update project manifest: The "path" argument must be of type string. Received undefined` | Shows the failure is not unique to `6000.3.6f1`; also exposes a manifest-update request path |
| Unity `6000.4.9f1` UPM log | `artifacts/restart/t2e-minimal-unity6000_4_9f1-upm.log` | `project:list-packages --> 500` and `project:update-dependencies --> 500` | Gives a deeper failing operation than the `6000.3.6f1` baseline |
| Environment review | `artifacts/restart/t2e-environment-review.json` | No UPM env vars, no `.upmconfig.toml`, no `.npmrc`; machine-level package access control list exists and contains one empty package id | Points remaining suspicion toward machine-level Unity license/package cache state or UPM internal state |

## 3. Package Manager Settings Review

| file/setting | finding | risk | action |
| ------------ | ------- | ---- | ------ |
| `ProjectSettings/PackageManagerSettings.asset` | Default registry only: `https://packages.unity.com`; no scoped registries; no proxy/cache path field visible | Low to medium | Preserved, temporarily removed, then restored |
| `m_Registries[0].m_Name` | Empty name on default registry | Low | Tested by removing the whole settings file; same error persisted |
| `m_UserSelectedRegistryName` | Empty | Low | No action retained |
| `ProjectSettings/EditorSettings.asset` | `m_CacheServerMode: 0`, empty `m_CacheServerEndpoint` | Low | Read-only |
| `ProjectSettings/ProjectSettings.asset` | `templatePackageId:` is empty; many unrelated platform path fields are empty | Medium-low | Read-only in this slice; not enough evidence to edit |
| User-level UPM config | `.upmconfig.toml`, `.npmrc`, AppData Unity UPM config files not present | Low | No action |
| Environment variables | `UPM_*`, proxy, `NODE_PATH`, npm cache variables are unset | Low | No action |

## 4. Lockfile Structural Review

| package/group | source | suspicious field | finding | action |
| ------------- | ------ | ---------------- | ------- | ------ |
| Entire lockfile | mixed | missing `version`, `depth`, or `source` | none found | Lock syntax/critical-field hypothesis weakened |
| `com.beans.deform` | git | git URL/hash | Valid git lock entry; prior T+2d removal did not change failure | Keep unchanged |
| `com.coplaydev.unity-mcp` | git | `?path=/MCPForUnity` in git URL | Valid lock entry; prior T+2d MCP-only and all-git removal did not change failure | Keep unchanged |
| Unity registry packages | registry | `url` | Registry entries point to `https://packages.unity.com`; no empty URL found | Keep unchanged |
| Feature/builtin packages | builtin | manifest registry-like entries lock as builtin | `com.unity.feature.mobile`, `com.unity.multiplayer.center`, URP/ShaderGraph-related packages resolve as builtin in lock | Not isolated as failure; minimal built-in manifest still failed |
| Manifest vs lock | all | direct dependency missing from lock | none found | No package-file fix retained |

Structural artifacts:

- `artifacts/restart/t2e-lock-structural-review.json`
- `artifacts/restart/t2e-manifest-vs-lock.csv`
- `artifacts/restart/t2e-lock-direct-dependencies.csv`

## 5. Minimal Manifest Diagnostic

| candidate | manifest shape | lock handling | result | reached stage | retained/reverted |
| --------- | -------------- | ------------- | ------ | ------------- | ----------------- |
| Baseline | Original manifest, original lock | Original lock retained | Fails with exact `path` error | UPM package resolution only | Artifacts retained, package files unchanged |
| Minimal built-in | Only original `com.unity.modules.*` dependencies | `packages-lock.json` removed after preservation | Fails with same `path` error | UPM package resolution only | Reverted |
| Minimal built-in + generated cleanup | Same built-in-only manifest | Lock absent; `Library/ScriptAssemblies` removed; no `PackageCache` or `PackageManager` folder existed | Fails with same `path` error | UPM package resolution only | Reverted |
| Minimal built-in + no PackageManagerSettings | Same built-in-only manifest | Lock absent; `PackageManagerSettings.asset` temporarily removed | Fails with same `path` error | UPM package resolution only | Reverted |
| Minimal built-in + Unity `6000.4.9f1` | Same built-in-only manifest | Lock absent | Fails with `Failed to update project manifest: The "path" argument...` | UPM list/update-dependencies only | Reverted |

Conclusion: a package group bisection would be non-informative until a minimal manifest can resolve. The blocker survives even when all non-built-in manifest packages are absent.

## 6. Package Group Bisection

| group | packages | result | conclusion | next |
| ----- | -------- | ------ | ---------- | ---- |
| Git packages | `com.beans.deform`, `com.coplaydev.unity-mcp` | Prior T+2d removals failed with same error | Not sufficient root cause | Keep as diagnostic-only removals |
| Unity registry/editor packages | AI, Burst, Input System, ProBuilder, Splines, Timeline, Visual Scripting, Newtonsoft, Visual Studio | Not bisected in T+2e because minimal built-in manifest failed first | Package group interaction is unlikely to be the first blocker | Resume only after minimal manifest resolves |
| Built-in modules | `com.unity.modules.*` only | Same error | Failure is below normal registry package group interaction | Move to environment/version/project path |
| PackageManagerSettings group | Project Package Manager settings file | Same error when temporarily absent | Registry settings not sufficient root cause | Keep original settings |

## 7. Generated State / Environment Tests

| candidate | files/folders touched | result | retained/reverted | conclusion |
| --------- | --------------------- | ------ | ----------------- | ---------- |
| Required preservation | `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/PackageManagerSettings.asset`, `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/EditorSettings.asset`, plus `ProjectVersion.txt` before version test | Copies written under `artifacts/restart/t2e-before-*` | Retained artifacts only | Safe baseline for reversible tests |
| Worktree generated cleanup | `Library/ScriptAssemblies` deleted after root-path check; `Library/PackageCache` and `Library/PackageManager` were absent | Same error | Generated folder deletion not retained as tracked diff | Worktree-local generated state unlikely as sole cause |
| Unity `6000.4.9f1` | No tracked ProjectSettings diff retained; Unity reported ArtifactDB format recreation in generated `Library/` | Same path error, with `project:update-dependencies --> 500` | Reverted package files; generated state left as Unity-generated | Not unique to `6000.3.6f1` |
| User-level UPM config review | Read only: env vars, `.upmconfig.toml`, `.npmrc`, AppData config candidates | No UPM/proxy/npm override found | Read-only | External config override unlikely |
| Machine-level UPM cache review | Read only: `C:\Users\PLANNER007\AppData\Local\Unity\cache\upm` exists | Cache exists; no cleanup performed outside worktree | Read-only | Cache corruption remains possible but untested by design |
| Machine-level package license ACL | Read only: `C:\Users\PLANNER007\AppData\Local\Unity\licenses\packages\packageAccessControlList.xml` | One empty `<Package Id=""/>` among 41 package ids | Read-only | Suspicious machine-level signal; next slice should test by safe backup/regeneration with user approval or explicit scope |

## 8. Hypothesis Update

| hypothesis | T+2d status | T+2e evidence | updated result | next action |
| ---------- | ----------- | ------------- | -------------- | ----------- |
| H1 manifest malformed | Not proven; manifest JSON parsed | Built-in-only manifest also fails | Unlikely as sole cause | Do not hand-edit package versions as first response |
| H2 lock malformed/stale | Lock removal failed in T+2d | Lock absent in all minimal runs and still fails | Very unlikely as sole cause | Keep lock restored |
| H3 git path package | MCP/Deform/all-git removals failed in T+2d | Built-in-only manifest removes all git packages and still fails | Ruled out as sole cause | Keep packages unchanged |
| H4 Unity version/install behavior | Possible | `6000.4.9f1` also fails, but gives deeper `update-dependencies` signal; licensing handshake warnings appear but license resolves | Still possible, not single-version-specific | Test with a new/empty control project or repaired UPM cache only in a scoped environment slice |
| H5 local generated/cache state | Possible | `Library/ScriptAssemblies` cleanup plus prior full generated cleanup did not help; `PackageCache`/`PackageManager` absent | Unlikely inside worktree | Do not repeat local cleanup |
| H6 ProjectSettings / PackageManagerSettings | Possible | Removing PackageManagerSettings did not help; ProjectSettings was otherwise read-only | PackageManagerSettings unlikely; broader ProjectSettings low-confidence | Avoid broad ProjectSettings edits without a control project |
| H7 package group interaction | Possible after T+2d | Minimal built-in manifest fails before any registry package group is present | Unlikely as first blocker | Do not bisect registry groups until minimal manifest resolves |
| H8 project path / machine environment | Possible | Same error survives minimal manifest, no UPM env/config overrides found, machine package ACL has empty package id, UPM cache exists outside worktree | Most likely remaining class | Next slice should isolate machine cache/license state or run same repo path under a clean user/cache/control project |

## 9. Final State

- Changed files intentionally added in this slice:
  - `docs/restart/VC_PACKAGE_MANAGER_ROOT_CAUSE_REPORT.md`
- Existing prior-slice dirty file still present:
  - `docs/restart/VC_PACKAGE_MANAGER_RESTORATION_REPORT.md`
- Existing prior-slice untracked file still present:
  - `docs/restart/VC_ORIGIN_MAIN_PARENT_BOOTSTRAP_REPORT.md`
- Package files restored:
  - `Packages/manifest.json` has no final diff.
  - `Packages/packages-lock.json` has no final diff.
- ProjectSettings restored:
  - `ProjectSettings/PackageManagerSettings.asset` has no final diff.
  - `ProjectSettings/ProjectSettings.asset` has no final diff.
  - `ProjectSettings/EditorSettings.asset` has no final diff.
  - `ProjectSettings/ProjectVersion.txt` has no final diff.
- UPM reached:
  - Unity startup and AssetDatabase initial refresh.
  - UPM package-resolution/list/update-dependencies request.
- UPM did not reach:
  - successful package resolution.
  - project import with loaded packages.
  - C# compile.
- Handoff branch remained untouched.

## 10. Decision Packet

Recommended default:

- Stop package-file guessing in this repo state.
- Keep `Packages/manifest.json` and `Packages/packages-lock.json` unchanged.
- Treat the remaining blocker as environment/path/machine-level UPM state until a control test disproves it.
- Next slice should be `VC-RST-2f-upm-environment-or-version-isolation`.

Alternatives:

| option | value | cost/risk | when to choose |
| --- | --- | --- | --- |
| Machine cache/license ACL isolation | Directly tests the strongest remaining class, including the empty package id signal | Requires touching files outside the worktree; needs explicit approval/scope | Best next move if user allows machine-level reversible cache backup/regeneration |
| Control Unity project in clean path/user cache | Separates project path from machine UPM state | Needs a safe scratch project/path outside this repo scope | Best next move if machine files should remain untouched |
| Unity Editor repair/reinstall or version pin change | Tests install corruption | User-side time and product environment churn | Only after cache/control tests |
| Broader ProjectSettings edit | Could reveal rare project setting corruption | Risky and low-signal after PackageManagerSettings test | Only with a separate disposable clone/worktree |

Rejected options:

- Permanent removal of Deform, Unity MCP, AI packages, URP, or other packages.
- Runtime/editor C# edits.
- Terrain architecture, DualGrid, CSG, mining, EasyRoads, Simulator split, Trail, or player-controller work.
- More bisection of registry packages before minimal manifest resolves.
- Machine-level cache deletion without explicit user approval/scope.

Confidence:

- High that UPM still fails before C# compilation on current parent.
- High that package JSON syntax, lockfile state, git/path packages, and normal package-group interaction are not sufficient root causes.
- Medium that the remaining root cause is machine-level UPM/license/cache state or project path/update-dependencies behavior.
- Low on the exact machine-level file until a scoped external-cache test is allowed.

Remaining unknowns:

- Whether the empty package id in `packageAccessControlList.xml` is causal or merely incidental.
- Whether a clean Unity project under the same user/cache reproduces the same UPM 500.
- Whether a cloned VastCore path outside this long worktree name reproduces the same UPM 500.
- Which internal UPM function receives `path = undefined`.

## Completion Matrix / Done Gates

| gate | done | total | unknown | meter | missing |
| --- | ---: | ---: | ---: | --- | --- |
| Worktree verified | 5 | 5 | 0 | `[#####] 5/5` | none |
| Deep logs gathered | 5 | 5 | 0 | `[#####] 5/5` | baseline log, UPM logs, minimal logs, version log, timestamped extracts captured |
| Settings/lock reviewed | 5 | 5 | 0 | `[#####] 5/5` | none |
| Minimal manifest diagnostic | 5 | 5 | 0 | `[#####] 5/5` | none |
| Group/environment isolation | 4 | 6 | 1 | `[####--] 4/6` | machine cache mutation, project path control |
| Validation | 3 | 5 | 0 | `[###--] 3/5` | UPM resolution, C# compile/tests |
| Report hygiene | 8 | 8 | 0 | `[########] 8/8` | none |

## Review Card / Review Debt

Review Card: not emitted.

Reason: this slice is UPM root-cause diagnostics, not a user-facing artifact review.

Review debt:

- The machine-level ACL/cache finding is not proven causal.
- No external-cache cleanup was performed because it is outside the parent worktree.
- No clean-control Unity project was created because this slice was bounded to the validated parent worktree.

## Command / Action Ledger

| action | command or method | result |
| --- | --- | --- |
| Verify parent worktree | `git status`, `git rev-parse`, `git worktree list` | Valid parent at `39f790c`; package files had no starting diff |
| Preserve files | PowerShell `Copy-Item` to `artifacts/restart/t2e-before-*` | Manifest, lock, required ProjectSettings copied |
| Read required project protocols | `docs/02_design/ASSEMBLY_ARCHITECTURE.md`, `docs/03_guides/UNITY_CODE_STANDARDS.md`, `docs/03_guides/COMPILATION_GUARD_PROTOCOL.md` | Read before diagnostics/report |
| Review PackageManagerSettings | `Get-Content`, `rg` | Default Unity registry only; no scoped registry/proxy/cache path |
| Review lock structure | PowerShell JSON parse/comparison | No missing critical fields; artifacts written |
| Baseline Unity run | `scripts/check-compile.ps1` with Unity `6000.3.6f1` | Same UPM error |
| Create diagnostic branch | `git switch -c codex/vc-rst-2e-upm-root-cause` | Branch created from `39f790c` |
| Minimal manifest run | built-in-only manifest, lock absent, Unity `6000.3.6f1` | Same UPM error |
| Generated-state cleanup | Deleted `Library/ScriptAssemblies`; `PackageCache` and `PackageManager` absent | Same UPM error |
| PackageManagerSettings removal test | Temporarily removed settings file | Same UPM error; restored |
| Unity version diagnostic | Minimal manifest with Unity `6000.4.9f1` | Same path error; deeper `update-dependencies` signal |
| Environment review | Read env vars, config paths, UPM cache presence, package ACL | No env/config override; one empty package id in machine ACL |
| Restore package files | Copied preserved manifest/lock/settings back | No final package/ProjectSettings diff |
| Final JSON parse | `ConvertFrom-Json` | Manifest and lock parse OK |

## User-Side Work

No user action is required to preserve the current repo state.

User action will be needed if the next slice touches machine-level Unity state, specifically:

- backing up/regenerating `C:\Users\PLANNER007\AppData\Local\Unity\licenses\packages\packageAccessControlList.xml`;
- clearing or relocating `C:\Users\PLANNER007\AppData\Local\Unity\cache\upm`;
- repairing/reinstalling Unity Editor or UPM components;
- approving a clean-control project outside this parent worktree.

## Agent-Side Work

Recommended next agent-side entry points:

| entry | friction reduced | enables |
| --- | --- | --- |
| Environment isolate | Removes uncertainty around machine-level UPM cache/license ACL | Confident decision on cache repair vs project-local fix |
| Path/control project test | Separates long worktree path/project metadata from Unity install state | Avoids editing real project files while proving path class |
| UPM trace escalation | Seeks stack/context behind `path undefined` | Turns opaque 500 into a fixable file/function hypothesis |
| Restore-to-compile gate | Only after UPM resolves | Starts `VC-RST-3-csharp-compile-restoration-gate` without mixing layers |

## Goal Stack

| horizon | state |
| --- | --- |
| Immediate | Deeper UPM evidence collected; minimal manifest still fails |
| Short-term | Root cause class moved away from package files and toward environment/path/machine UPM state |
| Mid-term | Need a bounded environment/version isolation slice |
| Long-term | C# compile and terrain work remain blocked until UPM resolves |

## Turn Calendar

| turn | expected move |
| --- | --- |
| T+2e | Completed root-cause isolation within parent worktree |
| T+2f | Machine-level UPM cache/license ACL or clean-control path isolation |
| T+3 | C# compile restoration only if UPM resolves |
| T+4 | Architecture/terrain implementation only after compile gate evidence |

## Visual Summary

```text
origin/main parent verified       [#####] 5/5
package files restored            [#####] 5/5
lock/settings reviewed            [#####] 5/5
minimal manifest tested           [#####] 5/5
package-group suspicion           [#----] low
machine/path suspicion            [####-] high-ish
UPM resolved                      [-----] 0/5
C# compile reached                [-----] 0/5
```

## Continuation State

Continue from:

- `C:\Users\PLANNER007\VastCore\VastCore-origin-main-parent-20260625`
- branch `codex/vc-rst-2e-upm-root-cause`
- base `39f790c4651718c03a8e64628fcbd3dd1def0b44`

Do not continue from:

- `C:\Users\PLANNER007\VastCore\VastCore-origin-main-compile`
- the handoff branch worktree for Unity/package diagnostics

Next recommended slice:

- `VC-RST-2f-upm-environment-or-version-isolation`

Do not start:

- `VC-RST-3-csharp-compile-restoration-gate` until UPM resolves.
- terrain, DualGrid, mining, CSG, EasyRoads, Simulator split, Trail, or player-controller work.


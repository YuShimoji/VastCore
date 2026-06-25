# VC Package Manager Restoration Report

Route:
`VastCore | VC-RST-2d-package-manager-restoration-from-current-parent | target: YuShimoji/VastCore@origin-main-parent-20260625`

Last updated: 2026-06-25

Review Card: not emitted.
reason: this slice is package-manager restoration, not a user-facing artifact review.
next_nonredundant_axis: package-root-cause fix; C# compile restoration remains blocked.

## 1. Current State

| Field | Value |
| --- | --- |
| Worktree path | `C:\Users\PLANNER007\VastCore\VastCore-origin-main-parent-20260625` |
| HEAD | `39f790c4651718c03a8e64628fcbd3dd1def0b44` |
| Branch state | detached HEAD |
| Unity version used | `6000.3.6f1 (bbb010bdb8a3)` |
| Unity executable | `C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Unity.exe` |
| Known bootstrap report status | `docs/restart/VC_ORIGIN_MAIN_PARENT_BOOTSTRAP_REPORT.md` exists as the allowed untracked bootstrap artifact |
| Previous blocker | `Failed to resolve packages: The "path" argument must be of type string. Received undefined. No packages loaded.` |
| Current blocker after this slice | Same UPM resolver error still reproduces after baseline, lockfile-regeneration, git-package removal, and generated-folder cleanup candidates |
| Handoff branch usage | not used for Unity/package diagnostics |

The parent worktree is valid for diagnostics. The Package Manager issue is now reproduced on current `origin/main`, not only in the older stale worktree.

## 2. Package Inventory

| package | declaration | lock status | source type | risk | finding |
| ------- | ----------- | ----------- | ----------- | ---- | ------- |
| `com.beans.deform` | `https://github.com/keenanwoodall/Deform.git` | lock present, `source=git`, hash `9e57dd3864ea6c742d819959724c99ca574a2f76` | git | medium-high | Removing only Deform did not change the UPM failure |
| `com.coplaydev.unity-mcp` | `https://github.com/justinpbarnett/unity-mcp.git?path=/MCPForUnity` | lock present, `source=git`, hash `aaf6308b331f6cbcc2a41f11d90ac2109154343e` | git with path query | high | Removing only MCP did not change the UPM failure; note prompt mentioned `UnityMcpBridge`, but manifest uses `/MCPForUnity` |
| `com.unity.ai.assistant` | `1.0.0-pre.12` | lock present, registry | registry prerelease | medium | Manifest/lock parse clean; no direct path evidence |
| `com.unity.ai.generators` | `1.0.0-pre.20` | lock present, registry | registry prerelease | medium | Manifest/lock parse clean; no direct path evidence |
| `com.unity.ai.inference` | `2.4.1` | lock present, registry | registry | medium | High-signal package, but no malformed value found |
| `com.unity.probuilder` | `6.0.8` | lock present, registry | registry | medium | High-signal package, but no path evidence |
| `com.unity.render-pipelines.universal` | `17.3.0` | lock present, builtin source in lock | builtin/registry hybrid | medium | Project render pipeline; not touched |
| `com.unity.splines` | `2.8.2` | lock present, registry | registry | medium | High-signal package, but no path evidence |
| `com.unity.inputsystem` | `1.18.0` | lock present, registry | registry | medium | High-signal package, but no path evidence |
| `com.unity.burst` | `1.8.27` | lock present, registry | registry | medium | Dependency of several packages; no malformed value found |
| `com.unity.feature.mobile` | `1.0.0` | lock present, builtin | feature/builtin | medium | Feature package pulls additional mobile packages through lock |
| `com.unity.visualscripting` | `1.9.9` | lock present, registry | registry | low-medium | No malformed value found |
| `com.unity.timeline` | `1.8.10` | lock present, registry | registry | low-medium | No malformed value found |
| `com.unity.ugui` | `2.0.0` | lock present, builtin | builtin | low-medium | No malformed value found |
| `com.unity.modules.*` | `1.0.0` entries | lock present, builtin | builtin/modules | low | Built-in module set; not suspected individually |

Machine checks:

- `Packages/manifest.json` parses as JSON.
- `Packages/packages-lock.json` parses as JSON.
- No manifest dependency value is null or empty.
- No local/path dependency entry exists.
- The only manifest git/path entries are Deform and Unity MCP.
- Lock entries have `version`, `depth`, `source`, and `dependencies`.

Full machine inventory artifact:

- `artifacts/restart/package-inventory.json`

## 3. Error Context

Baseline exact error:

```text
Failed to resolve packages: The "path" argument must be of type string. Received undefined. No packages loaded.
```

Surrounding Unity log context from the unmodified baseline:

```text
[Package Manager] Connected to IPC stream "Upm-22452" after 0.5 seconds.
Begin MonoManager ReloadAssembly
Application.AssetDatabase Initial Refresh Start
Package Manager log level set to [2]
[Package Manager] Done resolving packages with errors in 0.16 seconds
Failed to resolve packages: The "path" argument must be of type string. Received undefined. No packages loaded.
```

Stage assessment:

| question | result |
| --- | --- |
| Does the error occur before Package Manager success? | yes |
| Does Unity begin project startup? | yes |
| Does AssetDatabase initial refresh begin? | yes |
| Does Package Manager resolve packages? | no |
| Are any `error CS####` compiler errors emitted? | no |
| Was `StructureTagAdapter.cs` reached? | no |

The global UPM log for this run only records `project:resolve-packages --> 500` and does not expose a deeper stack trace.

## 4. Hypothesis Matrix

| hypothesis | evidence for | evidence against | result | next action |
| ---------- | ------------ | ---------------- | ------ | ----------- |
| H1: `manifest.json` contains a malformed dependency entry | Error mentions `path`; manifest contains one git URL with `?path=/MCPForUnity` | Manifest JSON parses; no null/empty/local path values; removing the MCP path package did not change the failure | Not proven | Investigate broader UPM resolver behavior or hidden package metadata, not just JSON syntax |
| H2: `packages-lock.json` is stale or malformed | Failure happens during package resolution | Lock JSON parses; required fields present; removing lockfile and letting Unity attempt regeneration produced the same error | Unlikely as sole cause | Keep lockfile unchanged; do not treat lock regeneration as sufficient |
| H3: Git package path query causes the failure | `com.coplaydev.unity-mcp` uses `?path=/MCPForUnity`; Deform is also a git package | Removing MCP only, Deform only, and both git packages still produced the same error | Unlikely as sole cause | Next package-root-cause slice should inspect UPM internals/feature packages rather than permanently removing git packages |
| H4: Unity version mismatch causes UPM behavior | Project requires Unity `6000.3.6f1`; prior failure was also with `6000.3.6f1` | Exact editor exists and was used; changing Unity version is out of scope | Possible Unity/UPM version bug, not resolved here | Keep Unity version fixed; gather deeper UPM diagnostics before any version change |
| H5: Local cache/project state corrupt | UPM can fail due generated cache state | First baseline rebuilt missing Library; deleting `Library/` and `Temp/` inside the parent worktree still produced same error | Unlikely as sole cause | Do not keep repeating cache cleanup; move to deeper resolver diagnostics |

## 5. Candidate Fixes Tried

| candidate | files touched | command/action | result | retained/reverted | rationale |
| --------- | ------------- | -------------- | ------ | ----------------- | --------- |
| Baseline unmodified compile/package check | none | `scripts/check-compile.ps1` with Unity `6000.3.6f1` | Same UPM `path` error reproduced before C# compile | retained artifacts only | Establish current-parent reproduction |
| Lockfile regeneration candidate | temporarily moved `Packages/packages-lock.json` out of `Packages/` | Ran `check-compile.ps1` without lockfile | Same UPM `path` error | reverted; original lock restored | H2 test; lockfile absence did not help |
| Remove MCP git/path package | temporary edits to `manifest.json` and `packages-lock.json` | Removed `com.coplaydev.unity-mcp`; ran `check-compile.ps1` | Same UPM `path` error | reverted | Tests the only `?path=` package |
| Remove Deform git package | temporary edits to `manifest.json` and `packages-lock.json` | Removed `com.beans.deform`; ran `check-compile.ps1` | Same UPM `path` error | reverted | Tests the other git package |
| Remove both git packages | temporary edits to `manifest.json` and `packages-lock.json` | Removed Deform and MCP; ran `check-compile.ps1` | Same UPM `path` error | reverted | Tests git package class as a group |
| Generated-folder cleanup | `Library/` and `Temp/` generated folders inside this parent worktree only | Removed generated folders after verifying resolved paths stayed under this worktree; reran `check-compile.ps1` | Same UPM `path` error | generated folders regenerated by Unity; no tracked package change retained | H5 test |

All package-file edits were diagnostic-only, reversible, and reverted. No package removal is retained or proposed as a product decision.

## 6. Final Package State

- Changed files under `Packages/`: none retained.
- Manifest changed: no.
- Lockfile changed: no.
- Package removed or changed: no.
- Intended package changes to keep: none.
- Changes to replay to `main` / branch: none.
- Generated folders affected: `Library/` and `Temp/` inside the validated parent worktree were removed once and regenerated by Unity.
- Report changes to keep: this report updates the stale VC-RST-2 package-restoration artifact.

## 7. Validation Results

| validation | worktree | command/action | result | reached stage | artifact/log |
| ---------- | -------- | -------------- | ------ | ------------- | ------------ |
| Starting worktree verification | parent `39f790c` | `git status`, `rev-parse`, worktree list | valid; only known bootstrap report untracked | not-run | terminal readback |
| Package JSON parse | parent `39f790c` | PowerShell `ConvertFrom-Json` | manifest and lock parse OK | package-json-parse | `artifacts/restart/package-json-parse.log` |
| Package inventory | parent `39f790c` | manifest/lock comparison script | high-signal inventory produced | package-json-parse | `artifacts/restart/package-inventory.json` |
| Baseline UPM / compile check | parent `39f790c` | `scripts/check-compile.ps1` | failed with UPM `path` error | package-resolution | `artifacts/logs/compile-check.log`, `artifacts/restart/baseline-compile-signals.txt` |
| Lockfile regeneration candidate | parent `39f790c` | remove lockfile then `check-compile.ps1` | same UPM error | package-resolution | `artifacts/logs/candidate-lock-regeneration/compile-check.log` |
| MCP removal candidate | parent `39f790c` | remove MCP from manifest/lock then `check-compile.ps1` | same UPM error | package-resolution | `artifacts/logs/candidate-remove-mcp/compile-check.log` |
| Deform removal candidate | parent `39f790c` | remove Deform from manifest/lock then `check-compile.ps1` | same UPM error | package-resolution | `artifacts/logs/candidate-remove-deform/compile-check.log` |
| All git packages removed candidate | parent `39f790c` | remove Deform and MCP then `check-compile.ps1` | same UPM error | package-resolution | `artifacts/logs/candidate-remove-all-git/compile-check.log` |
| Generated cache cleanup candidate | parent `39f790c` | remove `Library/` and `Temp/`, then `check-compile.ps1` | same UPM error | package-resolution | `artifacts/logs/candidate-cache-clean/compile-check.log` |
| C# compile | parent `39f790c` | not reached | blocked by UPM | not-run | no `error CS####` emitted |
| EditMode tests | parent `39f790c` | not run | blocked by UPM | not-run | none |
| PlayMode tests | parent `39f790c` | not run | out of scope | not-run | none |

## 8. Next Blocker

Package Manager is not fixed.

Smallest remaining package-level action:

1. Gather deeper UPM resolver diagnostics beyond Unity's current `-l 2` package-manager log, because every tested package-file hypothesis returned the same opaque `project:resolve-packages --> 500`.
2. Inspect whether Unity `6000.3.6f1` Package Manager has a known resolver bug with this package set or feature packages.
3. If deeper diagnostics remain opaque, create a minimal manifest-reduction diagnostic branch from this exact parent HEAD and bisect package groups, starting with Unity AI / feature packages rather than the already-tested git packages.

Recommended next slice:

`VC-RST-2e-package-manager-root-cause-fix`

Do not start:

- `VC-RST-3-csharp-compile-restoration-gate`
- terrain algorithm work
- DualGrid behavior work
- CSG/mining/EasyRoads/player/simulator work

until UPM resolves and Unity reaches C# compilation.

## 9. Decision Packet

Recommended default:

- Continue only in `C:\Users\PLANNER007\VastCore\VastCore-origin-main-parent-20260625`.
- Keep the handoff branch untouched.
- Keep `Packages/manifest.json` and `Packages/packages-lock.json` unchanged for now.
- Treat package removals as diagnostic only.
- Next diagnostic should improve UPM observability or bisect broader package groups, not repeat the already-tested git/lock/cache candidates.

Alternatives:

- Create a local diagnostic branch from `39f790c` before a broader manifest-reduction bisect.
- If available, run a deeper UPM log mode or Unity Package Manager server diagnostic before editing additional package groups.
- Compare behavior with another Unity `6000.3.x` editor only if a later slice explicitly allows Unity-version diagnostics.

Rejected options:

- Permanent removal of Deform or Unity MCP.
- Running package diagnostics in `C:\Users\PLANNER007\VastCore\VastCore`.
- Reusing stale `VastCore-origin-main-compile` at `3893388`.
- Starting C# compile fixes before UPM resolution.
- Treating generated-folder cleanup as a fix.

Confidence:

- High that the blocker is reproduced on current `origin/main`.
- High that JSON syntax, lockfile absence, git-package removal, and local generated cache are not sufficient fixes.
- Medium-low on exact root cause because Unity/UPM reports only `path undefined` and `project:resolve-packages --> 500`.

Remaining unknowns:

- The deeper UPM stack/source package that throws `path undefined`.
- Whether Unity AI / feature packages or a Unity `6000.3.6f1` UPM bug are involved.
- Whether C# compile will next fail at `StructureTagAdapter.cs` after UPM resolution.

## Completion Matrix

| gate | done | total | unknown | meter | missing |
| --- | ---: | ---: | ---: | --- | --- |
| Worktree verified | 5 | 5 | 0 | `[#####] 5/5` | none |
| Package files preserved | 4 | 4 | 0 | `[####] 4/4` | none |
| Package inventory | 5 | 5 | 0 | `[#####] 5/5` | none |
| Hypothesis matrix | 5 | 5 | 0 | `[#####] 5/5` | none |
| Candidate restoration | 6 | 6 | 0 | `[######] 6/6` | none |
| Validation | 3 | 5 | 0 | `[###--] 3/5` | C# compile, tests |
| Report hygiene | 8 | 8 | 0 | `[########] 8/8` | none |

## Command / Action Ledger

| owner | action | result |
| --- | --- | --- |
| EXECUTED_BY_AGENT | Read VC-RST-2d supervisor prompt | completed |
| EXECUTED_BY_AGENT | Verified parent worktree cwd/status/HEAD/worktree list | completed |
| EXECUTED_BY_AGENT | Read repo-local rules and runtime state | completed |
| EXECUTED_BY_AGENT | Preserved package files under `artifacts/restart/` | completed |
| EXECUTED_BY_AGENT | Parsed manifest and lock JSON | both OK |
| EXECUTED_BY_AGENT | Created package inventory artifact | completed |
| EXECUTED_BY_AGENT | Ran baseline `scripts/check-compile.ps1` | UPM failure reproduced |
| EXECUTED_BY_AGENT | Tested lockfile removal/regeneration candidate | same UPM failure; reverted |
| EXECUTED_BY_AGENT | Tested MCP removal candidate | same UPM failure; reverted |
| EXECUTED_BY_AGENT | Tested Deform removal candidate | same UPM failure; reverted |
| EXECUTED_BY_AGENT | Tested all-git-package removal candidate | same UPM failure; reverted |
| EXECUTED_BY_AGENT | Removed `Library/` and `Temp/` inside parent worktree after path safety check | completed |
| EXECUTED_BY_AGENT | Reran check after generated-folder cleanup | same UPM failure |
| EXECUTED_BY_AGENT | Updated this report | completed |
| DO_NOT_RUN | C# fixes | blocked because UPM did not resolve |
| DO_NOT_RUN | EditMode / PlayMode tests | blocked by UPM |
| DO_NOT_RUN | Runtime/editor/scene/prefab/asset changes | out of scope |
| DO_NOT_RUN | Handoff branch Unity/package work | forbidden |

## User-Side Work

None required in this slice.

Unity license/login did not block validation. Git package authentication did not surface as a blocker. No permanent product decision was made.

## Agent-Side Work

Next agent-side work is a package-root-cause fix slice with deeper UPM diagnostics or package-group bisection. Keep all such work inside the validated parent worktree or a local diagnostic branch based exactly on `39f790c`.

## Goal Stack

| horizon | state |
| --- | --- |
| Immediate | UPM failure reproduced on current parent |
| Short-term | initial package-file hypotheses tested and ruled out as sufficient fixes |
| Mid-term | deeper UPM root-cause work required before C# compile |
| Long-term | terrain architecture remains blocked until package/compile confidence |

## Turn Calendar

| turn | expected move |
| --- | --- |
| T+2e | deeper Package Manager root-cause diagnostics |
| T+3 | C# compile restoration only if UPM reaches compilation |
| T+4 | terrain/product architecture work only after compile gate evidence |

## Visual Summary

```text
Worktree verified      [#####] 5/5
Package JSON checked   [####] 4/4
Baseline reproduced    [#####] 5/5
Git package tested     [#####] 5/5
Cache tested           [#####] 5/5
UPM fixed              [-----] 0/5
C# compile reached     [-----] 0/5
```

## Continuation State

Handoff Gate: false.

Reason: the validated parent worktree remains available and the next slice can continue locally. No paste-ready next-agent prompt is included.

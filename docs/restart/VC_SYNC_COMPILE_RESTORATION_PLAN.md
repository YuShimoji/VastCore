# VC Sync / Compile Restoration Plan

Date: 2026-06-22

Route: `VC-RST-1-sync-compile-restoration-gate`

Target: `YuShimoji/VastCore@main`

Scope: sync and compile-restoration gate only. This slice did not change runtime code, editor code, terrain algorithms, DualGrid behavior, mining, CSG, EasyRoads, Simulator split, Trail, or player controller behavior.

Review Card: not emitted. This artifact is a restart/sync decision packet, not a code review card.

## Current Sync State

The original working tree remains intentionally unsynced. `git fetch --all --prune` was run, but `git pull`, `git merge`, `git rebase`, `git reset`, `git checkout .`, and `git clean` were not run.

| item | observed state | meaning |
|---|---:|---|
| Local branch | `main` | current workspace branch |
| Local HEAD | `a9e714218bd0a0ef4b45ba69405518dc2d0e4b81` | old local baseline |
| Remote HEAD | `3893388e5045ef49e22b401e4dd9f25a05cc3b38` | current `origin/main` after fetch |
| Ahead / behind | `0 / 11` | local has no unique commits but is 11 commits behind |
| Staged changes | `0` | no index work to preserve separately |
| Tracked diff files | `54` by `git diff --name-only` | enough overlap risk to avoid direct pull |
| Porcelain visible entries | `59` | includes collapsed untracked dirs |
| Untracked real files | `6` before this plan | prior restart audit/OpenSpec files are not yet committed |

Local package/project state is not the same as `origin/main`. The original worktree has `ProjectSettings/ProjectVersion.txt` at Unity `6000.4.9f1`, while clean `origin/main` has Unity `6000.3.6f1`. Package versions also differ, for example URP `17.4.0` locally versus `17.3.0` on `origin/main`.

Evidence preserved under ignored artifacts:

| artifact | role |
|---|---|
| `artifacts/restart/local-status.txt` | branch/status snapshot |
| `artifacts/restart/local-diff-stat.txt` | high-level tracked diff size |
| `artifacts/restart/local-diff-name-status.txt` | changed tracked path inventory |
| `artifacts/restart/local-untracked-files.txt` | untracked file inventory |
| `artifacts/restart/local-working-tree.patch` | binary-capable tracked diff preservation |
| `artifacts/restart/origin-main-worktree-status.txt` | clean worktree HEAD/status/version readback |
| `artifacts/restart/origin-main-package-baseline.txt` | clean `origin/main` package baseline |
| `artifacts/restart/origin-main-compile-check.log` | Unity compile-check log from clean worktree |
| `artifacts/restart/origin-main-compile-signal.txt` | compact extract of package/compile signals |

## Dirty Worktree Inventory

The original working tree has a mixed dirty set. It is not just docs, so a direct sync would make conflict diagnosis and compile attribution noisy.

| category | visible status entries | representative paths | conflict / preservation concern |
|---|---:|---|---|
| docs / OpenSpec | 33 | `docs/02_design/ASSEMBLY_ARCHITECTURE.md`, `docs/SSOT_WORLD.md`, `docs/restart/`, `openspec/` | high document churn; preserve restart artifacts before sync |
| generated / artifacts | 4 | `.serena/memories/*`, `spec-wiki.html` | likely local/generated; do not treat as product progress |
| packages / project settings | 7 | `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/ProjectVersion.txt`, `VastCore.slnx` | high risk; changes Unity/package baseline and compile behavior |
| runtime code | 6 | deleted `Assets/_Scripts/*.cs`, deleted `Assets/_Scripts/Vastcore.Legacy.asmdef` | high risk; deletions must be intentionally accepted or quarantined |
| scenes / prefabs / assets | 9 | deleted `.meta` files under `Assets/_Scripts`, `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` | high risk; `.meta`/asset identity changes can break references |
| editor code | 0 | none in current dirty status | not dirty locally, but `origin/main` contains recent editor changes |
| tests | 0 | none in current dirty status | not dirty locally, but `origin/main` contains recent test/meta changes |

The untracked restart artifacts before this plan were:

- `docs/04_reports/PROJECT_STATUS_SURVEY_2026-03-26.md`
- `docs/restart/VC_RESTART_ARCHITECTURE_AUDIT.md`
- `openspec/changes/restart-architecture-boundaries/proposal.md`
- `openspec/changes/restart-architecture-boundaries/specs/architecture-boundaries/spec.md`
- `openspec/changes/restart-architecture-boundaries/tasks.md`
- `spec-wiki.html`

This plan adds one more untracked restart document unless staged later:

- `docs/restart/VC_SYNC_COMPILE_RESTORATION_PLAN.md`

## Preservation Plan

Do not sync the original worktree until the mixed dirty set is intentionally split. The safest sequence is:

1. Keep the current original tree as-is.
2. Preserve the current diff artifacts under `artifacts/restart/`.
3. Stage/commit or deliberately discard only after a path-by-path decision.
4. Separate restart docs/OpenSpec from package/project/runtime deletions.
5. Only after that, perform a safe sync path such as a clean branch/worktree integration or a narrow patch application.

| local material | recommended treatment | why |
|---|---|---|
| `docs/restart/*` and `openspec/changes/restart-architecture-boundaries/*` | preserve as restart planning artifacts | they encode the current handoff decision path |
| `Packages/*` and `ProjectSettings/*` | do not auto-merge | they change the Unity/package baseline and can mask compile causes |
| deleted `Assets/_Scripts/*` | review against `origin/main` and current architecture before accepting | script/meta deletions are asset identity and compile-sensitive |
| `.serena/*` and `spec-wiki.html` | keep out of product progress claims unless explicitly needed | generated/local support files should not drive the sync |

## Clean `origin/main` Baseline Plan

A separate clean worktree was created instead of touching the dirty original:

```text
C:\Users\PLANNER007\VastCore\VastCore-origin-main-compile
```

Observed clean worktree state:

| item | observed state |
|---|---|
| HEAD | `3893388e5045ef49e22b401e4dd9f25a05cc3b38` |
| Git state | detached HEAD, no tracked changes before validation |
| Unity version | `6000.3.6f1 (bbb010bdb8a3)` |
| Exact Unity editor availability | present at `C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Unity.exe` |
| Existing Unity processes | present, but pointed at `C:\Users\PLANNER007\Desktop\galsurvival`, not VastCore |

Key `origin/main` package baseline:

| package | clean `origin/main` |
|---|---|
| `com.unity.render-pipelines.universal` | `17.3.0` |
| `com.unity.probuilder` | `6.0.8` |
| `com.unity.inputsystem` | `1.18.0` |
| `com.unity.splines` | `2.8.2` |
| `com.unity.ai.inference` | `2.4.1` |
| `com.beans.deform` | git `https://github.com/keenanwoodall/Deform.git` |
| `com.coplaydev.unity-mcp` | git `https://github.com/justinpbarnett/unity-mcp.git?path=/MCPForUnity` |

Compile validation was attempted in the clean worktree with:

```powershell
.\scripts\check-compile.ps1 `
  -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Unity.exe" `
  -ProjectPath "C:\Users\PLANNER007\VastCore\VastCore-origin-main-compile" `
  -LogsDir "C:\Users\PLANNER007\VastCore\VastCore-origin-main-compile\artifacts\logs"
```

Result:

| gate | result | detail |
|---|---|---|
| Unity launch | reached editor startup | license resolved and project path changed successfully |
| Package Manager | failed | `Failed to resolve packages: The "path" argument must be of type string. Received undefined. No packages loaded.` |
| C# compile | not reached in this run | no `error CS####` lines were emitted by this attempt |
| log path | created | `C:\Users\PLANNER007\VastCore\VastCore-origin-main-compile\artifacts\logs\compile-check.log` |

This means the latest measured blocker is currently Package Manager resolution, not a confirmed C# compile error in this run. Existing repo docs still identify a later C# blocker in `StructureTagAdapter.cs`, so it remains in the restoration queue after package resolution is restored.

## Compile Restoration Plan

The next implementation slice should restore gates in this order, because later compiler errors cannot be trusted until packages load.

| order | work | success signal | stop condition |
|---:|---|---|---|
| 1 | Resolve Package Manager failure in clean `origin/main` | Unity reaches script compilation instead of `No packages loaded` | dependency addition/removal or package source change requires approval |
| 2 | Repair invalid `.meta` GUID files already called out by repo docs | the five script/test `.meta` files have valid GUIDs and Unity no longer reports blank `guid:` warnings | GUID repair would break existing scene/prefab references |
| 3 | Fix `StructureTagAdapter.cs` assembly/type ownership | `Assets/Scripts/Generation/StructureTagAdapter.cs(3,55)` no longer emits CS0234 | fix would require broad asmdef/namespace refactor |
| 4 | Re-run `scripts/check-compile.ps1` on clean baseline | Unity exits 0 and no `error CS####` lines remain | errors move outside compile-restoration scope |
| 5 | Run targeted EditMode tests only after compile passes | structure/tag/material selector tests execute with clear pass/fail | tests require product behavior changes beyond restoration |
| 6 | Decide original worktree sync strategy | dirty tree is either committed, split, or cleanly rebased/merged from a safe branch | package/project/runtime deletion conflict remains ambiguous |

Known follow-up C# and asset-integrity signals from `origin/main` docs:

| signal | source | current meaning |
|---|---|---|
| `StructureTagAdapter.cs` references `CompoundArchitecturalGenerator` across assemblies | `docs/architecture/current-dependency-map.md`, source readback | likely CS0234 after package resolution |
| five `.meta` files have blank `guid:` | direct readback in clean worktree | asset identity problem is still present |
| CI workflow uses Unity `6000.2.2f1` | `.github/workflows/unity-tests.yml` | CI version is stale relative to `ProjectVersion.txt` `6000.3.6f1` |
| `scripts/run-csg-scan.ps1` hardcodes Unity `6000.2.2f1` | script readback | not a valid compile gate until reviewed |

The five currently observed blank-GUID `.meta` files are:

- `Assets/Scripts/Generation/AdjacencyRuleSet.cs.meta`
- `Assets/Scripts/Generation/PlacementZone.cs.meta`
- `Assets/Scripts/Terrain/DualGrid/StructurePlacementSolver.cs.meta`
- `Assets/Tests/EditMode/AdjacencyRuleSetTests.cs.meta`
- `Assets/Tests/EditMode/StructurePlacementSolverTests.cs.meta`

## Conflict Risk Map

| area | risk | why it matters before sync |
|---|---|---|
| `Packages/manifest.json` and `packages-lock.json` | high | local tree and `origin/main` have different package versions; Package Manager is the current measured gate |
| `ProjectSettings/ProjectVersion.txt` | high | local Unity `6000.4.9f1` conflicts with `origin/main` `6000.3.6f1`; compile evidence must name the exact editor |
| deleted `Assets/_Scripts/*` | high | runtime scripts and `.meta` deletions can be real cleanup or accidental loss; cannot auto-accept |
| `Assets/Scripts/Generation` and `Assets/Scripts/Terrain` | high | `StructureTagAdapter`/`CompoundArchitecturalGenerator` assembly boundary is a known compile risk |
| `Assets/Editor` and `Assets/Scripts/Editor` | medium-high | origin added/reworked editor pipeline tools; local tree is not dirty there, but later sync may introduce conflicts with docs decisions |
| `Assets/Tests` | medium-high | origin contains test/meta changes, and blank GUID files include tests |
| `docs/` and `openspec/` | medium | restart docs are valuable but untracked; sync should not lose them |
| generated/local files | low-medium | `.serena/*` and `spec-wiki.html` should be preserved or ignored intentionally, not mixed into product commits |

## Next Slice Recommendation

Default next slice: restore the clean `origin/main` Package Manager gate first. That is the narrowest blocker that prevents trustworthy compile evidence. Do not start terrain architecture, DualGrid behavior, mining, CSG adoption, simulator split, or player-controller work until the compile gate can at least reach C# diagnostics.

| option | what it unblocks | why choose it now |
|---|---|---|
| Advance: Package Manager restoration | lets Unity reach C# compilation on clean `origin/main` | current measured blocker is package resolution |
| Verify: Meta GUID repair | protects asset/test identity before compiler fixes | five blank GUID `.meta` files are already confirmed |
| Audit: `StructureTagAdapter` assembly boundary | prepares the minimal CS0234 fix | repo docs identify it as the next likely compiler blocker |
| Preserve: dirty-tree split | makes later pull/merge safe | original tree mixes docs, packages, project settings, runtime deletions, and generated files |

Recommended immediate command surface for the next slice:

```text
Worktree: C:\Users\PLANNER007\VastCore\VastCore-origin-main-compile
First gate: Package Manager resolution
Then: check-compile.ps1
Then: StructureTagAdapter/meta GUID fixes only if package resolution is restored
```

## Decision Packet

Recommended decision: continue from the clean `origin/main` worktree and fix only the gate that blocks Unity from loading packages. Keep the original worktree untouched until restart docs/OpenSpec and unrelated local changes are split.

| decision | effect | tradeoff |
|---|---|---|
| Use clean worktree for restoration | clean evidence, no local dirty collision | fixes must later be transferred intentionally |
| Do not pull original tree yet | preserves current local state | local branch remains 11 commits behind |
| Treat package resolution as first blocker | matches measured failure | does not yet solve expected C# compile errors |
| Delay terrain/product work | avoids building on a broken baseline | product feature progress waits for compile evidence |

Rejected for this slice:

- Direct `git pull` into the dirty original worktree.
- Broad asmdef/namespace refactor.
- Runtime/editor code edits.
- Package dependency additions/removals without a separate approval point.
- Claiming compile success.

## Completion Matrix

```text
Fetch/readback                  [######] 6/6
Original tree preservation      [######] 6/6
Dirty inventory classification  [########] 8/8
Clean origin worktree           [######] 6/6
Unity exact-editor availability [####] 4/4
Compile check execution         [###!!] 3/5  (ran; failed before C# compile)
Package gate diagnosis          [####?] 4/5  (failure line known; root cause still open)
Meta GUID readback              [#####] 5/5
Next-slice decision             [######] 6/6
Runtime/editor code untouched   [######] 6/6
```

Handoff Gate: false.

Reason: the next step is still local and narrow: package-resolution restoration on the clean `origin/main` worktree, followed by compile rerun. No ChatGPT Review Card was emitted.

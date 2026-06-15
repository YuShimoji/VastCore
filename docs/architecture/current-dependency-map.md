# VastCore M0 Current Dependency Map

Last updated: 2026-06-15

## Repository Readback

| Field | Observed value |
|---|---|
| Remote | `https://github.com/YuShimoji/VastCore.git` |
| Branch | `main` |
| HEAD | `97c0803 chore: SP-020 で追加された .meta ファイルを追跡` |
| Upstream parity | `HEAD...origin/main` is `0 0`; `git pull --ff-only` reported already up to date. |
| Worktree | Dirty before this M0 pass; mostly instruction/document modernization files plus new M0 docs. |
| Unity | `6000.3.6f1` |
| Render pipeline | URP is configured in `GraphicsSettings.asset`; package `com.unity.render-pipelines.universal` is `17.3.0`. |
| Compile status | Failing. `scripts/check-compile.ps1` reported compiler errors and timed out after 240s while Unity exited with code 1. |

## Package Highlights

| Package | Version / source | M0 treatment |
|---|---|---|
| `com.unity.render-pipelines.universal` | `17.3.0` | Keep; project render pipeline. |
| `com.unity.probuilder` | `6.0.8` | Adapter-only / editor-tool candidate. |
| `com.beans.deform` | git `https://github.com/keenanwoodall/Deform.git` | Adapter-only candidate. |
| `com.unity.splines` | `2.8.2` | Defer as possible terrain extension input. |
| `com.coplaydev.unity-mcp` | git MCPForUnity | Tooling/integration, not terrain core. |
| Unity AI packages | assistant/generators/inference | Tooling; not terrain core. |
| `com.unity.inputsystem`, `com.unity.ugui`, TextMeshPro | registry/built-in | Game/UI only; should not leak into Terrain Core. |

## Assembly Dependency Snapshot

| Assembly | Path | References | M0 risk |
|---|---|---|---|
| `Vastcore.Utilities` | `Assets/Scripts/Utilities` | `Unity.InputSystem` | Utility layer may be too high-level if input is broadly visible. |
| `Vastcore.Core` | `Assets/Scripts/Core` | `Vastcore.Utilities`, `Unity.TextMeshPro` | Core depends on UI text package; check and shrink. |
| `Vastcore.Generation` | `Assets/Scripts/Generation` | `Unity.ProBuilder`, `Deform`, Core, Utilities | External package leakage into runtime generation. |
| `Vastcore.Terrain` | `Assets/Scripts/Terrain` | `Unity.ProBuilder`, `Unity.TextMeshPro`, `Deform`, Core, Utilities, Generation, WorldGen | External package leakage; Terrain depends on WorldGen. |
| `Vastcore.WorldGen` | `Assets/Scripts/WorldGen` | Core, Utilities, Generation | Extension candidate; currently below Terrain in docs but also referenced by Terrain. |
| `Vastcore.Player` | `Assets/Scripts/Player` | ProBuilder, TextMeshPro, InputSystem, Core, Utilities, Terrain, Generation | Game feature consuming engine; external refs acceptable only game-side. |
| `Vastcore.Camera` | `Assets/Scripts/Camera` | Core, Utilities, Player, TextMeshPro, InputSystem | Game feature. |
| `Vastcore.UI` | `Assets/Scripts/UI` | Core, Utilities, Player, TextMeshPro, InputSystem | Game feature. |
| `Vastcore.Game` | `Assets/Scripts/Game` | Core, Utilities, Player, Terrain, Camera, Generation, UI | Game composition layer. |
| `Vastcore.Editor*` | `Assets/Editor`, `Assets/Scripts/Editor` | Core, Generation, Terrain, Utilities, WorldGen, ProBuilder, Deform | Should remain editor-only. |
| `Vastcore.Tests.*` | `Assets/Tests` | Broad runtime/editor refs | Acceptable for tests, but ownership should be narrowed when refactoring. |

## Boundary Violations And Smells

| Source | Forbidden / risky dependency | Evidence | Suggested fix | Priority |
|---|---|---|---|---|
| `Vastcore.Generation` | ProBuilder and Deform concrete APIs | asmdef references plus `using UnityEngine.ProBuilder` / `using Deform` in generation files | Move concrete adapters behind `Vastcore.Integrations` or editor-only authoring APIs. | P0 after compile restore |
| `Vastcore.Terrain` | ProBuilder, Deform, TextMeshPro concrete refs | `Vastcore.Terrain.asmdef` references all three; `Terrain/Map/*` uses ProBuilder/Deform | Split terrain core from primitive/external generation. | P0 after compile restore |
| `Assets/Scripts/Terrain/Map/*.cs` | Namespace/folder/assembly mismatch | files live under Terrain asmdef but declare `Vastcore.Generation` | Move or split files, or adjust asmdef boundaries after plan. | P0 |
| `Assets/Scripts/Core/*.cs` | UI/Generation namespaces inside Core folder | `PerformanceMonitor.cs` declares `Vastcore.UI`; several Core files declare `Vastcore.Generation*` or `Vastcore.Utilities` | Re-home or rename in a compile-restoring sequence. | P1 |
| `StructureTagAdapter.cs` | Cross-assembly type mismatch | References `Vastcore.Generation.CompoundArchitecturalGenerator`; class exists in Terrain asmdef path with same namespace | Restore compile by moving adapter/type ownership or adjusting assembly boundaries without creating a cycle. | P0 |
| `.meta` files | Invalid GUIDs | Unity log lists empty `guid:` in five `.meta` files | Regenerate or repair `.meta` files with Unity-safe procedure. | P0 |
| Trail | Potential terrain-core temptation | Current trail evidence is `TrailRenderer` in `Player/Movement/TranslocationSphere.cs` | Keep as `Game Feature`; future RouteField needs separate spec. | P1 |

## Open GitHub Issue Evidence

| Issue | State | Relevant M0 meaning |
|---|---|---|
| `#29 M1: Terrain provider architecture` | open | Provider architecture is not accepted as closed by issue state. |
| `#31 M1.1: TerrainGridBootstrap` | open | 3x3 bootstrap still tracked as open. |
| `#35 M2: World streaming terrain chunks` | open | Streaming remains future extension work. |
| `#37 Fix PrimitiveTerrainObject compilation errors` | open | Compile risk around primitive terrain remains historically/currently relevant. |
| `#44 CT-1 CSG実動作検証` | open | CSG requires editor/manual verification. |
| `#45 SG-2 RandomControlTab manual test` | open | RandomControlTab acceptance remains manual/test work. |
| `#46 T5 EditMode test extension` | open | Editor test coverage is still incomplete. |

## Compile Evidence

`scripts/check-compile.ps1` produced:

- invalid GUID warnings for:
  - `Assets/Scripts/Generation/AdjacencyRuleSet.cs.meta`
  - `Assets/Scripts/Generation/PlacementZone.cs.meta`
  - `Assets/Scripts/Terrain/DualGrid/StructurePlacementSolver.cs.meta`
  - `Assets/Tests/EditMode/AdjacencyRuleSetTests.cs.meta`
  - `Assets/Tests/EditMode/StructurePlacementSolverTests.cs.meta`
- `Assets/Scripts/Generation/StructureTagAdapter.cs(3,55): error CS0234`
  for `Vastcore.Generation.CompoundArchitecturalGenerator`

The compile failure is current evidence. Older docs claiming zero compile errors
are historical until re-verified.

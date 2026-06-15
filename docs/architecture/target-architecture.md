# VastCore M0 Target Architecture

Last updated: 2026-06-15

## Decision

The near target is a small terrain engine spine that can compile and generate
Unity Terrain from owned providers without requiring game/player/UI systems or
external terrain/mesh packages. External packages remain possible only through
integration or editor authoring boundaries.

## Namespace Policy

- Current code and asmdefs use `Vastcore.*`.
- M0 recommendation: keep `Vastcore` as the immediate namespace root to avoid a
  large case-only migration while compile is failing.
- `VastCore` casing can be revisited only as a dedicated namespace migration
  after compile and tests are green.
- Do not leave mixed `VastCore` / `Vastcore` namespaces inside code.

## Proposed Assemblies

| Assembly | Responsibility | Allowed dependencies | Forbidden dependencies |
|---|---|---|---|
| `Vastcore.Utilities` | Logging, diagnostics helpers, low-level utilities | UnityEngine minimum | Game, UI, Player, Terrain, external asset concrete APIs |
| `Vastcore.Core` | Result/error/common types and stable engine abstractions | Utilities, UnityEngine minimum | TextMeshPro, InputSystem, Game, UI, Player, external asset concrete APIs |
| `Vastcore.Terrain` | Height providers, terrain settings, terrain generation, Unity Terrain output | Core, Utilities, Unity Terrain modules | Game, Player, Trail, Camera, UI, ProBuilder, Deform, CSG, editor-only APIs |
| `Vastcore.Terrain.Extensions` | Chunk/grid, DualGrid, RouteField, erosion, biome, optional terrain layers | Core, Terrain, Utilities | Game/UI dependencies and external concrete APIs |
| `Vastcore.Integrations` | ProBuilder, Deform, CSG, Splines, external package adapters | Core, Terrain, specific external package | Public core APIs exposing external concrete types |
| `Vastcore.Editor` | Terrain generation windows, inspectors, preview panels | Core, Terrain, Integrations as needed, UnityEditor | Runtime-only core contamination |
| `Vastcore.Game` | Player, Camera, UI, GameLoop, scenario, game-side trail | Core, Terrain, extensions as needed | Being referenced by Core/Terrain |
| `Vastcore.Tests.EditMode` | EditMode verification | Tested assemblies | Tests becoming production dependencies |
| `Vastcore.Tests.PlayMode` | Runtime scene/play verification | Runtime/game assemblies as needed | Editor-only APIs |

## Terrain Core Minimum

M1 should not start until M0 compile and asset integrity blockers are handled.
The minimum core surface is:

- `IHeightmapProvider`
- `NoiseHeightmapProvider`
- texture/heightmap provider route, if current assets support it
- `TerrainGenerationConfig` or replacement settings type
- terrain chunk/data model
- Unity Terrain output adapter or chunk builder
- generation report / validation result
- EditMode tests for deterministic provider behavior and parameter validation

## Trail Treatment

Current Trail evidence is game-side `TrailRenderer` usage under
`Assets/Scripts/Player/Movement/TranslocationSphere.cs`. Treat it as
`Vastcore.Game` / `Vastcore.Player` behavior for now.

Future terrain-related route data may become a `Vastcore.Terrain.Extensions`
surface only if it is specified as RouteField / Path Annotation / Vector Field
data and does not depend on Player, Camera, UI, or TrailRenderer.

## External Asset Boundary

Desired dependency direction:

```text
External package -> Vastcore.Integrations adapter -> Vastcore.Terrain abstraction
```

Forbidden direction:

```text
Vastcore.Terrain / Vastcore.Core -> ProBuilder / Deform / CSG concrete API
```

## M1 Entry Conditions

- Unity compile failure is resolved or a narrower compile blocker is documented
  and accepted as the active task.
- Invalid `.meta` GUID files are repaired or quarantined with owner/impact.
- Terrain Core has no direct dependency on Game, Player, Trail, UI, Camera, or
  external concrete asset types.
- The namespace/asmdef ownership for provider, settings, and Unity Terrain
  output is explicit.
- At least one EditMode verification path exists for provider generation.

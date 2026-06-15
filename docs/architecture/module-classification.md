# VastCore M0 Module Classification

Last updated: 2026-06-15

This document classifies the current repository surface before any refactor. It
is evidence for M0 only; it is not implementation approval.

## Evidence Used

- `git status --short --branch`
- `git log -1 --oneline --decorate`
- `git pull --ff-only`
- `ProjectSettings/ProjectVersion.txt`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `Assets/**/*.asmdef`
- namespace and `using` scans under `Assets/Scripts`, `Assets/Editor`, and
  `Assets/Tests`
- GitHub issues `#29`, `#31`, `#35`, `#37`, `#44`, `#45`, `#46`

## Classification Rules

| Category | Meaning |
|---|---|
| Engine Core | Required for the smallest reusable terrain engine. |
| Engine Extension | Terrain-related but removable without breaking core generation. |
| Integration | External package, tool, or Unity package bridge. |
| Game Feature | Game-side use of the engine. |
| Experiment | Unproven, optional, or currently distant from M0/M1. |
| Deprecated / Quarantine | Broken, stale, duplicate, overcoupled, or not safe to treat as normal work yet. |

## Current Classification

| Item | Path | Current Role | Proposed Category | Reason | Risk |
|---|---|---|---|---|---|
| Terrain provider abstraction | `Assets/Scripts/Terrain/Providers/IHeightmapProvider.cs` | Height source contract | Engine Core | Directly supports M1 provider architecture. | API must stay free of game/external concrete types. |
| Noise heightmap provider | `Assets/Scripts/Terrain/Providers/NoiseHeightmapProvider.cs` | Procedural height source | Engine Core | Required for external-asset-free terrain generation. | Needs compile restored before acceptance. |
| Terrain config/settings | `Assets/Scripts/Terrain/Config/*.cs` | Provider settings and factory | Engine Core | ScriptableObject config path for generation. | Current API shape must be checked against tests. |
| Terrain chunk/grid bootstrap | `Assets/Scripts/Terrain/TerrainChunk*.cs`, `TerrainGridBootstrap.cs` | Unity Terrain chunk creation | Engine Core / Extension boundary | M1/M2 bridge; core for minimal output, extension once streaming expands. | Open issues still track bootstrap/streaming work. |
| Terrain facade | `Assets/Scripts/Terrain/Facade/*.cs` | Facade from terrain to WorldGen fields | Engine Extension | Connects terrain to a broader field pipeline. | Keep optional; avoid making WorldGen mandatory for M1. |
| HeightMap/Terrain generator | `Assets/Scripts/Generation/HeightMapGenerator.cs`, `TerrainGenerator.cs` | Legacy/active generation path | Engine Core candidate | Contains current height/noise generation logic. | Physical path, asmdef, and namespace differ from desired Terrain Core split. |
| DualGrid + prefab stamp | `Assets/Scripts/Terrain/DualGrid/*.cs` | Designer stamp terrain path | Engine Extension | Current invariant names DualGrid + HeightMap + Prefab Stamp as main axis. | Must not block minimal provider/output core. |
| WorldGen pipeline | `Assets/Scripts/WorldGen/**` | Graph/field/grammar/stamp systems | Engine Extension / Experiment | Useful long-term generation layer, not required for M1 minimum. | `Vastcore.Terrain` currently depends on `Vastcore.WorldGen`. |
| Volumetric/MarchingCubes path | `Assets/Scripts/Terrain/Volumetric`, `MeshExtraction` | 3D volumetric terrain | Experiment / Hold | Invariants require explicit reapproval to return 3D voxel/Marching Cubes to main path. | Do not select as default next work. |
| Marching Squares | `Assets/Scripts/Terrain/MarchingSquares` | 2D contour/spline terrain | Experiment / Hold | Invariants require conflict resolution with Prefab Stamp before main-path use. | Keep out of M1 core. |
| Erosion/GPU/cache/optimization | `Assets/Scripts/Terrain/Erosion`, `GPU`, `Cache`, `Optimization` | Quality/performance systems | Engine Extension | Valuable after minimum provider/output works. | Some namespaces currently live under `Vastcore.Generation.*`. |
| ProBuilder primitive generation | `Assets/Scripts/Generation/Map`, `Assets/Scripts/Terrain/Map` | Primitive/architectural mesh generation | Integration / Experiment | Depends on ProBuilder concrete APIs. | External type leakage into runtime assemblies. |
| Deform integration | `Assets/Scripts/Generation/Deform*.cs`, `Assets/Scripts/Terrain/Map/HighQualityPrimitiveGenerator*.cs` | Deform bridge and deformation helpers | Integration | Should be behind adapter/facade, not core generation. | Current runtime asmdefs reference `Deform`. |
| CSG providers | `Assets/Editor/StructureGenerator/Utils/*Csg*.cs` | Editor CSG bridge | Integration | Editor-only external/tool bridge. | Keep editor-only; avoid runtime dependency. |
| Structure Generator editor | `Assets/Editor/StructureGenerator/**` | Artist/editor authoring tool | Integration / Editor Tool | Useful authoring surface, not core engine. | Open issues require manual/Editor verification. |
| Player systems | `Assets/Scripts/Player/**` | Player movement/interaction | Game Feature | Game-side only. | Do not pull into Terrain Core. |
| Trail renderer use | `Assets/Scripts/Player/Movement/TranslocationSphere.cs` | Visual movement trail | Game Feature | Current Trail is game/player visual behavior, not terrain engine. | Future RouteField should be a separate terrain extension if needed. |
| Camera/UI/Game manager | `Assets/Scripts/Camera`, `Assets/Scripts/UI`, `Assets/Scripts/Game` | Game-side runtime | Game Feature | Should consume engine, not be consumed by engine. | Core asmdef currently references TextMeshPro; keep UI out of core. |
| Tests | `Assets/Tests/EditMode`, `Assets/Tests/PlayMode` | Verification | Supporting / Tests | Required for acceptance. | Test asmdefs depend broadly; later narrow by owning surface. |
| TextMesh Pro samples | `Assets/TextMesh Pro/Examples & Extras` | Vendor sample content | Quarantine candidate | Not part of terrain engine. | Can obscure scans and sample inventory. |
| Duplicate/test folders | `Assets/test`, `Assets/test_Terrain`, `Assets/TerrainPresets 1` | Unclear local/test artifacts | Quarantine candidate | Purpose is not clear from M0 evidence. | Do not delete until owner and impact are known. |

## M0 Classification Conclusion

The repository already contains a usable terrain-provider direction, but the
current module map mixes core terrain, architectural primitive generation,
external package integration, and game/UI/player features. M0 should restore a
small, compile-safe map before any broad move: identify the core provider/output
spine, isolate ProBuilder/Deform/CSG to integration/editor surfaces, and leave
Trail as game-side until a RouteField extension is explicitly specified.

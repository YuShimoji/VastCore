# WorldGen Architecture (M0-M3 Baseline)

Last Updated: 2026-02-22

## 1. Goal

This document defines the replacement-ready skeleton for the new world generation stack while keeping the current heightmap terrain path intact.

- `Classic`: existing heightmap + Unity Terrain
- `Volumetric`: new density field pipeline
- `Hybrid`: classic prior + local volumetric overlay

The Single Source of Truth is `WorldGenRecipe`.

## 2. Assembly Boundaries

### 2.1 New Assembly

- `Vastcore.WorldGen`
  - Path: `Assets/Scripts/WorldGen/`
  - References:
    - `Vastcore.Core`
    - `Vastcore.Utilities`
    - `Vastcore.Generation`

### 2.2 Existing Assemblies Updated

- `Vastcore.Terrain` now references `Vastcore.WorldGen`
- `Vastcore.Editor` now references `Vastcore.WorldGen`
- `Vastcore.Testing` now references `Vastcore.WorldGen`

## 3. Engine Split

### 3.1 Field Engine (implemented baseline)

Core types:

- `Vastcore.WorldGen.FieldEngine.IDensityField`
- `Vastcore.WorldGen.FieldEngine.CompositeDensityField`
- `Vastcore.WorldGen.FieldEngine.NoiseDensityField`
- `Vastcore.WorldGen.FieldEngine.StampDensityField`
- `Vastcore.WorldGen.FieldEngine.FieldEngineManager`
- `Vastcore.WorldGen.FieldEngine.IHeightmapFieldFactory`

Recipe types:

- `Vastcore.WorldGen.Recipe.WorldGenRecipe`
- `Vastcore.WorldGen.Recipe.FieldLayer`
- `Vastcore.WorldGen.Recipe.FieldLayerType`
- `Vastcore.WorldGen.Recipe.BooleanOp`

Stamp types:

- `Vastcore.WorldGen.Stamps.IStamp`
- `Vastcore.WorldGen.Stamps.StampBase`
- `Vastcore.WorldGen.Stamps.SphereStamp`
- `Vastcore.WorldGen.Stamps.BoxStamp`
- `Vastcore.WorldGen.Stamps.CapsuleStamp`

Terrain bridge:

- `Vastcore.Terrain.Facade.TerrainHeightmapFieldFactory`
  - Converts existing `HeightmapProviderSettings` to `IDensityField`
  - Uses tile cache and `IHeightmapProvider.Generate(...)`
  - Formula: `density = height01 * heightScale - worldY`

### 3.2 Graph Engine (implemented baseline)

- `Vastcore.WorldGen.GraphEngine.IGraphEngine`
- `Vastcore.WorldGen.GraphEngine.GraphEngineManager`
- `Vastcore.WorldGen.GraphEngine.RoadGraphGenerator`
- `Vastcore.WorldGen.GraphEngine.RiverGraphGenerator`
- `Vastcore.WorldGen.GraphEngine.GraphFieldBurner`
- `Vastcore.WorldGen.GraphEngine.GraphAsset`
- `Vastcore.WorldGen.GraphEngine.IGraphAutoGeneratorAdapter`
- `Vastcore.WorldGen.GraphEngine.GraphUpdateInfo`
- `Vastcore.WorldGen.GraphEngine.WorldGenGraphGizmoVisualizer`
- `Vastcore.WorldGen.GraphEngine.ConnectivityGraph` and model classes

### 3.3 Grammar Engine (stub slot)

- `Vastcore.WorldGen.GrammarEngine.IGrammarEngine`
- `Vastcore.WorldGen.GrammarEngine.GrammarEngineStub`
- `Vastcore.WorldGen.GrammarEngine.StructureBlueprint`

### 3.4 Deformation Engine (stub slot)

- `Vastcore.WorldGen.DeformationEngine.IDeformationEngine`
- `Vastcore.WorldGen.DeformationEngine.DeformationEngineStub`
- `Vastcore.WorldGen.DeformationEngine.DirtyRegion`

## 4. Pipeline

- `Vastcore.WorldGen.Pipeline.WorldGenPipeline`
- `Vastcore.WorldGen.Pipeline.WorldGenContext`
- `Vastcore.WorldGen.Observability.WorldGenStats`
- `Vastcore.WorldGen.Observability.LayerDebugData`

Pipeline execution order:

1. Build base field from `WorldGenRecipe`
2. Generate graph and burn into field
3. Generate grammar structures (stub currently empty)
4. Apply deformation phase (stub currently no-op)

## 5. Mesh Extraction and Chunk Streaming (M3)

Implemented in `Vastcore.Terrain`:

- `Vastcore.Terrain.MeshExtraction.IMeshExtractor`
- `Vastcore.Terrain.MeshExtraction.MarchingCubesMeshExtractor`
  - CPU minimal implementation via marching tetrahedra
- `Vastcore.Terrain.Volumetric.VolumetricChunk`
- `Vastcore.Terrain.Volumetric.VolumetricChunkPool`
- `Vastcore.Terrain.Volumetric.VolumetricDirtyRegionTracker`
- `Vastcore.Terrain.Volumetric.VolumetricStreamingController`

Integration:

- `TerrainFacade` now initializes and drives `VolumetricStreamingController` for Volumetric/Hybrid mode.
- Graph affected bounds are mapped to dirty chunk keys and regenerated locally.
- Per-chunk generation stats are recorded in `WorldGenStats` and periodically logged.

### 5.1 M3 Follow-up (implemented)

- Load smoothing:
  - distance-priority scheduling for create/regenerate target chunks
  - frame budget control via max meshing milliseconds per frame
- Allocation reduction:
  - `DensityGridPool` for reusable density buffers
- Seam mitigation (lightweight):
  - optional border vertex snapping and optional vertex quantization
  - applied after extraction before mesh upload
- Seam strategy boundary:
  - `IChunkSeamProcessor` + `ChunkSeamProcessor` introduced for swap-ready seam processing
  - `VolumetricStreamingController` now delegates post-process seam handling
- Mesh lifecycle safety:
  - `VolumetricChunk` destroys replaced/cleared runtime meshes to avoid native mesh leaks
- Regression safety:
  - EditMode tests added for dirty tracking, density grid pooling, and chunk mesh lifecycle

## 6. Data Flow (ASCII)

```text
WorldGenRecipe (SSOT)
   |
   v
WorldGenPipeline.Execute(recipe)
   |
   +--> FieldEngineManager.BuildField(recipe)
   |       +--> Heightmap layer via IHeightmapFieldFactory (Terrain bridge)
   |       +--> NoiseDensityField / CaveDensityField
   |       +--> StampDensityField
   |
    +--> GraphEngine (roads/rivers + field burn)
   +--> GrammarEngine (stub)
   +--> DeformationEngine (stub)
   |
   v
WorldGenContext { DensityField, GraphData, GrammarData, Stats }
   |
   v
VolumetricStreamingController
   +--> FieldEngine.FillDensityGrid(bounds)
   +--> MarchingCubesMeshExtractor.ExtractMesh(...)
   +--> VolumetricChunk.BuildMesh(...)
   +--> Dirty region selective regeneration
```

## 7. Graph Observability and Extension Hooks

- Scene Gizmo visualization:
  - `WorldGenGraphGizmoVisualizer` can draw:
    - center polyline
    - width envelope
    - intersections/junction nodes
- Adapter slot:
  - `IGraphAutoGeneratorAdapter` can replace internal road/river auto generation.
- Update hook:
  - `GraphEngineManager.GraphUpdated` emits `GraphUpdateInfo` with affected bounds.
  - `WorldGenContext.GraphAffectedBounds` stores latest affected AABBs.
- Editor overlay:
  - `Tools/Vastcore/WorldGen/Graph Overlay`
  - `WorldGenGraphOverlayWindow` provides toggles, legend, and runtime statistics in one panel.

## 8. Determinism Rules

- Main seed is stored in `WorldGenRecipe.seed`
- Layer seeds are derived deterministically from main seed + layer index
- Any random expansion should route through `DeterministicRng`

## 9. Known Next Steps

- M4: integrate actual grammar generator
- M5: split visual deform and physical deform with local re-extraction
- M3 follow-up: optimize extraction (Burst/Jobs or compute), reduce GC allocations, add seam handling and LOD transition

# Terrain Algorithm Notes: DualGrid and HeightMap

## 1. Scope
This note summarizes implemented behavior in the current codebase for:
- HeightMap-based terrain generation (classic TerrainData flow)
- DualGrid topology and vertical extrusion pipeline

## 2. HeightMap path (current implementation)
Primary classes:
- `Assets/MapGenerator/Scripts/TerrainGenerator.cs`
- `Assets/MapGenerator/Scripts/HeightMapGenerator.cs`
- `Assets/Scripts/Generation/TerrainGenerationProfile.cs`

### 2.1 Mode dispatch
`HeightMapGenerator.GenerateHeights(TerrainGenerator)` dispatches by mode:
- `Noise`
- `HeightMap`
- `NoiseAndHeightMap`

### 2.2 Noise mode
- Deterministic seed offset via `GetDeterministicOffsetFromSeed(int)`.
- Multi-octave Perlin accumulation with persistence/lacunarity.
- Final values normalized to `[0, 1]`.

### 2.3 HeightMap mode
- Reads selected channel (`R/G/B/A/Luminance`).
- Applies UV tiling + UV offset + optional vertical flip.
- Bilinear sampling from source texture and optional invert.
- Final value uses scale/offset then clamp to `[0, 1]`.

### 2.4 Combined mode
- Blends noise and heightmap using local gradient-derived influence.
- High gradient zones reduce noise influence to preserve authored features.

### 2.5 Test coverage status
EditMode tests exist for channel selection, seed determinism, UV tiling, invert behavior, and terrain integration:
- `Assets/Tests/EditMode/HeightMapGeneratorTests.cs`
- `Assets/Tests/EditMode/TerrainGeneratorIntegrationTests.cs`

## 3. DualGrid path (current implementation)
Primary classes:
- `Assets/Scripts/Terrain/DualGrid/IrregularGrid.cs`
- `Assets/Scripts/Terrain/DualGrid/GridTopology.cs`
- `Assets/Scripts/Terrain/DualGrid/ColumnStack.cs`
- `Assets/Scripts/Terrain/DualGrid/VerticalExtrusionGenerator.cs`

### 3.1 Topology
- `IrregularGrid.GenerateGrid(int radius)` delegates topology construction to `GridTopology.GenerateHexToQuadGrid(...)`.
- Output data model: `Node` list + `Cell` list.

### 3.2 Relaxation
- `IrregularGrid.ApplyRelaxation(seed, jitterAmount, usePerlinNoise)` offsets node positions.
- Supports Perlin-based and random jitter modes.
- Includes convexity warning check through `ValidateConvexity()`.

### 3.3 Vertical extrusion
`VerticalExtrusionGenerator` supports:
- `GenerateFromHeightMap(Texture2D)`
- `GenerateFromNoise(seed, maxHeight, noiseScale)`
- `GenerateFromHeightMapArray(float[,], mapSize)`

Current behavior:
- Converts cell center world coordinates to normalized UV.
- Samples height source and converts to discrete layer count.
- Writes solid layers into `ColumnStack`.

## 4. Current technical constraints
1. `VerticalExtrusionGenerator` still uses `Debug.Log*` directly and should migrate to `VastcoreLogger`.
2. HeightMap-to-DualGrid mapping currently assumes a fixed world range (`-10..10`) and needs profile-driven mapping.
3. DualGrid pipeline has debug/structure foundation but no finalized runtime mesh output pipeline in this document scope.
4. Some docs reference old Unity versions; consolidate around `6000.3.3f1`.

## 5. Recommended next engineering steps
1. Add profile-based coordinate mapping for DualGrid height sampling.
2. Define mesh generation contract from `ColumnStack` to renderable chunks.
3. Add DualGrid-focused unit tests for extrusion determinism and boundary sampling.
4. Integrate the vertical slice roadmap checkpoints into sprint planning.

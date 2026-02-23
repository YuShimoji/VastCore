# REPORT_TASK_WORLDGEN_M3_FOLLOW_20260222

Date: 2026-02-22
Scope: M3 follow-up (load smoothing, seam mitigation, allocation reduction)

## 1. Implemented

1. Load smoothing
   - distance-priority chunk scheduling for create/regenerate
   - frame budget using meshing time (`maxMeshingTimeMsPerFrame`)
2. Allocation reduction
   - added `DensityGridPool`
   - switched mesh generation path to acquire/release pooled `DensityGrid`
3. Seam mitigation (lightweight)
   - border vertex snapping
   - optional vertex quantization
   - applied post extraction before collider update
4. Observability extension
   - stats log includes active/dirty chunk counts and meshing budget
5. Mesh lifecycle hardening
   - `VolumetricChunk` now destroys previous runtime mesh on rebuild
   - `VolumetricChunk` now destroys runtime mesh on clear/destroy
   - prevents native mesh leak during repeated dirty-region regeneration
6. Regression tests (EditMode)
   - `DensityGridPoolTests`
   - `VolumetricDirtyRegionTrackerTests`
   - `VolumetricChunkMeshLifecycleTests`
   - added `Vastcore.WorldGen` reference to `Vastcore.Tests.EditMode` asmdef for pool test type resolution

## 2. Changed files

- `Assets/Scripts/Terrain/Volumetric/DensityGridPool.cs`
- `Assets/Scripts/Terrain/Volumetric/VolumetricStreamingController.cs`
- `Assets/Scripts/Terrain/Volumetric/VolumetricChunk.cs`
- `Assets/Tests/EditMode/DensityGridPoolTests.cs`
- `Assets/Tests/EditMode/VolumetricDirtyRegionTrackerTests.cs`
- `Assets/Tests/EditMode/VolumetricChunkMeshLifecycleTests.cs`
- `Assets/Tests/EditMode/Vastcore.Tests.EditMode.asmdef`
- `docs/02_design/WorldGenArchitecture.md`
- `docs/02_design/ASSEMBLY_ARCHITECTURE.md`

## 3. Compile verification

Executed:

```powershell
./scripts/check-compile.ps1
```

Result:

- Exit code: `0`
- Status: `Compilation check passed`
- Log: `artifacts/logs/compile-check.log`

## 4. Residual risks and next step

1. Border snapping mitigates tiny cracks but does not solve true multi-resolution seam stitching.
2. LOD seam stitching and transition mesh generation remain a dedicated next task.

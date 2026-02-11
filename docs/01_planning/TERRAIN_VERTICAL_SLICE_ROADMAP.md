# Terrain Vertical Slice Roadmap

## Goal
Validate and demonstrate terrain generation quality end-to-end using a focused vertical slice centered on HeightMap + DualGrid workflows.

## Slice Definition
In-scope:
- Terrain generation modes: Noise / HeightMap / NoiseAndHeightMap
- DualGrid topology + relaxation + vertical extrusion visualization
- Determinism, visual quality, and baseline performance checks

Out-of-scope (for this slice):
- Full production biome ecosystem
- Large-scale streaming world
- Final art polish

## Milestones

### M0: Baseline Reproduction
Deliverables:
- Local environment synchronized and reproducible
- Existing EditMode tests runnable
- Manual generation smoke log recorded

Exit criteria:
- No compile errors in Unity Editor
- Existing HeightMap/Terrain integration tests pass

### M1: Algorithm Visibility
Deliverables:
- Shared algorithm note for HeightMap and DualGrid
- Instrumented checklist for expected outputs per generation mode

Exit criteria:
- Team can trace each effect to concrete class/method
- Known constraints and risks are documented

### M2: Visual Effect Validation
Deliverables:
- Representative sample scenes/presets for each mode
- Side-by-side captures (Noise vs HeightMap vs Combined vs DualGrid extrusion)

Exit criteria:
- Terrain silhouettes and macro features are distinguishable by mode
- HeightMap channel/UV/invert effects are visually verifiable

### M3: Determinism and Stability
Deliverables:
- Seed reproducibility report
- Regression checklist for key parameters

Exit criteria:
- Same seed/profile yields consistent results
- No critical runtime errors during repeated regeneration

### M4: Performance Baseline
Deliverables:
- Baseline profiling report (Editor Play Mode)
- Cost hotspots and short-term optimization backlog

Exit criteria:
- Slice scenario maintains target baseline agreed by team (initial target: stable interactive editor operation)
- Hotspots are ranked with actionable follow-up items

### M5: Demo-Ready Vertical Slice
Deliverables:
- One reproducible demo scene + operation guide
- Handover package: roadmap status, risks, next sprint proposals

Exit criteria:
- Reviewer can follow guide and reproduce terrain effects
- Remaining work is clearly partitioned into post-slice backlog

## Evaluation Metrics
- Functional: generation succeeds for all targeted modes
- Visual: mode-specific terrain effect differences are observable
- Determinism: repeated runs with same seed/profile are equivalent within tolerance
- Stability: no blocker exceptions during repeated generation cycle
- Performance: profiler trace captured and compared against previous milestone

## Artifact Checklist
- `docs/03_guides/REDEVELOPMENT_LOCAL_SETUP.md`
- `docs/02_design/TERRAIN_ALGORITHM_NOTES_DUALGRID_HEIGHTMAP.md`
- `docs/03_guides/TERRAIN_VERTICAL_SLICE_RUNBOOK.md`
- `docs/tasks/TASK_031_TerrainVerticalSlice_Kickoff.md`
- `docs/tasks/TASK_032_DualGridHeightMap_ProfileMapping.md`
- `docs/tasks/TASK_033_DualGridHeightMap_ProfileMappingImplementation.md`
- test/profiling evidence files under `docs/04_reports/` (when each milestone completes)

## Suggested Sprint Mapping
- Sprint A: M0-M1
- Sprint B: M2-M3
- Sprint C: M4-M5

## Immediate Start Sequence
1. Execute `TASK_031` and generate kickoff report from `docs/04_reports/REPORT_TASK_031_VerticalSliceKickoff_TEMPLATE.md`.
2. Execute `TASK_032` design phase and generate mapping report from `docs/04_reports/REPORT_TASK_032_DualGridHeightMapProfileMapping_TEMPLATE.md`.
3. Execute implementation via `TASK_033` with Worker prompt `docs/inbox/WORKER_PROMPT_TASK_033.md`.
4. Execute Unity validation via `TASK_034` with Worker prompt `docs/inbox/WORKER_PROMPT_TASK_034.md`.
5. Promote approved outputs into next implementation sprint tasks.

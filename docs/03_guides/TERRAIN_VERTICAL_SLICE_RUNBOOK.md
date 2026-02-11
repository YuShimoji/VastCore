# Terrain Vertical Slice Runbook

## Purpose
Provide an execution-first operating guide to progress the terrain vertical slice while Unity/compiler blockers are being handled separately.

## Inputs
- Roadmap: `docs/01_planning/TERRAIN_VERTICAL_SLICE_ROADMAP.md`
- Algorithm note: `docs/02_design/TERRAIN_ALGORITHM_NOTES_DUALGRID_HEIGHTMAP.md`
- Local setup: `docs/03_guides/REDEVELOPMENT_LOCAL_SETUP.md`
- Tasks: `docs/tasks/TASK_031_TerrainVerticalSlice_Kickoff.md`, `docs/tasks/TASK_032_DualGridHeightMap_ProfileMapping.md`

## Operating Principle
1. Keep forward progress on planning, artifact creation, and deterministic verification design.
2. Defer compile/dependency blockers to dedicated fix tasks.
3. Log every milestone output in `docs/04_reports/`.

## Execution Steps

### Step 1: Kickoff (M0-M1)
1. Use `TASK_031` as the active task card.
2. Confirm current branch and workspace state:
```powershell
git status --short --branch
```
3. Record kickoff assumptions and known blockers in report template:
- `docs/04_reports/REPORT_TASK_031_VerticalSliceKickoff_TEMPLATE.md`

### Step 2: Artifact Preparation (M1-M2)
1. Prepare comparison matrix for mode outputs:
- Noise
- HeightMap
- NoiseAndHeightMap
- DualGrid extrusion
2. Define sample profiles and parameter presets to compare consistently.
3. Save artifacts and references in the report body.

### Step 3: Determinism and Mapping Design (M2-M3)
1. Use `TASK_032` to define profile-driven mapping behavior for DualGrid height sampling.
2. Document edge cases:
- world bounds mismatch
- UV wrap/clamp behavior
- layer quantization
3. Record target test cases before implementation.

### Step 4: Performance/Readiness Prep (M4-M5)
1. Prepare profiling checklist and expected capture points.
2. Draft demo operation script and reviewer steps.
3. Package final handoff summary with open risks.

## Evidence Policy
- One report per milestone or per task completion.
- Include:
  - date/time
  - branch/commit
  - decisions
  - blockers
  - next actions

## Out of Scope for This Runbook
- Directly resolving existing Unity compile blocker chains.
- Final production-level art pass.

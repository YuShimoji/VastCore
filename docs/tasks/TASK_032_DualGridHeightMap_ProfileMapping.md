# Task: DualGrid HeightMap Profile Mapping Design

## Status
Status: DONE

## Report
Report: `docs/04_reports/REPORT_TASK_032_DualGridHeightMapProfileMapping_2026-02-11.md`

## Tier
Tier: 2

## Branch
Branch: feature/TASK_032-dualgrid-profile-mapping (recommended)

## Created
Created: 2026-02-11

## Objective
Design and specify profile-driven coordinate mapping for `VerticalExtrusionGenerator` so DualGrid height sampling is not tied to fixed world range assumptions.

## Context
- Current limitation documented in `docs/02_design/TERRAIN_ALGORITHM_NOTES_DUALGRID_HEIGHTMAP.md`.
- Existing implementation maps world center using fixed range (`-10..10`) assumptions.
- Vertical slice requires predictable behavior across varying terrain extents.

## Focus Area
- `Assets/Scripts/Terrain/DualGrid/VerticalExtrusionGenerator.cs`
- `Assets/Scripts/Generation/TerrainGenerationProfile.cs`
- Design docs and test planning artifacts

## Non-Goals
- Full runtime mesh pipeline implementation
- Broad terrain architecture rewrite

## Proposed Outputs
1. Mapping spec: world bounds source.
2. Mapping spec: UV mapping mode (clamp/wrap).
3. Mapping spec: max height conversion policy.
4. API adjustment proposal for DualGrid settings/profile.
5. Test design for deterministic boundary sampling.

## DoD
- [x] Mapping spec document added and reviewed.
- [x] API change list defined with backward compatibility notes.
- [x] Test matrix prepared for edge and nominal cases.
- [x] Implementation follow-up task is clearly defined.

## Constraints
- Keep design aligned with existing `TerrainGenerationProfile` model.
- Keep output backward-compatible where practical.

## Stopping Conditions
- Team can implement mapping without further architectural clarification.
- Test conditions are explicit enough for regression coverage.

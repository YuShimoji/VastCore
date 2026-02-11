# Task: TASK_036 DualGrid Inspector Profile Preview

## Status
Status: READY

## Tier
Tier: 2

## Branch
Branch: feature/TASK_036-dualgrid-inspector-preview

## Created
Created: 2026-02-12

## Objective
Enable profile-driven DualGrid sampling preview from Inspector by wiring `TerrainGenerationProfile.DualGridHeightSampling` into debug generation flow.

## Context
- `TASK_033` implemented profile mapping primitives.
- `TASK_034` static review indicates `GridDebugVisualizer` currently does not pass sampling settings.
- Improvement proposal requests practical Inspector-level preview control.

## Focus Area
- `Assets/Scripts/Terrain/DualGrid/GridDebugVisualizer.cs`
- `Assets/Scripts/Generation/TerrainGenerationProfile.cs` (reference-only/minimal wiring)
- Related editor-facing serialized fields/tooltips

## Forbidden Area
- Full DualGrid runtime pipeline redesign
- Non-DualGrid editor overhauls

## Scope
1. Add optional `TerrainGenerationProfile` reference in debug visualizer.
2. Pass `DualGridHeightSamplingSettings` into `VerticalExtrusionGenerator` calls when profile is assigned.
3. Keep fallback behavior when no profile is assigned.
4. Document usage in a short guide/report.

## DoD
- [ ] Inspector exposes profile assignment for debug visualizer.
- [ ] Assigned profile affects sampling behavior in preview generation path.
- [ ] Null profile keeps legacy behavior.
- [ ] Report created: `docs/04_reports/REPORT_TASK_036_DualGridInspectorProfilePreview.md`.

## Deliverables
1. Minimal code wiring for preview path
2. Usage notes and screenshots/log references
3. Completion report

## Stopping Conditions
- Preview flow can switch between profile-driven and legacy behavior without code changes.
- Any unresolved issue is recorded with reproduction steps.

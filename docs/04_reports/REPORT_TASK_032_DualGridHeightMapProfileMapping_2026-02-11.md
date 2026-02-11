# Report: TASK_032 DualGrid HeightMap Profile Mapping

## Metadata
- Task ID: TASK_032
- Date: 2026-02-11
- Author: Codex
- Branch: main
- Commit: working tree (not committed)
- Status: DONE

## Goal
Produce design-ready mapping specification to remove hardcoded world-range assumptions in DualGrid height sampling.

## Design Summary
1. Bounds source: profile-defined world min/max (`WorldMinXZ`, `WorldMaxXZ`).
2. UV mapping policy: explicit `Clamp` or `Wrap`.
3. Height quantization policy: selectable floor/round/ceil behavior.

## API Change Draft
- Target files:
  - `Assets/Scripts/Generation/TerrainGenerationProfile.cs`
  - `Assets/Scripts/Terrain/DualGrid/VerticalExtrusionGenerator.cs`
- Planned additions:
  - `DualGridHeightSamplingSettings` container
  - `DualGridUvAddressMode` enum
  - `DualGridHeightQuantization` enum
- Compatibility:
  - preserve legacy behavior if new settings are not configured

## Test Matrix Draft
- Nominal:
  - profile bounds aligned with generated world range
- Boundary:
  - out-of-range coordinates under clamp/wrap modes
  - min/max boundary exact hits
- Determinism:
  - same profile + same height source -> identical layer counts

## Open Questions
1. Should profile bounds derive from terrain width/length automatically or be explicitly user-defined?
2. Should quantization be global or per-profile per-generation-mode?

## Next Actions
1. Review mapping spec with team and lock enum/field names.
2. Convert this design into implementation task(s) after compile blocker priorities are aligned.
3. Add focused tests when implementation starts.

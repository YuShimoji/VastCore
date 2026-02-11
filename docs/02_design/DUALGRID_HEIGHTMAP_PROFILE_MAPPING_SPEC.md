# DualGrid HeightMap Profile Mapping Spec (Draft)

## Status
Draft v0.1 (design only, no runtime code changes yet)

## Goal
Replace fixed world-range sampling assumptions in `VerticalExtrusionGenerator` with profile-driven mapping so DualGrid height sampling behaves consistently across map scales.

## Current Problem
`VerticalExtrusionGenerator` currently normalizes cell center coordinates using a fixed range assumption. This causes unstable sampling behavior when world extents differ from that implicit range.

## Design Requirements
1. Mapping bounds must come from profile data, not hardcoded constants.
2. UV addressing mode must be explicit (`Clamp` or `Wrap`).
3. Height quantization policy must be deterministic and documented.
4. Existing behavior should remain available as compatibility fallback.

## Proposed Profile Model

### New Settings Container
Suggested addition under `TerrainGenerationProfile`:
- `DualGridHeightSamplingSettings`

Suggested fields:
- `bool UseProfileBounds = true`
- `Vector2 WorldMinXZ = new Vector2(-10f, -10f)`
- `Vector2 WorldMaxXZ = new Vector2(10f, 10f)`
- `DualGridUvAddressMode UvAddressMode = DualGridUvAddressMode.Clamp`
- `DualGridHeightQuantization HeightQuantization = DualGridHeightQuantization.RoundToInt`

### Enums
- `DualGridUvAddressMode`
  - `Clamp`
  - `Wrap`
- `DualGridHeightQuantization`
  - `FloorToInt`
  - `RoundToInt`
  - `CeilToInt`

## Mapping Formula
For cell center `world = (x, z)`:
1. `uRaw = InverseLerp(WorldMinXZ.x, WorldMaxXZ.x, x)`
2. `vRaw = InverseLerp(WorldMinXZ.y, WorldMaxXZ.y, z)`
3. Addressing:
   - Clamp: `u = Clamp01(uRaw)`, `v = Clamp01(vRaw)`
   - Wrap: `u = Repeat(uRaw, 1f)`, `v = Repeat(vRaw, 1f)`
4. Sample indices:
   - `sx = Clamp(Floor(u * width), 0, width - 1)`
   - `sy = Clamp(Floor(v * height), 0, height - 1)`
5. Layer count:
   - `heightValue` in `[0, 1]`
   - `layerCount = Quantize(heightValue * maxHeight, HeightQuantization)`

## Backward Compatibility
- If new settings are absent, keep legacy mapping behavior.
- Default settings mirror existing practical assumptions as closely as possible.

## Determinism Notes
- Mapping must be pure function of profile + input position + height source.
- No random factor in mapping/quantization stage.
- Same profile/seed/height source should produce identical layer stacks.

## Test Matrix (Design)
1. Bounds: small/large/asymmetric world ranges.
2. UV mode: clamp vs wrap edge behavior.
3. Quantization: floor/round/ceil at threshold values.
4. Regression: legacy fallback path output parity.

## Implementation Follow-Up
1. Add settings and enums to profile model.
2. Add overload/parameter for mapping settings in `VerticalExtrusionGenerator`.
3. Add focused tests for boundary sampling and determinism.

# Tasks: Terrain Visual Improvement

## Phase 1: Baseline Visuals
- [x] Default URP Lit material with Standard fallback
- [x] Enable shadow casting and receiving
- [x] Distance-based LOD scaling (L1/L2) with smooth lerp

## Phase 2: Tuning & Options
- [ ] Serialize LOD distances and scales (inspector)
- [ ] Add global toggle to disable LOD
- [ ] Document performance tradeoffs (shadows vs quality)

## Phase 3: Validation
- [ ] Check no pink materials on URP projects
- [ ] Visual check of popping vs smoothness at thresholds
- [ ] Measure frame time impact at 60 FPS target

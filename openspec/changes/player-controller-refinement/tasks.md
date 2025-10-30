# Tasks: Player Controller Refinement

## Phase 1: Feel Improvements

- [x] Add input sensitivity parameter and apply to movement input
- [x] Add rotation speed parameter for facing direction
- [x] Add camera follow with smoothing and look-at height
- [x] Add jump buffer (jumpBufferDuration) and integrate with coyote time
- [x] Add sprint FOV effect with lerp and toggle

## Phase 2: Polishing & Safety

- [ ] Add clamped limits for sensitivity (0.1–3.0) and FOV (default±20)
- [ ] Expose a master toggle to disable all camera effects
- [ ] Add editor tooltips and defaults validation

## Phase 3: Testing

- [ ] Edge case tests: coyote + buffer at platform edges
- [ ] Performance check: LateUpdate camera + physics at 60 FPS target
- [ ] Playtest tuning: rotationSpeed, moveForce, sprintFov

## Acceptance Checks

- [ ] No lost-jump on 100 attempts at ledge edge (buffer active)
- [ ] FOV returns to default within 0.5s after sprint end
- [ ] Rotation feels responsive without jitter (no overshoot)

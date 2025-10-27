# Player Controller Refinement Proposal

## Overview

Refine the player controller to deliver tight, responsive, and readable controls. Build on current movement (forces + RB) and add quality-of-life features with optional toggles.

## Goals

- Consistent ground/air control with jump buffer and coyote time
- Camera follow polish with smoothing and FOV sprint effect
- Configurable acceleration/deceleration and rotation responsiveness
- Input System mappings and sensitivity settings exposed in Inspector

## Acceptance Criteria

- Input latency feels minimal; no lost jumps during edge cases
- Camera motion is stable and readable; sprint FOV is subtle and configurable
- Max speed, acceleration, rotation speed are tunable at runtime
- All features are guarded by toggles; off-state matches current baseline

## Risks

- Over-tuning physics forces causing instability on slopes
- Camera nausea if smoothing/FOV are aggressive

## Notes

- Keep RB-based approach; avoid CharacterController to stay compatible with physics interactions

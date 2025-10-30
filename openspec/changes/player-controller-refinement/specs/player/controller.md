# Spec: Player Controller Refinement

## Movement
- RB-based movement with force application
- Max horizontal speed clamped; vertical speed unaffected by limiter
- Rotation blends toward input direction with tunable `rotationSpeed`

## Camera
- Follow offset with smoothing (LateUpdate)
- LookAt target at configurable height
- Optional sprint FOV effect with configurable target FOV and lerp speed

## Jump System
- Coyote time: `coyoteTimeDuration`
- Jump buffer: `jumpBufferDuration`
- On jump, reset Y velocity then apply `jumpForce` as velocity change

## Input
- Input System (Keyboard) with `inputSensitivity` scalar
- Sprint/jump keys are configurable

## Telemetry & Debug
- Expose `isGrounded`, `currentMaxSpeed` via serialized fields
- Draw ground check gizmos

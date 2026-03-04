# TASK_034 Manual Validation Checklist

## Purpose
- Close `TASK_034_UnityValidation_DualGridProfileMapping` with minimum manual effort.
- Reuse automated gates first, then run only focused scene checks.

## Preconditions
- Unity Editor: `6000.3.3f1`
- Task context: `docs/tasks/TASK_034_UnityValidation_DualGridProfileMapping.md`
- Related implementation: `TASK_033`, `TASK_036`

## Step 1: Automated Gates (must pass)
1. `.\scripts\check-compile.ps1`
2. `.\scripts\run-tests.ps1 -TestMode editmode -RequireNonZeroTests`
3. `.\scripts\run-tests.ps1 -TestMode playmode -RequireNonZeroTests`

Record:
- compile: pass/fail
- editmode: total/passed/failed
- playmode: total/passed/failed

## Step 2: Focused Manual Runtime Checks (DualGrid only)
Use a scene with `GridDebugVisualizer` and assigned `TerrainGenerationProfile`.

1. `UseProfileBounds = true`
- Expected: profile bounds affect sampling area in preview output.

2. `UseProfileBounds = false`
- Expected: legacy fallback behavior is preserved.

3. `UvAddressMode = Clamp` vs `Wrap`
- Expected: edge sampling changes according to mode.

4. `HeightQuantization = FloorToInt / RoundToInt / CeilToInt`
- Expected: preview height levels differ consistently by quantization mode.

Record for each case:
- configuration
- observed result
- pass/fail
- screenshot/log reference (if available)

## Completion Rule
- If all Step 1 and Step 2 items pass: set `TASK_034` to `DONE`.
- If any Step 2 item cannot be verified: set `TASK_034` to `BLOCKED` with exact blocker and reproduction.

## Evidence Template
```
Date:
Executor:
Unity Version:

Compile:
EditMode:
PlayMode:

Manual Checks:
1) UseProfileBounds=true: PASS/FAIL, note:
2) UseProfileBounds=false: PASS/FAIL, note:
3) Clamp/Wrap: PASS/FAIL, note:
4) Floor/Round/Ceil: PASS/FAIL, note:

Final Status: DONE / BLOCKED
Blocker (if any):
```

# Terrain/Object Generation Shortest Plan

## Goal
- Validate terrain and object generation health with minimum elapsed time.
- Prioritize automated checks, keep manual checks to a narrow final pass.

## Fast Path (recommended order)
1. Compile gate
- `.\scripts\check-compile.ps1`

2. EditMode gate
- `.\scripts\run-tests.ps1 -TestMode editmode -RequireNonZeroTests`

3. PlayMode gate
- `.\scripts\run-tests.ps1 -TestMode playmode -RequireNonZeroTests`

4. Manual smoke (only if gates pass)
- Run `TASK_034` focused checklist:
  - `docs/03_guides/TASK_034_MANUAL_VALIDATION_CHECKLIST.md`

## Why this is shortest
- Compile + EditMode + PlayMode already cover broad regression surface.
- Manual verification is limited to DualGrid profile mapping behavior only.
- Avoids broad exploratory manual testing unless a gate fails.

## Pass/Fail Decision
- All automated gates pass and focused manual checks pass:
  - proceed with feature lane (`TASK_PC-1` readiness review).
- Automated gates pass but focused manual check is incomplete:
  - keep `TASK_034` as `PARTIAL_DONE` or `BLOCKED` with explicit reason.
- Any automated gate fails:
  - stop feature progress and fix failing gate first.

## Current Baseline (2026-02-26)
- Compile: pass
- EditMode: pass (75/75)
- PlayMode: fail for strict gate (`-RequireNonZeroTests` detects total=0)
- Remaining manual scope: `TASK_034` focused runtime checks only.

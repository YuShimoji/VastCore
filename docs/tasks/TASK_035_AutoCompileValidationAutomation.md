# Task: TASK_035 Auto Compile Validation Automation

## Status
Status: READY

## Tier
Tier: 1

## Branch
Branch: feature/TASK_035-auto-compile-validation

## Created
Created: 2026-02-12

## Objective
Automate Unity compile validation via headless/batchmode script for local and CI use, reducing manual validation wait time.

## Context
- `TASK_034` is PARTIAL_DONE due to manual Unity validation dependency.
- Existing script `scripts/run-tests.ps1` already handles Unity batch test execution.
- Project needs a lightweight compile-only check entrypoint.

## Focus Area
- `scripts/` (new/updated PowerShell automation)
- Optional CI wiring in `.github/workflows/` (if scope allows)
- Documentation under `docs/03_guides/`

## Forbidden Area
- Runtime feature changes
- Broad CI refactor unrelated to compile validation

## Scope
1. Add compile-check script using Unity `-batchmode -nographics -quit -logFile`.
2. Define output location for logs/artifacts.
3. Document command usage and expected pass/fail criteria.
4. (Optional) Add CI hook for manual trigger path.

## DoD
- [ ] Compile-check script is executable from repo root.
- [ ] Script returns non-zero exit on compile failure.
- [ ] Output log path is deterministic and documented.
- [ ] Guide doc updated with usage instructions.
- [ ] Report created: `docs/04_reports/REPORT_TASK_035_AutoCompileValidationAutomation.md`.

## Deliverables
1. Script file(s)
2. Usage documentation
3. Validation report

## Stopping Conditions
- Script works in local environment with clear success/failure signals.
- Any environment blocker is documented with workaround.

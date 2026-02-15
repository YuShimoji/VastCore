# Spec: Documentation Cleanup (Phase 2)

## Requirements

### R1. Placeholder date normalization
The system documentation in active reports MUST NOT contain unresolved date placeholders.

#### Acceptance
- Replace `YYYY-XX-XX` style placeholders with `YYYY`.
- Replace `YYYY-MM-XX` style placeholders with `YYYY-MM`.

### R2. Conflict-marker free docs
Documentation files MUST be free of git conflict markers.

#### Acceptance
- `docs/04_reports/DEV_LOG.md` contains no `<<<<<<<`, `=======`, `>>>>>>>`.

### R3. Verifiable cleanup gate
The documented grep procedure MUST match the current placeholder policy.

#### Acceptance
- Validation snippets in `docs/04_reports/FUNCTION_TEST_STATUS.md` use normalized patterns and align with actual checks.

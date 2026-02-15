# Change Proposal: docs-cleanup-placeholder-phase2

## Why
The previous SSOT rewiring left follow-up cleanup items in report documents.
`docs/04_reports/DEV_LOG.md` still contains placeholder dates (`YYYY-XX-XX` style), and unresolved merge markers are present.
These issues keep the cleanup verification from being unambiguous.

## What
- Normalize placeholder dates in report/history documents to year or year-month granularity.
- Remove unresolved merge conflict markers from `docs/04_reports/DEV_LOG.md`.
- Update cleanup verification snippets in `docs/04_reports/FUNCTION_TEST_STATUS.md` to match normalized patterns.
- Re-run grep-based validation and record zero unresolved placeholders in active docs scope.

## Scope
- In scope:
  - `docs/04_reports/DEV_LOG.md`
  - `docs/04_reports/FUNCTION_TEST_STATUS.md`
- Out of scope:
  - Content-level rewriting of historical entries beyond placeholder/date normalization
  - Unity runtime/editor behavior changes

## Acceptance Criteria
- No `YYYY-XX-XX` or `YYYY-MM-XX` placeholder dates remain in the two target files.
- No merge conflict markers (`<<<<<<<`, `=======`, `>>>>>>>`) remain in `docs/04_reports/DEV_LOG.md`.
- Cleanup verification section reflects current grep patterns used for validation.

## Risks
- Historical context may become less specific after normalization.
- Aggressive replacement could touch example snippets unintentionally.

## Mitigation
- Restrict edits to explicit placeholder patterns and conflict marker region.
- Re-validate with `rg` after edits.

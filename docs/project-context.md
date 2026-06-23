# Project Context

Durable decision log and handoff history live here going forward.

Current state remains in `docs/runtime-state.md`. Operating rules remain in
`docs/REPO_LOCAL_RULES.md`. Historical handoffs and reports remain useful
evidence, but they are not current acceptance unless re-verified.

## Decision Log

- 2026-06-23: VC-RST-2 package restoration did not enter Unity or Package
  Manager validation because the required clean worktree path
  `C:\Users\PLANNER007\VastCore\VastCore-origin-main-compile` was absent in the
  current environment. The accessible `main` checkout was left as the handoff
  documentation surface only.
- 2026-06-15: Agent instruction authority moved to the modern front-door pattern:
  `AGENTS.md` -> `docs/REPO_LOCAL_RULES.md` -> `docs/runtime-state.md`.
  `CLAUDE.md` is project context, not operational SSOT.
- 2026-06-15: M0 architecture context was captured as evidence docs under
  `docs/architecture/`. These docs record current compile and dependency risks;
  they do not approve a broad refactor or claim Unity acceptance.

## Handoff Notes

- Future handoffs should reference the active artifact and bottleneck from
  `docs/runtime-state.md`.
- VC-RST-2 restart context is tracked in
  `docs/restart/VC_PACKAGE_MANAGER_RESTORATION_REPORT.md`. A next terminal
  should recreate or locate a clean `origin/main` worktree before touching
  `Packages/`, `ProjectSettings/`, generated folders, or Unity batchmode.
- Remote sync for this block should include the restart report and the current
  trust boundary that Unity Package Manager, compile, and runtime behavior still
  need re-check.

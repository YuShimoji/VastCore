# CLAUDE.md - Claude Code entry pointer

**Operating rules:** [`docs/REPO_LOCAL_RULES.md`](../docs/REPO_LOCAL_RULES.md)

This file is intentionally thin. `AGENTS.md` is also an entry pointer. Do not add
detailed procedures, work history, report templates, roadmap status, or option
menus here.

Minimum rules:

- Usually read/write only this repo. If the user explicitly names cross-project
  scope, touch only that named scope.
- Normal restart is `AGENTS.md` -> `docs/REPO_LOCAL_RULES.md` ->
  `docs/runtime-state.md`.
- Read `docs/ai/*.md` only when a specific gate or status semantic is needed.
- For Unity code changes, check the owning assembly and
  `docs/02_design/ASSEMBLY_ARCHITECTURE.md` when dependencies or asmdefs are
  involved.

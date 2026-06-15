# AGENTS.md - VastCore entry pointer

This file is only a repo entry pointer. Do not grow it into procedures, status,
roadmaps, closeout templates, option menus, or history.

## Read Order

Normal restart / continue reads only:

1. `docs/REPO_LOCAL_RULES.md`
2. `docs/runtime-state.md`

Use `docs/NAV.md` when you are unsure where a specific spec, guide, task, or
report lives. Read additional docs only when the current task needs them.

## Authority

- User / developer instructions override this file.
- Project-local docs override global Codex fallback rules and global prompt
  helpers.
- `docs/REPO_LOCAL_RULES.md` owns daily hard rules, restart budget, ask hygiene,
  reporting expectations, git/test follow-through, and residual-work reporting.
- `docs/runtime-state.md` owns current position, active artifact, bottleneck,
  and next action.
- `docs/INVARIANTS.md` owns non-negotiable product and engineering boundaries.
- `docs/ai/*.md` owns vendor-neutral gates and semantics.

If a referenced file is missing, treat the reference as stale, not as a blocker.
Use the nearest repo-local instruction file and then the docs it names.

## Anti-Growth Rule

Put changes in the narrow owner instead:

- Rules / ask / closeout behavior: `docs/REPO_LOCAL_RULES.md` or
  `docs/INTERACTION_NOTES.md`
- Current state / next action: `docs/runtime-state.md`
- Durable decisions / handoff history: `docs/project-context.md` when present
- Document map: `docs/NAV.md`
- Feature status: `docs/spec-index.json`, `docs/tasks/`, or the owning spec

Global files under `C:\Users\thank\.codex\` are fallback helpers, not VastCore
authority.

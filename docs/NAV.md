# NAV.md - VastCore document map

Use this file when you are unsure where to look. It is an index, not a source of
truth for rules, status, or implementation details.

## Normal Restart

1. `AGENTS.md` - repo entry pointer
2. `docs/REPO_LOCAL_RULES.md` - short operating rules
3. `docs/runtime-state.md` - current position and next action

Stop there unless the task needs more evidence.

## Core Owners

- Product purpose and world-level priority: `docs/SSOT_WORLD.md`
- Current state and active bottleneck: `docs/runtime-state.md`
- Assembly boundaries: `docs/02_design/ASSEMBLY_ARCHITECTURE.md`
- Code standards: `docs/03_guides/UNITY_CODE_STANDARDS.md`
- Compile diagnosis: `docs/03_guides/COMPILATION_GUARD_PROTOCOL.md`
- Architecture overview: `docs/ARCHITECTURE.md`
- Spec registry and viewer data: `docs/spec-index.json`
- Task tickets: `docs/tasks/`
- Reports and historical evidence: `docs/04_reports/`

## AI / Agent Rule Owners

- Thin entry pointer: `AGENTS.md`
- Repo-local daily rules: `docs/REPO_LOCAL_RULES.md`
- Vendor-neutral gates: `docs/ai/*.md`
- Non-negotiables: `docs/INVARIANTS.md`
- User request ledger: `docs/USER_REQUEST_LEDGER.md`
- Operator workflow: `docs/OPERATOR_WORKFLOW.md`
- Interaction/reporting notes: `docs/INTERACTION_NOTES.md`
- Durable decision history: `docs/project-context.md` when present
- Claude Code pointer: `.claude/CLAUDE.md`
- Codex local config: `.codex/config.toml`

## Staleness Rule

If this index or another doc points to a missing file, treat that pointer as
stale. Do not block the task just because an old reference exists.

# REPO_LOCAL_RULES.md - VastCore repo-local operating rules

This is the short front-door for normal VastCore work. Keep it short enough to
read every restart. Do not store status snapshots, task history, or lane-specific
procedures here.

Detailed owners:

- Product / engineering boundaries: `docs/INVARIANTS.md`
- Current state / next action: `docs/runtime-state.md`
- Interaction failures and reporting style: `docs/INTERACTION_NOTES.md`
- Operator workflow and manual verification boundaries: `docs/OPERATOR_WORKFLOW.md`
- Durable requests and backlog deltas: `docs/USER_REQUEST_LEDGER.md`
- Document map: `docs/NAV.md`

## Restart Read Budget

Normal restart / continue reads only:

1. `AGENTS.md`
2. `docs/REPO_LOCAL_RULES.md`
3. `docs/runtime-state.md`

Read more only when the current task lacks evidence. Limit extra reading to the
relevant section, artifact, or owner doc. Full-corpus reading is a diagnostic
exception, not progress by itself.

## Core Rules

- Repo-local authority comes first. Global Codex files and prompt helpers are
  fallback only.
- Stay inside this repo unless the user explicitly names cross-project scope. If
  cross-project scope is explicit, touch only that scope.
- `AGENTS.md`, `.claude/CLAUDE.md`, and `.codex/config.toml` are entry/config
  adapters. Do not put detailed procedures, status, roadmap, report templates,
  or history in them.
- Use Japanese for user-facing project reports unless the user requests another
  language.
- Do not read `docs/archive/` unless explicitly asked.
- Do not choose next work by pendulum logic. Choose the current bottleneck.
- During user-declared REFRESH / REANCHOR / SCAN / AUDIT phases, default to
  read-only behavior unless the same user block explicitly asks for mutation.
- Do not present documentation cleanup as product progress unless it makes the
  active artifact path easier, safer, or more verifiable.

## Unity Engineering Rules

- Before code changes, identify the owning assembly and check
  `docs/02_design/ASSEMBLY_ARCHITECTURE.md` when dependencies, namespaces, or
  asmdefs are involved.
- Keep assembly dependencies one-way. Do not add lower-to-upper asmdef references
  or duplicate fully qualified type names.
- Preserve Unity `.meta` file integrity. Moves and deletes must account for the
  matching `.meta` file.
- `ProjectSettings/` and `Packages/` changes require explicit task relevance and
  must be reported clearly.
- Follow C# 9.0 constraints used by this Unity project. Avoid parameterless
  struct constructors.
- Runtime logging should use the project logger path, not new raw `Debug.Log`
  calls, unless an existing local pattern requires otherwise.

## Git And Tests

- Git follow-through is assistant-owned after a validated slice unless the user
  says not to commit or push. Stop before destructive operations, pushed-history
  rewrites, ambiguous large deletions, cross-repo publication, or explicit user
  prohibition.
- For code changes, run the narrow relevant check first. Prefer the project
  scripts (`scripts/check-compile.ps1`, `scripts/run-tests.ps1`) when they match
  the change.
- Do not run Unity-heavy checks for docs-only edits unless the docs changed an
  executable contract or verification command.
- If Unity Editor verification is required but not run, state that explicitly.

## Reporting Rule

Reports should make the work usable without forcing the user to open files.
State what changed, why it matters, what evidence supports it, what remains
uncertain, and what the next concrete move is.

When listing residual work, include each item's purpose, effect, requirements,
current state, owner, and next move. Avoid bare path lists, priority codes, or
test names as the whole explanation.

Keep output structure proportional. A small docs-only change can be a short
paragraph. A slice closeout should also preserve touched/untouched boundaries,
evidence, residual risk, recommended default, and next owner.

## Ask Hygiene

- Ask only decisions that change the bottleneck.
- Do not ask broad questions when repo evidence can decide the next move.
- Offer options only when they solve different bottlenecks.
- Do not mix manual verification with next-direction choice in one ask.
- When corrected, verify against repo evidence and make the smallest safe fix in
  the same block.

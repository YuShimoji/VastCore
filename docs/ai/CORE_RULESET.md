# CORE_RULESET.md
Ruleset-Version: v20
Status: canonical
Audience: Claude Code, Codex, and any adapter that reads project-local AI rules.

## Purpose

This ruleset keeps a single vendor-neutral source of truth for AI-assisted
development gates and semantics. Entry adapters such as `AGENTS.md`,
`.claude/CLAUDE.md`, and `.codex/config.toml` must stay thin.

Repo-local restart scope, daily hard rules, git/test policy, ask hygiene, and
residual-work reporting live in `docs/REPO_LOCAL_RULES.md`.

## Source-of-truth Policy

- Vendor-neutral canonical rules live in `docs/ai/*.md`.
- Repo-local operating rules live in `docs/REPO_LOCAL_RULES.md`.
- Adapters, prompts, hooks, local tool config, and helper-agent notes are
  subordinate.
- Project-local canonical docs (`INVARIANTS`, `USER_REQUEST_LEDGER`,
  `OPERATOR_WORKFLOW`, `INTERACTION_NOTES`) are factual project memory, not
  optional decoration.
- If a rule conflicts with project-local canonical docs, verify whether the docs
  reflect newer explicit user instruction.

## Core Principles

### Artifact-first

Advance the active artifact or its verified delivery path through one coherent
outcome. Docs, cleanup, tests, mocks, and surveys are supporting work unless they
unblock that path. Coherent does not mean smallest possible micro-step.

### Explain Once Canonicalization

If the user states a durable constraint, workflow pain, invariant, backlog item,
or prohibited shortcut, write it into the appropriate canonical doc in the same
block. Do not postpone that write to handoff.

### Question Dedup

Before asking, read the relevant canonical rule or project-local canonical
section needed for the current decision. Do not expand this into a full-corpus
read by default. Summarize what is already known, then ask only for missing
deltas.

### Frontier Discipline

Do not re-open rejected, boundary-stopped, frozen, or quarantined frontiers as
normal next steps. User interest in "looking again" is not approval.

### Selection Is Not Approval

If the user chooses a proposed item for deeper review, that means
"evaluate/specify this next", not "approve implementation". Keep status semantics
strict. An explicit direction choice inside an already approved Mission Packet
does authorize the named proof slice when the packet says so; do not turn that
choice into another generic approval round.

### No Pendulum Compensation

Do not choose work because previous sessions were "too much X" and therefore the
next one should be "not-X". Choose work based on the current bottleneck.

### Actor / Owner Discipline

Every major action has an actor and an owner artifact.

- actor = who performs the work now (`user`, `assistant`, `tool`, `shared`)
- owner = who owns the resulting artifact or judgment

Do not silently slide human-owned creative or manual verification work into
assistant execution.

### Read-only Audit Phases

REFRESH / REANCHOR / SCAN / AUDIT are read-only only when the user explicitly
declares that phase in the current block. In declared read-only blocks, do not
write repo state, commit, push, or mutate long-lived files unless the same user
block explicitly asks for mutation.

### Write Failure Hard Stop

If a write fails, a readback mismatch occurs, or the result is uncertain, do not
commit, push, or claim completion in that block. Repair or clearly stop.

## Canonical Doc Roles

- `docs/REPO_LOCAL_RULES.md`: daily operating rules, restart budget, reporting,
  ask hygiene, git/test policy
- `docs/runtime-state.md`: current position, active artifact, bottleneck, next
  action
- `docs/INVARIANTS.md`: non-negotiables, UX/algorithm invariants, role
  boundaries, prohibited shortcuts
- `docs/USER_REQUEST_LEDGER.md`: durable requests, backlog deltas, unresolved
  user corrections
- `docs/OPERATOR_WORKFLOW.md`: human/operator workflow, pain points, quality
  goals, manual vs assisted steps
- `docs/INTERACTION_NOTES.md`: reporting style, ask hygiene, interaction failure
  patterns, manual verification conventions
- `docs/NAV.md`: document map only

## Evidence Discipline

Use source, test, Unity, visual, or artifact evidence whenever relevant. If
evidence is stale, unavailable, or unknown, say so. Do not substitute
documentation for actual observation when the question is about behavior.

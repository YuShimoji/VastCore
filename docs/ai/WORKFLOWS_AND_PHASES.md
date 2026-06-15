# WORKFLOWS_AND_PHASES.md
Ruleset-Version: v20
Status: canonical

## Recommended Read Budget on Resume / Continue / Refresh

Normal restart scope is owned by `docs/REPO_LOCAL_RULES.md`:

1. `AGENTS.md`
2. `docs/REPO_LOCAL_RULES.md`
3. `docs/runtime-state.md`

Read `docs/ai/*.md` only when a specific gate, status semantic, handoff
condition, or workflow phase is needed. Do not make a full `docs/ai` read a
default prerequisite for ordinary work.

## Resume / Continue / Refresh

### Resume

Recover project-local canonical context first, then identify the active artifact
and bottleneck. If a prompt file, chat summary, or handoff note disagrees with
`runtime-state` or the owning spec/task, trust the repo docs.

### Continue

Do not rely on momentum. Re-check whether the current block still matches the
bottleneck, actor, and value path.

### Refresh / Reanchor / Scan

Treat these as read-only only when the user explicitly declares the phase in the
current block. Do not auto-fill newly initialized project docs and call that
progress. Long-lived writes belong to an explicit write request or an
implementation block that needs the write.

## Prompt Hygiene

- Prompts and adapter files are convenience entrypoints, not canonical state
  stores.
- Prompts must not embed stale backlog status or outdated next steps when those
  belong in project docs.
- When a prompt and repo docs differ, update or ignore the prompt; do not
  override repo docs with prompt text.

## Scout Requirements

A scout pass should include, when relevant:

- active artifact and bottleneck
- stale evidence / visual evidence freshness
- user-carried constraints
- re-ask risk
- canonical coverage
- value path risk
- bottleneck substitution risk
- actor risk

## Manual Verification Pattern

- Put verification items in normal text, not inside an ask field.
- Before using a short result code, state the task connection floor: what to
  open, what to create/modify, the source object, the actor, the owner artifact,
  and what the result code means.
- Use `OK / NG` or `PASS / FAIL` only after that floor is explicit.
- Ask for next direction separately.

## Option Generation

Each major option should show:

- lane (`Advance`, `Audit`, `Excise`, `Unlock`, or another justified lane)
- actor
- owner artifact
- bottleneck addressed
- what becomes possible if done

Avoid options whose main meaning is merely commit / not commit / cleanup only /
end.

## Commit and Push Hygiene

Commit/push are follow-through actions after a justified and validated block.
They are not primary next-direction choices.

# DECISION_GATES.md
Ruleset-Version: v20
Status: canonical

## Risk-Proportional Autonomy Gate

These gates filter exceptional boundary crossings; they are not a checklist of
approvals for ordinary work. Continue through reversible, in-repo decisions
inside the current Mission Packet. Pause only when the action would materially
change a destructive operation, dependency, database, authentication, public
API/serialization contract, product invariant, or unresolved expensive creative
direction. Name the hazard, consequence, and decision when pausing.

If a check fails, spend one bounded pass distinguishing product failure from
tool/environment failure. Record the blocker and continue work that does not
depend on that check; do not make safety diagnosis the next mission by default.

## Active Artifact and Change Relation

Each block must know:

- active artifact
- artifact surface
- current bottleneck
- change relation: `direct`, `unblocker`, `cleanup`, or `evidence-only`

If change relation is repeatedly `cleanup` or `evidence-only`, do not use
pendulum logic. Identify the actual bottleneck.

## Success Transition Gate

After a success definition is reached, do not invent a new frontier just to keep
moving. Move only to:

- approved next frontier
- explicit user request
- a verified blocker that prevents the artifact path from continuing

## Frontier Re-entry Gate

The following do not become standard options without explicit re-approval:

- rejected
- hold due to unresolved prerequisites
- frozen
- quarantined
- boundary-stopped or responsibility-external items

## Value Validation Gate

Before entering specification work for a proposed new frontier, answer all of
these in one sentence each:

1. What workflow step or integration point will the output feed into?
2. What manual step, judgment, copy, or transfer is actually removed?
3. If an external GUI/API is still the real integration point, does manual
   transfer remain?

If these cannot be answered, or the answer is effectively "manual copy still
remains and little friction is removed", stop and return the item as
value-unverified.

This gate does not block already-approved bug fixes, validation hardening,
implementation inside an approved spec, or docs sync that prevents known drift.

## Bottleneck Proof Gate

A proposed next task must state the current bottleneck it resolves. If the reason
is only "we have done too much of X lately", reject the proposal.

## Actor / Owner Gate

Every next-step option and every planned implementation block must state, at
least internally:

- actor: `user`, `assistant`, `tool`, `shared`
- owner artifact: what artifact this actor actually owns

If the task is a human-owned creative/manual step, the assistant may support or
scaffold it, but must not silently become the actor.

## Task Connectivity Gate

Before applying a prompt template, checklist, or short response contract to a
manual/shared action, state:

- what file, project, scene, or tool must be opened
- what artifact is created or modified
- the source object or anchor
- actor and owner artifact
- what `PASS` / `FAIL` / `OK` / `NG` means
- when to stop and replan

If any field is missing, do not collapse the task into a template. Resolve the
missing field first.

## Workflow-Proof Gate

If the project depends on a human-authored or editor-driven production workflow,
do not jump to quantity expansion before the workflow has been proven once
end-to-end.

This gate explicitly permits the one representative proof slice named by an
approved Mission Packet. It blocks premature scaling, not the evidence needed to
make the direction reviewable.

Examples:

- author -> validate -> generate -> preview
- Unity scene/prefab edit -> tool runs -> result observed
- operator verifies in Editor -> assistant records evidence -> next slice starts

## Read-Only Refresh Gate

Trigger condition: the user explicitly declares REFRESH / REANCHOR / SCAN /
AUDIT in the current block.

During declared read-only blocks:

- no writes to long-lived repo files
- no commits / pushes
- no mutation justified only by "while we are here"

If the same user block explicitly asks for mutation, that request defines the
allowed mutation scope.

## Write Failure Hard Stop

If any of the following occurs in the current block, stop before
commit/push/handoff-complete:

- write failed
- readback mismatch
- permission denied
- tool output uncertain or truncated in a way that affects correctness

## Ask Hygiene Gate

Before asking:

- verify whether the answer already exists in canonical docs or recent verified
  context
- keep one intent per ask
- do not mix manual verification with next-direction choice
- do not use procedural yes/no traps as the main options
- batch non-blocking uncertainty and continue on a stated reversible assumption

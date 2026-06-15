# STATUS_AND_HANDOFF.md
Ruleset-Version: v20
Status: canonical

## Feature Status Semantics

Keep priority separate from status.

### Priority

Priority answers: "How worth looking at is this item compared with others?"
Examples: high / medium / low, or a ranked list.

### Status

Status answers: "What lifecycle state is this item in now?"

- `proposed`: value is still being validated or the spec is incomplete
- `approved`: specification and scope are defined enough for implementation to
  start, and the user has approved that move
- `in_progress`: implementation or validation is actively underway
- `done`: accepted evidence exists for the stated scope
- `hold`: not rejected, but not the current move due to prerequisites, weak
  value path, timing, or other blockers
- `rejected`: should not be pursued within the current product/workflow scope
- `frozen`: deliberately parked; do not treat as the normal next route
- `quarantined`: potentially contaminated or unauthorized batch-derived item; do
  not treat as a normal candidate until re-reviewed

Selection of a `proposed` item for deeper review does not upgrade it to
`approved`.

## Registry Discipline

For each feature candidate, keep at least:

- short description
- priority
- status
- rationale
- integration point / value path note
- actor / owner note when relevant

`approved` requires:

- clear input/output or scope boundary
- no unresolved boundary violation
- value path is stated
- user approval for implementation is explicit

If an unauthorized item appears in a proposal batch, quarantine the whole batch by
default until individually re-reviewed.

## Handoff Minimum

A robust handoff should preserve:

- shared focus
- non-negotiables
- current trust assessment
- active artifact and bottleneck
- recovered canonical context
- feature/backlog status with strict semantics
- safe next-thread plan
- what not to do next
- new durable constraints created in the current thread

## Residual Work Contract

When residual work is reported, each item must state:

- purpose
- effect
- requirements
- current state
- owner
- next move

Bare file paths, test names, or priority labels are evidence, not explanation.

## Closeout Chain Minimum

Final responses should make the next move executable. Preserve the logical chain
in normal language:

- what is complete
- what was deliberately not changed
- what changed for the workflow or decision space
- what evidence supports it
- what uncertainty remains
- who moves next
- what happens after any user-owned return

Do not force fixed section names unless the user asks for that structure.

## No Progress Laundering

Do not claim progress merely because:

- a doc was created during refresh
- a framework-compliant report was produced
- a list of changed files was shown
- a low-friction helper feature was specified

Report what became easier, safer, or more real for the actual artifact path.

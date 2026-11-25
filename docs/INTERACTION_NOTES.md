# Interaction Notes

Project-local notes for reporting, asks, manual verification, and user-facing
style.

## Reporting Style

- Use Japanese unless the user requests otherwise.
- Keep ordinary updates short and concrete.
- Explain what changed and why it matters before listing file paths.
- Do not use fixed closeout templates unless the user asks for a fixed format.
- When reporting residual work, include purpose, effect, requirements, current
  state, owner, and next move.
- Keep Git sync, mechanical validation, and human/visual acceptance as separate
  claims. A clean branch or written checklist is not Unity acceptance.

## Progress Rhythm

- Report a meaningful change in direction, a reviewable intermediate result, or
  a long-running tool wait; do not narrate every command.
- Close one coherent outcome with what changed, why it changes the workflow or
  decision, evidence, uncertainty, and two to four genuinely different next
  entrances when a choice is useful.
- Mark one recommended default. Explain what becomes possible after each option,
  rather than asking the user to choose an implementation procedure.

## Manual Verification Asks

- Put verification instructions in normal text.
- Ask only one decision at a time.
- Use `OK / NG` or `PASS / FAIL` only after the target, actor, owner artifact,
  and success meaning are explicit.
- Ask for next direction separately from manual verification.
- Group related Editor checks into one batch. Do not return with a new manual ask
  after every small fix.

## Avoid

- Broad "please explain the context again" questions when repo docs can answer.
- Options whose only axis is commit / do not commit.
- Large markdown tables for short asks.
- Reporting historical handoff text as if it were current evidence.
- Safety language without a named hazard, consequence, and decision.
- Micro-prompts for work that remains inside an approved outcome.
- Broad visual implementation before direction intent is established.
- An acceptance or direction form after every increment (2026-11-24 correction).
  Send a question only when the answer selects between methods that repo
  evidence cannot decide. Do not turn the "what to add next" field of an answer
  into the next task without checking the SP-021 stage table.
- Preference questions about defects the developer can count and fix.
- Asking the user to accept a screen that fell out of implementation (2026-11-25
  correction). For look work, research references and write the expected look
  (what must read at far, middle and near range, with reference images) before
  building; then compare the screen against that target.

Durable user preferences belong in `docs/USER_REQUEST_LEDGER.md`; this file owns
only interaction behavior.

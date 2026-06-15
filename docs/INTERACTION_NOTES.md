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

## Manual Verification Asks

- Put verification instructions in normal text.
- Ask only one decision at a time.
- Use `OK / NG` or `PASS / FAIL` only after the target, actor, owner artifact,
  and success meaning are explicit.
- Ask for next direction separately from manual verification.

## Avoid

- Broad "please explain the context again" questions when repo docs can answer.
- Options whose only axis is commit / do not commit.
- Large markdown tables for short asks.
- Reporting historical handoff text as if it were current evidence.

## Current User Preference Notes

- The user asked for a strong modernization of VastCore project-local Agent
  instructions, aligned with newer projects such as NLMYTGen.
- `AGENTS.md` must remain thin and must not become procedures, status, roadmap,
  closeout template, or history.

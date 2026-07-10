# User Request Ledger

ユーザーの継続要望・差分要求・backlog delta を保持する台帳。

## Currently Active Requests

- Project-local Agent instruction files should start from a fresh, modern state.
- Align VastCore's AI instruction structure with newer projects such as
  NLMYTGen: thin adapters, repo-local rules front-door, runtime state as current
  truth, and no operational history inside `AGENTS.md`.
- Treat missing referenced docs as stale references, not blockers.
- For residual work, report purpose, effect, requirements, current state, owner,
  and next move.
- 2026-07-10: Supervisor prompts and development execution must optimize for one
  coherent outcome, not a chain of tiny instructions. Safe, reversible, in-scope
  work should continue through related fixes and verification without repeated
  approval.
- 2026-07-10: Safety gates must not become the main work. Stop only for a
  concrete destructive/contract/authority conflict or an unresolved expensive
  creative direction.
- 2026-07-10: Progress and next work must remain understandable outside chat.
  Keep repo state current and publish a generated external Project Pulse rather
  than relying on manually maintained Wiki prose.
- 2026-07-10: AI reviews must contribute creative alternatives across layout,
  localization, content adjacency, color, typography, and motion when relevant.
  Expensive production starts only after the user can compare directions.
- 2026-07-10: Avoid the broad-delivery-to-micro-tweak loop. Use an early
  direction checkpoint, a small proof slice, batched review, and return to the
  design principle when local tweaks do not converge after two rounds.

## Standing Reporting Preferences

- Use Japanese for normal project reports.
- Keep responses concise and repo-grounded.
- Report observable files, commands, checks, and unresolved items when the user
  asks for strict scope.
- Do not ask the user to re-explain context that the repo already stores.
- When the user asks to keep context in the project and make another terminal
  resume immediately, update repo-local state/handoff docs and push that state
  instead of leaving the handoff only in chat.
- Prefer a small number of enforced owners and automation over adding more
  overlapping instruction resources.

## Backlog Delta

- If a future task needs richer feature status, add or update the narrow owning
  registry/spec instead of expanding `AGENTS.md`.
- If local tool startup friction recurs, keep local machine settings out of
  tracked repo files unless the setting is intentionally shared.

## Operating Rule

Record durable user corrections here when they affect future work. Do not store
one-off status, temporary plans, or completed command logs.

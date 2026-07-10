# Project Context

Durable decision log and handoff history live here going forward.

Current state remains in `docs/runtime-state.md`. Operating rules remain in
`docs/REPO_LOCAL_RULES.md`. Historical handoffs and reports remain useful
evidence, but they are not current acceptance unless re-verified.

## Decision Log

- 2026-07-11: The current `thank` workstation is accepted as a valid local
  development environment for source work: Unity 6000.3.6f1 compile passed,
  EditMode passed 596/596, and PlayMode passed 9/9 after repairing test asset,
  discovery, logger, and UI creation defects. The earlier UPM `path undefined`
  result remains provenance-specific historical evidence, not the current local
  bottleneck.
- 2026-07-11: Development-readiness repairs continue on
  `codex/vc-development-readiness-20260711` from `f145df6`. This preserves draft
  PR #49 as a workflow-only review. The next product gate is the batched Designer
  Cockpit Editor acceptance, followed by one end-to-end structure-to-DualGrid
  proof before broad Phase D expansion.
- 2026-07-10: Supervisor-to-developer work is outcome-based. A Mission Packet
  authorizes one coherent implementation/verification/state-sync batch; only
  concrete destructive or contract boundaries and unresolved expensive creative
  direction require a mid-block stop.
- 2026-07-10: `docs/runtime-state.md` remains the narrative current-state SSOT.
  A pinned GitHub Project Pulse issue is a generated external projection, while
  Wiki/Pages remain candidates for durable reference material rather than live
  status.
- 2026-06-29: VC-RST-2 remote resume handoff was refreshed after the T+2n
  network/proxy/env audit. Network/proxy/env is now weakened as the direct UPM
  root; the next discriminator is user-approved Unity Editor / embedded UPM
  repair or reinstall, followed by a short-path empty-manifest control retest
  before VastCore is reopened.
- 2026-06-29: A local docs-only refresh of
  `docs/04_reports/LEGACY_UI_MIGRATION_REPORT.md` was preserved for remote sync.
  No `Assets`, `Packages`, or `ProjectSettings` changes are part of this
  handoff.
- 2026-06-23: VC-RST-2 package restoration did not enter Unity or Package
  Manager validation because the required clean worktree path
  `C:\Users\PLANNER007\VastCore\VastCore-origin-main-compile` was absent in the
  current environment. The accessible `main` checkout was left as the handoff
  documentation surface only.
- 2026-06-15: Agent instruction authority moved to the modern front-door pattern:
  `AGENTS.md` -> `docs/REPO_LOCAL_RULES.md` -> `docs/runtime-state.md`.
  `CLAUDE.md` is project context, not operational SSOT.
- 2026-06-15: M0 architecture context was captured as evidence docs under
  `docs/architecture/`. These docs record current compile and dependency risks;
  they do not approve a broad refactor or claim Unity acceptance.

## Historical Handoff Index

- Current state, active artifact, bottleneck, and next action always come from
  `docs/runtime-state.md`; historical entries below are evidence only.
- Designer Cockpit UX review handoff:
  `docs/restart/VC_DESIGNER_COCKPIT_UX_REMOTE_REVIEW_HANDOFF.md`.
- VC-RST-2 package restoration history:
  `docs/restart/VC_PACKAGE_MANAGER_RESTORATION_REPORT.md`.
- Earlier remote-resume packets under `docs/restart/` must not override current
  evidence without re-verification.

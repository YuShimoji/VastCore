# VastCore Runtime State

Last Updated: 2026-06-15

## Current Position

| Field | Value |
|---|---|
| Project | VastCore Terrain Engine |
| Branch | main |
| Active artifact | Project-local AI / Agent instruction surface and M0 architecture readback |
| Current bottleneck | Instruction modernization is ready for remote sync; the next product bottleneck is compile/asset-integrity restoration, not broader refactor work |
| Change relation | cleanup / unblocker / evidence-only |

## Current Block

Purpose: start future Agent sessions from a clean, modern instruction surface and
preserve the current M0 architecture context in tracked docs.

In scope:

- Thin `AGENTS.md`
- Repo-local rules front-door
- Current runtime state pointer
- Vendor-neutral `docs/ai` gates
- Tool-specific entry/config files
- Project-local canonical docs used by agents
- M0 dependency, module-classification, external-asset, target-architecture, and
  risk-register readbacks

Out of scope:

- Unity gameplay/code changes
- Historical task reports
- Broad rewrite of old handover artifacts
- Current Unity compile or Editor acceptance claims
- `ProjectSettings/`, `Packages/`, or Unity gameplay/source changes

## Current Trust Assessment

- Trusted: source tree location, current git HEAD readback, upstream parity before
  commit (`HEAD...origin/main` was `0 0`), tracked instruction file inventory,
  and M0 architecture docs from this block.
- Needs re-check: Unity compile state and runtime behavior; this block did not
  fix or accept the compile failure recorded in
  `docs/architecture/current-dependency-map.md`.
- Historical only: March 2026 handoff/progress summaries. Treat them as context,
  not current acceptance evidence.

## Next Action

After this remote sync, the next assistant-owned product move is a narrow
compile-restoration proposal/fix for the `StructureTagAdapter.cs` CS0234 blocker
and invalid `.meta` GUIDs. Do not start a broad namespace/asmdef refactor before
compile and asset integrity are restored or explicitly re-scoped.

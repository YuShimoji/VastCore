# Proposal: restart-architecture-boundaries

## Why

VastCore currently mixes terrain generation, external asset integration, editor authoring tools, player movement, Trail visuals, and game composition inside one Unity project. The repo also has a dirty local working tree, a fetched-but-unmerged `origin/main`, and current evidence that compile / asset integrity needs restoration before feature work.

This proposal defines the next architecture boundary slice without moving runtime/editor files yet.

## What Changes

- Document Terrain Generation Engine as the primary product surface.
- Document Simulator Harness as the secondary validation surface.
- Keep StructureGenerator and CSG as editor/integration tooling.
- Keep ProBuilder, Deform, Splines, and possible road/mining assets behind optional adapter gates.
- Define dependency rules that prevent Terrain Engine from depending on Player, Camera, UI, Game, or TrailRenderer.
- Define PoC gates for dual contouring, voxel/SDF mining, road assets, and CSG providers.

No runtime code, scenes, prefabs, packages, or ProjectSettings are changed by this proposal.

## Impacted Assemblies

This proposal is documentation-only. Future implementation slices may affect:

| Assembly | Expected role |
|---|---|
| `Vastcore.Terrain` | immediate Terrain Engine spine and extensions until split |
| `Vastcore.WorldGen` | optional density-field / volumetric extension |
| `Vastcore.Generation` | legacy/current generation path; candidate for ownership cleanup |
| `Vastcore.Player` | Simulator Harness |
| `Vastcore.Game` | harness composition layer |
| `Vastcore.Editor.StructureGenerator` | editor authoring and CSG provider surface |
| `Vastcore.Tests.EditMode` / `Vastcore.Tests.PlayMode` | boundary and PoC validation |

## Dependencies

No new package dependencies.

## Risks

- Current local branch is behind `origin/main`; sync should be handled in a separate safe slice.
- Unity compile state is not accepted until `scripts/check-compile.ps1` or equivalent Unity Editor verification passes.
- Broad asmdef moves before compile restoration could hide the actual blocker.

## Acceptance

- Boundary rules are reviewed against `docs/restart/VC_RESTART_ARCHITECTURE_AUDIT.md`.
- No runtime/editor implementation is changed in this proposal slice.
- Next implementation slice names the owning assembly, added using statements, and validation command before editing.

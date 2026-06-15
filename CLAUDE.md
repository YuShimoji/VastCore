# CLAUDE.md - VastCore project context

This file is project context, not the operating-rules source of truth. Daily AI
rules live in `docs/REPO_LOCAL_RULES.md`; entry pointers are `AGENTS.md` and
`.claude/CLAUDE.md`.

## Project Summary

VastCore is a Unity terrain and structure-generation project focused on broad
landscapes with distinctive procedural artificial structures.

- Engine: Unity 6000.3.x / URP
- Language constraints: C# / .NET Standard 2.1 / C# 9.0 limitations
- Main architecture: DualGrid + HeightMap + designer Prefab Stamp placement
- Normal source root: `Assets/Scripts/`
- Normal tests root: `Assets/Tests/EditMode/`

Current position and next action are owned by `docs/runtime-state.md`. Do not
infer current acceptance or compile status from historical notes in this file.

## Key Paths

- Assembly rules: `docs/02_design/ASSEMBLY_ARCHITECTURE.md`
- Unity code standards: `docs/03_guides/UNITY_CODE_STANDARDS.md`
- Compile diagnosis: `docs/03_guides/COMPILATION_GUARD_PROTOCOL.md`
- Architecture overview: `docs/ARCHITECTURE.md`
- World-level product purpose: `docs/SSOT_WORLD.md`
- Spec registry: `docs/spec-index.json`
- Task index: `docs/tasks/TASK_INDEX.md`
- Document map: `docs/NAV.md`

## Architecture Anchors

- DualGrid is the active grid/placement foundation.
- HeightMap supplies terrain height data and sampling paths.
- Prefab Stamp placement is the designer-facing structure placement route.
- Structure generation should stay compatible with assembly boundaries and
  explicit data assets.

Frozen or non-primary directions remain frozen unless the user reopens them:

- 3D Voxel / Marching Cubes terrain as the primary path
- Marching Squares mesh generation as the primary path
- Broad test expansion as a substitute for user-visible artifact progress

## Engineering Constraints

- Do not add lower-to-upper asmdef dependencies.
- Do not introduce duplicate type names across assemblies.
- Preserve Unity `.meta` files during moves and deletes.
- Treat `ProjectSettings/` and `Packages/` as high-blast-radius surfaces.
- Use project logging conventions instead of adding new raw runtime `Debug.Log`
  calls.

## Decision Anchors

- DualGrid + HeightMap + Prefab Stamp is the accepted core terrain/structure
  direction.
- Prefab stamps started as single-cell placement and later expanded toward
  footprint support.
- Terrain height coupling should use `IHeightSampler` style abstraction rather
  than direct terrain dependence from lower layers.
- Authoring-first workflow is preferred over runtime-only procedural generation
  when designer control is the bottleneck.
- Parametric variation is the first variation route; WFC/CSG-style expansion is
  a later frontier, not the default next step.

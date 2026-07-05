# Project Cockpit

## Current Cockpit Surface

The first designer-facing cockpit is a local Unity Editor window:

- Menu: `Tools/VastCore/Designer Cockpit`
- Code: `Assets/Editor/VastCore/DesignerCockpit/`
- Session type: `VastCoreDesignerSession`
- Default session asset folder: `Assets/Data/VastCore/DesignerSessions`
- Sample session asset: `Assets/Data/VastCore/DesignerSessions/Designer_Session.asset`

The cockpit gives a designer one place to inspect selected object count, session
name, seed, notes, last saved time, feature status tiles, and a minimal local
random transform recipe.

## Current Capability

- Create/reset an unsaved local session.
- Save/load a session as a project asset.
- Edit seed, notes, position min/max, rotation min/max, scale min/max,
  relative/world position mode, and uniform/individual scale mode.
- Apply deterministic random transforms to selected scene objects with Undo.
- Show honest status for RandomControl, Composition, Deform, Terrain, and future
  Topology/DualGrid.
- Persist a local sample session asset for reviewer restart/readback.

## Honest Boundaries

- Random transform application is cockpit-owned and does not prove
  RandomControlTab coverage.
- Composition status is inspection-only until CSG/Blend actions are run and
  recorded.
- Deform status is conditional on package symbols and manual execution.
- Terrain status checks parameter-type availability only; it does not generate
  terrain.
- Topology/DualGrid is a future slot only.

## Next Review Axes

- SG-2: run a designer smoke pass and record transform/Undo/session evidence.
- CT-1: connect Composition verification results into the status tile.
- Topology preview: add a read-only DualGrid/topology IR preview slot without
  implementing the algorithm in the cockpit.

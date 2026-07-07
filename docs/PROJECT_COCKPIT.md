# Project Cockpit

## Current Cockpit Surface

The first designer-facing cockpit is a local Unity Editor window:

- Menu: `Tools/VastCore/Designer Cockpit`
- Code: `Assets/Editor/VastCore/DesignerCockpit/`
- Session type: `VastCoreDesignerSession`
- Default session asset folder: `Assets/Data/VastCore/DesignerSessions`
- Sample session asset: `Assets/Data/VastCore/DesignerSessions/Designer_Session.asset`

The cockpit gives a designer one place to inspect selected object count, session
name, seed, loaded asset status, last saved time, concise capability badges, and
a local random transform recipe without exposing every numeric range at once.

## Current Capability

- Create/reset an unsaved local session.
- Save/load a session as a project asset. This was user-confirmed for the MVP
  before this UX simplification slice.
- Edit seed, notes, and designer-facing random controls: Position Spread, Y
  Lock/Height Variation, Yaw Variation, Scale Variation, Relative Position, and
  Uniform Scale.
- Keep full position/rotation/scale min/max ranges available in Advanced ranges
  for compatibility with existing session assets.
- Apply deterministic random transforms to selected scene objects through the
  existing Undo-recorded code path. Manual Apply/Undo smoke remains pending
  unless a Unity scene pass is run.
- Show short status badges in the main cockpit and move verbose Review Status,
  raw notes, and class/package detection details into Diagnostics.
- Persist a local sample session asset for reviewer restart/readback.

## Honest Boundaries

- Random transform application is cockpit-owned and does not prove
  RandomControlTab coverage.
- Composition status is inspection-only until CSG/Blend actions are run and
  recorded.
- Deform status is conditional on package symbols and manual execution.
- Terrain status checks parameter-type availability only; it does not generate
  terrain. The current detection path is `Vastcore.Generation.*`; the older
  `Vastcore.Terrain.*` lookup does not resolve on this branch.
- Topology/DualGrid is a future slot only.

## Next Review Axes

- SG-2: run a designer smoke pass and record transform/Undo/session evidence.
- CT-1: connect Composition verification results into Diagnostics or a future
  Evidence Tile.
- Topology preview: add a read-only DualGrid/topology IR preview slot without
  implementing the algorithm in the cockpit.
- Terrain Preview: turn the status-only Terrain mode into a preview harness once
  the parameter detection and evidence tile contract are accepted.

# Designer Cockpit UX Notes

The cockpit should show task-relevant controls first and keep raw implementation
state out of the designer's main path.

## Current Rule

- Put session identity, seed, selected count, save/load state, and concise
  capability badges in the top summary.
- Keep primary actions visible: New, Save, Load, Refresh, and Apply.
- Use modes for task context: Overview, Random, Terrain, Compose, Deform, and
  Diagnostics.
- In Random, expose Position Spread, Y Lock/Height Variation, Yaw Variation,
  Scale Variation, Relative Position, and Uniform Scale before full vector
  ranges.
- Keep full min/max numeric ranges in Advanced ranges for compatibility and
  technical review.
- Put Review Status details, class/package detection, Terrain namespace notes,
  and raw status notes in Diagnostics.
- Empty selection is not an error. Disable Apply and use calm copy that tells
  the designer what unlocks the action.

## Deferred

- Evidence Tiles can promote validated Random, Composition, Deform, and Terrain
  evidence from Diagnostics into the main cockpit when real checks exist.
- Terrain Preview should be a separate mode-level harness, not a hidden side
  effect of opening the cockpit.
- DualGrid/topology remains a future read-only preview slot until that slice is
  explicitly selected.

## 2026-11-01 restart: observed scope and proposed improvements

Source inspection at accd587 confirms Random Apply edits selected transforms and
records Unity Undo. Relative Position adds offsets; selection is sorted by name
then InstanceID. Reproducibility needs the same baseline and target order, not
only the same seed. Scene modifications are marked dirty.

New creates an unsaved session. Save persists session fields and selects the
session asset; Load selects an existing asset directly. Session data contains no
scene target list, geometry, terrain configuration, version linkage or adoption
state. Save also calls SaveAssets globally. These facts are not live Editor
verification; the [smoke test](DESIGNER_COCKPIT_SMOKE_TEST.md) remains unexecuted
in this restart.

Use Hierarchy/Inspector and Unity's Scene name/dirty indicator for current target
and save scope. Use Structure Generator for geometry and Inspector for Stamp /
terrain settings. Preserve a saved baseline before Apply/Undo or regeneration.
Record technical success and the user's adoption decision separately against the
actual asset/Scene version.

Proposals, not implemented or adopted: show exact affected target names and count,
Scene and Prefab version/save scope, session-vs-Scene save status, and candidate
adoption status together; warn before New/Load discards an unsaved session edit.
Validate the existing screen before choosing a broad layout direction. None of
the historical layout candidates is adopted by this preparation.

The complete creation/save/reopen/output path is in
[SP-020](02_design/DESIGNER_PIPELINE_SPEC.md#8-2026-11-01-再開準備採用済み経路と今回の差分),
not a new cockpit runtime-generation commitment.

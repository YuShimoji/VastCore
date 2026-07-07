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

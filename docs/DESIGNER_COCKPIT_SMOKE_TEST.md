# Designer Cockpit Smoke Test

Purpose: verify the local Unity Editor cockpit is visible, honest about
untested areas, and usable for a first designer session.

## Access

- Menu path: `Tools/VastCore/Designer Cockpit`
- Session asset target: `Assets/Data/VastCore/DesignerSessions`
- Window class: `Vastcore.Editor.DesignerCockpit.VastCoreDesignerCockpitWindow`

## Manual Smoke Checklist

1. Open `Tools/VastCore/Designer Cockpit`.
2. Confirm the compact top summary shows session name, seed, selected object
   count, last saved UTC, loaded asset path/status, short capability badges,
   and the last operation message.
3. Confirm the primary action row exposes New, Save, Load, Refresh, and Apply.
4. Select zero scene objects and confirm Apply is disabled with calm copy:
   `Select a scene object to apply Random Variation.`
5. Switch through Overview, Random Variation, Terrain, Composition, Deform, and
   Diagnostics.
6. In Overview, change session name, seed, notes, and session asset selection.
7. In Random, adjust Position Spread, Y Lock/Height Variation, Yaw Variation,
   Scale Variation, Relative Position, and Uniform Scale.
8. Open Advanced ranges and confirm Position/Rotation/Scale min/max remain
   accessible without being always visible.
9. Click `Refresh` and confirm selected object count updates.
10. Create or select one or more scene objects.
11. Click `Apply`.
12. Confirm selected transforms changed and Unity Undo restores the previous
   transforms.
13. Click `Save` and confirm a `.asset` is created under
   `Assets/Data/VastCore/DesignerSessions`.
14. Click `New`, then `Load`, and confirm the saved fields are
    restored.
15. Open Diagnostics and confirm Review Status details, class/package
    detection details, Terrain namespace detection, and raw status notes live
    there rather than in the main mode panels.
16. Confirm Terrain, Compose, and Deform panels use concise status copy and do
    not imply runtime generation, CSG/Blend, or Deform execution is complete.

## Not Covered

- CSG/Blend execution from CompositionTab
- Deform package runtime behavior
- Terrain generation runtime behavior
- DualGrid/topology algorithm or preview
- Public release, cloud sync, or production acceptance

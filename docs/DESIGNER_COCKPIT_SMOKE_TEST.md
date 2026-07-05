# Designer Cockpit Smoke Test

Purpose: verify the local Unity Editor cockpit is visible, honest about
untested areas, and usable for a first designer session.

## Access

- Menu path: `Tools/VastCore/Designer Cockpit`
- Session asset target: `Assets/Data/VastCore/DesignerSessions`
- Window class: `Vastcore.Editor.DesignerCockpit.VastCoreDesignerCockpitWindow`

## Manual Smoke Checklist

1. Open `Tools/VastCore/Designer Cockpit`.
2. Confirm the top strip shows selected object count, session name, seed, last
   saved UTC, and status.
3. Select zero scene objects and confirm `Apply Random Transform to Selected`
   is disabled with a clear warning.
4. Create or select one or more scene objects.
5. Click `Refresh Status` and confirm selected object count updates.
6. Change session name, seed, notes, and random transform min/max values.
7. Click `Apply Random Transform to Selected`.
8. Confirm selected transforms changed and Unity Undo restores the previous
   transforms.
9. Click `Save Session` and confirm a `.asset` is created under
   `Assets/Data/VastCore/DesignerSessions`.
10. Click `New Session`, then `Load Session`, and confirm the saved fields are
    restored.
11. Confirm status tiles label Composition, Deform, Terrain, and
    Topology/DualGrid as partial, untested, or placeholder rather than complete.

## Not Covered

- CSG/Blend execution from CompositionTab
- Deform package runtime behavior
- Terrain generation runtime behavior
- DualGrid/topology algorithm or preview
- Public release, cloud sync, or production acceptance

# A3-1: UI Migration Apply Report (Menus)

- Scope: `Assets/Scripts/UI/Menus/`
- Date: 2025-10-06
- Author: Automation (Cascade)

## 1. Preview (Dry)

- Files scanned:
  - `Assets/Scripts/UI/Menus/TitleScreenManager.cs`

- Heuristics:
  - Legacy UI: `UnityEngine.UI` usage, `OnGUI()` methods
  - Text rendering: `TextMeshProUGUI`, `TMP_Text`, `TextMeshPro`
  - UITK: `UnityEngine.UIElements`, `UIDocument`
  - Namespace migration need: `NarrativeGen.UI` → `Vastcore.UI.*`

- Findings:
  - Namespace: `namespace Vastcore.UI.Menus` → OK (no migration needed)
  - Uses `TMPro.TextMeshPro` (3D/World TextMeshPro). No `TextMeshProUGUI` usage here.
  - No `UnityEngine.UI` references
  - No IMGUI `OnGUI()`
  - No `UnityEngine.UIElements` / `UIDocument`

```csharp
// Excerpt (TitleScreenManager.cs)
using UnityEngine;
using System.Collections;
using TMPro;
using Vastcore.Player;

namespace Vastcore.UI.Menus
{
    public class TitleScreenManager : MonoBehaviour
    {
        [SerializeField] private TextMeshPro m_TitleText;
        // ...
    }
}
```

## 2. Apply

- Result: No code changes required for this scope.
- Rationale: Target namespace already aligned (`Vastcore.UI.Menus`). No legacy UI/IMGUI usage detected. Text rendering uses 3D `TextMeshPro`, which is valid for world-space title mesh.

## 3. Verify (Compile & Quick Runtime)

- Compile (Editor):
  1. Open Unity project in Editor
  2. Allow scripts to recompile on domain reload (no errors expected)

- Quick Runtime Check (manual):
  1. Open a scene that initializes `VastcoreGameManager` and attaches `TitleScreenManager`
  2. Enter Play Mode
  3. Confirm behavior:
     - Title appears within view (approx. Viewport 0.5, 0.6 at `m_DisplayDistance`)
     - Title fades in, faces camera
     - Look-away beyond `m_LookAwayAngle` triggers fade-out and deactivation

- Expected outcome: No functional changes introduced; behavior unchanged and stable.

## 4. Risk & Notes

- Tier: 1 (docs-only, no code delta)
- No migration actions were necessary. Future A3 tasks can proceed to next scoped folders.

## 5. Artifacts

- Report file: `Documentation/QA/A3-1_UI_MIGRATION_APPLY_REPORT.md`
- Branch: feature (see PR)

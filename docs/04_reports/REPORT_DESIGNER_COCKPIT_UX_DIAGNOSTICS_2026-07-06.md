# Designer Cockpit UX Diagnostics Report

Date: 2026-07-06

## What Changed

This slice reorganized the Designer Cockpit from an always-visible parameter
surface into a mode-based authoring surface. The window still opens from
`Tools/VastCore/Designer Cockpit`, keeps the existing session asset workflow,
and preserves the deterministic random transform path with Undo recording.

## Files

| File | Role |
| --- | --- |
| `Assets/Editor/VastCore/DesignerCockpit/VastCoreDesignerCockpitWindow.cs` | UI simplification, mode selector, context panels, Diagnostics drawer, corrected status detection |
| `docs/DESIGNER_COCKPIT_SMOKE_TEST.md` | Updated manual smoke flow for top summary, modes, advanced ranges, Apply/Undo, and Diagnostics |
| `docs/PROJECT_COCKPIT.md` | Updated current cockpit capability and honest boundaries |
| `docs/DESIGNER_COCKPIT_UX_NOTES.md` | Captured the task-relevant-controls-first design rule |

## Assembly Impact

| Area | Assembly | Notes |
| --- | --- | --- |
| Designer Cockpit EditorWindow | `Vastcore.Editor.Root` | No asmdef or reference change |
| Runtime/session data | `Vastcore.Editor.Root` | No serialized field migration |

## Validation

| Check | Result | Notes |
| --- | --- | --- |
| Remote parity | Pass | After `git fetch --all --prune`, local HEAD and `origin/codex/vc-rst-2e-upm-root-cause` read `0 0` with `git rev-list --left-right --count` |
| `git diff --check` | Pass | Whitespace check passed; Git reported only expected CRLF normalization warnings |
| Menu/class duplicate search | Pass | One `Tools/VastCore/Designer Cockpit` menu item and one cockpit window class found |
| Unity batchmode compile | Blocked before C# compile | `scripts/check-compile.ps1` stopped at Package Manager resolution: `The "path" argument must be of type string. Received undefined. No packages loaded.` Existing restart reports classify this as a machine/UPM blocker, not a cockpit code error |
| Narrow direct compiler rerun | Inconclusive | Direct `csc` reruns reached Unity/NetStandard/IMGUI reference-set setup failures. The latest Unity log has no `error CS` entries, but full source acceptance still needs Unity to pass package resolution or a correctly scripted narrow compile harness. |

Manual visual smoke remains required for final designer acceptance because this
report cannot prove the Editor window layout inside a live Unity session.

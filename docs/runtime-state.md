# VastCore Runtime State

Last Updated: 2026-07-11

## Current Position

| Field | Value |
|---|---|
| Project | VastCore Terrain Engine |
| Branch | `codex/vc-development-readiness-20260711` |
| Integrated baseline | `f145df6` from `origin/codex/vc-ai-workflow-refresh-20260710` |
| Active outcome | Keep the latest remote workflow/Cockpit baseline intact, restore trustworthy local Unity compile and non-zero test gates, and hand the next supervisor a long-range product path |
| Active artifact | `docs/04_reports/REPORT_REMOTE_SYNC_DEVELOPMENT_READINESS_AND_LONG_RANGE_GOALS_2026-07-11.md` |
| Product artifact | `docs/DESIGNER_COCKPIT_SMOKE_TEST.md` |
| Current bottleneck | Local Unity compile, EditMode 596/596, and PlayMode 9/9 now pass; the remaining immediate product gate is the Designer Cockpit in-Editor layout, Apply/Undo, and session Save/New/Load acceptance |
| Batch boundary | Remote sync, narrow development-gate repairs, local Unity validation, and supervisor handoff; no `Packages`, `ProjectSettings`, terrain/DualGrid algorithm, broad visual production, or release change |
| Change relation | unblocker |

## Current Block

Completed or verified in this block:

- Started from a clean, upstream-equal `ccc6822`, fetched with prune, and proved
  that `origin/codex/vc-ai-workflow-refresh-20260710` at `f145df6` is its strict
  six-commit descendant with no divergence.
- Switched to that latest remote handoff, pulled with `--ff-only`, and verified
  `HEAD...@{u} = 0 0` before starting local repairs.
- Created `codex/vc-development-readiness-20260711` from `f145df6` so draft PR
  #49 remains a workflow-only review instead of absorbing product/test repairs.
- Repaired two blank Unity `.meta` GUIDs that caused 21 EditMode tests to be
  silently ignored; all 21 restored tests now pass.
- Made `VastcoreLogger` safe in EditMode and aligned tests with its timestamped,
  categorized, exception-preserving console contract.
- Restored PlayMode discovery by removing the stale Editor exclusion from the
  test-only asmdef and documented that platform boundary in assembly SSOT.
- Fixed the three test compile errors exposed by real discovery and updated one
  obsolete Unity 6 test API.
- Fixed `SliderUIElement` child creation so Fill/Handle areas own
  `RectTransform` before layout access.
- Ran Unity 6000.3.6f1 compile successfully with no `error CS`, invalid GUID,
  `path undefined`, or `No packages loaded` markers.
- Ran EditMode `596 / 596` and PlayMode `9 / 9`, with failed and skipped both 0.
- Added a supervisor-facing current-state, acceptance, and G0-G9 goal report.

Deliberately not changed:

- Designer Cockpit source, sessions, sample assets, and visual layout.
- `Packages/`, `ProjectSettings/`, Unity installation, caches, or other running
  Unity projects.
- Five existing stashes and two other worktrees.
- The divergent remote-handoff branch with mixed package/settings/deletion work.
- Broad deletion/archive of old workflow SSOT, indexes, and restart reports.
- GitHub default branch, branch protection, or the existing conflicting draft PR.

## Current Trust Assessment

- Trusted: baseline `f145df6b1866f72da043be8244f99bbd4f62b95a`
  exactly matched `origin/codex/vc-ai-workflow-refresh-20260710` before repair.
- Trusted: repair commit `e14a0dbbfc105bfbb258945bfa4afbb803603a97`
  contains only the narrow Unity/test-gate repairs and assembly-owner update.
- Trusted on this machine: Unity Editor 6000.3.6f1 registered 71 packages,
  completed C#/IL post-processing/Tundra, and exited batchmode with code 0.
- Trusted on this machine: `editmode-results.xml` is 596/596 and
  `playmode-results.xml` is 9/9; both have failed 0 and skipped 0.
- Trusted: restored `.meta` test suites contributed 21/21 passing tests.
- Trusted: Project State/Pulse checks on ancestor `f145df6` are green and draft
  PR #49 is open, clean, and based on the Cockpit UX branch.
- Not accepted: Cockpit layout, Random Variation Apply/Undo, and session
  Save/New/Load in a live Editor.
- Not configured: repository secret `UNITY_LICENSE`; the repaired Actions
  workflow must report Unity test jobs as skipped until that secret exists.
- Local-only evidence boundary: `artifacts/logs/` and `artifacts/test-results/`
  are ignored; another machine or CI must not inherit these pass claims.
- External visibility risk: GitHub still defaults to obsolete `master`; Project
  Pulse mitigates current-state visibility but does not replace baseline repair.

## Open Decisions

- Run the one-batch Cockpit acceptance and choose accept or revise from evidence.
- After Cockpit acceptance, fast-forward the accepted tip to `main`, change the
  GitHub default from `master` to `main`, and align protections/workflows.
- After the current screen is captured, choose Operations Console, Generative
  Atelier, or Guided Forge before broad Cockpit production; Operations Console
  is the current recommendation for the acceptance bottleneck.
- Run a separate reference-aware Excise slice before deleting or archiving old
  SSOT/index/handoff documents.

## Next Action

Use `docs/DESIGNER_COCKPIT_SMOKE_TEST.md` as one batched manual acceptance pass:

1. Open `Tools/VastCore/Designer Cockpit` and inspect the top summary, primary
   actions, mode selector, mode panels, and Diagnostics drawer.
2. Apply Random Variation to selected objects and verify Undo.
3. Save, create a new session, reload, and verify restored fields.
4. Capture the layout result and all functional checks together, update the
   Cockpit evidence, and choose accept or revise before starting G1/G2.

This machine no longer reproduces the earlier UPM failure. If an interactive
Editor session does reproduce it, record the exact environment once and treat it
as a new evidence boundary rather than restarting the old micro-diagnostic chain.

## External Pulse

The pinned GitHub issue
[#48 Project Pulse](https://github.com/YuShimoji/VastCore/issues/48) is generated
from this file by `.github/workflows/project-pulse.yml`. This file remains the
authoritative source; the issue is the public projection and discussion surface.
Repository variable `PROJECT_PULSE_BRANCH` assigns publication to
`codex/vc-development-readiness-20260711` as the active handoff branch. Draft PR
[#49](https://github.com/YuShimoji/VastCore/pull/49) remains the narrow workflow
review surface for ancestor `f145df6`; it is not the tip of the readiness repair.

## Remote Resume

Do not start from GitHub's default `master`; it is obsolete. The current remote
handoff is the readiness branch named above.

For an existing clone that already has the local branch:

```powershell
git fetch origin --prune
git switch codex/vc-development-readiness-20260711
git pull --ff-only
```

For a clone that does not yet have the local branch:

```powershell
git fetch origin --prune
git switch --track origin/codex/vc-development-readiness-20260711
```

Then read only the normal restart set and validate the recovered state:

```powershell
Get-Content AGENTS.md
Get-Content docs/REPO_LOCAL_RULES.md
Get-Content docs/runtime-state.md
.\scripts\check-project-state.ps1 -ExpectedBranch codex/vc-development-readiness-20260711
```

Then read the active report and resume from `## Next Action`:

```powershell
Get-Content docs/04_reports/REPORT_REMOTE_SYNC_DEVELOPMENT_READINESS_AND_LONG_RANGE_GOALS_2026-07-11.md
```

Use PR #49 only for the ancestor workflow diff and Issue #48 for the public
current-state projection. Do not merge old draft PR #47, apply the five
historical stashes, modify the two other worktrees, or restart the old UPM
diagnostic chain unless new evidence changes the current bottleneck.

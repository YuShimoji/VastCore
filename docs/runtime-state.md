# VastCore Runtime State

Last Updated: 2026-08-12

## Current Position

| Field | Value |
|---|---|
| Project | VastCore Terrain Engine |
| Branch | `codex/vc-development-readiness-20260711` |
| Integrated baseline | `fbf8c9a` equal to `origin/codex/vc-development-readiness-20260711` |
| Active outcome | Preserve the latest remote handoff, re-establish trustworthy Unity gates on the PLANNER007 checkout, and keep Cockpit acceptance as the next product gate |
| Active artifact | `docs/04_reports/REPORT_DEVELOPMENT_RESUME_CHECKOUT_AUDIT_2026-08-12.md` |
| Product artifact | `docs/DESIGNER_COCKPIT_SMOKE_TEST.md` |
| Current bottleneck | On the PLANNER007 profile, Unity 6000.3.6f1 UPM exits before C# compile with `path undefined / No packages loaded`; compile, current tests, and Cockpit acceptance are blocked here even though the same commit passed on the prior checkout/machine |
| Batch boundary | Saved-checkout remote sync, read-only environment audit, local validation, and supervisor handoff; no dependency/global Unity repair, `Packages`, `ProjectSettings`, terrain/DualGrid algorithm, broad visual production, or publication change |
| Change relation | blocker isolation |

## Current Block

Completed or verified in this block:

- Started from clean `codex/vc-ai-workflow-refresh-20260710` at `f145df6`,
  fetched `origin` with prune and tags, and confirmed its upstream parity was
  0 ahead / 0 behind.
- Found the strict two-commit descendant and current remote handoff
  `origin/codex/vc-development-readiness-20260711` at `fbf8c9a`; switched from
  the clean checkout to a local tracking branch and verified 0 / 0 parity.
- Confirmed there are no Git commits dated 2026-08-11 or 2026-08-12. The 15
  tracked files with an August 12 filesystem mtime were materialized by branch
  switching and have blobs identical to `HEAD`.
- Preserved 30 ignored roots, five stashes, and two other worktrees. Tracked,
  staged, and untracked counts remained zero through sync and Unity execution.
- Verified Unity 6000.3.6f1, the complete 52-direct / 71-total manifest-lock
  mapping, Project State, Git diff hygiene, and 210 / 210 materialized LFS files.
- Ran the canonical compile script into a new ignored evidence directory. On
  this profile UPM failed before C# compilation with `path undefined / No
  packages loaded`; current EditMode and PlayMode tests were therefore not run.
- Added a supervisor-facing checkout audit that separates current local facts,
  prior-machine test evidence, Drive/Sheets acceptance, and the E0-G9 path.

Deliberately not changed:

- Designer Cockpit source, sessions, sample assets, and visual layout.
- `Packages/`, `ProjectSettings/`, Unity installation, caches, or other running
  Unity projects.
- Five existing stashes and two other worktrees.
- The divergent remote-handoff branch with mixed package/settings/deletion work.
- Broad deletion/archive of old workflow SSOT, indexes, and restart reports.
- GitHub default branch, branch protection, Project Pulse, remote branches, PRs,
  Drive, or Sheets.

## Current Trust Assessment

- Trusted: local `fbf8c9ae994adbf78e020dcdcfd2f130cd3c9621` exactly
  matches `origin/codex/vc-development-readiness-20260711`; its ancestor
  `e14a0db` contains the narrow Unity/test-gate repairs.
- Trusted on this checkout: canonical state validation, manifest-lock mapping,
  Git diff check, clean tracked status, and LFS 210 / 210 all pass.
- Trusted as historical evidence from the prior checkout/machine: this commit
  registered 71 packages, compiled, and passed EditMode 596 / 596 plus PlayMode
  9 / 9. Those results are not current-machine acceptance.
- Blocked on this machine: Unity 6000.3.6f1 reaches licensing and initial Asset
  Database refresh, then UPM exits with `path undefined / No packages loaded`
  before C# compilation. The new local evidence is under
  `artifacts/resume-20260812-019ff3b6/`.
- Not accepted: Cockpit layout, Random Variation Apply/Undo, and session
  Save/New/Load in a live Editor.
- Not configured: repository secret `UNITY_LICENSE`; the repaired Actions
  workflow must report Unity test jobs as skipped until that secret exists.
- Local-only evidence boundary: existing test XML is from March 2026 and does
  not represent current HEAD; another machine or CI must not inherit pass claims.
- External visibility risk: GitHub still defaults to obsolete `master`; Project
  Pulse remains a remote projection of the last pushed state because this local
  audit is intentionally not published in the current authorization.

## Open Decisions

- Choose a safe E0 environment route: after other Unity work is closed, either
  authorize a scoped Unity 6000.3.6f1 / embedded UPM repair or use the known-good
  host to revalidate the same commit.
- After packages and compile succeed, run non-zero EditMode and PlayMode gates,
  then run the one-batch Cockpit acceptance and choose accept or revise.
- After Cockpit acceptance, fast-forward the accepted tip to `main`, change the
  GitHub default from `master` to `main`, and align protections/workflows.
- After the current screen is captured, choose Operations Console, Generative
  Atelier, or Guided Forge before broad Cockpit production; Operations Console
  is the current recommendation for the acceptance bottleneck.
- Run a separate reference-aware Excise slice before deleting or archiving old
  SSOT/index/handoff documents.

## Next Action

Do not restart the historical cache, ACL, path, or network micro-diagnostic
chain. The PLANNER007 profile has reproduced the same UPM failure in VastCore
and earlier clean controls, while the same commit passed on another checkout.

1. Finish or safely close other Unity project work before any global Editor/UPM
   action.
2. With explicit approval, repair Unity 6000.3.6f1 / embedded UPM or compare its
   installed state with the known-good host; test a clean control first.
3. Only after the control resolves packages, run `scripts/check-compile.ps1`,
   non-zero EditMode, and non-zero PlayMode tests in VastCore.
4. Then use `docs/DESIGNER_COCKPIT_SMOKE_TEST.md` for the batched layout,
   Apply/Undo, Save/New/Load, screenshot, and accept/revise decision.

## External Pulse

The pinned GitHub issue
[#48 Project Pulse](https://github.com/YuShimoji/VastCore/issues/48) is generated
from this file by `.github/workflows/project-pulse.yml`. This file remains the
authoritative source; the issue is the public projection and discussion surface.
Repository variable `PROJECT_PULSE_BRANCH` assigns publication to
`codex/vc-development-readiness-20260711` as the active handoff branch. Draft PR
[#49](https://github.com/YuShimoji/VastCore/pull/49) remains the narrow workflow
review surface for ancestor `f145df6`; it is not the tip of the readiness repair.
The August 12 audit is local-only and has not updated Issue #48.

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
Get-Content docs/04_reports/REPORT_DEVELOPMENT_RESUME_CHECKOUT_AUDIT_2026-08-12.md
```

Use PR #49 only for the ancestor workflow diff and Issue #48 for the public
current-state projection. Do not merge old draft PR #47, apply the five
historical stashes, modify the two other worktrees, or restart the old UPM
diagnostic chain unless new evidence changes the current bottleneck.

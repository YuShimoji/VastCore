# VastCore Runtime State

Last Updated: 2026-07-10

## Current Position

| Field | Value |
|---|---|
| Project | VastCore Terrain Engine |
| Branch | `codex/vc-ai-workflow-refresh-20260710` |
| Integrated baseline | `fd1cb29` from `origin/codex/vc-rst-cockpit-ux-diagnostics-20260706` |
| Active outcome | Make supervisor-to-developer work outcome-based, keep current state visible outside chat, and preserve the Cockpit review path |
| Active artifact | `docs/04_reports/REPORT_AI_COLLABORATION_WORKFLOW_REFRESH_2026-07-10.md` |
| Product artifact | `docs/DESIGNER_COCKPIT_SMOKE_TEST.md` |
| Current bottleneck | Git and source editing are ready; Unity batchmode still exits before C# compile with UPM `path undefined`, and the Designer Cockpit has not received its in-Editor manual smoke/layout acceptance |
| Batch boundary | Workflow docs/automation, state validation, and external Project Pulse only; no `Assets`, `Packages`, `ProjectSettings`, dependency, or product-code change in this branch |
| Change relation | unblocker |

## Current Block

Completed or verified in this block:

- Fetched all remote branches and tags with prune, then ran `git pull --ff-only`.
  The prior Cockpit UX branch is exactly at its remote tip `fd1cb29`.
- Fast-forwarded local `main` from `a9e7142` to `origin/main` at `39f790c`;
  no unique local `main` commits were discarded.
- Created `codex/vc-ai-workflow-refresh-20260710` from the latest Cockpit UX tip
  so workflow changes do not contaminate the product review branch.
- Re-ran `scripts/check-compile.ps1` with Unity 6000.3.6f1. It exited 1 after
  Package Manager reported `The "path" argument must be of type string. Received
  undefined. No packages loaded.` No C# compiler error was reached.
- Replaced micro-prompt behavior with an outcome-based Mission Packet, bounded
  autonomy, concrete stop conditions, an early creative direction checkpoint,
  proof-slice review, and a two-round escape from local tweak loops.
- Made runtime-state/owning-doc sync part of completion and added a state check
  plus GitHub Project Pulse automation.
- Created and pinned Project Pulse Issue #48; repository variables identify its
  issue number and this branch as the single authorized publishing source.
- Verified GitHub Actions run `29082183177`: canonical state validation and
  Project Pulse publication both passed, and Issue #48 was updated from this
  branch. The new workflow now uses the current checkout/github-script majors.
- Verified follow-up run `29083404696` with those current action majors; both
  jobs passed without the prior Node runtime warning.
- The same push exposed an existing invalid Unity workflow before any job ran.
  Replaced the direct secret-in-job-condition with a license-gate output and
  aligned its configured Editor from 6000.2.2f1 to project version 6000.3.6f1.
- Dispatched repaired Unity workflow run `29083435552`: license-gate passed and
  the Unity test job was correctly skipped because no license secret exists.
- Opened draft PR #49 against the preceding Cockpit UX branch so review contains
  only this workflow refresh rather than 349 commits from obsolete `master`.
- Restored the canonical `docs/INVARIANTS.md` from mojibake to readable Japanese.
- Removed stale "current" claims from `docs/project-context.md`; historical
  handoffs remain evidence only.

Deliberately not changed:

- Designer Cockpit source, sessions, sample assets, and Unity `.meta` files.
- `Packages/`, `ProjectSettings/`, Unity installation, caches, or other running
  Unity projects.
- Five existing stashes and two other worktrees.
- The divergent remote-handoff branch with mixed package/settings/deletion work.
- Broad deletion/archive of old workflow SSOT, indexes, and restart reports.
- GitHub default branch, branch protection, or the existing conflicting draft PR.

## Current Trust Assessment

- Trusted: `origin/codex/vc-rst-cockpit-ux-diagnostics-20260706` and the local
  integrated baseline are both `fd1cb29116e6462684d91252a715e7eea3b563b6`.
- Trusted: `origin/main` is a direct ancestor of that baseline and local `main`
  now matches `origin/main` at `39f790c`.
- Trusted: the workflow refresh started from a clean worktree; product code and
  Unity configuration are untouched in this branch.
- Trusted: Unity Editor 6000.3.6f1, Git 2.50.1, and Node 22.19.0 are installed.
- Trusted: the latest batch log at `artifacts/logs/compile-check.log` reproduces
  the known UPM failure before package load and before C# compilation.
- Trusted: GitHub Actions run `29082183177` passed both Project State and Pulse
  jobs and updated the pinned external issue from the configured owner branch.
- Trusted: follow-up Pulse run `29083404696` passed on current action majors;
  Unity workflow dispatch `29083435552` passed its gate and skipped tests.
- Not accepted: current C# compile, EditMode/PlayMode tests, Cockpit layout,
  Random Variation Apply/Undo, and session save/load in the Editor.
- Not configured: repository secret `UNITY_LICENSE`; the repaired Actions
  workflow must report Unity test jobs as skipped until that secret exists.
- External visibility risk: GitHub still defaults to obsolete `master`; Project
  Pulse mitigates current-state visibility but does not replace baseline repair.

## Open Decisions

- Review and adopt the Mission Packet/autonomy/direction-checkpoint contract.
- After Cockpit acceptance, fast-forward the accepted tip to `main`, change the
  GitHub default from `master` to `main`, and align protections/workflows.
- Run a separate reference-aware Excise slice before deleting or archiving old
  SSOT/index/handoff documents.

## Next Action

Use `docs/DESIGNER_COCKPIT_SMOKE_TEST.md` as one batched manual acceptance pass:

1. Open `Tools/VastCore/Designer Cockpit` and inspect the top summary, primary
   actions, mode selector, mode panels, and Diagnostics drawer.
2. Apply Random Variation to selected objects and verify Undo.
3. Save, create a new session, reload, and verify restored fields.
4. Capture the layout result and all functional checks together, then update the
   Cockpit evidence and choose whether to advance, revise direction, or repair
   the Unity environment.

If the Editor cannot reach the Cockpit because of the same UPM failure, record
that once as an environment blocker and do not split it into another chain of
micro-prompts.

## External Pulse

The pinned GitHub issue
[#48 Project Pulse](https://github.com/YuShimoji/VastCore/issues/48) is generated
from this file by `.github/workflows/project-pulse.yml`. This file remains the
authoritative source; the issue is the public projection and discussion surface.
Repository variable `PROJECT_PULSE_BRANCH` currently assigns publication to
`codex/vc-ai-workflow-refresh-20260710` and must move with an explicit branch
handoff. The active review surface is draft PR
[#49](https://github.com/YuShimoji/VastCore/pull/49).

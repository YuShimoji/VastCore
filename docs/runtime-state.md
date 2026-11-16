# VastCore Runtime State

Last Updated: 2026-11-16

## Current Position

| Field | Value |
|---|---|
| Project | VastCore Terrain Engine |
| Branch | `codex/vc-development-readiness-20260711` |
| Integrated baseline | `4c7f9fa` for Unity source, packages, and project settings |
| Active outcome | Resume Designer Cockpit acceptance on the verified Unity baseline |
| Active artifact | `docs/DESIGNER_COCKPIT_SMOKE_TEST.md` |
| Current bottleneck | Designer Cockpit layout, Apply/Undo, and session Save/New/Load await live Editor acceptance |
| Change relation | unblocker |

## Current Trust Assessment

- On PLANNER007, Unity 6000.3.6f1 registered 71 packages and passed
  `scripts/check-compile.ps1`. EditMode passed 596/596 and PlayMode passed 9/9
  with `-RequireNonZeroTests` on `4c7f9fa`. The manifest/lock mapping is
  52 direct dependencies and 71 total. All 210 tracked LFS objects are
  materialized and passed `git lfs fsck`. Local logs/XML are under
  `artifacts/logs/resume-20261116/`.
- The August startup failure (`path undefined / No packages loaded`) did
  not recur. Its historical evidence is in
  `docs/04_reports/REPORT_DEVELOPMENT_RESUME_CHECKOUT_AUDIT_2026-08-12.md`.
  The current run did not require an Editor/UPM repair or package change.
- The three worktrees were clean at census and their heads are included in
  the readiness history. All named local branches already had remote
  coverage. Keep the five historical stashes and old reflog lineage outside
  the current development branch. Old draft PR #47 combines package/settings
  edits and large deletions. The October Cloud environment is a separate
  tooling branch (`origin/codex/cloud-codex-environment-20261011`).
- Cockpit behavior in a live Editor and release acceptance are pending.
  GitHub Unity test jobs depend on the repository's `UNITY_LICENSE` secret.

## Next Action

1. Open the repo with Unity 6000.3.6f1. Follow the Designer Cockpit smoke test
   from `Tools/VastCore/Designer Cockpit`. Check layout and selection,
   Random Variation Apply/Undo, and session Save/New/Load in one batch.
2. Save a screenshot and record the accept/revise decision. If revision is
   needed, use the observations to select the next Cockpit change.
3. After Cockpit acceptance, consider fast-forwarding the accepted tip to
   `main` and aligning the default branch, protections, and workflows.

If package startup fails again, keep the new compile log and compare it
with the successful November run before revisiting Editor/UPM repair.

## External Pulse

[#48 Project Pulse](https://github.com/YuShimoji/VastCore/issues/48) is the
projection of this file. `PROJECT_PULSE_BRANCH` assigns publication to the
readiness branch. Draft [PR #49](https://github.com/YuShimoji/VastCore/pull/49)
covers the ancestor workflow change at `f145df6`.

## Remote Resume

GitHub defaults to the historical `master`. Use the readiness branch
explicitly on an existing checkout:

```powershell
git fetch origin --prune
git switch codex/vc-development-readiness-20260711
git pull --ff-only
.\scripts\check-project-state.ps1 -ExpectedBranch codex/vc-development-readiness-20260711
```

If there is no local tracking branch, use
`git switch --track origin/codex/vc-development-readiness-20260711` for
the switch step. Read `AGENTS.md`, `docs/REPO_LOCAL_RULES.md`, and this
file before starting the Cockpit check.

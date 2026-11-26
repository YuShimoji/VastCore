# NAV.md - VastCore document map

Use this file when you are unsure where to look. It is an index, not a source of
truth for rules, status, or implementation details. Updated 2026-11-26.

## Normal Restart

1. `AGENTS.md` - repo entry pointer
2. `docs/REPO_LOCAL_RULES.md` - short operating rules
3. `docs/runtime-state.md` - current position and next action

Stop there unless the task needs more evidence.

## Next Work Entry

The current work is SP-021: generate a region from a seed, draw it as a bird's-eye
map, and run a point-and-search exploration on top (user direction, 2026-11-21).

1. `docs/runtime-state.md` - `Next Action`
2. `docs/02_design/REGION_MAP_EXPLORATION_SPEC.md` - user's original request,
   whole-process table (§4), how to run (§7), open decisions (§9)
3. Code: `Assets/Scripts/RegionMap/`, scene `Assets/Scenes/RegionExplorer.unity`,
   preview console `tools/RegionMapPreview/`, batch entry `scripts/region-map.ps1`

On hold (not the next work unless the user reopens it): SP-017 Arch round trip
(`docs/02_design/STAMP_EXPORT_PIPELINE_SPEC.md`) and SP-020
(`docs/02_design/DESIGNER_PIPELINE_SPEC.md`). Their last state is in
`docs/restart/VC_RESTART_20261101_HISTORY.md`.

## Core Owners

- [スクリーンショットと進捗](project-gallery.html) — ブラウザーで開く画像一覧・確認ページ・開発履歴
- Product purpose and world-level priority: `docs/SSOT_WORLD.md`
- Current state and active bottleneck: `docs/runtime-state.md`
- Public current-state projection: pinned GitHub Issue
  [#48 Project Pulse](https://github.com/YuShimoji/VastCore/issues/48), generated
  from `docs/runtime-state.md`
- Durable decisions and handoff history: `docs/project-context.md`
- Assembly boundaries: `docs/02_design/ASSEMBLY_ARCHITECTURE.md`
- Code standards: `docs/03_guides/UNITY_CODE_STANDARDS.md`
- Compile diagnosis: `docs/03_guides/COMPILATION_GUARD_PROTOCOL.md`
- Architecture overview: `docs/ARCHITECTURE.md`
- Spec registry and viewer data: `docs/spec-index.json` (`docs/spec-viewer.html`)
- Designer Cockpit feature and checks: `docs/PROJECT_COCKPIT.md`,
  `docs/DESIGNER_COCKPIT_SMOKE_TEST.md`, `docs/DESIGNER_COCKPIT_UX_NOTES.md`

## AI / Agent Rule Owners

- Thin entry pointer: `AGENTS.md`
- Repo-local daily rules: `docs/REPO_LOCAL_RULES.md`
- Vendor-neutral gates: `docs/ai/*.md`
- Non-negotiables: `docs/INVARIANTS.md`
- User request ledger: `docs/USER_REQUEST_LEDGER.md`
- Operator workflow: `docs/OPERATOR_WORKFLOW.md`
- Interaction/reporting notes: `docs/INTERACTION_NOTES.md`
- Claude Code pointer: `.claude/CLAUDE.md`
- Codex local config: `.codex/config.toml`; Cloud Codex: `docs/CLOUD_CODEX.md`
- State validation and external projection: `scripts/check-project-state.ps1`
  and `.github/workflows/project-pulse.yml`

## Folder Roles

"History" means evidence from a past date. Do not read its status, test counts,
branches, or next steps as current. Files stay in place because scripts, the
spec viewer, Unity editor tools, and many docs link to them.

| Path | Role |
|---|---|
| `docs/` root | Current owners listed above, plus history: `HANDOVER.md`, `WORKFLOW_STATE_SSOT.md`, `MILESTONE_PLAN.md`, `SESSION_SUMMARY_2026-03-04.md`, `DOCS_INDEX.md` (old index; use this file instead) |
| `docs/01_planning/` | Plans up to 2026-03. `DEVELOPMENT_ROADMAP_2026.md` is the long-range plan (dated 2026-03-17); the rest is history |
| `docs/02_design/` | Feature specs. Active: SP-021 `REGION_MAP_EXPLORATION_SPEC.md` (3D look target: `REGION_TERRAIN_LOOK_SPEC.md`), `ASSEMBLY_ARCHITECTURE.md`. On hold: SP-017 `STAMP_EXPORT_PIPELINE_SPEC.md`, SP-020 `DESIGNER_PIPELINE_SPEC.md`. Status per spec in `spec-index.json` |
| `docs/03_guides/` | How-to guides. Active: `UNITY_CODE_STANDARDS.md`, `COMPILATION_GUARD_PROTOCOL.md` |
| `docs/04_reports/` | History reports. `LEGACY_UI_MIGRATION_REPORT.md`, `A3-2_UI_MIGRATION_APPLY_REPORT.md`, `UI_MIGRATION_RULES_DRYRUN.md` are rewritten by Unity editor tools |
| `docs/restart/` | History of restart and UPM investigations (2026-06 to 2026-07). `VC_RESTART_20261101_HISTORY.md` holds the moved 2026-11-01/16/21 state bodies and SP-017/SP-020 run records |
| `docs/tasks/` | Task tickets frozen at 2026-03-17 (`TASK_INDEX.md`). SP-017/SP-020 work has no ticket here |
| `docs/terrain/`, `docs/inbox/`, `docs/EXAMPLES/` | History (2026-01 to 2026-03) and samples |
| `docs/architecture/` | 2026-06-15 compile and dependency risk evidence (M0) |
| `docs/ai/` | Vendor-neutral gates (current) |
| `Documentation/` (repo root) | Concept art images |

## Staleness Rule

If this index or another doc points to a missing file, treat that pointer as
stale. Do not block the task just because an old reference exists.

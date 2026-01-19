# Phase 0: SSOT確認

## 目的
環境とSSOTを確認し、作業可能な状態にする。

## 手順
1. `.cursor/MISSION_LOG.md` を読む（無ければ `.cursor/MISSION_LOG_TEMPLATE.md` から作成）
2. SSOT 確認: `docs/Windsurf_AI_Collab_Rules_latest.md`（無ければ `v2.0.md` -> `v1.1.md` の順で探索）
3. `ensure-ssot.js` で SSOT を補完（最大1回のみ試行）
4. `docs/HANDOVER.md` に `GitHubAutoApprove: true/false` が記載されているか確認（未記載なら1回だけ確認して追記）
5. MISSION_LOG.md を更新（Phase 0 完了を記録）

## 完了条件
- SSOT が参照可能
- MISSION_LOG.md が存在し、Current Phase が P1 に更新されている

## 次フェーズ
P1（Sync & Merge）


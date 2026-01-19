# Phase 1: Sync & Merge

## 目的
リモートと同期し、Worker レポートを回収する。

## 手順
1. `git fetch origin` → `git status -sb`
2. `docs/inbox/` を確認し、レポートがあれば `docs/HANDOVER.md` に統合
3. 統合済みレポートは `docs/reports/` へアーカイブ
4. MISSION_LOG.md を更新（Phase 1 完了を記録）

## 完了条件
- `docs/inbox/` は `.gitkeep` のみ
- 統合済みレポートが HANDOVER に反映されている

## 次フェーズ
P1.5（巡回監査）


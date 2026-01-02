# Phase 6: Orchestrator Report（保存/検証/提案）

## 目的

- 状態を確定させ、次の行動が迷わない形で残す（“一度きりで終わる”を防ぐ）
- Complete Gate 相当の検証を通し、虚偽完了を防ぐ

## 手順

1. レポート保存（ファイル）
   - `templates/ORCHESTRATOR_REPORT_TEMPLATE.md` をベースに `docs/inbox/REPORT_ORCH_<ISO8601>.md` を作成

2. 検証（必須）
   - `report-validator.js` を実行し、成功/失敗/警告をレポートへ追記
   - HANDOVER も検証対象にする（可能なら `--profile handover`）

3. Inbox整理（可能なら）
   - `finalize-phase.js` が使える場合は優先
   - 使えない場合は `docs/inbox/REPORT_*.md` を `docs/reports/` に移動（必要なら commit）

4. MISSION_LOG 更新（必須）
   - Phase 6 完了として記録
   - 次フェーズ/次アクション（担当込み）を必ず記録
   - 改善提案も記録

## チャット出力（固定5セクションのみ）

必ず以下の5セクションだけを、この順番で出力する。追加セクションは禁止。

1. `## 現状`
2. `## 次のアクション`
3. `## ガイド`
4. `## メタプロンプト再投入条件`
5. `## 改善提案（New Feature Proposal）`



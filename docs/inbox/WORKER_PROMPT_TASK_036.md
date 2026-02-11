以下のタスクチケットを実行してください。

# Task Ticket
`docs/tasks/TASK_036_DualGridInspectorProfilePreview.md`

# 指示
1. Scope/DoD を満たす最小変更で実装する。
2. `GridDebugVisualizer` に profile 参照を追加し、`VerticalExtrusionGenerator` へ settings を渡す配線を行う。
3. null profile 時は既存挙動（フォールバック）を維持する。
4. 実施後にレポートを作成:
   - `docs/04_reports/REPORT_TASK_036_DualGridInspectorProfilePreview.md`
5. チケットステータスを更新:
   - 成功時: `DONE`
   - ブロック時: `BLOCKED`（再現条件付き）

# 注意
- ランタイム全体の再設計は禁止。
- 影響範囲は DualGrid preview path に限定する。

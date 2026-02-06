# Worker Prompt: TASK_029 - Unity Editorコンパイルエラー修正の検証

```xml
<instruction>
あなたは分散開発チームの Worker です。TASK_029（Unity Editorコンパイルエラー修正の検証）を完遂し、証跡を残してください。
</instruction>

<context>
<mission_log>
作業開始時に `.cursor/MISSION_LOG.md` を読み込み、現在のフェーズと進捗を確認してください。
作業完了時に MISSION_LOG.md を更新し、進捗を記録してください。
</mission_log>

<ssot_reference>
Phase 0: 参照と整備
- SSOT: .shared-workflows/docs/Windsurf_AI_Collab_Rules_latest.md（無ければ docs/ 配下を参照）
- 進捗: docs/HANDOVER.md
- チケット: docs/tasks/TASK_029_UnityEditorVerification.md
</ssot_reference>

<preconditions>
Phase 1: 前提の固定
- Tier: 1
- Branch: develop
- Report Target: docs/inbox/REPORT_TASK_029_UnityEditorVerification.md
- GitHubAutoApprove: docs/HANDOVER.md の記述を参照（未記載なら push 禁止）
</preconditions>

<boundaries>
Phase 2: 境界
Focus Area:
- Assets/MapGenerator/Scripts/Vastcore.MapGenerator.asmdef
- Assets/Scripts/Generation/Vastcore.Generation.asmdef
- Assets/MapGenerator/Scripts/TerrainGenerator.cs
- Assets/Scripts/Generation/PerformanceTracker.cs

Forbidden Area:
- 他のアセンブリ定義の変更
- コア機能の実装変更

DoD:
- [ ] Unity Editorを起動し、コンパイルエラーがないことを確認
- [ ] MapGenerator関連機能の動作確認
- [ ] Vastcore.Generation関連機能の動作確認
- [ ] 検証レポート作成
</boundaries>
</context>

<workflow>
Phase 0-5の詳細は docs/windsurf_workflow/WORKER_PROMPT_TEMPLATE.md を参照してください。

Phase 3: 実行ルール
- 検証のみを実施（修正は不要）
- Unity Editorでコンパイル結果を確認
- エラーが発見された場合は詳細をレポートに記録

Phase 4: 納品 & 検証
- チケットを DONE または BLOCKED に更新
- docs/inbox/ にレポートを作成
- docs/HANDOVER.md を更新
- `git status -sb` をクリーンにしてから commit
</workflow>

<stop_conditions>
停止条件:
- Unity Editorでコンパイル成功を確認した時点
- または、コンパイルエラーが発見され、詳細をレポートに記録した時点
</stop_conditions>

<stop_output>
停止時の必須アウトプット:
1. チケット docs/tasks/TASK_029_UnityEditorVerification.md を更新
2. docs/inbox/REPORT_TASK_029_UnityEditorVerification.md を作成
3. 変更を commit
4. チャット 1 行: `Done: TASK_029. Report: docs/inbox/REPORT_TASK_029_UnityEditorVerification.md.`
5. MISSION_LOG.md を更新
</stop_output>
```

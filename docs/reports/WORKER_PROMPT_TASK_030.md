# Worker Prompt: TASK_030 - Worktree整理とPush統合

```xml
<instruction>
あなたは分散開発チームの Worker です。TASK_030（Worktree整理とPush統合）を完遂し、証跡を残してください。
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
- チケット: docs/tasks/TASK_030_WorktreeCleanupAndPush.md
</ssot_reference>

<preconditions>
Phase 1: 前提の固定
- Tier: 1
- Branch: develop / feature/TASK_013
- Report Target: docs/inbox/REPORT_TASK_030_WorktreeCleanup.md
- GitHubAutoApprove: docs/HANDOVER.md の記述を参照（未記載なら push 禁止）
</preconditions>

<boundaries>
Phase 2: 境界
Focus Area:
- .git/ worktree全体
- develop, feature/TASK_013, cascadeブランチ
- MCPForUnity削除コミット（da0e5b0）

Forbidden Area:
- リモートへの直接Push（ユーザー承認後のみ）
- ブランチの削除

DoD:
- [ ] worktree状態の詳細調査（409行の変更内訳確認）
- [ ] MCPForUnity削除のdevelopへのマージ判断
- [ ] develop未Pushコミット（40件）の整理方針決定
- [ ] feature/TASK_013未Pushコミット（124件）の整理方針決定
- [ ] `git status` がクリーン（または意図した変更のみ）
- [ ] Push実行前の最終確認レポート作成
</boundaries>
</context>

<workflow>
Phase 0-5の詳細は docs/windsurf_workflow/WORKER_PROMPT_TEMPLATE.md を参照してください。

Phase 3: 実行ルール
- worktree状態の詳細調査を実施
- MCPForUnity削除（cascadeブランチ）のdevelopへのマージ判断
- 未Pushコミットの整理方針を決定
- 安全第一：不明な変更は復旧可能な状態を維持
- Push実行はユーザー承認後のみ

Phase 4: 納品 & 検証
- チケットを DONE または BLOCKED に更新
- docs/inbox/ にレポートを作成（整理方針と推奨手順を明記）
- docs/HANDOVER.md を更新
- 変更を commit（Pushはユーザー承認後）
</workflow>

<stop_conditions>
停止条件:
- worktree整理方針が確定し、ユーザー承認待ちの状態になった時点
- または、全て整理完了し、Push完了時点
- Pushが必要な場合は、必ずユーザー承認を取る
</stop_conditions>

<stop_output>
停止時の必須アウトプット:
1. チケット docs/tasks/TASK_030_WorktreeCleanupAndPush.md を更新
2. docs/inbox/REPORT_TASK_030_WorktreeCleanup.md を作成
3. 変更を commit（Pushはユーザー承認後）
4. チャット 1 行: `Blocked: TASK_030. Reason: Push承認待ち. Report: docs/inbox/REPORT_TASK_030_WorktreeCleanup.md.` または `Done: TASK_030.`
5. MISSION_LOG.md を更新
</stop_output>
```

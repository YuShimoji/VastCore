# Phase 5: Worker起動用プロンプト生成

## 目的
Worker に渡すプロンプトを生成する。

## 手順
1. 各チケットごとに Worker プロンプトを生成
2. `docs/windsurf_workflow/WORKER_PROMPT_TEMPLATE.md` をベースに動的生成
3. MISSION_LOG.md を更新（Phase 5 完了を記録）

## プロンプトに必ず含める項目
- チケットパス
- Tier / Branch
- Focus Area / Forbidden Area
- 停止条件
- 納品先: `docs/inbox/REPORT_...`
- AI_CONTEXT の背景情報
- MISSION_LOG.md の最新状態

## 出力形式
```text
# Worker Prompt: TASK_XXX_...

## 参照
- チケット: docs/tasks/TASK_XXX_....md
- SSOT: docs/Windsurf_AI_Collab_Rules_latest.md
- HANDOVER: docs/HANDOVER.md

## 境界
- Focus Area: ...
- Forbidden Area: ...

## DoD
- [ ] ...

## 停止条件
- ...

## 納品先
- docs/inbox/REPORT_TASK_XXX_....md
```

## 完了条件
- Worker プロンプトが生成されている

## 次フェーズ
P6（Orchestrator Report）


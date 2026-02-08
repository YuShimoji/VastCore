# AI Context

- 最終更新: 2026-02-06
- 現在のミッション: プロジェクトクリーンアップ完了、次期プラン策定待ち
- ブランチ: main (synced with origin/main)
- 関連: <https://github.com/YuShimoji/VastCore>
- shared-workflows: v3.0 (4ad0a0a)

## 決定事項

- Orchestrator Protocol (P0-P6) を厳格に運用中。
- `MISSION_LOG.md` が進行の SSOT。
- `HANDOVER.md` が成果物の SSOT。
- shared-workflows v3.0 適用済み（.cursorrules, Global Rules 更新）。

## プロジェクト構造

- ルート直下: README.md, CHANGELOG.md, AGENTS.md, AI_CONTEXT.md のみ
- docs/: 01_planning, 02_design, 03_guides, 04_reports, tasks, terrain, windsurf_workflow
- Assets/: MCPForUnity ローカルコピー削除済み（パッケージ参照のみ）
- MapGenerator: asmref/asmdef共存問題解消済み

## タスク管理

### 完了タスク

- [done] TASK_013_DualGridTerrainSystem_Phase1 (2026-01-11)
- [done] TASK_014_MarchingSquaresTerrainSystem_Phase1
- [done] TASK_015_MarchingSquaresTerrainSystem_Phase2
- [done] TASK_016_MarchingSquaresTerrainSystem_Phase3
- [done] TASK_019_FixSwDoctorRulesConfig (2026-01-30)
- [done] TASK_022_FixCyclicDependencies (2026-01-29)
- [done] TASK_023_MergeConflictResolution (2026-01-22)
- [done] TASK_030_WorktreeCleanupAndPush (2026-02-02)
- [done] TASK_031_MCPForUnity重複解消 (2026-02-06)
- [done] TASK_032_MapGeneratorアセンブリ定義整理 (2026-02-06)

### アクティブタスク

- [recheck] TASK_029_UnityEditorVerification (ブロッカー解消済み、再検証待ち)
- [blocked] TASK_021_MergeIntegrationCheck (Tests failing to run in batchmode)

### 短期（Next）

- [pending] TASK_029 再検証: Unity Editor コンパイル確認
- [pending] TASK_026: 3D Voxel Terrain Phase 1

### 中期

- 3D Voxel Terrain Hybrid System (Phase 1-5)
- Deform System 統合

### 長期

- 2D/3D統合、パフォーマンス最適化

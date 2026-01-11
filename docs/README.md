# Vastcore Documentation

標準ディレクトリ構成:

- 01_planning/  計画・優先度・ロードマップ
- 02_design/    設計仕様・引き継ぎ文書
- 03_guides/    セットアップ・運用ガイド
- 04_reports/   レポート・検証記録
- Spec/         技術仕様書
- design/       設計書
- tasks/        タスクチケット
- inbox/        一時レポート

## 主要システムドキュメント

### 地形生成システム

- **2Dハイトマップシステム**: `docs/terrain/TerrainGenerationV0_Spec.md`
  - 既存の2Dハイトマップ地形生成システム
  - `TerrainGenerator`, `HeightMapGenerator` の仕様

- **Dual Grid Terrain System**: `docs/Spec/DualGridTerrainSystem_Spec.md`
  - 不規則グリッドベースの3D地形生成システム（Townscaper風）
  - Phase 1実装完了（2026-01-11）
  - 統合設計: `docs/design/DualGridTerrainSystem_Integration_Design.md`

- **3Dボクセル地形システム（バックログ）**: `docs/tasks/BACKLOG_3D_VoxelTerrain_HybridSystem.md`
  - 将来的な3Dボクセル地形システムの設計

### タスク管理

- **完了タスク**: `docs/tasks/TASK_010_*.md`, `TASK_011_*.md`, `TASK_012_*.md`, `TASK_013_*.md`
- **進行中タスク**: `docs/tasks/` を参照

### 進捗管理

- **HANDOVER**: `docs/HANDOVER.md` - プロジェクトの進捗と状態
- **MISSION_LOG**: `.cursor/MISSION_LOG.md` - 作業ログ

当面は段階的に移行し、重複や旧パス（Documentation/）参照は順次解消します。

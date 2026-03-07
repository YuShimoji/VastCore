# VastCore Documentation

> **最上位SSOT**: [SSOT_WORLD.md](SSOT_WORLD.md) — プロジェクト全体の目的・構造・優先順位の最終権威
> **全ドキュメント索引**: [DOCS_INDEX.md](DOCS_INDEX.md)
> **アーキテクチャ概観**: [ARCHITECTURE.md](ARCHITECTURE.md)

## プロジェクト概要

VastCore は Unity 6 ベースの GPU 加速テレインエンジン。6バイオーム、LOD、マネージャーアーキテクチャを備える。現在は **Phase C**（Deform統合 + CSG検証）実行中。

- 環境: Unity 6000.3.3f1 (URP) / C# (.NET Standard 2.1)
- バージョン: v1.0.0 安定版
- 構成: 278 C# ファイル、21 アセンブリ定義、75 EditMode テスト

## ディレクトリ構成

- **01_planning/** - 計画・優先度・ロードマップ
- **02_design/** - 設計仕様・引き継ぎ文書
- **03_guides/** - セットアップ・運用ガイド
- **04_reports/** - レポート・検証記録
- **tasks/** - タスクチケット（[TASK_INDEX.md](tasks/TASK_INDEX.md) で一覧管理）
- **inbox/** - 一時レポート
- **terrain/** - 地形システム仕様
- **EXAMPLES/** - サンプルコード・テンプレート

詳細な索引は [DOCS_INDEX.md](DOCS_INDEX.md) を参照。

## 主要ドキュメント

### SSOT 階層

- **SSOT_WORLD.md** - 最上位SSOT（プロジェクト全体の目的・構造・優先順位）
- **WORKFLOW_STATE_SSOT.md** - 実行状態SSOT（現在の作業状況）
- **HANDOVER.md** - 成果物SSOT（完了タスク・進捗状況）
- **CLAUDE.md** - セッション開始時の文脈（PROJECT CONTEXT / DECISION LOG）

### アーキテクチャ

- **ARCHITECTURE.md** - アーキテクチャ概観
- **02_design/ASSEMBLY_ARCHITECTURE.md** - 21 アセンブリ定義の詳細
- **02_design/PHASE_C_SCOPE_DEFINITION.md** - Phase C スコープ定義

### 地形システム

- **2D ハイトマップシステム**: `terrain/TerrainGenerationV0_Spec.md`
  - `TerrainGenerator`, `HeightMapGenerator` の仕様
- **Dual Grid Terrain System**: `02_design/DualGridTerrainSystem_Spec.md`
  - 不規則グリッドベース3D地形生成（Townscaper風）
  - Phase 1 実装完了（2026-01-11）
- **3D ボクセル地形システム（バックログ）**: `tasks/BACKLOG_3D_VoxelTerrain_HybridSystem.md`

### Deform 統合

- **03_guides/Deform_Usage_Documentation.md** - Deform パッケージ統合仕様（PC-1 完了）
- **03_guides/COMPILATION_GUARD_PROTOCOL.md** - versionDefines パターン

### 開発ガイド

- **03_guides/UNITY_CODE_STANDARDS.md** - コーディング規約
- **03_guides/REDEVELOPMENT_LOCAL_SETUP.md** - ローカル環境セットアップ
- **03_guides/TERRAIN_VERTICAL_SLICE_RUNBOOK.md** - Vertical Slice 実行手順

## タスク管理

すべてのタスクは [tasks/TASK_INDEX.md](tasks/TASK_INDEX.md) で一覧管理。

- **39 タスク** (34 完了 / 0 オープン / 5+2 レガシー)
- **Phase C 完了**: PC-1 (Deform統合), PC-2 (CSG Blend), PC-3 (StructureGenerator), PC-4 (Lint整理)
- **ブロック中**: PC-5 (GameManager -- TerrainGenerator クラス未存在)

## 進捗状況

最新の進捗は [HANDOVER.md](HANDOVER.md) を参照。

**直近の成果**:
- (2026-03-08) レガシーファイル一掃: AI_CONTEXT.md, .cursor/, WORKER_PROMPT_*, ORCHESTRATION_PROMPT 等 33件削除。用語統一（Orchestrator/Worker -> リードエージェント/サブエージェント）
- (2026-03-08) PC-2 CSG Blend 4モード実装、PC-3 Arch/Pyramid + GlobalSettings Save/Load 実装
- (2026-03-07) ドキュメント負債一掃（8コミット、219ファイル、約15,000行削除）

## Spec Viewer

仕様ドキュメントをブラウザで閲覧可能: [spec-viewer.html](spec-viewer.html)

- **21 仕様エントリ** を spec-index.json で管理
- ステータス・実装率・カテゴリで絞り込み可能

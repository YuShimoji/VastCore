# VastCore — ドキュメント索引

> **上位SSOT**: [SSOT_WORLD.md](SSOT_WORLD.md)

**最終更新**: 2026-02-14

---

## SSOT 優先順位

矛盾がある場合、上位を優先する。

| 優先度 | ドキュメント | スコープ |
|--------|------------|---------|
| 1 (最上位) | [SSOT_WORLD.md](SSOT_WORLD.md) | プロジェクト全体の目的・構造・優先順位 |
| 2 | [DEVELOPMENT_ROADMAP_2026.md](01_planning/DEVELOPMENT_ROADMAP_2026.md) | 開発ロードマップ（Phase A-E） |
| 3 | [EVERY_SESSION.md](windsurf_workflow/EVERY_SESSION.md) | セッション運用プロトコル |
| 4 | [HANDOVER.md](HANDOVER.md) | フェーズ完了と成果物引き継ぎ |
| 5 | [TASK_*.md](tasks/) | 個別実装チケット |

---

## docs/ ディレクトリ構成

### ルート直下

| ファイル | 役割 |
|---------|------|
| [SSOT_WORLD.md](SSOT_WORLD.md) | 最上位仕様（憲法） |
| [ARCHITECTURE.md](ARCHITECTURE.md) | モジュール/依存/責務の鳥瞰 |
| [DOCS_INDEX.md](DOCS_INDEX.md) | 本索引 |
| [HANDOVER.md](HANDOVER.md) | 成果物SSOT・フェーズ完了状態 |
| [README.md](README.md) | docs/ ディレクトリガイド |
| [CONTRIBUTING.md](CONTRIBUTING.md) | コントリビューションルール |

### 01_planning/ — 計画・ロードマップ

| ファイル | 役割 |
|---------|------|
| [DEVELOPMENT_ROADMAP_2026.md](01_planning/DEVELOPMENT_ROADMAP_2026.md) | ロードマップ正本（Phase A-E） |
| [ROADMAP.md](01_planning/ROADMAP.md) | ロードマップ導線（サマリー＋リンク） |
| [DEV_PLAN_ARCHIVE_2025-01.md](01_planning/DEV_PLAN_ARCHIVE_2025-01.md) | 目的関数のアーカイブ（2025年1月版） |
| [DOCUMENTATION_CLEANUP_PLAN.md](01_planning/DOCUMENTATION_CLEANUP_PLAN.md) | 表現統一・検証ゲート規約 |
| [PHASE_A_DEPENDENCY_MAP.md](01_planning/PHASE_A_DEPENDENCY_MAP.md) | Phase A タスク依存関係マップ |
| [TERRAIN_VERTICAL_SLICE_ROADMAP.md](01_planning/TERRAIN_VERTICAL_SLICE_ROADMAP.md) | 地形バーティカルスライス計画 |
| [TASK_PRIORITIZATION.md](01_planning/TASK_PRIORITIZATION.md) | タスク優先度マトリクス |
| [REFACTORING_PLAN.md](01_planning/REFACTORING_PLAN.md) | リファクタリング計画 |
| [REFACTORING_ACTION_PLAN.md](01_planning/REFACTORING_ACTION_PLAN.md) | リファクタリング実行計画 |
| [PROJECT_RESTRUCTURE_PLAN.md](01_planning/PROJECT_RESTRUCTURE_PLAN.md) | プロジェクト再構成計画 |

### 02_design/ — 設計仕様

| ファイル | 役割 |
|---------|------|
| [DualGridTerrainSystem_Spec.md](02_design/DualGridTerrainSystem_Spec.md) | DualGrid 実装仕様 |
| [MarchingSquaresTerrainSystem_Spec.md](02_design/MarchingSquaresTerrainSystem_Spec.md) | MarchingSquares 実装仕様 |
| [DUALGRID_HEIGHTMAP_PROFILE_MAPPING_SPEC.md](02_design/DUALGRID_HEIGHTMAP_PROFILE_MAPPING_SPEC.md) | DualGrid ハイトマップ統合仕様 |
| [TERRAIN_ALGORITHM_NOTES_DUALGRID_HEIGHTMAP.md](02_design/TERRAIN_ALGORITHM_NOTES_DUALGRID_HEIGHTMAP.md) | 地形アルゴリズムノート |
| [ADVANCED_STRUCTURE_DESIGN_DOCUMENT.md](02_design/ADVANCED_STRUCTURE_DESIGN_DOCUMENT.md) | 6段階構造物生成設計（歴史資料） |
| [REFACTORING_HANDOVER_DOCUMENT.md](02_design/REFACTORING_HANDOVER_DOCUMENT.md) | リファクタリング引き継ぎ |
| [RANDOMCONTROL_UI_DESIGN.md](02_design/RANDOMCONTROL_UI_DESIGN.md) | RandomControl UI 設計 |
| [PHASE3_DEFORM_TECHNICAL_INVESTIGATION.md](02_design/PHASE3_DEFORM_TECHNICAL_INVESTIGATION.md) | Deform 技術調査 |

### 03_guides/ — ガイド・手順書

| ファイル | 役割 |
|---------|------|
| [REDEVELOPMENT_LOCAL_SETUP.md](03_guides/REDEVELOPMENT_LOCAL_SETUP.md) | ローカル環境セットアップ |
| [GIT_SETUP_GUIDE.md](03_guides/GIT_SETUP_GUIDE.md) | Git ワークフロー |
| [TERRAIN_VERTICAL_SLICE_RUNBOOK.md](03_guides/TERRAIN_VERTICAL_SLICE_RUNBOOK.md) | 地形バーティカルスライス手順書 |
| [DEVELOPMENT_PROTOCOL.md](03_guides/DEVELOPMENT_PROTOCOL.md) | 開発プロトコル |
| [StructureGenerator_JA.md](03_guides/StructureGenerator_JA.md) | StructureGenerator 日本語ガイド |
| [Deform_Usage_Documentation.md](03_guides/Deform_Usage_Documentation.md) | Deform 使用方法 |

### 04_reports/ — レポート・検証記録

スプリントレポート、タスクレポート、コンパイル/依存性分析、UI移行記録、オーケストレータレポート等。60+ ファイル。

### tasks/ — タスクチケット

| ファイル | 役割 |
|---------|------|
| TASK_PA-1 ~ PA-5 | Phase A タスクチケット |
| TASK_010 ~ 015 | 地形システム関連タスク |
| TASK_019, 031, 032 | 個別修正タスク |
| BACKLOG_3D_VoxelTerrain_HybridSystem.md | 将来仕様 |

### windsurf_workflow/ — ワークフロー・運用

| ファイル | 役割 |
|---------|------|
| [EVERY_SESSION.md](windsurf_workflow/EVERY_SESSION.md) | 運用SSOT（毎回の実行手順） |
| [ORCHESTRATOR_PROTOCOL.md](windsurf_workflow/ORCHESTRATOR_PROTOCOL.md) | オーケストレータプロトコル |
| [OPERATIONS_RUNBOOK.md](windsurf_workflow/OPERATIONS_RUNBOOK.md) | 運用手順書 |
| [OPEN_HERE.md](windsurf_workflow/OPEN_HERE.md) | エントリポイント |

### その他

| ディレクトリ | 役割 |
|------------|------|
| terrain/ | 地形システム固有ドキュメント |
| inbox/ | 一時レポート・受信箱 |
| EXAMPLES/ | Mermaid 図テンプレート |

---

## 検証手順 (DOCUMENTATION_CLEANUP_PLAN 準拠)

### プレースホルダ/不適切表現チェック

```bash
# docs/ 配下で以下パターンが 0件 であること
grep -rn --include="*.md" -E "([0-9]{4}-XX-XX|[0-9]{4}-[0-9]{2}-XX|重大修正|仕様外実装)" docs/

# 許容: DOCUMENTATION_CLEANUP_PLAN.md 内の引用のみ
```

### SSOT 参照整合性チェック

```bash
# SSOT_WORLD への参照が主要ドキュメントに存在すること
grep -rn --include="*.md" "SSOT_WORLD" docs/

# 期待: ARCHITECTURE.md, ROADMAP.md, DEVELOPMENT_ROADMAP_2026.md,
#        EVERY_SESSION.md, HANDOVER.md, DOCS_INDEX.md, README.md に参照あり
```

### 転送ページチェック

```bash
# Documentation/ 配下が全て転送ページであること
grep -rn --include="*.md" "This doc moved to" Documentation/
```

### 新規ドキュメント追加時のルール

1. `DOCS_INDEX.md` に必ず追記する
2. SSOT 階層内での位置を明示する
3. 上位SSOT への逆リンクを先頭に記載する

---

**参照**: [SSOT_WORLD.md](SSOT_WORLD.md) | [ARCHITECTURE.md](ARCHITECTURE.md) | [DOCUMENTATION_CLEANUP_PLAN.md](01_planning/DOCUMENTATION_CLEANUP_PLAN.md)

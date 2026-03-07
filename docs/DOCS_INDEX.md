# VastCore — ドキュメント索引

> **上位SSOT**: [SSOT_WORLD.md](SSOT_WORLD.md)

**最終更新**: 2026-03-07

---

## SSOT 優先順位

矛盾がある場合、上位を優先する。

| 優先度 | ドキュメント | スコープ |
|--------|------------|---------|
| 1 (最上位) | [SSOT_WORLD.md](SSOT_WORLD.md) | プロジェクト全体の目的・構造・優先順位 |
| 2 | [DEVELOPMENT_ROADMAP_2026.md](01_planning/DEVELOPMENT_ROADMAP_2026.md) | 開発ロードマップ（Phase A-E） |
| 3 | [CLAUDE.md](../CLAUDE.md) | セッション運用プロトコル |
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

### 01_planning/ — 計画・ロードマップ (19 files)

| ファイル | 役割 |
|---------|------|
| [DEVELOPMENT_ROADMAP_2026.md](01_planning/DEVELOPMENT_ROADMAP_2026.md) | ロードマップ正本（Phase A-E） |
| [ROADMAP.md](01_planning/ROADMAP.md) | ロードマップ導線（サマリー＋リンク） |
| [DEV_PLAN.md](01_planning/DEV_PLAN.md) | 開発計画（現行） |
| [DEV_PLAN_ARCHIVE_2025-01.md](01_planning/DEV_PLAN_ARCHIVE_2025-01.md) | 目的関数のアーカイブ（2025年1月版） |
| [DOCUMENTATION_CLEANUP_PLAN.md](01_planning/DOCUMENTATION_CLEANUP_PLAN.md) | 表現統一・検証ゲート規約 |
| [PHASE_A_DEPENDENCY_MAP.md](01_planning/PHASE_A_DEPENDENCY_MAP.md) | Phase A タスク依存関係マップ |
| [TERRAIN_VERTICAL_SLICE_ROADMAP.md](01_planning/TERRAIN_VERTICAL_SLICE_ROADMAP.md) | 地形バーティカルスライス計画 |
| [TASK_PRIORITIZATION.md](01_planning/TASK_PRIORITIZATION.md) | タスク優先度マトリクス |
| [REFACTORING_PLAN.md](01_planning/REFACTORING_PLAN.md) | リファクタリング計画 |
| [REFACTORING_ACTION_PLAN.md](01_planning/REFACTORING_ACTION_PLAN.md) | リファクタリング実行計画 |
| [PROJECT_RESTRUCTURE_PLAN.md](01_planning/PROJECT_RESTRUCTURE_PLAN.md) | プロジェクト再構成計画 |
| [RESTRUCTURE_PLAN.md](01_planning/RESTRUCTURE_PLAN.md) | 再構成計画（別版） |
| [ISSUES_BACKLOG.md](01_planning/ISSUES_BACKLOG.md) | 課題バックログ |
| [README.md](01_planning/README.md) | 計画ディレクトリガイド |
| [Diagram.md](01_planning/Diagram.md) | 図表定義 |
| [SG1_TEST_VERIFICATION_PLAN.md](01_planning/SG1_TEST_VERIFICATION_PLAN.md) | SG1 テスト検証計画 |
| [SPRINT_PLAN_02.md](01_planning/SPRINT_PLAN_02.md) | スプリント計画 #2 |
| [TEST_PLAN.md](01_planning/TEST_PLAN.md) | テスト計画（全体） |
| [WEB_DEVELOPMENT_ROADMAP.md](01_planning/WEB_DEVELOPMENT_ROADMAP.md) | Web開発ロードマップ |

### 02_design/ — 設計仕様 (17 files)

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
| [Phase15_RuntimeRefactor_Design.md](02_design/Phase15_RuntimeRefactor_Design.md) | Phase 1.5 Runtime リファクタ設計（歴史資料） |
| [LegacyIsolation_Design.md](02_design/LegacyIsolation_Design.md) | レガシー隔離設計（歴史資料） |
| [Phase2_TemplateIntegration_Spec.md](02_design/Phase2_TemplateIntegration_Spec.md) | Phase 2 テンプレート統合仕様（歴史資料） |
| [WorldGenArchitecture.md](02_design/WorldGenArchitecture.md) | WorldGen アーキテクチャ設計（M0-M3） |

### 03_guides/ — ガイド・手順書 (15 files)

| ファイル | 役割 |
|---------|------|
| [REDEVELOPMENT_LOCAL_SETUP.md](03_guides/REDEVELOPMENT_LOCAL_SETUP.md) | ローカル環境セットアップ |
| [GIT_SETUP_GUIDE.md](03_guides/GIT_SETUP_GUIDE.md) | Git ワークフロー |
| [TERRAIN_VERTICAL_SLICE_RUNBOOK.md](03_guides/TERRAIN_VERTICAL_SLICE_RUNBOOK.md) | 地形バーティカルスライス手順書 |
| [DEVELOPMENT_PROTOCOL.md](03_guides/DEVELOPMENT_PROTOCOL.md) | 開発プロトコル |
| [StructureGenerator_JA.md](03_guides/StructureGenerator_JA.md) | StructureGenerator 日本語ガイド |
| [Deform_Usage_Documentation.md](03_guides/Deform_Usage_Documentation.md) | Deform 使用方法 |
| [COMPILATION_GUARD_PROTOCOL.md](03_guides/COMPILATION_GUARD_PROTOCOL.md) | コンパイルガード プロトコル |
| [UNITY_CODE_STANDARDS.md](03_guides/UNITY_CODE_STANDARDS.md) | Unity コーディング標準 |
| [UNITY_CACHE_CLEANUP_GUIDE.md](03_guides/UNITY_CACHE_CLEANUP_GUIDE.md) | Unity キャッシュクリーンアップガイド |
| [CURSOR_WEB_DEVELOPMENT_GUIDE.md](03_guides/CURSOR_WEB_DEVELOPMENT_GUIDE.md) | Cursor Web 開発ガイド |
| [ORCHESTRATION_PROMPT.md](03_guides/ORCHESTRATION_PROMPT.md) | オーケストレーションプロンプト |
| [TERRAIN_OBJECT_GENERATION_SHORTEST_PLAN.md](03_guides/TERRAIN_OBJECT_GENERATION_SHORTEST_PLAN.md) | 地形オブジェクト生成最短計画 |
| [UI_MIGRATION_NOTES.md](03_guides/UI_MIGRATION_NOTES.md) | UI 移行ノート |
| [TASK_034_MANUAL_VALIDATION_CHECKLIST.md](03_guides/TASK_034_MANUAL_VALIDATION_CHECKLIST.md) | Task 034 手動検証チェックリスト |
| [README.md](03_guides/README.md) | ガイドディレクトリガイド |

### 04_reports/ — レポート・検証記録 (21 files)

Terrain Vertical Slice、WorldGen M0-M3、Phase A/B 完了レポート等（2026-03-07整理済み、旧レポートはgit履歴に保存）。

### tasks/ — タスクチケット

| ファイル | 役割 |
|---------|------|
| TASK_PA-1 ~ PA-5 | Phase A タスクチケット |
| TASK_010 ~ 015 | 地形システム関連タスク |
| TASK_019, 031, 032 | 個別修正タスク |
| BACKLOG_3D_VoxelTerrain_HybridSystem.md | 将来仕様 |

### windsurf_workflow/ --- （削除済み）

Windsurf IDE 固有のワークフロー文書。2026-03-07 に削除。セッション運用は CLAUDE.md に移行済み。

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
#        CLAUDE.md, HANDOVER.md, DOCS_INDEX.md, README.md に参照あり
```

### Documentation/ ディレクトリ

転送スタブは全て削除済み（2026-03-07）。残存するのは `Documentation/Concept Arts/` のみ（アートアセット）。

### 新規ドキュメント追加時のルール

1. `DOCS_INDEX.md` に必ず追記する
2. SSOT 階層内での位置を明示する
3. 上位SSOT への逆リンクを先頭に記載する

---

**参照**: [SSOT_WORLD.md](SSOT_WORLD.md) | [ARCHITECTURE.md](ARCHITECTURE.md) | [DOCUMENTATION_CLEANUP_PLAN.md](01_planning/DOCUMENTATION_CLEANUP_PLAN.md)

---

## 2026-02-22 Addendum

New design document added for the volumetric migration skeleton:

- `docs/02_design/WorldGenArchitecture.md`
  - SSOT recipe baseline
  - 4-engine boundaries
  - assembly impact and dependency direction
  - current milestone status (M0-M1 baseline)
- `docs/04_reports/REPORT_TASK_WORLDGEN_M0_M1_20260222.md` (WorldGen migration baseline report)
- `docs/04_reports/REPORT_TASK_WORLDGEN_M2_20260222.md` (M2 graph auto-generation + field burn report)
- `docs/04_reports/REPORT_TASK_WORLDGEN_M2_VISUALIZATION_20260222.md` (M2 graph gizmo visualization + adapter/update-hook report)
- `docs/04_reports/REPORT_TASK_WORLDGEN_M2_EDITOR_OVERLAY_20260222.md` (Graph overlay EditorWindow report)
- `docs/04_reports/REPORT_TASK_WORLDGEN_M3_20260222.md` (M3 mesh extraction/chunking/dirty-region report)
- `docs/04_reports/REPORT_TASK_WORLDGEN_M3_FOLLOW_20260222.md` (M3 follow-up smoothing/seam/allocation report)
- `docs/04_reports/REPORT_TASK_WORLDGEN_M3_RECOMMENDED_20260222.md` (M3 recommended seam-processor decoupling report)
- `docs/04_reports/REPORT_TASK_WORKFLOW_SYNC_20260223.md` (Shared Workflows sync and remote-ready baseline report)

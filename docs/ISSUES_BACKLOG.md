# Issues Backlog

最終更新: 2025-12-04

## 優先度: 高（短期）

### P3-2: DeformerTab 動的パラメータUI実装

- **Status**: Completed (2025-12-04)
- **Description**: DeformIntegrationManager と連携した動的パラメータUIの実装
- **Tasks**:
  - [x] DeformerTab を DeformIntegrationManager.DeformerSettings ベースに整理
  - [x] DeformerType 選択に応じた動的UIフィールド生成
  - [x] アニメーション設定UIの基盤設計
  - [ ] プリセット保存・読み込み機能の設計（将来タスク）

### SG-2: RandomControlTab 手動テストと結果反映

- **Status**: Pending
- **Description**: `docs/SG1_TEST_VERIFICATION_PLAN.md` に沿ったテスト実施と結果反映
- **Tasks**:
  - [ ] Position/Rotation/Scale Randomization テスト
  - [ ] Preview Mode テスト
  - [ ] 実測結果を FUNCTION_TEST_STATUS.md に反映
  - [ ] 改善ポイントのIssue化

## 優先度: 中（中期）

### T4: Phase 3（Deform 統合）設計ドキュメント整備

- **Status**: Pending
- **Description**: Deform パッケージ統合の実戦用ドキュメント作成
- **Tasks**:
  - [ ] `PHASE3_DEFORM_TECHNICAL_INVESTIGATION.md` の整備
  - [ ] Deformer 一覧と既存システムへのマッピング
  - [ ] 適用箇所の優先度決定
  - [ ] ランタイム負荷とエディタ操作性のバランス設計

### T5: 自動テスト・可観測性の強化

- **Status**: Pending
- **Description**: Unity EditMode テストの拡張とカバレッジ向上
- **Tasks**:
  - [ ] Terrain/Primitive まわりのテスト追加
  - [ ] Editor ツールのテスト追加
  - [ ] `FUNCTION_TEST_STATUS.md` の更新
  - [ ] CI への Unity テスト統合案

### T6: Unity MCP 導入 PoC

- **Status**: Backlog
- **Description**: 情報取得専用 MCP の設計と実装
- **Tasks**:
  - [ ] テスト結果 XML の要約取得エンドポイント
  - [ ] asmdef 構造/シーンリストの取得
  - [ ] ドキュメント要約取得

## 優先度: 低（長期）

### Terrain ストリーミングシステム

- **Status**: Backlog
- **Description**: 大規模 Terrain 生成、動的ロード/アンロード、メモリ効率を考慮した疎結合設計
- **Prerequisite**: 基礎実装が安定した後に着手

### uGUI → UITK 移行

- **Status**: Backlog
- **Description**: UI Toolkit への完全移行（別スプリント）
- **Prerequisite**: 現在の uGUI 実装が安定した後に着手

## 完了済み

### T1: UI 移行トラッククローズ

- **Status**: Completed (2025-12-01)
- **Description**: UI 移行 Sprint 02 のドキュメント統合
- **Artifacts**:
  - `docs/UI_MIGRATION_NOTES.md` (A3-2 結果追記)
  - `Documentation/QA/LEGACY_UI_MIGRATION_REPORT.md` (スキャン結果)

### T2: Unity テスト環境の健全化

- **Status**: Completed (2025-12-02)
- **Description**: Unity 6000.2.2f1 でのコンパイルエラー全解決
- **Artifacts**:
  - 条件付きコンパイルガード追加（18ファイル）
  - BiomePresetManager API修正
  - アセンブリ参照追加
  - `COMPILATION_STATUS_REPORT.md`

### T3: Terrain/Primitive 仕様ギャップ分析

- **Status**: Completed (2025-12-03)
- **Description**: 3系統の地形生成システム比較分析
- **Artifacts**:
  - `docs/T3_TERRAIN_GAP_ANALYSIS.md`
  - UnifiedTerrainParams 統合方針案

### P3-1: Deform統合スケルトン実装

- **Status**: Completed (2025-12-03)
- **Description**: DeformerTab/DeformIntegrationManager のスケルトン実装
- **Artifacts**:
  - `Assets/Editor/StructureGenerator/Tabs/Deform/DeformerTab.cs` (条件付きコンパイル対応)
  - `Assets/_Scripts/Integrations/Deform/DeformIntegrationManager.cs` (API実装)

### SG-1: Composition/Random Tab 未テスト機能の検証準備

- **Status**: Completed (2025-12-03)
- **Description**: CompositionTab実装状況調査とRandomControlTabテスト計画作成
- **Artifacts**:
  - `docs/SG1_TEST_VERIFICATION_PLAN.md`
  - `FUNCTION_TEST_STATUS.md` 更新（Composition Tab実態反映）

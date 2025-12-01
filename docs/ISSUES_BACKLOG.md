# Issues Backlog

最終更新: 2025-12-01

## 優先度: 高（短期）

### T2: Unity テスト環境の健全化

- **Status**: Pending
- **Description**: `scripts/run-tests.ps1 -TestMode editmode` が非ゼロ終了コードを返す問題の特定と修正
- **Tasks**:
  - [ ] `artifacts/logs/editmode.log` の解析
  - [ ] テスト失敗原因の特定（設定/環境/テストコード）
  - [ ] 最小修正でグリーンに戻す
  - [ ] `TEST_PLAN.md` に必要条件を明記

### T3: PrimitiveTerrainGenerator vs Terrain V0 仕様のギャップ分析

- **Status**: Pending
- **Description**: `docs/terrain/TerrainGenerationV0_Spec.md` と現在の実装の対応表作成
- **Tasks**:
  - [ ] V0 仕様の要件整理
  - [ ] 現在の `PrimitiveTerrainGenerator` の機能マッピング
  - [ ] Deform 導入時の置き換え対象の特定

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

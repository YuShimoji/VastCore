# Issues Backlog

最終更新: 2025-12-15

## 優先度: 高（短期）

### T4: Terrain統合パラメータ実装

- **Status**: Completed (2025-12-05)
- **Description**: `docs/T3_TERRAIN_GAP_ANALYSIS.md` に基づく3系統地形システムの統合
- **Tasks**:
  - [x] UnifiedTerrainParams 構造体の実装
  - [x] 各ジェネレータへのアダプタ作成
  - [x] 高さパラメータの統一（scale.y / maxHeight / Depth）
  - [x] ノイズパラメータの統一

### SG-2: RandomControlTab 手動テストと結果反映

- **Status**: In Progress (コード完了・暫定テスト済み／本格テスト・最終反映待ち)
- **Description**: `docs/SG1_TEST_VERIFICATION_PLAN.md` に沿ったテスト実施と結果反映
- **Tasks**:
  - [x] 実装状況を FUNCTION_TEST_STATUS.md に反映
  - [x] Position/Rotation/Scale／Preview の軽い手動テスト（基本動作確認）
  - [ ] `docs/SG1_TEST_VERIFICATION_PLAN.md` に沿った網羅的な手動テスト
  - [ ] 改善ポイントのIssue化

## 優先度: 中（中期）

### RC-1: RandomControlTab 高度機能実装

- **Status**: Pending
- **Description**: RandomControlTab の未実装高度機能
- **Tasks**:
  - [ ] Adaptive Random（周囲環境を考慮したランダム化）
  - [ ] Preset Management（プリセット保存・読み込み）
  - [ ] Mesh Deformation（メッシュ頂点レベル変形）

### CT-1: CompositionTab 実装

- **Status**: In Progress (UIスケルトン完了、CSGプロバイダ抽象導入、ProBuilder内蔵CSG(reflection)でUnion成立／CompositionTab側の実動作検証待ち)
- **Description**: CSG演算・合成機能の実装
- **Tasks**:
  - [x] CompositionTab.cs スケルトン作成
  - [x] StructureGeneratorWindowに登録
  - [x] ProBuilder 内蔵 CSG 機能の可用性調査（reflectionスキャンレポート作成）
  - [x] Union/Intersection/Difference（Parabox.CSG版・条件付き）実装
  - [x] ProBuilder内蔵CSG（`Unity.ProBuilder.Csg`）の利用可否を最小PoCで確認
    - 反射で `UnityEngine.ProBuilder.Csg.CSG.Union/Subtract/Intersect` 呼び出し
    - `Model` から `Mesh` / `Material[]` を取り出せることを確認（reflection）
  - [x] CSG依存方針の決定（ProBuilder reflection を第一候補）
  - [x] CSGプロバイダ抽象（ProBuilder internal / Parabox.CSG を reflection で選択）を追加
  - [ ] CompositionTab で Union/Intersection/Difference を実動作確認（結果メッシュ/Undo/元オブジェクトの非表示・削除/複数チェイン）
  - [ ] Blend機能（Layered/Surface/Adaptive/Noise）

### P3-3: Deformer プリセットシステム

- **Status**: Pending
- **Description**: DeformerTab のプリセット保存・読み込み機能
- **Tasks**:
  - [ ] プリセットデータ構造設計
  - [ ] ScriptableObject ベースの保存機構
  - [ ] プリセット選択UI

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

### P3-2: DeformerTab 動的パラメータUI実装

- **Status**: Completed (2025-12-04)
- **Description**: DeformIntegrationManager と連携した動的パラメータUIの実装
- **Artifacts**:
  - `DrawDynamicDeformerParameters()` メソッド（8種Deformer対応）
  - アニメーション設定UI基盤
  - DeformIntegrationManager を Vastcore.Generation 名前空間に移動

### SG-1: Composition/Random Tab 未テスト機能の検証準備

- **Status**: Completed (2025-12-03)
- **Description**: CompositionTab実装状況調査とRandomControlTabテスト計画作成
- **Artifacts**:
  - `docs/SG1_TEST_VERIFICATION_PLAN.md`
  - `FUNCTION_TEST_STATUS.md` 更新（Composition Tab実態反映）

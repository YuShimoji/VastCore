# Phase C: Deform + CSG スコープ定義

> 最終更新: 2026-03-08 | ROADMAP 検証済み

## 1. Phase C の位置づけ

Phase A (Terrain Core Stabilization) と Phase B (Test Baseline) の完了を受け、
機能統合と検証に進むフェーズ。

前提条件:
- Phase A: 完了 (PA-1 ~ PA-5)
- Phase B: 完了 (PB-1, PB-2)
- EditMode テスト: 75件 PASS

## 2. タスク一覧 (2026-03-07 検証済み)

| ID | タイトル | サイズ | 状態 | 依存 |
|---|---|---|---|---|
| PC-1 | Deform パッケージ正式統合 | L | **DONE** | PA-1 |
| PC-2 | CompositionTab CSG検証 + Blend実装 | L | **DONE** | PA-2 |
| PC-3 | StructureGenerator 残タスク | M | **DONE** | PC-2 |
| PC-4 | GeologicalFormation Erosion | M | **DONE** | - |
| PC-5 | GameManager TerrainGenerator接続 | M | ブロック | - |

## 3. 完了済みタスク

### PC-1: Deform パッケージ正式統合 (DONE)

- 完了日: 2026-03-07
- 成果: API ギャップ 5件修正、asmdef 6件設定、コンパイルエラー 0
- レポート: `docs/inbox/REPORT_PC-1_DeformIntegration.md`
- 仕様書: `docs/03_guides/Deform_Usage_Documentation.md`

### PC-4: GeologicalFormation Erosion (DONE)

- 完了日: 2026-03-07 (commit 7463332)
- 成果: RockLayerPhysicalProperties として統合
  - 風化効果 (Chemical/Physical/Mixed)
  - 浸食効果 (Fluvial/Coastal/Glacial/Aeolian)
  - 経年変化 (酸化、鉱物変質)
- ROADMAP の HydraulicErosion/ThermalErosion クラス新規作成は行わず、
  RockLayerPhysicalProperties が同等機能を提供する設計で実装済み

## 4. 残タスク詳細

### PC-2: CompositionTab CSG検証 + Blend実装 (DONE)

- 完了日: 2026-03-08
- 成果:
  - Blend 4モード実装 (Layered/Surface/Adaptive/Noise)
  - 頂点ベースメッシュブレンド: ワールド空間変換 → 最近傍頂点マッピング → モード別ファクター計算 → Lerp/Slerp
  - CSG (Union/Intersect/Subtract) は既存実装で動作済み
- 制約: 頂点数が大きく異なるメッシュ同士のブレンドは品質に限界がある

### PC-3: StructureGenerator 残タスク (DONE)

- 完了日: 2026-03-08
- 成果:
  - BasicStructureTab: Arch (半楕円アーチ) + Pyramid (N角錐) の ProBuilderMesh 生成を追加
  - GlobalSettingsTab: EditorPrefs ベース自動保存/復元 + JSON エクスポート/インポート
  - RandomControlTab: 機能的に完成済み (Position/Rotation/Scale ランダム化 + Preview)

### PC-5: GameManager TerrainGenerator接続

**ブロック理由**: TerrainGenerator クラスが存在しない

**必要な作業 (大型)**:
- TerrainGenerator クラスの新規実装
- VastcoreGameManager.cs のスタブ解除 (L25, L155-166, L203-208, L231)
- ComprehensiveSystemTest.cs のスタブ解除 (L30-32, L239-251)

**判断ポイント**: Phase C のスコープとして適切か、Phase D に移動すべきか要検討。
TerrainGenerator の設計はプロジェクト全体のアーキテクチャに影響するため、
仕様策定が先行する必要がある。

## 5. CSG アーキテクチャ (検証済み)

### プロバイダパターン

```
ICsgProvider (interface)
  +-- ProBuilderInternalCsgProvider (リフレクション経由)
  +-- ParaboxCsgProvider (フォールバック)

CsgProviderResolver
  - TryExecuteWithFallback(): ProBuilder → Parabox の順で実行
  - IsAvailable(): 実行時にアセンブリ読み込み状態を確認
```

### 設計判断

- コンパイル時依存なし (リフレクション)
- `HAS_PROBUILDER` versionDefine は PB-2 で削除済み (ProBuilder 5.2.2 < 要件 6.0.0 だったため)
- 実行時の `IsAvailable()` チェックで安全にフォールバック

## 6. 未起票の依存タスク

### PB-5: Core アセンブリ分割

ROADMAP では Phase B タスクとして定義されていたが未起票。
- GeologicalFormationGenerator を Core → Terrain に移動
- DeformPresetLibrary を Core → Generation に移動 (PC-1 で namespace 対応済み)

**現在の判断**: PC-4 が PB-5 なしで完了しているため、急ぎの必要なし。
Core アセンブリの肥大化が問題になった時点で再検討。

## 7. Phase C 完了基準

- [x] Phase 3 (Deform統合) 完了
- [x] CompositionTab CSG + Blend 検証済み
- [x] StructureGenerator 全タブ機能完了
- [ ] GameManager → TerrainGenerator 接続 (PC-5 のスコープ次第)
- [ ] Architecture Health Score: 85

> **LEGACY**: この文書は旧版です。最新版は [SP010_PrefabStampPlacement_Spec.md](SP010_PrefabStampPlacement_Spec.md) を参照。

# SP-014: DualGrid Prefab Stamp Placement (旧版)

- **最終更新日時**: 2026-03-16
- **ステータス**: Superseded by SP-010
- **実装**: SG-1 (単セル配置コア完了)

---

## 概要

DualGrid のセルにデザイナー作成の Prefab を配置するシステム。
ScriptableObject でスタンプテンプレートを定義し、Registry が占有管理とルール適用を担当する。

## アーキテクチャ決定

| 日付 | 決定 | 理由 |
|------|------|------|
| 2026-03-06 | DualGrid+HeightMap+Prefabスタンプを正式アーキテクチャに | デザイナー介入最大化、低コストバリエーション量産 |
| 2026-03-16 | 単セルから開始、マルチセルは次スライス | 最小実装で方向性検証 |
| 2026-03-16 | 占有管理はセルID単位 | Cell.Id で一意識別可能 |

## コンポーネント構成

### PrefabStampDefinition (ScriptableObject)

スタンプテンプレート。Inspector で編集可能。

| フィールド | 型 | 用途 |
|-----------|-----|------|
| Prefab | GameObject | 配置する Prefab |
| DisplayName | string | 表示名 (未設定時はアセット名) |
| RotationMode | StampRotationMode | Fixed / Step90 / Free |
| HeightRule | StampHeightRule | TopOfStack / GroundLevel / SpecificLayer |
| ScaleRange | Vector2 | min/max スケール変動 |
| FootprintOffsets | Vector2Int[] | 相対セルオフセット (空=単セル) |

### StampPlacement (Serializable DTO)

配置済みインスタンスの不変データ。セーブ/ロード対象。

| フィールド | 型 | 用途 |
|-----------|-----|------|
| PlacementId | int | 一意識別子 (自動採番) |
| Definition | PrefabStampDefinition | テンプレート参照 |
| AnchorCellId | int | 配置先セルID |
| AnchorHexQ/R | int | Hex座標 (シリアライズ冗長化) |
| Rotation | float | Y軸回転 (度) |
| Layer | int | 垂直レイヤーインデックス |
| Scale | float | 均一スケール |

### StampRegistry (占有管理 + ライフサイクル)

- OccupancyMap: CellId → PlacementId の高速占有チェック
- CanPlace: Definition有効性 + セル占有チェック
- Place: HeightRule解決 + 配置作成 + 占有登録
- Remove: 占有解放 + 配置削除
- HeightRule解決: TopOfStack → ColumnStack.GetHeight(), GroundLevel → 0

### PrefabStampPlacer (シーン描画)

- Instantiate: StampPlacement + IrregularGrid → GameObject 生成
- セル中心座標 + Layer * c_LayerHeight (1.0f) で垂直オフセット
- PlacementId → GameObject の Dictionary で管理

## テストカバレッジ

16テスト (SG-1 で追加):
- Registry: 空、CanPlace検証、配置/占有/二重配置ブロック/削除/取得/クリア
- Placement: セルデータ保持
- HeightRule: TopOfStack レイヤー解決
- Definition: IsValid、IsSingleCell、回転モード

## 残作業 (次スライス)

- [x] マルチセルフットプリント: FootprintOffsets による複数セル占有 — SG-2 完了
- [x] 占有チェックのマルチセル対応 (全オフセットセルの空き確認) — SG-2 完了
- [ ] Gizmo可視化の拡張 (フットプリント境界表示)
- [ ] Unity実機検証 (コンパイル + Gizmo目視)
- [ ] セーブ/ロード対応

## ファイル一覧

| ファイル | アセンブリ |
|----------|-----------|
| Assets/Scripts/Terrain/DualGrid/PrefabStampDefinition.cs | VastCore.Terrain |
| Assets/Scripts/Terrain/DualGrid/PrefabStampPlacer.cs | VastCore.Terrain |
| Assets/Scripts/Terrain/DualGrid/StampPlacement.cs | VastCore.Terrain |
| Assets/Scripts/Terrain/DualGrid/StampRegistry.cs | VastCore.Terrain |
| Assets/Tests/EditMode/PrefabStampTests.cs | VastCore.Tests.EditMode |

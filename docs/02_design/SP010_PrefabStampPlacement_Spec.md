# SP-010: DualGrid Prefab Stamp Placement

**Status**: partial (90%)
**Category**: core
**Last Updated**: 2026-03-17

---

## 概要

DualGrid セルに対してデザイナー定義の Prefab を配置するシステム。
ScriptableObject による宣言的な配置ルール定義、セルID 単位の占有管理、
単一セルおよびマルチセル（フットプリント）配置に対応する。

## 目的

- デザイナーが Prefab を DualGrid 上に配置できる仕組みを提供する
- 配置の占有管理により重複配置を防止する
- 単一セルから複数ヘックスにまたがる大型構造物まで対応する
- 将来のバリエーションエンジン・WFC との接続基盤となる

## アーキテクチャ

```
PrefabStampDefinition (ScriptableObject)
  配置ルール: Prefab参照、回転モード、高さルール、スケール範囲、フットプリント
    │
    ▼
StampRegistry (Pure C#)
  占有管理: CanPlace → Place → Remove
  占有マップ: Dictionary<cellId, placementId>
    │
    ▼
StampPlacement (Serializable Data)
  配置データ: アンカーセル座標、回転、レイヤー、スケール、占有セルID群
    │
    ▼
PrefabStampPlacer (MonoBehaviour Helper)
  GameObject 実体化: StampPlacement + Grid → Instantiate/Destroy
```

## コンポーネント

### PrefabStampDefinition

**ファイル**: `Assets/Scripts/Terrain/DualGrid/PrefabStampDefinition.cs`
**種別**: ScriptableObject (`CreateAssetMenu: Vastcore/Terrain/Prefab Stamp Definition`)

| フィールド | 型 | 既定値 | 説明 |
|---|---|---|---|
| m_Prefab | GameObject | null | 配置する Prefab |
| m_DisplayName | string | "" | 表示名 (空の場合はアセット名) |
| m_RotationMode | StampRotationMode | Step90 | 回転モード |
| m_HeightRule | StampHeightRule | TopOfStack | 高さ配置ルール |
| m_ScaleRange | Vector2 | (0.8, 1.2) | スケールバリエーション範囲 |
| m_FootprintOffsets | Vector2Int[] | 空配列 | マルチセル用 Hex オフセット |

#### StampRotationMode

| 値 | 動作 |
|---|---|
| Fixed | 回転なし (0度固定) |
| Step90 | 90度刻みランダム (0/90/180/270) |
| Free | 0-360度の自由回転 |

#### StampHeightRule

| 値 | 動作 |
|---|---|
| TopOfStack | ColumnStack の最上層に配置 |
| GroundLevel | レイヤー0 (地面) に配置 |
| SpecificLayer | 指定レイヤーに配置 (将来拡張) |

#### フットプリント

- `FootprintOffsets` が空 → 単一セル (`IsSingleCell = true`)
- `FootprintOffsets` が非空 → マルチセル。各要素は (dq, dr) の Hex Axial オフセット
- アンカーヘックスは暗黙的に含まれる (オフセットに (0,0) を含める必要なし)

### StampPlacement

**ファイル**: `Assets/Scripts/Terrain/DualGrid/StampPlacement.cs`
**種別**: `[System.Serializable]` クラス

| プロパティ | 型 | 説明 |
|---|---|---|
| PlacementId | int | 一意な配置ID |
| Definition | PrefabStampDefinition | スタンプ定義への参照 |
| AnchorCellId | int | アンカーセルの Cell.Id |
| AnchorHexQ / AnchorHexR | int | アンカーの Hex Axial 座標 |
| AnchorSubIndex | int | アンカーのサブセルインデックス (0/1/2) |
| Rotation | float | Y 軸回転 (度) |
| Layer | int | 配置レイヤー (高さ) |
| Scale | float | スケール |
| OccupiedCellIds | int[] | 占有しているセルID群 |

### StampRegistry

**ファイル**: `Assets/Scripts/Terrain/DualGrid/StampRegistry.cs`
**種別**: Pure C# クラス

#### 占有ルール

**単一セルスタンプ** (`IsSingleCell = true`):
- アンカーセル 1 つのみを占有

**マルチセルスタンプ** (`IsSingleCell = false`):
- アンカーヘックスの全サブセル (0, 1, 2) を占有
- 各フットプリントオフセットヘックスの全サブセルを占有
- いずれかのヘックスがグリッド上に存在しない場合、配置不可

#### API

| メソッド | 説明 |
|---|---|
| `IsOccupied(cellId)` | セルが占有されているか |
| `CanPlace(def, cell)` | 単一セル配置可否 (後方互換) |
| `CanPlace(def, cell, grid)` | マルチセル対応配置可否 |
| `Place(def, cell, stack, rot, scale)` | 単一セル配置 (後方互換) |
| `Place(def, cell, stack, rot, scale, grid)` | マルチセル対応配置 |
| `Remove(placementId)` | 配置削除 (全占有セル解放) |
| `GetPlacementAt(cellId)` | 任意の占有セルから配置を取得 |
| `GetPlacementById(placementId)` | ID で配置を取得 |
| `Clear()` | 全配置クリア |

### PrefabStampPlacer

**ファイル**: `Assets/Scripts/Terrain/DualGrid/PrefabStampPlacer.cs`
**種別**: Pure C# クラス

- StampPlacement のアンカーセル位置に GameObject を Instantiate する
- レイヤー高さ: `layer * 1.0f` (GridDebugVisualizer と整合)
- 配置 ID → GameObject のマップを管理
- InstantiateAll で StampRegistry 全体を一括生成

## 座標系

- Hex 座標: Axial (q, r) — `Coordinates.cs` で定義
- サブセル: 各 Hex は 3 つの四角形サブセルに分割 (index 0, 1, 2)
- セル識別: `Cell.Id` (int) — グリッド生成時に一意割当
- ワールド座標: `Coordinates.AxialToWorld3D(q, r)` → `Vector3(x, 0, z)`

## WorldGen/Stamps との責務区分

| 系統 | 名前空間 | 用途 | 操作対象 |
|---|---|---|---|
| WorldGen/Stamps | `Vastcore.WorldGen.Stamps` | SDF (密度場) スタンプ | ボリュメトリック地形の形状変形 |
| DualGrid Stamps | `Vastcore.Terrain.DualGrid` | Prefab 配置スタンプ | DualGrid セル上への GameObject 配置 |

これらは競合しない。WorldGen/Stamps は密度場ベースの地形造形、DualGrid Stamps はグリッド上のオブジェクト配置を担当する。

## 制約と設計判断

| 判断 | 選択肢 | 理由 |
|---|---|---|
| 占有管理はセルID単位 | セルID / Hex座標 / 空間ハッシュ | Cell.Id で一意識別可能 |
| マルチセルはヘックス単位で全サブセル占有 | サブセル単位 / ヘックス単位 | 大型構造物はヘックス全体をカバー |
| フットプリント回転は未対応 | 回転連動 / 固定 | Hex 60度 vs Step90 の不整合。別スライスで対応 |
| 単一セルから開始 | 単セル先行 / マルチセル込み | 最小実装で方向性検証 |

## 実装済み

- [x] SG-1: 単一セル配置 (PrefabStampDefinition, StampPlacement, StampRegistry, PrefabStampPlacer)
- [x] SG-1: GridDebugVisualizer Gizmo 表示
- [x] SG-1: EditMode テスト 16 件
- [x] SG-2: マルチセルフットプリント (StampRegistry 拡張, IrregularGrid hex 検索)
- [x] SG-2: EditMode テスト 8 件追加

## 未実装

- [ ] Unity 実機検証 (コンパイル + Gizmo 目視)
- [ ] Editor Inspector 統合 (SG-3 候補)
- [ ] フットプリント回転連動
- [ ] PrefabStampPlacer のマルチセル重心対応
- [ ] バリエーションエンジンとの接続

## テスト

**ファイル**: `Assets/Tests/EditMode/PrefabStampTests.cs`
**件数**: 24 件 (SG-1: 16 件 + SG-2: 8 件)

- StampRegistry: 空レジストリ、null 検証、配置・占有・二重配置拒否・削除・検索・クリア
- StampPlacement: セルデータ保存、OccupiedCellIds
- PrefabStampDefinition: 有効性、IsSingleCell、回転モード
- マルチセル: 全サブセル占有、重複フットプリント拒否、Remove 全解放、任意セル検索、境界外hex拒否、後方互換、3ヘックスフットプリント

# Dual Grid Terrain System: Technical Specification

**Version:** 0.3 (Post SG-1)
**Target:** Unity / C#
**Concept:** Irregular Grid System (inspired by Townscaper)
**Last Updated:** 2026-03-16

---

## 1. 概要 (Overview)

本システムは、有機的で自然な景観を持つ3D地形をプロシージャル生成するための基盤システムである。従来の「正方形グリッド」の硬さを排除し、かつ「ボロノイ図」のような計算負荷をかけずに、**構造化された不規則性（Structured Irregularity）**を実現する。

### 主な特徴

- **Hex-to-Quad Subdivision**: 六角形グリッドをベースに、全てを四角形セル（Quad）に分割・変換する。
- **Irregularity**: 頂点座標を緩和（Relaxation/Jitter）させることで、トポロジーを変えずに有機的な歪みを作る。
- **Vertical Stacking**: 2Dの不規則グリッドをY軸方向に積層し、高低差のある地形を表現する。
- **Retro Aesthetics**: PS1スタイルのレンダリングに適した、低ポリゴン・歪みのあるテクスチャマッピングを許容・活用する。

---

## 2. アーキテクチャ (Architecture)

### 2.1 コア概念: The Dual Grid

通常、ゲームのグリッドは「タイルの中心」に情報を持たせるが、本システムでは**「交点（Corner/Node）」**に情報を持たせる（Dual Grid方式）。

- **Node (Vertex)**: 物理的な「角」。ここに「陸地か海か」「高さ」などの情報を持つ。
- **Cell (Face)**: 4つのNodeに囲まれた領域。レンダリングの単位となる。

### 2.2 データ構造 (Data Structure)

#### Node クラス

洞窟・オーバーハングを実現するため、垂直方向の接続性を持つデータ構造を採用する。

```csharp
public class Node
{
    public int Id;
    public Vector3 Position; // 緩和処理済みの座標
    
    // 変更: 単純なIsSolidではなく、垂直方向の接続性を持つ
    // これにより「床はあるが天井はない（テラス）」や「床も天井もある（洞窟）」を区別可能
    public bool HasGround;   // 足元に地面があるか
    public bool HasCeiling;  // 頭上に天井があるか
    
    public int HeightIndex;  // 高さの階層インデックス (0, 1, 2...)
}
```

#### Cell クラス

```csharp
public class Cell
{
    public int Id;
    public Node[] Corners = new Node[4]; // 構成する4つの頂点
    public Cell[] Neighbors = new Cell[4]; // 隣接する4つのセル
}
```

#### Coordinate System (Coordinates.cs)

六角形グリッドと四角形グリッドを相互変換するための座標系を定義する。

- 内部的には **Axial Coordinates (q, r)** を使用して六角形を管理する。
- 各六角形は 3つの四角形サブセルを持つため、識別子として `(q, r, index[0-2])` を使用する。

#### Grid Manager (IrregularGrid.cs)

グリッド全体を管理するクラス。

- `GenerateGrid(int radius)`: 指定された半径で六角形を敷き詰める。
- **Nodes (Vertices)**: 四角形の「角（頂点）」のリスト。これらがワールド座標 `(x, 0, z)` を持つ。
- **Cells (Quads)**: 4つのNodeへの参照を持つ四角形データのリスト。
- **Relaxation Logic**: 各Nodeの座標に対し、Perlin Noiseまたはランダムなオフセットを加え、グリッドを不規則に歪ませる処理。ただし、トポロジー（接続関係）は維持する。

#### Vertical Data (ColumnStack.cs)

各セル（Cell）は、高さ方向のデータを持つ。

- `Dictionary<CellID, List<bool>>` または `BitArray` を使用。
- 各レイヤー（階層）において、そのセルが Solid（埋まっている）か Empty（空虚）かを保持する。

---

## 3. 生成アルゴリズム (Generation Pipeline)

地形生成は以下のステップで実行される。

### Step 1: Topology Generation (位相生成)

1. **Hex Grid**: Axial座標 `(q, r)` を用いて六角形グリッドを論理的に生成。
2. **Subdivision**: 各六角形の中心点 `(c)` を定義し、各辺の中点 `(m)` と結ぶことで、1つの六角形を3つの四角形（菱形）に分割する。これにより、全てのセルは「4つの頂点」と「4つの隣接」を持つことが保証される（＝配列として扱いやすい）。

**重要**: 各四角形（Cell）が、正しく4つの頂点（Node）と4つの隣接セル（Neighbor Cells）を取得できるロジックを含めること。

### Step 2: Geometry Relaxation (形状緩和)

1. **Initial Position**: 論理的な六角形の座標に頂点を配置。
2. **Jitter/Noise**: 各Nodeの座標 `(x, z)` にパーリンノイズ、またはランダムなオフセットを加算する。
3. **制約**: セルが裏返らない（凸性を維持する）範囲に留めること。

### Step 3: Vertical Data (スタック処理)

各Nodeに対し、Y軸方向のデータを決定する。

- 2Dノイズマップを参照し、その座標の「地面の高さ」を決定。
- 必要に応じて「空洞（洞窟）」レイヤーを定義。
- `HasGround` / `HasCeiling` フラグを設定。

### Step 4: Content Placement (Prefab Stamp System)

~~Marching Squares によるメッシュ解決~~ → **凍結** (2026-03-06 決定)。
Prefabスタンプ方式に置換。詳細は [SP-014: PREFAB_STAMP_PLACEMENT_SPEC.md](PREFAB_STAMP_PLACEMENT_SPEC.md) を参照。

- デザイナー作成の Prefab を ScriptableObject (PrefabStampDefinition) で定義
- StampRegistry がセル単位の占有管理とルール適用を担当
- PrefabStampPlacer がセル中心座標 + レイヤー高さで GameObject を配置
- HeightRule (TopOfStack / GroundLevel / SpecificLayer) でレイヤー解決

### 3.4 座標変換と無限生成 (Infinite Grid Math)

無限地形を実現するため、以下の座標変換フローを実装する。

1. **World to Hex**: プレイヤーのワールド座標 `(x, z)` を、純粋な六角形グリッドの Axial座標 `(q, r)` に変換する（緩和前の整列グリッドとして計算）。
2. **Chunk Hashing**: `(q, r)` を一定のチャンクサイズ（例: `16 × 16` Hex）で割り、チャンクIDを算出する。
   - `ChunkID = (floor(q / size), floor(r / size))`
3. **Local Offset**: チャンク内でのローカル座標を特定し、その周辺のメッシュのみを生成・アクティブ化する。

---

## 4. 実装ステップ (Implementation Steps)

### Phase 1: データ構造とトポロジー生成

#### Step 1.1: The Grid Topology

純粋なC#クラス（MonoBehaviour継承なし）で `GridTopology` を作成。

- Axial座標系を用いて、隣接関係が正しい「六角形→3分割四角形」のグラフ構造を生成する。
- **重要**: 各四角形（Cell）が、正しく4つの頂点（Node）と4つの隣接セル（Neighbor Cells）を取得できるロジックを含めること。

**実装クラス**:
- `Coordinates.cs`: 座標変換ロジック
- `GridTopology.cs`: グリッドトポロジー生成
- `IrregularGrid.cs`: グリッド管理

#### Step 1.2: Visualization (Debug)

`GridDebugVisualizer` (MonoBehaviour) を作成。

- `OnDrawGizmos` を使用して以下を描画する：
  - **Nodes**: 小さいスフィアで描画。
  - **Edges**: Node同士を結ぶ線を描画（歪んだグリッドを可視化）。
  - **Cells**: セルの中心に色付きの面、またはIDを表示。

**実装クラス**:
- `GridDebugVisualizer.cs`: Gizmos描画

#### Step 1.3: Vertical Extrusion Logic

単純な高さマップ、またはノイズ関数を使用して、各セルに「高さ（何階層までブロックがあるか）」を設定するテストロジックを追加。

- Gizmosで、その高さの分だけワイヤーフレームのボックス（歪んだ柱）を積み上げて表示する。

**実装クラス**:
- `ColumnStack.cs`: 垂直データ管理
- `VerticalExtrusionGenerator.cs`: 高さ生成ロジック

### Phase 2: メッシュ生成とレンダリング

#### Step 2.1: Marching Squares 実装

生成されたグリッドデータに基づき、コーナー（頂点）の状態（0/1）から適切なメッシュを選択して配置するロジック。

#### Step 2.2: メッシュ結合

バッチング処理により、複数のセルを効率的にレンダリングする。

---

## 5. レンダリングとアセット (Rendering & Assets)

### 5.1 アセット要件 (Prefab Stamp 方式)

Prefabスタンプ方式により、メッシュ生成ではなくデザイナー制作のPrefabを配置する。

- **Prefab 設計**: PrefabStampDefinition (ScriptableObject) でテンプレート化。RotationMode / HeightRule / ScaleRange で配置バリエーションを制御
- **Unit Size**: c_LayerHeight = 1.0f を基準。Prefab はこの単位高さに合わせて制作
- **Pivot**: セル中心配置のため、ピボットはモデルの底面中央が望ましい
- **Skirts (スカート処理)**: 隣接セルとの隙間を防ぐため、底面や側面をわずかに拡張、または重なりを持たせる

### 5.2 PS1スタイルへの適応

- **World Space UV**: グリッドが歪むため、Triplanar等のワールド座標ベースマッピングが有効
- **Vertex Snapping**: シェーダーレベルでのピクセルスナップ（オプション）

---

## 6. ナビゲーションとゲームプレイ (Navigation)

### 6.1 移動ロジック

Unity標準のNavMeshやPhysics移動は使用しない（不規則地形でのバグ回避のため）。

- **Graph-based Movement**: キャラクターは「座標」ではなく「現在のCell ID」を持つ。
- **Input**: 入力方向（スティック）に最も近い「隣接Cell」を検索し、そこへ遷移する。
- **Visual Interpolation**: 論理的な移動完了後、見た目上の座標を `Vector3.Lerp` で滑らかに移動させる。

### 6.2 垂直移動 (Stairs)

階段セルは「坂道」ではなく「接続ポイント」として機能する。階段に入ると、キャラクターの高さ（Y座標）は補間アニメーションで上のレイヤーへ移動する。

---

## 7. 制約と最適化 (Constraints & Optimization)

### 7.1 パフォーマンス最適化

- **Avoid GameObject Overhead**: セル一つ一つをGameObjectにしないこと。データは配列やリストで管理し、描画は最終的に Mesh 生成、または `Graphics.DrawMeshInstanced` で行う（Phase 2以降）。現在はデータ構造とGizmos表示に集中する。
- **Modularity**: グリッド生成ロジックと、描画ロジックは分離する。
- **Determinism**: `System.Random` にシード値を与え、常に同じ形状が再現できるようにする。

### 7.2 チャンクシステム

無限地形に対応するため、プレイヤー位置に応じた動的なチャンク生成・破棄システムの実装。

---

## 8. 今後の拡張 (Future Roadmap)

- **マルチセルフットプリント**: FootprintOffsets による複数セル占有。大型構造物の配置 (次スライス)
- **Biome Blending**: 頂点カラー等を使用し、草地・土・石畳などのテクスチャを滑らかにブレンドする
- **WFC (Wave Function Collapse)**: 隣接制約に基づく自動配置。現時点では未実装・未設計 (SG-2 で調査予定)
- **バリエーションエンジン**: スタンプ定義の組み合わせによるバリエーション生成。仕様検討中
- **Climate Visual連携**: QW-A (SP-010) でShader Globalsブリッジ + 季節色調変化

---

## 9. プロジェクト固有要件: "Git" Narrative Integration

本システムは「Gitのバージョン管理」をメタファーとしたゲームプレイをサポートする。

### 9.1 Branching (World State)

- グリッド形状（Topology）は全ブランチ（並行世界）で共有・維持するが、各Nodeの `HasGround` / `HasCeiling` データはブランチごとに切り替え可能にする。
- **効果**: 世界の形状そのものが変わるのではなく、同じ場所でも「ある世界では壁、ある世界では通路」という変化を、メッシュの差し替えのみで低コストに表現できる。

### 9.2 データ構造の拡張

将来的には、各Nodeに「ブランチID」を紐付けることで、複数の並行世界を同時に管理できるようにする。

---

## 10. 実装チェックリスト

### Phase 1: 基盤実装 ✅ 完了 (2026-01-11)

- [x] `Coordinates.cs`: Axial座標系の実装 ✅
  - 実装パス: `Assets/Scripts/Terrain/DualGrid/Coordinates.cs`
  - Axial座標 `(q, r)` とワールド座標 `(x, z)` の相互変換を実装
  - 六角形の6方向の隣接関係を計算する `GetHexNeighbor` を実装
- [x] `GridTopology.cs`: 六角形→四角形分割ロジック ✅
  - 実装パス: `Assets/Scripts/Terrain/DualGrid/GridTopology.cs`
  - 六角形を中心点と各辺の中点で3つの四角形（菱形）に分割
  - 隣接関係を構築する `BuildNeighborRelations` を実装
- [x] `IrregularGrid.cs`: グリッド管理クラス ✅
  - 実装パス: `Assets/Scripts/Terrain/DualGrid/IrregularGrid.cs`
  - `GenerateGrid(int radius)`: 指定された半径で六角形を敷き詰める
  - `ApplyRelaxation(int seed, float jitterAmount, bool usePerlinNoise)`: 形状緩和を適用
  - `ValidateConvexity()`: 凸性を検証（簡易実装）
- [x] `Node.cs`: 頂点データ構造（HasGround/HasCeiling対応） ✅
  - 実装パス: `Assets/Scripts/Terrain/DualGrid/Node.cs`
  - `HasGround`, `HasCeiling`, `HeightIndex` プロパティを実装
  - `IsSolid()` メソッドで固体判定を実装
- [x] `Cell.cs`: セルデータ構造 ✅
  - 実装パス: `Assets/Scripts/Terrain/DualGrid/Cell.cs`
  - 4つのNodeへの参照（`Corners`）を実装
  - 4つの隣接セルへの参照（`Neighbors`）を実装
  - `GetCenter()` メソッドでセルの中心座標を計算
- [x] `ColumnStack.cs`: 垂直データ管理 ✅
  - 実装パス: `Assets/Scripts/Terrain/DualGrid/ColumnStack.cs`
  - `Dictionary<int, List<bool>>` を使用してセルID→高さレイヤーのリストを管理
  - `IsSolid(int cellId, int layer)`, `SetLayer(int cellId, int layer, bool isSolid)`, `GetHeight(int cellId)` を実装
- [x] `GridDebugVisualizer.cs`: Gizmos描画 ✅
  - 実装パス: `Assets/Scripts/Terrain/DualGrid/GridDebugVisualizer.cs`
  - `OnDrawGizmos` を使用して Nodes/Edges/Cells/VerticalStacks を描画
  - Inspectorで各種設定（Grid Radius, Seed, Jitter Amount等）を調整可能
- [x] `VerticalExtrusionGenerator.cs`: 高さ生成ロジック ✅
  - 実装パス: `Assets/Scripts/Terrain/DualGrid/VerticalExtrusionGenerator.cs`
  - `GenerateFromHeightMap`: 高さマップ（Texture2D）を使用して高さを生成
  - `GenerateFromNoise`: ノイズ関数（PerlinNoise）を使用して高さを生成
  - `GenerateFromHeightMapArray`: 2D配列の高さマップを使用して高さを生成

**実装結果の知見**:
- 座標変換ロジックは標準的なAxial座標系を使用し、数学的正確性を確認
- グリッド生成ロジックは半径3のグリッドで約63セルを生成（期待通り）
- Relaxationはシード値による再現性を確認
- 凸性チェックは簡易実装（警告のみ、自動修正は未実装）→ Phase 2で改善予定

### Phase 2: 可視化とデバッグ ✅ 実装完了 (Unity Editor検証待ち)

- [x] Gizmosで歪んだグリッドが画面に表示される ✅
- [x] 高さ方向の積み上げが可視化される ✅
- [x] 座標変換ロジックの動作確認 ✅
- [x] Prefabスタンプの配置可視化 ✅ (GridDebugVisualizer拡張, SG-1)

### Phase 3: コンテンツ配置 (Prefab Stamp) ✅ 単セル完了

~~Marching Squares~~ → **凍結** (2026-03-06)。Prefabスタンプに置換。

- [x] PrefabStampDefinition (ScriptableObject) — テンプレート定義 ✅
- [x] StampPlacement / StampRegistry — 占有管理・配置データ ✅
- [x] PrefabStampPlacer — GameObject 実体化 ✅
- [x] EditMode テスト 16件 ✅
- [ ] マルチセルフットプリント (FootprintOffsets による複数セル占有)
- [ ] Unity Editor でのコンパイル確認 + Gizmo目視

---

## 11. 既存システムとの統合検討

### 11.1 統合方針

既存の2Dハイトマップシステム（`TerrainGenerator` / `HeightMapGenerator`）とDual Grid Systemを統合する方針を検討する。

#### 統合ポイント

1. **TerrainGenerationProfile の拡張**
   - `TerrainGenerationProfile` に `DualGridSettings` を追加
   - 2D Mode / 3D Dual Grid Mode / Hybrid Mode を切替可能に

2. **TerrainGenerationWindow の拡張**
   - `TerrainGenerationWindow` に「3D Dual Grid Mode」タブを追加
   - 既存の2D Modeは維持（v0は2D専用のまま）

3. **高さマップの共有**
   - 既存の `HeightMapGenerator.GenerateFromHeightMap` の結果をDual Grid Systemの高さ生成に活用
   - `VerticalExtrusionGenerator.GenerateFromHeightMap` で既存の高さマップを参照可能

#### 統合設計（将来実装）

```csharp
[System.Serializable]
public class DualGridTerrainSettings
{
    public bool UseDualGridMode = false;
    public int GridRadius = 3;
    public int Seed = 42;
    public float JitterAmount = 0.3f;
    public bool UsePerlinNoise = true;
    public int MaxHeight = 5;
    public bool UseHeightMap = false;
    public Texture2D HeightMap;
    // ... その他
}
```

### 11.2 パフォーマンス最適化方針

Phase 1の実装では、データ構造とGizmos表示に集中しているが、Phase 2以降で以下の最適化を検討する。

#### 最適化項目

1. **Job System / Burst Compiler の活用**
   - グリッド生成ロジックを `IJobParallelFor` で並列化
   - Relaxation処理をBurst Compilerで最適化
   - 大規模グリッド（半径10以上）での性能向上を目指す

2. **メッシュ生成の最適化**
   - Marching Squares の実装時に、Job Systemを使用
   - メッシュ結合処理を `Graphics.DrawMeshInstanced` でバッチング

3. **メモリ効率化**
   - 大規模グリッドでのメモリ使用量を削減
   - チャンクシステムの実装（無限地形対応）

#### 最適化の優先順位

1. **Phase 2**: メッシュ生成の実装（Job System / Burst Compiler を検討）
2. **Phase 3**: チャンクシステムの実装（無限地形対応）
3. **Phase 4**: パフォーマンスプロファイリングと最適化

---

## 12. 参考資料

- Townscaper: 不規則グリッドシステムの参考
- Hexagonal Grids: Red Blob Games の六角形グリッド解説
- Marching Squares: 2Dメタボール等で使用されるアルゴリズム

---

## 13. 実装結果に基づく仕様見直し

### 13.1 Phase 1完了後に判明した制約事項

1. **GridTopology.cs**: 六角形の分割ロジックは簡易実装（将来的に改善の余地あり）
2. **隣接関係**: 六角形の隣接関係を4方向（上下左右）にマッピング（簡易実装）
3. **凸性チェック**: 簡易実装（警告のみ、自動修正は未実装）

### 13.2 SG-1完了後の方針転換 (2026-03-06)

1. **Marching Squares 凍結**: Prefabスタンプ方式に置換。メッシュ自動生成ではなくデザイナーPrefab配置で進める
2. **3D Voxel / Marching Cubes 凍結**: 重すぎる。Prefabスタンプ方針と合わない (TASK_026)
3. **セル検索**: 現在 O(n) 線形検索。大規模グリッドでは空間ハッシュが必要 (Phase D PD-3 で対応予定)
4. **マルチセルフットプリント**: FootprintOffsets フィールドは定義済みだが、占有チェックはアンカーセルのみ (次スライス)

### 13.3 未解決の改善項目

1. **凸性チェックの強化**: 外積による厳密チェック + 自動修正
2. **隣接関係の改善**: 6方向の隣接関係を正確にマッピング
3. **セル検索の高速化**: 空間ハッシュまたは Dictionary<int, Cell> の導入
4. **セーブ/ロード対応**: StampPlacement のシリアライズ統合

---

## 14. ファイル一覧 (2026-03-16 時点)

| ファイル | 行数 | 役割 |
|----------|------|------|
| Node.cs | 83 | 頂点データ (HasGround/HasCeiling/HeightIndex) |
| Cell.cs | 110 | セルデータ (Corners[4]/Neighbors[4]/HexQ,R) |
| Coordinates.cs | 167 | Axial ↔ World座標変換 |
| GridTopology.cs | 219 | Hex→Quad分割ロジック |
| ColumnStack.cs | 158 | 垂直データ管理 |
| IrregularGrid.cs | 174 | グリッド管理 |
| VerticalExtrusionGenerator.cs | 201 | HeightMap/Noise → 高さ生成 |
| PrefabStampDefinition.cs | 155 | スタンプテンプレート (ScriptableObject) |
| StampPlacement.cs | 104 | 配置インスタンスDTO |
| StampRegistry.cs | 210 | 占有管理 + ライフサイクル |
| PrefabStampPlacer.cs | 211 | シーン描画 (GameObject実体化) |
| GridDebugVisualizer.cs | 367 | Gizmo可視化 + テストハーネス |
| **合計** | **2,159** | **12ファイル** |

---

**注意**: この仕様書は実装の進行に合わせて更新される。直近更新: SG-1 (Prefabスタンプ単セル配置) 完了に基づく方針転換を反映 (2026-03-16)。

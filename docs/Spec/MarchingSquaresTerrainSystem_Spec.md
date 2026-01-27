# Marching Squares Terrain System: Technical Specification

**Version:** 1.0  
**Target:** Unity / C#  
**Concept:** Dual Grid (Vertex Data) + Marching Squares + Spline Input + Prefab Placement  
**Last Updated:** 2026-01-12  
**Status:** 仕様確定（実装未着手）

---

## 1. 概要 (Overview)

本システムは、グリッドベースのデータ管理と、事前制作された3Dモデル（アセット）を組み合わせ、有機的かつ整合性の取れた地形を生成する。データ構造にはデュアルグリッド（頂点データ）を採用し、描画決定にはビットマスク（Marching Squares）を用いる。入力インターフェースとしてスプライン曲線をサポートし、直感的なレベルデザインを可能にする。

### 主な特徴

- **Dual Grid (Vertex Data)**: データは「マスの中」ではなく「格子の交点（頂点）」に保持する
- **Marching Squares**: 4頂点の状態から16通りのパターンを判定し、適切なメッシュを選択
- **Spline Input**: UnityのSplineパッケージ等で描かれた曲線を座標データとして取得
- **Prefab Placement**: インデックスに対応する3Dモデルを生成（またはプールから取得）して配置
- **Layer System**: 高さ、バイオーム、道路、建物などのレイヤー構造をサポート

### 既存システムとの関係

- **既存の六角形グリッドシステム**（`DualGridTerrainSystem_Spec.md`）とは別のアプローチ
- 既存の2Dハイトマップシステム（`TerrainGenerator` / `HeightMapGenerator`）と並行運用
- 両システムは「Dual Grid」という共通の概念（頂点データ）を持つが、アプローチが異なる

---

## 2. アーキテクチャ構成

### 2.1 データレイヤー (Grid Data)

マップ全体を管理する2次元配列。Dual Gridの原則: データは「マスの中」ではなく「格子の交点（頂点）」に保持する。

```csharp
// 基本データ構造
bool[,] mapData;  // 各頂点が「埋まっている(True)」か「空(False)」かを保持

// 拡張データ構造（将来実装）
struct GridPoint {
    int Height;      // 高さ (0=海, 1=浜, 2=平地, 5=山頂...)
    int BiomeId;     // 地質 (0=水, 1=砂, 2=草, 3=岩...)
    int RoadId;      // 道路 (0=なし, 1=舗装路, 2=歩道, 3=線路...)
    int BuildingId;  // 建物 (0=なし, 1=家, 2=ビル基礎, 3=壁...)
}
```

### 2.2 入力レイヤー (Input Processing)

#### スプライン入力

UnityのSplineパッケージ等で描かれた曲線を座標データとして取得。

#### ラスタライズ処理

曲線の座標をグリッド座標に変換し、該当する頂点の `mapData` を `True` に書き換える。

```csharp
void RasterizeSpline(Spline spline, float brushRadius) {
    // スプライン全体を細かく走査
    foreach (var point in spline.GetSamplePoints()) {
        // ワールド座標 -> グリッド座標変換
        int gridX = Mathf.RoundToInt(point.x / cellSize);
        int gridY = Mathf.RoundToInt(point.z / cellSize);

        // ブラシ範囲内の頂点を全てONにする
        SetGridDataInRange(gridX, gridY, brushRadius, true);
    }
}
```

### 2.3 描画レイヤー (Visualization)

#### セル走査

グリッドの各セル（4つの頂点に囲まれた四角形）ごとにループ処理を行う。

#### ビットマスク計算

4頂点の状態からインデックス（0～15）を算出。

```csharp
// セル (x, y) の4つの頂点データを取得
int tl = mapData[x, y + 1] ? 1 : 0;  // Top-Left
int tr = mapData[x + 1, y + 1] ? 1 : 0;  // Top-Right
int br = mapData[x + 1, y] ? 1 : 0;  // Bottom-Right
int bl = mapData[x, y] ? 1 : 0;  // Bottom-Left

// ビット演算でインデックス化
int index = (tl << 3) | (tr << 2) | (br << 1) | bl;
```

#### プレハブ配置

インデックスに対応する3Dモデルを生成（またはプールから取得）して配置。

```csharp
void GenerateMap() {
    for (int x = 0; x < width - 1; x++) {
        for (int y = 0; y < height - 1; y++) {
            // ビットマスク計算
            int index = CalculateMarchingSquaresIndex(x, y);
            
            // 配列からプレハブを取得して生成
            if (prefabs[index] != null) {
                Instantiate(prefabs[index], 
                            new Vector3(x * cellSize, 0, y * cellSize), 
                            Quaternion.identity);
            }
        }
    }
}
```

---

## 3. 詳細仕様

### 3.1 グリッドとセルの定義

セル $(x, y)$ は、以下の4つの頂点データを参照する。

- **Top-Left (TL)**: $(x, y+1)$
- **Top-Right (TR)**: $(x+1, y+1)$
- **Bottom-Right (BR)**: $(x+1, y)$
- **Bottom-Left (BL)**: $(x, y)$

### 3.2 ビットマスク計算ロジック

各頂点の状態（On=1, Off=0）を以下のビット順序で整数化する。

$$Index = (TL \times 8) + (TR \times 4) + (BR \times 2) + (BL \times 1)$$

これにより、0（全空）から 15（全埋まり）までの16通りのパターンが得られる。

### 3.3 必要アセットリスト（全16種）

実際には回転で流用可能だが、実装を単純化するため「16個の要素を持つ配列」として管理することを推奨する。

| インデックス | ビット (TL,TR,BR,BL) | 形状の説明 | 必要なメッシュ形状 |
|------------|---------------------|-----------|------------------|
| 0 | 0000 | 何もなし | (Air / Empty) |
| 1 | 0001 | 左下のみ | 外角 (Outer Corner) |
| 2 | 0010 | 右下のみ | 外角 (90度回転) |
| 3 | 0011 | 下半分 | 壁 (Wall) |
| 4 | 0100 | 右上のみ | 外角 (180度回転) |
| 5 | 0101 | 左下と右上 | 対角線 (Diagonal) ※1 |
| 6 | 0110 | 右半分 | 壁 (90度回転) |
| 7 | 0111 | 左上が欠け | 内角 (Inner Corner) |
| 8 | 1000 | 左上のみ | 外角 (270度回転) |
| 9 | 1001 | 左半分 | 壁 (270度回転) |
| 10 | 1010 | 右下と左上 | 対角線 (Diagonal) ※1 |
| 11 | 1011 | 右上が欠け | 内角 (270度回転) |
| 12 | 1100 | 上半分 | 壁 (180度回転) |
| 13 | 1101 | 右下が欠け | 内角 (180度回転) |
| 14 | 1110 | 左下が欠け | 内角 (90度回転) |
| 15 | 1111 | 全て埋まり | 完全に地面 (Solid) |

※1 対角線（Diagonal）の扱い: デュアルグリッドの特性上、5番と10番は「道が交差している」か「壁が接している」か曖昧になるケースです。通常は「埋まっている部分を繋ぐ」モデルを用意します。

---

## 4. 拡張仕様：レイヤー構造による統合マップ生成システム

### 4.1 データ構造の定義（拡張版）

各グリッド頂点（交差点）は、以下の構造体（Struct）を持つ。

```csharp
struct GridPoint {
    int Height;      // 高さ (0=海, 1=浜, 2=平地, 5=山頂...)
    int BiomeId;     // 地質 (0=水, 1=砂, 2=草, 3=岩...)
    int RoadId;      // 道路 (0=なし, 1=舗装路, 2=歩道, 3=線路...)
    int BuildingId;  // 建物 (0=なし, 1=家, 2=ビル基礎, 3=壁...)
}
```

### 4.2 自然地形の作成（Natural Layer）

#### A. 海・砂浜・草原（バイオーム遷移）

ビットマスクだけでは「形」しか分かりません。「隣が海なら、海岸線のモデルを置く」という**遷移（Transition）**が必要です。

**アルゴリズム**: 基本の16パターンに加え、**「隣接バイオーム比較」**を行います。

- 自分＝「砂」、隣＝「海」 → 「砂浜の波打ち際」モデルを選択
- 自分＝「草」、隣＝「砂」 → 「草から土への遷移」モデルを選択

**アセット要件**: 各バイオームにつき、「中心（ソリッド）」と「境界線（エッジ）」のモデルセットを用意します。

#### B. 山・崖（高さマップ）

マーチングキューブのような複雑な計算は不要です。**「段差」**を見ます。

**アルゴリズム**: セルを構成する4頂点の Height を比較します。

- 平地: 4点の高さが同じ → 平らな地面
- 崖: 4点のうち、いくつかが高い → 「崖（Cliff）」モデルを配置
- 坂道: 緩やかに高さが変わる設定なら → 「スロープ」モデルに置換
- 山を作る: 中心が高い同心円状に Height を上げていけば、階段状の山ができます

#### C. 洞窟（Cave）

デュアルグリッドの最大の弱点は「横穴」です。これを解決するには2つのアプローチがあります。

- **簡易版（トンネル・アセット）**: 「崖」モデルの一部として「穴の空いた崖」を用意し、そこに入ると別シーンへ移動、あるいは中が空洞になっているモデルを使う
- **上級版（ボクセルとのハイブリッド）**: 洞窟エリアだけ「グリッドの天井」を定義するレイヤーを追加します（実装コスト高）

**推奨**: 地形の上に、巨大な**「岩山のような中空オブジェクト」**を別配置し、その中を洞窟とするのが最も手軽で高品質です。

### 4.3 人工構造物の作成（Structure Layer）

#### A. 道路・歩道・橋

スプライン入力を使用しますが、地形の高さとの関係でモデルを切り替えます。

- **道路**: 地形に沿って「道路用タイル」を敷き詰めます
- **橋**: スプラインが通る場所の Height が低い（川や谷）場合、**「橋脚付きモデル」**に切り替えます
- **ペイント（白線など）**: モデルに直接描くのではなく、道路モデルの上に**デカール（Decal）**を投影するか、テクスチャのバリエーション（直進用、横断歩道用）で切り替えます

#### B. ビル・家屋・駅

これらは「1マスのデュアルグリッド」では表現しきれません。

- **小型（家屋）**: 1マス、または2x2マスのグリッドが「建物ID」で埋まったら、その中心に家のプレハブをドンと置きます
- **大型（ビル・駅）**: **「モジュラー建築方式」**を使います
  - グリッドが BuildingId で埋め尽くされたエリアに対し、
  - 端（Edge）: 壁や窓のモデル
  - 角（Corner）: 柱のモデル
  - 中（Center）: 床や天井のモデル
  - を配置します。これで、どんな形のビルも自動生成できます

### 4.4 実装マトリクスと必要アセット

網羅的な作成ガイドとして、以下のマトリクスを参考にしてください。

| カテゴリ | 具体物 | 生成ロジック | 必要な3Dモデル（アセット） |
|---------|--------|------------|----------------------|
| 水域 | 海・湖 | Heightが海面以下 | 水面シェーダー付き平面 |
| 境界 | 砂浜・岸壁 | Biome境界判定 | 波打ち際、護岸ブロック |
| 陸地 | 草原・荒野 | Biome基本 | 草・土・岩などの地面タイル |
| 高低差 | 崖・山肌 | Height差分判定 | 垂直な岩肌、斜面 |
| 装飾 | 木・岩・草花 | ランダム（ノイズ） | 木、石（グリッドに依存せず配置） |
| 道路 | 車道・歩道 | スプライン焼込 | 直線、カーブ、T字路、十字路 |
| 特殊路 | 橋・高架 | Height < Road | 橋桁、欄干、橋脚 |
| 建築 | 家・店 | ポイント配置 | 完成された建物プレハブ |
| 大型 | ビル・駅 | 連結ビットマスク | 壁、角、屋根、入り口パーツ |

---

## 5. 実装ステップ (Implementation Steps)

### Phase 1: 基盤実装（プロトタイプ）

#### Step 1.1: データ構造とグリッド管理

- `MarchingSquaresGrid.cs`: グリッドデータ管理クラス
  - `bool[,] mapData`: 各頂点が「埋まっている(True)」か「空(False)」かを保持
  - `int width, int height`: グリッドサイズ
  - `float cellSize`: セルサイズ

#### Step 1.2: Marching Squares アルゴリズム

- `MarchingSquaresCalculator.cs`: ビットマスク計算ロジック
  - `CalculateIndex(int x, int y)`: セルの4頂点からインデックス（0～15）を算出
  - `GetCellCorners(int x, int y)`: セルの4頂点の状態を取得

#### Step 1.3: プレハブ配置

- `MarchingSquaresGenerator.cs`: メッシュ生成・配置ロジック
  - `GameObject[] prefabs`: 16種類のプレハブ配列
  - `GenerateMap()`: 全セルを走査してプレハブを配置
  - オブジェクトプール対応（将来実装）

#### Step 1.4: デバッグ可視化

- `MarchingSquaresDebugVisualizer.cs` (MonoBehaviour): Gizmos描画
  - `OnDrawGizmos`: グリッド、頂点、セルを可視化
  - Inspectorで各種設定を調整可能

### Phase 2: スプライン入力対応

#### Step 2.1: スプラインラスタライズ

- `SplineRasterizer.cs`: スプラインをグリッドに焼き込む
  - `RasterizeSpline(Spline spline, float brushRadius)`: スプラインをグリッド座標に変換
  - `SetGridDataInRange(int gridX, int gridY, float radius, bool value)`: ブラシ範囲内の頂点を設定

#### Step 2.2: Unity Spline Package 統合

- Unity Spline Package を使用してスプラインを取得
- スプライン上の点を一定間隔でサンプリング

### Phase 3: レイヤー構造対応（拡張）

#### Step 3.1: 拡張データ構造

- `GridPoint.cs`: 拡張データ構造（Height, BiomeId, RoadId, BuildingId）
- `MarchingSquaresGrid` を拡張して `GridPoint[,]` をサポート

#### Step 3.2: バイオーム遷移

- `BiomeTransitionCalculator.cs`: 隣接バイオーム比較ロジック
- 境界線モデルの選択ロジック

#### Step 3.3: 高さマップ対応

- `HeightMapProcessor.cs`: 高さマップからHeightを設定
- 崖・スロープモデルの選択ロジック

### Phase 4: 最適化と統合

#### Step 4.1: オブジェクトプール

- プレハブの生成・破棄を最適化
- メモリ効率の向上

#### Step 4.2: 既存システムとの統合

- `TerrainGenerationProfile` に `MarchingSquaresSettings` を追加
- `TerrainGenerationWindow` に「Marching Squares Mode」タブを追加

---

## 6. 開発の進め方（推奨手順）

一度に全て作ると破綻します。以下の順序で実装してください。

1. **地形ベース (Height & Biome)**: まずは「海と陸」「平地と崖」が正しく生成されるシステムを作る（モデルはただの箱でOK）
2. **道路レイヤー (Road)**: 地形の上に「道路」が上書きされる処理を作る。「橋」の判定を入れる
3. **建物レイヤー (Structure)**: 道路以外の場所に「家」が生える処理を入れる
4. **アセット差し替え**: 箱モデルを、ちゃんとした3Dモデル（アセットストア等で購入、または自作）に置き換える

**注意**: この中でも**「バイオームの境界（海と陸のつなぎ目）」と「崖（高低差）」**の処理ロジックが最も複雑になりがちです。

---

## 7. 制約と最適化 (Constraints & Optimization)

### 7.1 パフォーマンス最適化

- **オブジェクトプール**: プレハブの生成・破棄を最適化
- **バッチング**: 同じプレハブを `Graphics.DrawMeshInstanced` でバッチング
- **チャンクシステム**: 大規模マップに対応するため、チャンク単位で生成・破棄

### 7.2 メモリ効率

- `bool[,]` の代わりに `BitArray` を使用（将来実装）
- 拡張データ構造は必要に応じてのみ使用

---

## 8. 既存システムとの統合検討

### 8.1 統合方針

- **並行運用**: 既存の2Dハイトマップシステム、六角形グリッドシステムと並行運用
- **共通インターフェース**: 将来的に `TerrainGenerationProfile` を参照可能にする
- **エディタ統合**: `TerrainGenerationWindow` に「Marching Squares Mode」タブを追加

### 8.2 統合ポイント

1. **TerrainGenerationProfile の拡張**
   - `MarchingSquaresSettings` を追加
   - 2D Mode / 3D Dual Grid Mode / Marching Squares Mode を切替可能に

2. **TerrainGenerationWindow の拡張**
   - 「Marching Squares Mode」タブを追加
   - 既存の2D Mode、3D Dual Grid Modeは維持

3. **高さマップの共有**
   - 既存の `HeightMapGenerator.GenerateFromHeightMap` の結果を活用
   - `HeightMapProcessor` で既存の高さマップを参照可能

---

## 9. 参考資料

- **Marching Squares**: 2Dメタボール等で使用されるアルゴリズム
- **Dual Grid**: 頂点データを保持するグリッドシステム
- **Unity Spline Package**: Unity公式のスプライン編集パッケージ

---

## 10. 実装チェックリスト

### Phase 1: 基盤実装（プロトタイプ）

- [ ] `MarchingSquaresGrid.cs`: グリッドデータ管理クラス
- [ ] `MarchingSquaresCalculator.cs`: ビットマスク計算ロジック
- [ ] `MarchingSquaresGenerator.cs`: メッシュ生成・配置ロジック
- [ ] `MarchingSquaresDebugVisualizer.cs`: Gizmos描画
- [ ] Unity Editor上での動作確認（プリミティブ形状でテスト）

### Phase 2: スプライン入力対応

- [ ] `SplineRasterizer.cs`: スプラインラスタライズ
- [ ] Unity Spline Package 統合
- [ ] スプライン入力の動作確認

### Phase 3: レイヤー構造対応（拡張）

- [ ] `GridPoint.cs`: 拡張データ構造
- [ ] `BiomeTransitionCalculator.cs`: バイオーム遷移
- [ ] `HeightMapProcessor.cs`: 高さマップ対応

### Phase 4: 最適化と統合

- [ ] オブジェクトプール実装
- [ ] 既存システムとの統合
- [ ] パフォーマンス最適化

---

**注意**: この仕様書は実装の進行に合わせて更新される。

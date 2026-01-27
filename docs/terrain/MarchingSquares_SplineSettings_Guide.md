# Marching Squares Terrain System - スプライン設定ガイド

## 概要

このドキュメントでは、Marching Squares Terrain Systemにおけるスプライン入力機能とその設定パラメータについて、実装コードに基づいて詳細に解説します。

---

## 1. スプラインの基本構造

### 1.1 SplineContainerコンポーネント

**役割**: Unity Spline Packageの`SplineContainer`コンポーネントは、1つまたは複数のスプライン曲線を保持するコンテナです。

**実装上の扱い**:
- `MarchingSquaresGenerator`は`SplineContainer`への参照をInspectorで設定します
- `RasterizeFromSpline()`メソッドは、`SplineContainer.Splines`プロパティから全スプラインを取得します
- 複数のスプラインが存在する場合、すべてのスプラインが順次処理されます

```csharp
// MarchingSquaresGenerator.cs より
var splines = m_SplineContainer.Splines;
for (int i = 0; i < splines.Count; i++)
{
    var spline = splines[i];
    // 各スプラインをラスタライズ
}
```

### 1.2 スプラインの座標系

**重要な注意点**: 現在の実装では、スプラインの**ローカル座標**を直接グリッド座標に変換しています。

```csharp
// SplineRasterizer.cs より（89行目）
Vector3 localPoint = SplineUtility.EvaluatePosition(_spline, normalizedT);
Vector2Int gridPos = _grid.WorldToGrid(localPoint);
```

**座標変換の流れ**:
1. `SplineUtility.EvaluatePosition()`: スプライン上の点を**ローカル座標**で取得
2. `WorldToGrid()`: ワールド座標をグリッド座標に変換（XZ平面のみ使用）
   - `x = RoundToInt(worldPos.x / cellSize)`
   - `y = RoundToInt(worldPos.z / cellSize)`
   - **Y座標は無視されます**

**実用的な意味**:
- スプラインのTransform（Position, Rotation, Scale）の影響は**現在考慮されていません**
- スプラインは原点(0, 0, 0)を基準としたローカル座標で処理されます
- グリッドも原点を基準としているため、スプラインとグリッドの原点を一致させる必要があります

---

## 2. 設定パラメータの詳細解説

### 2.1 Brush Radius（ブラシ半径）

**Inspector表示**: `Brush Radius`  
**範囲**: 0.1 ～ 10.0（ワールド座標単位）  
**デフォルト値**: 0.5

**動作の仕組み**:

1. **サンプリングポイントの決定**:
   - スプライン上の各サンプリングポイントを中心として、半径`Brush Radius`の円形範囲内のグリッド頂点を設定します

2. **グリッド座標への変換**:
   ```csharp
   // SplineRasterizer.cs より（142-144行目）
   float cellSize = _grid.CellSize;
   float radiusInGrid = _radius / cellSize;  // グリッド単位に変換
   ```

3. **影響範囲の計算**:
   ```csharp
   // ブラシ範囲内のグリッド座標を計算（147-150行目）
   int minX = Mathf.FloorToInt(_gridX - radiusInGrid);
   int maxX = Mathf.CeilToInt(_gridX + radiusInGrid);
   int minY = Mathf.FloorToInt(_gridY - radiusInGrid);
   int maxY = Mathf.CeilToInt(_gridY + radiusInGrid);
   ```

4. **距離判定**:
   ```csharp
   // ワールド座標での距離計算（163-169行目）
   Vector3 gridWorldPos = _grid.GridToWorld(x, y);
   Vector3 centerWorldPos = _grid.GridToWorld(_gridX, _gridY);
   float distance = Vector3.Distance(gridWorldPos, centerWorldPos);
   
   if (distance <= _radius)  // ブラシ半径内の場合
   {
       _grid.SetVertex(x, y, _value);  // 頂点を設定
   }
   ```

**実用的な意味**:
- **小さい値（0.1～0.3）**: 細い線路や小道のような細いパスを描画
- **中程度の値（0.5～1.0）**: 一般的な道路や川の幅
- **大きい値（2.0～10.0）**: 広いエリアや湖のような大きな領域を描画

**例**: 
- `CellSize = 1.0`、`BrushRadius = 0.5`の場合
  - グリッド単位での半径: `0.5 / 1.0 = 0.5`
  - 約1セル分の範囲に影響
- `CellSize = 0.5`、`BrushRadius = 1.0`の場合
  - グリッド単位での半径: `1.0 / 0.5 = 2.0`
  - 約4セル分の範囲に影響

---

### 2.2 Sampling Interval（サンプリング間隔）

**Inspector表示**: `Sampling Interval`  
**範囲**: 0.01 ～ 1.0（ワールド座標単位）  
**デフォルト値**: 0.1

**動作の仕組み**:

1. **スプラインの長さ取得**:
   ```csharp
   // SplineRasterizer.cs より（72行目）
   float splineLength = _spline.GetLength();
   ```

2. **サンプリングポイントの生成**:
   ```csharp
   // スプライン上の点を一定間隔でサンプリング（82-100行目）
   float currentDistance = 0f;
   while (currentDistance <= splineLength)
   {
       float normalizedT = Mathf.Clamp01(currentDistance / splineLength);
       Vector3 localPoint = SplineUtility.EvaluatePosition(_spline, normalizedT);
       // ... ラスタライズ処理 ...
       currentDistance += _samplingInterval;  // 次のサンプリングポイントへ
   }
   ```

3. **終端の処理**:
   ```csharp
   // 最後の点（スプラインの終端）も処理（102-105行目）
   Vector3 endPoint = SplineUtility.EvaluatePosition(_spline, 1f);
   // ... ラスタライズ処理 ...
   ```

**実用的な意味**:
- **小さい値（0.01～0.05）**: 
  - より多くのサンプリングポイントが生成される
  - スプラインの形状がより正確に反映される
  - **処理時間が長くなる**（特に長いスプラインの場合）
  - カーブの多い複雑なスプラインに適している

- **中程度の値（0.1～0.2）**: 
  - バランスの取れた設定
  - ほとんどの用途で十分な精度
  - デフォルト値として推奨

- **大きい値（0.5～1.0）**: 
  - サンプリングポイントが少なくなる
  - 処理が高速
  - **スプラインの形状が粗くなる可能性がある**
  - 直線的なスプラインや大まかな形状に適している

**例**:
- スプラインの長さが10.0の場合:
  - `SamplingInterval = 0.1`: 約100個のサンプリングポイント
  - `SamplingInterval = 0.5`: 約20個のサンプリングポイント
  - `SamplingInterval = 1.0`: 約10個のサンプリングポイント

---

### 2.3 Auto Generate After Rasterize（ラスタライズ後に自動生成）

**Inspector表示**: `Auto Generate After Rasterize`  
**型**: ブール値（チェックボックス）  
**デフォルト値**: `true`（チェック済み）

**動作の仕組み**:

```csharp
// MarchingSquaresGenerator.cs より
if (m_AutoGenerateAfterRasterize && totalRasterized > 0)
{
    GenerateMap();  // 地形を自動生成
}
```

**実用的な意味**:
- **有効（true）**: 
  - スプラインラスタライズ後、自動的に`GenerateMap()`が呼び出されます
  - グリッドデータの変更が即座に地形に反映されます
  - **推奨設定**: リアルタイムで地形の変化を確認したい場合

- **無効（false）**: 
  - スプラインラスタライズ後、地形は生成されません
  - 手動で`GenerateMap()`を呼び出す必要があります
  - 複数のスプラインを連続してラスタライズしてから、最後に一度だけ地形を生成したい場合に有効

**使用例**:
```csharp
// 複数のスプラインを連続してラスタライズ
generator.m_AutoGenerateAfterRasterize = false;  // 自動生成を無効化
generator.RasterizeFromSpline();  // スプライン1
generator.RasterizeFromSpline();  // スプライン2
generator.RasterizeFromSpline();  // スプライン3
generator.GenerateMap();  // 最後に一度だけ地形を生成
```

---

## 3. 座標系とTransformの関係

### 3.1 現在の実装の制約

**重要な制約**: 現在の実装では、スプラインの**Transform（Position, Rotation, Scale）は考慮されていません**。

**理由**:
- `SplineUtility.EvaluatePosition()`はローカル座標を返します
- このローカル座標を直接`WorldToGrid()`に渡しています
- `SplineContainer`のTransformを考慮した変換は行われていません

**実用的な影響**:
- スプラインのGameObjectのPositionを変更しても、グリッド上の位置は変わりません
- スプラインとグリッドの原点（0, 0, 0）を一致させる必要があります
- スプラインを移動させたい場合は、スプラインの制御点（Knots）を直接編集する必要があります

### 3.2 グリッドの座標系

**グリッドの原点**: 常にワールド座標の(0, 0, 0)  
**座標変換**:
```csharp
// MarchingSquaresGrid.cs より
public Vector3 GridToWorld(int _x, int _y)
{
    return new Vector3(_x * m_CellSize, 0f, _y * m_CellSize);
}

public Vector2Int WorldToGrid(Vector3 _worldPos)
{
    int x = Mathf.RoundToInt(_worldPos.x / m_CellSize);
    int y = Mathf.RoundToInt(_worldPos.z / m_CellSize);
    return new Vector2Int(x, y);
}
```

**注意点**:
- Y座標（高さ）は無視されます
- XZ平面のみが使用されます
- `CellSize`が座標変換の基準となります

---

## 4. 実用的な使用例

### 4.1 細い道路を描画する場合

**設定**:
- `Brush Radius`: 0.3
- `Sampling Interval`: 0.1
- `Auto Generate After Rasterize`: true

**結果**: 細い道路が滑らかに描画されます

### 4.2 広い川を描画する場合

**設定**:
- `Brush Radius`: 2.0
- `Sampling Interval`: 0.2
- `Auto Generate After Rasterize`: true

**結果**: 広い川が描画されます（サンプリング間隔を少し大きくして処理速度を向上）

### 4.3 複雑なカーブを正確に描画する場合

**設定**:
- `Brush Radius`: 0.5
- `Sampling Interval`: 0.05（または0.01）
- `Auto Generate After Rasterize`: true

**結果**: カーブの形状がより正確に反映されます（処理時間は長くなります）

---

## 5. トラブルシューティング

### 5.1 スプラインがグリッドに反映されない

**原因**:
- スプラインのローカル座標がグリッドの範囲外にある可能性
- `SplineContainer`が設定されていない
- スプラインが空（Knotsが0個）

**解決方法**:
- スプラインの制御点を原点(0, 0, 0)付近に配置
- Inspectorで`Spline Container`が正しく設定されているか確認
- スプラインに少なくとも2つのKnotsがあるか確認

### 5.2 ブラシ半径が期待通りに動作しない

**原因**:
- `CellSize`と`BrushRadius`の比率が適切でない可能性
- グリッドのサイズが小さすぎる

**解決方法**:
- `CellSize`を確認し、`BrushRadius`を`CellSize`の倍数で設定することを検討
- グリッドサイズ（`Grid Width`、`Grid Height`）を大きくする

### 5.3 処理が遅い

**原因**:
- `Sampling Interval`が小さすぎる
- `Brush Radius`が大きすぎて、影響範囲が広い
- グリッドサイズが大きすぎる

**解決方法**:
- `Sampling Interval`を0.2～0.5に増やす
- `Brush Radius`を必要最小限に設定
- グリッドサイズを適切な範囲に調整

---

## 6. 今後の改善案

### 6.1 Transform対応

現在の実装では、スプラインのTransformが考慮されていません。将来的には以下の改善が考えられます：

```csharp
// 改善案（未実装）
Vector3 localPoint = SplineUtility.EvaluatePosition(_spline, normalizedT);
Vector3 worldPoint = splineContainer.transform.TransformPoint(localPoint);
Vector2Int gridPos = _grid.WorldToGrid(worldPoint);
```

これにより、スプラインのGameObjectを移動・回転・スケールしても、グリッド上の位置が正しく反映されます。

### 6.2 非等間隔サンプリング

現在は等間隔でサンプリングしていますが、カーブの曲率に応じてサンプリング密度を調整することで、より効率的に処理できます。

---

## まとめ

- **Brush Radius**: スプラインの「太さ」を制御（ワールド座標単位）
- **Sampling Interval**: スプラインの「滑らかさ」と「処理速度」のバランスを制御
- **Auto Generate After Rasterize**: ラスタライズ後の自動地形生成を制御
- **座標系**: 現在はローカル座標を直接使用（Transformは未考慮）
- **グリッド**: XZ平面のみ使用、Y座標は無視

これらの設定を適切に調整することで、様々な地形パターンを効率的に生成できます。

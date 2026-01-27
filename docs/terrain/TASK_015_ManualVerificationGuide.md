# TASK_015 実動作確認ガイド

**タスク**: TASK_015 - Marching Squares Terrain System - Phase 2 実装（スプライン入力対応）  
**作成日**: 2026-01-18  
**目的**: Unity Editor上でスプライン入力機能が正常に動作することを確認する

---

## 前提条件

### 1. Unity Spline Packageのインストール確認

1. Unity Editorを起動
2. **Window** → **Package Manager** を開く
3. **Packages: In Project** を選択
4. **com.unity.splines** がインストールされているか確認
   - インストールされていない場合:
     - **Packages: Unity Registry** に切り替え
     - 検索バーで「Splines」を検索
     - **Splines** パッケージを選択して **Install** をクリック

### 2. コンパイルエラーの確認

1. Unity Editorの **Console** ウィンドウを開く（**Window** → **General** → **Console**）
2. エラーメッセージがないことを確認
3. エラーがある場合は、エラーメッセージを確認して修正

---

## ステップバイステップ実動作確認手順

### Step 1: シーンの準備

1. **新しいシーンを作成**:
   - **File** → **New Scene**
   - **Basic (Built-in)** または **Basic (URP)** を選択

2. **GameObjectを作成**:
   - ヒエラルキーで右クリック → **Create Empty**
   - 名前を `MarchingSquaresTerrain` に変更

3. **MarchingSquaresGeneratorコンポーネントを追加**:
   - `MarchingSquaresTerrain` を選択
   - **Inspector** で **Add Component** をクリック
   - 「Marching Squares Generator」を検索して追加

4. **MarchingSquaresDebugVisualizerコンポーネントを追加**（オプション、推奨）:
   - 同じGameObjectに **Add Component** をクリック
   - 「Marching Squares Debug Visualizer」を検索して追加

### Step 2: グリッド設定

1. **Inspector** で `MarchingSquaresGenerator` コンポーネントを確認

2. **Grid Settings** セクションを設定:
   - **Grid Width**: `20`（推奨: 10～50）
   - **Grid Height**: `20`（推奨: 10～50）
   - **Cell Size**: `1.0`（推奨: 0.5～2.0）

3. **Prefab Settings** セクションを設定:
   - **Prefabs** 配列を展開（16要素）
   - 各要素にプリミティブ形状を設定（Phase 1で使用したもの）:
     - **Element 0**: `GameObject` → **3D Object** → **Cube** を作成し、ドラッグ&ドロップ
     - または、既存のプレハブがあれば使用

### Step 3: Unity Spline Packageの設定

1. **SplineContainerを作成**:
   - **方法A（推奨）**: ヒエラルキーで右クリック → **Spline** → **Square**
   - **方法B**: ヒエラルキーで右クリック → **Create Empty** → 名前を `SplineContainer` に変更 → **Add Component** → 「Spline Container」を検索して追加
   - 名前を `SplineContainer` に変更（オプション）
   - **注意**: Unity Spline Package 2.5.1では、メニュー項目名が「Spline Container」ではなく「Square」になっている場合があります。機能的には同じです。

2. **スプラインを編集**:
   - `SplineContainer` を選択
   - **Scene View** でスプラインの制御点（Knots）を編集:
     - **Spline Tools** パネル（Scene View上部）で **Edit Spline** を選択
     - スプライン上をクリックして制御点を追加
     - 制御点をドラッグして形状を調整
     - **重要**: スプラインの制御点は原点(0, 0, 0)付近に配置する（座標系の制約）

3. **MarchingSquaresGeneratorにSplineContainerを設定**:
   - `MarchingSquaresTerrain` を選択
   - **Inspector** の `MarchingSquaresGenerator` コンポーネントで:
     - **Spline Input Settings** セクションを展開
     - **Spline Container** フィールドに `SplineContainer` をドラッグ&ドロップ

### Step 4: スプライン設定パラメータの調整

1. **Spline Input Settings** セクションで以下を設定:

   - **Brush Radius**: `0.5`（推奨: 0.3～1.0）
     - スプラインの「太さ」を制御
     - 小さい値: 細い道路
     - 大きい値: 広いエリア

   - **Sampling Interval**: `0.1`（推奨: 0.05～0.2）
     - スプラインの「滑らかさ」を制御
     - 小さい値: より正確だが処理が遅い
     - 大きい値: 高速だが粗い

   - **Auto Generate After Rasterize**: `true`（チェック済み）
     - スプラインラスタライズ後に自動的に地形を生成

### Step 5: スプラインラスタライズの実行

1. **Unity AI Toolkitパッケージの問題を回避**（重要）:
   - **問題**: Unity AI Toolkit（`com.unity.ai.assistant`、`com.unity.ai.generators`等）がインストールされている場合、Context Menu実行時にエディタがハングする可能性があります
   - **解決方法**: 
     - **Window** → **Package Manager** を開く
     - **Packages: In Project** を選択
     - 以下のパッケージを削除（Marching Squaresには不要）:
       - `com.unity.ai.assistant`
       - `com.unity.ai.generators`
       - `com.unity.ai.inference`
     - Unity Editorを再起動
   - **詳細**: `docs/terrain/TASK_015_Troubleshooting_Update.md` を参照

2. **Context Menuから実行**（推奨）:
   - `MarchingSquaresTerrain` を選択
   - **Inspector** で `MarchingSquaresGenerator` コンポーネントの右上の **⋮** メニューをクリック
   - **Rasterize From Spline** を選択
   - **注意**: 処理時間が3分以上かかる場合は、Unity AI Toolkitパッケージを削除してください

2. **または、スクリプトから実行**:
   - `MarchingSquaresGenerator` コンポーネントに `RasterizeFromSpline()` メソッドを呼び出すスクリプトを作成

3. **Console** でログを確認:
   - 以下のようなログが表示されることを確認:
     ```
     SplineRasterizer.RasterizeSpline: Rasterized X vertices from spline (length: Y, sampling interval: Z).
     MarchingSquaresGenerator.RasterizeFromSpline: Rasterized X vertices from N spline(s).
     ```

### Step 6: 地形生成の確認

1. **Scene View** で地形が生成されていることを確認:
   - スプラインの形状に対応したプレハブが配置されている
   - プレハブは16種類のパターンに基づいて配置されている

2. **Game View** で確認（オプション）:
   - **Play** ボタンを押して実行
   - 地形が正しく表示されることを確認

3. **Debug Visualizer** で確認（オプション）:
   - `MarchingSquaresDebugVisualizer` コンポーネントが追加されている場合:
     - **Scene View** でGizmosを有効化
     - グリッド、頂点、セルが可視化される
     - スプラインで設定された頂点が色分けされて表示される

### Step 7: 動作確認チェックリスト

以下の項目を確認し、すべて正常であることを確認:

- [ ] Unity Spline Packageがインストールされている
- [ ] コンパイルエラーがない
- [ ] `MarchingSquaresGenerator` コンポーネントが正常に追加されている
- [ ] `SplineContainer` が作成され、`MarchingSquaresGenerator` に設定されている
- [ ] スプラインに制御点（Knots）が2つ以上存在する
- [ ] `RasterizeFromSpline()` を実行した際にConsoleにログが表示される
- [ ] スプラインの形状に対応した地形が生成される
- [ ] プレハブが正しく配置されている（16種類のパターンに対応）
- [ ] `Auto Generate After Rasterize` が有効な場合、ラスタライズ後に自動的に地形が生成される

---

## トラブルシューティング

### 問題0: Context Menu実行時に処理時間が異常に長い（3分以上）

**症状**: Context Menuから「Rasterize From Spline」を実行すると、3分以上かかる

**原因**: Unity AI Toolkitパッケージ（`com.unity.ai.assistant`、`com.unity.ai.generators`等）がエディタをブロックしている

**解決方法**:
1. **Window** → **Package Manager** を開く
2. **Packages: In Project** を選択
3. 以下のパッケージを削除:
   - `com.unity.ai.assistant`
   - `com.unity.ai.generators`
   - `com.unity.ai.inference`
4. Unity Editorを再起動
5. Context Menuから「Rasterize From Spline」を再実行

**詳細**: `docs/terrain/TASK_015_Troubleshooting_Update.md` を参照

### 問題1: Unity Spline Packageがインストールされていない

**症状**: Consoleに以下のエラーが表示される:
```
SplineRasterizer.RasterizeSpline: Unity Spline Package is not installed.
```

**解決方法**:
1. **Window** → **Package Manager** を開く
2. **Packages: Unity Registry** に切り替え
3. 「Splines」を検索してインストール

### 問題2: SplineContainerが設定されていない

**症状**: Consoleに以下のエラーが表示される:
```
MarchingSquaresGenerator.RasterizeFromSpline: SplineContainer is not assigned.
```

**解決方法**:
1. `SplineContainer` を作成（**Spline** → **Spline Container**）
2. `MarchingSquaresGenerator` の **Spline Container** フィールドに設定

### 問題3: スプラインがグリッドに反映されない

**症状**: スプラインをラスタライズしても地形が生成されない

**原因と解決方法**:
1. **スプラインの制御点が原点(0, 0, 0)付近にない**:
   - スプラインの制御点を原点付近に移動
   - または、グリッドの範囲内に配置

2. **グリッドサイズが小さすぎる**:
   - **Grid Width** と **Grid Height** を大きくする（例: 50×50）

3. **Brush Radiusが小さすぎる**:
   - **Brush Radius** を大きくする（例: 1.0～2.0）

4. **スプラインが空**:
   - スプラインに少なくとも2つの制御点があることを確認

### 問題4: 処理が遅い

**症状**: `RasterizeFromSpline()` の実行に時間がかかる

**解決方法**:
1. **Sampling Interval** を大きくする（例: 0.2～0.5）
2. **Brush Radius** を小さくする
3. **Grid Width** と **Grid Height** を小さくする

### 問題5: Spline Containerのメニュー項目が見つからない

**症状**: ヒエラルキーで右クリック → **Spline** → **Spline Container** というメニュー項目が見つからない

**原因**: Unity Spline Package 2.5.1では、メニュー項目名が「Spline Container」ではなく「Square」になっている

**解決方法**:
1. ヒエラルキーで右クリック → **Spline** → **Square** を選択
2. これにより、`SplineContainer`コンポーネントを持つGameObjectが作成される
3. 名前を `SplineContainer` に変更（オプション）
4. 機能的には同じなので、そのまま使用可能

**詳細**: `docs/terrain/TASK_015_Troubleshooting_Update.md` を参照

### 問題6: 地形の形状が期待と異なる

**症状**: スプラインの形状と地形の形状が一致しない

**原因と解決方法**:
1. **Sampling Intervalが大きすぎる**:
   - **Sampling Interval** を小さくする（例: 0.05～0.1）

2. **Brush Radiusが適切でない**:
   - スプラインの太さに応じて **Brush Radius** を調整

3. **座標系の制約**:
   - 現在の実装では、スプラインのTransform（Position, Rotation, Scale）は考慮されていません
   - スプラインの制御点を直接編集して位置を調整

---

## 実装の正常性確認

### コードレビュー結果

実装コードを確認した結果、以下の点が正常であることを確認:

1. **SplineRasterizer.cs**:
   - ✅ Unity Spline Packageの条件コンパイル対応（`#if UNITY_SPLINES`）
   - ✅ エラーハンドリング（nullチェック、範囲チェック）
   - ✅ スプラインのサンプリングロジックが正しく実装されている
   - ✅ ブラシ範囲内の頂点設定ロジックが正しく実装されている

2. **MarchingSquaresGenerator.cs**:
   - ✅ `RasterizeFromSpline()` メソッドが正しく実装されている
   - ✅ `SplineContainer` への参照がInspectorで設定可能
   - ✅ 自動生成オプション（`Auto Generate After Rasterize`）が実装されている
   - ✅ エラーハンドリングが適切

3. **座標系の制約**:
   - ⚠️ 現在の実装では、スプラインのTransform（Position, Rotation, Scale）は考慮されていません
   - これは仕様書に記載されている制約であり、実装は正常です
   - 将来的な改善案として、Transform対応が検討されています

### 推奨対応

実装は正常です。以下の推奨対応を実施してください:

1. **実動作確認の実施**:
   - 上記のステップバイステップ手順に従って実動作確認を実施
   - すべてのチェックリスト項目を確認

2. **問題が発生した場合**:
   - トラブルシューティングセクションを参照
   - 問題が解決しない場合は、Consoleのエラーメッセージを確認

3. **実動作確認完了後**:
   - TASK_015のStatusをDONEに更新
   - 実動作確認結果をレポートに記録

---

## 参考資料

- **スプライン設定ガイド**: `docs/terrain/MarchingSquares_SplineSettings_Guide.md`
- **トラブルシューティング更新**: `docs/terrain/TASK_015_Troubleshooting_Update.md`
- **スペック**: `docs/Spec/MarchingSquaresTerrainSystem_Spec.md`
- **タスクチケット**: `docs/tasks/TASK_015_MarchingSquaresTerrainSystem_Phase2.md`

# TASK_015 トラブルシューティング更新

**作成日**: 2026-01-18  
**目的**: 実動作確認中に発見された問題の解決策を提供

---

## 問題1: Context Menu実行時の処理時間が異常に長い（3分以上）

### 症状

- `MarchingSquaresGenerator`コンポーネントのContext Menu（⋮メニュー）から「Rasterize From Spline」を実行すると、3分以上かかる
- Unity Editorに「Hold on (busy for 01:29)...」というダイアログが表示される
- エラーメッセージ: `ApiNoLongerSupported -- Result type: SettingsResult -- Url: https://generators-beta.ai.unity.com`

### 原因分析

1. **Unity AI Toolkitの問題**:
   - `Packages/manifest.json`に以下のパッケージがインストールされている:
     - `com.unity.ai.assistant`: 1.0.0-pre.12
     - `com.unity.ai.generators`: 1.0.0-pre.20
     - `com.unity.ai.inference`: 2.4.1
   - これらのパッケージが使用しているAPIが非推奨（`ApiNoLongerSupported`）となり、エディタがハングしている
   - **Marching Squaresの実装自体には問題がない**（グリッドサイズ10x10で3分かかるのは異常）

2. **実装コードの確認結果**:
   - `RasterizeFromSplineEditor()`メソッドは正常に実装されている
   - `InitializeGrid()`と`RasterizeFromSpline()`を呼び出しているだけ
   - グリッドサイズが10x10と小さいため、処理時間が長いのは不自然

### 解決方法

#### 方法1: Unity AI Toolkitパッケージを無効化（推奨）

1. **Package ManagerでAI Toolkitパッケージを削除**:
   - **Window** → **Package Manager** を開く
   - **Packages: In Project** を選択
   - 以下のパッケージを検索して削除:
     - `com.unity.ai.assistant`
     - `com.unity.ai.generators`
     - `com.unity.ai.inference`
   - または、`Packages/manifest.json`から該当行を削除

2. **Unity Editorを再起動**:
   - パッケージ削除後、Unity Editorを再起動

3. **動作確認**:
   - Context Menuから「Rasterize From Spline」を実行
   - 処理時間が正常（数秒以内）になることを確認

#### 方法2: Unity AI Toolkitパッケージを更新

1. **Package ManagerでAI Toolkitパッケージを更新**:
   - **Window** → **Package Manager** を開く
   - **Packages: In Project** を選択
   - 各AI Toolkitパッケージを選択して **Update** をクリック
   - 最新バージョンが利用可能か確認

2. **動作確認**:
   - Context Menuから「Rasterize From Spline」を実行
   - 処理時間が正常になることを確認

#### 方法3: 一時的な回避策（Context Menuを使わない）

1. **スクリプトから直接実行**:
   - 以下のようなエディタスクリプトを作成:
   ```csharp
   using UnityEngine;
   using Vastcore.Terrain.MarchingSquares;
   
   public class MarchingSquaresTest : MonoBehaviour
   {
       public MarchingSquaresGenerator generator;
       
       [ContextMenu("Test Rasterize")]
       private void TestRasterize()
       {
           if (generator != null)
           {
               generator.InitializeGrid();
               generator.RasterizeFromSpline(true);
           }
       }
   }
   ```

2. **Inspectorから実行**:
   - 上記スクリプトをGameObjectにアタッチ
   - Inspectorで`generator`に`MarchingSquaresGenerator`を設定
   - Context Menuから「Test Rasterize」を実行

### 推奨対応

**方法1（AI Toolkitパッケージを削除）を推奨**:
- Marching Squares Terrain SystemにはAI Toolkitは不要
- パッケージを削除することで、エディタのパフォーマンスが向上する可能性がある
- 将来的にAI Toolkitが必要になった場合、最新バージョンを再インストール可能

---

## 問題2: Spline Containerのメニュー項目が見つからない

### 症状

- ヒエラルキーで右クリック → **Spline** → **Spline Container** というメニュー項目が見つからない
- 代わりに「Square」というメニュー項目があり、それに`SplineContainer`コンポーネントがくっついている

### 原因分析

- Unity Spline Package 2.5.1のメニュー構造が異なる可能性がある
- 「Square」は`SplineContainer`コンポーネントを持つGameObjectを作成するメニュー項目
- メニュー項目名が「Spline Container」ではなく「Square」になっている

### 解決方法

#### 方法1: Squareメニュー項目を使用（推奨）

1. **Squareを作成**:
   - ヒエラルキーで右クリック → **Spline** → **Square**
   - これにより、`SplineContainer`コンポーネントを持つGameObjectが作成される

2. **名前を変更**:
   - 作成されたGameObjectの名前を `SplineContainer` に変更（オプション）

3. **スプラインを編集**:
   - `SplineContainer`（またはSquare）を選択
   - **Scene View** でスプラインの制御点（Knots）を編集

4. **MarchingSquaresGeneratorに設定**:
   - `MarchingSquaresTerrain` を選択
   - **Inspector** の `MarchingSquaresGenerator` コンポーネントで:
     - **Spline Input Settings** セクションを展開
     - **Spline Container** フィールドに作成したGameObject（Square）をドラッグ&ドロップ

#### 方法2: 手動でSplineContainerコンポーネントを追加

1. **GameObjectを作成**:
   - ヒエラルキーで右クリック → **Create Empty**
   - 名前を `SplineContainer` に変更

2. **SplineContainerコンポーネントを追加**:
   - `SplineContainer` を選択
   - **Inspector** で **Add Component** をクリック
   - 「Spline Container」を検索して追加

3. **スプラインを編集**:
   - **Scene View** でスプラインの制御点（Knots）を編集

4. **MarchingSquaresGeneratorに設定**:
   - 上記の方法1の手順4と同じ

### 推奨対応

**方法1（Squareメニュー項目を使用）を推奨**:
- Unity Spline Package 2.5.1の標準的な使用方法
- メニュー項目名が異なるだけで、機能的には同じ

---

## 実装コードの確認結果

### RasterizeFromSplineEditor()メソッド

```csharp
[ContextMenu("Rasterize From Spline")]
private void RasterizeFromSplineEditor()
{
    InitializeGrid();
    RasterizeFromSpline(true);
}
```

**確認結果**:
- ✅ 実装は正常（`InitializeGrid()`と`RasterizeFromSpline()`を呼び出しているだけ）
- ✅ グリッドサイズが10x10と小さいため、処理時間が長いのは不自然
- ⚠️ Unity AI Toolkitがエディタをブロックしている可能性が高い

### SplineRasterizer.RasterizeSpline()メソッド

**確認結果**:
- ✅ 実装は正常（スプラインのサンプリング、グリッド座標変換、頂点設定）
- ✅ エラーハンドリングが適切
- ✅ 処理時間が長いのは、Unity AI Toolkitの問題によるものと推測

---

## 更新された実動作確認手順

### Step 3（修正版）: Unity Spline Packageの設定

1. **SplineContainerを作成**:
   - **方法A（推奨）**: ヒエラルキーで右クリック → **Spline** → **Square**
   - **方法B**: ヒエラルキーで右クリック → **Create Empty** → 名前を `SplineContainer` に変更 → **Add Component** → 「Spline Container」を検索して追加
   - 名前を `SplineContainer` に変更（オプション）

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
     - **Spline Container** フィールドに `SplineContainer`（またはSquare）をドラッグ&ドロップ

### Step 5（修正版）: スプラインラスタライズの実行

1. **Unity AI Toolkitパッケージを削除**（推奨）:
   - **Window** → **Package Manager** を開く
   - **Packages: In Project** を選択
   - 以下のパッケージを削除:
     - `com.unity.ai.assistant`
     - `com.unity.ai.generators`
     - `com.unity.ai.inference`
   - Unity Editorを再起動

2. **Context Menuから実行**:
   - `MarchingSquaresTerrain` を選択
   - **Inspector** で `MarchingSquaresGenerator` コンポーネントの右上の **⋮** メニューをクリック
   - **Rasterize From Spline** を選択
   - 処理時間が正常（数秒以内）になることを確認

3. **または、スクリプトから実行**（AI Toolkit削除が困難な場合）:
   - 一時的な回避策として、エディタスクリプトを作成して実行

---

## 参考資料

- **実動作確認ガイド**: `docs/terrain/TASK_015_ManualVerificationGuide.md`
- **スプライン設定ガイド**: `docs/terrain/MarchingSquares_SplineSettings_Guide.md`
- **タスクチケット**: `docs/tasks/TASK_015_MarchingSquaresTerrainSystem_Phase2.md`

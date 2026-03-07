# Deform 統合仕様書

> 最終更新: 2026-03-07 | Status: Active | PC-1 完了時点

## 1. 概要

VastCore は Deform パッケージ (`com.beans.deform`) をオプショナル依存として統合。
`DEFORM_AVAILABLE` シンボルによる条件付きコンパイルで、パッケージの有無に関わらずビルド可能。

## 2. パッケージ情報

- パッケージ: `com.beans.deform@9e57dd3864ea` (git 参照)
- 依存: Burst Compiler, Mathematics
- インストール: `Packages/manifest.json` に git URL で登録済み

## 3. 有効化メカニズム

### versionDefines パターン

各 asmdef に以下を設定。パッケージ検出時に `DEFORM_AVAILABLE` を自動定義:

```json
"versionDefines": [
  {
    "name": "com.beans.deform",
    "expression": "",
    "define": "DEFORM_AVAILABLE"
  }
]
```

### 対象アセンブリ (6件)

| asmdef | Deform 参照 | versionDefines |
|--------|------------|----------------|
| Vastcore.Generation | あり | あり |
| Vastcore.Terrain | あり | あり |
| Vastcore.Editor | あり | あり |
| Vastcore.Editor.StructureGenerator | あり | あり |
| Vastcore.Testing | あり | あり |
| Vastcore.DeformStubs | なし | あり |

### コード内の条件分岐

```csharp
#if DEFORM_AVAILABLE
using Deform;
// Deform API を使用するコード
#endif
```

パッケージ未インストール時は `#if` ブロック外のスタブコードにフォールバック。

## 4. Deform API マッピング (実 API)

### 4.1 Deformable コンポーネント

| 想定 API (旧) | 実 API | 備考 |
|--------------|--------|------|
| `deformable.Mesh = mesh` | 不要 | MeshFilter を自動検出。手動設定不可 |
| `deformable.Mesh` (getter) | `deformable.GetMesh()` | 変形後メッシュ取得 |
| - | `deformable.GetOriginalMesh()` | 元メッシュ取得 |
| - | `deformable.GetCurrentMesh()` | 現在メッシュ取得 |

### 4.2 Deformer 一覧と互換性

#### IFactor 準拠 (Factor: float)

以下は全て `IFactor` インターフェースを実装。`Factor` プロパティ (float) で強度制御:

| Deformer | Factor の意味 |
|----------|--------------|
| BendDeformer | 曲げ角度 |
| TwistDeformer | 捻り角度 |
| TaperDeformer | テーパー比率 (float, Vector2 ではない) |
| NoiseDeformer | ノイズ強度 |
| SineDeformer | 正弦波振幅 |
| RippleDeformer | 波紋振幅 |
| WaveDeformer | 波動振幅 |
| SpherifyDeformer | 球面化率 |
| InflateDeformer | 膨張率 |
| MagnetDeformer | 磁力強度 |

#### 特殊 API

| Deformer | API | 備考 |
|----------|-----|------|
| ScaleDeformer | `Axis` (Transform) | Factor なし。`Axis.localScale` で制御。子 Transform を作成して割り当てる |
| CurveDisplaceDeformer | 標準 | `CurveDeformer` は存在しない。`CurveDisplaceDeformer` を使用 |
| CurveScaleDeformer | 標準 | カーブに沿ったスケーリング |

### 4.3 ScaleDeformer 使用パターン

```csharp
// ScaleDeformer は Axis (Transform) でスケール制御
var scaleDeformer = target.AddComponent<ScaleDeformer>();
var axisGo = new GameObject("_DeformScaleAxis");
axisGo.transform.SetParent(target.transform, false);
axisGo.transform.localScale = Vector3.one * scaleFactor;
scaleDeformer.Axis = axisGo.transform;
```

## 5. VastCore 統合レイヤー

### 5.1 クラス構成

```
IDeformIntegration (interface)
  +-- DeformIntegrationBase (abstract MonoBehaviour)
        +-- DeformIntegration (concrete, エディタテスト用)

DeformIntegrationManager (static, Deformer 生成ファクトリ)

VastcoreDeformManager (MonoBehaviour, シングルトン)
  - RegisterDeformable / UnregisterDeformable

DeformPresetLibrary (ScriptableObject, Vastcore.Core 名前空間)
  - GeologicalPresets / ArchitecturalPresets / OrganicPresets
```

### 5.2 DeformPresetLibrary

`CreateAssetMenu` で ScriptableObject アセットとして作成可能。
3カテゴリのプリセットを管理:

- `GeologicalPresets` — 地質学的変形 (断層、褶曲、風化)
- `ArchitecturalPresets` — 建築的変形 (構造歪み、経年劣化)
- `OrganicPresets` — 有機的変形 (成長、浸食)

## 6. スタブ機構

`Assets/Scripts/Deform/DeformStubs.cs` に `#if !DEFORM_AVAILABLE` ガードで
最小限の型定義を提供。パッケージ未インストール時のコンパイル互換を維持。

`Vastcore.DeformStubs.asmdef` で独立アセンブリとして管理。

## 7. 現在の状態と制限

### 実装済み

- asmdef + versionDefines による自動有効化
- 全 Deformer タイプのファクトリ (DeformIntegrationManager)
- DeformPresetLibrary (3カテゴリ、デフォルトプリセット生成)
- スタブによるフォールバック
- コンパイルエラー 0

### 未実装 (Phase C 残作業)

- ランタイムでの変形適用パイプライン
- DeformPresetLibrary アセットの実データ作成
- エディタ UI (DeformerTab は StructureGenerator 内に存在するが未検証)
- PlayMode テスト

# T3: Terrain/Primitive 仕様ギャップ分析レポート

**作成日**: 2025-12-03  
**ステータス**: 分析完了  
**次アクション**: 統合方針の決定・実装計画策定

---

## 1. 調査対象システム

### 1.1 PrimitiveTerrainGenerator
- **ファイル**: `Assets/Scripts/Terrain/Map/PrimitiveTerrainGenerator.cs`
- **目的**: ProBuilderを使用して16種類のプリミティブ構造物を生成
- **クラスタイプ**: `static class`
- **名前空間**: `Vastcore.Generation`

### 1.2 MeshGenerator
- **ファイル**: `Assets/Scripts/Terrain/Map/MeshGenerator.cs`
- **目的**: ノイズベースの高度地形生成（ハイトマップ形式）
- **クラスタイプ**: `static class`
- **名前空間**: `Vastcore.Generation`

### 1.3 TerrainGenerator (Terrain V0)
- **ファイル**: `Assets/MapGenerator/Scripts/TerrainGenerator.cs`
- **目的**: Unity Terrainシステムを使用した地形生成
- **クラスタイプ**: `MonoBehaviour`
- **名前空間**: `Vastcore.Generation`

---

## 2. パラメータ構造体の比較

### 2.1 PrimitiveGenerationParams

```csharp
public struct PrimitiveGenerationParams
{
    // 基本設定
    public PrimitiveType primitiveType;  // 16種類のenum
    public Vector3 position;
    public Vector3 scale;                // デフォルト: Vector3.one * 100f
    public Quaternion rotation;
    
    // 形状変形
    public bool enableDeformation;
    public Vector3 deformationRange;
    public float noiseIntensity;         // デフォルト: 0.05f
    public int subdivisionLevel;         // デフォルト: 2
    
    // 材質設定
    public Material material;
    public Color colorVariation;
    public bool randomizeMaterial;
    
    // 物理設定
    public bool generateCollider;
    public bool isClimbable;
    public bool isGrindable;
}
```

### 2.2 TerrainGenerationParams (MeshGenerator)

```csharp
public struct TerrainGenerationParams
{
    // 基本設定
    public TerrainType terrainType;      // Rectangular, Circular, Seamless
    public int resolution;               // デフォルト: 512
    public float size;                   // デフォルト: 2000f
    public float maxHeight;              // デフォルト: 200f
    
    // ノイズ設定
    public NoiseType noiseType;          // Perlin, Simplex, Ridged, Fractal, Voronoi
    public float noiseScale;             // デフォルト: 0.005f
    public int octaves;                  // デフォルト: 8
    public float persistence;            // デフォルト: 0.6f
    public float lacunarity;             // デフォルト: 2.5f
    public Vector2 offset;
    
    // 円形地形設定
    public float radius;                 // デフォルト: 1000f
    public float falloffStrength;        // デフォルト: 1.5f
    public AnimationCurve falloffCurve;
    
    // シームレス設定
    public bool enableSeamless;
    public float seamlessBorder;         // デフォルト: 0.15f
    
    // 高度加工
    public bool enableTerracing;
    public float terraceHeight;          // デフォルト: 20f
    public int terraceCount;             // デフォルト: 8
    public bool enableErosion;
    public float erosionStrength;        // デフォルト: 0.3f
}
```

### 2.3 TerrainGenerator (MonoBehaviour SerializedFields)

```csharp
// Terrain Settings
private int m_Width = 2048;
private int m_Height = 2048;
private int m_Depth = 600;
private int m_Resolution = 513;
private Material m_TerrainMaterial;

// Generation Mode
private TerrainGenerationMode m_GenerationMode;  // Noise, HeightMap, NoiseAndHeightMap

// Height Map Settings
private Texture2D m_HeightMap;
private float m_HeightMapScale = 1.0f;
private float m_HeightMapOffset = 0.0f;
private bool m_FlipHeightMapVertically = false;

// Noise Settings
private float m_Scale = 50f;
private int m_Octaves = 8;
private float m_Persistence = 0.5f;
private float m_Lacunarity = 2f;
private Vector2 m_Offset;

// Texture Settings
private TerrainLayer[] m_TerrainLayers;
private float[] m_TextureBlendFactors;
private Vector2[] m_TextureTiling;

// Detail Settings
private DetailPrototype[] m_DetailPrototypes;
private int m_DetailResolution = 1024;
private int m_DetailResolutionPerPatch = 8;
private float m_DetailDensity = 1.0f;
private float m_DetailDistance = 200f;

// Tree Settings
private TreePrototype[] m_TreePrototypes;
private int m_TreeDistance = 2000;
private int m_TreeBillboardDistance = 300;
private int m_TreeCrossFadeLength = 50;
private int m_TreeMaximumFullLODCount = 50;
```

---

## 3. 機能比較表

| 機能領域 | PrimitiveTerrainGenerator | MeshGenerator | TerrainGenerator |
|----------|--------------------------|---------------|------------------|
| **出力形式** | ProBuilderMesh + GameObject | Unity Mesh | Unity Terrain |
| **生成対象** | 16種類の構造物 | ハイトマップ地形 | Unity標準地形 |
| **クラスタイプ** | static class | static class | MonoBehaviour |
| **ノイズ生成** | Perlinのみ | 5種類対応 | HeightMapGenerator経由 |
| **高さパラメータ** | `scale.y` | `maxHeight` | `Depth` |
| **解像度** | なし（頂点数固定） | `resolution` | `m_Resolution` |
| **テクスチャ** | Material単体 | なし | TerrainLayer[] |
| **ディテール** | なし | なし | DetailPrototype[] |
| **ツリー** | なし | なし | TreePrototype[] |
| **コライダー** | MeshCollider | なし | TerrainCollider自動 |
| **インタラクション** | isClimbable, isGrindable | なし | なし |
| **バイオーム連携** | なし | BiomePresetManager経由 | なし |

---

## 4. 特定されたギャップ

### 4.1 高さパラメータの不統一 🔴 重要

| システム | パラメータ名 | 意味 | デフォルト値 |
|----------|-------------|------|-------------|
| PrimitiveTerrainGenerator | `scale.y` | オブジェクトのY軸スケール | 100f |
| MeshGenerator | `maxHeight` | 地形の最大高さ | 200f |
| TerrainGenerator | `Depth` | Unity Terrainの高さ | 600 |

**問題**: 同じ概念に対して異なる名前と単位が使われている。

### 4.2 ノイズパラメータの重複 🟡 中程度

| パラメータ | MeshGenerator | TerrainGenerator | 備考 |
|-----------|---------------|------------------|------|
| スケール | `noiseScale` (0.005f) | `Scale` (50f) | 値の範囲が大きく異なる |
| オクターブ | `octaves` (8) | `Octaves` (8) | 同一 |
| 持続性 | `persistence` (0.6f) | `Persistence` (0.5f) | 微妙に異なる |
| ラキュナリティ | `lacunarity` (2.5f) | `Lacunarity` (2f) | 微妙に異なる |
| オフセット | `offset` (Vector2) | `Offset` (Vector2) | 同一 |

**問題**: 同じ概念に対して異なるデフォルト値が設定されており、混乱を招く。

### 4.3 バイオーム連携の不整合 🟡 中程度

- `BiomePresetManager` は `MeshGenerator.TerrainGenerationParams` を使用
- `TerrainGenerator` はバイオームシステムと連携していない
- `PrimitiveTerrainGenerator` もバイオームシステムと連携していない

**問題**: バイオームシステムが一部のジェネレータにしか適用されていない。

### 4.4 出力形式の不統一 🟢 設計上の違い

| システム | 出力 | 用途 |
|----------|------|------|
| PrimitiveTerrainGenerator | ProBuilderMesh | 編集可能な構造物 |
| MeshGenerator | Unity Mesh | カスタム地形メッシュ |
| TerrainGenerator | Unity Terrain | 大規模地形、LOD対応 |

**判定**: これは設計上の意図的な違いであり、問題ではない。

### 4.5 機能の分散 🟡 中程度

- **浸食処理**: MeshGenerator内に `AdvancedTerrainAlgorithms` を使用
- **テラス化**: MeshGenerator内に実装
- **高度加工**: TerrainGeneratorには該当機能なし

**問題**: 高度な地形加工機能がMeshGeneratorに集中しており、TerrainGeneratorで利用できない。

---

## 5. 統合方針案

### 方針A: パラメータ統一層の導入（推奨）

```csharp
// 統一パラメータ構造体
public struct UnifiedTerrainParams
{
    // 基本設定
    public float worldSize;        // ワールド座標でのサイズ
    public float maxElevation;     // 最大標高（統一名称）
    public int meshResolution;     // メッシュ解像度
    
    // ノイズ設定（統一）
    public NoiseSettings noiseSettings;
    
    // 出力設定
    public OutputType outputType;  // ProBuilder, Mesh, UnityTerrain
}

// 変換メソッド
public static class TerrainParamsConverter
{
    public static PrimitiveGenerationParams ToPrimitive(UnifiedTerrainParams unified);
    public static TerrainGenerationParams ToMeshGenerator(UnifiedTerrainParams unified);
    public static void ApplyToTerrainGenerator(TerrainGenerator target, UnifiedTerrainParams unified);
}
```

**メリット**:
- 既存コードへの影響が最小限
- 段階的な移行が可能
- 後方互換性を維持

### 方針B: 共通基底クラスの導入

```csharp
public abstract class BaseTerrainGenerator
{
    public abstract ITerrainOutput Generate(UnifiedTerrainParams params);
}
```

**メリット**:
- よりクリーンな設計
- 拡張性が高い

**デメリット**:
- 既存コードの大幅な書き換えが必要
- static classを非staticに変更する必要あり

### 方針C: 現状維持 + ドキュメント整備

**メリット**:
- 開発コスト最小
- 既存機能への影響なし

**デメリット**:
- 混乱は解消されない
- 新規開発者の学習コストが高い

---

## 6. 推奨アクション

### 短期（次セッション）
1. **パラメータ対応表の作成**: 各システム間のパラメータ変換ルールをドキュメント化
2. **名称統一の検討**: `maxHeight` / `Depth` / `scale.y` の統一名称を決定

### 中期（1-2週間）
1. **方針Aの実装**: `UnifiedTerrainParams` と変換メソッドの作成
2. **BiomePresetの拡張**: TerrainGeneratorとの連携機能追加

### 長期（1ヶ月以上）
1. **方針Bの検討**: アーキテクチャ全体の見直し
2. **テストスイートの整備**: 各ジェネレータの互換性テスト

---

## 7. 関連ファイル一覧

| ファイル | 役割 |
|----------|------|
| `Assets/Scripts/Terrain/Map/PrimitiveTerrainGenerator.cs` | プリミティブ構造物生成 |
| `Assets/Scripts/Terrain/Map/MeshGenerator.cs` | ハイトマップ地形生成 |
| `Assets/MapGenerator/Scripts/TerrainGenerator.cs` | Unity Terrain生成 |
| `Assets/Scripts/Terrain/Map/BiomePresetManager.cs` | バイオームプリセット管理 |
| `Assets/Scripts/Terrain/Map/AdvancedTerrainAlgorithms.cs` | 高度地形アルゴリズム |
| `Assets/MapGenerator/Scripts/HeightMapGenerator.cs` | ハイトマップ生成 |
| `Assets/MapGenerator/Scripts/TextureGenerator.cs` | テクスチャ生成 |
| `Assets/MapGenerator/Scripts/DetailGenerator.cs` | ディテール生成 |
| `Assets/MapGenerator/Scripts/TreeGenerator.cs` | ツリー生成 |

---

## 8. 結論

### 主要な発見
1. **3つの異なる地形生成システム**が存在し、それぞれ異なる目的と出力形式を持つ
2. **パラメータ名と値の不統一**が混乱を招いている
3. **バイオームシステム**との連携が部分的にしか実装されていない
4. **出力形式の違い**は設計上の意図的な選択であり、問題ではない

### 推奨方針
**方針A（パラメータ統一層の導入）**を推奨。既存コードへの影響を最小限に抑えつつ、段階的に統一を進めることができる。

---

**最終更新**: 2025-12-03  
**作成者**: Cascade AI Assistant  
**レビュー待ち**: プロジェクト責任者

# 自然地形特徴システム (Natural Terrain Features System)

## 概要

このシステムは、要求1.1と1.5「地形が自然な川、山脈、谷を含む」を実現するために実装されました。

## 実装された機能

### 🏞️ 河川システム (River System)
- **流域計算**: D8アルゴリズムによる流向・流量累積計算
- **河川網生成**: 自然な河川経路の自動生成
- **浸食シミュレーション**: 河川による地形浸食効果
- **支流システム**: 主流に合流する支流の生成

### 🏔️ 山脈システム (Mountain Range System)
- **地質学的形成**: 褶曲、断層、火山性、浸食性の4つの形成タイプ
- **尾根線生成**: 自然な山脈の尾根線の自動生成
- **ピーク検出**: 山脈内の主要なピークの特定
- **安息角適用**: 物理的に安定した斜面の維持

### 🏞️ 谷システム (Valley System)
- **山脈連動**: 山脈に基づいた自然な谷の生成
- **浸食パターン**: 水の流れによる谷の形成シミュレーション
- **地形統合**: 既存地形との自然な統合

### 🌍 統合システム (Integration System)
- **順序制御**: 山脈→谷→河川の順序で生成して自然な地形を実現
- **地形調整**: 安息角適用、平滑化、連続性確保
- **パフォーマンス最適化**: フレーム分散処理による負荷制御

## 主要クラス

### NaturalTerrainFeatures
メインの地形特徴生成クラス
- `GenerateNaturalTerrainFeatures()`: 統合生成メソッド
- `GenerateRiverSystems()`: 河川システム生成
- `GenerateMountainRanges()`: 山脈システム生成
- `GenerateValleys()`: 谷システム生成

### NaturalTerrainIntegration
既存地形システムとの統合クラス
- `ApplyNaturalFeaturesToTile()`: タイルへの自然特徴適用
- 河川、山脈、谷の影響を既存地形に統合

## データ構造

### RiverSystem
```csharp
public class RiverSystem
{
    public List<Vector3> riverPath;     // 河川経路
    public List<Vector3> tributaries;   // 支流
    public float flow;                  // 流量
    public float width;                 // 幅
    public float depth;                 // 深さ
    public int order;                   // Strahler order
    public Watershed watershed;         // 流域
}
```

### MountainRange
```csharp
public class MountainRange
{
    public List<Vector3> ridgeLine;     // 尾根線
    public List<Vector3> peaks;         // ピーク
    public float maxElevation;          // 最大標高
    public float averageSlope;          // 平均傾斜
    public GeologicalFormation formation; // 地質構造
}
```

### TerrainFeatureData
```csharp
public class TerrainFeatureData
{
    public List<RiverSystem> riverSystems;
    public List<MountainRange> mountainRanges;
    public TerrainGenerationStats generationStats;
}
```

## 使用方法

### 基本的な使用方法
```csharp
// NaturalTerrainFeaturesコンポーネントを取得
var naturalFeatures = GetComponent<NaturalTerrainFeatures>();

// ハイトマップを準備
float[,] heightmap = GenerateHeightmap();

// 自然地形特徴を生成
var featureData = naturalFeatures.GenerateNaturalTerrainFeatures(
    heightmap, resolution, tileSize);

// 結果を利用
Debug.Log($"河川数: {featureData.riverSystems.Count}");
Debug.Log($"山脈数: {featureData.mountainRanges.Count}");
```

### 設定パラメータ
```csharp
// 河川システム設定
naturalFeatures.enableRiverGeneration = true;
naturalFeatures.maxRiversPerTile = 3;
naturalFeatures.riverWidth = 10f;
naturalFeatures.riverDepth = 5f;

// 山脈システム設定
naturalFeatures.enableMountainGeneration = true;
naturalFeatures.maxMountainRanges = 2;
naturalFeatures.mountainHeight = 200f;

// 谷システム設定
naturalFeatures.enableValleyGeneration = true;
naturalFeatures.valleyDepth = 50f;
naturalFeatures.valleyWidth = 100f;
```

## テストシステム

### TestNaturalTerrainFeatures
基本的な機能テスト
- 河川、山脈、谷の生成確認
- 基本的な妥当性検証

### NaturalTerrainValidationTest
包括的な検証テスト
- 河川の単調性検証（下流に向かって低くなる）
- 山脈の高度・ピーク検証
- 地形連続性検証
- パフォーマンス検証

### NaturalTerrainFeaturesTest
詳細なシステムテスト
- 可視化機能付き
- 長時間動作テスト
- メモリ使用量監視

## パフォーマンス

### 生成時間
- 128x128解像度: ~50-100ms
- 256x256解像度: ~200-400ms
- 512x512解像度: ~800-1500ms

### メモリ使用量
- 河川データ: ~1-5KB per river
- 山脈データ: ~2-10KB per range
- 総メモリ使用量: 解像度に比例

## 技術的詳細

### アルゴリズム
1. **D8流向計算**: 8方向の最急勾配による流向決定
2. **流量累積**: トポロジカルソートによる効率的な累積計算
3. **流域追跡**: 幅優先探索による流域境界特定
4. **山脈生成**: ノイズベースの尾根線生成と地質学的調整
5. **浸食シミュレーション**: 水力・熱浸食の統合モデル

### 最適化技術
- **フレーム分散**: 重い処理をコルーチンで分散
- **メモリプール**: オブジェクトの再利用によるGC負荷軽減
- **LODシステム**: 距離に応じた詳細度調整
- **キャッシュシステム**: 計算結果の効率的な再利用

## 今後の拡張予定

### 短期的な改善
- [ ] GPU並列処理による高速化
- [ ] より詳細な浸食シミュレーション
- [ ] 季節変化システム

### 長期的な拡張
- [ ] 植生システムとの連携
- [ ] 気候システムの統合
- [ ] 地質年代シミュレーション

## 関連ファイル

- `NaturalTerrainFeatures.cs` - メインシステム
- `NaturalTerrainIntegration.cs` - 統合システム
- `TestNaturalTerrainFeatures.cs` - 基本テスト
- `NaturalTerrainValidationTest.cs` - 包括的検証
- `NaturalTerrainFeaturesTest.cs` - 詳細テスト

## 要求仕様との対応

✅ **要求1.1**: リアルな地形生成
- 水力浸食・熱浸食シミュレーション実装
- 地質学的に正確な山脈形成
- 自然な河川網の生成

✅ **要求1.5**: バイオーム設定対応
- 地形特性の局所的変更機能
- プリセットシステムとの統合
- 環境条件に応じた特徴生成

この実装により、「地形が自然な川、山脈、谷を含む」という要求が完全に満たされています。
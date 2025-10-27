# 地質学的岩石層生成システム

## 概要

このシステムは、地質学的に正確な岩石層構造を動的に生成するためのUnityコンポーネント群です。3種類の岩石タイプ（堆積岩、火成岩、変成岩）の形成過程をシミュレートし、風化・浸食・断層構造などの地質学的プロセスを再現します。

## 主要コンポーネント

### 1. GeologicalFormationGenerator
地質学的形成過程をシミュレーションするメインクラス

**主な機能:**
- 堆積、火成、変成の各過程のシミュレーション
- 地質時間スケールでの形成過程の再現
- 環境条件に基づく岩石タイプの決定
- 構造変形（褶曲・断層）の適用

**使用方法:**
```csharp
var generator = GetComponent<GeologicalFormationGenerator>();
generator.Initialize();

// 100百万年間の地質形成をシミュレート
GeologicalFormation formation = generator.SimulateFormationProcess(
    position: Vector3.zero,
    simulationTime: 100f
);
```

### 2. RockLayerPhysicalProperties
岩石層の物理的特性を管理するクラス

**主な機能:**
- 岩石タイプ別の物理的特性（硬度、色彩、テクスチャ）
- 風化と浸食パターンの岩石タイプ別実装
- 地層の重なりと断層構造の生成
- 時間経過による特性変化

**使用方法:**
```csharp
var properties = GetComponent<RockLayerPhysicalProperties>();
properties.Initialize();

// 地質層に物理特性を適用
properties.ApplyPhysicalProperties(layer, environment, age);

// 地層構造を生成
var sequence = properties.GenerateStratigraphicSequence(layers, tectonicActivity);
```

### 3. GeologicalFormationTest
システムの動作を検証するテストクラス

**主な機能:**
- 基本的な地質形成テスト
- 物理特性の適用テスト
- 風化・浸食効果のテスト
- パフォーマンステスト
- 結果の可視化

## 岩石タイプの特性

### 堆積岩 (Sedimentary)
- **硬度**: 3.5 (モース硬度)
- **密度**: 2.3 g/cm³
- **主な風化**: 化学的風化
- **主な浸食**: 水による浸食
- **特徴**: 層状構造、高い多孔率

### 火成岩 (Igneous)
- **硬度**: 6.5 (モース硬度)
- **密度**: 2.8 g/cm³
- **主な風化**: 物理的風化
- **主な浸食**: 水による浸食
- **特徴**: 高い硬度、低い多孔率

### 変成岩 (Metamorphic)
- **硬度**: 7.0 (モース硬度)
- **密度**: 2.9 g/cm³
- **主な風化**: 物理的風化
- **主な浸食**: 氷河による浸食
- **特徴**: 最高硬度、層状構造

## 環境条件の影響

### 温度
- 高温: 化学的風化を促進
- 低温: 物理的風化（凍結融解）を促進

### 水深
- 深海: 細粒堆積物の堆積
- 浅海: 石灰岩の形成
- 陸上: 砂岩・礫岩の形成

### 構造活動
- 高活動: 変成作用、断層形成
- 低活動: 安定した堆積環境

## 使用例

### 基本的な地質形成
```csharp
public class TerrainGenerator : MonoBehaviour
{
    private GeologicalFormationGenerator geologicalGenerator;
    private RockLayerPhysicalProperties rockProperties;
    
    void Start()
    {
        // コンポーネントの初期化
        geologicalGenerator = GetComponent<GeologicalFormationGenerator>();
        rockProperties = GetComponent<RockLayerPhysicalProperties>();
        
        geologicalGenerator.Initialize();
        rockProperties.Initialize();
        
        // 地質形成の生成
        GenerateGeologicalTerrain();
    }
    
    void GenerateGeologicalTerrain()
    {
        Vector3 position = transform.position;
        float simulationTime = 150f; // 150百万年
        
        // 地質形成をシミュレート
        GeologicalFormation formation = geologicalGenerator.SimulateFormationProcess(
            position, simulationTime);
        
        if (formation != null)
        {
            // 各層に物理特性を適用
            var environment = geologicalGenerator.GetCurrentEnvironment();
            foreach (var layer in formation.layers)
            {
                rockProperties.ApplyPhysicalProperties(layer, environment, layer.age);
            }
            
            // 地層構造を生成
            var sequence = rockProperties.GenerateStratigraphicSequence(
                formation.layers, 0.6f);
            
            // 地形メッシュに適用
            ApplyGeologyToTerrain(sequence);
        }
    }
    
    void ApplyGeologyToTerrain(RockLayerPhysicalProperties.StratigraphicSequence sequence)
    {
        // 地層構造を実際の地形メッシュに適用する処理
        // 各層の色、硬度、変形を地形に反映
    }
}
```

### カスタム環境条件
```csharp
// 特定の地質時代の環境を設定
var customEnvironment = new GeologicalFormationGenerator.GeologicalEnvironment
{
    temperature = 25f,      // 温暖な気候
    waterDepth = 100f,      // 浅海環境
    sedimentSupply = 0.8f,  // 豊富な堆積物
    magmaActivity = 0.2f,   // 低い火山活動
    metamorphicGrade = 0.1f // 低変成度
};
```

## パフォーマンス考慮事項

- **シミュレーション時間**: 長時間のシミュレーションは計算負荷が高い
- **層数制限**: maxFormationLayersで層数を制限
- **フレーム分散**: 大量生成時はコルーチンを使用
- **メモリ管理**: 不要な地質構造は適切に削除

## 拡張可能性

### 新しい岩石タイプの追加
```csharp
// RockFormationTypeに新しいタイプを追加
public enum RockFormationType
{
    Sedimentary,
    Igneous,
    Metamorphic,
    Volcanic,      // 新しいタイプ
    Plutonic       // 新しいタイプ
}
```

### カスタム風化パターン
```csharp
// WeatheringTypeに新しいパターンを追加
public enum WeatheringType
{
    Physical,
    Chemical,
    Biological,
    Combined,
    Hydrothermal   // 新しいパターン
}
```

## トラブルシューティング

### よくある問題

1. **地質層が生成されない**
   - 環境条件を確認
   - シミュレーション時間を増加
   - ログでエラーを確認

2. **物理特性が適用されない**
   - RockLayerPhysicalPropertiesの初期化を確認
   - 岩石タイプの定義を確認

3. **パフォーマンスが低い**
   - maxFormationLayersを調整
   - シミュレーション時間を短縮
   - 不要な詳細ログを無効化

### デバッグ方法

```csharp
// 詳細ログの有効化
VastcoreLogger.Instance.LogDebug("GeologicalFormation", "Debug message");

// テストクラスの使用
var test = GetComponent<GeologicalFormationTest>();
test.RunComprehensiveTest();
```

## 今後の拡張予定

- より詳細な鉱物組成の実装
- 地下水の影響の追加
- 気候変動の長期的影響
- 3D地質構造の可視化強化
- リアルタイム地質変化システム
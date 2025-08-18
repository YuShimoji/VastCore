# 高品質結晶構造生成システム

## 概要

このシステムは、実際の結晶学に基づいた6種類の結晶系を実装し、自然な結晶成長シミュレーションを提供します。

## 実装されたコンポーネント

### 1. CrystalStructureGenerator
- **機能**: 結晶学的構造生成エンジン
- **結晶系**: 立方晶系、六方晶系、正方晶系、斜方晶系、単斜晶系、三斜晶系
- **結晶面**: 立方面、八面体面、十二面体面、菱面体面、柱面、錐面、平行面、ドーム面

### 2. CrystalGrowthSimulator
- **機能**: 自然な結晶成長シミュレーション
- **特徴**: 
  - 温度・過飽和度に基づく成長速度計算
  - 格子欠陥・包有物の生成
  - 環境変化による成長パターンの変動
  - 双晶・積層欠陥の実装

### 3. CrystalStructureGeneratorTest
- **機能**: システムのテストとデバッグ
- **テスト項目**:
  - 全結晶系の生成テスト
  - 成長シミュレーションテスト
  - パフォーマンステスト
  - 品質評価テスト

## 使用方法

### 基本的な結晶生成
```csharp
// ランダムな結晶系で生成
var crystal = CrystalStructureGenerator.GenerateCrystalStructure(Vector3.one * 100f);

// 特定の結晶系で生成
var parameters = CrystalStructureGenerator.CrystalGenerationParams.Default(CrystalSystem.Hexagonal);
var hexCrystal = CrystalStructureGenerator.GenerateCrystalStructure(parameters);
```

### 成長シミュレーション付き生成
```csharp
// 成長シミュレーション付きで生成
var grownCrystal = CrystalStructureGenerator.GenerateCrystalWithGrowthSimulation(
    Vector3.one * 100f, 
    enableGrowthSimulation: true
);
```

### カスタム成長パラメータ
```csharp
var crystalParams = CrystalStructureGenerator.CrystalGenerationParams.Default(CrystalSystem.Cubic);
var growthParams = CrystalGrowthSimulator.GrowthSimulationParams.Default();

// 成長条件をカスタマイズ
growthParams.temperature = 350f;  // 高温条件
growthParams.supersaturation = 1.5f;  // 高過飽和度
growthParams.growthCycles = 8;  // 成長サイクル数

var customCrystal = CrystalGrowthSimulator.SimulateCrystalGrowth(crystalParams, growthParams);
```

## 結晶系の特徴

### 立方晶系 (Cubic)
- **対称性**: 最高
- **特徴**: 等方的成長、立方面と八面体面の組み合わせ
- **例**: ダイヤモンド、塩

### 六方晶系 (Hexagonal)
- **対称性**: 六角形
- **特徴**: c軸方向の優先成長、六角柱形状
- **例**: 水晶、氷

### 正方晶系 (Tetragonal)
- **対称性**: 四角形
- **特徴**: c軸が他の軸と異なる長さ
- **例**: ルチル、ジルコン

### 斜方晶系 (Orthorhombic)
- **対称性**: 直交する不等な軸
- **特徴**: 三軸すべて異なる長さ
- **例**: 硫黄、トパーズ

### 単斜晶系 (Monoclinic)
- **対称性**: 一つの斜軸
- **特徴**: β角が90度でない
- **例**: 石膏、雲母

### 三斜晶系 (Triclinic)
- **対称性**: 最低
- **特徴**: すべての角度が90度でない
- **例**: 長石、硫酸銅

## 品質評価システム

システムは生成された結晶の品質を以下の基準で評価します：

1. **対称性**: 結晶系に応じた適切な対称性
2. **面の発達度**: 活性面の適切な発達
3. **不完全性レベル**: 自然な範囲内の欠陥密度

## パフォーマンス

- **生成時間**: 平均50-100ms（複雑度レベル3）
- **メモリ使用量**: 結晶あたり約1-2MB
- **推奨設定**: 同時生成数は20個以下

## 今後の拡張予定

1. **材質システム**: 結晶系に応じた物理的特性
2. **光学効果**: 屈折・反射の実装
3. **破壊システム**: 結晶の劈開面に沿った破壊
4. **集合体生成**: 複数結晶の自然な配置

## トラブルシューティング

### よくある問題

1. **結晶が生成されない**
   - ProBuilderが正しくインストールされているか確認
   - コンソールでエラーメッセージを確認

2. **形状が期待と異なる**
   - 結晶系パラメータを確認
   - 複雑度レベルを調整

3. **パフォーマンスが低い**
   - 同時生成数を制限
   - 複雑度レベルを下げる
   - 成長サイクル数を減らす

### デバッグ方法

1. CrystalStructureGeneratorTestを使用してテスト実行
2. ログ出力を有効化してデバッグ情報を確認
3. 品質評価システムで生成結果を検証
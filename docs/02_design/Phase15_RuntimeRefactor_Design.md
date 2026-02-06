# Phase 1.5: Runtime 責務整理デザインドキュメント

## 概要

V01 地形生成システムのランタイムコンポーネントの責務を明確化し、
将来の拡張（Phase 2 Template 系、Phase 2.5 TerrainEngine Lite）に備えた設計を行う。

---

## 現状分析

### V01 コアコンポーネント

| コンポーネント | ファイル | 行数 | 責務 |
|---------------|---------|------|------|
| TerrainGenerator | MapGenerator/Scripts/TerrainGenerator.cs | 320 | 地形生成の統括、Terrain オブジェクト作成 |
| HeightMapGenerator | MapGenerator/Scripts/HeightMapGenerator.cs | 157 | 高さマップ生成（Noise/HeightMap/Combined） |
| TerrainGenerationProfile | Scripts/Generation/TerrainGenerationProfile.cs | 286 | 生成パラメータの保存・読み込み |
| TerrainGenerationConstants | Scripts/Generation/TerrainGenerationConstants.cs | 117 | 定数値の一元管理 |
| TerrainGenerationMode | Scripts/Generation/TerrainGenerationMode.cs | 19 | 生成モード enum |

### ヘルパーコンポーネント

| コンポーネント | 責務 |
|---------------|------|
| TextureGenerator | テクスチャレイヤー設定 |
| DetailGenerator | ディテールマップ設定 |
| TreeGenerator | ツリー配置設定 |
| TerrainOptimizer | Terrain 最適化設定 |

---

## 責務分析

### TerrainGenerator（現状）

```text
責務:
1. TerrainData 作成
2. HeightMapGenerator 呼び出し
3. Terrain GameObject 作成
4. マテリアル設定
5. テクスチャレイヤー設定（委譲）
6. ディテールマップ設定（委譲）
7. ツリー設定（委譲）
8. 最適化設定（委譲）
9. Profile からの読み込み / Profile への保存
```

**評価**: 責務は多いが、ヘルパークラスに適切に委譲されている。
バッチ処理（SetHeightsInBatches）も内部で完結。単一責任原則に概ね適合。

### HeightMapGenerator（現状）

```text
責務:
1. Noise ベース高さマップ生成
2. HeightMap テクスチャからの高さマップ生成
3. Noise と HeightMap の合成
```

**評価**: 高さマップ生成に特化。単一責任原則に適合。

### TerrainGenerationProfile（現状）

```text
責務:
1. 生成パラメータの保持
2. バリデーション（解像度、範囲制限）
3. シード値ランダム化
4. デフォルト値リセット
```

**評価**: データ保持とバリデーションに特化。単一責任原則に適合。

---

## 将来の拡張ポイント

### Phase 2: Template 系との連携

```text
DesignerTerrainTemplate
    ↓ 選択
TemplateBrowser / TemplateEditor
    ↓ パラメータ抽出
TerrainGenerationProfile (拡張)
    ↓ 生成指示
TerrainGenerator (既存)
```

**設計方針**:

- `TerrainGenerationProfile` に Template 参照フィールドを追加（オプショナル）
- `TerrainGenerator` は Profile 経由で Template パラメータを受け取る
- Template 専用の生成ロジックは別クラス（TemplateTerrainGenerator）に分離

### Phase 2.5: TerrainEngine Lite 統合

```text
TerrainEngine (TerrainEngineMode)
    ↓ モード判定
    ├─ TemplateOnly → TemplateTerrainGenerator
    ├─ ProceduralOnly → TerrainGenerator (V01)
    └─ Hybrid → 両方を組み合わせ
```

**設計方針**:

- `TerrainEngine` は `TerrainGenerator` のファサードとして機能
- `TerrainEngineMode` に基づき適切な生成器を選択
- V01 の `TerrainGenerator` はそのまま維持

---

## リファクタリング候補

### 優先度: 高

| 項目 | 現状 | 提案 |
|------|------|------|
| なし | - | V01 コンポーネントは品質良好 |

### 優先度: 中

| 項目 | 現状 | 提案 |
|------|------|------|
| HeightMapGenerator のテスト | テストなし | ユニットテスト追加 |
| TerrainGenerator のテスト | テストなし | 統合テスト追加 |

### 優先度: 低（Phase 2 以降）

| 項目 | 現状 | 提案 |
|------|------|------|
| BlendSettings 重複 | 2箇所に存在 | 統合または明確な分離 |
| TemplateTerrainGenerator | 未実装 | Phase 2 で新規作成 |

---

## インターフェース設計（将来）

```csharp
namespace Vastcore.Generation
{
    /// <summary>
    /// 地形生成器の共通インターフェース
    /// </summary>
    public interface ITerrainGenerator
    {
        /// <summary>
        /// 地形を生成する
        /// </summary>
        /// <param name="profile">生成プロファイル</param>
        /// <returns>生成された Terrain コンポーネント</returns>
        IEnumerator GenerateTerrain(TerrainGenerationProfile profile);
        
        /// <summary>
        /// 生成された Terrain
        /// </summary>
        UnityEngine.Terrain GeneratedTerrain { get; }
    }
    
    /// <summary>
    /// 高さマップ生成器の共通インターフェース
    /// </summary>
    public interface IHeightMapProvider
    {
        /// <summary>
        /// 高さマップを生成する
        /// </summary>
        /// <param name="profile">生成プロファイル</param>
        /// <param name="resolution">解像度</param>
        /// <returns>高さマップ配列</returns>
        float[,] GenerateHeightMap(TerrainGenerationProfile profile, int resolution);
    }
}
```

**導入時期**: Phase 2 開始時に検討

---

## 結論

### V01 コンポーネントの評価

- **TerrainGenerator**: ✅ 品質良好、リファクタリング不要
- **HeightMapGenerator**: ✅ 品質良好、リファクタリング不要
- **TerrainGenerationProfile**: ✅ 品質良好、リファクタリング不要
- **TerrainGenerationConstants**: ✅ 品質良好、必要に応じて定数追加

### 推奨アクション

1. **V01 のまま維持**: 現行コンポーネントは拡張に耐えうる設計
2. **テスト追加**: Phase 1.5 でユニットテスト・統合テストを追加
3. **インターフェース導入**: Phase 2 開始時に `ITerrainGenerator` を導入し、多態性を確保

---

## 関連ドキュメント

- [TerrainGenerationV0_Spec.md](../terrain/TerrainGenerationV0_Spec.md) - V0 仕様書
- [V01_TestPlan.md](../terrain/V01_TestPlan.md) - V01 テスト計画
- [Handover_Nov20.md](../progress/Handover_Nov20.md) - 申し送りノート

---

- **作成日**: 2025-11-26
- **作成者**: Cascade (AI)

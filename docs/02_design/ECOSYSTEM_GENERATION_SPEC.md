# Ecosystem Generation 仕様書

- **最終更新日時**: 2026-03-08
- **ステータス**: Draft
- **難易度**: 低〜中
- **前提**: Phase C 完了

---

## 1. 目的

バイオーム・気候データに基づき、植生・樹木を生態学的に妥当な密度・分布で自動配置する。
既存の DetailGenerator / TreeGenerator を BiomePreset / ClimateData と連携させ、
WorldGenRecipe パイプラインに統合する。

---

## 2. 既存インフラ

| コンポーネント | 状態 | 役割 |
|--------------|------|------|
| BiomePreset | 動作中 | moisture, temperature, fertility, rockiness (0-1) |
| BiomeTypes | 動作中 | 6種 (Grassland/Forest/Desert/Mountain/Polar/Coastal) |
| ClimateData | 動作中 | 温度/湿度/風/季節/海洋距離 |
| DetailGenerator | 動作中 | 高さ/傾斜フィルタで草を配置 |
| TreeGenerator | 動作中 | グリッド+ジッターで樹木配置、高さ/傾斜フィルタ |
| IDensityField | 動作中 | Sample(Vector3) → float インターフェース |
| FieldLayerType | 動作中 | Heightmap/NoiseDensity/Cave/SDF (4種) |
| CompositeDensityField | 動作中 | IDensityField のブール合成 |
| WorldGenRecipe | 動作中 | ScriptableObject、レイヤー駆動生成 |

---

## 3. アーキテクチャ

```
WorldGenRecipe
  └─ FieldLayer (type: Ecosystem)
       └─ EcosystemDensityField : IDensityField
            ├─ BiomePreset (moisture/fertility → 密度)
            ├─ ClimateData (温度/降水量 → 種類選択)
            └─ TerrainData (高さ/傾斜 → 配置制約)
                    |
                    v
            DetailGenerator / TreeGenerator
            (密度値に基づく配置)
```

---

## 4. フェーズ分割

### Phase E1: BiomePreset 連携 (4h)
- TreeGenerator に BiomePreset 参照を追加
- バイオーム別パラメータ:
  - treeDensity: fertility * moisture → 0~1
  - treeTypes: BiomeType に応じたプリファブリスト
  - maxSlope: バイオーム別 (Forest=30, Mountain=45 等)
- DetailGenerator に humidity フィルタ追加
  - grassDensity *= moisture (乾燥地帯で草を減少)

### Phase E2: IDensityField 統合 (6h)
- FieldLayerType に `Ecosystem` を追加
- EcosystemDensityField クラス作成:
  ```csharp
  public class EcosystemDensityField : IDensityField
  {
      BiomePreset biome;
      ClimateData climate;

      public float Sample(Vector3 pos)
      {
          float base = biome.fertility * biome.moisture;
          float tempFactor = Mathf.InverseLerp(-10, 30, climate.temperature);
          return base * tempFactor;
      }
  }
  ```
- FieldEngineManager.CreateLayerField() の switch に追加

### Phase E3: 種類選択ロジック (4h)
- VegetationProfile ScriptableObject:
  - List<VegetationEntry> entries (プリファブ + 温度範囲 + 湿度範囲 + 密度係数)
- BiomeType → デフォルト VegetationProfile マッピング
- TreeGenerator / DetailGenerator が VegetationProfile を参照

### Phase E4: 季節変動 (4h, オプション)
- SeasonalData → 密度係数 (春1.0 / 夏1.2 / 秋0.8 / 冬0.3)
- 落葉表現: 冬に DetailGenerator 密度 → 0
- Climate Visual Integration (別仕様) との連携

---

## 5. リスク

- DetailGenerator / TreeGenerator はランタイム生成時に呼ばれるため、パフォーマンス影響あり
  - 対策: EcosystemDensityField の Sample() を軽量に保つ (三角関数回避)
- BiomePreset のパラメータ範囲 (0-1) と ClimateData の範囲 (-50~50等) の正規化が必要
- VegetationProfile の適切なデフォルト値はアーティスト判断が必要

---

## 6. 完了条件

- [ ] Phase E1: Forest バイオームで樹木が密に、Desert で疎に配置されること
- [ ] Phase E2: WorldGenRecipe に Ecosystem レイヤーが追加・動作すること
- [ ] Phase E3: 温度/湿度に応じて異なる植生プリファブが選択されること

# Climate Visual Integration 仕様書

- **最終更新日時**: 2026-03-08
- **ステータス**: Draft
- **難易度**: 低〜中
- **前提**: Phase C 完了

---

## 1. 目的

既存の ClimateSystem が算出する気候データ（温度・湿度・風・季節）を、
テレインの視覚表現にリアルタイム反映する。
ClimateSystem 自体は改変せず、一方向のデータブロードキャストで実現する。

---

## 2. 既存インフラ

| コンポーネント | 状態 | 役割 |
|--------------|------|------|
| ClimateSystem | 動作中 (LEGACY マーク) | GetClimateAt(Vector3) → ClimateData |
| ClimateData | 動作中 | 11フィールド構造体 (温度/湿度/風/季節等) |
| TerrainTexturingSystem | 動作中 | 高度/傾斜ベース4層テクスチャ + BiomePreset 色調 |
| BiomeMaterialSettings | 動作中 | terrainTint, ambientColor |
| ClimateTerrainFeedbackSystem | スタブのみ | ハードコード値を返す (未実装) |

---

## 3. アーキテクチャ

```
ClimateSystem.GetClimateAt(pos)
        |
        v
ClimateVisualBridge (新規 MonoBehaviour)
        |
        +---> Shader.SetGlobalXxx()       -- 気候ユニフォーム
        +---> MaterialPropertyBlock       -- タイル別色調
        +---> ParticleSystem              -- 天候パーティクル
        +---> URP Volume                  -- フォグ密度/色
```

**設計原則**: ゼロカップリング。ブリッジが存在しなくても既存システムに影響なし。

---

## 4. フェーズ分割

### Phase V1: Shader Globals (3h)
- ClimateVisualBridge MonoBehaviour 作成
- OnClimateDataUpdated / OnSeasonChanged にサブスクライブ
- 以下のグローバル変数をブロードキャスト:
  - `_VCTemperature` (float, -50~50)
  - `_VCMoisture` (float, 0~5000)
  - `_VCHumidity` (float, 0~100)
  - `_VCWindDirection` (Vector4)
  - `_VCWindSpeed` (float)
  - `_VCSeasonProgress` (float, 0~1)

### Phase V2: テレイン色調 (4h)
- 季節 → terrainTint マッピング (春:緑/夏:深緑/秋:橙/冬:白)
- TerrainTexturingSystem の Snow レイヤー閾値を温度連動に変更
  - 現在: 固定高度 120-300m
  - 変更後: `snowLine = baseAltitude - temperature * factor`

### Phase V3: 風アニメーション (6h)
- 草/植生用頂点シェーダー作成
- `_VCWindDirection` / `_VCWindSpeed` で頂点オフセット
- 高さに比例した揺れ強度

### Phase V4: 天候パーティクル (8h)
- 雨: humidity > 閾値で ParticleSystem 有効化、rate = humidity 連動
- 雪: temperature < 閾値で切り替え
- 砂塵: 砂漠バイオーム + windSpeed > 閾値

### Phase V5: フォグ (10h, オプション)
- URP Volume Profile の Fog 密度を moisture 連動
- Fog 色を temperature + 季節で変化

---

## 5. リスク

- ClimateSystem が LEGACY マークだが機能は完全。本仕様で依存しても問題なし
- ClimateTerrainFeedbackSystem のスタブは本仕様では不要（一方向ブロードキャストのため）
- Phase V3 以降はカスタムシェーダーが必要。URP ShaderGraph または HLSL

---

## 6. 完了条件

- [ ] Phase V1: 任意のシェーダーで `_VCTemperature` が読めること
- [ ] Phase V2: 季節変化でテレイン色が変わること
- [ ] Phase V3: 風速に応じて草が揺れること
- [ ] Phase V4: 気候条件に応じて天候が変化すること

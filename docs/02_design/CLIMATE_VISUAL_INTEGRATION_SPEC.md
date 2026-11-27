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

## 7. 地上景観の気象パッケージ

2026-11-28 の依頼は、街路から地平線まで地形と気象で縮尺を見せること、複数の天候を組み替えられること、今回外した案を次の自然地形パッケージへ残すこと。`Packages/com.vastcore.weather` はシーンで気象の位置・向き・時刻を指定する。1 Unity unit を 1 m として扱う。上記の ClimateSystem 接続と地域探索への取込は別工程。

| プリセット | 地形と気象の組み合わせ | 画面で確かめること |
|---|---|---|
| 街路・局地雷雨 | 側溝脇の水たまり。短い時間で通過する雨域を雨脚・滴・雷・濡れに共有 | 同じ角度で接近前・降雨中・通過後を比べる。雨域外は乾き、通過後は波紋が消えて水が残る。[気象庁の局地的大雨](https://www.jma.go.jp/jma/kishou/know/yougo_hp/kousui.html)・[雨滴の着水実験](https://pure.tue.nl/ws/files/1414355/Metis133016.pdf) |
| 山岳の谷霧 | 冷えた空気が流れ込む夜明けの谷。高さの異なる霧の体積を谷筋に置く | 尾根の重なり、谷底の霞、霧の中に入ったときの視程。[NWS](https://www.weather.gov/safety/fog-mountain-valley) |
| 海・山の向こうの積乱雲 | 共通の塔・かなとこを対岸と稜線裏に配置。雨脚は塔の下 | 遠方の地形が雲底を隠し、雲頂と風下のかなとこで高さ・向きが分かる。[気象衛星センターの対岸写真とレーダー](https://www.data.jma.go.jp/mscweb/ja/prod/pattern_04.html) |
| モーニング・グローリー | 低い海岸の夜明け。先頭ロールは長さ 106 km、幅 1.82 km、厚さ 1.12 km。約 12 m/s で軸に直交して進む | 地平線と雲底の間に空があり、斜めから先頭と後続を読める。[現地観測と写真](https://www.meteo.physik.uni-muenchen.de/~roger/AustralianProjects/TheMorningGlory/TheMorningGlory.html) |
| 創作・稜線の雲帯 | 高さの違う3つのレンズ状の帯。輪郭を山に留め、内部の流れる向きを層ごとに変える | 帯に厚みを持たせ、地形への固定と内部の流れが別々に見える。[山岳波に伴うレンズ雲](https://www.weather.gov/abq/features_acsl) |
| 大平原・スーパーセル | 傾いた回転上昇流、底の低い雲、風下側の雨脚を分ける。手前に道と電柱 | 視点を変えても非対称な構造と地上の縮尺が読み取れる。[NWS の構造と写真](https://www.weather.gov/spotterguide/supercell2) |

### Unity で使う

- `Vastcore > Weather > Open Environment Sample` から開き、Play で地点、3視点、晴天比較、seed、時刻を切り替える。`Street thunderstorm` は 42 秒で街路に雨が入り、77 秒で水たまりが残る。`Play time` を押すと時刻が進む。
- 自分のシーンでは `WeatherField` に `WeatherPreset` を設定する。サンプルカメラの Inspector で Site を変え、`Vastcore > Weather > Save Current Preset` で編集できる Asset を作る。体積の位置・寸法・向き・密度と、雨域の範囲・移動・乾燥時間を組み替えられる。URP カメラで Depth Texture を有効にし、不透明パスの後に深度をコピーする。雨滴を表示するカメラと太陽を割り当てる。`WeatherPuddle` は側溝などの平らなくぼみに置き、サイズと参照する `WeatherField` を設定する。
- 既存の Weather Maker 8.0.4 には濡れ・雨ゾーン・嵐のデモがある。導入済み端末で比較できる。気象パッケージは形・遮蔽・通過を複数視点から確かめるために配置可能な体積を使う。背景素材は視点が固定された遠景、Weather Maker は既設の空と一般天候を使う構成の候補。

### Unity の結果と次の素材

- Unity 6000.3.6f1／URP／Direct3D 11 で7プリセットを撮影した。各地点で同じ視点の晴天と気象あり、追加2視点、街路の降雨前後と遠景の時刻差を比較。Play Mode はメニューから開き、街路の降雨・残留濡れと、seed 53 のモーニング・グローリーへの切替を確認した。[構図ラフ・初回と最終の画面・計測](<D:/AI-Runs/VastCore/weather-environments/index.html>)
- 初回のスーパーセルは手前の電柱に重なった。体積の入口も不透明物の深度と比べて遮蔽を直した。積乱雲は雲塔を太くして対岸と稜線に重ね、谷霧は斜面の境で薄くした。雨脚のサンプル数を8〜16に絞り、同じ街路画面で雨脚と波紋の両方を確認した。
- 1600×900、RTX 4060 Ti で晴天に対する増分は0.62〜3.40 ms、場面生成は9.87〜40.41 ms。カメラ描画と同期ピクセル読み戻しの中央値を7回から計算。Player のGPU計測と実地形での性能は未確認。
- 差が大きいのは低い視点での街路の建物反射、後続ロールが先頭の陰に入る角度、スーパーセルの渦と雨脚の内部の濃淡。次は側溝の低点と反射対象を持つ舗装、ロールの間隔を変えた別の海岸、雲の内部の陰影と雨脚を比較する。次の自然地形でも晴天／気象ありを同じ視点で撮り、近景の材質不足と遠景の輪郭・遮蔽不足を特定してから素材を加える。

### 自然地形へ持ち越す候補

- 雨の次は舗装の低点・側溝から水たまりを配置する仕組み、植生や崖の湿り、雨陰・風上斜面の霧。海岸では海霧と雨の境界を試せる。必要な地形入力は高さ、低点、風上の遮蔽、水域の外洋への接続。[自然地形パッケージ](NATURAL_ENVIRONMENT_PACKAGES_SPEC.md)
- 雪と吹雪は降下・堆積・融雪の時間差、砂埃と砂嵐は乾いた地表・吹き出す位置・視程を設計する候補。ひょう、海上の渦状の雲、雷鳴は別の拡張候補。地域の2D雲に出す段階では、地上と同じ気象の位置・範囲・風向・時刻を渡す。

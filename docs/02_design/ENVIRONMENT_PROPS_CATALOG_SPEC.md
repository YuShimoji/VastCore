# 環境用 Props カタログ・制作基準・バリエーション設計仕様 (DS-011)

- **作成日**: 2026-11-27
- **ステータス**: active (設計・3Dアセット制作基準)
- **上位仕様**: [SSOT_WORLD.md](../SSOT_WORLD.md)
- **関連仕様**: [REGION_TERRAIN_LOOK_SPEC.md](REGION_TERRAIN_LOOK_SPEC.md) / [REGION_SURFACE_MATERIALS_SPEC.md](REGION_SURFACE_MATERIALS_SPEC.md) / [SP018_PARAMETRIC_VARIATION_SPEC.md](SP018_PARAMETRIC_VARIATION_SPEC.md) / [BUILDING_DEFINITION_SPEC.md](BUILDING_DEFINITION_SPEC.md) / [BUILDING_STRUCTURE_INVENTORY.md](BUILDING_STRUCTURE_INVENTORY.md)
- **機械可読データ**: [environment-props-catalog.json](environment-props-catalog.json)

---

## 1. 目的と現状ギャップ

VastCore には大型建築やプリミティブ地形生成の枠組み（[BUILDING_STRUCTURE_INVENTORY.md](BUILDING_STRUCTURE_INVENTORY.md)）、3D砂岩景観の転石（[RegionRockField](../../Assets/Scripts/RegionMap/Core/RegionRocks.cs)）・植生4種（[RegionPlantField](../../Assets/Scripts/RegionMap/Core/RegionPlantField.cs)）はあるが、**環境中景〜近景の複雑性を担う環境 Props の体系・リスト・バリエーション基準**が存在しなかった。

本書は、外部の環境アセット体系（Quixel Megascans / Fab の環境キット、Skyrim Creation Kit の分類および Base Object Swapper・SMIM 等のバリエーション手法、AAA 3Dゲームのプロップ制作標準）を構造面で分析し、VastCore の世界観（砂岩・自然地形／古代遺構／中世〜現代〜未来の都市・拠点・産業施設）で迷わず 3D アセットを制作・配置できる**分類体系・技術基準・バリエーション設計・データ定義**を確定する。

> **境界**: [INVARIANTS.md](../INVARIANTS.md) に従い、参照元の固有名・画面意匠は複製せず、分類設計・モジュール化・差分展開の仕組みだけを適用する。

---

## 2. 外部体系の調査分析と VastCore への適用方針

### 2.1 Quixel Megascans (Fab) パッケージ体系の分析

Megascans 系（Medieval Village / Roman Ruins / Quarry / Industrial 等）では、単発の小道具を並べるのではなく、**1テーマにつき4層を1セット**で揃えることで接地感と密度を両立している。

| 層 | Megascans での構成要素 | 役割 | VastCore での対応 |
|---|---|---|---|
| 1. Structural Assemblies | 壁片・柱・梁・階段・配管・レール | 空間の骨格と導線を作る | `STR`（構造・境界・導線）＋ モジュラー建築キット |
| 2. Hero / Standard Props | 木箱・樽・荷車・機械・家具・コンテナ | 場所の用途と生活・産業痕を示す | `CNT`（容器）/ `MAC`（設備）/ `SET`（拠点・生活） |
| 3. Debris & Rubble | 瓦礫山・煉瓦片・木片・砂利塊・倒木 | 構造物の足元を隠し、自然地形と繋ぐ | `RUB`（瓦礫・崩壊）/ `NAT`（自然近景補完） |
| 4. Atlases & Decals | 枯葉・草株・紙屑・ひび割れ・汚れ・水染み | テクスチャの繰り返しと直線の境界を消す | 地面混合・TopDust・デカール/スカート補完 |

### 2.2 Skyrim Creation Kit / Mod パッケージ体系の分析

Skyrim（Creation Kit + SMIM + Ruins Clutter Improved + Base Object Swapper 系）は、広大なオープンワールドで負荷を抑えつつ手置きと自動配置の両面で密度を出すため、以下の3原則を採っている。

1. **挙動・衝突クラスによる分離**:
   - `Static`（建築・大型瓦礫・背景岩: 物理計算なし・バッチング最優先）
   - `MovableStatic` / `Interactive`（揺れる鎖・荷車・開く箱・破壊片: 必要箇所のみ個別制御）
   - `Container` / `Landmark`（木箱・樽・遺物匣・祭壇: 探索・発見の対象になり得る要素）
   - `Clutter`（皿・工具・小石・骨: 物理爆発を防ぐため原則 Static 複合塊として扱う）
2. **Base Object Swapper (BOS) 型の同ID異形展開**:
   - 配置側には論理名（例: `cnt_crate_wood`）だけを置き、生成・配置時に地域・時代・状態に応じて「健全／破損／蓋開き」「材質違い」「中身溢れ」を確率テーブルで差し替える。
3. **Clutter Layering（3段の接地レイヤー）**:
   - 大型構造物（Macro）の周囲に中型 Props（Meso）を寄せ、足元を瓦礫・土砂・小礫スカート（Micro）で埋めることで「地面に刺さった箱」を防ぐ。

### 2.3 VastCore への統合方針（4つの柱）

1. **「単体完成品」ではなく「キット単位」で制作する**: Crate を1個作るときも「本体＋蓋＋補強板＋破損片（スプリント）」を同一テクスチャ・トリムシート上で組む。
2. **全 Props に同じランダムを掛けない**: 人工物（Crate・壁・柵）は**接地・直交・積み上げ整合**を保ったうえで「組み方・損傷・材質」を変え、自然物・瓦礫（Rubble・岩塊）は**広がり方・埋没深度・傾き・疎密**を変える。
3. **既存パイプラインへの直結**:
   - [SP-018](SP018_PARAMETRIC_VARIATION_SPEC.md) の `ScaleRange` / `RotationMode` / `PositionJitter` / `MaterialVariants` / `ChildToggleGroups` をそのまま Props バリエーションの第1層として使う。
   - [SP-019](BUILDING_DEFINITION_SPEC.md) の `StructureTagProfile` と同じフラットタグで時代・用途・バイオーム親和度を記述する。
   - [REGION_SURFACE_MATERIALS_SPEC.md](REGION_SURFACE_MATERIALS_SPEC.md) の砂塵・湿り気・鏡面抑制（乾いた材質は鏡面弱め）を共有し、3D砂岩地形上で浮かないようにする。

---

## 3. 3Dアセット制作技術基準（迷わないための統一規格）

3Dモデリング・UV・Unity 取込時に毎回判断がぶれないよう、数値基準を以下に固定する。詳細は [environment-props-catalog.json](environment-props-catalog.json) の `technicalStandards` を正本とする。

### 3.1 スケール・グリッド・ピボット規則

| 区分 | スケール基準 | ピボット（原点）位置 | 接地・埋没ルール |
|---|---|---|---|
| **積載・直立 Props**（Crate, Barrel, Drum, Chest, Furniture） | 1 unit = 1 m。外寸は `0.25m` / `0.5m` / `1.0m` のモジュール倍数（例: Crate `1.0×1.0×1.0m`, `0.5×0.5×0.5m`） | **底面中心** `(0, minY, 0)`。Z+ または X+ を正面に統一 | 傾いた地形でも直立〜最大 `6°` 追従。足元埋没は `0〜0.03m` |
| **モジュラー構造片**（Wall, Pillar, Fence, Pipe, Rail） | 幅 `1.0 / 2.0 / 4.0m`、高さ `1.0 / 2.0 / 3.0m` のグリッド整合 | **底面の接合端**または**底面中心**（キット内で統一） | 壁下端に `0.15m` の基礎スカートを持たせ斜面浮きを防止 |
| **瓦礫・破片塊**（Rubble Pile, Debris Cluster, Scree） | 塊径 `1.0〜6.0m`、小破片 `0.15〜0.8m` | **重心直下の接地平面** `(centerX, contactY, centerZ)` | 傾斜面追従最大 `28°`。下面に `0.08〜0.20m` の埋没スカート必須 |
| **自然補完**（Stratified Boulder, Driftwood, Pebble Cluster） | `0.2〜8.0m`（既存 `RegionRockField` の `0.2〜9m` と連続） | **重心直下の底面 10〜20% 上方**（斜面に深く刺せる位置） | 地表への埋没 `15〜35%` を前提に底面メッシュも閉じる |

### 3.2 テクセル密度・UV・シェーダー方針（Unity URP / Linear）

- **テクセル密度目標**:
  - **Tier S（近景小物 `<0.8m`）**: `512 px/m`（トリムシートまたは 1K/2K アトラス共有）
  - **Tier M（標準 Prop `0.8〜3.0m`）**: `512 px/m`（主役は固有 1K〜2K、量産品は 2K トリムシート共有）
  - **Tier L（大型瓦礫・構造モジュール `3.0〜8.0m`）**: `256 px/m`（2K タイリング材質＋頂点カラー/マスク）
  - **Tier XL（巨大遺構・ランドマーク `>8.0m`）**: `128 px/m`＋World/Triplanar 詳細マスク
- **マテリアル・色空間**:
  - プロジェクト設定は **Linear**（`2026-11-27` 確定）。アルベドの暗部は sRGB `30〜40`（Linear `0.015〜0.025`）を下回らない。
  - **環境順応シェーダー入力（推奨）**: 全 Props 共通で上面マスク（`WorldNormal.y`）による**砂塵・雪・苔の堆積（Top Layer）**と、接地近傍（`LocalY < 0.25m`）の**土砂なじませ（Ground Skirt Blend）**を受け取れる設計にする。

### 3.3 LOD・ポリゴン数・コライダー基準

| Tier | 代表サイズ | LOD0 三角形目安 | LOD1 / LOD2 比率 | カリング距離目安 | コライダー方針 |
|---|---|---|---|---|---|
| **S (Micro)** | `0.1〜0.8m` | `150〜1,200` | `50% / なし` | `40〜80m` | 装飾破片は **Collider なし**。容器のみ Box/Capsule |
| **M (Standard)** | `0.8〜3.0m` | `800〜4,000` | `50% / 20%` | `120〜250m` | Simple Box / Capsule（積み上げ対象は外寸に厳密一致） |
| **L (Large)** | `3.0〜8.0m` | `2,500〜10,000` | `45% / 15%` | `350〜800m` | 複数 Box 複合 または 128〜256 tris Convex Hull |
| **XL (Landmark)** | `>8.0m` | `8,000〜30,000` | `45% / 15% / 5%` | `1,500m〜` | 歩行可能面のみ専用低ポリ MeshCollider、他は Box 複合 |

---

## 4. 環境 Props 体系化リスト（全7ドメイン・38原型アセット）

VastCore の現行3D景観（砂岩・乾燥地）と今後の展開予定（古代遺構／中世集落／現代工場・基地・都市／未来施設）を過不足なく覆うため、**7ドメイン・38原型アセット**（P0:17 / P1:10 / P2:11）に体系化する。各アセットの完全な数値パラメータ・タグ・材質・確率は [environment-props-catalog.json](environment-props-catalog.json) に定義する。

| ドメイン | 対象領域 | 代表ID | 優先度(P0:砂岩/探索基礎, P1:遺構/拠点, P2:都市/産業) |
|---|---|---|---|
| **NAT** | 自然近景補完 | `nat_sand_drift`, `nat_stratified_slab`, `nat_pebble_cluster`, `nat_dry_root_log`, `nat_cliff_overhang_cap` | P0（既存 `RegionRockField` の多面体岩を補う層状岩・吹きだまり・流木） |
| **RUB** | 瓦礫・崩壊 | `rub_stone_rubble_pile`, `rub_masonry_blocks`, `rub_concrete_debris`, `rub_wood_splinters`, `rub_scrap_metal_pile` | P0〜P2（構造物の根元・崖下・廃墟の接地境界を馴染ませる要） |
| **STR** | 構造・境界 | `str_ruin_pillar`, `str_ruin_wall_segment`, `str_stone_stair_slab`, `str_wooden_fence`, `str_metal_barricade`, `str_scaffold_frame`, `str_utility_pole` | P0〜P2（導線・視線誘導・遮蔽・通行制御） |
| **CNT** | 容器・積荷 | `cnt_wood_crate`, `cnt_metal_cargo_container`, `cnt_wood_barrel`, `cnt_steel_drum`, `cnt_fabric_sack`, `cnt_supply_pallet`, `cnt_relic_chest` | P0〜P2（時代別・材質別・組み方別の差異が最も効く補給/探索群） |
| **MAC** | 設備・機械 | `mac_field_generator`, `mac_pipe_module`, `mac_antenna_relay`, `mac_ore_cart_rail`, `mac_solar_array` | P1〜P2（工場・基地・採掘場・現代/未来拠点の説得力） |
| **SET** | 生活・野営 | `set_expedition_tent`, `set_campfire_hearth`, `set_work_table_bench`, `set_trail_signpost`, `set_lantern_beacon` | P0〜P1（人間のスケール感と「誰かが居た/居る」物語性） |
| **EXP** | 探索・遺構 | `exp_monolith_fragment`, `exp_stone_altar_pedestal`, `exp_survey_marker`, `exp_carved_relief_stele` | P0〜P1（SP-021 探索地点・宝探し目標と直接結びつく目印群） |

### 4.1 制作順（ボトルネック駆動の推奨優先度）

1. **P0（17件・最優先：3D砂岩＋探索拠点の即時品質向上）**:
   - `nat_stratified_slab`, `nat_sand_drift`, `nat_pebble_cluster`, `nat_dry_root_log`, `nat_cliff_overhang_cap`
   - `rub_stone_rubble_pile`, `rub_masonry_blocks`
   - `str_ruin_pillar`, `str_ruin_wall_segment`, `cnt_wood_crate`, `cnt_wood_barrel`, `cnt_fabric_sack`
   - `set_expedition_tent`, `set_campfire_hearth`, `set_trail_signpost`, `exp_monolith_fragment`, `exp_survey_marker`
2. **P1（10件・中世集落・古代遺跡・採掘拠点の拡張）**:
   - `rub_wood_splinters`, `str_stone_stair_slab`, `str_wooden_fence`, `cnt_supply_pallet`, `cnt_relic_chest`
   - `mac_ore_cart_rail`, `set_work_table_bench`, `set_lantern_beacon`, `exp_stone_altar_pedestal`, `exp_carved_relief_stele`
3. **P2（11件・現代/未来・工場/基地/空港の展開時）**:
   - `rub_concrete_debris`, `rub_scrap_metal_pile`, `str_metal_barricade`, `str_scaffold_frame`, `str_utility_pole`
   - `cnt_metal_cargo_container`, `cnt_steel_drum`, `mac_field_generator`, `mac_pipe_module`, `mac_antenna_relay`, `mac_solar_array`

---

## 5. Props 別ランダムバリエーション設計（7つの直交軸）

**原則**: ランダム化は「見た目の反復を消す」ことと「物理・構造の破綻（浮き・めり込み・傾きすぎた箱）を起こさない」ことを両立させるため、以下の **7軸** を Prop ごとに明示的に許可／禁止する。

| 軸ID | 軸名称 | 制御パラメータ例 | 適用方法（Unity 側） |
|---|---|---|---|
| **V1** | Transform（姿勢・寸法） | `positionJitter`, `yawRange`, `maxSlopeTiltDeg`, `scaleRange`, `nonUniformScaleMax` | 配置ソルバ / `PrefabStampDefinition` |
| **V2** | Damage（損傷段階） | `pristine`, `worn`, `damaged`, `ruined`（欠け・板抜け・凹み・折れ） | `ChildToggleGroups` または破損別バリアントメッシュ |
| **V3** | Construction（組み方・構造） | 板の張り方・補強枠・蓋の開閉・積み段数・ジョイント角 | サブメッシュのモジュール切替 / 複合レシピ |
| **V4** | Material & Weathering（材質・経年） | `MaterialVariants`（生木/古木/塗装/金属）、Per-Instance 色相・汚れ・縁摩耗 | `MaterialVariants` + `MaterialPropertyBlock` |
| **V5** | Contents & Attachments（中身・付属物） | 縄・留め具・タグ・溢れ出た中身・上に載る小物 | オプション子オブジェクトの確率トグル |
| **V6** | Cluster & Spread（群生・広がり方） | `Single`, `StackedGrid`, `Huddled`, `RadialScatter`, `DirectionalCone`, `WallSkirt` | 複合配置ルール（コア＋周辺散乱の2段階配置） |
| **V7** | Biome Overgrowth（環境順応） | `TopDust`（砂塵）, `TopSnow`（雪）, `MossMask`（苔）, `GroundSkirt`（埋没） | シェーダーのグローバル/領域パラメータ連動 |

### 5.1 代表 Props のバリエーション詳細仕様

#### A. Crate（木箱・貨物箱 `cnt_wood_crate` / `cnt_metal_cargo_container`）
- **やってはいけないこと**: 強いランダム回転X/Z（斜めに傾いた木箱は壊れたか物理演算ミスに見える）、不均一スケールの過剰適用（板厚が歪む）。
- **与えるべきバリエーション**:
  - **V3 組み方（Construction、必須5型）**:
    1. `slat_open`: 隙間のあるすのこ枠組み（中の荷物が透けて見える）
    2. `plank_closed`: 隙間なく板を張った密閉箱
    3. `framed_reinforced`: 対角筋交い（X字/Z字ブレース）＋角補強金具付き
    4. `lid_ajar_or_open`: 蓋が外れた／斜めに立て掛けられた開放型（中身が見える）
    5. `long_weapon_or_gear`: 長尺物用の横長（`1.5×0.5×0.5m`）またはパレット一体型
  - **V4 材質（MaterialVariants）**: `raw_pine`（新しい白木）、`weathered_cedar`（日焼け・割れ木）、`painted_stencil`（軍用/産業用の色塗装＋ステンシル印字）、`iron_banded`（鉄帯補強）。
  - **V2 損傷（Damage）**: `pristine` (40%) / `worn_chipped` (35%) / `broken_slat` (20%, 板1〜2枚欠損) / `collapsed` (5%, 潰れた残骸＋周囲へ木片散乱)。
  - **V6 積み方・広がり方（Cluster）**: 単体置きではなく **`StackedGrid`（パレット上に 2〜4 個を直角〜±12° ずらして2段まで積む）** または **`Huddled`（壁際・テント横に大きさ違いを2〜3個寄せる）** を標準とする。斜面に置く場合は最大 `4°` 傾きまで許容し、それ以上は基礎パレット/土台板を下に敷く。

#### B. Rubble / Debris（瓦礫・崩壊群 `rub_stone_rubble_pile`, `rub_masonry_blocks`, `rub_concrete_debris`）
- **役割**: 直線的な建築・崖・道路と自然地面の境界を壊し、自然物と人工物を繋ぐ。
- **与えるべきバリエーション**:
  - **V6 広がり方（Cluster & Spread、最重要軸）**:
    1. `DirectionalCone`（崩落扇状）: 崖や崩れた壁から一定方向へ、大塊（根元）→ 中塊 → 小片・砂利（先端）の**サイズ勾配**を持って広がる。
    2. `WallSkirt`（壁・基礎沿い帯状）: 壁や柱の足元に沿って帯状に溜まり、直線的な接地境界を隠す。
    3. `RadialImpact`（同心円散乱）: 落石・破壊点の周囲へ中心高・外側低の密度分布で散る。
    - いずれも **Core Pile（大型の複合メッシュ塊 1〜2 個）＋ Mid Fragments（0.3〜0.8m 塊 3〜8 個）＋ Micro Scatter（0.05〜0.25m 破片 10〜25 個）** の3層で広げる。
  - **V1 姿勢・スケール**: Yaw `0〜360°` 完全自由、Pitch/Roll `±18°`（ブロック系は平たい面が下を向きやすいバイアスを掛ける）、均一スケール `0.6〜1.5x`、不均等スケール最大 `±20%`。
  - **V4/V7 材質・順応**: 親構造物と同一の石材/コンクリート材を引き継ぎ、地面との接点 `0〜0.15m` に土砂ブレンド＋`TopDust` を強く掛ける。

#### C. Barrel / Drum（木樽・ドラム缶 `cnt_wood_barrel`, `cnt_steel_drum`）
- **V3 組み方・状態**: 縦置き閉鎖 / 蓋なし開放（液体/砂/資材入り） / **横倒し＋転がり止め楔**（斜面や荷車付近用）。
- **V4 材質**: 木樽＝古材＋鉄タガ（錆度3段階） / 銅・真鍮タガ。ドラム缶＝塗装色4種（黄/赤/オリーブ/灰）× 錆・凹み度合い。
- **V2 損傷**: 木樽＝板1枚割れ・タガ外れ。ドラム缶＝側面の凹み（2パターン）・潰れ・底抜け。
- **V6 群生**: `2〜5本` の小集団（1〜2本が横倒しだと生活感が出る）。

#### D. Ruin Pillar / Wall Segment（遺跡柱・壁片 `str_ruin_pillar`, `str_ruin_wall_segment`）
- **V2 損傷（破断断面キット）**: 完品 / 中段斜め折れ（上部欠損＋近くに折れた柱頭 `Drum` が転がるペア配置）/ 基部のみ残存。
- **V3 構造**: フルーティング（縦溝）あり／無地角柱／砂岩ブロック積み。
- **V6 広がり**: 折れた柱や崩れた壁は**必ず `rub_stone_rubble_pile` または `rub_masonry_blocks` を足元へ自動随伴**させる（単体で地面に刺さない）。

#### E. Fence / Barricade / Signpost（柵・防壁・道標）
- **V3 組み方**: 連続スナップ用エンド/コーナー/ミドル片に加え、**1スパン欠損・傾き片（`±8°`）** を混ぜて直線的単調さを消す。
- **V5 付属物**: 道標なら矢印板の枚数（1〜3枚）・方向角・ランタン吊り下げフック・布/紐の有無を `ChildToggleGroups` で切り替える。

---

## 6. 既存 VastCore システムとの接続マッピング

| VastCore 既存要素 | 本仕様の対応先 | 接続方法 |
|---|---|---|
| `RegionRockField` (`0.2〜9m` 角ばった岩) | `NAT` の自然岩層（特に `nat_stratified_slab`, `nat_pebble_cluster`） | 既存プロシージャル多面体は崖錐・遠中景の量産を担い、近景の層状岩・小石クラスタを本カタログのメッシュで補う |
| `RegionSediment` (`Deposit`, `Scree`, `Wear`) | `RUB` / `NAT` の配置密度・広がり方向 | `Scree` と `Deposit` の勾配ベクトルを `DirectionalCone` 瓦礫の主軸方向に使う |
| `PrefabStampDefinition` (SP-018) | V1 (`PositionJitter`, `ScaleRange`, `RotationMode`), V2/V3/V5 (`ChildToggleGroups`), V4 (`MaterialVariants`) | 1 Prop 原型 = 1 `PrefabStampDefinition`（またはバリアント群）として登録可能 |
| `StructureTagProfile` (SP-019) | JSON の `structureTags` | 既存のフラットタグ（`weathered`, `fortified`, `functional`, `sacred`, `primitive` 等）を継承し、小道具向けに最小限の時代/環境タグを補う |
| `RegionSurfaceMaterials` (T2/T3) | V4 / V7 の砂塵・土砂なじませ | `Surfaces/<Role>` と同じ色相・鏡面抑制（反射率低め）と高さブレンド思想を Props シェーダーへ揃える |

### 6.1 平野・田園の高速道路と一般幹線

本人選択は「平野・田園」。開けた土地を挟んで約4.6 kmずつ道路を通す。高速は上下線を分け、中央分離帯・舗装路肩・ガードレールが続く。一般幹線は側方の短い進入口から砂利敷きの64 m × 42 mの空き地へつながる。寸法の参照は[国土交通省の車線・中央帯・路肩の断面](https://www.mlit.go.jp/road/sign/kouzourei_kaisetsu.html)。画面の参照は遠景の[水原バイパス空撮](https://www.hrr.mlit.go.jp/press/2022/10/221014niikoku.pdf#page=2)、中景の[圏央道沿道](https://www.ktr.mlit.go.jp/oomiya/oomiya00131.html)、近景の[新東名の土工部](https://www.c-nexco.co.jp/corporate/pressroom/news_release/4941.html)。

- Unity の `Vastcore > Region Map > Highway Corridor Preview` で `Seed` と `Road layout` を選び、`Generate preview`。`Country` は片側2車線の高速と片側1車線の幹線、`Sweeping` は同じ断面で曲がりを増やし、`Wide` はそれぞれ片側3・2車線。`Shape only` と遠景・中景・入口・高速の固定視点がある。
- [道路データ](../../Assets/Scripts/RegionMap/Core/RegionRoads.cs)はメートル単位の中心線、高さの取得関数、幅員から縦断と地形のすり付けを作る。[道路部品](../../Assets/Scripts/RegionMap/View/RegionRoadView.cs)は同じ断面を舗装、標示、防護柵、当たり判定に使う。敷地の `RegionRoadsideSite` は、道路ID、中心線に沿った距離、側、入口・敷地の座標と向きを持つ。敷地IDは生成した場面内で一意。探索マップからの道路・敷地呼び出しは別の接続工程。
- 舗装は `Asphalt010`、砂利は `Ground007`、地表は `Grass005` と `Ground009`。中遠景の株と樹木は既存の `DesertPlants` から再利用し、畦と浅い溝の高低、畑の畝、植生をまとめる配置を追加した。[素材の出所](../../Assets/Resources/MapGenerator/Textures/texture-source.json)・[植生の配置](../../Assets/Scripts/RegionMap/View/RegionRoadLandscape.cs)
- 道路脇は舗装端から砂利2.2 m、法面、底幅3 mの浅い溝、畦、田畑へつなぐ。地形格子を4.5 mから2.25 mへ細かくし、[断面と敷地の除外](../../Assets/Scripts/RegionMap/Core/RegionRoadMargin.cs)を地形・植生・路肩に共用する。既存の砂利・土・草を実寸で混ぜ、畦や溝に沿って丈の違う株を群れと空きに分ける。遠景の尾根は[別の粗い地形](../../Assets/Scripts/RegionMap/View/RegionRoadGroundView.cs)で二重に重ねる。
- 自然地形へ持ち越す手順は、既存素材を増やす前に単色の形状と実測断面を確認し、土の高さと同じ境界から植生を配置して、別 seed でも接地と除外範囲を確かめること。畝の色画像は Terrain Lit の光沢を避けるため RGB で作る。写真との差は、溝合流部の丸み、林の樹冠の厚み、近景の草の密度に残る。[4視点・完成想定図・実測の比較](<D:/AI-Runs/VastCore/highway-arterial/roadside-detail/index.html>)
- Unity 6000.3.6f1 の検証用コピーで、Country / 8 と Sweeping / 19 の各版を同じカメラ・日照で比較した。両 seed の修正版は道路179点・入口と敷地80点ずつで突き抜けなし。入口の高さ差は1.14 cm／1.10 cm、敷地の25測点の高低差は表示桁で0.000 cm。生成は0.65→1.88秒／0.68→1.61秒、Mid は+0.86／+0.60 ms（1600×900、CPUとGPU同期込み）。EditMode 6件通過。

---

## 7. 未決事項・本人の判断待ち

- **制作手法の採否**: 本カタログの P0 Props を「Blender 等での手動/プロシージャル造形＋ベイク」で作るか、「既存所有アセット／CC0 スキャン素材（Poly Haven 等）の規格変換」で揃えるか。
- **現代・未来（P2）の着手順**: SP-021 の現行優先（3D砂岩と鳥瞰図詳細化）に合わせ、本仕様では **P0（砂岩・探索拠点・基礎自然/遺構）を先、P1/P2 を後続**としている。
- **モデルの取り込み経路（2026-11-27、本人の選択「A：FBX に変換」）**: GameProjects の主経路はハイブリッド（CC0 スキャン PBR＋構造・反復回避の生成＋近景の主役だけ手編集。GameProjects `docs/decision-log.md` 2026-11-27）。受け取った P0 の GLB 71 個（`D:\AI-Runs\GameProjects\operations\vastcore-handoff-staging-20261127\bundle-full`、175 MB。各ファイルが 2K の画像を埋め込む）は glTF の取り込み手段が無いので Unity で読めない。(A) Blender で形と材質の名前だけの FBX に変換する（`scripts/glb-to-fbx.py`。Packages は変えない。71 個で 12 MB、LFS。画像は 8節の共有の材質）と (B) glTFast を足して GLB を読む（依存の追加、画像がモデルごとに重複）から (A) を選んだ。FBX は `Assets/Art/Models/` に置き、GLB と FBX の sha256・三角形の数は `Assets/Art/Models/model-source.json`。GLB が更新されたら同じスクリプトで変換し直す。

---

## 8. 実アセットの差し替え口（2026-11-27）

Proof Slice（`RegionLookSpec.Props`）に、実モデルで置いた組を足す入口。既定は切（`RegionLookSpec.PropsRealAssets = false`）で、今までの比較と既定の場面は変わらない。入れると同じ配置でプレースホルダの組（`DS011_Props_Proof`）と実アセットの組（`DS011_Props_Real`）を作り、実アセットの組を出す。

| 部分 | 中身 |
|---|---|
| 置き場所 | `Assets/Art/Models/<ドメイン>/<カタログ ID>/<変種>_lod0.*`・`_lod1.*`（lod の無い名前は LOD0 だけ）。Unity が GameObject として取り込めた物だけを数え、対応する実モデルが無い状態はプレースホルダで置く。取り込みの既定は `RegionPropModelImport`（カメラ・光・動き・当たり判定なし、接線を計算、材質は名前だけ） |
| 読み出し | `IRegionPropAssetSource`（Editor は `RegionPropAssetDatabase` が AssetDatabase で読む）。選び方は `RegionPropAssets`（EditMode で試験） |
| 変種 | 損傷・組み方・形の値（`fallen_drum`・`broken_mid`・`intact` など）を変種の名前に探し、無ければ全部から。同じ群れの隣どうしは別の変種（瓦礫を2つ以上並べても同じ物に見えない） |
| 大きさ | 横の大きい方をカタログの寸法×個体の倍率に合わせる。瓦礫（`rub_stone_rubble_pile`・`rub_masonry_blocks`）の GLB は1つで群れ全体（大・中・細の3層）なので、作った大きさのまま CorePile にだけ置き、中と細の個体は置かない。傾きは地面の法線に合わせる |
| 材質（V4） | 元の材質の名前から共有の材質へ: 木は Poly Haven `raw_plank_wall`、砂岩は `worn_rock_natural_01`（粗さの画像が無いので一定 0.88）、砂・灰は既存の ambientCG Ground054 の色。砂岩と砂は地形の岩と土の色へ寄せる（8.1節）。Pale・Warm・Deep・Dark・Charred などは明るさと暖かさで変える。個体ごとの明るさ（`TintShift`）と暖かさは MaterialPropertyBlock |
| 環境順応（V7） | シェーダー `Vastcore/RegionProp`: 上を向いた面に砂塵（`TopDust`）、足元の地面（傾いた平面）から帯の高さ（`GroundSkirtBlendM`、大きい物ほど広い）までを地面の色へ。光と空気は地形と同じ |
| 入口 | `-propsCompare true -propsReal true` で `props-*-real.png` を足して撮る（`region-map.ps1` には未接続。Unity の `-executeMethod Vastcore.RegionMap.Editor.RegionTerrainViewMenu.CaptureFromCommandLine` に直接渡す） |

配置の傾きの符号を直した（`RegionPropPlacementSolver.ResolveTilt`）。前と右の向きを Core の座標で取り違えていて、斜面の向きによって下り側が浮いていた（プレースホルダも同じ）。

### 8.1 比較の結果（2026-11-27、PLANNER007、Unity 6000.3.6f1、Linear、PC 品質、自前の空）

GLB 71 個を Blender 5.2 で FBX に変換し（`scripts/glb-to-fbx.py`）、`Assets/Art/Models/` に置いて撮った（この FBX を7節の選択の後に commit した）。seed 9 は既定の場所、seed 8 は `-lookCenter 82.93144,62.38425`。

| | seed 9 | seed 8 |
|---|---|---|
| 実モデルで置いた個体 | 14（瓦礫は群れ全体のモデル 2、層状岩 4、石柱 1、道標 1、荷置き台 6） | 8 |
| LOD0 の三角形 | 64,572 | 31,628 |
| プレースホルダの組との差（8階調を超える画素） | rubble 22.0%・crates 4.2%・near 2.8% | 13.9%・0.8%・0.1% |

見えたこと: 瓦礫・層状岩の多面体の面が、写真の欠け・層・凹凸に替わり、瓦礫は大・中・細の3層が1つの山として斜面に載る。木箱・樽はモデルが無くプレースホルダのまま。砂岩の写真素材（`worn_rock_natural_01`）は周りの赤い砂岩と地面より淡く黄白に見えた（下の色合わせで直した）。`rubble_low_radial` は幅 0.7 m 前後で、CorePile（1〜6 m）としては小さい。初めの版で瓦礫の中・細の個体（51 個）に群れ全体のモデルを縮めて置き、三角形が 558,512 になったので、群れ全体のモデルは CorePile にだけ置くようにした。

画像は撮影端末の `artifacts/region-map/ds011-real-20261127/`（`s9/`・`s8/` の `props-*.png` と `props-*-real.png`、`pair-*.png` は左右の対、`s9-rubble-zoom.png`）。EditMode 717/717（`RegionPropAssetsTests` 6 件を含む）。

**色合わせ（本人の選択「赤い砂岩に合わせる」、2026-11-27）**: 写真の模様は残し、チャンネルごとの倍率（`RegionPropAssets.StoneMatch`）で平均の色を地形の転がる岩（`RegionSandstone.hlsl` の block の式の平均）へ寄せ、同じ絵の地形の岩との比で画面の補正（`StoneRenderCalibration`）を掛けた。瓦礫の裾の砂は Ground054 を地形の土の色へ寄せた（`SandMatch`）。瓦礫の視点で、影を落とさない物とその物が無い絵の差を物の画素とし、明るい半分の線形の平均を比べた（`props-rubble-mask-*.png`）:

| r/g・b/g | seed 9 | seed 8 |
|---|---|---|
| 地形の岩 | 1.64・0.52 | 1.68・0.50 |
| Props 前 | 1.16・0.58 | 1.24・0.56 |
| Props 後 | 1.40・0.52 | 1.46・0.51 |

平たい岩（Warm）は地形の岩に近い赤みのある橙になった（seed 9 の手前の範囲で r/g 1.57）。瓦礫の山の小さな欠片は淡いまま（r/g 1.42）で、裾の砂を寄せても変わらなかった。残る黄みは V7 の砂塵・足元のなじみの色（`_PropGroundColor` は個体ごとに渡さず既定の (0.72,0.56,0.40)）と考えるが未検証。画像は `artifacts/region-map/ds011-color-20261127/`（`pair-s9-rubble-color.png`・`pair-s8-rubble-color.png`、途中の段は `-step1`・`-step2`）。

未確認: Props を足した時の GPU 時間（三角形の数だけ測った）、LOD1 へ切り替わる距離の見え方、通常 Editor のメニュー（`Open 3D Mesa Look + DS-011 Props Proof`）からの実アセットの表示。

### 8.2 木箱・木樽・瓦礫の通常配置（2026-11-27）

`Vastcore > Region Map > Open 3D Mesa Look + DS-011 Props Proof` で、通常の AssetDatabase 読み出しから `DS011_Props_Real` を表示する。撮影は既存の `CaptureFromCommandLine` に `-regionSeed 9 -propsCompare true -propsReal true -lookSky ours` を渡す。実モデルの二段積みは `props-stack-real.png`、斜面は `props-rubble-real.png`、接地の値は `props-proof-report.txt`。

GameProjects の木箱6型・木樽2型・扇状の瓦礫1型を、元GLBのSHA-256を既存71点と照合して通常の `Assets/Art/Models/` に追加。元GLBと変換後FBXは `model-source.json` で照合できる。既存の瓦礫4型（各LOD0/LOD1）は維持する。カタログの `clusterModelVariant` に指定された群れ全体モデルがあれば、同じ群れに1点だけ置く。未取得時は従来のモデル選択に戻る。

積み上げは閉じた木箱を支持に選び、上段の敷木を下段の蓋の高さに合わせる。箱を別に置く荷台は土台として使い、積荷付きパレットをそこへ重ねない。開口と蓋付きの樽は実モデルを使い、横倒し樽に対応するFBXが無い場合は仮モデルで姿勢を保つ。瓦礫は下り方向へ向け、描画地形の三角形から各石の沈み込みを求める。

[seed 9の通常配置での撮影結果](<D:/AI-Runs/VastCore/ds011-glb-fbx-20261127/seed9-placement/props-proof-report.txt>)では、筋交い入り木箱の敷木2本と蓋の高さの差は最大0.043 cm。斜度30.6°の瓦礫は地面に触れる28片の最低点を地表から0.3〜1.2 cm下へ収め、上の2片は支持する石に追従させた。

## 9. 現代の混在街路と入れる建物

本人選択「混在街路を軸にする（推奨）」。[三軒茶屋の小区画の商業棟](https://www.keyoperation.com/en/archives/projects/the-city-sangenjaya/)から路面店・上階の間口と窓の奥行き、[虎ノ門の業務棟](https://www.mori.co.jp/en/projects/toranomonhills/business-tower/)からガラスの上階と低層部の使い分けを参照した。生成した[設計時の完成イメージ](<D:/AI-Runs/VastCore/modern-city-block/design-directions.png>)の右列が混在街路。

### 9.1 組み立てと確認

- [Modern City Kit](../../Assets/Art/ModernCity/)の `ModernCity.unity` を開くか、`Vastcore > Modern City > Open Sample` を選ぶ。PlayでGameビューをクリックし、WASDとマウスで歩く。Escでマウスを解放する。ゲームパッドは左右スティック。
- `Variants` の `Street block` は seed・街路形・Props密度を変えて別シーンを作る。`Building` は間口・奥行き・階数・外装・階の用途を選んで、開いているシーンへ1棟を足す。`Prefabs/Assembled` の3棟や `Buildings`・`Street`・`Props` の単体部材も直接置ける。
- ブロックを組む基準は幅・奥行き4 m、階高3 m、外装は底面中央を原点に正面が−Z。床上端が各階の基準面。階段の上階側の4 m角を開け、上り終端側には床をつなぎ、開口の縁に柵を付ける。主要入口の開口1.84 m、搬入口1.46 m。幅0.68 m・高さ1.8 mの歩行カプセルで確認する。
- `Export Asset Package` はパッケージ内のPrefab・Mesh・材質・シーン・コードと、参照先の舗装・レンガ・木材のCC0画像だけを含める。Unity 6 URPとInput Systemが必要。画像の出所は同梱の `Modern City Kit.txt`。DS-011の地形用配置とSP-021の地域生成は、それぞれ既存の入口から使う。
- バッチの入口は [CityReview](../../Assets/Art/ModernCity/Editor/CityReview.cs) の `BuildCaptureAndPlayFromCommandLine`。Unity 6000.3.6f1に `-batchmode -projectPath <VastCore> -executeMethod Vastcore.ModernCity.Editor.CityReview.BuildCaptureAndPlayFromCommandLine -cityOutput <出力先>` を渡す。Prefabと見本シーンを作り直し、歩行・撮影・配布用書き出し・Playモードを順に確認する。

### 9.2 次の人工物キットへ持ち越す検証

市街の単色試作で歩道・開口・床・階段と距離別の輪郭を確認し、店頭、室用途、搬入口、屋上の順でPropsの組を置いた。初回の撮影では車道の抜けとカフェの空床が目立ち、階段前の光は飽和した。入口の見える歩道側へ視点を移し、街路の端に小広場、店内に客席と陳列のまとまり、階段に低い位置からの明かりを足した。後退した入口では舗装と室内床が重なったため、敷地側の舗装を床面より28 mm下げた。

次のキットでも、部材数より先に「外で用途が読めるか」「室内の目的地と階の接続が歩いて分かるか」を試す。棚・椅子・照明は用途間で共有し、什器の組み方で区別する。Propsを増やす前後は別配置を含む2地点で固定カメラと日照をそろえ、通行・場面の生成時間・描画完了までの時間を照合する。近景の材質と室内照明、街路の輪郭はそれぞれの視点で評価する。

初版の配布版は建築部材32、街路部材8、Props 71、組立見本3。seed 417と1031の各9棟で正面と搬入口、22・23か所の階の接続をカプセルで確認した。保存シーンはメニューから開き、Play中のゲームパッド入力でカフェ入口を通過した。[撮影の比較ページ](<D:/AI-Runs/VastCore/modern-city-block/review.html>)では同じ視点で基本構成と拡充後を比較できる。RTX 4060 Tiの街路視点・1600×900で、Props拡充による描画と1画素読み戻しの中央値の増分は0.93 msと1.03 ms。未確認の指標はGPU単独時間。

完成イメージに比べ、店先の用途は判別しやすくなったが、遠景は平らな屋根の反復が残り、近景は石・ガラスの反射と使い込んだ細部が不足する。次の人工物キットで先に手を付ける箇所は、用途が見える地上階の開口と、輪郭を変えるセットバックや屋上部。近景の仕上げは撮影で差が目立つ入口と主役の什器に集中させ、同じ部材で別の建物を組めることを確かめる。

この端末の検証用作業ツリー `C:\Users\PLANNER007\.codex\worktrees\modern-city-block\VastCore` は、チャットの既定チェックアウトに設定されているためアーカイブできなかった。検証した資産は元のVastCoreへ反映し、`803a724` をpush済み。作業ツリーが既定のチェックアウトから外れた時点でアーカイブできる。

### 9.3 角のカフェ

[前後と参考の比較](<D:/AI-Runs/VastCore/modern-city-block/corner-cafe/review.html>)はseed 417の4視点を同じ露出・太陽高度38°で撮影。窓を約0.56 m、扉を約0.67 m引き込み、石の目地、庇の木の下面、敷居が読めるようにした。歩道から商品と席、入口から注文台と階段口が見える。Playでは注文台を経由して+3 mの上階から歩道へ往復した。

4 m幅・3 m高の部材を使い、16 mのカフェとseed 1031の12 mの店舗で窓・入口・庇・階段口・什器のPrefab 7種を共有した。配布版は建築36、街路8、Props 77、組立見本3。残る差は石の色むら、ガラスの周囲の映り込み、室内の光の回り込み。次も開口の断面を先に撮り、什器は窓越しと入口から見て、別の間口で視線と通行を確かめる。

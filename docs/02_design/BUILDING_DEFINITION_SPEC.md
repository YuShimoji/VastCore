# 建物定義仕様書 (Building Definition Specification)

- **ID**: SP-019
- **ステータス**: partial
- **バージョン**: 0.1.0
- **作成日**: 2026-03-18
- **前提**: DS-009 (棚卸し完了), SP-010 (Stamp配置), SP-017 (StampExport), SP-018 (パラメトリック変異)

---

## 1. 概要

### 1.1 目的

「建物とは何か」をプロジェクト全体で統一的に定義する。

現在、50種以上の構造物生成コードが存在するが、建物の概念定義がなく、
4つの独立したenum (ArchitecturalType / CompoundArchitecturalType / BasicShapeType / PrimitiveType) が
互いに無関係に存在している。

本仕様は、**タグ重み複合体 (Tag-Weight Composite)** 方式を導入し、
既存コードを破壊せずに統一的な建物定義を実現する。

### 1.2 設計原則

1. **空の箱から始まる**: 建物はデフォルトで何の性質も持たない
2. **タグで性質を記述する**: 各建物が持つ性質を数値的な重み (0.0〜1.0) で表現
3. **文化的カテゴリを硬コードしない**: 「宗教施設」「要塞」等はプリセットとして提供
4. **自由拡張可能**: ユーザーが任意のタグを追加でき、カスタム種族・文化の建物に対応
5. **既存コードを壊さない**: ラッパー方式で既存の生成コードにタグを付与

### 1.3 スコープ

| 含む | 含まない |
|------|----------|
| タグシステムの全体設計 | StructureGenerator各タブの個別仕様 |
| 構成要素の最小セット定義 | 内部空間・インテリア生成 |
| 既存コードとの接続設計 | Grammar Engine (SP-013) の詳細 |
| 配置パターン・密度ルール | 破壊可能建築 (SP-012) の詳細 |
| 建築スタイルの視覚定義 | LOD・パフォーマンス最適化 |

---

## 2. タグシステム

### 2.1 StructureTagProfile

建物の性質を記述するデータ構造。全タグがフラットに並び、各タグに 0.0〜1.0 の重みを持つ。

```csharp
/// <summary>
/// 建物の性質を記述するタグ重み複合体。
/// 全タグはフラット構造で、重みは 0.0 (無関係) 〜 1.0 (完全適合) の範囲。
/// </summary>
[System.Serializable]
public class StructureTagProfile
{
    /// <summary>タグ名 → 重み (0.0〜1.0) のマッピング</summary>
    [SerializeField]
    private List<TagEntry> m_Tags = new List<TagEntry>();

    /// <summary>タグの重みを取得。未定義タグは 0.0 を返す</summary>
    public float GetWeight(string tagName);

    /// <summary>タグの重みを設定</summary>
    public void SetWeight(string tagName, float weight);

    /// <summary>定義済みタグの一覧を取得</summary>
    public IReadOnlyList<TagEntry> GetAllTags();

    /// <summary>
    /// 他プロファイルとのブレンドスコアを算出 (コサイン類似度)。
    /// 両方に存在するタグの重みベクトルの内積を正規化して返す。
    /// </summary>
    public float BlendScore(StructureTagProfile other);
}

[System.Serializable]
public struct TagEntry
{
    public string tagName;

    [Range(0f, 1f)]
    public float weight;
}
```

### 2.2 タグの設計

タグはフラット構造で、階層を持たない。タグ名は小文字英数字 + アンダースコアとする。

#### 組み込みタグ (初期提供)

以下は初期状態で定義されるタグ。ユーザーはこれらを自由に追加・変更できる。

**形態タグ** — 構造物の物理的な形を記述:

| タグ名 | 説明 | 用例 |
|--------|------|------|
| `arch` | アーチ構造 | ローマ式アーチ、ゴシックアーチ、水道橋 |
| `tower` | 塔・高層構造 | 望楼、鐘楼、灯台 |
| `wall` | 壁・障壁構造 | 城壁、防壁、塀 |
| `dome` | ドーム構造 | 神殿、天文台 |
| `column` | 柱・列柱構造 | 列柱廊、コロネード |
| `bridge` | 橋梁構造 | 歩道橋、水道橋、高架橋 |
| `enclosure` | 囲い込み構造 | 回廊、中庭、円形劇場 |
| `spire` | 尖塔構造 | 教会尖塔、ミナレット |
| `stepped` | 段状構造 | ピラミッド、ジグラット |
| `crystal` | 結晶構造 | 結晶塔、宝石状構造 |

**属性タグ** — 構造物の性格・印象を記述:

| タグ名 | 説明 | 用例 |
|--------|------|------|
| `massive` | 巨大・重厚 | 要塞、大聖堂、記念碑 |
| `ornate` | 装飾的 | バロック建築、寺院 |
| `weathered` | 風化・経年 | 廃墟、古代遺跡 |
| `fortified` | 防御的 | 城壁、要塞、塔 |
| `sacred` | 神聖・儀式的 | 神殿、聖堂、祭壇 |
| `functional` | 実用的 | 倉庫、橋、水道 |
| `elegant` | 優美・繊細 | 宮殿、アーケード |
| `primitive` | 原始的・素朴 | 巨石、自然石積み |
| `organic` | 有機的形状 | 樹木型構造、洞窟 |
| `geometric` | 幾何学的 | 結晶、正多面体 |

### 2.3 ブレンドスコア算出

建物タグと生成要素タグの類似度を**コサイン類似度**で算出する。

```
BlendScore(A, B) = (A · B) / (|A| × |B|)
```

- 両プロファイルに共通するタグの重みベクトルで内積を計算
- 片方にしか存在しないタグは、もう片方で 0.0 として扱う
- 結果は 0.0 (無関係) 〜 1.0 (完全一致) の範囲
- 両方が空の場合は 0.0 を返す

#### 生成要素の選択

ブレンドスコアを確率的重みとして使用し、ルーレット選択を行う。

```
選択確率(要素i) = BlendScore(建物, 要素i) / Σ BlendScore(建物, 要素j)
```

スコアが 0 の要素は選択されない。全スコアが 0 の場合はランダム選択にフォールバック。

### 2.4 StructureTagPreset (ScriptableObject)

事前定義されたタグの組み合わせ。ユーザーが複製してカスタムプリセットを作成可能。

```csharp
/// <summary>
/// 建物タグの事前定義プリセット。
/// ユーザーはこれを複製してカスタムプリセットを作成可能。
/// </summary>
[CreateAssetMenu(fileName = "NewStructurePreset", menuName = "VastCore/Structure Tag Preset")]
public class StructureTagPreset : ScriptableObject
{
    /// <summary>プリセットの表示名</summary>
    public string displayName;

    /// <summary>プリセットの説明</summary>
    [TextArea(2, 4)]
    public string description;

    /// <summary>タグプロファイル</summary>
    public StructureTagProfile profile;
}
```

#### 初期プリセット例

| プリセット名 | 主要タグ | 説明 |
|-------------|---------|------|
| Cathedral | {arch:0.7, dome:0.5, massive:0.8, ornate:0.9, sacred:0.95} | 大聖堂 |
| Fortress | {wall:0.9, tower:0.7, massive:0.95, fortified:0.95, functional:0.6} | 要塞 |
| Aqueduct | {arch:0.8, bridge:0.9, massive:0.6, functional:0.9} | 水道橋 |
| Ruins | {wall:0.4, column:0.5, weathered:0.95, primitive:0.6, massive:0.5} | 廃墟 |
| CrystalSpire | {spire:0.8, crystal:0.95, elegant:0.7, geometric:0.9} | 結晶尖塔 |
| Amphitheater | {enclosure:0.9, stepped:0.6, massive:0.7, ornate:0.5} | 円形劇場 |
| Monolith | {massive:0.95, primitive:0.8, geometric:0.6} | 巨石碑 |

---

## 3. 構成要素 (Structural Components)

### 3.1 概要

建物は以下の3カテゴリの構成要素から成る。各要素がタグ親和度ベクトルを持ち、
建物タグとのブレンドスコアで自動選択される。

### 3.2 カテゴリ定義

#### 外殻 (Shell)

建物の外形を構成する要素。

| 要素 | 説明 | パラメータ |
|------|------|-----------|
| Wall | 壁面 | thickness, height, segmentCount |
| Roof | 屋根 | style (flat/gable/hip/dome/spire), pitch, overhang |
| Foundation | 基礎 | depth, stepCount, width |
| Floor | 床面/階層 | floorCount, floorHeight |

#### 開口部 (Aperture)

壁面に設けられる開口。

| 要素 | 説明 | パラメータ |
|------|------|-----------|
| Window | 窓 | width, height, frameThickness |
| Door | 入口 | width, height, archType (none/round/pointed) |
| Vent | 通気口 | size, pattern |

#### 装飾 (Ornament)

構造上不可欠ではないが視覚的印象を与える要素。

| 要素 | 説明 | パラメータ |
|------|------|-----------|
| Column | 柱 | style (doric/ionic/corinthian/plain), height, radius |
| Arch | アーチ装飾 | span, rise, keystone |
| Carving | 彫刻 | density, depth, pattern |
| Battlement | 胸壁（鋸歯状） | merlonWidth, crenelWidth, height |
| Buttress | 控壁/飛梁 | depth, width, style (standard/flying) |
| Pinnacle | 小尖塔 | height, baseWidth |

### 3.3 構成要素とタグの接続

各構成要素バリエーションが StructureTagProfile を持ち、建物タグとのブレンドスコアで選択される。

```csharp
/// <summary>
/// 構成要素の1バリエーション。
/// タグ親和度ベクトルを持ち、建物タグとのブレンドスコアで選択確率が決まる。
/// </summary>
[System.Serializable]
public class ComponentVariant
{
    /// <summary>バリエーション名 (例: "GothicWindow", "RoundWindow")</summary>
    public string variantName;

    /// <summary>このバリエーションが親和するタグの重みプロファイル</summary>
    public StructureTagProfile affinity;

    /// <summary>構成要素のパラメータ (型に依存)</summary>
    // 具体的なパラメータはサブクラスまたはSerializedDictionaryで実装
}
```

#### 選択フロー

```
1. 建物の StructureTagProfile を取得
2. 該当カテゴリ(外殻/開口部/装飾)の全バリエーションを列挙
3. 各バリエーションと建物プロファイルのブレンドスコアを算出
4. スコアを確率重みとしてルーレット選択
5. 選択されたバリエーションのパラメータで生成
```

#### バリエーション例: 窓

| バリエーション | 親和タグ | 説明 |
|---------------|---------|------|
| GothicWindow | {ornate:0.8, sacred:0.7, spire:0.5} | 尖頭アーチ窓 |
| RoundWindow | {dome:0.6, ornate:0.5, elegant:0.6} | 丸窓 (ローズウィンドウ) |
| SlitWindow | {fortified:0.9, wall:0.7, functional:0.5} | 狭間窓 (矢狭間) |
| LatticeWindow | {ornate:0.6, elegant:0.7, organic:0.4} | 格子窓 |
| PlainWindow | {functional:0.8, primitive:0.5} | 無装飾窓 |

---

## 4. 既存コードとの接続 (ラッパー方式)

### 4.1 設計方針

既存の生成コード (ArchitecturalGenerator, CompoundArchitecturalGenerator, PrimitiveTerrainGenerator,
CrystalStructureGenerator) はそのまま残す。

各既存enumの値に対して、デフォルトの StructureTagProfile をマッピングするアダプター層を追加する。

### 4.2 StructureTagAdapter

```csharp
/// <summary>
/// 既存enumとタグシステムを接続するアダプター。
/// 既存の生成コードを変更せずにタグシステムに組み込む。
/// </summary>
public static class StructureTagAdapter
{
    /// <summary>
    /// ArchitecturalType からデフォルトタグプロファイルを取得。
    /// </summary>
    public static StructureTagProfile GetDefaultProfile(ArchitecturalType type);

    /// <summary>
    /// CompoundArchitecturalType からデフォルトタグプロファイルを取得。
    /// </summary>
    public static StructureTagProfile GetDefaultProfile(CompoundArchitecturalType type);

    /// <summary>
    /// PrimitiveType からデフォルトタグプロファイルを取得。
    /// </summary>
    public static StructureTagProfile GetDefaultProfile(PrimitiveType type);

    /// <summary>
    /// CrystalSystem からデフォルトタグプロファイルを取得。
    /// </summary>
    public static StructureTagProfile GetDefaultProfile(CrystalSystem system);
}
```

### 4.3 デフォルトマッピング表

#### ArchitecturalType → タグプロファイル

| ArchitecturalType | arch | tower | wall | dome | bridge | massive | ornate | sacred | fortified | functional |
|-------------------|------|-------|------|------|--------|---------|--------|--------|-----------|------------|
| SimpleArch | 0.9 | | | | | | | | | 0.6 |
| RomanArch | 0.9 | | | | | 0.5 | 0.6 | | | 0.5 |
| GothicArch | 0.9 | | | | | 0.5 | 0.8 | 0.6 | | |
| Bridge | 0.7 | | | | 0.9 | 0.6 | | | | 0.8 |
| Aqueduct | 0.8 | | | | 0.8 | 0.7 | | | | 0.9 |
| Cathedral | 0.7 | 0.4 | | 0.5 | | 0.8 | 0.9 | 0.95 | | |
| Colonnade | | | | | | | 0.6 | | | 0.5 |
| Viaduct | 0.6 | | | | 0.9 | 0.7 | | | | 0.9 |

（空欄は 0.0）

#### CompoundArchitecturalType → タグプロファイル

| CompoundArchitecturalType | arch | tower | wall | dome | bridge | enclosure | massive | ornate | sacred | fortified | functional |
|---------------------------|------|-------|------|------|--------|-----------|---------|--------|--------|-----------|------------|
| MultipleBridge | 0.7 | | | | 0.9 | | 0.7 | | | | 0.8 |
| AqueductSystem | 0.8 | | | | 0.8 | | 0.8 | | | | 0.9 |
| CathedralComplex | 0.7 | 0.5 | | 0.6 | | 0.4 | 0.9 | 0.9 | 0.95 | | |
| FortressWall | | 0.7 | 0.9 | | | 0.6 | 0.9 | | | 0.95 | 0.6 |
| Amphitheater | 0.5 | | | | | 0.9 | 0.7 | 0.5 | | | 0.6 |
| Basilica | 0.6 | | | 0.4 | | | 0.7 | 0.7 | 0.8 | | |
| Cloister | 0.5 | | | | | 0.9 | 0.4 | 0.6 | 0.7 | | |
| TriumphalArch | 0.9 | | | | | | 0.8 | 0.9 | | | |

### 4.4 PrefabStampDefinition との統合

既存の PrefabStampDefinition に StructureTagProfile を追加する。

```csharp
// PrefabStampDefinition.cs への追加フィールド
[Header("Tag Profile")]
[Tooltip("この構造物のタグプロファイル。配置・バリエーション選択に使用。")]
public StructureTagProfile tagProfile;
```

StampExporter (SP-017) がPrefab変換時に、ソースとなった ArchitecturalType 等から
StructureTagAdapter 経由でデフォルトタグプロファイルを自動設定する。
ユーザーはInspectorで自由に上書き可能。

---

## 5. 配置パターン・密度ルール

### 5.1 タグ親和度マトリクス

建物タグ同士の隣接親和度を定義するマトリクス。
「どのタグの建物がどのタグの建物の近くに配置されやすいか」を制御する。

```csharp
/// <summary>
/// タグ間の隣接親和度を定義するマトリクス。
/// 配置時に隣接セルの建物タグとの相性を評価するために使用。
/// </summary>
[CreateAssetMenu(fileName = "NewAdjacencyRules", menuName = "VastCore/Adjacency Rules")]
public class AdjacencyRuleSet : ScriptableObject
{
    /// <summary>タグペア → 親和度 (0.0〜1.0) のマッピング</summary>
    [SerializeField]
    private List<AdjacencyRule> m_Rules = new List<AdjacencyRule>();

    /// <summary>
    /// 2つのタグ間の隣接親和度を取得。
    /// 未定義ペアはデフォルト値 (0.5) を返す。
    /// </summary>
    public float GetAffinity(string tagA, string tagB);
}

[System.Serializable]
public struct AdjacencyRule
{
    public string tagA;
    public string tagB;

    [Range(0f, 1f)]
    public float affinity;
}
```

#### 初期隣接ルール例

| タグA | タグB | 親和度 | 説明 |
|-------|-------|--------|------|
| sacred | sacred | 0.8 | 聖域は集まりやすい |
| sacred | fortified | 0.3 | 神殿と要塞は離れやすい |
| fortified | fortified | 0.7 | 要塞は連なりやすい |
| functional | functional | 0.6 | 実用施設は集まりやすい |
| ornate | ornate | 0.5 | 装飾建築は中程度 |
| massive | massive | 0.3 | 巨大建築は間隔が空く |
| weathered | primitive | 0.7 | 風化建築と素朴建築は共存 |
| crystal | organic | 0.2 | 結晶と有機は離れやすい |

### 5.2 配置密度制御

エリアごとの建物密度を StructureTagProfile で制御する。
DualGridセルに対して「このエリアにどんなタグの建物が生まれやすいか」を指定。

```csharp
/// <summary>
/// エリアごとの建物配置密度と傾向を定義。
/// DualGridセル群に適用してゾーン的な配置制御を行う。
/// </summary>
[CreateAssetMenu(fileName = "NewPlacementZone", menuName = "VastCore/Placement Zone")]
public class PlacementZone : ScriptableObject
{
    /// <summary>ゾーンの表示名</summary>
    public string displayName;

    /// <summary>建物密度 (0.0=なし 〜 1.0=最密)</summary>
    [Range(0f, 1f)]
    public float density;

    /// <summary>このゾーンで生まれやすい建物のタグ傾向</summary>
    public StructureTagProfile zoneBias;

    /// <summary>最小建物間距離 (DualGridセル単位)</summary>
    public int minSpacing;

    /// <summary>最大建物数 (0=無制限)</summary>
    public int maxCount;
}
```

### 5.3 配置アルゴリズム概要

```
1. 配置対象エリアの PlacementZone を取得
2. 密度に基づいて配置候補セルを抽出
3. 各候補セルについて:
   a. zoneBias と利用可能な StructureTagPreset のブレンドスコアを算出
   b. スコアで確率的にプリセットを選択
   c. 隣接セルの既存建物タグとの AdjacencyRuleSet 評価
   d. 親和度が閾値以上なら配置確定
   e. StampRegistry で占有登録
4. minSpacing / maxCount の制約を適用
```

---

## 6. 建築スタイルの視覚定義

### 6.1 マテリアルパレット (ScriptableObject)

マテリアルの組み合わせをSOとして定義。各マテリアルにタグ親和度を持たせる。

```csharp
/// <summary>
/// マテリアルの組み合わせパレット。
/// 建物タグとのブレンドスコアで自動選択される。
/// </summary>
[CreateAssetMenu(fileName = "NewMaterialPalette", menuName = "VastCore/Material Palette")]
public class StructureMaterialPalette : ScriptableObject
{
    /// <summary>パレット名</summary>
    public string displayName;

    /// <summary>このパレットが親和するタグ</summary>
    public StructureTagProfile affinity;

    /// <summary>壁面マテリアル</summary>
    public Material wallMaterial;

    /// <summary>屋根マテリアル</summary>
    public Material roofMaterial;

    /// <summary>装飾マテリアル</summary>
    public Material ornamentMaterial;

    /// <summary>基礎マテリアル</summary>
    public Material foundationMaterial;

    /// <summary>風化度 (0.0=新築 〜 1.0=廃墟)</summary>
    [Range(0f, 1f)]
    public float weatheringLevel;
}
```

### 6.2 マテリアル選択フロー

```
1. 建物の StructureTagProfile を取得
2. 全 StructureMaterialPalette とのブレンドスコアを算出
3. 最高スコアのパレットを基本選択
4. weathered タグの重みに応じて weatheringLevel を調整
5. パラメトリック変異 (SP-018) の MaterialVariants と連携
```

### 6.3 既存 GlobalSettingsTab との接続

StructureGenerator の GlobalSettingsTab が持つマテリアルパレット機能を、
StructureMaterialPalette SO に段階的に移行する。

- Phase 1: GlobalSettingsTab のマテリアル設定を StructureMaterialPalette にエクスポートする機能を追加
- Phase 2: StructureGenerator 内での生成時に StructureMaterialPalette を参照する経路を追加
- Phase 3: 旧マテリアルパレット機能を deprecated 化

---

## 7. データフロー全体図

```
StructureTagPreset (SO)          ユーザーが選択・カスタムしたタグ重みセット
        │
        ▼
StructureTagProfile              建物の性質を記述するタグ重みベクトル
        │
        ├──→ ComponentVariant.affinity との BlendScore
        │           → 構成要素バリエーションの確率的選択
        │
        ├──→ StructureMaterialPalette.affinity との BlendScore
        │           → マテリアルの自動選択
        │
        ├──→ AdjacencyRuleSet との照合
        │           → 隣接配置の可否判定
        │
        ├──→ PlacementZone.zoneBias との BlendScore
        │           → エリア内での出現確率
        │
        └──→ StructureTagAdapter
                    → 既存enum (ArchitecturalType等) との双方向変換

                    ┌─────────────────────────────┐
                    │ PrefabStampDefinition        │
                    │   + tagProfile               │
                    │   + variationSettings (SP-018)│
                    └──────────┬──────────────────┘
                               │
                               ▼
                    StampRegistry (SP-010)
                    DualGrid 配置
```

---

## 8. 実装計画

### Phase 1: コアデータ構造 (優先度: HIGH) — 実装済み

- [x] `StructureTagProfile` クラス実装 (TagEntry, GetWeight, SetWeight, BlendScore)
- [x] `StructureTagPreset` SO 実装
- [x] 初期プリセット 7件 作成 (Editorメニュー: Vastcore > Create Initial Structure Presets)
- [x] EditMode テスト 20件 (BlendScore 算出の正確性)

### Phase 2: 既存コード接続 (優先度: HIGH) — 実装済み

- [x] `StructureTagAdapter` 実装 (4つのenum → タグプロファイル変換, 38enum値のマッピング)
- [x] `PrefabStampDefinition` に tagProfile フィールド追加
- [x] StampExporter に自動タグ付与機能追加 (_tagProfile 引数 + ApplyTagProfile)
- [x] EditMode テスト 15件 (全enum網羅性 + クロスタイプBlendScore検証)

### Phase 3: 構成要素システム (優先度: MEDIUM) — 実装済み

- [x] `ComponentVariant` + `ComponentSelector` クラス実装 (ルーレット選択)
- [x] `BuiltInComponentVariants` — 初期バリエーション 22種 (窓5/ドア3/柱4/屋根4/壁3/装飾3)
- [x] バリエーション選択ロジック (Select / SelectCategory / SelectAll)
- [x] EditMode テスト 13件 (選択傾向検証 + 決定論性 + カテゴリ選択)

### Phase 4: 配置ルール (優先度: MEDIUM) — 部分実装

- [x] `AdjacencyRuleSet` SO 実装 (AdjacencyRule struct + GetAffinity + EvaluateAdjacency)
- [x] `PlacementZone` SO 実装 (Density/ZoneBias/MinSpacing/MaxCount)
- [x] 初期隣接ルール定義 (Editorメニュー: Vastcore > Create Initial Adjacency Rules, 23件)
- [x] 配置アルゴリズム実装 (StructurePlacementSolver: ゾーンバイアス + 隣接親和度 + ルーレット選択)
- [x] EditMode テスト (AdjacencyRuleSetTests 12件 + StructurePlacementSolverTests 7件)

### Phase 5: スタイルシステム (優先度: MEDIUM)

- [x] `StructureMaterialPalette` SO 実装 (displayName/affinity/wall/roof/ornament/foundation/weatheringLevel)
- [x] マテリアル選択ロジック実装 (StructureMaterialSelector: ルーレット選択 + SelectBest決定論的選択)
- [ ] GlobalSettingsTab エクスポート機能
- [x] 初期パレットプリセット生成 (Editorメニュー: Vastcore > Create Initial Material Palettes, 7件)
- [x] EditMode テスト (StructureMaterialSelectorTests 15件)

### Phase 6: Inspector統合 (優先度: LOW)

- [ ] StructureTagProfile の Custom Inspector (タグ一覧 + 重みスライダー)
- [ ] StructureTagPreset の Custom Inspector (プレビュー + 比較)
- [ ] AdjacencyRuleSet の Custom Inspector (マトリクス表示)

---

## 9. 既存仕様への影響

| 仕様 | 影響 | 対応 |
|------|------|------|
| SP-010 (Stamp配置) | PrefabStampDefinition に tagProfile 追加 | Phase 2 で対応 |
| SP-017 (StampExport) | StampExporter にタグ自動付与 | Phase 2 で対応 |
| SP-018 (パラメトリック変異) | MaterialVariants とスタイルシステムの連携 | Phase 5 で対応 |
| SP-013 (Composite Structure) | GrammarEngine がタグを参照する設計の可能性 | 将来検討 |

---

## 10. 用語定義

| 用語 | 定義 |
|------|------|
| タグ (Tag) | 建物の性質を表す名前付きラベル。`arch`, `massive` 等 |
| 重み (Weight) | タグに付与される 0.0〜1.0 の数値。性質の強さを表す |
| タグプロファイル (Tag Profile) | タグ名→重みのマッピング。建物の性質の全体像 |
| ブレンドスコア (Blend Score) | 2つのタグプロファイル間のコサイン類似度 |
| プリセット (Preset) | 事前定義されたタグプロファイル。SOとして提供 |
| 構成要素 (Component) | 建物を構成するパーツ。外殻/開口部/装飾の3カテゴリ |
| バリエーション (Variant) | 構成要素の具体的な型。各バリエーションが親和度ベクトルを持つ |
| 親和度マトリクス (Adjacency Matrix) | タグ間の隣接配置の相性を定義するルール |
| 配置ゾーン (Placement Zone) | エリアごとの建物密度・傾向を定義するSO |
| マテリアルパレット (Material Palette) | 建物の視覚スタイルを定義するマテリアルの組み合わせSO |

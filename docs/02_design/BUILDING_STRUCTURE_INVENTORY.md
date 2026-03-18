# 建物・構造物 現状棚卸し

**Last Updated:** 2026-03-18
**目的:** 建物・構造物に関する「何があるか」「何がないか」を明確化する

---

## 全体構造

```
StructureGenerator (Editor, 10タブ, ~5000行)
  │ 「何を生成するか」→ コード実装あり、仕様なし
  │
  ├── BasicStructureTab ──────── 7種 (Cube/Sphere/Cylinder/Torus/Wall/Arch/Pyramid)
  ├── StructureGenerationTab ── 6種 (Primitives, BasicStructureTabと重複あり)
  ├── AdvancedStructureTab ──── 3種 (Monolith/TwistedTower/ProceduralColumn)
  ├── ProceduralTab ──────────── 3種 (ContinuousWall/Stairs/ProceduralStructure)
  ├── DeformerTab ───────────── 8種の変形 (Bend/Twist/Taper/Noise/Wave/Spherify/Ripple/Sine)
  ├── CompositionTab ─────────── CSG 3演算 + Blend 4種 + Advanced 3種
  ├── RandomControlTab ────────── 位置/回転/スケールのランダム化
  ├── ParticleDistributionTab ── 8配置パターン (Linear/Circular/Spiral/Grid/Random/Fractal/Voronoi/Organic)
  ├── RelationshipTab ─────────── 構造物間の親子/接続関係
  └── GlobalSettingsTab ────────── マテリアルパレット/スポーン設定
  │
  ↓ StampExporter (SP-017, 75%)
PrefabStampDefinition (SP-010, 90%)
  │ 「何を配置するか」→ 仕様あり
  │
  ↓ TerrainWithStampsBootstrap
DualGrid 地形配置
  「どこに配置するか」→ 仕様あり
```

---

## ランタイム生成クラス

### 建築構造 (ArchitecturalGenerator)

**ファイル:** `Assets/Scripts/Generation/Map/ArchitecturalGenerator.cs` (1154行)
**仕様書:** なし

| ArchitecturalType | 説明 | 特徴 |
|-------------------|------|------|
| SimpleArch | 単純アーチ | 基本アーチ形状 |
| RomanArch | ローマ式アーチ | 半円形アーチ |
| GothicArch | ゴシック式アーチ | 尖頭アーチ |
| Bridge | 橋梁 | アーチ + 路面 |
| Aqueduct | 水道橋 | 多層アーチ |
| Cathedral | 大聖堂 | ネイブ + サイドアイル |
| Colonnade | 列柱廊 | 等間隔の柱列 |
| Viaduct | 高架橋 | 多スパン橋梁 |

**共通パラメータ:** span, height, thickness, keyStoneRatio, archSegments, compressionFactor, 装飾密度, 風化効果

### 複合建築構造 (CompoundArchitecturalGenerator)

**ファイル:** 4分割、合計1363行
- `CompoundArchitecturalGenerator.cs` (270行)
- `CompoundArchitecturalGenerator.TypesA.cs` (439行)
- `CompoundArchitecturalGenerator.TypesB.cs` (321行)
- `CompoundArchitecturalGenerator.Connection.cs` (333行)

**仕様書:** なし

| CompoundArchitecturalType | 説明 | 構造数 |
|---------------------------|------|--------|
| MultipleBridge | 複数アーチ橋 | 3-8 |
| AqueductSystem | 水道橋システム | 4-12 |
| CathedralComplex | 大聖堂複合体 | 5-10 |
| FortressWall | 要塞壁 | 8-16 |
| Amphitheater | 円形劇場 | 6-12 |
| Basilica | バシリカ | 4-8 |
| Cloister | 回廊 | 4-8 |
| TriumphalArch | 凱旋門 | 3-5 |

**共通パラメータ:** overallSize, structureCount, structureSpacing, enableSymmetry, heightVariation, mixedStyles, unifiedDecorations

### プリミティブ地形 (PrimitiveTerrainGenerator)

**ファイル:** `Assets/Scripts/Generation/Map/PrimitiveTerrainGenerator.cs` (767行)
**仕様書:** なし

16種: Cube, Sphere, Cylinder, Pyramid, Torus, Prism, Cone, Octahedron, Crystal, Monolith, Arch, Ring, Mesa, Spire, Boulder, Formation

### 結晶構造 (CrystalStructureGenerator)

**ファイル:** `Assets/Scripts/Generation/Map/CrystalStructureGenerator.cs` (885行)
**仕様書:** なし

6結晶系: Cubic, Hexagonal, Tetragonal, Orthorhombic, Monoclinic, Triclinic
8結晶面: Cube, Octahedron, Dodecahedron, Rhombohedron, Prism, Pyramid, Pinacoid, Dome

---

## 仕様の有無マトリクス

### ある仕様

| ID | タイトル | pct | 対象 |
|----|---------|-----|------|
| SP-010 | Prefab Stamp配置 | 90% | DualGridセル占有・配置管理 |
| SP-017 | Stamp Export Pipeline | 75% | StructureGenerator→Prefab→StampDefinition変換 |
| SP-018 | Parametric Variation V1 | 85% | PositionJitter/MaterialVariants/ChildToggleGroups |
| SP-013 | Composite Structure Assembly Rules | 0% | GrammarEngine + StructureAssemblyRecipe (todo) |
| SP-012 | Destructible Architecture | 0% | DensityGridベース破壊 (todo) |
| SP-004 | Advanced Procedural Structure Generation | legacy | 高度プロシージャル構造物生成 (凍結) |

### ない仕様

| 領域 | 内容 | 影響度 |
|------|------|--------|
| **建物カテゴリ統一分類** | Architectural(8) / Compound(8) / Basic(7) / Primitive(16) / Crystal(6) が個別定義。統一enumなし | 高 |
| **構成要素の定義** | 窓・ドア・屋根・階数・壁厚・階高・内部空間のパラメータ化がない | 高 |
| **配置パターン・密度ルール** | StampRegistryは占有のみ。街並み・集落・都市のレイアウト規則がない | 中 |
| **建築スタイルの視覚定義** | 「様式×建物種→マテリアル/装飾/プロポーション」の対応表がない | 中 |
| **StructureGeneratorタブの仕様書** | 10タブ・~5000行のEditorツールに仕様書が1つもない | 中 |
| **生成物の品質基準** | 頂点数・ポリゴン数・LOD・パフォーマンス目標が未定義 | 低 |

### 曖昧な仕様

| 領域 | 状態 |
|------|------|
| ProceduralTab | 「現在開発中」とだけ記載。3種の生成物はコードから推測のみ |
| AdvancedStructureTab | 3種のモジュレーション構造物。仕様書なし |
| Grammar Engine (SP-013) | 仕様書はDraft、実装はStubのみ |
| V2-V4 バリエーション | Phase D定義にパスは書かれているが詳細仕様なし |

---

## 重複・不整合

| 問題 | 詳細 |
|------|------|
| BasicStructureTab vs StructureGenerationTab | 7種 vs 6種のプリミティブ生成。Cube/Sphere/Cylinder/Wall/Pyramidが重複 |
| ArchitecturalGenerator vs CompoundArchitecturalGenerator | 単一/複合の境界が不明確。Bridgeは両方に存在 |
| PrimitiveTerrainGenerator.Arch vs ArchitecturalGenerator | 異なるクラスで「アーチ」を生成 |

---

## 現在の「建物」生成能力の総括

**生成可能な構造物**: 50種類以上（重複含む）
**仕様化された構造物**: 0種類
**テスト済み**: EditMode 24件（配置システムのみ。生成物自体のテストなし）
**Unity実機検証**: 未実施

**核心的な問題**:
パイプラインの入口（StructureGenerator: 何を作るか）には豊富な実装があるが仕様がない。
出口（DualGrid Stamp: どこに置くか）には仕様がある。
両者を繋ぐ変換レイヤー（StampExporter）は実装済みだが未検証。
「建物」という概念自体の定義がプロジェクトに存在しない。

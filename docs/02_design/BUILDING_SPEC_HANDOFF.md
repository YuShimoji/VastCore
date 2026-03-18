# 建物仕様策定 — 別セッション Handoff Packet

**作成日:** 2026-03-18
**目的:** 別セッション（別Opus）で建物仕様を集中策定するための引き継ぎ情報

---

## ミッション

「広大な景観に映える、ユニークで巨大な人工構造物をプロシージャルに生成する」ための、建物・構造物自体の仕様を策定すること。

## 現状

- **棚卸しドキュメント:** `docs/02_design/BUILDING_STRUCTURE_INVENTORY.md` を先に読むこと
- **生成能力:** 50種以上の構造物生成コードが実装済み（仕様なし）
- **配置システム:** DualGrid Stamp配置は仕様化済み (SP-010/017/018)
- **課題:** 「建物とは何か」の定義がプロジェクトに存在しない

## 策定すべき仕様（優先度順）

### 1. 建物カテゴリ統一分類 (HIGH)

現在4つの独立したenum/分類が存在:
- ArchitecturalType (8種): アーチ系構造物
- CompoundArchitecturalType (8種): 複合建築
- BasicShapeType (7種): プリミティブ形状
- PrimitiveType (16種): 地形プリミティブ

**決めること:**
- 統一分類体系（住居/商業/記念建築/要塞/宗教/インフラ/自然構造物 等）
- 既存enumとの対応関係
- 新規カテゴリの要否

### 2. 構成要素の定義 (HIGH)

現在まったく定義されていない:
- 窓・ドア・屋根の有無とバリエーション
- 階数・階高・壁厚のパラメータ
- 内部空間の定義（空洞/充実）
- 外観スタイルのプロパティ

**決めること:**
- 全構造物共通の構成要素セット
- カテゴリ固有の構成要素
- パラメータの型と範囲

### 3. 配置パターン・密度ルール (MEDIUM)

StampRegistryは占有管理のみ。「街並み」のルールがない:
- 隣接ルール（何の隣に何を置くか）
- 密度指定（エリアごとの建物密度）
- 道路・地形との関係

### 4. 建築スタイルの視覚定義 (MEDIUM)

マテリアル参照はあるが対応表がない:
- 「様式×建物種→見た目」のマトリクス
- マテリアルパレットの定義
- 風化・経年表現のルール

### 5. StructureGeneratorタブの仕様化 (LOW)

10タブ・~5000行のEditorツールに仕様書が0件:
- 各タブの入出力定義
- タブ間の依存関係
- 生成物の品質基準

## 読むべきファイル

| ファイル | 目的 |
|---------|------|
| `docs/02_design/BUILDING_STRUCTURE_INVENTORY.md` | 最初に読む。全体像 |
| `docs/02_design/COMPOSITE_STRUCTURE_RULES_SPEC.md` | Grammar Engine仕様 (todo/0%) |
| `docs/02_design/DESTRUCTIBLE_ARCHITECTURE_SPEC.md` | 破壊可能建築仕様 (todo/0%) |
| `docs/02_design/PHASE_D_SCOPE_DEFINITION.md` | Phase Dスコープ (PD-1~PD-3) |
| `docs/02_design/SP018_PARAMETRIC_VARIATION_SPEC.md` | V1パラメトリック変異 |
| `Assets/Scripts/Generation/Map/ArchitecturalGenerator.cs` | 建築構造生成 (1154行) |
| `Assets/Scripts/Terrain/Map/CompoundArchitecturalGenerator.cs` + TypesA/B/Connection | 複合建築 (1363行) |
| `Assets/Editor/StructureGenerator/Tabs/` | 全10タブ実装 |
| `CLAUDE.md` | プロジェクトルール・HUMAN_AUTHORITY境界 |

## 制約

- HUMAN_AUTHORITY: 建物の種類・外観・スタイルは体験・感性系の判断を含む → 選択肢を提示して承認を得ること
- SPEC FIRST: 仕様を先に言語化してから実装
- T1方針: オーサリング主体。StructureGeneratorはEditorツールとして深化する方向
- V4方針: バリエーションは段階的 (V1パラメトリック → V2 WFC → V3 CSG)
- コード標準: PascalCase public, _camelCase params, m_CamelCase private, #region, 日本語XMLdoc

## 期待する成果物

1. 新規仕様書 `docs/02_design/BUILDING_DEFINITION_SPEC.md` (統一分類 + 構成要素)
2. spec-index.json へのエントリ追加
3. 必要に応じて既存仕様 (SP-013等) の更新提案

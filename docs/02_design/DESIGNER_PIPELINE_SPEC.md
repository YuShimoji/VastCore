> **上位SSOT**: [SSOT_WORLD.md](../SSOT_WORLD.md) | **索引**: [DOCS_INDEX.md](../DOCS_INDEX.md)

# SP-020: Designer Pipeline Specification

**ステータス**: DRAFT
**最終更新**: 2026-03-23
**関連仕様**: SP-010, SP-017, SP-018, SP-019

---

## 1. 目的

VastCore の構造物生成パイプラインの end-to-end フローを定義する。
デザイナーが「どの工程で何をするか」「どこが自動化されているか」を明文化し、
断絶しているコンポーネント間の接続方針を示す。

---

## 2. パイプライン全体像

```text
[Stage 1]                [Stage 2]              [Stage 3]              [Stage 4]
StructureGenerator  -->  StampExporter  -->  BuildingDefinition  -->  DualGrid配置
   (Editor)                (Editor)            (SO群)                 (Runtime/Editor)

デザイナー操作:          デザイナー操作:       デザイナー操作:          デザイナー操作:
 形状生成・変形           Export実行            タグ設定・ルール定義     ゾーン設定・配置実行
 マテリアル選択           (自動Prefab化)        マテリアルパレット設定   (自動+手動調整)
 ランダム制御                                  配置ルール設定
```

---

## 3. Stage 定義

### Stage 1: 構造物生成 (StructureGenerator)

| 項目 | 値 |
|------|-----|
| ツール | StructureGeneratorWindow (Menu: Tools/Vastcore/Structure Generator) |
| 入力 | 空のGameObject or 既存Prefab |
| 出力 | シーン内のGameObject (MeshFilter + MeshRenderer構成) |
| 介入 | 手動: 全操作がデザイナー手動 |
| 自動化 | なし (全7タブの操作はインタラクティブ) |

**タブ構成:**
1. GlobalSettings: マテリアルパレット管理
2. BasicStructure: 基本形状生成 (Cube, Cylinder, Sphere等)
3. AdvancedStructure: 高度形状生成
4. Relationship: 関係性設定
5. ParticleDistribution: パーティクル分布
6. Deformer: 変形機能
7. Composition: CSG演算・形状合成
8. RandomControl: ランダム制御

### Stage 2: スタンプ化 (StampExporter)

| 項目 | 値 |
|------|-----|
| ツール | StampExporter.ExportAsStamp() (StructureGeneratorWindow内ボタン) |
| 入力 | Stage 1 の GameObject |
| 出力 | Prefab (`Assets/Resources/Stamps/Prefabs/`) + PrefabStampDefinition (`Assets/Resources/Stamps/Definitions/`) |
| 介入 | 手動: Export ボタン押下 |
| 自動化 | Prefab化、Definition生成、子オブジェクト検出、ToggleGroups設定 |

**現状の断絶 (GAP-1):**
- StampExporter は `_tagProfile` パラメータを受け取れるが、StructureGeneratorWindow からの呼び出し時に常に null
- StructureGeneratorWindow にタグプロファイル選択UI がない
- 結果: Export された PrefabStampDefinition の m_TagProfile が常に空

### Stage 3: 建物定義 (BuildingDefinition SO群)

| 項目 | 値 |
|------|-----|
| ツール | Inspector (各SO) + EditorCreator (メニュー) |
| 入力 | Stage 2 の PrefabStampDefinition |
| 出力 | 設定済みの SO群 (TagProfile, AdjacencyRuleSet, PlacementZone, MaterialPalette) |
| 介入 | 手動: Inspector での各SO設定 |
| 自動化 | ブレンドスコア計算 (コサイン類似度)、ルーレット選択 |

**構成SO:**

| SO | 役割 | 作成ツール |
|----|------|-----------|
| StructureTagPreset | タグプロファイルのプリセット | StructureTagPresetCreator |
| AdjacencyRuleSet | タグ間の隣接親和度ルール | AdjacencyRuleSetCreator |
| PlacementZone | エリア別密度・バイアス | (手動作成) |
| StructureMaterialPalette | マテリアルセット + タグ親和度 | StructureMaterialPaletteCreator |

**現状の断絶 (GAP-2):**
- Phase 6 Inspector が未実装のため、SO 設定に専用UIがない
- 各SOの初期値生成ツール (Creator) は実装済みだが、編集UIはデフォルトInspector依存

### Stage 4: DualGrid配置

| 項目 | 値 |
|------|-----|
| ツール | TerrainWithStampsBootstrap (MonoBehaviour) |
| 入力 | PrefabStampDefinition[], DualGrid, TerrainChunks |
| 出力 | 配置済みGameObject群 (地形上の構造物) |
| 介入 | 手動: Inspector で StampDefinitions 割り当て + Build() 実行 |
| 自動化 | 地形生成、グリッド生成、ランダム配置、Prefabインスタンス化 |

**現状の断絶 (GAP-3, GAP-4, GAP-5):**
- GAP-3: TerrainWithStampsBootstrap.PlaceStampsAuto() がランダム配置のみ。StructurePlacementSolver が未接続
- GAP-4: PlacementZone / AdjacencyRuleSet の Inspector 公開がない
- GAP-5: StructureMaterialSelector が配置フロー内で呼ばれていない

---

## 4. GAP 一覧 (断絶点)

| ID | 断絶箇所 | 原因 | 影響 | 修復方針 (案) |
|----|----------|------|------|--------------|
| GAP-1 | StructureGeneratorWindow → StampExporter 間の TagProfile | UIなし | Export時にタグ情報が失われる | StructureGeneratorWindow に TagPreset ドロップダウン追加 |
| GAP-2 | BuildingDefinition SO群の編集UI | Phase 6 未着手 | デフォルトInspector では操作性が悪い | SP-019 Phase 6 実装 |
| GAP-3 | PlaceStampsAuto → StructurePlacementSolver | 未接続 | 智能配置ができない (ランダムのみ) | Bootstrap に Solver 統合 |
| GAP-4 | PlacementZone/AdjacencyRuleSet の Inspector 公開 | 未接続 | ゾーンバイアス・隣接ルールが使えない | Bootstrap Inspector に公開 |
| GAP-5 | StructureMaterialSelector の配置フロー統合 | 未接続 | マテリアル自動選択が効かない | PrefabStampPlacer に統合 |

---

## 5. デザイナーワークフロー (目標像)

### 5.1 新規構造物の作成フロー

```text
1. StructureGeneratorWindow を開く
2. 各タブで形状を生成・調整
3. タグプリセットを選択 (GAP-1 修復後)
4. [Export as Stamp] ボタンで Prefab + Definition を一括生成
5. Definition の Inspector で詳細設定を調整 (GAP-2 修復後)
   - TagProfile の微調整
   - MaterialVariants の設定
   - ChildToggleGroups の調整
```

### 5.2 街区配置フロー

```text
1. シーンに TerrainWithStampsBootstrap を配置
2. Inspector に StampDefinition[] を割り当て
3. PlacementZone SO をアサイン (GAP-4 修復後)
4. AdjacencyRuleSet SO をアサイン (GAP-4 修復後)
5. MaterialPalette[] をアサイン (GAP-5 修復後)
6. Build() を実行
   → 地形生成 → グリッド生成 → 智能配置 → マテリアル適用 → GameObject生成
```

### 5.3 反復調整フロー

```text
1. Build 結果を確認
2. 不満点に応じて調整:
   a. 配置パターン → PlacementZone の density/bias 調整
   b. 隣接関係 → AdjacencyRuleSet の affinity 調整
   c. マテリアル → MaterialPalette の追加/修正
   d. 形状バリエーション → PrefabStampDefinition の Jitter/Toggle 調整
3. Build() を再実行
```

---

## 6. 未決定事項 (HUMAN_AUTHORITY)

以下はデザイナー体験に直結するため、人間の判断を必要とする。

| # | 論点 | 選択肢候補 | 影響範囲 |
|---|------|-----------|----------|
| Q-1 | GAP 修復の優先順位 | GAP-1→5の順 / GAP-3→4→5→1→2 / 体験スライスで逆算 | Phase D 残作業の順序 |
| Q-2 | 智能配置の統合方法 | Bootstrap拡張 / 独立EditorWindow / 両方 | アーキテクチャ |
| Q-3 | マテリアル適用タイミング | 配置時に自動 / 配置後に手動 / 両方選択可 | デザイナー操作感 |
| Q-4 | StructureGeneratorWindow のタグUI | ドロップダウン / タグエディタ / プリセットのみ | UI設計 |
| Q-5 | Stage 3→4 間の自動化度合い | SO設定→Build一発 / ステップ実行 / プレビュー付き段階実行 | ワークフロー粒度 |

---

## 7. 関連仕様

- [SP-010: DualGrid Prefab Stamp Placement](SP010_PrefabStampPlacement_Spec.md) — DualGridセル配置基盤
- [SP-017: Stamp Export Pipeline](STAMP_EXPORT_PIPELINE_SPEC.md) — StructureGenerator→Prefab変換
- [SP-018: Parametric Variation](SP018_PARAMETRIC_VARIATION_SPEC.md) — PrefabStampDefinition のバリエーション
- [SP-019: Building Definition](BUILDING_DEFINITION_SPEC.md) — タグ重み複合体・配置ルール・マテリアルパレット

---

**参照**: [SSOT_WORLD.md](../SSOT_WORLD.md) | [HANDOVER.md](../HANDOVER.md)

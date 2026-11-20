> **上位SSOT**: [SSOT_WORLD.md](../SSOT_WORLD.md) | **索引**: [DOCS_INDEX.md](../DOCS_INDEX.md)

# SP-020: Designer Pipeline Specification

**ステータス**: DRAFT
**最終更新**: 2026-11-01（再開準備とSP-017最小検証結果。作品・表現方針の採用は含まない）
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

**GAP-1 の現物照合（2026-11-01、accd587）:**
- StructureGeneratorWindow に Tag Preset ObjectField があり、選んだ Profile を StampExporter へ渡している。
- Exporter には SerializedObject 経由のタグ転写がある。実Editorでの転写・再読込は未実施。

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

**GAP-2 の現物照合（2026-11-01）:**
- `Assets/Scripts/Editor/PrefabStampDefinitionEditor.cs` が存在するため「専用UIなし」とは扱わない。
- SO群全体の編集UXと保存往復は未検証。Creatorが存在しても既存素材の上書きや一括実行の許可にはならない。

### Stage 4: DualGrid配置

| 項目 | 値 |
|------|-----|
| ツール | TerrainWithStampsBootstrap (MonoBehaviour) |
| 入力 | PrefabStampDefinition[], DualGrid, TerrainChunks |
| 出力 | 配置済みGameObject群 (地形上の構造物) |
| 介入 | 手動: Inspector で StampDefinitions 割り当て + Build() 実行 |
| 自動化 | 地形生成、グリッド生成、ランダム配置、Prefabインスタンス化 |

**GAP-3, GAP-4, GAP-5 の現物照合（2026-11-01、accd587）:**
- Bootstrap は placementZone がある場合に StructurePlacementSolver を呼び、未指定なら従来のランダム配置へ戻る。
- placementZone、adjacencyRules、materialPalettes は public serialized field として存在する。
- Bootstrap から Placer.SetMaterialPalettes()、Placer から StructureMaterialSelector.Select() への接続がある。
- 以上はソース上の接続確認。ルールの効果・素材参照・実画面・再現性の合格は未確認。

---

## 4. GAP 一覧 (断絶点)

| ID | 断絶箇所 | 原因 | 影響 | 修復方針 (案) |
|----|----------|------|------|--------------|
| GAP-1 | TagProfile 転写 | 接続コードあり | 保存後のタグ保持は未検証 | 実Editorで照合。再実装しない |
| GAP-2 | SO群の編集UI | StampDefinitionのCustomEditorあり | 全SOの使い勝手は未確認 | 通常Inspector操作を観測して不足を限定する |
| GAP-3 | Solver | placementZone指定時に呼出あり | 選択結果・再現性は未検証 | SP-019のルール別検証へ接続 |
| GAP-4 | ゾーン・隣接ルール | public fieldあり | 保存・再読込と実効性は未検証 | Sceneの参照と配置結果を照合 |
| GAP-5 | 素材選択 | Placerへの接続あり | 適用対象と出力保持は未検証 | SP-018/019で確認 |

---

## 5. デザイナーワークフロー (目標像)

### 5.1 新規構造物の作成フロー

```text
1. StructureGeneratorWindow を開く
2. 各タブで形状を生成・調整
3. Tag Presetを選択（接続実装あり、実機未検証）
4. [Export as Stamp] ボタンで Prefab + Definition を一括生成
5. Definition の Inspector で詳細設定を調整（通常操作の受入は未確認）
   - TagProfile の微調整
   - MaterialVariants の設定
   - ChildToggleGroups の調整
```

### 5.2 街区配置フロー

```text
1. シーンに TerrainWithStampsBootstrap を配置
2. Inspector に StampDefinition[] を割り当て
3. PlacementZone SO をアサイン
4. AdjacencyRuleSet SO をアサイン
5. materialPalettes に StructureMaterialPalette[] をアサイン
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

以下は2026-03-23時点の設計論点。2026-11-01のソース接続確認を踏まえ、全項目の再回答を通常検証の開始条件にはしない。広いUI制作・高コストな表現方針を変える場合だけ、現物と差分を示して判断する。

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

## 8. 2026-11-01 再開準備：採用済み経路と今回の差分

本節はSP-020内の全工程設計。現在地は [runtime-state](../runtime-state.md)、最初の実行仕様・依頼は [SP-017](STAMP_EXPORT_PIPELINE_SPEC.md#2026-11-01-最小検証仕様未実行) が所有する。新しい全体計画・dispatcherは作らない。

利用経路は「デザイナーが人工構造物の形状・素材、地形、配置条件を指定し、景観内で確認・修正し、編集元と設定を保存して同じ結果へ戻り、採用した版を必要なSceneまたは実行先へ渡す」。目的は [SSOT_WORLD](../SSOT_WORLD.md) の巨大でユニークな人工構造物であり、地形整備はその利用環境である。

継承する判断はDualGrid + HeightMap + designer Prefab Stamp、T1オーサリング主体、V4段階的拡張。Unity/URP、既存生成器・SO・Prefabを継続使用する。WorldGenのField/Graph/GrammarやVoxelを新しい主経路にしない。SP-019がspec-indexではdone、所有仕様冒頭ではpartialと食い違うため、ラベルから全体受入を推定しない。今回その状態や進捗率を引き上げない。

### 現物に基づく選定

2026-11-01の入力はHEAD `accd5871fb2eadc825c7101df9df0548c5205cae`、現在checkoutは `codex/vc-development-readiness-20260711`。以下の版は最新推奨版ではなく、この入力のmanifest/lock・ローカル現物の観測である。

| 手段 | 適合する範囲と確認 | 維持する理由・制限・見直す条件 |
|---|---|---|
| Unity 6000.3.6f1 / URP 17.3.0 | ProjectVersion、manifest/lock、6000.3.6f1実行ファイルが存在 | Scene/Prefab/.metaと既存資産を使う。import・ライセンス動作・compileは今回未実行。版変更は今回対象外 |
| Unity Terrain + HeightmapProviderSettings | TerrainGenerationConfig.CreateHeightProvider、TerrainChunk、Bootstrapへの接続 | 既存の高さ供給と地形表示を使う。Bootstrapの高さサンプルは先頭チャンクのみ。広域化時に複数チャンク境界を検証 |
| StructureGenerator / ProBuilder 6.0.8 | 既存の形状・配置・CSGタブとExportボタン、PackageCacheの6.0.8 | 人が編集できる既存経路。標準Transform/Prefab/Inspectorを併用。CSGプロバイダの実動作を主検証の必須依存に増やさない |
| Deform 1.2.2 | lock commit `9e57dd3864ea6c742d819959724c99ca574a2f76` とローカルpackage.json/LICENSE.txt | 既存の変形用。最初のArch検証では不要。形状表現に必要になった時だけ既存adapterと実行可否を確認 |
| SP-010/017/018/019 | PrefabStampDefinition、StampRegistry、Placer、タグ・ゾーン・パレットの接続あり | 既存SOを拡張点とする。実装存在を保存往復や景観の採用へ広げない |
| Cockpit / Unity通常操作 | Random VariationはTransform編集、Terrain等はstatus-only | Cockpitを統合生成器と仮定しない。形状はStructure Generator、参照設定はInspector、保存はScene/Prefab各標準操作 |
| Node 24.13.0 / Git 2.53.0.windows.1 | このcheckoutで起動・読み取りと構造検査を実行 | 追加npm依存なし。C#・実Editorの代わりにはならない。PowerShellのcompile/test入口は読取のみ |
| Unity MCP / AI tooling / Splines | manifestに存在。MCPのlockは `aaf6308b331f6cbcc2a41f11d90ac2109154343e`、既存cacheのfingerprintは `afece389a066812c599670e6ebe48baa71c16691` | cacheとlockの差は観測のみ。接続・実行可否は未確認。今回MCP・別モデル・外部AI・CLI追加を必須にしない |

[外部資産評価](../architecture/external-asset-evaluation.md) のadapter境界は参照するが、今回の準備を依存除去やアセンブリ再構成へ拡張しない。Unity標準Terrain/Prefab/SOを継続する案を推奨する。別エンジンや新しい永続化基盤への移行は、今回確認された接続・保存の問題を解消せず、移行と互換検証を追加するため採用しない。標準操作で足りない実測結果が出た時に対象を絞って比較する。

### 入力から利用先までの工程

担当は役割であり、新しい人・agentの割当や起動を表さない。開発AIが技術観測と証拠、デザイナーが制作操作、本人が作品性・体験の採用を担う。実施担当が兼ねても、この出口を混同しない。

| 工程・既存計画 | 入力 | 成果 | 出口確認 | 必須依存 | 担当 |
|---|---|---|---|---|---|
| 実行前確認／readiness | HEAD、dirty一覧、保存状態、6000.3.6f1 | 保全基準と実行可能範囲 | 競合・未保存・拒否履歴の対象照合、compile、non-zero EditMode/PlayMode | 実行範囲の依頼とライセンス付きEditor。修復は別範囲 | 開発AI |
| Stage 1／T1形状制作 | Basic/Advanced形状、寸法、変形、素材 | Scene内編集元GameObject | Mesh/Renderer、寸法・原点・法線・素材参照を確認。景観の採否は本人へ | 既存生成器と使用素材 | デザイナー＋開発AI |
| Stage 2／SP-017 | 編集元の確定した候補とTag Preset | 新規Prefab＋StampDefinition | 元対象不変、Mesh/Material/タグの参照が保存・再読込後も有効 | Stage 1、Exporter | 開発AI、デザイナーが対象選択 |
| Stage 3／SP-018/019 | Definition、配置ゾーン、隣接ルール、素材候補、seed | 保存したSO群とSceneの参照 | 既定値で旧データを読める。選択範囲、footprint、素材適用を確認 | Stage 2、利用条件を確認できる素材 | デザイナー＋開発AI |
| Stage 4／SP-010 | SO群、Heightmap設定、DualGrid/Stamp seed、セル | 景観内の配置候補 | 配置数・セル占有・地形高さ・向き・スケール・素材を照合 | Stage 3、TerrainWithStampsBootstrap | 開発AIが技術、本人が景観採用 |
| 修正・保存・再開／PD-2へ接続 | 編集元Scene/Prefab、SO、Cockpit session、候補の版情報 | 戻れる編集元と再生成条件 | Sceneとsessionを別々に保存・再読込。新しいPlayで同条件を再現。手編集と候補を保持 | 保存対象・適用範囲の識別 | デザイナー＋開発AI |
| 利用先への受渡し／PD-1・PD-3・Phase E | 技術確認済み候補、依存参照、本人の採用範囲 | ローカルUnity場面／必要時の配布候補 | 同じ素材・形状・配置が利用先に届く。ビルド・端末動作・性能・権利は各別に確認 | 上記往復と、配布形態の確定 | 開発AI。本人が体験・公開判断 |

最初はSP-017の既存Arch一件を、一チャンク・明示セルで保存から再配置まで通す。Cockpit smokeは先行する操作安全性の確認であり、ここまでの技術証拠を巨大景観・複数チャンク・全構造物へ一般化しない。その後SP-018の素材/子切替/ジッター、SP-019のSolver・複数セル、複数チャンク、巨大構造物の見え方、PD-1合成/LOD、PD-2制御、PD-3性能、利用先のbuildを必要順に広げる。既存ロードマップの優先順位を新たな採用決定で置き換えない。

### 保存・修正・再生成の契約

- Cockpit session `.asset` はname/seed/notes/RandomTransformRecipe等を保存する。対象GameObject一覧、形状メッシュ、地形設定、Scene全体、採用状態を保存する形式ではない。SaveはAssetDatabase.SaveAssets()を呼ぶため、別のdirty assetがある環境で一括保存を試さない。
- 編集元はUnityのScene/Prefabと参照するMesh/Material/SO。GeneratorのGlobalSettings JSON/EditorPrefsは共通scale/spawn/material参照用であり、全タブの形状レシピの完全な往復保存とは確認できていない。再制作に必要な形状パラメータを最初の証拠に記録し、保存できない項目は欠落として残す。
- StampExporterはunique pathでPrefabとDefinitionを作り、コピーのposition/rotationをリセットする。メッシュの永続化を独立に保証する処理はこのExporterでは確認できず、ProBuilderを含めた復元は実検証が必要。元オブジェクトのtransformと編集内容は保持する。
- Placerはinstance.localScaleを配置scaleで代入し、CalculateWorldPositionはcell centerを使う。Prefabの非unit scaleやgrid親の移動を含めた寸法・ワールド座標の一致は未検証。最小検証では原点やscaleを結果に合わせて変えず、入力から定めた期待値と照合する。
- BootstrapのBuild()には既存rootやm_Chunksのclear処理が見当たらず、同一instanceへの反復Buildを置換操作と扱わない。検証では保存した入力から新しいPlayを開始し、古い出力を破壊しない。生成出力の手編集を次のBuildへ引き継ぐ仕組みは未確認。
- `stampDefinition` の単一定義プロパティは `stampDefinitions[0]` の互換アクセスだが、旧serialized fieldからの移行証明にはならない。旧Sceneをコピーで開く試験を後段へ置く。既存enum値・serialized名・GUIDを変更する場合は、別の互換仕様と許可が必要。
- 最初は証拠内に、HEAD、入力asset path/GUID/hash、Scene path、形状寸法、3種のseed（地形／DualGrid／Stamp）、Cockpit seed、対象と未変更対象、出力asset、技術結果、本人の採用状態を記録する。恒久的な別DBや新保存形式は導入しない。

### 拡張時に変更する場所と互換性

| 追加するもの | 既存の追加・選択箇所 | 保全と出口 |
|---|---|---|
| 新しい人工構造物 | 既存生成器の形状選択／編集元Prefab → TagPreset → Export | 単なる新形状ならPrefab＋Definitionを追加。既存enumやbase型の再編を先行させない。旧Prefabと新候補を別名で保持し、通常Sceneへの配置まで確認 |
| 形状操作 | StructureGeneratorの既存tab、Deform/CSG provider境界 | 破壊操作前の編集元を保持。Meshの保存・Undo・export後の再読込を確認。CSG/Deformの実可用性がなければその操作だけ保留 |
| 配置規則 | PlacementZone / AdjacencyRuleSet / StructurePlacementSolver / StampRegistry | 新SOで試す。null fallback、占有、明示セル指定の経路を維持。既存Sceneと旧Definitionで回帰確認 |
| 素材・部品差替 | MaterialVariants、ChildToggleGroups、StructureMaterialPalette | 利用条件と参照を確認し新候補へ設定。Palette優先・MaterialVariants fallback、root Rendererのみの適用、子名の一致を検証。既存共通Materialを直接変更しない |

### 操作画面と採用の扱い

実装済み操作と改善案の詳細は [Cockpit UX Notes](../DESIGNER_COCKPIT_UX_NOTES.md)。対象はHierarchy/Inspector、版はasset pathとGUID/hash、適用は選択対象のTransformかStamp配置かを明示して試験する。Random Applyは名前→InstanceID順で乱数を消費し、Relative Positionは加算するため、同seedの連続Applyは元位置からの再生成と同じではない。比較は同じ保存基準と同じ対象順に戻して行う。

Newは新規session、Loadは既存assetそのものへの参照切替。Sceneを初期化/復元する操作と扱わない。Cockpitの上部表示だけではSceneのdirty、Prefabの版、候補採用状態は分からない。現行はUnity標準のScene名/dirty表示、Inspectorと証拠記録を併用する。これらを上部へ統合するUXは未採用案であり今回実装しない。

技術結果は「保存・復元できた項目と範囲」、本人の判断は「この版・視点・場面の形状／景観／操作感のacceptまたはrevise」で残す。技術PASS、生成完了メッセージ、asset存在、スクリーンショットを作品採用や公開許可へ変換しない。

### 利用先・配布・復旧

確認した端末はDESKTOP-H53P1T4 / Windows 11 Home。Editor内制作を当面の検証先とする。最終配布先・最低GPU/RAM・商用形態は未確認で、mobile/AIパッケージの存在から対象端末を決めない。観測時RAM約15.8GiB、空き約2.2GiB、C空き45.5GiB/D空き285.1GiBは瞬間値で、Unityの実行余裕を保証しない。

`ProjectSettings/EditorBuildSettings.asset`にはSampleScene、Playground、newScene、Mainの4件がenabled。最小検証Sceneを製品build対象へ混ぜない。今回、明示的なVastCore専用build CLIは確認できていないため、将来はUnity標準Build Profilesで対象Scene/OS/moduleを確認し、ローカルbuild→同じ版の起動→対象端末操作へ進む。Windows向けは検証候補であり配布先の採用決定ではない。Editor assembly内のCockpit sessionをPlayerの保存基盤に使わない。

ログ・テストは既存の `artifacts/`（ignored）、編集元はAssets、Exportは既存の `Assets/Resources/Stamps/{Prefabs,Definitions}`、sessionは既存の `Assets/Data/VastCore/DesignerSessions`。次回検証出力の予定場所はSP-017で指定する。既存ログ、Temp/Library、Stamps、session、stash、別worktreeは削除しない。buildの大容量出力は既存Builds等と容量を照合してから選び、今回新しい保存root・junction・キャッシュ移動を導入しない。

復旧は、失敗時の版・途中成果・ログ・dirty一覧を保持し、今回の新規候補だけを担当が識別する。reset/clean/既存Sceneの上書きで戻さない。次回は同じ入力の記録から失敗工程へ戻る。新しいUPM障害を観測した場合だけ対象版・端末・実エラーを記録し、修復・依存変更に進まず独立した資料整理を続ける。過去の拒否の対象をpath/command/担当変更で取り直さない。

素材の利用条件は個別入力の由来に結び付ける。2026-11-01にDeformのlock版MIT本文と、Assets/PluginsのClipper Boost / Poly2Tri BSD表記を確認。URP/ProBuilder、TMP同梱フォント、各Material/Texture/Heightmapの実配布条件・権利・購入主体は一括確認できていない。下記一般規約を個々の素材の許可証にしない。生成物のローカル利用、編集可能なPrefab/素材の再配布、Playerへの同梱、公開は別に扱う。

公式参照（確認日2026-11-01）：[Unity 6000.3 ScriptableObject](https://docs.unity3d.com/6000.3/Documentation/Manual/class-ScriptableObject.html)（Editorのasset保存とbuildでの利用）、[Deform lock版LICENSE](https://github.com/keenanwoodall/Deform/blob/9e57dd3864ea6c742d819959724c99ca574a2f76/LICENSE.txt)（通知保持条件）、[Asset Store EULA §2.2/2.3/2.7](https://unity.com/legal/as-terms)（素材ごとの適用条件・Restricted Asset・OSSを照合）。購入・アップロード・外部AI送信・公開・外部設定変更は今回の準備から自動承認しない。

### 次の判断が必要になる条件

今回の準備を終えるための質問はない。最小検証のArchは既存SP-017の技術fixtureであり、人工構造物の様式や基準作を選んだことにはならない。広い景観制作へ移る時は、保存往復した実物を用いて、巨大さを読む距離・地形との関係・形状の方向を具体的に比較し、本人の選択を得る。配布先を変える場合は必要な出力形式と端末を確認する。これらの将来判断で現在の技術観測を止めない。

## 9. 2026-11-01 SP-017検証からの返却

本人が実行用missionを発行し、同じaccd587と準備文書を入力にrun `20261101-0328-sp017` を開始した。証拠は `artifacts/restart-20261101/20261101-0328-sp017/RESULT.md`、現在の操作と保留理由はruntime-state。新規Scene/sessionのみを使い、実装・依存修復・commit/pushへ広げない。

| 全工程への返却 | 今回の実測 | 残る出口 |
|---|---|---|
| readiness | 6000.3.6f1 compile exit 0、EditMode 596/596、PlayMode 9/9。各一回、失敗/skip 0 | 通常GUIのUPM path undefined / offline package-list errorを別記。修復・全package機能の受入ではない |
| Stage 1 形状制作 | **FAIL（一回）**。本人のGeneratorドッキング後、Arch既定寸法/分割数、scale5/spawn原点でGenerate。CreateManualArch:314の4インデックスFaceがProBuilder 6.0.8の長さ%3要求に反し例外 | Mesh作成前に失敗。ソース修正は今回範囲外。全quadの限定三角化と同条件再検証を未採用の次作業案として返す |
| Stage 2 Export | 未実施 | unique Prefab/Definition、元対象不変、Mesh/Material/タグの保存往復 |
| Stage 3 SO設定 | 未実施 | 新規Terrain設定、Definition設定、既存session読取互換。SP-018/019拡張へは進んでいない |
| Stage 4 地形配置 | 未実施 | 一チャンク・実在明示セルの個数/位置/高さ/寸法を事前期待値と照合 |
| 保存・再開 | 新Scene/session保存、新session Save/New/Loadの固有名/seed/notes復元。Cockpit Apply/Undoで対象一件復元・未選択対象不変 | Arch手編集・参照・Editor再起動後の再生成は未実施。sessionをScene/形状保存としない |
| 利用先・採用 | 未実施 | 巨大景観、複数チャンク、build/性能、本人の形状・操作感・配布判断は今回のテスト成功から推定しない |

区切り時点の前後照合で既存tracked 1,648ファイルの全hash一致、開始時のAssets/Packages/ProjectSettings全ファイルも変更/欠落0。新規Scene/session/metaは6件、構造error 0、project-state/diff検査成功。既存自動UI Migration処理によるレポート生成日時の変更、profile側GiCache作成、Library/Temp/Logs/UserSettings等の派生書込みを記録し保持した。指定書込み先との相違を含め無条件合格にはしない。独立窓の制約は本人によるCockpitドッキング後に解消した範囲だけを記録し、操作不能を製品コード欠陥と断定しない。技術未確認項目を未実装扱いして再実装しない。

本人のGeneratorドッキング対応は完了し、現在の障害は上記のArch面インデックス。Editor PID16004は正常終了した。今回の検証バッチは範囲外の修正が必要な生成失敗に到達して閉じ、ソース/依存修正で延長しない。次は本人が修正を含む次作業を依頼した場合、担当する開発AIがCreateManualArchの三角化を検討し、同条件Arch→Export→一チャンク配置→保存/再開へ戻る。未採用案で次担当未割当。例外/該当行/入力/保存SceneはRESULT.mdから参照する。既存テスト成功からArchの通常生成が保証されるとはしない。今回の取得済み証拠を別の実行日/入力の新ゲート成功へ転記しない。

## 10. 2026-11-01 Arch・保存・配置の限定修正

本人の続行・方向修正依頼で、前節の未採用案を実装・検証へ進めた。ローカルcommit `af36a0e`、証拠は `artifacts/restart-20261101/20261101-0430-arch-fix/RESULT.md`。既存差分と前回runを保持し、push・依存/設定修復・巨大景観へは広げない。

| 全工程への返却 | 今回の結果 | 残る確認 |
|---|---|---|
| Stage 1 | ArchのFace三角化後、通常GUIで同条件生成が成功。68頂点、Object Size(15,20,25)。分割数4/16/64の閉鎖性・向き・boundsテスト成功 | 空パレットのProBuilderDefaultはマゼンタ表示。本人の形状/素材/景観採用は未実施 |
| Stage 2 | 最初のGUI Exportで元Mesh消失/Prefabの空Mesh参照を観測し、元Mesh保全・独立Mesh資産保存へ限定修正。再import後の参照/素材path、元対象・非表示の子・Colliderを回帰テストで確認 | 修正版の通常GUI Export、Editor再起動後の実物参照は未確認。旧失敗候補は保持 |
| Stage 3/4 | 親座標を高さサンプラーへ渡し、元Prefab localScaleに配置倍率を掛ける修正。親(132,0,232)、観測cell0、期待XZ=(130.5927,231.4375)と倍率1/0.75のテスト成功 | 新Terrain SO/実地形一チャンク、手編集、Scene/Editor再読込と新Playでの再配置は未実施 |
| ゲート・再開 | 最終compile成功、EditMode603/603、PlayMode9/9、失敗/skip0。修正は3既存C#、追加回帰7件 | 再起動Editor PID38072の画面取得/前面化が foreground window did not report a process id で失敗。本人へ前面化のみを依頼済み |

次は前面化後に担当AIがEditorの保存状態を確認し、SP-017の別Scene/候補 `_MeshFix` から通常GUIの保存・配置を再開する。全体合格が出る前にSP-018/019拡張や巨大景観へ移らない。保存元の編集データとExport済み候補を区別し、手編集の自動伝播や反復Buildの置換安全性は未検証のままとする。

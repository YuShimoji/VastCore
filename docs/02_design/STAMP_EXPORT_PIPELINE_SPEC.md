> **上位SSOT**: [SSOT_WORLD.md](../SSOT_WORLD.md) | **索引**: [spec-index.json](../spec-index.json) SP-017

# SP-017: Stamp Export Pipeline Spec

**Status:** partial (75%)
**Version:** v1.0
**Last Updated:** 2026-11-21

下部の読み方：「2026-11-21 次回集中セッションの実行用依頼」が次に使う依頼文、「2026-11-01 最小検証仕様」がその手順と出口、「2026-11-01 の実行結果（要約）」が工程ごとの現状。partial状態は維持し、実機検証済みへ変更しない。

---

## 概要

StructureGenerator で生成した構造物メッシュを PrefabStampDefinition に変換し、
DualGrid 配置パイプラインに接続する。

これにより「構造物生成 → 配置 → 地形表示」のエンドツーエンドフローが成立する。

## ミッション接続

**ミッション**: 広大な景観に映える、ユニークで巨大な人工構造物をプロシージャルに生成する

このスペックが解決する分断:
- StructureGenerator (22ファイル/8タブ) — メッシュ生成
- DualGrid + PrefabStampPlacer — グリッド配置
- **欠落していたもの**: 両者をつなぐ変換レイヤー

## データフロー

```
StructureGenerator (任意のタブ)
  → GameObject (MeshFilter + MeshRenderer) をシーンに生成
    → StampExporter.ExportAsStamp()
      → Assets/Resources/Stamps/Prefabs/{name}.prefab
      → Assets/Resources/Stamps/Definitions/{name}_StampDef.asset
        → PrefabStampDefinition (ScriptableObject)
          → TerrainWithStampsBootstrap.stampDefinition にアサイン
            → StampRegistry.Place() → PrefabStampPlacer.InstantiateAll()
              → 地形上に構造物が表示
```

## コンポーネント

### StampExporter (Editor-only static class)

- `ExportAsStamp(GameObject target) → PrefabStampDefinition`
- 入力: シーン上の任意の MeshFilter 付き GameObject
- 出力:
  1. Prefab アセット (.prefab)
  2. PrefabStampDefinition アセット (.asset)
  3. 生成時の一時Meshは `Assets/Resources/Stamps/Meshes/` の独立アセットへ保存する。既存Meshアセットは参照を保持する。
- デフォルト設定: Step90回転、TopOfStack高さ、0.8-1.2xスケール範囲
- 位置/回転はリセットしてPrefab化
- Export用コピーのProBuilderコンポーネントを除去して標準MeshのPrefabとする。元SceneのProBuilder編集データ、Mesh、Transformは保持する。

### StructureGeneratorWindow UI

- "Export as Stamp" ボタン（Selection.activeGameObjectがMeshFilter付きの場合のみ表示）
- エクスポート後にInspectorでDefinitionを選択・表示

## フォルダ構成

```
Assets/Resources/Stamps/
  ├── Prefabs/       ← エクスポートされた Prefab
  ├── Definitions/   ← PrefabStampDefinition SO
  └── Meshes/        ← 一時生成Meshの保存先（元Meshと分離）
```

## 体験スライス: "1構造物が地形に立つ"

1. StructureGenerator > Basic Shapes > Arch を生成
2. シーン上の Arch を選択
3. "Export as Stamp" ボタンをクリック
4. 生成された StampDef を TerrainWithStampsBootstrap.stampDefinition にアサイン
5. Play → DualGrid セル上に Arch が配置される

### 成功条件

- [ ] Prefab が Assets/Resources/Stamps/Prefabs/ に保存される
- [ ] StampDef が Assets/Resources/Stamps/Definitions/ に保存される
- [ ] StampDef.Prefab が正しく参照されている
- [ ] TerrainWithStampsBootstrap 経由で地形上に配置される

## 制約

- Editor-only（ランタイムでの動的エクスポートは対象外）
- 単一セル配置のみ（マルチセルフットプリントの自動推定は将来課題）
- マテリアルは元のGameObjectのものを保持（StampDefinition側では管理しない）

## 将来拡張

- バッチエクスポート（複数構造物の一括変換）
- フットプリント自動推定（メッシュバウンズから占有セル数を算出）
- バリエーション生成（パラメータ変異→複数Prefab→StampDefinition群）
- StructureGenerator内でのプレビュー（配置シミュレーション表示）

## 2026-11-21 次回集中セッションの実行用依頼（未発行）

2026-11-01の二つのrunの続き。生成の修正（`af36a0e`）までは済み、通常GUIでの修正版Export以降が残っている。検証の手順と許容差は下の「2026-11-01 最小検証仕様」と、thank端末の `artifacts/restart-20261101/20261101-0430-arch-fix/fixture-plan.md` を使う。別端末にはfixture-planが無いため、必要な固定入力を下に転記した。

| 固定入力 | 値（2026-11-01の実測・事前期待） |
|---|---|
| Arch生成 | segments 16、width 3、height 4、thickness 0.5、depth 1、count 1、Global scale 5、spawn原点、空のmaterial palette。期待はraw bounds (3,4,5)、root scale (5,5,5)、world bounds (15,20,25)、68頂点 |
| 地形 | resolution 33、worldSize 64、heightScale 8、erosion null、noise既定seed 12345 |
| Bootstrap | gridX/Z 1、worldOrigin (100,200)、dualGridRadius 1、dualGridSeed 42、jitter 0、stampSeed 42。Definitionは Fixed / scaleRange (1,1) / jitter 0 |
| 期待位置 | terrain X[100,164] / Z[200,264]、grid親 (132,0,232)、cell0のcenter (-1.407291,0,-0.5625)、stamp XZ (130.592709, 231.4375)。Yはterrain height＋記録したlayer offset |
| 許容差 | 位置・高さ・bounds 0.001m、回転0.01度、scale 0.0001。Meshの頂点/triangle数、素材の同一性、個数1は一致 |
| 手編集 | Export後の元Archをx=2へ移動し、別に保存する。Export済み候補へ自動反映されたとは扱わない |

```text
[VASTCORE MISSION]
Outcome: af36a0eで修正したArchを、通常GUIで編集元Scene → Prefab Stamp（_MeshFix候補）→ 再読込 → 原点以外の一チャンク地形へ同条件で再配置できるかを、保全証拠付きで項目別に判定する。
Why now: 修正はテストで確認済みだが、通常GUIでの保存・配置・再起動の往復は未実施で、SP-017の技術出口が閉じていない。11-01のEditorは前面化されないまま11-20の再起動で消えた。
Start from: C:/Users/thank/Storage/Game Projects/VastCore_TerrainEngine/VastCore（別端末なら同じrepoのcheckout）
  branch codex/vc-development-readiness-20260711、2026-11-21の統合commit以降。開始時にHEAD・dirty一覧・Unityプロセス・lockfileを記録する。
Read: AGENTS.md → docs/REPO_LOCAL_RULES.md → docs/runtime-state.md → 本SP-017のこの節と「最小検証仕様」の「操作と出口」。
  thankなら artifacts/restart-20261101/20261101-0430-arch-fix/RESULT.md と fixture-plan.md も読む。
Owner: 実行を受けた開発AI一名が検証・証拠・所有文書の更新を担当。本人は形状・素材・景観・操作感の採用と、Cockpitのaccept/reviseを担当。
  .serena差分、LEGACY_UI_MIGRATION_REPORTの差分、既存Scene/session/Stamps、旧失敗候補、stash、他worktreeを保全する。
Scope: Unity 6000.3.6f1の起動、別端末ならcompile/EditMode/PlayModeを一度ずつ、_MeshFix Scene/候補でのExport、新規Terrain SOとBootstrapでの一チャンク配置、手編集保存、Scene再読込、Editor再起動、新しいPlayでの再配置比較。
  この出口を閉じるのに必要な限定修正は可（2026-11-01の二回目のrunと同じ扱い）。asmdef・依存・Packages・ProjectSettings・serialized名/enum/GUIDの変更、UPM/Unity修復、素材購入、外部AI送信、公開は含めない。
Write: 新規 Assets/Data/VastCore/RestartValidation/<run>/ のScene・地形設定SO・meta、Exporter既定の Stamps/Prefabs・Definitions・Meshes へ _MeshFix 付きのunique新規asset。
  修正する場合は該当C#と回帰テスト。docs/runtime-state.md、docs/project-context.md、本SP-017、SP-020の観測差分。
Output: artifacts/restart-<日付>/<run>/ に入力一覧とhash、前後保全、compile/test logとXML、画面、RESULT.md。既存runを上書きしない。
Checks: node scripts/codex/check.mjs --require-assets、scripts/check-project-state.ps1、git diff --check。
  コードを変えたら scripts/check-compile.ps1 と scripts/run-tests.ps1 -RequireNonZeroTests を、6000.3.6f1の明示UnityPath・ProjectPath・固有出力先で実行する。
Budget: 一担当、Export候補は一件（手編集後の比較で必要なら二件まで）、地形一チャンク、明示セル一件、再読込後の再生成一回。
Failure: 画面操作ツールが対象Editorを取得できない場合は、その操作だけを止めて本人へ具体的な一操作を頼み、Editorを強制終了しない。
  新しい失敗は対象と操作を限定して一度だけ切り分ける。拒否された操作をコマンド・保存先・担当を替えて取り直さない。
Acceptance: 「最小検証仕様」の技術出口を項目別に判定する。本人の採用・操作感・配布は代筆しない。
Git: 検証して読み戻したら同じbranchへcommitし、通常のpushまで行う。
Next: 成功範囲と失敗工程をSP-020へ返す。URP素材の候補比較、SP-018/019、巨大景観は、この出口の後の別の成果として扱う。
```

## 2026-11-01 最小検証仕様（未実行）

見出しの「未実行」は作成時点の表記（他文書のリンク先として保持）。2026-11-01に二度実行し、現状は下の「実行結果（要約）」にある。

全工程の所有者は [SP-020](DESIGNER_PIPELINE_SPEC.md#8-2026-11-01-再開準備採用済み経路と今回の差分)。本検証で最も先に崩したい前提は「既存の生成形状をPrefab Stampとして保存し、Editorを開き直しても同じ入力から地形上へ再配置できる」。UIの一操作だけでは、製品目的へ届くこの接続と永続化を確かめられないため選ぶ。既存SP-017のArchを技術fixtureとして使い、景観・様式の基準作には採用しない。

比較した検証範囲：Cockpit Apply/Undoだけなら操作安全性は分かるが生成・保存・配置を通らない。巨大景観一式は未確認のメッシュ永続化や単一チャンク制限を含めて費用が広がる。よって一構造物・一チャンク・明示セルを推奨する。これは検証設計上の推奨であり実行の着手記録ではない。

### 入力と保全

- 基準HEAD：`accd5871fb2eadc825c7101df9df0548c5205cae`、branch `codex/vc-development-readiness-20260711`、Unity `6000.3.6f1`。
- 同HEAD上の2026-11-01準備文書差分を含む。SP-020入力SHA256：`728dd1eb07904d7fb74fcfebb519e63adf3bf5f7ba455f2a0dab6e3340eba766`。開始時にHEAD、文書hash、dirty一覧を照合して記録する。相違はまず差分を読み、無関係な修正を消したり古い版へ戻したりしない。
- 入力現物：`Assets/Editor/StructureGenerator/Tabs/Generation/BasicStructureTab.cs` のArch、`Assets/Editor/StructureGenerator/Core/StampExporter.cs`、`Assets/Scripts/Terrain/TerrainWithStampsBootstrap.cs`、`Assets/Scripts/Terrain/DualGrid/{PrefabStampDefinition,StampRegistry,PrefabStampPlacer}.cs`。
- 読み込む旧session：`Assets/Data/VastCore/DesignerSessions/Designer_Session.asset`。これは読取互換確認用で、保存先にしない。新sessionだけでSave/New/Loadを試す。
- 2026-11-01開始時の変更は `.serena/project.yml` のみ。Assets/Packages/ProjectSettingsと同ファイルのtracked 1,648ファイル、198,712,827 bytesの保全基準はSHA256 `d18d91cfbe630a7b8236c0987d9002fab77b12eb7b9698d63bd6d7cbac8d018c`。方式はpath昇順にpath＋NUL＋各ファイルSHA256バイナリを連結してSHA256。次回はその時点の差分も含めて新たに採る。
- 元Scene、Prefab、SO、meta/GUID、手編集、stash、他worktreeを上書きしない。既存素材は複製して編集するか読取参照に留める。Editorにdirty Scene/assetがあれば、担当の保存状態を確認するまでSaveAssets、Scene切替、Play、batch起動をしない。保存・破棄・強制終了を代行しない。

### 操作と出口

1. 同じ入力の保存状態・担当・拒否履歴を再確認する。Unity 6000.3.6f1の実行可否は今回未検証なので、修復を前提にしない。compile→non-zero EditMode→non-zero PlayModeを一度ずつ実行し、host/HEAD/Unity版/終了コード/テスト数/失敗数を残す。失敗したgateより先の実操作を成功扱いしない。
2. [Cockpit smoke](../DESIGNER_COCKPIT_SMOKE_TEST.md) を新規検証Sceneと新sessionで一バッチ実施する。無選択でApply不可、対象一件のApply/Undo、新sessionのSave/New/Loadを確認。元Sceneと旧sessionのbyte不変を確認する。Cockpitの画面受入は技術操作の結果と本人のaccept/reviseを分ける。
3. 空の検証Sceneに、通常メニュー `Tools/Vastcore/Structure Generator` からArch一件を生成する。実際の全形状パラメータ、global scale/spawn、root transform、Mesh頂点/triangle数とbounds、素材pathを記録する。比較用に二つ目の未選択オブジェクトを置き、変更対象外であることも記録する。外観案の大量生成・CSG symbol切替・Deform追加は不要。
4. 元Archの名前を今回の候補prefixにし、Export as Stampを一度実行。Prefab/Definitionの新規pathとGUID、Mesh/Material参照、TagProfile、footprint、scale/rotation ruleを記録する。入力のtransform不変を確認する。Exporterが生成メッシュを保存できたかは、asset依存と再読込後のMeshから判断し、その場の表示だけで通さない。
5. 新規TerrainGenerationConfigとNoiseHeightmapSettingsを通常のCreate/Inspector操作で作る。設定値を全て記録する。既存Bootstrap一括Creatorを通常素材へ実行しない。BootstrapはgridX=gridZ=1、DualGrid radius=1、seed固定、定義一件、実在セル一件を明示指定、jitter=0、scale range=(1,1)、rotation Fixedを検証用Definitionへ設定する。config/worldSize/resolution/heightScaleと地形seed、dualGridSeed、stampSeedを明示する。worldOriginを原点以外にして、grid親の移動を含む期待ワールド位置と先頭チャンク範囲を入力から先に定める。セルIDは観測してから記録し、存在を推測しない。実出力に合わせて原点や期待位置を変更しない。
6. Edit Modeの入力SceneとSOを保存する。PlayでBootstrapを一度実行し、配置root/個数/位置/回転/scale、Mesh数/bounds、素材参照、地形高さとの関係、Consoleを記録する。地形だけ、あるいは無効なDefinitionをskipして出たBuild completeメッセージは合格にしない。技術許容差はfixtureの寸法と単位から実行前に明記し、結果を見て緩めない。
7. Play終了後、保存した検証Sceneを閉じて開き直し、PrefabとSOの参照、元Arch形状・手編集、session設定を再照合する。対象Editorだけを正常終了・再起動できる条件ならEditor再起動も一度行い、できなければ「Scene再読込まで／Editor再起動未実施」として永続化の完全合格を残さない。新しいPlayで同じ入力を一度だけ生成し、個数、対象セル、transform、Mesh/素材を比較する。runtime InstanceIDやGameObject名だけを永続IDにしない。
8. 同一instanceへの連続Buildは重複の恐れがあるため、この試験の再生成は新しいPlayで行う。生成結果の手編集保存やBuildの置換安全性は合格対象外と明記し、元Archに一つ手編集を行いScene保存・再読込で保持されることを別に確認する。その手編集がExport済み候補へ自動反映されたと扱わない。新候補の再Exportが必要なら予算内の二度目として別名に保存し旧候補を保持する。
9. 前後の保全照合と、全操作の実施／未実施／失敗、本人の景観・形状・操作感の採用判断は未実施であることをまとめる。実際の本人判断が得られた場合だけ、その対象版と範囲を記録する。失敗に関係するファイル・入力・出力・ログと次に変える条件を所有仕様へ返す。スクリーンショットにはScene/対象/版/Play状態を添える。

技術合格の範囲は一件の編集元、Export依存、Scene/SO/session別保存、再読込と同条件再配置、対象外保全まで。巨大構造物、マルチセル、複数チャンク、Solverの規則効果、SP-018の素材/子切替、全旧データ移行、Player build、性能、人の採用、権利・公開はこの合格から推定しない。保存できない形状パラメータや手編集・再生成の断絶が見つかったら、不足項目と最小修正候補をSP-020へ返し、仕様やserializationを無断変更しない。

### 実行用依頼

2026-11-01版の依頼文は本人が発行し、実行済み。原文は [再開履歴](../restart/VC_RESTART_20261101_HISTORY.md) へ移した。次回の依頼は上の「2026-11-21 次回集中セッションの実行用依頼」を使う。

## 2026-11-01 の実行結果（要約）

run `20261101-0328-sp017` と `20261101-0430-arch-fix` の原文は [再開履歴](../restart/VC_RESTART_20261101_HISTORY.md) へ移した。証拠はthank端末の `artifacts/restart-20261101/<run>/RESULT.md`。

| 工程 | 結果 | 確認方法 | 残り |
|---|---|---|---|
| ゲート | `af36a0e` でcompile exit 0、EditMode 603/603、PlayMode 9/9 | `final-gates/` のlog・XML | 別端末では未確認 |
| Arch生成 | 最初のrunでFace例外。`af36a0e` で四角面を三角化し、通常GUIで生成成功（68頂点、Object Size(15,20,25)） | GUI操作と専用テスト3件 | 空パレットのProBuilderDefaultがマゼンタ表示 |
| Export | 修正前のGUI Exportで元Mesh消失を観測。元Meshの保全と独立Mesh資産の保存へ修正 | 回帰テスト2件（再import後の参照を含む） | 修正版での通常GUI Export |
| 配置 | 親座標の無視と元scaleの上書きを修正。期待XZ (130.5927, 231.4375) | 回帰テスト2件 | 実地形一チャンクでのGUI配置 |
| 保存・再開 | Cockpit sessionのSave/New/Load、一件のApply/Undo | GUI操作（最初のrun） | 手編集の保存、Scene再読込、Editor再起動後の再配置 |
| 本人の採用 | 未実施 | — | 形状・素材・景観・操作感 |

旧失敗候補（`RestartValidation_20261101_0430_Arch` のPrefab・StampDef）は保持し、合格候補には使わない。

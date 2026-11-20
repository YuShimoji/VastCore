> **上位SSOT**: [SSOT_WORLD.md](../SSOT_WORLD.md) | **索引**: [spec-index.json](../spec-index.json) SP-017

# SP-017: Stamp Export Pipeline Spec

**Status:** partial (75%)
**Version:** v1.0
**Last Updated:** 2026-11-01

2026-11-01追記：最小検証仕様と実行用依頼を下部に追加。上記partial状態は維持し、実機検証済みへ変更しない。

同日実行追記：本人発行のmissionによりrun `20261101-0328-sp017` を開始した。下部の「未実行」「未発行」は準備時点の状態を保存したもの。現在の観測は末尾の実行記録を参照。SP-017全体合格へは変更しない。

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

## 2026-11-01 最小検証仕様（未実行）

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

### 実行用依頼（未発行・未実行）

以下は次回本人が検証を依頼する際にそのまま使える。今回の準備依頼では実行を開始しない。出力先・時間はこの検証に対する提案で、製品全体の予算・配布方針ではない。

```text
[VASTCORE MISSION]
Outcome: 既存Archを編集元SceneからPrefab Stampへ保存し、再読込後に同条件で一チャンク地形へ再配置できるかを、保全証拠付きで判定する。
Why now: SP-017の保存・再配置は製品目的への接続であり、Cockpit操作の成功だけでは保証されない。
Start from: C:/Users/thank/Storage/Game Projects/VastCore_TerrainEngine/VastCore
  branch codex/vc-development-readiness-20260711
  HEAD accd5871fb2eadc825c7101df9df0548c5205cae と2026-11-01準備文書差分。
  入力版はこのSP-017の「入力と保全」。HEADや文書の差があれば変更内容を照合する。
Read: AGENTS.md → docs/REPO_LOCAL_RULES.md → docs/runtime-state.md。
  追加は本SP-017、SP-020の再開準備、Cockpit smoke、使う生成/配置/保存コードに限定。
Owner: 実行を受けた開発AI一名が検証・証拠・所有文書更新を担当。本人は形状/景観/操作感の採用。
  他の編集者・Scene・生成物・.serena差分を保全する。他chat/agentを起動・dispatchしない。
Scope: 上の最小検証を一バッチで行う。Unity起動/import、compile、既存テスト、隔離した新規検証assetsと通常操作の確認まで。
  ソース実装、UPM/Unity修復、Packages/ProjectSettings変更、依存追加、素材購入、外部AI送信、公開、pushは含めない。commitもこの検証では行わない。
Write: 新規 Assets/Data/VastCore/RestartValidation/20261101-<run>/ 以下のSceneと地形設定SO、対応meta。
  新規sessionは Assets/Data/VastCore/DesignerSessions/ 内の RestartValidation_<run> 名だけ。
  Exporterの既定出力 Assets/Resources/Stamps/Prefabs/ と Definitions/ に同prefixのunique新規assetsだけ。
  Unityに伴うLibrary/Temp/logの派生書込みは記録し、削除・修復しない。
  docs/runtime-state.md、docs/project-context.md、本SP-017、SP-020の観測差分だけを更新。
Output: 予定 artifacts/restart-20261101/<run>/ に入力一覧/hash、前後保全、compile/test logとXML、画面、結果Markdown。
  既存出力を再利用・上書きせず、runは開始時に日時等で衝突なく決めて記録する。
Checks: 構造検査 --require-assets、project-state、diff --check。
  scripts/check-compile.ps1 は6000.3.6f1の明示UnityPathとProjectPath、固有LogsDirを渡す。
  scripts/run-tests.ps1 は同じUnityPath/ProjectPath、固有ResultsDir/LogsDir、-RequireNonZeroTestsを使う。
  gate後にCockpit smokeと本仕様。既存dirty assetがある状態でSaveAssetsを呼ばない。
Budget: 提案上限90分、一担当、compile一回、EditMode一回、PlayMode一回、Cockpit一バッチ。
  Arch候補一件（手編集後の比較が必要なら二件まで）、地形一チャンク、明示セル一件、再読込後の再生成一回。
  予算切れは取得済み証拠と未実施項目を保存し、実装・修復で延長しない。課金API・新モデルCLIは使わない。
Failure: 拒否された対象/操作/理由/解除条件を保持し、コマンド・保存先・担当を替えて再試行しない。
  Unity/UPM障害は今回のhostと版で記録し、旧PLANNER007障害と区別する。診断は一度の限定切分けまで、修復しない。
  結果不明なら状態照合が先。未保存Scene、他担当の競合、破壊/依存/serialization境界に触れた操作だけ保留する。
  他Editorを終了せず、自分の検証Editorも正常終了できる範囲で扱う。途中成果とログを保持する。
Acceptance: 本仕様の技術出口を項目別に判定。本人の作品採用・操作感・配布承認は代筆しない。
Next: 成功範囲と失敗工程をSP-020の全工程へ返す。SP-018/019の拡張確認や高コストな景観制作は別の次成果として扱う。
```

## 2026-11-01 修正結果：20261101-0430-arch-fix

本人の続行・方向修正依頼により、前回の「修正未採用」から限定実装へ進んだ。結果は `artifacts/restart-20261101/20261101-0430-arch-fix/RESULT.md`。ローカルcommit `af36a0e`、pushなし。既存差分・前回run・失敗候補は保持し、SP-017全体の合格へ変更しない。

- Archのquadを同じ周回順の2三角形へ変換。前回と同じsegments16/寸法/globalScale5/空パレットで通常GUI生成に成功。68頂点、Object Size(15,20,25)、source root scale5。
- 生成後の最初のGUI Exportで元MeshがNone/Size0となり、PrefabにもMesh参照が残らないことを新たに実測した。コピーのProBuilder破棄が共有Meshを削除していた。完了ダイアログを保存合格としない。
- Exporterを元Mesh保全・独立Meshアセット保存へ修正。コピーのProBuilderを保護して除去し、元Sceneの編集データを保持する。動的MeshはMeshes/へunique保存し、既存Mesh資産参照を再利用、Colliderにも永続Meshを接続。フィールド/asmdef/依存は変更しない。
- 配置の専用テストで親座標無視と元scale上書きを確認して限定修正。親(132,0,232)、観測cell0の期待XZ=(130.5927,231.4375)、ワールド高さ取得、元Prefab localScaleへの倍率1/0.75を検査した。原点や期待値を実出力へ合わせて変更しない。
- 最終compile成功、EditMode603/603、PlayMode9/9、失敗/skip0。追加回帰7件はArch3、Exporter2、配置2。再import検査と通常GUI・Editor再起動往復を区別する。診断中のテスト入力/API互換失敗もRESULTに残す。
- 修正版GUI Export以降は未完了。最初のEditor PID34844は正常終了したが、再起動PID38072の画面取得/前面化が `foreground window did not report a process id` で失敗。本人へ前面化だけを依頼し、Editorは強制終了せず保持。復旧後、別Scene/候補 `_MeshFix` →新Terrain SO/原点以外の一チャンク/明示cell0→手編集保存→再読込・再起動往復へ進む。空パレットのProBuilderDefaultのマゼンタ表示は今回観測した別の表示課題で、素材参照の永続化成功と本人の視覚受入へ読み替えない。

以下は前回バッチの履歴であり、現在の「修正未採用」「次担当未割当」ではない。

## 2026-11-01 実行記録：20261101-0328-sp017

証拠・項目別判定の現物は `artifacts/restart-20261101/20261101-0328-sp017/RESULT.md`。入力HEAD/branch/SP-020 hashは「入力と保全」と一致。既存差分を保持し、実装・依存・設定修復・commit/pushは行わない。

- 実行前ゲート：構造error 0、project-state/diff検査成功、Unity 6000.3.6f1 compile exit 0、EditMode 596/596、PlayMode 9/9、失敗/skip 0。各一回、明示UnityPath/ProjectPath/固有出力先とRequireNonZeroTestsを使用。
- Cockpitの主要操作：新規検証Scene/sessionで無選択Apply不可、Save→New→Loadの固有名/seed=24680/notes保持、一件のMain Camera Apply/Undoを確認。未選択Directional Light不変はserialized比較でも確認。詳細範囲と各status-only画面を記録。全smoke項目・本人受入の完了ではない。
- 区切り時点の前後保全：Assets/Packages/ProjectSettingsと.serenaのtracked 1,648ファイル/198,712,827 bytesは全hash一致、準備時集計d18d91cf...018cと同一。開始時のAssets/Packages/ProjectSettings全ファイルも変更/欠落0、新規Scene/session/metaは6件。Unityの既存自動UIスキャンによる既存レポート生成日時一行の変更とprofile側GiCache作成は、指定書込み先との相違として記録し保持した。書込み範囲も含めた無条件合格にはしない。
- 今回host/版の通常GUIでもUPM path undefined / offline package-list errorを観測。修復せず、compile/tests成功と区別。独立ツール窓は本人によるCockpit/Generatorのドッキング後に操作できた。現在の阻害要因はドッキング待ちではない。
- **Stage 1 FAIL（一回）**：Archをsegments16、width3、height4、thickness0.5、depth1、count1、Global scale5、spawn原点、空パレットで生成し、`ArgumentException: Face indexes must be a multiple of 3.` が発生。BasicStructureTab.cs:314の4インデックスFaceと、導入済みProBuilder 6.0.8 Face.SetIndexesの長さ%3要求を一度照合した。ProBuilderMesh.Createより前に失敗し、HierarchyにArchなし、SceneはUndo後コピーと同hash。例外・ソース行・package版/hash・画面をrun内に保存。
- unique Prefab/Definition、Mesh/Material永続参照、Terrain SO、明示セル、初回配置、手編集保持、Scene/Editor再読込と同条件再配置は未実施。実入力は生成前にfixture-plan.mdへ記録し、既存EditorPrefsも変更していない。前段FAILからExporterや配置コード自体のFAILを推定しない。

本人の形状・景観・操作感・配布の採用は未実施。今回の範囲は検証のみでソース修正を含まないため、この失敗工程を記録してバッチを閉じる。別形状や依存更新で代用・延長しない。

Editor PID16004を正常終了し、04:02 JSTにUnityプロセス/lockなしとshutdown logを確認。構造377 C#/883 meta/error 0、project-state、diff --checkも通過。予算切れではなく範囲外の修正が必要な生成失敗による終了。残る目的・条件・担当はRESULT.mdの「残る操作と再開」に記録した。次の未採用案はCreateManualArchのループ4面/端面2面を向きを保った三角形インデックスへ変換し、別の修正依頼で同条件の生成→Export→再読込→配置を検証すること。次担当未割当、修正・再Generate未実施。SP-017保存往復全体は合格を出せず、下流の永続化/配置コード自体は未判定。

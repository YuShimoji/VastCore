# VastCore Runtime State

Last Updated: 2026-11-27

## Current Position

| Field | Value |
|---|---|
| Project | VastCore Terrain Engine |
| Branch | `codex/vc-development-readiness-20260711` |
| Integrated baseline | 湖と平野の筋 `9e825c5` と3D砂岩 `a7907cb` を統合。探索画面の拡大 `63e0fc9`、写真の縮尺の細部 `b8795a6`、現代試作 `439a92b`。以前の版は [履歴](archive/REGION_MAP_EXPLORATION_HISTORY.md) |
| Active outcome | SP-021 の現代試作。2都市・都市間幹線・空港・郊外の建物を地域データから配置し、広域と拡大の同じ座標で描く。探索画面は ×4 以上で細部へ切り替わる。1 画素 3 m 未満の窓では、街区の中を敷地・屋根・屋上の設備・樹木・車・車線・影まで描く（写真の縮尺の細部）。探索画面は ×512 まで拡大でき、×128 以上でこの細部へ切り替わる。 |
| Active artifact | `docs/02_design/REGION_MAP_EXPLORATION_SPEC.md` |
| Next mission | 探索画面で寄って見られるようにした写真の縮尺の細部（2026-11-24 の[建物の見え方の依頼](USER_REQUEST_LEDGER.md)）に、本人の確認を受ける。開発は工場・基地と中世・未来の都市への展開（順は開発担当の整理）。 |
| Current bottleneck | 写真の縮尺の細部は、探索画面で寄ってから絵が出るまで 10〜36 秒かかり、窓は画面より粗く輪郭がやわらかい（内訳と条件は SP-021 8節）。街区は 570 m の格子、建物の枠は1街区に最大9個のままで、細部はその枠の中を展開している（曲がった道・不整形の敷地は無い）。空港・郊外の建物は従来の面のまま。他時代・施設への展開が残る。地域の規模と採用解像度、平野の筋を減らすしきい値は未決（SP-021 8節・9節）。巨大構造物は形の画像の道具待ち（`01M821KYJMRRFKP1VY1F6Q1HS2`）。 |
| Change relation | 訂正（2026-11-24、本人「今現在のこのプロジェクトで間違った方向に進みつつあるので訂正してほしいです。」。本人が選んだ訂正は作業対象と進め方の2つ）。同日の差し戻し3件・動きの回答・段6の「保留」と所見「色々ゲーム部分を勝手に決めすぎ」も同じ向き。2026-11-22 の依頼「複数のタスクの軸が考えられるため整理して、そのまま可能なら開発を継続」で開発担当が作った軸 A〜H の順（SP-021 4.1節）は、次の作業の順として使わない |

目的は [SSOT_WORLD](SSOT_WORLD.md) 1節と1.1節。目標アンカー：

- 開発目標：seedから地域一帯の詳細な鳥瞰図を自動で作り、その上にポイントクリック的な宝探しの探索ゲームを載せられる。
- 今回の増分：現代の土地利用を地形に重ね、鳥瞰図で2都市と幹線と空港を見分け、拡大して街区・道路・施設の接続を確かめられる。さらに寄ると、建物・屋根・樹木・車が航空写真の縮尺で見える（探索画面では ×128 以上）。
- 現在のボトルネック：上の表の Current bottleneck。

全体工程（段1〜6）と現在地の表は [SP-021](02_design/REGION_MAP_EXPLORATION_SPEC.md) 4節が持つ。SP-017 / SP-020（Arch・Prefab Stamp・DualGridの経路）は未実施のまま保留で、2026-11-21再開準備時の状態本文は [再開履歴](restart/VC_RESTART_20261101_HISTORY.md) の「2026-11-21 thank版（SP-017再開準備）」へ原文のまま移した。`af36a0e` の当時の確認（EditMode 603/603・PlayMode 9/9、修正版の通常GUI往復は未実施、そのコードは以後変えていない）も同じ節。

## 作業ツリーと端末の現物（2026-11-22 thank観測）

- SP-021で追加したもの：`Assets/Scripts/RegionMap/`（Core・View・Editor、asmdef 2件）、`Assets/Scenes/RegionExplorer.unity`、`Assets/Tests/EditMode/RegionMapTests.cs`、`tools/RegionMapPreview/`、`scripts/region-map.ps1`。`1982fdd` の後に足したのは `Core/RegionDetail.cs`（拡大した地形）・`Core/RegionStructures.cs`（構造物の形の案）・`Core/RegionNames.cs`（呼び名の響き）の3件。変えたのは `Core/` の `RegionData`・`RegionGenerator`・`RegionSite`・`RegionAreas`・`RegionSpec`・`RegionMapRenderer`、テスト、`tools/RegionMapPreview/Program.cs`。
- 砂岩の台地の骨格・転石・固定視点は `48974a3`、表面材質は `ddb06ac`、植生は `0f70616`。通常シーンの崖下のアルコーブは `RegionMesaAlcove` が地層の境目を選び、`RegionAlcoveView` が局所の体積・崩落岩・しみ出しの緑・追加視点を組み込む。軸 G の旧記録は履歴の「2026-11-25 3D化の見え方の第1区切り」。
- DS-011 Props（2026-11-27）：`Core/RegionJson.cs`・`RegionPropCatalog.cs`・`RegionPropPlacement.cs`・`View/RegionPropView.cs`・`RegionPropCatalogTests.cs` で `environment-props-catalog.json`（7ドメイン・38原型・V1〜V7）の P0 代表6種をプレースホルダで置く（既定 `RegionLookSpec.Props = false`）。実アセットの差し替え口は `View/RegionPropAssets.cs`・`Editor/RegionPropAssetDatabase.cs`・`Resources/RegionMap3D/RegionProp.shader`・`RegionPropAssetsTests.cs`（`Assets/Art/Models/<Domain>/<CatalogId>/`、既定 `PropsRealAssets = false`、[DS-011 8節](02_design/ENVIRONMENT_PROPS_CATALOG_SPEC.md)）。Props 用の Poly Haven 2組は `Assets/Art/Textures/`。
- 砂岩の通い道は地域探索の左下から seed 8 の地上を開く。W/Sで歩き、Tabで従来の岩棚と新PBR候補を比較し、Escで地図へ戻る。[本編Playerの撮影・描画計測](<D:/AI-Runs/GameProjects/operations/wilderness-variations-20261126/structure/vastcore/urp/index.html#native>)。岩棚の初期表示は従来版。
- 命名（2026-11-22 の最初の版と川岸、2026-11-23 の第2版）で足したのは `Core/RegionNaming.cs`・`Core/RegionNamingRules.cs`・`Core/RegionFeatures.cs` と `Assets/Tests/EditMode/RegionNamingTests.cs`。変えたのは `Core/` の `RegionNames`・`RegionSite`・`RegionAreas`・`RegionSpec`・`RegionData`、`View/RegionExplorer.cs`、`Editor/RegionExplorerSmoke.cs`、`tools/RegionMapPreview/Program.cs`。
- 拡大表示（2026-11-22〜23）で足したのは `View/RegionDetailLayer.cs`・`View/RegionRingGraphic.cs`・`Assets/Tests/EditMode/RegionDetailLayerTests.cs`（各 meta つき）。変えたのは `View/RegionExplorer.cs`（最大拡大 8→64、右上の札、サーチの輪、矢印キー1回分の移動量、標高の読み）と `Editor/RegionExplorerSmoke.cs`（拡大の確認の手順）。`Core/`・シーン・`ProjectSettings/`・`Packages/` は変更していない。
- 遠征の規則（軸 H、2026-11-23）で足したのは `Core/RegionExpedition.cs` と `Assets/Tests/EditMode/RegionExpeditionTests.cs`（各 meta つき）。変えたのは `Core/ExplorationSession.cs`（最寄りの地点 `Nearest`、`Reset()`）、`View/RegionExplorer.cs`（規則の切替、左の「遠征の残り」、右の「手掛かり」、地図の野営地・拠点の輪・線・方角の矢印、終わりの結果）、`Editor/RegionExplorerSmoke.cs`（遠征の撮影7枚）。
- 動き（軸 E、2026-11-23）で足したのは `View/RegionMotion.cs` と `Editor/RegionExplorerMotionCapture.cs`（各 meta つき）。変えたのは `View/RegionExplorer.cs`（部品ごとの動き、動きのスイッチ・長さ・C の3要素の入れ切り、右の手掛かりの下の地名を隠す、「。」などが行頭に1つだけ残る折り返しを防ぐ）と `Editor/RegionExplorerSmoke.cs`（動きが終わるまで待ってから撮る）。`Core/` は変えていない。
- 名前の出し方の4点（2026-11-23）で変えたのは `Core/RegionNaming.cs`・`Core/RegionNamingRules.cs`（地域ごとの経緯と音の質感、峰の規則）、`View/RegionExplorer.cs`（左上の地域の部品の「経緯・ことばの音」の行）、`Editor/RegionExplorerSmoke.cs`（別の地域2つの撮影）、`Assets/Tests/EditMode/RegionNamingTests.cs`（2件）。足したファイルは無い。
- 湖と平野の筋（2026-11-25、段1・2）で変えたのは `Core/` の `RegionGenerator`・`RegionLake`・`RegionData`・`RegionDetail`・`RegionSpec`、`tools/RegionMapPreview/` の `Program.cs`・`TerrainMeasurements.cs`、`RegionMapTests.cs`（湖のテスト1件）。足したファイルは無い。
- 命名の第3版（2026-11-25）で変えたのは `Core/RegionNaming.cs`・`Core/RegionNamingRules.cs`（規則1〜5）と `Assets/Tests/EditMode/RegionNamingTests.cs`。確認ページは `review-20261125-naming-system/`。
- 確認ページの一覧と音（軸 F）のファイルの行（2026-11-22〜25）は [履歴](archive/REGION_MAP_EXPLORATION_HISTORY.md) の「2026-11-27 文書の上限のために正本から移した行」へ原文のまま移した（2026-11-27）。
- 未commitのまま保持するもの（前から同じ）：`.serena/project.yml`、`docs/04_reports/LEGACY_UI_MIGRATION_REPORT.md` の差分、untrackedの `Assets/Resources/Stamps/`（旧失敗候補）とそのmeta。どれも上書き・削除しない。
- 生成物は ignored の `artifacts/region-map/`（鳥瞰図PNG、探索画面の画像 `explorer-*.png`、下の確認ページ、ラフの元 `rough-20261122-screen/`）と `artifacts/logs/region-map-*.log`。thank端末にだけある。
- Unityのバッチ起動は、Unity Hubが起動していないと `No valid Unity Editor license found`（exit 198）で失敗する。本人がこのプロジェクトを Editor で開いている間は、バッチも2つ目の Editor も起動できない（`another Unity instance is running`）。その間の確認用に、`D:\vcv\VastCore`（Assets・Packages・ProjectSettings・scripts の写しと、そこで取り込んだ Library。git 管理外）を残してある。要らなくなったらフォルダごと消してよい。
- 残しているローカル限定の履歴（`master`、`feature/mcp-verification`、stash 2件）とworktree `.codex-tmp/cloud-codex-20261011/VastCore` は再開準備時のまま。

## Current Trust Assessment

- 再開確認（PLANNER007、2026-11-27、`0eea2e1`）：Unity 6000.3.6f1 の EditMode 702/702・PlayMode 9/9。状態・素材検査と RegionMapPreview Release ビルドも通過。2026-11-25 統合版も同じ検査を通過（EditMode 666/666・PlayMode 9/9）。
- thank / 2026-11-25（湖と平野の筋、SP-021 段1・2）：.NET で seed 1〜12 を変更前と変更後で生成。1024²（前後を交互に、中央値）：湖 1→38 個・0.25→353 km²（最大 0.25→101 km²）、海へ出ない流れの行き止まり 合計7→0 セル、平野の筋 旧い数え方 45.7→38.2 km²・新しい数え方（起伏の向きがそろう所）158.6→173.1 km²、雪 0.74→0.74%、低地 48.5→42.8%、生成 13.7→17.0 秒（侵食 10.1→13.3 秒）。512²：湖 1→42、行き止まり 1→0。2048²：湖 1→35・440 km²、行き止まり 40→0、筋 104.3→90.4（旧）・35.6→47.5 km²（新）、生成 約96 秒・ピーク 0.58〜0.84 GB、PNG 5.7 MB。
- 同（続き）：4096² は湖の上限を入れる前の版の seed 7 で 455 秒・1.45 GB・PNG 22 MB。seed 9 の現代試作は都市・空港の位置と街区 725 が変更前と一致。原因としきい値の数は SP-021 8節と[履歴](archive/REGION_MAP_EXPLORATION_HISTORY.md)。Unity 6000.3.6f1 で EditMode 655/655。絵は `artifacts/region-map/terrain-detail-20261125/`（`before3/`・`after2/`・`pairs/`）。未確認：探索画面（Unity）での湖の見え方と待ち時間、512²・2048² の時間の前後比較（EditMode と同時に走った）、4096² の最終の版、本人の確認。
- thank / Unity 6000.3.6f1 / 2026-11-25（命名の第3版。写し `D:\vcv\vcn`）：EditMode 654/654（失敗・skip 0）、探索の再生 終了コード 0・画面 30 枚（seed 7・8・9 の地図の名前と発見カードの別名・由来を目視）。.NET の seed 1〜12（探索画面の設定）で、規則1 99→0、規則2 87→0（古い呼び名 103→0）、規則4 79→0（1字の足し・抜き 12→0）、規則5 0→0、方角入り 57→9（9つとも理由あり）。数え方は [履歴](archive/REGION_MAP_EXPLORATION_HISTORY.md) の「2026-11-25 命名の第3版」。
- thank / 2026-11-25（探索画面の拡大）：探索シーンで seed 9 の4か所を ×64・×128・×256・×512 で撮った（`artifacts/region-map/explorer-photo-20261125/unity-final/`）。×128 以上で敷地・屋根・車・並木の影が出る。描画を速くした後も絵は変更前とバイト一致（14枚）。Unity 6000.3.6f1 で EditMode 653/653（失敗・スキップ0、窓の段のテスト1件を追加）。撮影4回のうち2回は16場面すべて出た。1回は ×512 の窓が240秒出ず（原因は未特定）、1回は Unity が起動時に終了した（Package Manager へ未接続）。開発担当が見て、雲が画面を覆う点を直した（×64〜×128 で消す）。本人の確認は未。
- [履歴](archive/REGION_MAP_EXPLORATION_HISTORY.md) へ原文のまま移したもの：SP-021 の地形の修正前・修正後の計測（PLANNER007、seed 1〜12、2行）と PLANNER007 での取り込み版 `9f7a732` の確認（1行）は「2026-11-24 写真の縮尺の細部」、2026-11-24 の確認2行は「2026-11-25 探索画面の拡大」、現代試作（PLANNER007）の確認1行と未確認（動き・音）の2行（今も効く）は「2026-11-25 湖と平野の筋」、命名の第2版・名前の出し方の本人の回答と未確認（4行。未確認は今も効く）は「2026-11-25 命名の第3版」。

- 拡大表示（2026-11-22〜23。EditMode 627/627）、遠征の規則（2026-11-23。639/639、遠征の7場面）、動き（2026-11-23。639/639、3組の撮影）、命名の第2版（2026-11-23。631/631）、名前の出し方（2026-11-23。641/641）の Unity での確認、.NET での拡大した地形・エリアと雲の測定（2026-11-22）、通常Editorでの実マウス・キーボードの確認と十字カーソルの直し（2026-11-21〜22）は [履歴](archive/REGION_MAP_EXPLORATION_HISTORY.md) へ原文のまま移した。音の既定を「なし」にした確認（2026-11-25、654/654）は同じ履歴の「2026-11-25 3D化の試作・段階 B（正本から移した行）」へ移した。3D砂岩の植生版とアルコーブの確認（1行）も同じ節へ移した（2026-11-26）。
- 本人の回答（拡大表示、2026-11-23、`01M81W8NN4XG16SQXMV2R3NBKT`）：「近い」。6領域（広域から細部へ切り替わる見え方・右上の札・細部の地形・広域の地図との一致・拡大中のサーチと発見とカーソル・縮めて広域へ戻る見え方）は全て「軽微（余裕があれば）」。操作のしやすさは5項目とも7段階の4。前の版より「良くなった」。次は「今回の続き」。記述は無し。
- 本人の回答（サーチのゲーム性、2026-11-23、`01M82CXPP4H2DKENWRZT0V0MGH`）：近い案は「組み合わせ」、記述「CとBがより近い。ゲームシステムとして。ナラティブについては要検討。」。足りないものは総当たり・見返り・目的と終わり・方角で、記述「モニュメント発見に寄っており、資材獲得や何らかの基地発見など、より広範で実利的な目的を持たせたい。」。「など」は地点の中身・探す目的と依頼・別の地域をまたぐ収集・拡大表示との連動で、ストアの記述は [USER_REQUEST_LEDGER](USER_REQUEST_LEDGER.md)。最初に作る大きさは「選んだ案の最小の実装まで」。
- 本人の回答（動きの案、2026-11-23、`01M828W77SDBJKNBXA22T8XJ8B`）：「B 地図で広がる」、記述「BとCを取り入れた動き方が良いです。」。4つの場面とも B。動きの長さは「長く（×1.6）」。進める範囲は「選んだ案の最小の実装まで」。削る動きは未回答。
- 本人の回答（2026-11-24）：動きの受入（`01M831H77K6HV96N1BYM6J0JZK`）は「一部直したい」。段6の残り3設問（`01M84QPNKRE5XXVTF20ZHEN8RF`）は「保留」。遠征の規則の受入（`01M82V588FZE0C05C0YP34DNGN`）は差し戻し。原文は [USER_REQUEST_LEDGER](USER_REQUEST_LEDGER.md)。
- 本人の回答（2026-11-22）：エリア・雲の流れ・地点の根拠を足した版（`01M80215JPBGRPF79KE7J0YQQF`）は「近い」、「エリアと地点の呼び名」だけ「直したい（次の前に）」、他は「軽微」、次は「ここで十分」。進め方（`01M80219XNGW9T27XQMXDZJQFG`）は SP-021 4.1節。拡大した地形（`01M81JA2CC4WC37TF841JJ8ZEE`）は「近い」・全項目「軽微」・次は「今回の続き」。呼び名と形（`01M81JA6HWR4TJQXZA6T07TC6J`）は SP-021 4.1節。命名の仕組みの方向（`01M81SA3GF8YMRZRETVZCHBGE2`、2026-11-23）は SP-021 5節。
- DS-011 Props（PLANNER007、2026-11-27）：プレースホルダは seed 9 で撮影・目視（EditMode 700/700）。実アセットは GLB を Blender で FBX にした物（`Assets/Art/Models/`）で seed 9・8 を撮り、実モデル 14・8 個、LOD0 三角形 64,572・31,628（[DS-011 8.1節](02_design/ENVIRONMENT_PROPS_CATALOG_SPEC.md)）。EditMode 717/717、texture audit 106 枚 error 0・warning 0。未確認：Props の GPU 時間、Weather Maker の空での比較。
- シームレス移動（2026-11-27）：seed 7 で地図→着地、B/F・P/V/Tab、Esc。Unity EditMode 734・PlayMode 13 件通過。着地2.44秒・p95 35.7ms／最大163.5ms。斜面に境界線。未確認：長距離走行、雲の個々の輪郭。

## Next Action

次の作業は SP-021 4節の工程表の段1・2・5 の不足から選ぶ。本人の回答の「次に足したいもの」と表面の軸の受入を、工程表と照合せずに次の作業にしない。SP-017のArch往復検証も自動で次の作業にしない。

1. 写真の縮尺の細部は `dotnet run --project tools/RegionMapPreview -c Release -- --seed 9 --development-details --development-size 8,1,0.5 --development-at "110.8:202.5" --out <出力>` で出す（`--development-size` は窓の一辺 km の並び、`--development-at` は追加で切り出す位置 km を `x:y;x:y`）。探索画面で見るには `./scripts/region-map.ps1 -Mode play -ModernDevelopment -Seed 9` で開き、街の上でホイールを回して ×128 以上へ寄る（`-Mode explore` に付けると4か所を撮る）。
   - 現代試作は `./scripts/region-map.ps1 -Mode setup -Seed 9 -ModernDevelopment` または RegionMapPreview の `--seed 9 --development-details` で広域・拡大を出す。Unity は `Vastcore > Region Map > Export Modern Development PNG...` でも広域を書き出せる。開発担当はこの試作を基準に次段の配置と見え方を比較する。
2. 本人（段5）：巨大構造物の形の方向を決める4枚の画像のための、画像生成の道具の用意（記名ノート `01M826EJVYAQ286JCV61M303BM`）。使えたら開発担当が4枚を生成し、方向を選ぶ問いを1つ送る。地形つきの画像と形への起こし方は、方向が決まった後。それまで構造物の形は作らない（プリミティブからの案は不採用。承認済みの計画は [SP-021 履歴](archive/REGION_MAP_EXPLORATION_HISTORY.md)）。
3. 本人の判断（方式の分岐）：地域の規模と解像度（256km・1024² のまま／広げる／細かくする。SP-021 9節）。回答が無い間は今の規模のまま Next Action 1 を進める。
   - 起動元（判断）：平野の筋を減らすしきい値 `RegionSpec.IncisionThreshold`（0／0.05／0.08／0.12、SP-021 8節・9節）。0.05 で seed 9 の都市が別の場所へ移り（Next Action 1 の `--development-at` も変わる）、0.08 以上では都市を置けない。決まるまで既定0。
4. 止めているもの（再開は本人の指示）：動き（軸 E）、音（軸 F）、サーチのゲーム性と遠征の規則（軸 H）、命名の響きと表示、段6。コード・確認ページ・回答の記録は残す。本人が挙げた直す点（波紋が距離に見える、矢印が答えの座標を指す、演出が機能に見える、発見物の分類を開発担当が決めた）は SP-021 4.1節と9節。命名の数えられる生成の欠陥は第3版で直した（2026-11-25、`1c69f16`。数は履歴）。
5. 進め方：問いは、答えで方式が分かれる時だけ送る（[INTERACTION_NOTES](INTERACTION_NOTES.md)、[INVARIANTS](INVARIANTS.md)）。本人が Unity Editor を開いている間は写し `D:\vcv\VastCore` で確かめて1回で戻し、写しの Unity が動いている間は写しの C# を編集しない。
6. 砂岩の3Dはアルコーブ・光と空気・表面の段3まで。晴れは Weather Maker、砂塵は自作。本人の選択は Linear・雲量上限0.8（形を優先）・夕方の雲は今の淡さ（2026-11-27、sky-trial m36）。空・雲・霞の作業は終了。終了後の追加は本人の選択「模様を2Dの風で流す」で、塊が2Dと同じ向き・速さで流れる（一周で戻さない。m43）。thank 端末にも Weather Maker を導入し、PC・Mobile の描画設定を維持した。探索から地上へ地点と雲の時刻を渡せる。個々の雲の輪郭の照合には差が残る。[通常の入口と確認結果](02_design/REGION_TERRAIN_LOOK_SPEC.md#scene-workflow)・[本人の選択](USER_REQUEST_LEDGER.md)。
   - 探索メニューからseed 8・9を開き、既存の砂・礫を比較用に適用したPC品質の動画とフレーム間隔を確認。[動画と計測](02_design/REGION_TERRAIN_LOOK_SPEC.md#scene-workflow)。表面の [T0〜T5](02_design/REGION_SURFACE_MATERIALS_SPEC.md) は済み（T0は本人の回答「B主・A従」、T3で土砂の場を標準に、T4で岩が地面へなじむ、T5で本人が選んだ7素材を置き岩屑の繰り返しを崩した）。残りは中景より先の崖と錐の面の形（扱いは本人）。GPU予算は本人の選択で1.2 ms（seed 9の1視点が約1.1 ms）。アーチ・峡谷は後続。
   - DS-011 Props は差し替え口・比較撮影・モデル 71 個の commit まで（取り込み経路は本人の選択で A＝FBX、[DS-011 7節](02_design/ENVIRONMENT_PROPS_CATALOG_SPEC.md)）。岩の色は本人の選択で地形の赤い砂岩へ寄せた（Props の岩は画面の r/g 1.16〜1.24→1.40〜1.46、地形の岩 1.64〜1.68。瓦礫の小さな欠片は淡いまま、8.1節）。次は P0 残件（17種）。
- 多バイオーム 3D 地形・草原 Props（2026-11-27）：探索地点の草原は自動植生と芝・ロームの写真材で描く。地表材は Editor / Player 共通の Resources 参照。草原固定配置44点は専用メニューと `-grasslandSample` で確認し、地面の三角形に合わせて接地する。[Seed 8 の前後比較](<D:/AI-Runs/VastCore/review-20261127-grassland-multibiome/index.html#quality-repair>)。4系統のカタログ登録・美術採否は本人の判断待ち。

## External Pulse And Remote Resume

[#48 Project Pulse](https://github.com/YuShimoji/VastCore/issues/48) は `.github/workflows/project-pulse.yml` による本ファイルの外部投影。`PROJECT_PULSE_BRANCH` はこのreadiness branch。Draft [PR #49](https://github.com/YuShimoji/VastCore/pull/49) は祖先 `f145df6` のworkflow変更。GitHubのdefault branchは歴史的な `master` のまま。

既存checkoutで再開するときは、readiness branchを明示する。

```powershell
git fetch origin --prune
git switch codex/vc-development-readiness-20260711
git pull --ff-only
.\scripts\check-project-state.ps1 -ExpectedBranch codex/vc-development-readiness-20260711
```

追跡branchが無い場合は、switchの代わりに `git switch --track origin/codex/vc-development-readiness-20260711` を使う。その後 `AGENTS.md`、`docs/REPO_LOCAL_RULES.md`、本ファイルを読む。

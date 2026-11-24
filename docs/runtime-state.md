# VastCore Runtime State

Last Updated: 2026-11-24

## Current Position

| Field | Value |
|---|---|
| Project | VastCore Terrain Engine |
| Branch | `codex/vc-development-readiness-20260711` |
| Integrated baseline | `2b398b0`（仮の音で鳴る場所、軸 F）。その前に名前の出し方の4点（`8644662`）、探索画面の動き（軸 E、`c12ba69`）、遠征の規則（軸 H、`e13924f`）、命名の第2版と名前・別名・由来の表示、拡大表示（`81e2b19`） |
| Active outcome | seedから地域一帯の鳥瞰図を自動で作り、その上で座標を指定してサーチし隠れた地点を発見する探索を動かす（SP-021 段1〜4）。本人の回答で段5（拡大した地形と巨大構造物）に着手し、方式「地図をそのまま細かくする」の地形を探索画面へ統合した（×4 以上で細部の地形に切り替わる。構造物の形は止めている） |
| Active artifact | `docs/02_design/REGION_MAP_EXPLORATION_SPEC.md` |
| Next mission | 2026-11-24 に本人が方向を訂正した（原文は [USER_REQUEST_LEDGER](USER_REQUEST_LEDGER.md)）。開発担当の次の作業は、地域データと鳥瞰図の詳細化（SP-021 段1・2 の不足）。段4の表面の軸（E 動き・F 音・H サーチのゲーム性・命名の響き）と段6（ゲームの中身）は止め、今のコードは変えずに残す。巨大構造物の生成（段5）は、形の方向を決める4枚の画像が未生成で、画像生成の道具を本人が用意するまで止まる（`01M821KYJMRRFKP1VY1F6Q1HS2`） |
| Current bottleneck | 核の2つ（詳細な鳥瞰図・オブジェクト生成）が進んでいない。鳥瞰図は、湖が無い・平野に薄い平行な筋が残る・1024² を超える出力が未確認・地域の規模と解像度が未決（SP-021 8節・9節）。巨大構造物は形を1つも作っていない（不採用の案だけ。画像の道具待ち）。2026-11-21〜24 の `Assets/Scripts/RegionMap/` 約13,500行のうち、探索画面・命名・音・動き・遠征の規則が約7,300行、構造物は165行（開発担当の観測） |
| Change relation | 訂正（2026-11-24、本人「今現在のこのプロジェクトで間違った方向に進みつつあるので訂正してほしいです。」。本人が選んだ訂正は作業対象と進め方の2つ）。同日の差し戻し3件・動きの回答・段6の「保留」と所見「色々ゲーム部分を勝手に決めすぎ」も同じ向き。2026-11-22 の依頼「複数のタスクの軸が考えられるため整理して、そのまま可能なら開発を継続」で開発担当が作った軸 A〜H の順（SP-021 4.1節）は、次の作業の順として使わない |

目的は [SSOT_WORLD](SSOT_WORLD.md) 1節と1.1節。目標アンカー：

- 開発目標：seedから地域一帯の詳細な鳥瞰図を自動で作り、その上にポイントクリック的な宝探しの探索ゲームを載せられる。
- 今回の増分：別の seed を開いても湖・平野・川が地形として破綻なく出て、1024² を超える鳥瞰図を書き出せる。前後は同じ seed 群の数値と絵で比べる（開発担当の整理。対象は Next Action 1）。
- 現在のボトルネック：上の表の Current bottleneck。

全体工程（段1〜6）と現在地の表は [SP-021](02_design/REGION_MAP_EXPLORATION_SPEC.md) 4節が持つ。SP-017 / SP-020（Arch・Prefab Stamp・DualGridの経路）は未実施のまま保留で、2026-11-21再開準備時の状態本文は [再開履歴](restart/VC_RESTART_20261101_HISTORY.md) の「2026-11-21 thank版（SP-017再開準備）」へ原文のまま移した。

## 作業ツリーと端末の現物（2026-11-22 thank観測）

- SP-021で追加したもの：`Assets/Scripts/RegionMap/`（Core・View・Editor、asmdef 2件）、`Assets/Scenes/RegionExplorer.unity`、`Assets/Tests/EditMode/RegionMapTests.cs`、`tools/RegionMapPreview/`、`scripts/region-map.ps1`。`1982fdd` の後に足したのは `Core/RegionDetail.cs`（拡大した地形）・`Core/RegionStructures.cs`（構造物の形の案）・`Core/RegionNames.cs`（呼び名の響き）の3件。変えたのは `Core/` の `RegionData`・`RegionGenerator`・`RegionSite`・`RegionAreas`・`RegionSpec`・`RegionMapRenderer`、テスト、`tools/RegionMapPreview/Program.cs`。
- 軸 G（2026-11-22）で `View/RegionMapSkin.cs` を足し、`View/RegionExplorer.cs`（画像の読み込みと4か所の描き方）、`Editor/RegionExplorerSmoke.cs`（差し替えの有無を1行ログに出す）、`Assets/Tests/EditMode/RegionMapTests.cs`（1件）を変えた。シーン・`ProjectSettings/`・`Packages/`・Terrain・StructureGenerator・Stamp は変更していない。画像を置く `Assets/Resources/RegionMapSkin/` は空のフォルダ（`.gitkeep` とフォルダの meta だけ）。
- 命名（2026-11-22 の最初の版と川岸、2026-11-23 の第2版）で足したのは `Core/RegionNaming.cs`・`Core/RegionNamingRules.cs`・`Core/RegionFeatures.cs` と `Assets/Tests/EditMode/RegionNamingTests.cs`。変えたのは `Core/` の `RegionNames`・`RegionSite`・`RegionAreas`・`RegionSpec`・`RegionData`、`View/RegionExplorer.cs`、`Editor/RegionExplorerSmoke.cs`、`tools/RegionMapPreview/Program.cs`。
- 拡大表示（2026-11-22〜23）で足したのは `View/RegionDetailLayer.cs`・`View/RegionRingGraphic.cs`・`Assets/Tests/EditMode/RegionDetailLayerTests.cs`（各 meta つき）。変えたのは `View/RegionExplorer.cs`（最大拡大 8→64、右上の札、サーチの輪、矢印キー1回分の移動量、標高の読み）と `Editor/RegionExplorerSmoke.cs`（拡大の確認の手順）。`Core/`・シーン・`ProjectSettings/`・`Packages/` は変更していない。
- 遠征の規則（軸 H、2026-11-23）で足したのは `Core/RegionExpedition.cs` と `Assets/Tests/EditMode/RegionExpeditionTests.cs`（各 meta つき）。変えたのは `Core/ExplorationSession.cs`（最寄りの地点 `Nearest`、`Reset()`）、`View/RegionExplorer.cs`（規則の切替、左の「遠征の残り」、右の「手掛かり」、地図の野営地・拠点の輪・線・方角の矢印、終わりの結果）、`Editor/RegionExplorerSmoke.cs`（遠征の撮影7枚）。
- 動き（軸 E、2026-11-23）で足したのは `View/RegionMotion.cs` と `Editor/RegionExplorerMotionCapture.cs`（各 meta つき）。変えたのは `View/RegionExplorer.cs`（部品ごとの動き、動きのスイッチ・長さ・C の3要素の入れ切り、右の手掛かりの下の地名を隠す、「。」などが行頭に1つだけ残る折り返しを防ぐ）と `Editor/RegionExplorerSmoke.cs`（動きが終わるまで待ってから撮る）。`Core/` は変えていない。
- 名前の出し方の4点（2026-11-23）で変えたのは `Core/RegionNaming.cs`・`Core/RegionNamingRules.cs`（地域ごとの経緯と音の質感、峰の規則）、`View/RegionExplorer.cs`（左上の地域の部品の「経緯・ことばの音」の行）、`Editor/RegionExplorerSmoke.cs`（別の地域2つの撮影）、`Assets/Tests/EditMode/RegionNamingTests.cs`（2件）。足したファイルは無い。
- 仮の音で鳴る場所（軸 F、2026-11-24）で足したのは `View/RegionSound.cs`・`View/RegionSoundPlayer.cs`・`Assets/Tests/EditMode/RegionSoundTests.cs`（各 meta つき）と、空のフォルダ `Assets/Resources/RegionMapSound/`（`.gitkeep` とフォルダの meta）。変えたのは `View/RegionExplorer.cs`（各場面から鳴らす、「音」ボタン、`CloseCard`・`OpenDiscovered`、AudioListener を付ける）と `Editor/RegionExplorerSmoke.cs`（仮の音の WAV の書き出し、音の手順と `sound-result.txt`）。`Core/`・シーン・`ProjectSettings/`・`Packages/` は変えていない。
- 未commitのまま保持するもの（前から同じ）：`.serena/project.yml`、`docs/04_reports/LEGACY_UI_MIGRATION_REPORT.md` の差分、untrackedの `Assets/Resources/Stamps/`（旧失敗候補）とそのmeta。どれも上書き・削除しない。
- 生成物は ignored の `artifacts/region-map/`（鳥瞰図PNG、探索画面の画像 `explorer-*.png`、下の確認ページ、ラフの元 `rough-20261122-screen/`）と `artifacts/logs/region-map-*.log`。thank端末にだけある。
- 確認ページ（`artifacts/region-map/` の下）：`review-20261124-sound/`（軸 F の鳴る場所と仮の音）・`review-20261123-naming-display/`（名前の出し方の4点）・`review-20261123-motion-impl/`（軸 E の動きの実画面）・`review-20261123-search-impl/`（軸 H の遠征の規則の実画面）・`review-20261123-search/`（軸 H のサーチの案）・`review-20261123-motion/`（軸 E の動きの案）・`review-20261123-naming/`（命名の第2版と探索画面の実画面）・`review-20261122-r10/`（拡大表示の実画面）・`review-20261123-r1/`（山頂のモノリスの形の方向4通りの計画と参照資料）・`review-20261122-r9/`（構造物の形を画像から起こす進め方）・`review-20261122-r4/`〜`r8/`（エリア・地点の根拠・雲の流れ・拡大した地形・形の案・呼び名・命名の仕組み）。
- Unityのバッチ起動は、Unity Hubが起動していないと `No valid Unity Editor license found`（exit 198）で失敗する。本人がこのプロジェクトを Editor で開いている間は、バッチも2つ目の Editor も起動できない（`another Unity instance is running`）。その間の確認用に、`D:\vcv\VastCore`（Assets・Packages・ProjectSettings・scripts の写しと、そこで取り込んだ Library。git 管理外）を残してある。要らなくなったらフォルダごと消してよい。
- 残しているローカル限定の履歴（`master`、`feature/mcp-verification`、stash 2件）とworktree `.codex-tmp/cloud-codex-20261011/VastCore` は再開準備時のまま。

## Current Trust Assessment

- PLANNER007 / Unity 6000.3.6f1 / 2026-11-24：取り込み版 `9f7a732` の正本チェックと Git LFS 検査が通り、71 パッケージを登録してコンパイル成功。EditMode 649/649（Region Map 関連46件）、PlayMode 9/9、失敗・スキップ0。ログと結果 XML は `artifacts/logs/resume-20261124-planner007/`。`RegionMapPreview` で seed 7・1024² の衛星写真風 PNG を `artifacts/region-map/region-seed7-satellite.png` に書き出した。
- thank / Unity 6000.3.6f1 / 2026-11-24（仮の音で鳴る場所、軸 F。写し `D:\vcv\VastCore` で実行し、元のリポジトリへ戻した10ファイルとハッシュ一致）：EditMode 649/649、失敗/skip 0（音のテスト8件）。探索シーンの再生（`-Mode explore`）：PASS。17場面のうち16場面で鳴らし、16場面とも鳴らした後のフレームで AudioSource が再生中で再生位置が進んだのを見た（耳で聞いた確認ではない。0.03〜0.09秒の短い音は次のフレームまでに鳴り終わる回がある）。「音」ボタンを「なし」にした間のサーチでは鳴らした数0。遠征の強い反応（西）は左 -0.60 に寄った。全部見つけた終わり（`ExpeditionComplete`）は操作の台本で起きない。仮の音17個を WAV に書き出し、Windows の読み込みで17個とも読めた。元のリポジトリでは確認用ツールのビルド（0エラー）だけ行った。

- 拡大表示（2026-11-22〜23。EditMode 627/627）、遠征の規則（2026-11-23。639/639、遠征の7場面）、動き（2026-11-23。639/639、3組の撮影）、命名の第2版（2026-11-23。631/631）、名前の出し方（2026-11-23。641/641）の Unity での確認、.NET での拡大した地形・エリアと雲の測定（2026-11-22）、通常Editorでの実マウス・キーボードの確認と十字カーソルの直し（2026-11-21〜22）は [履歴](02_design/REGION_MAP_EXPLORATION_HISTORY.md) へ原文のまま移した。
- 本人の回答（拡大表示、2026-11-23、`01M81W8NN4XG16SQXMV2R3NBKT`）：「近い」。6領域（広域から細部へ切り替わる見え方・右上の札・細部の地形・広域の地図との一致・拡大中のサーチと発見とカーソル・縮めて広域へ戻る見え方）は全て「軽微（余裕があれば）」。操作のしやすさは5項目とも7段階の4。前の版より「良くなった」。次は「今回の続き」。記述は無し。
- 本人の回答（命名の第2版、2026-11-23、`01M825M5QZP791BTF91XA1FDAS`）：「近い」。9領域（骨格と音の質感の分け方・複数の名前の持ち方・土地の経緯の決まり方・記号名の形・土地の名の作り・発見カードの別名と由来・一覧の由来・地図上の名前・音の響き）は全て「軽微（余裕があれば）」。次は「別の方向へ」、記述「サーチシステムなどのゲーム性を向上させたい。」
- 本人の回答（サーチのゲーム性、2026-11-23、`01M82CXPP4H2DKENWRZT0V0MGH`）：近い案は「組み合わせ」、記述「CとBがより近い。ゲームシステムとして。ナラティブについては要検討。」。足りないものは総当たり・見返り・目的と終わり・方角で、記述「モニュメント発見に寄っており、資材獲得や何らかの基地発見など、より広範で実利的な目的を持たせたい。」。「など」は地点の中身・探す目的と依頼・別の地域をまたぐ収集・拡大表示との連動で、ストアの記述は [USER_REQUEST_LEDGER](USER_REQUEST_LEDGER.md)。最初に作る大きさは「選んだ案の最小の実装まで」。
- 本人の回答（動きの案、2026-11-23、`01M828W77SDBJKNBXA22T8XJ8B`）：「B 地図で広がる」、記述「BとCを取り入れた動き方が良いです。」。4つの場面とも B。動きの長さは「長く（×1.6）」。進める範囲は「選んだ案の最小の実装まで」。削る動きは未回答。
- 本人の回答（名前の出し方、2026-11-23、`01M825M7PECFN9BY6CFVJREM58`）：既定の音の質感は「地域（seed）ごとに質感を自動で選ぶ」。名前が分かる時は「最初から全部出す（今回の版）」。山の名前は「文明のあるエリアから近い高い峰には、そのことばで土地の名も付ける」。土地の経緯は「地域ごとに 1 つ」。
- 本人の回答（2026-11-24）：動きの受入（`01M831H77K6HV96N1BYM6J0JZK`）は「一部直したい」。段6の残り3設問（`01M84QPNKRE5XXVTF20ZHEN8RF`）は「保留」。遠征の規則の受入（`01M82V588FZE0C05C0YP34DNGN`）は差し戻し。原文は [USER_REQUEST_LEDGER](USER_REQUEST_LEDGER.md)。
- 本人の回答（2026-11-22）：エリア・雲の流れ・地点の根拠を足した版（`01M80215JPBGRPF79KE7J0YQQF`）は「近い」、「エリアと地点の呼び名」だけ「直したい（次の前に）」、他は「軽微」、次は「ここで十分」。進め方（`01M80219XNGW9T27XQMXDZJQFG`）は SP-021 4.1節。拡大した地形（`01M81JA2CC4WC37TF841JJ8ZEE`）は「近い」・全項目「軽微」・次は「今回の続き」。呼び名と形（`01M81JA6HWR4TJQXZA6T07TC6J`）は SP-021 4.1節。命名の仕組みの方向（`01M81SA3GF8YMRZRETVZCHBGE2`、2026-11-23）は SP-021 5節。
- 2026-11-01の `af36a0e`（Arch・Export・配置の修正）に対するcompile・EditMode 603/603・PlayMode 9/9は当時の結果で、そのコードは今回変更していない。修正版の通常GUI往復（SP-017）は未実施のまま。
- 未確認（命名）：元のリポジトリを開いた本人の Unity での再生、実マウスで発見した時のカードの出方、発見数が増えて一覧の由来の行が名前だけの行へ戻る切替、seed 7 以外の探索画面での名前の見え方（名前の一覧は seed 3・21 も見た）、16:9 以外での地名の重なり、規則を人が書き換える時の使い勝手、確認ページ `review-20261123-naming` を本人のブラウザで開いた時の見え方、川岸へ移した地点の seed 7 以外での見え方（テストは seed 5〜9 の位置だけ確かめる）。
- 未確認：差し替えの画像を本人が置いた時の見え方（仮の画像1組でだけ確認。PNG以外の形式、正方形でない画像、取り込み設定が Sprite の画像、1920×1080 以外の画面は未確認）、確認ページ `review-20261122-r7` を本人のブラウザで開いた時の表示（Recordinator の「開く」は応答200。添付18ファイルのハッシュは元と一致）、seed 7 以外の地域と地点 5・10・14 以外での拡大した地形、`9cc6770` 以降の画面での実マウスの操作（右・中ドラッグでの移動、ホイールの拡大・縮小、一覧の行とボタンのクリック、「地名」ボタン、部品の上のクリックが地図へ通らないこと）、雲の層を3枚に増やした後の実際の描画の速さ、拡大した時の地名の重なり、16:9 以外の画面の形での部品の重なり、元のリポジトリの Editor での探索シーン再生（写しでだけ確認）、ビルドした Player、Windows以外でのフォント表示、1024²を超える解像度、残る見た目の制限（SP-021 8節）の解消。
- 未確認（拡大表示）：実マウスでのホイール・右ドラッグの手触りと、細部を作っている最中の滑らかさ（更新の間隔の測定だけ）、元のリポジトリの Editor での再生（写しでだけ確認）、seed 7 以外と 1920×1080 以外の画面の形、ビルドした Player での作成時間、拡大 ×40 以上でのサーチの輪（画面より大きく見えない）、確認ページ `review-20261122-r10` を本人のブラウザで開いた時の画像の出方。
- 未確認（動き）：実マウスでの操作感、1920×1080 以外、×4 以上の拡大中、ビルドした Player、画像の差し替え（軸 G）と組み合わせた見え方、元のリポジトリを開いた本人の Editor での再生（写しでだけ確認）。
- 未確認（名前の出し方）：元のリポジトリを開いた本人の Editor での再生（写しでだけ確認）、seed 7〜9 以外の探索画面での見え方（seed 1〜12 は名前の表だけ見た）、実マウスで「別の地域」を押した時の見え方、規則を「混在」に書き換えた時の探索画面（Unity には規則を読む入口が無く、書き出しでだけ確認）、確認ページ `review-20261123-naming-display` を本人のブラウザで開いた時の見え方。
- 未確認（音）：耳で聞いた大きさと聞き分けやすさ（Unity の再生状態と WAV の形だけ見た）、元のリポジトリを開いた本人の Editor での再生（写しでだけ確認）、ビルドした Player、実マウス・実キー（Esc と一覧の行は同じ入口をコードから呼んだ）、差し替えの音を置いた時の鳴り方、全部見つけた遠征の終わりの音（Unity の再生の台本で起きない。合成と表はテストで確認）、16:9 以外での下端のボタン列の重なり、確認ページ `review-20261124-sound` を本人のブラウザで開いた時の再生。

## Next Action

次の作業は SP-021 4節の工程表の段1・2・5 の不足から選ぶ。本人の回答の「次に足したいもの」と表面の軸の受入を、工程表と照合せずに次の作業にしない。SP-017のArch往復検証も自動で次の作業にしない。

1. 開発担当（段1・2）：鳥瞰図の詳細化。対象は SP-021 8節の、湖（窪地を毎回埋めている）、平野の薄い平行な筋、1024² を超える出力、侵食の時間。直す前に seed 1〜12 で現状を数え（湖になるはずの窪地、筋の出る面積、解像度ごとの生成時間）、直した後に同じ数え方と同じ seed の絵で比べる。数えて分かる欠陥は直してから見せ、好みの設問にしない。使うのは `Core/` と `tools/RegionMapPreview/`。`View/RegionExplorer.cs` は変えない。
2. 本人（段5）：巨大構造物の形の方向を決める4枚の画像のための、画像生成の道具の用意（記名ノート `01M826EJVYAQ286JCV61M303BM`）。使えたら開発担当が4枚を生成し、方向を選ぶ問いを1つ送る。地形つきの画像と形への起こし方は、方向が決まった後。それまで構造物の形は作らない（プリミティブからの案は不採用。承認済みの計画は [SP-021 履歴](02_design/REGION_MAP_EXPLORATION_HISTORY.md)）。
3. 本人の判断（方式の分岐）：地域の規模と解像度（256km・1024² のまま／広げる／細かくする。SP-021 9節）。回答が無い間は今の規模のまま Next Action 1 を進める。
4. 止めているもの（再開は本人の指示）：動き（軸 E）、音（軸 F）、サーチのゲーム性と遠征の規則（軸 H）、命名の響きと表示、段6。コード・確認ページ・回答の記録は残す。本人が挙げた直す点（波紋が距離に見える、矢印が答えの座標を指す、演出が機能に見える、発見物の分類を開発担当が決めた）は SP-021 4.1節と9節。命名の生成の欠陥（seed 1〜12 で数えた。[履歴](02_design/REGION_MAP_EXPLORATION_HISTORY.md)）は、段1・2 の後に開発担当が数え方を変えずに直す（順は開発担当の整理）。
5. 進め方：問いは、答えで方式が分かれる時だけ送る（[INTERACTION_NOTES](INTERACTION_NOTES.md)、[INVARIANTS](INVARIANTS.md)）。本人が Unity Editor を開いている間は写し `D:\vcv\VastCore` で確かめて1回で戻し、写しの Unity が動いている間は写しの C# を編集しない。

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

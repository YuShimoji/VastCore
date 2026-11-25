# VastCore Runtime State

Last Updated: 2026-11-25

## Current Position

| Field | Value |
|---|---|
| Project | VastCore Terrain Engine |
| Branch | `codex/vc-development-readiness-20260711` |
| Integrated baseline | `63e0fc9`（探索画面の拡大）。その前は `b8795a6`（写真の縮尺の細部）、`439a92b`（現代試作）。それ以前は `2b398b0`（仮の音で鳴る場所、軸 F）。その前に名前の出し方の4点（`8644662`）、探索画面の動き（軸 E、`c12ba69`）、遠征の規則（軸 H、`e13924f`）、命名の第2版と名前・別名・由来の表示、拡大表示（`81e2b19`） |
| Active outcome | SP-021 の現代試作。2都市・都市間幹線・空港・郊外の建物を地域データから配置し、広域と拡大の同じ座標で描く。探索画面は ×4 以上で細部へ切り替わる。1 画素 3 m 未満の窓では、街区の中を敷地・屋根・屋上の設備・樹木・車・車線・影まで描く（写真の縮尺の細部）。探索画面は ×512 まで拡大でき、×128 以上でこの細部へ切り替わる。 |
| Active artifact | `docs/02_design/REGION_MAP_EXPLORATION_SPEC.md` |
| Next mission | 探索画面で寄って見られるようにした写真の縮尺の細部（2026-11-24 の[建物の見え方の依頼](USER_REQUEST_LEDGER.md)）に、本人の確認を受ける。開発は工場・基地と中世・未来の都市への展開（順は開発担当の整理）。 |
| Current bottleneck | 写真の縮尺の細部は、探索画面で寄ってから絵が出るまで 10〜36 秒かかり、窓は画面より粗く輪郭がやわらかい（内訳と条件は SP-021 8節）。街区は 570 m の格子、建物の枠は1街区に最大9個のままで、細部はその枠の中を展開している（曲がった道・不整形の敷地は無い）。空港・郊外の建物は従来の面のまま。他時代・施設への展開が残る。地域の規模と採用解像度は未決（SP-021 8節・9節）。巨大構造物は形の画像の道具待ち（`01M821KYJMRRFKP1VY1F6Q1HS2`）。 |
| Change relation | 訂正（2026-11-24、本人「今現在のこのプロジェクトで間違った方向に進みつつあるので訂正してほしいです。」。本人が選んだ訂正は作業対象と進め方の2つ）。同日の差し戻し3件・動きの回答・段6の「保留」と所見「色々ゲーム部分を勝手に決めすぎ」も同じ向き。2026-11-22 の依頼「複数のタスクの軸が考えられるため整理して、そのまま可能なら開発を継続」で開発担当が作った軸 A〜H の順（SP-021 4.1節）は、次の作業の順として使わない |

目的は [SSOT_WORLD](SSOT_WORLD.md) 1節と1.1節。目標アンカー：

- 開発目標：seedから地域一帯の詳細な鳥瞰図を自動で作り、その上にポイントクリック的な宝探しの探索ゲームを載せられる。
- 今回の増分：現代の土地利用を地形に重ね、鳥瞰図で2都市と幹線と空港を見分け、拡大して街区・道路・施設の接続を確かめられる。さらに寄ると、建物・屋根・樹木・車が航空写真の縮尺で見える（探索画面では ×128 以上）。
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
- 命名の第3版（2026-11-25）で変えたのは `Core/RegionNaming.cs`・`Core/RegionNamingRules.cs`（規則1〜5）と `Assets/Tests/EditMode/RegionNamingTests.cs`。確認ページは `review-20261125-naming-system/`。
- 仮の音で鳴る場所（軸 F、2026-11-24、`2b398b0`）で足したのは `View/RegionSound.cs`・`View/RegionSoundPlayer.cs`・`Assets/Tests/EditMode/RegionSoundTests.cs`（各 meta つき）と空のフォルダ `Assets/Resources/RegionMapSound/`。変えたのは `View/RegionExplorer.cs`（各場面から鳴らす、「音」ボタン、`CloseCard`・`OpenDiscovered`、AudioListener）と `Editor/RegionExplorerSmoke.cs`（仮の音の WAV の書き出し、`sound-result.txt`）。2026-11-25 に既定を「なし」にした（`8dc7729`。`RegionExplorer.cs`・`Editor/RegionExplorerSmoke.cs`・`RegionSoundTests.cs`）。`Core/`・シーン・`ProjectSettings/`・`Packages/` は変えていない。
- 未commitのまま保持するもの（前から同じ）：`.serena/project.yml`、`docs/04_reports/LEGACY_UI_MIGRATION_REPORT.md` の差分、untrackedの `Assets/Resources/Stamps/`（旧失敗候補）とそのmeta。どれも上書き・削除しない。
- 生成物は ignored の `artifacts/region-map/`（鳥瞰図PNG、探索画面の画像 `explorer-*.png`、下の確認ページ、ラフの元 `rough-20261122-screen/`）と `artifacts/logs/region-map-*.log`。thank端末にだけある。
- 確認ページ（`artifacts/region-map/` の下）：`review-20261124-sound/`（軸 F の鳴る場所と仮の音）・`review-20261123-naming-display/`（名前の出し方の4点）・`review-20261123-motion-impl/`（軸 E の動きの実画面）・`review-20261123-search-impl/`（軸 H の遠征の規則の実画面）・`review-20261123-search/`（軸 H のサーチの案）・`review-20261123-motion/`（軸 E の動きの案）・`review-20261123-naming/`（命名の第2版と探索画面の実画面）・`review-20261122-r10/`（拡大表示の実画面）・`review-20261123-r1/`（山頂のモノリスの形の方向4通りの計画と参照資料）・`review-20261122-r9/`（構造物の形を画像から起こす進め方）・`review-20261122-r4/`〜`r8/`（エリア・地点の根拠・雲の流れ・拡大した地形・形の案・呼び名・命名の仕組み）。
- Unityのバッチ起動は、Unity Hubが起動していないと `No valid Unity Editor license found`（exit 198）で失敗する。本人がこのプロジェクトを Editor で開いている間は、バッチも2つ目の Editor も起動できない（`another Unity instance is running`）。その間の確認用に、`D:\vcv\VastCore`（Assets・Packages・ProjectSettings・scripts の写しと、そこで取り込んだ Library。git 管理外）を残してある。要らなくなったらフォルダごと消してよい。
- 残しているローカル限定の履歴（`master`、`feature/mcp-verification`、stash 2件）とworktree `.codex-tmp/cloud-codex-20261011/VastCore` は再開準備時のまま。

## Current Trust Assessment

- thank / Unity 6000.3.6f1 / 2026-11-25（命名の第3版。写し `D:\vcv\vcn`）：EditMode 654/654（失敗・skip 0）、探索の再生 終了コード 0・画面 30 枚（seed 7・8・9 の地図の名前と発見カードの別名・由来を目視）。.NET の seed 1〜12（探索画面の設定）で、規則1 99→0、規則2 87→0（古い呼び名 103→0）、規則4 79→0（1字の足し・抜き 12→0）、規則5 0→0、方角入り 57→9（9つとも理由あり）。数え方は [履歴](02_design/REGION_MAP_EXPLORATION_HISTORY.md) の「2026-11-25 命名の第3版」。
- thank / 2026-11-25（探索画面の拡大）：探索シーンで seed 9 の4か所を ×64・×128・×256・×512 で撮った（`artifacts/region-map/explorer-photo-20261125/unity-final/`）。×128 以上で敷地・屋根・車・並木の影が出る。描画を速くした後も絵は変更前とバイト一致（14枚）。Unity 6000.3.6f1 で EditMode 653/653（失敗・スキップ0、窓の段のテスト1件を追加）。撮影4回のうち2回は16場面すべて出た。1回は ×512 の窓が240秒出ず（原因は未特定）、1回は Unity が起動時に終了した（Package Manager へ未接続）。開発担当が見て、雲が画面を覆う点を直した（×64〜×128 で消す）。本人の確認は未。
- 現代試作（PLANNER007、256 km・1024²）。.NET の seed 7・8・9 で2都市・幹線・空港・郊外を生成。seed 9 の再生成は PNG の SHA-256 が一致。Unity 6000.3.6f1 で seed 9 の広域と4箇所の8 km拡大図を出力し、EditMode 651/651、PlayMode 9/9。同じ seed でも Unity と .NET で道路・空港位置に差が出た。画像は `artifacts/region-map/sp021-modern-20261124/`。
- SP-021 の地形の修正前・修正後の計測（PLANNER007、seed 1〜12、2行）と、PLANNER007 での取り込み版 `9f7a732` の確認（1行）は [履歴](02_design/REGION_MAP_EXPLORATION_HISTORY.md) の「2026-11-24 写真の縮尺の細部」へ、2026-11-24 の確認2行は「2026-11-25 探索画面の拡大」へ原文のまま移した。
- 3D化の試作（PLANNER007、2026-11-25。範囲の外）：仕組み（地層の段・通行の区分と道筋・局所の体積・Unity での表示。EditMode 661/661）は作ったが、本人は Unity の絵を「のっぺりしており初期状態のTerrainという感じなので直したいです。」と評価した。開発担当は見え方の想定を作らずに実装し、副産物の画面に可否を求めていた。数値と確認の記録は履歴の「2026-11-25 3D化の見え方の訂正（正本から移した行と出典）」。

- 拡大表示（2026-11-22〜23。EditMode 627/627）、遠征の規則（2026-11-23。639/639、遠征の7場面）、動き（2026-11-23。639/639、3組の撮影）、命名の第2版（2026-11-23。631/631）、名前の出し方（2026-11-23。641/641）の Unity での確認、.NET での拡大した地形・エリアと雲の測定（2026-11-22）、通常Editorでの実マウス・キーボードの確認と十字カーソルの直し（2026-11-21〜22）は [履歴](02_design/REGION_MAP_EXPLORATION_HISTORY.md) へ原文のまま移した。音の既定を「なし」にした確認（2026-11-25、654/654）は同じ履歴の「2026-11-25 3D化の試作・段階 B（正本から移した行）」へ移した。
- 本人の回答（拡大表示、2026-11-23、`01M81W8NN4XG16SQXMV2R3NBKT`）：「近い」。6領域（広域から細部へ切り替わる見え方・右上の札・細部の地形・広域の地図との一致・拡大中のサーチと発見とカーソル・縮めて広域へ戻る見え方）は全て「軽微（余裕があれば）」。操作のしやすさは5項目とも7段階の4。前の版より「良くなった」。次は「今回の続き」。記述は無し。
- 本人の回答（サーチのゲーム性、2026-11-23、`01M82CXPP4H2DKENWRZT0V0MGH`）：近い案は「組み合わせ」、記述「CとBがより近い。ゲームシステムとして。ナラティブについては要検討。」。足りないものは総当たり・見返り・目的と終わり・方角で、記述「モニュメント発見に寄っており、資材獲得や何らかの基地発見など、より広範で実利的な目的を持たせたい。」。「など」は地点の中身・探す目的と依頼・別の地域をまたぐ収集・拡大表示との連動で、ストアの記述は [USER_REQUEST_LEDGER](USER_REQUEST_LEDGER.md)。最初に作る大きさは「選んだ案の最小の実装まで」。
- 本人の回答（動きの案、2026-11-23、`01M828W77SDBJKNBXA22T8XJ8B`）：「B 地図で広がる」、記述「BとCを取り入れた動き方が良いです。」。4つの場面とも B。動きの長さは「長く（×1.6）」。進める範囲は「選んだ案の最小の実装まで」。削る動きは未回答。
- 本人の回答（2026-11-24）：動きの受入（`01M831H77K6HV96N1BYM6J0JZK`）は「一部直したい」。段6の残り3設問（`01M84QPNKRE5XXVTF20ZHEN8RF`）は「保留」。遠征の規則の受入（`01M82V588FZE0C05C0YP34DNGN`）は差し戻し。原文は [USER_REQUEST_LEDGER](USER_REQUEST_LEDGER.md)。
- 本人の回答（2026-11-22）：エリア・雲の流れ・地点の根拠を足した版（`01M80215JPBGRPF79KE7J0YQQF`）は「近い」、「エリアと地点の呼び名」だけ「直したい（次の前に）」、他は「軽微」、次は「ここで十分」。進め方（`01M80219XNGW9T27XQMXDZJQFG`）は SP-021 4.1節。拡大した地形（`01M81JA2CC4WC37TF841JJ8ZEE`）は「近い」・全項目「軽微」・次は「今回の続き」。呼び名と形（`01M81JA6HWR4TJQXZA6T07TC6J`）は SP-021 4.1節。命名の仕組みの方向（`01M81SA3GF8YMRZRETVZCHBGE2`、2026-11-23）は SP-021 5節。
- 2026-11-01の `af36a0e`（Arch・Export・配置の修正）に対するcompile・EditMode 603/603・PlayMode 9/9は当時の結果で、そのコードは今回変更していない。修正版の通常GUI往復（SP-017）は未実施のまま。
- 命名の第2版・名前の出し方の本人の回答と未確認（4行）は [履歴](02_design/REGION_MAP_EXPLORATION_HISTORY.md) の「2026-11-25 命名の第3版」へ原文のまま移した（未確認は今も効く）。
- 未確認：差し替えの画像を本人が置いた時の見え方（仮の画像1組でだけ確認。PNG以外の形式、正方形でない画像、取り込み設定が Sprite の画像、1920×1080 以外の画面は未確認）、確認ページ `review-20261122-r7` を本人のブラウザで開いた時の表示（Recordinator の「開く」は応答200。添付18ファイルのハッシュは元と一致）、seed 7 以外の地域と地点 5・10・14 以外での拡大した地形、`9cc6770` 以降の画面での実マウスの操作（右・中ドラッグでの移動、ホイールの拡大・縮小、一覧の行とボタンのクリック、「地名」ボタン、部品の上のクリックが地図へ通らないこと）、雲の層を3枚に増やした後の実際の描画の速さ、拡大した時の地名の重なり、16:9 以外の画面の形での部品の重なり、元のリポジトリの Editor での探索シーン再生（写しでだけ確認）、ビルドした Player、Windows以外でのフォント表示、残る見た目の制限（SP-021 8節）の解消。
- 未確認（拡大表示）：実マウスでのホイール・右ドラッグの手触りと、細部を作っている最中の滑らかさ（更新の間隔の測定だけ）、元のリポジトリの Editor での再生（写しでだけ確認）、seed 7 以外と 1920×1080 以外の画面の形、ビルドした Player での作成時間、拡大 ×40 以上でのサーチの輪（画面より大きく見えない）、確認ページ `review-20261122-r10` を本人のブラウザで開いた時の画像の出方。
- 未確認（動き）：実マウスでの操作感、1920×1080 以外、×4 以上の拡大中、ビルドした Player、画像の差し替え（軸 G）と組み合わせた見え方、元のリポジトリを開いた本人の Editor での再生（写しでだけ確認）。
- 未確認（音）：耳で聞いた大きさと聞き分けやすさ（Unity の再生状態と WAV の形だけ見た）、元のリポジトリを開いた本人の Editor での再生（写しでだけ確認）、ビルドした Player、実マウス・実キー（Esc と一覧の行は同じ入口をコードから呼んだ）、差し替えの音を置いた時の鳴り方、全部見つけた遠征の終わりの音（Unity の再生の台本で起きない。合成と表はテストで確認）、16:9 以外での下端のボタン列の重なり、確認ページ `review-20261124-sound` を本人のブラウザで開いた時の再生。

## Next Action

次の作業は SP-021 4節の工程表の段1・2・5 の不足から選ぶ。本人の回答の「次に足したいもの」と表面の軸の受入を、工程表と照合せずに次の作業にしない。SP-017のArch往復検証も自動で次の作業にしない。

1. 写真の縮尺の細部は `dotnet run --project tools/RegionMapPreview -c Release -- --seed 9 --development-details --development-size 8,1,0.5 --development-at "110.8:202.5" --out <出力>` で出す（`--development-size` は窓の一辺 km の並び、`--development-at` は追加で切り出す位置 km を `x:y;x:y`）。探索画面で見るには `./scripts/region-map.ps1 -Mode play -ModernDevelopment -Seed 9` で開き、街の上でホイールを回して ×128 以上へ寄る（`-Mode explore` に付けると4か所を撮る）。
   - 現代試作は `./scripts/region-map.ps1 -Mode setup -Seed 9 -ModernDevelopment` または RegionMapPreview の `--seed 9 --development-details` で広域・拡大を出す。Unity は `Vastcore > Region Map > Export Modern Development PNG...` でも広域を書き出せる。開発担当はこの試作を基準に次段の配置と見え方を比較する。
2. 本人（段5）：巨大構造物の形の方向を決める4枚の画像のための、画像生成の道具の用意（記名ノート `01M826EJVYAQ286JCV61M303BM`）。使えたら開発担当が4枚を生成し、方向を選ぶ問いを1つ送る。地形つきの画像と形への起こし方は、方向が決まった後。それまで構造物の形は作らない（プリミティブからの案は不採用。承認済みの計画は [SP-021 履歴](02_design/REGION_MAP_EXPLORATION_HISTORY.md)）。
3. 本人の判断（方式の分岐）：地域の規模と解像度（256km・1024² のまま／広げる／細かくする。SP-021 9節）。回答が無い間は今の規模のまま Next Action 1 を進める。
4. 止めているもの（再開は本人の指示）：動き（軸 E）、音（軸 F）、サーチのゲーム性と遠征の規則（軸 H）、命名の響きと表示、段6。コード・確認ページ・回答の記録は残す。本人が挙げた直す点（波紋が距離に見える、矢印が答えの座標を指す、演出が機能に見える、発見物の分類を開発担当が決めた）は SP-021 4.1節と9節。命名の数えられる生成の欠陥は第3版で直した（2026-11-25、`1c69f16`。数は履歴）。
5. 進め方：問いは、答えで方式が分かれる時だけ送る（[INTERACTION_NOTES](INTERACTION_NOTES.md)、[INVARIANTS](INVARIANTS.md)）。本人が Unity Editor を開いている間は写し `D:\vcv\VastCore` で確かめて1回で戻し、写しの Unity が動いている間は写しの C# を編集しない。
6. 3D化（範囲の外）：実装を止め、見え方の想定を先に作る。調査（履歴の同じ節の出典）から方向案を出し、本人が最初の方向を選ぶ。その後に、遠景・中景・近景で見えるべき条件と参照写真を書き、今の絵との差を並べてから実装する。段階ごとに可否を聞かない。

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

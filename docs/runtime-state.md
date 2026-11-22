# VastCore Runtime State

Last Updated: 2026-11-22

## Current Position

| Field | Value |
|---|---|
| Project | VastCore Terrain Engine |
| Branch | `codex/vc-development-readiness-20260711` |
| Integrated baseline | `33c0d24`（拡大した地形・形の案・呼び名の響きの案と、軸 G の差し替えの入口）の上に、命名の仕組みの最初の版と、川の上の地点を川岸へ移す処理を足した（2026-11-22、本ファイルと同じ commit） |
| Active outcome | seedから地域一帯の鳥瞰図を自動で作り、その上で座標を指定してサーチし隠れた地点を発見する探索を動かす（SP-021 段1〜4）。本人の回答で段5（拡大した地形と巨大構造物）に着手した。方式は「地図をそのまま細かくする」 |
| Active artifact | `docs/02_design/REGION_MAP_EXPLORATION_SPEC.md` |
| Next mission | 本人が、命名の仕組みの方向の問い（`01M81SA3GF8YMRZRETVZCHBGE2`）に答える。開発担当は、回答を待たずに進められる拡大した地形の探索画面への統合を進め、回答が来たら命名の仕組みを作り込んで呼び名を探索画面へ出す |
| Current bottleneck | 命名は仕組みの最初の版ができたが、土台の方向（意味を持つ語根で良いか、ことばの範囲、音と語根を誰が書くか）が決まっていない。構造物の形は「画像生成から逆算したい」となり、画像の枚数・形式が決まるまで形を作れない。どちらも決めるのは本人。拡大した地形は本人が「近い」と答えたが、探索画面で拡大しても細かい絵へ切り替わらない（統合は未着手） |
| Change relation | 継続（2026-11-22、本人依頼「複数のタスクの軸が考えられるため整理して、そのまま可能なら開発を継続」「回答しました。命名のシステムについてはより体系的に構築したいです。」と、回答 `01M81JA2CC4WC37TF841JJ8ZEE`・`01M81JA6HWR4TJQXZA6T07TC6J`）。順は SP-021 4.1節。軸 G の入口の後に、命名の仕組みの最初の版と川岸への移動を作った |

目的は [SSOT_WORLD](SSOT_WORLD.md) 1節と1.1節。目標アンカー：

- 開発目標：seedから地域一帯の詳細な鳥瞰図を自動で作り、その上にポイントクリック的な宝探しの探索ゲームを載せられる。
- 今回の増分：本人が、名前がどう組み立てられ、どこを書き換えられるかを確認ページで見て、命名の仕組みの土台がこの方向で良いか、何が足りないかを言える。
- 現在のボトルネック：上の表の Current bottleneck。

全体工程（段1〜6）と現在地の表は [SP-021](02_design/REGION_MAP_EXPLORATION_SPEC.md) 4節が持つ。SP-017 / SP-020（Arch・Prefab Stamp・DualGridの経路）は未実施のまま保留で、2026-11-21再開準備時の状態本文は [再開履歴](restart/VC_RESTART_20261101_HISTORY.md) の「2026-11-21 thank版（SP-017再開準備）」へ原文のまま移した。

## 作業ツリーと端末の現物（2026-11-22 thank観測）

- SP-021で追加したもの：`Assets/Scripts/RegionMap/`（Core・View・Editor、asmdef 2件）、`Assets/Scenes/RegionExplorer.unity`、`Assets/Tests/EditMode/RegionMapTests.cs`、`tools/RegionMapPreview/`、`scripts/region-map.ps1`。`1982fdd` の後に足したのは `Core/RegionDetail.cs`（拡大した地形）・`Core/RegionStructures.cs`（構造物の形の案）・`Core/RegionNames.cs`（呼び名の響き）の3件。変えたのは `Core/` の `RegionData`・`RegionGenerator`・`RegionSite`・`RegionAreas`・`RegionSpec`・`RegionMapRenderer`、テスト、`tools/RegionMapPreview/Program.cs`。
- 軸 G（2026-11-22）で `View/RegionMapSkin.cs` を足し、`View/RegionExplorer.cs`（画像の読み込みと4か所の描き方）、`Editor/RegionExplorerSmoke.cs`（差し替えの有無を1行ログに出す）、`Assets/Tests/EditMode/RegionMapTests.cs`（1件）を変えた。シーン・`ProjectSettings/`・`Packages/`・Terrain・StructureGenerator・Stamp は変更していない。画像を置く `Assets/Resources/RegionMapSkin/` は空のフォルダ（`.gitkeep` とフォルダの meta だけ）。
- 命名の仕組みと川岸（2026-11-22）で足したのは `Core/RegionNaming.cs` と `Assets/Tests/EditMode/RegionNamingTests.cs`。変えたのは `Core/` の `RegionNames`・`RegionSite`・`RegionAreas`・`RegionSpec` と `tools/RegionMapPreview/Program.cs`。`View/` と `Editor/` は変えていない。
- 未commitのまま保持するもの（前から同じ）：`.serena/project.yml`、`docs/04_reports/LEGACY_UI_MIGRATION_REPORT.md` の差分、untrackedの `Assets/Resources/Stamps/`（旧失敗候補）とそのmeta。どれも上書き・削除しない。
- 生成物は ignored の `artifacts/region-map/`（鳥瞰図PNG、探索画面の画像 `explorer-*.png`、確認ページ `review-20261122-r8/`（命名の仕組み・川岸へ移した地点）・`review-20261122-r7/`（拡大した地形・形の案・呼び名の響き）・`review-20261122-r6/`（エリア・地点の根拠・雲の流れ）・`review-20261122-r5/`・`review-20261122-r4/`、ラフの元 `rough-20261122-screen/`）と `artifacts/logs/region-map-*.log`。thank端末にだけある。
- Unityのバッチ起動は、Unity Hubが起動していないと `No valid Unity Editor license found`（exit 198）で失敗する。本人がこのプロジェクトを Editor で開いている間は、バッチも2つ目の Editor も起動できない（`another Unity instance is running`）。その間の確認用に、`D:\vcv\VastCore`（Assets・Packages・ProjectSettings・scripts の写しと、そこで取り込んだ Library。git 管理外）を残してある。要らなくなったらフォルダごと消してよい。
- 残しているローカル限定の履歴（`master`、`feature/mcp-verification`、stash 2件）とworktree `.codex-tmp/cloud-codex-20261011/VastCore` は再開準備時のまま。

## Current Trust Assessment

- thank / Unity 6000.3.6f1 / 2026-11-22（命名の仕組みと川岸。写し `D:\vcv\VastCore` へ元のリポジトリの RegionMap まわりを揃えて実行）：EditMode が622/622、失敗/skip 0（命名と川岸のテスト5件を足した。同じ5件は .NET 9 でも通った）。確認用ツールで seed 7・3・21 の名前と由来を書き出し、開発担当が一覧を見て、1字違いの名前と、多くのエリアに当てはまる事実ばかりが選ばれる点を直した。川岸は seed 7 の地点 10・18 の 2km四方の絵で、構造物の本体が川に重ならないことを見た（地点 18 は影が川にかかる）。探索シーンの再生は今回実行していない。元のリポジトリの Editor では未実行。
- thank / Unity 6000.3.6f1 / 2026-11-22（軸 G、差し替えの入口。写し `D:cvVastCore` で実行し、元のリポジトリへ戻した後にハッシュ一致を確認）：`run-tests.ps1 -TestMode editmode` が617/617、失敗/skip 0（入口のテスト1件を足した）。探索シーンの再生（`-Mode explore`）は、画像を置かない版と、仮の画像1組（4部品。確認用で、デザイン案ではなく、リポジトリへ入れていない）を置いた版の両方が PASS（1920×1080、発見1/18、地図が出るまで 10.1秒・9.2秒）。2つの画面の発見直後（`explorer-3-found`）とサーチ直後（`explorer-2-signal`）を同じ位置で拡大して見比べ、差し替えた絵の中心がカーソル・地点とずれないこと、サーチの輪・発見の輪・点の大きさが既定と同じこと、十字の腕が発見の輪（半径27）の外にあって重ならないこと、上下の向きが保たれることを確かめた。画像を置かない時の画面は、入口を足す前と同じ（既定の絵）。元のリポジトリの Editor では未実行。
- thank / Unity 6000.3.6f1 / 2026-11-22（拡大した地形・形の案・呼び名を足した版を写し `D:\vcv\VastCore` で実行。元のリポジトリへ戻した14ファイルとハッシュ一致）：`run-tests.ps1 -TestMode editmode` が616/616、失敗/skip 0（呼び名と拡大した地形のテスト2件を足した）。探索シーンの再生（`-Mode explore`）は今回実行していない（`View/` と `Editor/` は変えていない）。前の版（`1982fdd`）では `-Mode explore` がPASS（1920×1080、発見1/18、地図が出るまで9.0秒）。元のリポジトリの Editor では未実行。PlayModeは実行していない。
- thank / .NET 9（Unity非依存、2026-11-22、拡大した地形）：`tools/RegionMapPreview` で seed 7 の地点 5・10・14 の周りを 8km四方と 2km四方で書き出した。海陸の一致 99.0〜100%、川の一致 100%、標高の差の平均は平野 0.9〜1.5m・山地 12.2〜13.2m。作成 1.9〜4.2秒、描画 約0.3秒（1024²）。絵は開発担当が見て、細い谷が黒い溝に見える点・川沿いの升目状の緑・長すぎる影・2km四方での谷の線の4点を直した。広域の衛星写真風と地図帳風の PNG は、変更前の出力とバイト単位で一致した。
- thank 通常Editor（2026-11-21、`-Mode play`、実マウス・キーボード、開発担当）：左クリックでのカーソル移動、Space・Enter・ボタンでのサーチ、矢印キー、ホイール、発見と一覧のクリック、描き方の切替、別の地域の生成を確認した。これは組み直す前の画面での結果で、`9cc6770` 以降の画面では実マウスの操作をまだ試していない（入力の受け方は書き直した）。
- 十字カーソル：2026-11-22、本人の所見（縦と横の重なり）を受け、中心を空けた1枚の画像に替えた。`9cc6770` で腕を発見の輪（半径27）の外へ出し、発見した地点に重ねても輪と腕が重ならない（実画面の拡大で確認）。
- thank / .NET 9（Unity非依存、2026-11-22、エリアと雲）：生成は約6〜8秒で、エリア区分けを含む「土地の種類と探索地点」の工程は約0.5〜0.8秒。エリアは seed 7 で13、seed 3 で11、seed 21 で12。雲の動きは連番192枚を前後の版で書き出し、動画で見比べた。最高標高は .NET とUnityで同じ2887m。
- 本人の回答（2026-11-22）：エリア・雲の流れ・地点の根拠を足した版（`01M80215JPBGRPF79KE7J0YQQF`）は「近い」、「エリアと地点の呼び名」だけ「直したい（次の前に）」、他は「軽微」、次は「ここで十分」。進め方（`01M80219XNGW9T27XQMXDZJQFG`）は SP-021 4.1節。拡大した地形（`01M81JA2CC4WC37TF841JJ8ZEE`）は「近い」・全項目「軽微」・次は「今回の続き」。呼び名と形（`01M81JA6HWR4TJQXZA6T07TC6J`）は SP-021 4.1節。命名の仕組みへの回答は未記録（問い `01M81SA3GF8YMRZRETVZCHBGE2`）。
- 2026-11-01の `af36a0e`（Arch・Export・配置の修正）に対するcompile・EditMode 603/603・PlayMode 9/9は当時の結果で、そのコードは今回変更していない。修正版の通常GUI往復（SP-017）は未実施のまま。
- 未確認（命名と川岸）：仕組みで作った名前を探索画面へ出した時の見え方（確認用ツールの一覧でだけ見た）、命名の規則を人が書き換える時の使い勝手、seed 7・3・21 以外の名前、川岸へ移した地点の seed 7 以外での見え方（テストは seed 5〜9 の位置だけ確かめる）、確認ページ `review-20261122-r8` を本人のブラウザで開いた時の表示（Browser pane では文字と表の配置だけ見た。画像は出なかった）。
- 未確認：差し替えの画像を本人が置いた時の見え方（仮の画像1組でだけ確認。PNG以外の形式、正方形でない画像、取り込み設定が Sprite の画像、1920×1080 以外の画面は未確認）、拡大した地形の Unity での作成時間と、探索画面へ出した時の見え方（確認用ツールの絵でだけ見た）、確認ページ `review-20261122-r7` を本人のブラウザで開いた時の表示（Recordinator の「開く」は応答200。添付18ファイルのハッシュは元と一致）、seed 7 以外の地域と地点 5・10・14 以外での拡大した地形、`9cc6770` 以降の画面での実マウスの操作（右・中ドラッグでの移動、ホイールの拡大・縮小、一覧の行とボタンのクリック、「地名」ボタン、部品の上のクリックが地図へ通らないこと）、雲の層を3枚に増やした後の実際の描画の速さ、拡大した時の地名の重なり、16:9 以外の画面の形での部品の重なり、元のリポジトリの Editor での再生（写しでだけ確認）、PlayMode、ビルドした Player、Windows以外でのフォント表示、1024²を超える解像度、PLANNER007でのSP-021のcompile/tests、残る見た目の制限（SP-021 8節）の解消。

## Next Action

次の作業は、本人の回答から決める。SP-017のArch往復検証を自動で次の作業にしない。

1. 本人：Recordinatorの問いに答える（Project `pc96190c8a9`）。命名の仕組みの方向 `01M81SA3GF8YMRZRETVZCHBGE2`（名前の成り立ち、ことばの範囲、音と語根を誰が書くか、地点名の型の混ぜ方、由来を画面に出すか、足したいもの。添付は Artifact `01M81S8NDJ8B0J2DJ25PKPPW42`＝確認ページ `review-20261122-r8`）。答えられる設問だけでよい。回答済みで閉じた問いは [USER_REQUEST_LEDGER](USER_REQUEST_LEDGER.md) にある。
2. 開発担当：回答を読み、命名の仕組みを作り込む（`Core/RegionNaming.cs` と、規則の既定 `RegionNamingRules.DefaultText`）。土台が決まったら `RegionSpec.NameStyle` の既定を替え、探索画面の地図・発見カード・一覧に出す。使った回答IDと変えた箇所はRecordinatorのTask `8a96ef4f-2e5e-5a58-be66-08d8e485c523` へ1行残す。本人が Editor を開いている間のコード編集と確認は写しで行い、1回で戻す。
3. 開発担当（回答を待たずに進められる）：拡大した地形を探索画面へ統合する（拡大した時に細かい絵へ切り替える。切り替える拡大率と、作成の待ち時間の見せ方は、着手時にラフを出す。調整の定数は `RegionDetail.cs` の `Build` と `CarveGullies`）。その後は E 動きの案（短い動画で2〜3通り）→ F 仮の音で鳴る場所。
4. 本人：構造物の形を画像から作る進め方（画像の枚数・形式・誰が生成するか）を決める。開発担当は計画を出し、承認を得てから画像を作る（計画はまだ出していない）。軸 G のデザインの画像ができたら `Assets/Resources/RegionMapSkin/` に置く（SP-021 6節）。本人の判断待ちは SP-021 9節の表。段6（ゲームの内容）は本人の領分。

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

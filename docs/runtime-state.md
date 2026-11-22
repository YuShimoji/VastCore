# VastCore Runtime State

Last Updated: 2026-11-22

## Current Position

| Field | Value |
|---|---|
| Project | VastCore Terrain Engine |
| Branch | `codex/vc-development-readiness-20260711` |
| Integrated baseline | `1982fdd`（エリア区分け・地点の根拠・2層の雲の流れ）の上に、拡大した地形・構造物の形の案・呼び名の響きの案を足した（2026-11-22、本ファイルと同じ commit） |
| Active outcome | seedから地域一帯の鳥瞰図を自動で作り、その上で座標を指定してサーチし隠れた地点を発見する探索を動かす（SP-021 段1〜4）。本人の回答で段5（拡大した地形と巨大構造物）に着手した。方式は「地図をそのまま細かくする」 |
| Active artifact | `docs/02_design/REGION_MAP_EXPLORATION_SPEC.md` |
| Next mission | 本人が、問い2件に答える（拡大した地形の受入、呼び名の響きと構造物の形の方向）。開発担当は回答を読み、響きの既定の切り替えと、拡大した地形の探索画面への統合へ進む |
| Current bottleneck | 拡大した地形は確認用ツールの絵にだけ出て、探索画面では拡大しても細かい絵へ切り替わらない。統合の前に、この作り方で良いか（地形の受入）と、構造物の形の方向・大きさが要る。決めるのは本人。問いの ID は Next Action |
| Change relation | 継続（2026-11-22、本人依頼「複数のタスクの軸が考えられるため整理して、そのまま可能なら開発を継続」と、進め方の回答 `01M80219XNGW9T27XQMXDZJQFG`）。着手の順は SP-021 4.1節。今回は、呼び名の響きの案と、軸 D の最初の絵を作った |

目的は [SSOT_WORLD](SSOT_WORLD.md) 1節と1.1節。目標アンカー：

- 開発目標：seedから地域一帯の詳細な鳥瞰図を自動で作り、その上にポイントクリック的な宝探しの探索ゲームを載せられる。
- 今回の増分：本人が、1地点の周りを細かく作り直した地形を広域の絵と見比べて、求めた拡大の形に近いかを言える。構造物の形の案3通りと、呼び名の響き3通りから、近い方向を言える。
- 現在のボトルネック：上の表の Current bottleneck。

全体工程（段1〜6）と現在地の表は [SP-021](02_design/REGION_MAP_EXPLORATION_SPEC.md) 4節が持つ。SP-017 / SP-020（Arch・Prefab Stamp・DualGridの経路）は未実施のまま保留で、2026-11-21再開準備時の状態本文は [再開履歴](restart/VC_RESTART_20261101_HISTORY.md) の「2026-11-21 thank版（SP-017再開準備）」へ原文のまま移した。

## 作業ツリーと端末の現物（2026-11-22 thank観測）

- SP-021で追加したもの：`Assets/Scripts/RegionMap/`（Core・View・Editor、asmdef 2件）、`Assets/Scenes/RegionExplorer.unity`、`Assets/Tests/EditMode/RegionMapTests.cs`、`tools/RegionMapPreview/`、`scripts/region-map.ps1`。`1982fdd` の後に足したのは `Core/RegionDetail.cs`（拡大した地形）・`Core/RegionStructures.cs`（構造物の形の案）・`Core/RegionNames.cs`（呼び名の響き）の3件。変えたのは `Core/` の `RegionData`・`RegionGenerator`・`RegionSite`・`RegionAreas`・`RegionSpec`・`RegionMapRenderer`、テスト、`tools/RegionMapPreview/Program.cs`。`View/`・`Editor/`・シーン・`ProjectSettings/`・`Packages/`・Terrain・StructureGenerator・Stamp は変更していない。
- 未commitのまま保持するもの（前から同じ）：`.serena/project.yml`、untrackedの `Assets/Resources/Stamps/`（旧失敗候補）とそのmeta。どれも上書き・削除しない。
- 生成物は ignored の `artifacts/region-map/`（鳥瞰図PNG、探索画面の画像 `explorer-*.png`、確認ページ `review-20261122-r7/`（拡大した地形・形の案・呼び名の響き）・`review-20261122-r6/`（エリア・地点の根拠・雲の流れ）・`review-20261122-r5/`・`review-20261122-r4/`、ラフの元 `rough-20261122-screen/`）と `artifacts/logs/region-map-*.log`。thank端末にだけある。
- Unityのバッチ起動は、Unity Hubが起動していないと `No valid Unity Editor license found`（exit 198）で失敗する。本人がこのプロジェクトを Editor で開いている間は、バッチも2つ目の Editor も起動できない（`another Unity instance is running`）。その間の確認用に、`D:\vcv\VastCore`（Assets・Packages・ProjectSettings・scripts の写しと、そこで取り込んだ Library。git 管理外）を残してある。要らなくなったらフォルダごと消してよい。
- 残しているローカル限定の履歴（`master`、`feature/mcp-verification`、stash 2件）とworktree `.codex-tmp/cloud-codex-20261011/VastCore` は再開準備時のまま。

## Current Trust Assessment

- thank / Unity 6000.3.6f1 / 2026-11-22（拡大した地形・形の案・呼び名を足した版を写し `D:\vcv\VastCore` で実行。元のリポジトリへ戻した14ファイルとハッシュ一致）：`run-tests.ps1 -TestMode editmode` が616/616、失敗/skip 0（呼び名と拡大した地形のテスト2件を足した）。探索シーンの再生（`-Mode explore`）は今回実行していない（`View/` と `Editor/` は変えていない）。前の版（`1982fdd`）では `-Mode explore` がPASS（1920×1080、発見1/18、地図が出るまで9.0秒）。元のリポジトリの Editor では未実行。PlayModeは実行していない。
- thank / .NET 9（Unity非依存、2026-11-22、拡大した地形）：`tools/RegionMapPreview` で seed 7 の地点 5・10・14 の周りを 8km四方と 2km四方で書き出した。海陸の一致 99.0〜100%、川の一致 100%、標高の差の平均は平野 0.9〜1.5m・山地 12.2〜13.2m。作成 1.9〜4.2秒、描画 約0.3秒（1024²）。絵は開発担当が見て、細い谷が黒い溝に見える点・川沿いの升目状の緑・長すぎる影・2km四方での谷の線の4点を直した。広域の衛星写真風と地図帳風の PNG は、変更前の出力とバイト単位で一致した。
- thank 通常Editor（2026-11-21、`-Mode play`、実マウス・キーボード、開発担当）：左クリックでのカーソル移動、Space・Enter・ボタンでのサーチ、矢印キー、ホイール、発見と一覧のクリック、描き方の切替、別の地域の生成を確認した。これは組み直す前の画面での結果で、`9cc6770` 以降の画面では実マウスの操作をまだ試していない（入力の受け方は書き直した）。
- 十字カーソル：2026-11-22、本人の所見（縦と横の重なり）を受け、中心を空けた1枚の画像に替えた。`9cc6770` で腕を発見の輪（半径27）の外へ出し、発見した地点に重ねても輪と腕が重ならない（実画面の拡大で確認）。
- thank / .NET 9（Unity非依存、2026-11-22、エリアと雲）：生成は約6〜8秒で、エリア区分けを含む「土地の種類と探索地点」の工程は約0.5〜0.8秒。エリアは seed 7 で13、seed 3 で11、seed 21 で12。雲の動きは連番192枚を前後の版で書き出し、動画で見比べた。最高標高は .NET とUnityで同じ2887m。
- 本人の回答（2026-11-22）：エリア・雲の流れ・地点の根拠を足した版（`01M80215JPBGRPF79KE7J0YQQF`）は「近い」、「エリアと地点の呼び名」だけ「直したい（次の前に）」、他は「軽微」、次は「ここで十分」。進め方（`01M80219XNGW9T27XQMXDZJQFG`）は SP-021 4.1節。拡大した地形・形の案・呼び名の響きへの本人の回答は未記録（問い2件、ID は Next Action）。
- 2026-11-01の `af36a0e`（Arch・Export・配置の修正）に対するcompile・EditMode 603/603・PlayMode 9/9は当時の結果で、そのコードは今回変更していない。修正版の通常GUI往復（SP-017）は未実施のまま。
- 未確認：拡大した地形の Unity での作成時間と、探索画面へ出した時の見え方（確認用ツールの絵でだけ見た）、確認ページ `review-20261122-r7` を本人のブラウザで開いた時の表示（Recordinator の「開く」は応答200。添付18ファイルのハッシュは元と一致）、seed 7 以外の地域と地点 5・10・14 以外での拡大した地形、`9cc6770` 以降の画面での実マウスの操作（右・中ドラッグでの移動、ホイールの拡大・縮小、一覧の行とボタンのクリック、「地名」ボタン、部品の上のクリックが地図へ通らないこと）、雲の層を3枚に増やした後の実際の描画の速さ、拡大した時の地名の重なり、16:9 以外の画面の形での部品の重なり、元のリポジトリの Editor での再生（写しでだけ確認）、PlayMode、ビルドした Player、Windows以外でのフォント表示、1024²を超える解像度、PLANNER007でのSP-021のcompile/tests、残る見た目の制限（SP-021 8節）の解消。

## Next Action

次の作業は、本人の回答から決める。SP-017のArch往復検証を自動で次の作業にしない。

1. 本人：Recordinatorの問い2件に答える（Project `pc96190c8a9`、添付Artifact はどちらも `01M81J1D5NV1NMANPRJP7HE63B`＝確認ページ `review-20261122-r7`）。受入 `01M81JA2CC4WC37TF841JJ8ZEE`（拡大した地形：8km四方と2km四方）、方向 `01M81JA6HWR4TJQXZA6T07TC6J`（呼び名の響き、地点名の形、構造物の形の方向と大きさ、川に重なる地点の置き方）。答えられる設問だけでよい。回答済みで閉じた問い：`01M80215JPBGRPF79KE7J0YQQF`、`01M80219XNGW9T27XQMXDZJQFG`、`01M7ZR26FG8ND2E55JDM2PMJX3`、`01M7ZCPPXKKC8V0HS21SYQ9G2Y`、`01M7Z6YNHPZKBMGY0Z5429WE71`、`01M7ZABDTFVETDP0AAFAGVWP0C`。
2. 開発担当：回答を読む。呼び名の響きが選ばれたら `RegionSpec.NameStyle` の既定を替え、探索画面の地図・発見カード・一覧に出す（地点名の形の回答も反映する）。拡大した地形の受入に「直したい」以上があれば先に直す（調整の定数は `RegionDetail.cs` の `Build` と `CarveGullies`）。無ければ、探索画面で拡大した時に細かい絵へ切り替える統合へ進む（切り替える拡大率、作成の待ち時間の見せ方は、着手時にラフを出す）。構造物の形は、選ばれた方向の違いを2〜3通り作って再提示する。使った回答IDと変えた箇所はRecordinatorのTask `8a96ef4f-2e5e-5a58-be66-08d8e485c523` へ1行残す。本人が Editor を開いている間のコード編集と確認は写しで行い、1回で戻す。
3. その後の順（SP-021 4.1節）：G 差し替えの入口 → E 動きの案（短い動画で2〜3通り）→ F 仮の音で鳴る場所。本人の判断待ちは SP-021 9節の表。段6（ゲームの内容）は本人の領分。
4. 回答を待たずに進められる作業：軸 G（十字・印・輪の画像を外から差し替える入口）は、今の2件の回答に依存しない。今回は着手していない。

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

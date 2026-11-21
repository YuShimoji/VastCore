# VastCore Runtime State

Last Updated: 2026-11-22

## Current Position

| Field | Value |
|---|---|
| Project | VastCore Terrain Engine |
| Branch | `codex/vc-development-readiness-20260711` |
| Integrated baseline | `9cc6770`（全面の地図と半透明の部品の探索画面）の上に、エリア区分け・地点の根拠・2層の雲の流れを足した（2026-11-22、本ファイルと同じ commit） |
| Active outcome | seedから地域一帯の鳥瞰図を自動で作り、その上で座標を指定してサーチし隠れた地点を発見する探索を動かす（SP-021 段1〜4）。本人は探索画面と雲の版を「近い」と答え、次の軸を挙げた（SP-021 4.1節の A〜G） |
| Active artifact | `docs/02_design/REGION_MAP_EXPLORATION_SPEC.md` |
| Next mission | 本人が、問い2件に答える（足した版の受入、残りの軸 D〜G と呼び名の進め方）。開発担当は回答の方式から次の軸に着手する |
| Current bottleneck | 残りの軸（D 拡大した表示と巨大構造物、E UIエフェクト・アニメーション、F サウンド、G デザインの差し替え）は、方式・誰が方向を出すか・素材の出所が決まっていない。決めるのは本人。問いの ID は Next Action |
| Change relation | 継続（2026-11-22、本人依頼「複数のタスクの軸が考えられるため整理して、そのまま可能なら開発を継続」）。回答2件を7つの軸に分け、方式の判断が要らない A エリア区分けと名前・B 雲の流れ・C 地点の位置の根拠を実装した |

目的は [SSOT_WORLD](SSOT_WORLD.md) 1節と1.1節。目標アンカー：

- 開発目標：seedから地域一帯の詳細な鳥瞰図を自動で作り、その上にポイントクリック的な宝探しの探索ゲームを載せられる。
- 今回の増分：本人が、探索画面でエリアの名前と境界・場所ごとに動きの違う雲を見て、地点がなぜそこに置かれたかを図と表で読み、求めた形に近いかを言える。
- 現在のボトルネック：上の表の Current bottleneck。

全体工程（段1〜6）と現在地の表は [SP-021](02_design/REGION_MAP_EXPLORATION_SPEC.md) 4節が持つ。SP-017 / SP-020（Arch・Prefab Stamp・DualGridの経路）は未実施のまま保留で、2026-11-21再開準備時の状態本文は [再開履歴](restart/VC_RESTART_20261101_HISTORY.md) の「2026-11-21 thank版（SP-017再開準備）」へ原文のまま移した。

## 作業ツリーと端末の現物（2026-11-22 thank観測）

- SP-021で追加したもの：`Assets/Scripts/RegionMap/`（Core・View・Editor、asmdef 2件）、`Assets/Scenes/RegionExplorer.unity`、`Assets/Tests/EditMode/RegionMapTests.cs`、`tools/RegionMapPreview/`、`scripts/region-map.ps1`。`9cc6770` の後に足したのは `Core/RegionAreas.cs`（エリア区分け）・`Core/RegionCloudFlow.cs`（雲の流れの計算）・`View/RegionCloudLayer.cs`（升目のメッシュで描く雲の層）の3件。変えたのは `Core/` の `RegionData`・`RegionGenerator`・`RegionSite`・`RegionClouds`、`View/RegionExplorer.cs`、`Editor/RegionExplorerSmoke.cs`、テスト、`tools/RegionMapPreview/Program.cs`。シーン・`ProjectSettings/`・`Packages/`・Terrain・StructureGenerator・Stamp は変更していない。
- 未commitのまま保持するもの（前から同じ）：`.serena/project.yml`、untrackedの `Assets/Resources/Stamps/`（旧失敗候補）とそのmeta。どれも上書き・削除しない。
- 生成物は ignored の `artifacts/region-map/`（鳥瞰図PNG、探索画面の画像 `explorer-*.png`、確認ページ `review-20261122-r6/`（エリア・地点の根拠・雲の流れ）・`review-20261122-r5/`（全面の地図の版）・`review-20261122-r4/`、ラフの元 `rough-20261122-screen/`）と `artifacts/logs/region-map-*.log`。thank端末にだけある。
- Unityのバッチ起動は、Unity Hubが起動していないと `No valid Unity Editor license found`（exit 198）で失敗する。本人がこのプロジェクトを Editor で開いている間は、バッチも2つ目の Editor も起動できない（`another Unity instance is running`）。その間の確認用に、`D:\vcv\VastCore`（Assets・Packages・ProjectSettings・scripts の写しと、そこで取り込んだ Library。git 管理外）を残してある。要らなくなったらフォルダごと消してよい。
- 残しているローカル限定の履歴（`master`、`feature/mcp-verification`、stash 2件）とworktree `.codex-tmp/cloud-codex-20261011/VastCore` は再開準備時のまま。

## Current Trust Assessment

- thank / Unity 6000.3.6f1 / 2026-11-22（エリア・雲の流れを足した版を写し `D:\vcv\VastCore` で実行。元のリポジトリへ戻したファイルとハッシュ一致）：`-Mode explore` がPASS（1920×1080、発見1/18、サーチ2回、再生から地図が出るまで9.0秒。Editor 起動直後の1回目は14.4秒）。`run-tests.ps1 -TestMode editmode` が614/614、失敗/skip 0（エリアと雲の流れのテスト2件を足した）。撮った7枚（開始・反応・発見・地域の全体・地名と境界・雲が薄い・地図帳）と連続撮影145枚を開発担当が見て、白い雲の上で地名が読めない点を、縁を濃くして撮り直した。元のリポジトリの Editor では未実行。PlayModeは実行していない。`-Mode setup` は今回実行していない。
- thank 通常Editor（2026-11-21、`-Mode play`、実マウス・キーボード、開発担当）：左クリックでのカーソル移動、Space・Enter・ボタンでのサーチ、矢印キー、ホイール、発見と一覧のクリック、描き方の切替、別の地域の生成を確認した。これは組み直す前の画面での結果で、`9cc6770` 以降の画面では実マウスの操作をまだ試していない（入力の受け方は書き直した）。
- 十字カーソル：2026-11-22、本人の所見（縦と横の重なり）を受け、中心を空けた1枚の画像に替えた。`9cc6770` で腕を発見の輪（半径27）の外へ出し、発見した地点に重ねても輪と腕が重ならない（実画面の拡大で確認）。
- thank / .NET 9（Unity非依存、2026-11-22）：`tools/RegionMapPreview` で seed 7 の衛星写真風を、雲を2層へ分ける前後で書き出し、PNG がバイト単位で一致した（1024²。焼き込みの絵と地点の位置は変わっていない）。生成は約6〜8秒で、エリア区分けを含む「土地の種類と探索地点」の工程は約0.5〜0.8秒（区分けを足す前の同じ工程の時間は測っていない）。エリアは seed 7 で13、seed 3 で11、seed 21 で12。雲の動きは連番192枚を前後の版で書き出し、動画で見比べた。最高標高は .NET とUnityで同じ2887m。
- 本人の回答（2026-11-22）：全面の地図へ組み直した探索画面（`01M7ZR26FG8ND2E55JDM2PMJX3`）は「近い」「構想図どおり」、雲と待ち時間を直した版（`01M7ZCPPXKKC8V0HS21SYQ9G2Y`）は「近い」。指摘は全て「軽微」で、次は「別の方向へ」。今回足したエリア・雲の流れ・地点の根拠への本人の回答は未記録（問い2件、ID は Next Action）。
- 2026-11-01の `af36a0e`（Arch・Export・配置の修正）に対するcompile・EditMode 603/603・PlayMode 9/9は当時の結果で、そのコードは今回変更していない。修正版の通常GUI往復（SP-017）は未実施のまま。
- 未確認：`9cc6770` 以降の画面での実マウスの操作（右・中ドラッグでの移動、ホイールの拡大・縮小、一覧の行とボタンのクリック、「地名」ボタン、部品の上のクリックが地図へ通らないこと）、雲の層を3枚に増やした後の実際の描画の速さ（自動再生は画面の保存で遅くなるので測れていない）、拡大した時の地名の重なり、16:9 以外の画面の形での部品の重なり、発見した地点が多い時の一覧の詰まり方（呼び名が長くなった）、元のリポジトリの Editor での再生（写しでだけ確認）、PlayMode、ビルドした Player、Recordinator の画面での確認ページの表示、Windows以外でのフォント表示、1024²を超える解像度（全面に広げたので前より粗く見える）、PLANNER007でのSP-021のcompile/tests、残る見た目の制限（SP-021 8節：湖、平野の薄い筋）の解消。

## Next Action

次の作業は、本人の回答から決める。SP-017のArch往復検証を自動で次の作業にしない。

1. 本人：Recordinatorの問い2件に答える（Project `pc96190c8a9`、添付Artifact はどちらも `01M802056CA6S2DYZT5VFA2EME`＝確認ページ `review-20261122-r6`）。受入 `01M80215JPBGRPF79KE7J0YQQF`（エリア区分け・地点の根拠・雲の流れを足した版）、進め方 `01M80219XNGW9T27XQMXDZJQFG`（次に最初に着手する軸、拡大した表示と巨大構造物の方式、エフェクトとアニメーション・サウンドの進め方、デザインの差し替えの入口、呼び名の付け方）。Unityで触る場合は `Assets/Scenes/RegionExplorer` を再生する（Editor を閉じているなら `.\scripts\region-map.ps1 -Mode play`）。回答済みで閉じた問い：`01M7ZR26FG8ND2E55JDM2PMJX3`、`01M7ZCPPXKKC8V0HS21SYQ9G2Y`、`01M7Z6YNHPZKBMGY0Z5429WE71`、`01M7ZABDTFVETDP0AAFAGVWP0C`。
2. 開発担当：回答を読む。受入に「直したい」以上があれば先に直す。無ければ、進め方の問いで選ばれた軸の、選ばれた方式から着手する（SP-021 4.1節・9節）。エリアの分け方は `RegionAreas.cs` 冒頭の定数、雲の動きは `RegionCloudFlow.cs` 冒頭の定数、地名の出し方は `RegionExplorer.cs` の `BuildAreaLabels` / `RefreshAreaLayers` で変えられる。使った回答IDと変えた箇所はRecordinatorのTask `8a96ef4f-2e5e-5a58-be66-08d8e485c523` へ1行残す。本人が Editor を開いている間のコード編集と確認は写しで行い、1回で戻す。
3. 本人の判断待ち：SP-021 9節の表。未回答の設問の軸には着手しない。段6（ゲームの内容）は本人の領分。
4. 回答を待たずに進める作業：無い（軸 D〜G は方式・方向・素材の出所が回答で変わる。実マウスでの操作の確認は、本人が Unity で触る回答と重なるので、回答の内容を見てから範囲を決める）。

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

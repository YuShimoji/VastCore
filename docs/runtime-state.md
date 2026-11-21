# VastCore Runtime State

Last Updated: 2026-11-22

## Current Position

| Field | Value |
|---|---|
| Project | VastCore Terrain Engine |
| Branch | `codex/vc-development-readiness-20260711` |
| Integrated baseline | `657d649`（雲の層・待ち時間の短縮・十字カーソル）の上に、`9cc6770` で探索画面を全面の地図と半透明の部品へ組み直した |
| Active outcome | seedから地域一帯の鳥瞰図を自動で作り、その上で座標を指定してサーチし隠れた地点を発見する探索を動かす（SP-021 段1〜4） |
| Active artifact | `docs/02_design/REGION_MAP_EXPLORATION_SPEC.md` |
| Next mission | 本人が、全面の地図へ組み直した探索画面と、雲と待ち時間を直した版を見て答える。重い指摘から直す |
| Current bottleneck | 組み直した探索画面の受入が未回答。本人はラフ3案に対しチャットで「CとAの混合」と答え（原文は [USER_REQUEST_LEDGER](USER_REQUEST_LEDGER.md)）、その形で組み直した。実画面は開発担当が自動再生の画像で確かめただけで、本人はまだ見ていない。受入は問い `01M7ZR26FG8ND2E55JDM2PMJX3`、雲と待ち時間は問い `01M7ZCPPXKKC8V0HS21SYQ9G2Y` |
| Change relation | 方向転換（2026-11-21、本人依頼）。SP-017のArch往復検証から、地域鳥瞰図と探索の土台へ主作業を移した。同日、鳥瞰図の描き方は本人が「衛星写真風」と答え、作り直した版には「近い」と答えた |

目的は [SSOT_WORLD](SSOT_WORLD.md) 1節と1.1節。目標アンカー：

- 開発目標：seedから地域一帯の詳細な鳥瞰図を自動で作り、その上にポイントクリック的な宝探しの探索ゲームを載せられる。
- 今回の増分：本人が、全面の地図に半透明の部品を重ねた探索画面を実画面（できれば Unity で触って）で見て、部品の透け方・配置・文字・発見した時の出方が求めた形に近いかを言える。
- 現在のボトルネック：上の表の Current bottleneck。

全体工程（段1〜6）と現在地の表は [SP-021](02_design/REGION_MAP_EXPLORATION_SPEC.md) 4節が持つ。SP-017 / SP-020（Arch・Prefab Stamp・DualGridの経路）は未実施のまま保留で、2026-11-21再開準備時の状態本文は [再開履歴](restart/VC_RESTART_20261101_HISTORY.md) の「2026-11-21 thank版（SP-017再開準備）」へ原文のまま移した。

## 作業ツリーと端末の現物（2026-11-22 thank観測）

- SP-021で追加したもの：`Assets/Scripts/RegionMap/`（Core・View・Editor、asmdef 2件）、`Assets/Scenes/RegionExplorer.unity`、`Assets/Tests/EditMode/RegionMapTests.cs`、`tools/RegionMapPreview/`、`scripts/region-map.ps1`。`657d649` の後に変えたのは `View/RegionExplorer.cs`（画面の組み立てと表示計算の書き直し）と `Editor/RegionExplorerSmoke.cs`（地域の全体の撮影を追加）の2件。Core・テスト・シーン・`ProjectSettings/`・`Packages/`・Terrain・StructureGenerator・Stamp は変更していない。
- 未commitのまま保持するもの（前から同じ）：`.serena/project.yml`、untrackedの `Assets/Resources/Stamps/`（旧失敗候補）とそのmeta。どれも上書き・削除しない。
- 生成物は ignored の `artifacts/region-map/`（鳥瞰図PNG、探索画面の画像 `explorer-*.png`、確認ページ `review-20261122-r5/`（全面の地図の版）と `review-20261122-r4/`、ラフの元 `rough-20261122-screen/`）と `artifacts/logs/region-map-*.log`。thank端末にだけある。
- Unityのバッチ起動は、Unity Hubが起動していないと `No valid Unity Editor license found`（exit 198）で失敗する。本人がこのプロジェクトを Editor で開いている間は、バッチも2つ目の Editor も起動できない（`another Unity instance is running`）。その間の確認用に、`D:\vcv\VastCore`（Assets・Packages・ProjectSettings・scripts の写しと、そこで取り込んだ Library。git 管理外）を残してある。要らなくなったらフォルダごと消してよい。
- 残しているローカル限定の履歴（`master`、`feature/mcp-verification`、stash 2件）とworktree `.codex-tmp/cloud-codex-20261011/VastCore` は再開準備時のまま。

## Current Trust Assessment

- thank / Unity 6000.3.6f1 / 2026-11-22（`9cc6770` と同じ内容を写し `D:\vcv\VastCore` で実行）：`-Mode explore` がPASS（1920×1080、発見1/18、サーチ2回、再生から地図が出るまで11.8秒）。`run-tests.ps1 -TestMode editmode` が612/612、失敗/skip 0。撮った6枚（開始・反応・発見・地域の全体・雲が薄い・地図帳）を開発担当が等倍で見て、一覧の行が作った直後に白く光る・数字と日本語の値の高さが揃わない・十字の腕が発見の輪に重なる、の3点を直して撮り直した。`-Mode setup`（生成12.5秒・描画1.7秒）は `657d649` の時の結果で、今回は Core を変えていない。元のリポジトリの Editor では未実行。PlayModeは実行していない。
- thank 通常Editor（2026-11-21、`-Mode play`、実マウス・キーボード、開発担当）：左クリックでのカーソル移動、Space・Enter・ボタンでのサーチ、矢印キー、ホイール、発見と一覧のクリック、描き方の切替、別の地域の生成を確認した。これは組み直す前の画面での結果で、`9cc6770` の画面では実マウスの操作をまだ試していない（入力の受け方は書き直した）。
- 十字カーソル：2026-11-22、本人の所見（縦と横の重なり）を受け、中心を空けた1枚の画像に替えた。`9cc6770` で腕を発見の輪（半径27）の外へ出し、発見した地点に重ねても輪と腕が重ならない（実画面の拡大で確認）。
- thank / .NET 9（Unity非依存）：`tools/RegionMapPreview` で seed 7 の衛星写真風を、並列化と経路探索の作り替えの前後で書き出し、PNG がバイト単位で一致した（1024²、生成約6秒。前は約9秒）。雲の層は2×2に並べて継ぎ目が無いことを開発担当が目で確かめた。最高標高は .NET とUnityで同じ2887m。
- 山地の平行な溝は流れ先のゆらぎで消えた（2026-11-21）。作り直した版に本人は「近い」と答え、受入で雲・探索画面・待ち時間を「直したい」とした。探索画面の方向は本人がチャットで「CとAの混合」と答えた。組み直した版と、雲・待ち時間を直した版への本人の回答は未記録（問い2件、ID は Next Action）。
- 2026-11-01の `af36a0e`（Arch・Export・配置の修正）に対するcompile・EditMode 603/603・PlayMode 9/9は当時の結果で、そのコードは今回変更していない。修正版の通常GUI往復（SP-017）は未実施のまま。
- 未確認：`9cc6770` の画面での実マウスの操作（右・中ドラッグでの移動、ホイールの拡大・縮小、一覧の行とボタンのクリック、部品の上のクリックが地図へ通らないこと）、16:9 以外の画面の形での部品の重なり、発見した地点が多い時の一覧の詰まり方、元のリポジトリの Editor での再生（写しでだけ確認）、PlayMode、ビルドした Player、Recordinator の画面での確認ページの表示、Windows以外でのフォント表示、1024²を超える解像度（全面に広げたので前より粗く見える）、PLANNER007でのSP-021のcompile/tests、残る見た目の制限（SP-021 8節：湖、平野の薄い筋）の解消。

## Next Action

次の作業は、本人の受入の回答から決める。SP-017のArch往復検証を自動で次の作業にしない。

1. 本人：Recordinatorの問い2件に答える（Project `pc96190c8a9`）。受入 `01M7ZR26FG8ND2E55JDM2PMJX3`（全面の地図へ組み直した探索画面、添付Artifact `01M7ZR1AXN41KE4SM1VTQ09YXK`）、受入 `01M7ZCPPXKKC8V0HS21SYQ9G2Y`（雲と待ち時間、添付Artifact `01M7ZCNXR74S36807YX64589YA`）。Unityで触る場合は `Assets/Scenes/RegionExplorer` を再生する（Editor を閉じているなら `.\scripts\region-map.ps1 -Mode play`）。閉じた問い：`01M7Z6YNHPZKBMGY0Z5429WE71` と `01M7ZABDTFVETDP0AAFAGVWP0C` は回答済み、方向 `01M7ZCPS41GTYK18C3BP6ZS3T6` は本人がチャットで答えたので取り下げた。
2. 開発担当：回答を読み、重い指摘から直す。部品の透け方・大きさ・位置、文字、発見カードは `RegionExplorer.cs` 冒頭の定数と各 `Build*` で変えられる。全体の形が違うと言われた時はラフへ戻る。使った回答IDと変えた箇所はRecordinatorのTask `8a96ef4f-2e5e-5a58-be66-08d8e485c523` へ1行残す。本人が Editor を開いている間のコード編集と確認は写しで行い、1回で戻す。
3. 本人の判断待ち（SP-021 9節）：地点のローカル表示の方式（既存の構造物経路を使うか）、地域の規模と解像度。決まるまで段5・6へは進まない。
4. 回答を待たずに進める作業：無い（組み直した画面は出した。実マウスでの操作の確認は、本人が Unity で触る回答と重なるので、回答の内容を見てから範囲を決める）。

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

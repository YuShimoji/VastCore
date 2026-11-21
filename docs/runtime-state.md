# VastCore Runtime State

Last Updated: 2026-11-22

## Current Position

| Field | Value |
|---|---|
| Project | VastCore Terrain Engine |
| Branch | `codex/vc-development-readiness-20260711` |
| Integrated baseline | `f3f7146`（探索画面を触って見つけた点の修正）の上に、雲の層・濃さの切替・流れる動き、待ち時間の短縮、十字カーソルの作り直しを追加 |
| Active outcome | seedから地域一帯の鳥瞰図を自動で作り、その上で座標を指定してサーチし隠れた地点を発見する探索を動かす（SP-021 段1〜4） |
| Active artifact | `docs/02_design/REGION_MAP_EXPLORATION_SPEC.md` |
| Next mission | 本人が雲と待ち時間を直した版を見て答え、探索画面を洗練する方向をラフ3案から選ぶ。選ばれた方向で探索画面を組み直す |
| Current bottleneck | 探索画面の洗練の方向が未定。本人の回答（`01M7ZABDTFVETDP0AAFAGVWP0C` revision 1、2026-11-22読取）は、文字と文言・見た目の質感・操作の流れ・カーソルと印・発見した時が「直したい（次の前に）」、配置と反応の出方は「軽微」。所見は「十字キーが重なって縦が上になっていたりするので、洗練が必要のように見えています」。名指しの十字は直した。全体の方向は問い `01M7ZCPS41GTYK18C3BP6ZS3T6` で聞いている |
| Change relation | 方向転換（2026-11-21、本人依頼）。SP-017のArch往復検証から、地域鳥瞰図と探索の土台へ主作業を移した。同日、鳥瞰図の描き方は本人が「衛星写真風」と答え、作り直した版には「近い」と答えた |

目的は [SSOT_WORLD](SSOT_WORLD.md) 1節と1.1節。目標アンカー：

- 開発目標：seedから地域一帯の詳細な鳥瞰図を自動で作り、その上にポイントクリック的な宝探しの探索ゲームを載せられる。
- 今回の増分：本人が、増えた雲・濃さの切替・流れる動き・短くなった待ち時間を動画と実画面で見て答え、探索画面の洗練ラフ3案のどれが近いかを言える。
- 現在のボトルネック：上の表の Current bottleneck。

全体工程（段1〜6）と現在地の表は [SP-021](02_design/REGION_MAP_EXPLORATION_SPEC.md) 4節が持つ。SP-017 / SP-020（Arch・Prefab Stamp・DualGridの経路）は未実施のまま保留で、2026-11-21再開準備時の状態本文は [再開履歴](restart/VC_RESTART_20261101_HISTORY.md) の「2026-11-21 thank版（SP-017再開準備）」へ原文のまま移した。

## 作業ツリーと端末の現物（2026-11-22 thank観測）

- SP-021で追加したもの：`Assets/Scripts/RegionMap/`（Core・View・Editor、asmdef 2件）、`Assets/Scenes/RegionExplorer.unity`、`Assets/Tests/EditMode/RegionMapTests.cs`、`tools/RegionMapPreview/`、`scripts/region-map.ps1`。`f3f7146` の後に変えたのは、`Core/RegionClouds.cs`（新規）と `RegionNoise`・`RegionMapRenderer`・`RegionGenerator`・`FlowRouter`、`View/RegionExplorer.cs`・`RegionMapTexture.cs`、`Editor/RegionExplorerSmoke.cs`、テスト2件、`scripts/region-map.ps1`（`-ClipSeconds`）、シーンの雲の量。Terrain・StructureGenerator・Stamp・`ProjectSettings/`・`Packages/` は変更していない。
- 未commitのまま保持するもの（前から同じ）：`.serena/project.yml`、untrackedの `Assets/Resources/Stamps/`（旧失敗候補）とそのmeta。どれも上書き・削除しない。
- 生成物は ignored の `artifacts/region-map/`（鳥瞰図PNG、探索画面の画像、確認ページ `review-20261122-r4/`、ラフの元 `rough-20261122-screen/`）と `artifacts/logs/region-map-*.log`。thank端末にだけある。
- Unityのバッチ起動は、Unity Hubが起動していないと `No valid Unity Editor license found`（exit 198）で失敗する。本人がこのプロジェクトを Editor で開いている間は、バッチも2つ目の Editor も起動できない（`another Unity instance is running`）。その間の確認用に、`D:\vcv\VastCore`（Assets・Packages・ProjectSettings・scripts の写しと、そこで取り込んだ Library。git 管理外）を残してある。要らなくなったらフォルダごと消してよい。
- 残しているローカル限定の履歴（`master`、`feature/mcp-verification`、stash 2件）とworktree `.codex-tmp/cloud-codex-20261011/VastCore` は再開準備時のまま。

## Current Trust Assessment

- thank / Unity 6000.3.6f1 / 2026-11-22（雲・待ち時間・十字カーソルの後。本人が元のプロジェクトを Editor で開いていたので、写し `D:\vcv\VastCore` で実行）：`-Mode setup` がexit 0（コンパイルエラー0、Editor内で生成12.5秒・描画1.7秒。前は26.8秒・3.0秒）。`run-tests.ps1 -TestMode editmode` が612/612（既存603＋`RegionMapTests` 9）、失敗/skip 0。`-Mode explore` がPASS（1920×1080、発見1/18、再生から地図が出るまで9.2〜10.9秒、雲は16.1秒で東へ0.0376）。十字と連続撮影を足した後は explore だけ再実行した。元のリポジトリの Editor では未実行。PlayModeは実行していない。
- thank 通常Editor（2026-11-21、`-Mode play`、Gameビューを実マウス・キーボードで操作、開発担当）：左クリックでカーソルが押した位置へ動く（拡大中も一致）、Space・Enter・サーチボタンでサーチ、ボタンや一覧をクリックした後のSpaceは1回だけ数える、矢印キーは0.5（Shiftで0.1）、ホイールで拡大と縮小、地点の発見と一覧のクリックでの移動、描き方の切替、別の地域の生成（seed 8、発見とサーチ回数が0に戻る）を確認した。知らせの帯は数秒で消える。
- 同じ確認で直した点：白だけのカーソルが雲の上と地図帳風で見えなかったので暗い縁を付けた。2026-11-22、本人の所見（縦と横の重なり）を受け、十字を中心を空けた1枚の画像に替えた（実画面の拡大で確認）。
- thank / .NET 9（Unity非依存）：`tools/RegionMapPreview` で seed 7 の衛星写真風を、並列化と経路探索の作り替えの前後で書き出し、PNG がバイト単位で一致した（1024²、生成約6秒。前は約9秒）。雲の層は2×2に並べて継ぎ目が無いことを開発担当が目で確かめた。最高標高は .NET とUnityで同じ2887m。
- 山地の平行な溝は流れ先のゆらぎで消えた（2026-11-21）。作り直した版に本人は「近い」と答え、受入で雲・探索画面・待ち時間を「直したい」とした。今回の版（雲・待ち時間・十字）と、探索画面の洗練の方向に対する本人の回答は未記録。問いは2件送った（ID は Next Action）。
- 2026-11-01の `af36a0e`（Arch・Export・配置の修正）に対するcompile・EditMode 603/603・PlayMode 9/9は当時の結果で、そのコードは今回変更していない。修正版の通常GUI往復（SP-017）は未実施のまま。
- 未確認：元のリポジトリの Editor での今回の版の再生（写しでだけ確認）、実マウスでの「雲」ボタンと新しいボタン配置の操作（自動再生では関数を呼んで確認）、右・中ドラッグでの地図の移動、PlayMode、ビルドした Player での生成時間、Recordinator の画面での動画の再生、Windows以外でのフォント表示、1024²を超える解像度、PLANNER007でのSP-021のcompile/tests、残る見た目の制限（SP-021 8節：湖、平野の薄い筋）の解消。

## Next Action

次の作業は、本人の受入の回答から決める。SP-017のArch往復検証を自動で次の作業にしない。

1. 本人：Recordinatorの問い2件に答える（Project `pc96190c8a9`、添付Artifact `01M7ZCNXR74S36807YX64589YA`）。受入 `01M7ZCPPXKKC8V0HS21SYQ9G2Y`（雲と待ち時間を直した版）、方向 `01M7ZCPS41GTYK18C3BP6ZS3T6`（探索画面の洗練ラフ A・B・C）。Unityで触る場合は、開いている Editor で `Assets/Scenes/RegionExplorer` を再生する（閉じているなら `.\scripts\region-map.ps1 -Mode play`）。閉じた問い：`01M7Z6YNHPZKBMGY0Z5429WE71` と `01M7ZABDTFVETDP0AAFAGVWP0C` は回答済み。
2. 開発担当：回答を読み、受入の重い指摘から直す。方向が選ばれたら、軸ごとに違う案の時は組み合わせのラフを1枚出してから探索画面を組み直す。使った回答IDと変えた箇所はRecordinatorのTask `8a96ef4f-2e5e-5a58-be66-08d8e485c523` へ1行残す。本人が Editor を開いている間のコード編集と確認は写しで行い、1回で戻す。
3. 本人の判断待ち（SP-021 9節）：地点のローカル表示の方式（既存の構造物経路を使うか）、地域の規模と解像度。決まるまで段5・6へは進まない。
4. 回答を待たずに進める作業：無い（雲・待ち時間・十字は出した。探索画面の見た目・文言・サーチの操作・発見時の出方は方向の回答まで作り直さない）。

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

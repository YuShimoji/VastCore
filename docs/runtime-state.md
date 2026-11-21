# VastCore Runtime State

Last Updated: 2026-11-21

## Current Position

| Field | Value |
|---|---|
| Project | VastCore Terrain Engine |
| Branch | `codex/vc-development-readiness-20260711` |
| Integrated baseline | `515a73b`（SP-021の最初の開発区切り）の上に、衛星写真風の作り直しを追加 |
| Active outcome | seedから地域一帯の鳥瞰図を自動で作り、その上で座標を指定してサーチし隠れた地点を発見する探索を動かす（SP-021 段1〜4） |
| Active artifact | `docs/02_design/REGION_MAP_EXPLORATION_SPEC.md` |
| Next mission | 作り直した版への本人の受入（Recordinatorの問い1件）と、SP-021 9節の判断 |
| Current bottleneck | 描き方の方向は衛星写真風に決まり、その版を作り直した。作り直した版の採否と、次に直す箇所（地形・色・川・海・雲・探索画面のどこか）が本人未判断。探索画面はマウスとキーボードで触った確認が無い |
| Change relation | 方向転換（2026-11-21、本人依頼）。SP-017のArch往復検証から、地域鳥瞰図と探索の土台へ主作業を移した。同日、鳥瞰図の描き方は本人が「衛星写真風」と答えた |

目的は [SSOT_WORLD](SSOT_WORLD.md) 1節と1.1節。目標アンカー：

- 開発目標：seedから地域一帯の詳細な鳥瞰図を自動で作り、その上にポイントクリック的な宝探しの探索ゲームを載せられる。
- 今回の増分：本人が衛星写真風に作り直した鳥瞰図（前の版との比較つき）と探索画面を見て、「近いか」「何が気になるか」を言える。
- 現在のボトルネック：上の表の Current bottleneck。

全体工程（段1〜6）と現在地の表は [SP-021](02_design/REGION_MAP_EXPLORATION_SPEC.md) 4節が持つ。SP-017 / SP-020（Arch・Prefab Stamp・DualGridの経路）は未実施のまま保留で、2026-11-21再開準備時の状態本文は [再開履歴](restart/VC_RESTART_20261101_HISTORY.md) の「2026-11-21 thank版（SP-017再開準備）」へ原文のまま移した。

## 作業ツリーと端末の現物（2026-11-21 thank観測）

- SP-021で追加したもの：`Assets/Scripts/RegionMap/`（Core・View・Editor、asmdef 2件）、`Assets/Scenes/RegionExplorer.unity`、`Assets/Tests/EditMode/RegionMapTests.cs`、`tools/RegionMapPreview/`、`scripts/region-map.ps1`。衛星写真風の作り直しで変えたのは `Core/` の5ファイル（FlowRouter・RegionGenerator・RegionMapRenderer・RegionNoise・RegionSpec）と確認用コンソールの引数。Terrain・StructureGenerator・Stamp・`ProjectSettings/`・`Packages/` は変更していない。
- 未commitのまま保持するもの（前から同じ）：`.serena/project.yml`、untrackedの `Assets/Resources/Stamps/`（旧失敗候補）とそのmeta。どれも上書き・削除しない。
- 生成物は ignored の `artifacts/region-map/`（鳥瞰図PNG、探索画面の画像、確認ページ `review-20261121-r2/`、調整中の書き出し `work-r2/`）と `artifacts/logs/region-map-*.log`。thank端末にだけある。
- Unityのバッチ起動は、Unity Hubが起動していないと `No valid Unity Editor license found`（exit 198）で失敗する。Hubを起動すると通った。
- 残しているローカル限定の履歴（`master`、`feature/mcp-verification`、stash 2件）とworktree `.codex-tmp/cloud-codex-20261011/VastCore` は再開準備時のまま。

## Current Trust Assessment

- thank / Unity 6000.3.6f1 / 2026-11-21（作り直し後）：`scripts/region-map.ps1 -Mode setup` がexit 0（コンパイルエラー0、Unity経由のPNG書き出し。Editor内で生成23.4秒・描画3.1秒。作り直し前は19.6秒・11.1秒）。`scripts/run-tests.ps1 -TestMode editmode` が610/610（既存603＋`RegionMapTests` 7）、失敗/skip 0。PlayModeは実行していない。
- thank 通常Editor（2026-11-21、作り直し後）：`scripts/region-map.ps1 -Mode explore` で探索シーンを1920×1080で再生し、開始画面、6離れた位置のサーチ（Strong）、地点の位置のサーチ（Found、一覧へ追加）、描き方の切替を画像に残した（PASS、発見1/18）。操作はスクリプトから公開APIを呼んだもので、マウスとキーボードでの操作は未確認。発見の通知の帯が地図の南端に重なる。
- thank / .NET 9（Unity非依存）：`tools/RegionMapPreview` で seed 3・7・21・44 の衛星写真風を書き出し、開発担当が目で確かめた（1024²、生成約8〜12秒、描画約1秒）。seed 7 は地図帳風・陰影図も書き出した。同じソースの `RegionMapTests` 7件は `dotnet test` でも合格。最高標高は .NET とUnityで同じ2887m。
- 山地の平行な溝は、同じseedで「傾きの上限なし」「斜面拡散5倍」「流れ先のゆらぎあり」を比べ、ゆらぎだけで消えた（原因は8方向の流れの直線化）。作り直した版の見え方と、探索画面の配置・操作感に対する本人のaccept/reviseは未記録。受入の問いはRecordinatorへ送った（ID は Next Action）。
- 2026-11-01の `af36a0e`（Arch・Export・配置の修正）に対するcompile・EditMode 603/603・PlayMode 9/9は当時の結果で、そのコードは今回変更していない。修正版の通常GUI往復（SP-017）は未実施のまま。
- 未確認：マウス・キーボードでの探索操作、確認ページをブラウザで開いた時の画像の表示、Windows以外でのフォント表示、1024²を超える解像度、ビルドしたPlayerでの生成時間、PLANNER007でのSP-021のcompile/tests、残る見た目の制限（SP-021 8節：湖、平野の薄い筋）の解消。

## Next Action

次の作業は、本人の受入の回答から決める。SP-017のArch往復検証を自動で次の作業にしない。

1. 本人：作り直した版の確認ページと探索画面を見て、Recordinatorの問い `01M7Z6YNHPZKBMGY0Z5429WE71`（気になる所。Project `pc96190c8a9`、添付Artifact `01M7Z6X3E3EGXCJESFEZF1Q91G`）に答える。Unityで触る場合は `Vastcore > Region Map > Open Explorer Scene` を開いて再生する。前の問い2件は、描き方の方向 `01M7YC8G7GRZH29KJ9Q3P9GS1D` を取り下げ（チャットで「衛星写真風」と回答済み）、最初の版の受入 `01M7YC8J5ABRH8JYHYQEAE9V9C` を新しい問いへ置き換えた。
2. 開発担当：回答を読み、重大度の高い指摘から直す。使った回答IDと変えた箇所はRecordinatorのTask `8a96ef4f-2e5e-5a58-be66-08d8e485c523` へ1行残す。
3. 本人の判断待ち（SP-021 9節）：地点のローカル表示の方式（既存の構造物経路を使うか）、地域の規模と解像度。決まるまで段5・6へは進まない。
4. 回答が無い間に進められる作業：マウス・キーボードでの探索操作の実画面確認、発見の通知の帯が地図の南端に重なる点の修正、Editor内の生成時間（約23秒）の短縮。どれも受入の回答に依存しない。

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

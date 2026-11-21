# VastCore Runtime State

Last Updated: 2026-11-21

## Current Position

| Field | Value |
|---|---|
| Project | VastCore Terrain Engine |
| Branch | `codex/vc-development-readiness-20260711` |
| Integrated baseline | `1a2cfc2`（2026-11-21の再開準備）の上に、SP-021の最初の開発区切りを追加 |
| Active outcome | seedから地域一帯の鳥瞰図を自動で作り、その上で座標を指定してサーチし隠れた地点を発見する探索を動かす（SP-021 段1〜4） |
| Active artifact | `docs/02_design/REGION_MAP_EXPLORATION_SPEC.md` |
| Next mission | 本人の受入（鳥瞰図の描き方、探索画面の配置と操作感）と、SP-021 9節の判断 |
| Current bottleneck | 段1〜4は動くが、見え方と操作感の採否が本人未判断。次に磨く向き（描き方、既知の制限のどれを先に直すか、地点のローカル表示の方式）が決まっていない |
| Change relation | 方向転換（2026-11-21、本人依頼）。SP-017のArch往復検証から、地域鳥瞰図と探索の土台へ主作業を移した |

目的は [SSOT_WORLD](SSOT_WORLD.md) 1節と1.1節。目標アンカー：

- 開発目標：seedから地域一帯の詳細な鳥瞰図を自動で作り、その上にポイントクリック的な宝探しの探索ゲームを載せられる。
- 今回の増分：本人が鳥瞰図の画像と探索画面を見て触り、「どの描き方が近いか」「何が気になるか」を言える。
- 現在のボトルネック：上の表の Current bottleneck。

全体工程（段1〜6）と現在地の表は [SP-021](02_design/REGION_MAP_EXPLORATION_SPEC.md) 4節が持つ。SP-017 / SP-020（Arch・Prefab Stamp・DualGridの経路）は未実施のまま保留で、2026-11-21再開準備時の状態本文は [再開履歴](restart/VC_RESTART_20261101_HISTORY.md) の「2026-11-21 thank版（SP-017再開準備）」へ原文のまま移した。

## 作業ツリーと端末の現物（2026-11-21 thank観測）

- 追加：`Assets/Scripts/RegionMap/`（Core・View・Editor、asmdef 2件）、`Assets/Scenes/RegionExplorer.unity`、`Assets/Tests/EditMode/RegionMapTests.cs`、`tools/RegionMapPreview/`、`scripts/region-map.ps1`。既存の変更は `Vastcore.Tests.EditMode.asmdef` への参照1行と `.gitignore` の例外1行。Terrain・StructureGenerator・Stamp・`ProjectSettings/`・`Packages/` は変更していない。
- 未commitのまま保持するもの（前から同じ）：`.serena/project.yml`、untrackedの `Assets/Resources/Stamps/`（旧失敗候補）とそのmeta。どれも上書き・削除しない。
- 生成物は ignored の `artifacts/region-map/`（鳥瞰図PNG、探索画面の画像）と `artifacts/logs/region-map-*.log`。thank端末にだけある。
- Unityのバッチ起動は、Unity Hubが起動していないと `No valid Unity Editor license found`（exit 198）で失敗する。Hubを起動すると通った。
- 残しているローカル限定の履歴（`master`、`feature/mcp-verification`、stash 2件）とworktree `.codex-tmp/cloud-codex-20261011/VastCore` は再開準備時のまま。

## Current Trust Assessment

- thank / Unity 6000.3.6f1 / 2026-11-21（SP-021）：`scripts/region-map.ps1 -Mode setup` がexit 0（コンパイルエラー0、探索シーン作成、Unity経由のPNG書き出し。Editor内で生成19.6秒・描画11.1秒）。`scripts/run-tests.ps1 -TestMode editmode` が610/610（既存603＋`RegionMapTests` 7）、失敗/skip 0。PlayModeは今回実行していない。
- thank 通常Editor（2026-11-21）：`scripts/region-map.ps1 -Mode explore` で探索シーンを1920×1080で再生し、開始画面、6離れた位置のサーチ（Strong）、地点の位置のサーチ（Found、一覧へ追加）、描き方の切替を画像に残した（PASS、発見1/18）。操作はスクリプトから公開APIを呼んだもので、マウスとキーボードでの操作は未確認。
- thank / .NET 9（Unity非依存）：`tools/RegionMapPreview` で seed 3・7・21・44 の鳥瞰図を書き出した（1024²、生成約8秒、描画約2.7秒）。同じソースの `RegionMapTests` 7件は `dotnet test` でも合格。
- 見え方（地形の説得力、配色、雲）と探索画面の配置・操作感に対する本人のaccept/reviseは未記録。受入の問いはRecordinatorへ送った（ID は Next Action）。
- 2026-11-01の `af36a0e`（Arch・Export・配置の修正）に対するcompile・EditMode 603/603・PlayMode 9/9は当時の結果で、そのコードは今回変更していない。修正版の通常GUI往復（SP-017）は未実施のまま。
- 未確認：マウス・キーボードでの探索操作、Windows以外でのフォント表示、1024²を超える解像度、ビルドしたPlayerでの生成時間、PLANNER007でのSP-021のcompile/tests、既知の見た目の制限（SP-021 8節）の解消。

## Next Action

次の作業は、本人の受入の回答から決める。SP-017のArch往復検証を自動で次の作業にしない。

1. 本人：鳥瞰図の画像と探索画面を見て、Recordinatorの問い2件（描き方の方向 `01M7YC8G7GRZH29KJ9Q3P9GS1D`、最初の版の受入 `01M7YC8J5ABRH8JYHYQEAE9V9C`。Project `pc96190c8a9`、添付Artifact `01M7YC786HSEQVJQ7738MP9G4C`）に答える。Unityで触る場合は `Vastcore > Region Map > Open Explorer Scene` を開いて再生する。
2. 開発担当：回答を読み、重大度の高い指摘から直す。描き方の方向が決まったら、その様式の配色・陰影・雲と、SP-021 8節の制限（山地の平行な溝、海の格子模様、湖）を直す順を決める。
3. 本人の判断待ち（SP-021 9節）：地点のローカル表示の方式（既存の構造物経路を使うか）、地域の規模と解像度。決まるまで段5・6へは進まない。
4. 回答が無い間に進められる作業：マウス・キーボードでの探索操作の実画面確認、Editor内の生成時間（約20秒）の短縮。どちらも描き方の選択に依存しない。

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

# VastCore Runtime State

Last Updated: 2026-11-21

## Current Position

| Field | Value |
|---|---|
| Project | VastCore Terrain Engine |
| Branch | `codex/vc-development-readiness-20260711` |
| Integrated baseline | thank系列 `accd587` → `af36a0e`（Arch・Export・配置の限定修正）→ `e810a50`（2026-11-01文書）と、PLANNER007の `de4d515`（2026-11-16、runtime-stateのみ）を2026-11-21に統合 |
| Active outcome | 修正版Archを通常GUIで編集元Scene → Prefab Stamp → 再読込 → 原点以外の一チャンク地形へ同条件で再配置できるかを判定する（SP-017の技術出口） |
| Active artifact | `docs/02_design/STAMP_EXPORT_PIPELINE_SPEC.md` |
| Next mission | 同仕様「2026-11-21 次回集中セッションの実行用依頼」（未発行） |
| Current bottleneck | 修正コードはcompile・EditMode 603/603・PlayMode 9/9まで成功。通常GUIでの修正版Export・地形配置・再読込往復が未実施。11-01の検証Editor PID38072は前面化されないまま、11-20のPC再起動で正常終了せずに消えた |
| Change relation | 再開準備（2026-11-21、資料整理のみ）。11-01の修正から続く同じ技術出口 |

目的は [SSOT_WORLD](SSOT_WORLD.md) の「広大な景観に映える、ユニークで巨大な人工構造物をプロシージャルに生成する」。DualGrid + HeightMap + designer Prefab Stamp、T1、V4を引き継ぐ。全工程はSP-020、最初の検証と依頼文はSP-017が持つ。2026-11-01と2026-11-16の状態本文は [再開履歴](restart/VC_RESTART_20261101_HISTORY.md) へ原文のまま移した。

## 再開時の現物（2026-11-21 thank観測）

- Unityプロセスなし。`Temp/UnityLockfile`（2026-11-01 04:59）が残っている。Editor log `interactive-editor-v2.log` は2026-11-02 10:19のLicensing/UPM IPC切断で終わり、shutdownの記録がない。11-01 05:09以降に更新されたAssets・docs・Packages・ProjectSettingsのファイルは0件で、未保存作業の喪失は観測していない。PCの起動は2026-11-20 14:12。
- 保存済みの編集元は `Assets/Data/VastCore/RestartValidation/20261101-0430-arch-fix/RestartValidation_20261101_0430_Arch.unity`（Export前のProBuilder形状）。旧失敗候補は `Assets/Resources/Stamps/Prefabs/RestartValidation_20261101_0430_Arch.prefab` と同名StampDef。2026-11-21に本人の選択で、`Assets/Data/VastCore/RestartValidation/` の検証Scene 2件と、`DesignerSessions/RestartValidation_20261101_0328_sp017.asset` をcommitした（Stampsを参照しない）。Stamps（旧失敗候補）は、buildに含まれる `Resources` 配下にあるためuntrackedのまま、thank端末にだけある。どれも上書き・削除しない。
- 証拠はignoredの `artifacts/restart-20261101/`（run別の `RESULT.md`、`fixture-plan.md`）にあり、thank端末にだけある。
- 未commitのまま保持するもの：`.serena/project.yml`（11-01開始前からの差分。Serenaの設定形式の更新で `languages: [csharp]` が消え、空の `language_servers:` が入っている。所有不明）、上記のStamps（untracked）。
- 2026-11-21の整理：中身の無いAssetsフォルダ6件（`_Scripts`、`Docs`、`Documentation`、`Adaptive Performance`、`TerrainPresets`、`TerrainPresets 1`）とそのmetaを削除した（GUID参照0件。`TerrainPresets` は使用時に `TerrainPresetManager` が再作成する）。HEADへmerge済みのローカルbranch 5件を削除した。UIスキャンの再生成レポートはcommitした。
- 残しているローカル限定の履歴：`master`（origin/masterに無い2025-10のcommit 28件）、`feature/mcp-verification`（commit 1件）、stash 2件（2026-02）。どれもリモートに無いので消していない。
- worktreeは当checkoutと `.codex-tmp/cloud-codex-20261011/VastCore`。直近4日のCodex sessionに、このcheckoutへの書込みは見つからない。

## Current Trust Assessment

- thank / Unity 6000.3.6f1 / `af36a0e`（2026-11-01）：compile exit 0、EditMode 603/603、PlayMode 9/9、失敗/skip 0。追加回帰7件（Arch 3、Exporter 2、配置 2）。証拠は `artifacts/restart-20261101/20261101-0430-arch-fix/final-gates/`。
- thank 通常GUI（2026-11-01）：修正後のArch生成に成功（68頂点、Object Size(15,20,25)）。修正前のExporterで元Mesh消失を観測し、修正した。修正版での通常GUI Exportは未実施。
- thank Cockpit（2026-11-01、run `20261101-0328-sp017`）：無選択でApply不可、新sessionのSave/New/Load、一件のApply/Undoと未選択対象の不変を確認。レイアウト・操作感への本人のaccept/reviseは未記録。
- PLANNER007 / `4c7f9fa`（2026-11-16）：71 packagesを登録、compile成功、EditMode 596/596、PlayMode 9/9、LFS 210 objectsが `git lfs fsck` 成功。8月のUPM `path undefined / No packages loaded` は再発しなかった。ログは同端末の `artifacts/logs/resume-20261116/`。`af36a0e` の修正はこの確認に含まれない。
- thankの通常GUIでは2026-11-01にUPM `The "path" argument must be of type string. Received undefined` / offline package-list errorを観測した。登録済みpackageでのcompile/testsとは別の事象で、修復していない。
- 未確認：修正版GUI ExportのMesh/Material参照がEditor再起動後も有効か、Terrain SO一チャンク配置のGUI結果、手編集の保存、Scene再読込・Editor再起動後の同条件再配置、URPでの表示（空パレットのProBuilderDefaultはマゼンタ）、PLANNER007での `af36a0e` のcompile/tests、本人による形状・景観・操作感の採用。
- 8月の端末別UPM障害は `docs/04_reports/REPORT_DEVELOPMENT_RESUME_CHECKOUT_AUDIT_2026-08-12.md` の履歴。現在の端末の成功・障害へ転記しない。

## Next Action

次回の集中セッションは、SP-017「2026-11-21 次回集中セッションの実行用依頼」を本人が発行して始める。2026-11-21に本人がSP-017をCockpit受入より先にすると選んだ。

1. 実行端末を確かめる。どの端末でも、commit済みの編集元Scene `RestartValidation_20261101_0430_Arch.unity` を使う。PLANNER007など別端末では `git pull --ff-only` の後に、`af36a0e` 以降のcompile・EditMode・PlayModeを一度ずつ実行する。編集元が開けない場合だけ、SP-017に転記した固定入力からArchを新規生成する。
2. Unityプロセスが無いことを確かめる。古いlockfileは削除せず、Unity 6000.3.6f1の起動に任せる。起動後のConsoleとdirty状態を記録する。
3. 編集元を `_MeshFix` 付きの別Sceneへ保存し、Archを `_MeshFix` 候補名でExportする。元Meshの保持と、Prefab・Mesh・Materialの参照を確認する。
4. 新しいTerrain SOとBootstrap（worldOrigin(100,200)、grid 1×1、cell0）で一チャンクに配置し、事前の期待XZ(130.5927, 231.4375)と照合する。
5. 元Archをx=2へ手編集して保存し、Scene再読込とEditor再起動の後、新しいPlayで同条件に再配置して比較する。
6. 結果を項目別のPASS/FAILでSP-017・SP-020へ返す。形状・素材（マゼンタ表示）・景観の採否は本人が決める。
7. Cockpitのレイアウト・操作感のaccept/revise（PLANNER007版の次の一手1・2）は本人の判断として別に記録する。main統合とdefault branchの整列（同3）は、Cockpit受入とSP-017技術出口の後の候補で、未採用。

過去のキャッシュ・ACL・ネットワーク診断をやり直さない。新しい失敗が出たときだけ対象を限定して切り分け、証拠と未実施範囲を残す。

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

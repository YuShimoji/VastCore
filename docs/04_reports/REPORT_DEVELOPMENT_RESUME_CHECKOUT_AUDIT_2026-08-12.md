# VastCore 保存 checkout 開発再開監査 — 2026-08-12

対象読者: ユーザー、監修AI、次の開発AI

対象 checkout: `C:/Users/PLANNER007/VastCore/VastCore`

remote: `origin` -> `https://github.com/YuShimoji/VastCore.git`

## 結論

保存されていた checkout は、既存資産を壊さずに現在の remote handoff へ復元できた。
開始時の `codex/vc-ai-workflow-refresh-20260710` / `f145df6b1866` は upstream と
0 ahead / 0 behind だったが、fetch 後に、その厳密な2コミット子孫である
`origin/codex/vc-development-readiness-20260711` / `fbf8c9ae994a` を確認した。
子孫側の `docs/runtime-state.md` は同枝を current remote handoff と明記していたため、
clean checkout から同 remote 枝を追跡するローカル枝へ切り替えた。最終状態も
0 ahead / 0 behind である。

Git、LFS、正本構造、manifest/lockfile の静的整合は再確認できた。一方、この
Windows profile では Unity 6000.3.6f1 の Package Manager が C# コンパイル前に
`The "path" argument must be of type string. Received undefined. No packages loaded.`
で停止した。したがって、2026-07-11 の別 checkout / machine で記録された
EditMode 596 / 596、PlayMode 9 / 9 は同一 commit の重要な履歴証拠ではあるが、
今回のローカル成功ではない。現在の最短 bottleneck は製品コードではなく、
PLANNER007 profile の Unity Editor / embedded UPM 環境である。

Designer Cockpit の in-Editor レイアウト、Random Variation の Apply/Undo、
session の Save/New/Load は未受入のまま。そのため Git 同期成功を Unity受入へ、
過去の別端末テスト成功をこの端末の成功へ、ローカル検証を Drive / Sheets の
実データ受入へ外挿しない。

## 同期前後の checkout

| 判断点 | 同期前 | fetch・handoff解決後 | 判断 |
|---|---|---|---|
| 実パス | `C:/Users/PLANNER007/VastCore/VastCore` | 同じ | repo外へ移動していない |
| branch / HEAD | `codex/vc-ai-workflow-refresh-20260710` / `f145df6b1866f72da043be8244f99bbd4f62b95a` | `codex/vc-development-readiness-20260711` / `fbf8c9ae994adbf78e020dcdcfd2f130cd3c9621` | 最新 remote handoff を tracking branch として復元 |
| upstream parity | `origin/codex/vc-ai-workflow-refresh-20260710` と 0 ahead / 0 behind | `origin/codex/vc-development-readiness-20260711` と 0 ahead / 0 behind | 未push commit、未取得 commit、分岐なし |
| 履歴関係 | `f145df6` | `e14a0db` -> `fbf8c9a` | 元 HEAD は最終 HEAD の厳密な祖先。merge/rebase/cherry-pick 不要 |
| remote default | `origin/HEAD` は `origin/master` | 同じ | `master` は現成果より古く、入口正常化は未実施 |
| tracked / staged / untracked | 0 / 0 / 0 | 報告作成前は 0 / 0 / 0 | ユーザー差分の退避・上書きなし |

新しい2コミットのうち `e14a0db` は test discovery、logger、Unity 6 test API、
PlayMode asmdef、Slider UI生成の狭い修復、`fbf8c9a` は current state と長期目標の
handoff である。`Packages/` と `ProjectSettings/` はこの2コミットでも今回作業でも
変更していない。

## 2026-08-11 / 2026-08-12 更新証拠の判定

fetch 後の全 ref を `2026-08-10T00:00:00+09:00` 以降で検索したが、Git commit は
0件だった。remote の最も新しい commit は `fbf8c9a`、commit date は
2026-07-11 03:44:53 +09:00 である。

2026-08-12 の tracked filesystem mtime を持つファイルは15件あったが、すべて
旧枝から最新 handoff 枝へ切り替えた時刻に集中し、worktree blob と `HEAD` blob が
完全一致した。対象は `e14a0db..fbf8c9a` が持つ11件の Unity code/test/asmdef、
`docs/02_design/ASSEMBLY_ARCHITECTURE.md`、本報告より前の active report、
`docs/project-context.md`、`docs/runtime-state.md` である。これは checkout による
materialization の証拠であり、8月11–12日に新しい内容が作られた証拠ではない。

したがって今回優先した実体ある更新は、mtime ではなく、2026-07-11 の2コミットが
導入した test-gate 修復と remote handoff である。

## 保護したローカル状態

| 保護対象 | 実測 | 今回の扱い |
|---|---:|---|
| tracked / staged / untracked | 開始時 0 / 0 / 0 | stash、restore、reset、rebase、clean を使わず保持 |
| ignored roots | 30 | `Library/`、`Temp/`、`Logs/`、`UserSettings/`、生成 `.csproj`、`artifacts/`、`build/`、私有/生成asset rootを削除・移動しない |
| stashes | 5 | 一覧確認のみ。apply/drop/pop なし |
| 別 worktree | 2 | `VastCore-origin-main-compile` と `VastCore-origin-main-parent-20260625` を変更しない |
| LFS | 210 / 210 materialized、pointer-only 0 | fetch/checkout 後も全実体を保持 |
| submodule | 0 | 追加・初期化なし |
| 他 Unity project | 別プロジェクトの Editor/worker が稼働 | 終了、再起動、設定変更、global Unity state 変更なし |

今回新規に作った ignored 証拠は
`artifacts/resume-20260812-019ff3b6/logs/compile-check.log` のみで、既存
`artifacts/logs/compile-check.log` は上書きしていない。前者は現在の blocker の
再現証拠なので保持する。VastCore に紐づく batch Unity process は終了済みである。

## 開発環境の復元と検証

`ProjectSettings/ProjectVersion.txt` が要求する Unity `6000.3.6f1` は
`C:/Program Files/Unity/Hub/Editor/6000.3.6f1/Editor/Unity.exe` に実在する。
PowerShell は 7.6.4、Git は 2.50.1.windows.1、Node は 22.19.0。

`Packages/manifest.json` の直接依存52件は `Packages/packages-lock.json` の
71エントリにすべて対応し、missing direct dependency は0件だった。既存
`Library/PackageCache` も存在する。ただし package cache の存在は package resolution
成功の代替証拠ではない。今回の Unity 起動は lockfile に従う通常の同期経路へ入ったが、
UPM内部エラーで package load 前に止まった。manifest と lockfile の SHA-256 は実行前後で
それぞれ `20EB6B...56C0EB2`、`9CF7C0...FAC15C7F` のまま一致し、依存追加、upgrade、
manifest/lockfile書換えはない。

| Gate | 今回の結果 | 何を証明したか / していないか |
|---|---|---|
| `scripts/check-project-state.ps1` | pass | branch、active artifact、canonical docs の構造は有効 |
| manifest / lock JSON整合 | pass | 52直接依存が71 lock entriesに含まれる。実際のUPM load成功ではない |
| Unity batch compile | fail、exit 1 | license初期化後、Package Managerが `path undefined / No packages loaded`。C#診断へ未到達 |
| EditMode / PlayMode | 今回は未実行 | compileのpackage-resolution前提が成立しないため。同一commitの別端末履歴は596/596と9/9 |
| `git diff --check` | pass | 同期後 checkout に whitespace errorなし |
| Git LFS | pass | 210 / 210 materialized、push/commit対象なし |
| tracked status | 検証後も clean | Unity試行は tracked fileを変更していない |

この checkout に残っていた `artifacts/test-results/` は 2026-03-06 の
EditMode 427 / 427 と PlayMode 3 / 3 であり、現在 HEAD の test-gate証拠には使えない。
同様に既存 `artifacts/logs/compile-check.log` は 2026-07-10 の同じ UPM失敗である。
今回の結果と整合するが、新しい test acceptance ではない。

## ローカル Git、Drive、Sheets の受入境界

| 境界 | 今回確認したこと | 現在状態 | 未実施の受入 |
|---|---|---|---|
| ローカル Git | remote fetch、正当なhandoff枝解決、0/0 parity、LFS、正本構造、status | Git再開可能 | local docs差分のreview/commit。pushは未承認 |
| ローカル Unity | Editor/version/lockfile存在、headless compile試行 | UPMで環境blocked | package resolution、C# compile、596 EditMode、9 PlayMode、Cockpit smoke |
| Google Drive | current state、active report、Cockpit checklist、project-contextに具体的なDrive名/ID/linkなし | 照合対象を特定できない | 根拠付き対象が提示された場合のみread-only照合 |
| Google Sheets | current handoffに具体的なspreadsheet名/ID/rangeなし | 実データ受入は対象化されていない | 実sheet readback、行/列/ID単位の受入。書込みは今回非承認 |

Drive全体の探索は行っていない。Drive / Sheets の書込み、共有変更、公開、配布も
行っていない。

## 現在の製品・開発位置

非交渉のNorth Starは「広大な景観に映える、ユニークで巨大な人工構造物を
プロシージャルに生成する」こと。主経路は
`Designer Session -> StructureGenerator -> StampExporter -> BuildingDefinition /
variation / material -> DualGrid + HeightMap placement -> scene/performance evidence`。

確定しているのは、現 remote tip に Cockpit UX、test discovery修復、loggerの
EditMode安全化、PlayMode discovery復旧、Unity 6 test API整合、Slider UI生成修復が
含まれること。2026-07-11 の別 checkout では同 commit に対して compile、596 EditMode、
9 PlayMode が成功した。未確定なのは、この PLANNER007 profile で同 gate を再現できるか、
Designer Cockpit が実画面で受け入れ可能か、代表構造物が全主経路を通るかである。

現状の主機能見積りは既存 active report の仮説を維持する。SP-010 Prefab Stamp 90%、
SP-017 Stamp Export 75%、SP-018 Parametric Variation V1 85%、SP-019 Building Definition
done、SP-020 Designer Pipeline 60%。これらは仕様・実装量の目安であり、Unity実機受入率
ではない。新しい横機能より、まずこの checkout のUnity gateを回復し、Cockpit G0と
代表構造物1件のend-to-end G2を証拠化する価値が高い。

3D Voxel / Marching Cubes を主経路へ戻すにはユーザーの明示的な再承認が必要。
Marching SquaresもPrefab Stamp方針との衝突を解かずに主経路へ戻さない。
`Packages/`、`ProjectSettings/`、global Unity環境、DB/認証/API/serialization contract、
大規模visual productionは、現在の環境監査から自動的に触れてよい範囲ではない。

## 次の一手からNorth Starまで

| 到達点 | 解くボトルネック | 必要条件 | 完了条件 | その先に可能になること |
|---|---|---|---|---|
| E0 PLANNER007 Unity環境回復 | UPMがpackage load前に停止 | 他Unity projectの安全な終了、global環境変更の明示承認、または既知の正常host | clean controlとVastCoreの両方でpackages登録、compile exit 0 | current hostでテストとCockpitを信頼して回せる |
| G0 Cockpit Acceptance | 製品成果がEditor未受入 | E0、`docs/DESIGNER_COCKPIT_SMOKE_TEST.md`、代表scene object | layout確認、Apply/Undo、Save/New/Load、screenshot、accept/revise | Cockpit枝を製品成果として判断できる |
| G1 Remote Baseline | GitHub default `master` がobsolete | G0受入、maintainer権限、PR/branch保護設計 | accepted tipを`main`へ統合しdefault/Pulse/Actions/protectionを一致 | 外部入口と実際のcurrent lineが一致 |
| G2 Phase D V1 End-to-End | 部品は多いが全経路の実物証拠なし | G0、固定seed、代表scene/prefab | 代表巨大構造物1件がSessionからDualGrid配置・scene evidenceまで通る | SP-010/017/018を実物で閉じられる |
| G3-G4 Evidence / Controlled Random | 証拠回収と候補比較の摩擦 | G2で証拠形式とseed契約を確定、UX方向選択 | Overview+Diagnostics proof、非破壊3候補preview | 反復検証と創造探索がCockpit内で短くなる |
| G5-G8 Production path | 多様性、Composition、性能、配布品質 | G2-G4、代表scene、作品性判断 | recipe/placement、Union/Blend/LOD、性能budget、CI/a11y/i18n/release gate | 再現可能で保守・配布できる生成基盤になる |
| G9 North Star | 景観の中で巨大人工構造物が作品価値を持つ | 構造物主経路と性能budgetの安定 | デザイナーが一つのsessionから生成・比較・採用・配置し、証拠と性能をCockpitで確認 | ClimateかEcosystemを価値検証し、景観増幅へ進める |

E0の最小で非重複な解消案は、既存の長いUPM診断を再演することではない。
PLANNER007上の他Unity作業を安全に閉じられる時間を確保し、明示承認後に
Unity Hubの6000.3.6f1 repair/reinstall、またはpassing hostとのEditor/embedded UPM
整合比較を行い、まずclean controlで判定する。controlが通ってからのみVastCoreの
compile -> EditMode -> PlayMode -> Cockpitへ戻る。

## 次に開く入口

| 入口 | 減らす摩擦 | 選ぶための条件 | 選ぶと次に可能になること |
|---|---|---|---|
| **Advance — E0環境回復**（推奨） | package load前停止を除き、current checkoutで機械検証を回復 | 他Unity projectを安全に閉じ、Editor/UPM repairまたは正常host比較を明示承認 | compile、596+9 test、Cockpit G0を同じ端末証拠として再開 |
| **Verify — known-good host** | このprofile修復を待たず、commit自体とCockpitを検証 | 2026-07-11にpassしたhost/check-outへアクセス可能 | canonical scripts再実行とCockpit手動受入。PLANNER007の不具合は別境界として残せる |
| **Audit — remote baseline readiness** | obsolete `master`、PR/Pulse/Actionsの移管判断を準備 | read-only GitHub監査の明示対象、G0を未完のまま変更しないこと | G0後に一度でmain/default/protectionを揃える実行計画ができる |
| **Excise — current docs整合** | 古いSSOT/statusがcurrentを誤認させる | reference graph監査、G0/E0を製品進捗として水増ししない | active ownerが減り、次の監修AIの再開コストを下げる |

現在の推奨は Advance だが、global Unity環境のrepair/reinstallは今回の包括承認には
含まれない。今回の作業は、破壊的またはglobalな変更の直前で止め、再現ログ、正確な
依存条件、次の判定順を残したところまでで完了とする。

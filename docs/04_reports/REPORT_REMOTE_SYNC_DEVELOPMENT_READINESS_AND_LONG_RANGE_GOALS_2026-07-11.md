# VastCore リモート同期・開発準備・長期目標提案

更新日: 2026-07-11 (JST)

対象読者: ユーザー、監修AI、次の開発AI

## 結論

VastCore は、現在のWindows端末上では **Git同期、Unity C#コンパイル、
EditModeテスト、PlayModeテストまで再開可能** になった。開始時の作業枝
`codex/vc-rst-2e-upm-root-cause` は追跡先と一致していたが、fetch後に見つかった
`origin/codex/vc-ai-workflow-refresh-20260710` がその6コミット先の直系後継であり、
Designer Cockpit UX と最新のAI運用刷新をすべて内包していた。そこを最新統合基準
として取得し、既存draft PR #49の「運用差分のみ」というレビュー境界を守るため、
開発準備修復は専用枝 `codex/vc-development-readiness-20260711` に分離した。

修復コミットは `e14a0dbbfc105bfbb258945bfa4afbb803603a97`。最終差分での
検証結果は次のとおりである。

- Unity 6000.3.6f1 batch compile: 成功、`error CS` 0件。
- EditMode: 596 / 596 成功、failed 0、skipped 0。
- PlayMode: 9 / 9 成功、failed 0、skipped 0。
- Package Manager: 71パッケージを登録。以前の別端末で記録された
  `path undefined / No packages loaded` は現端末では再現しなかった。
- Git LFS: 210 / 210 オブジェクトが実体化済み。submoduleは存在しない。

ただし **Designer Cockpit自体のUnity Editor手動受入はまだ完了していない**。
次の製品ボトルネックは、新機能追加ではなく、レイアウト、Diagnostics、
Random VariationのApply/Undo、sessionのSave/New/Loadを一度の手動バッチで
確認して、Cockpitをaccept / reviseのどちらへ進めるか確定することである。

## 進捗イメージ

以下は仕様上の厳密な進捗率ではなく、次の意思決定までの残距離を示す監修用の
概算である。実装量とUnity acceptanceは分けて評価する。

- Repo / local toolchain readiness: `[█████████░] 90%`
  - ローカルcompileと両test suiteは緑。CI license、default branch、Pulse移管が残る。
- Designer Cockpit current slice: `[███████░░░] 70%`
  - 実装と自動検証は存在。Editorの見た目・操作・save/load受入が未完。
- North-star product path: `[█████░░░░░] 約50%`
  - Phase A/B/Cは上位SSOT上で完了、Phase Dは実装済み部品が多いがend-to-end証拠が
    なく、PD-1/PD-2/PD-3とPhase Eが残る。

## 1. リモート同期の事実

### 1.1 開始時

- repo: `C:\Users\thank\Storage\Game Projects\VastCore_TerrainEngine\VastCore`
- remote: `origin` -> `https://github.com/YuShimoji/VastCore.git`
- branch: `codex/vc-rst-2e-upm-root-cause`
- HEAD: `ccc682212777afe488b31c67a4c20bff22be0ce2`
- upstream parity: `0 ahead / 0 behind`
- worktree: clean

### 1.2 fetch後に確定した正当な継続線

履歴は分岐競合ではなく、次の完全な直系だった。

```text
ccc6822  Designer Cockpit MVP
  -> 55440cf  Cockpit UX diagnostics
  -> fd1cb29  Cockpit UX remote handoff
  -> 3f690c8  AI workflow and Project Pulse
  -> c6ff620  workflow hardening
  -> b9c928d  verification record
  -> f145df6  remote resume handoff
```

`ccc6822...f145df6 = 0 / 6` であり、mergeやcherry-pickは不要だった。
最新workflow枝は直前のUX枝を完全に含み、最新 `runtime-state` も
`f145df6` をremote resume入口として指定していた。

### 1.3 現在のブランチ境界

- integrated remote baseline: `f145df6`
- readiness repair commit: `e14a0db`
- active branch: `codex/vc-development-readiness-20260711`
- 旧workflow枝のdraft PR #49は変更せず、運用差分レビューとして保全した。
- 競合状態の旧draft PR #47、5件のstash、2件の別worktreeには触れていない。
- `Packages/`、`ProjectSettings/`、Unityインストール、他プロジェクトには触れていない。

GitHubのdefaultは現在も古い `master`。現枝は `origin/master` の353コミット先、
`origin/main` の18コミット先である。default branch変更はCockpitと今回の修復が
受け入れられてから行い、未受入枝を直接defaultへ昇格しない。

## 2. 開発可能性の修復内容

### 2.1 EditMode test assetの復旧

次のtracked `.meta` は追加時から `guid:` が空で、Unityが対応するC#資産を無視していた。

- `Assets/Tests/EditMode/AdjacencyRuleSetTests.cs.meta`
- `Assets/Tests/EditMode/StructurePlacementSolverTests.cs.meta`

重複しないUnity GUIDを付与した。これにより脱落していた21テストが再発見され、
21 / 21成功した。

### 2.2 VastcoreLoggerのEditMode安全化

`VastcoreLogger.Instance` は既存instanceを検索しても代入せず、新規GameObjectを作り、
EditModeでも `DontDestroyOnLoad` を呼んでいた。次を修正した。

- 検索した既存loggerを実際に `instance` へ再利用する。
- `DontDestroyOnLoad` は `Application.isPlaying` の時だけ呼ぶ。
- `Awake` 側も同じPlayMode境界に合わせる。

これによりEditor testからloggerを使っても、PlayMode専用APIによる例外を出さない。

### 2.3 logger移行後のテスト期待値整合

旧テストは生の `Debug.LogError(message)` と完全一致する文字列を期待していたが、
現行 `VastcoreLogger` は時刻・level・categoryを付け、Exception付きerrorは
`Debug.LogException` として保持する。製品ログ形式を弱めず、テストを次へ合わせた。

- 安定した本文を `Regex` で期待する。
- callback例外は `LogType.Exception` として期待する。

対象はCinematicCamera、HeightMapGenerator、ModernUIManager、
RealtimeUpdateSystem、TerrainGenerationConfigの既存EditModeテストである。

### 2.4 PlayMode discoveryとcompileの復旧

`Vastcore.Tests.PlayMode.asmdef` は `Editor` をexcludeしていたため、Editorベースの
PlayMode Test Runnerから3ファイル・9テストが0件に見えていた。アセンブリ正本へ
発見条件を先に記録し、platform除外だけを外した。依存参照は増やしていない。

発見後に露出した `Mathf.Pow(float)` -> `int` の暗黙変換3件は、整数の直径を整数乗算
する形へ修正した。obsoleteな `FindObjectsOfType` もUnity 6の
`FindObjectsByType(..., FindObjectsSortMode.None)` へ更新した。

### 2.5 PlayModeで露出したUI生成バグ

`SliderUIElement` は `Fill Area` と `Handle Slide Area` を通常のGameObjectとして
生成した直後に `RectTransform` を取得していた。UI要素として生成時から
`RectTransform` を持たせ、ModernUIManagerのPlayMode smokeで出ていた
`MissingComponentException` を解消した。

## 3. 検証証拠

### 3.1 実行コマンド

```powershell
.\scripts\check-project-state.ps1 -ExpectedBranch codex/vc-development-readiness-20260711
.\scripts\check-compile.ps1
.\scripts\run-tests.ps1 -TestMode editmode -RequireNonZeroTests
.\scripts\run-tests.ps1 -TestMode playmode -RequireNonZeroTests
git diff --check
git lfs status
```

### 3.2 最終結果

| Gate | Result | Meaning |
|---|---|---|
| Project state | pass | canonical files、branch、active artifactの構造が有効 |
| Unity compile | pass | UPM解決、C#、IL post-process、Tundra build、batch quit成功 |
| Compile errors | 0 | `error CS`なし |
| Invalid GUID warnings | 0 | 2 test assetsは正しくimport |
| EditMode | 596 / 596 pass | failed 0、skipped 0 |
| PlayMode | 9 / 9 pass | failed 0、skipped 0、zero-test false green解消 |
| `git diff --check` | pass | whitespace errorなし。CRLF予告のみ |
| LFS | clean | 210 / 210 materialized、pointer-only 0 |

`artifacts/logs/` と `artifacts/test-results/` はGit管理外の同一端末証拠である。
別端末やCIの成功を代弁しない。特にGitHub Actionsは `UNITY_LICENSE` 未設定のため、
Unity test jobがskipされる現状を「CI test成功」と解釈してはならない。

## 4. Acceptance audit

### 4.1 現スライスを製品受入と呼ぶ前のmust-fix

1. `docs/DESIGNER_COCKPIT_SMOKE_TEST.md` をUnity Editorで一度のバッチとして実行する。
2. top summary、primary actions、mode selector、mode panels、Diagnosticsを目視する。
3. Random Variationをselected objectsへApplyし、Undoでtransformが戻ることを確認する。
4. Save Session -> New Session -> Load Sessionで値が復元することを確認する。
5. screenshotと結果をCockpit evidenceへ記録し、accept / reviseを明示する。

Actorはsharedである。assistant/toolは手順、観測、記録を担当し、userはEditor上の
実機感と作品性を最終判断する。

### 4.2 今回は許容できるdebt

- CockpitのComposition、Deform、Terrainがstatus-onlyであること。
- Topology/DualGridがfuture slotであること。
- `com.unity.ai.generators` のdeprecated警告。
- `System.Runtime.CompilerServices.Unsafe.dll` のversion重複警告。
- CIの `UNITY_LICENSE` 未設定とUnity test skip。
- Actions checkoutでLFS materializationが明示されていないこと。

これらは隠さないが、Cockpit手動smokeを先送りしてまで同じスライスへ混ぜない。
Packages整理、CI license/LFS hardeningは別のboundary-changing sliceとする。

### 4.3 計画を誤らせるdocs debt

- `WORKFLOW_STATE_SSOT.md` はPhase D実装完了に近い表現を持つ一方、上位SSOTは
  Phase D進行中で、実機検証を未完としている。
- `BUILDING_DEFINITION_SPEC.md` のstatusと `spec-index.json` のdone/100%が不一致。
- `DESIGNER_PIPELINE_SPEC.md` はGAP未接続の古い記述を残すが、実装commitでは
  GAP-1/3/4/5が接続済み。
- `TASK_INDEX.md` はactive task 0で、現在のCockpit acceptance laneを表現しない。
- 古いmilestone/handover文書の一部はPhase C以前の状態をcurrentに見せる。

これは直近smokeのblockerではない。Cockpit受入後、参照graphを確認する独立した
Excise/SSOT整合スライスで、current ownerへ情報を昇格してから整理する。

## 5. 現在の製品位置

North starは「広大な景観に映える、ユニークで巨大な人工構造物をプロシージャルに
生成する」こと。現行主経路は次のとおりである。

```text
Designer Session
  -> StructureGenerator
  -> StampExporter
  -> BuildingDefinition / variation / material
  -> DualGrid + HeightMap placement
  -> scene evidence / performance evidence
```

3D Voxel / Marching Cubesを主経路へ戻すにはユーザーの明示的な再承認が必要。
Marching SquaresもPrefab Stamp方針との衝突を先に解決しない限り主経路へ戻さない。

主要実装の現在位置:

- SP-010 Prefab Stamp: 90%、Unity/Gizmo/variation接続の受入が残る。
- SP-017 Stamp Export: 75%、Prefab -> StampDef -> DualGridの実物証拠が残る。
- SP-018 Parametric Variation V1: 85%、Unity実機証拠が残る。
- SP-019 Building Definition: index上done/100%、実装commitも現HEADの祖先。
- SP-020 Designer Pipeline: 60%、接続実装と古い仕様本文の差を整理する必要がある。
- PD-1 Advanced Composition、PD-2 Controlled Random、PD-3 Performance: 0%。

したがって、次に新しい横機能を増やすより、代表的な巨大構造物1件を全経路へ通す
end-to-end proofの価値が最も高い。

## 6. 最長目標ラダー

最終到達像は、**デザイナーが一つの再現可能なsessionから巨大構造物を生成・比較・
採用し、DualGrid景観へ配置し、その証拠と性能状態までCockpitで確認できること**。

| Goal | 目的 | 効果 | 必要条件 | 現在状態 | Owner | 次のmove |
|---|---|---|---|---|---|---|
| G0 Cockpit Acceptance | 現UX候補を実物として判定 | 製品進捗をdocs中心からEditor成果へ戻す | 現branch、Unity Editor、smoke checklist | 自動gate緑、手動未実施 | shared | 一括smokeとscreenshot、accept/revise |
| G1 Remote Baseline | GitHub入口を実際の現在地へ統一 | `main`、Pulse、Actions、保護規則が同じ基準を指す | G0受入、PR review、maintainer権限 | defaultはobsolete `master`、PR #49 draft | user/maintainer + AI準備 | accepted tipを`main`へ、default/Pulse/protectionを同時移管 |
| G2 Phase D V1 End-to-End | 代表構造物1件を全4 Stageへ通す | SP-010/017/018を実物証拠で閉じる | G0、固定seed、代表scene/prefab | 部品実装は広いが統合証拠なし | AI + user作品性判断 | StructureGenerator -> Stamp -> Definition -> DualGrid proof |
| G3 Cockpit Evidence Console | compile/test/smoke/pipeline証拠を一画面化 | 検証反復と監修判断を短縮 | G2で実際の証拠形式が確定 | Diagnosticsは存在、Evidence Tileなし | AI design/implementation + user direction | Operations ConsoleのOverview + Diagnostics proof slice |
| G4 Controlled Random V1.5 | 再現可能な候補生成と比較 | 偶発性を保ちつつ採用結果を制御 | G2、seed契約、preview境界 | SP-018 V1は85%、PD-2は0% | AI + user美的判断 | preset/session保存、non-destructive 3候補preview |
| G5 Structure/Placement Diversity | 構造・配置レベルの多様性を増やす | cosmetic jitterを越えた巨大構造物群 | G4、Prefab Stamp主経路維持 | SP-013 todo、配置実装の祖先あり | AI | Tower recipe、Grammar最小核、zone/adjacency solver proof |
| G6 Advanced Composition | Union/Blend/LODで形状独自性を増やす | 複数meshから作品性の高い構造を作る | G5、CT-1 CSG実動作証拠 | PD-1 0%、一部legacy/partial carrierあり | AI + user作品性判断 | 最小Union/Blend -> LOD -> preview -> asset化 |
| G7 Performance Budget | 大規模sceneで速度とGCを制御 | 巨大景観での実用性を担保 | G2-G6代表scene、profiler baseline | PD-3 0% | tool/AI | baseline計測後、根拠箇所だけJob/Burst/pool最適化 |
| G8 Phase E Production Readiness | 配布可能な品質ゲートを形成 | 再現、CI、操作性、保守性をまとめて保証 | G7、方向受入 | 未着手 | shared | CI license/LFS、a11y/i18n、test scene、release gate |
| G9 Landscape Amplification | 構造物が映える景観を強化 | 気候・植生が作品価値を増幅 | G7後、構造物主経路が安定 | Climate/Ecosystem todo | user direction + AI | Climate visualかEcosystemを価値検証して一方だけ着手 |

Destructible Architectureはdensity/voxel経路を要求するためparked。現invariantsのまま
G9へ自動的に混ぜず、3D Voxel主経路を再承認する独立判断が必要である。

## 7. G0-G2後の創造方向

広いUI実装へ入る前に、同じ画面の色違いではない次の方向を比較する。

### A. Operations Console — 現時点の推奨

- 目的: 状態、警告、証拠、Diagnosticsを反復検証の中心に置く。
- 強くなる点: smoke、compile、test、pipeline結果の回収が短くなる。
- tradeoff: 情報密度が高くなりやすい。
- best fit: G0-G3。まずOverview + Diagnosticsの一画面だけproofする。

### B. Generative Atelier

- 目的: SceneView、recipe、seed候補比較を創造作業の中心に置く。
- 強くなる点: variation探索、favorite、before/after比較。
- tradeoff: non-destructive previewと状態管理の実装コストが高い。
- best fit: G4 Controlled Random開始時。

### C. Guided Forge

- 目的: Create -> Shape -> Compose -> Verifyの段階導線を作る。
- 強くなる点: 初回学習、i18n、context help、preset例示。
- tradeoff: pipeline未成熟時に工程を固定すると設計を縛る。
- best fit: G2 end-to-endが安定した後の導入・教育レーン。

G0の実画面を見ずにA+B+Cを一括実装しない。2巡の局所修正で収束しない場合は、
個別調整を止め、選択したdesign principleへ戻る。

## 8. 監修AIが次に発行するMission Packet案

```text
[VASTCORE MISSION]
Outcome: Designer Cockpitを一度のUnity Editorバッチで観測し、
         accept / revise / environment-blockedのどれかを証拠付きで確定する。
Why now: local compile、EditMode 596/596、PlayMode 9/9は緑になり、
         次の実ボトルネックがCockpit手動acceptanceへ移ったため。
Scope: top summary、primary actions、mode panels、Diagnostics、Apply/Undo、
       Save/New/Load、screenshot、evidence同期を含む。
       Packages、ProjectSettings、terrain algorithm、DualGrid redesign、
       broad visual production、main/default切替は含まない。
Acceptance: docs/DESIGNER_COCKPIT_SMOKE_TEST.mdを一括実行し、
            各結果、画面、失敗時の正確なblockerを記録する。
Risk tier: routine evidence pass。高コストなUX方向変更だけ別checkpoint。
Autonomy: 観測支援、狭い再現修正、機械再検証、owning docs同期、commit/push。
Stop only if: Unityがprojectを開けない、データ破壊の恐れ、serialized contract変更、
              またはA/B/Cから未選択の高コスト方向へ入る必要がある時。
Creative checkpoint: 現画面の証拠取得後にA/B/Cを比較。先に全面実装しない。
Sync on close: docs/runtime-state.md、Cockpit evidence/report、Project Pulse。
Start from: codex/vc-development-readiness-20260711 のremote tip、
            docs/DESIGNER_COCKPIT_SMOKE_TEST.md、docs/PROJECT_COCKPIT.md。
```

## 9. 明示的に触れなかったもの

- Cockpitのvisual polishや新mode実装。
- DualGrid/topology algorithm、terrain generation runtime behavior。
- CSG/Blend、Deform、Climate、Ecosystem、Destructionの新規実装。
- `Packages/`、`ProjectSettings/`、Unity installation、cache削除。
- 旧draft PR #47、stash 5件、別worktree 2件。
- GitHub default branch、branch protection、production release。

次のAIは、これらを「未着手だから自由に選べる次作業」と解釈せず、G0の完了と
現在のinvariantsを先に守ること。

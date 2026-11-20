# Project Context

Durable decision log and handoff history live here going forward.

Current state remains in `docs/runtime-state.md`. Operating rules remain in
`docs/REPO_LOCAL_RULES.md`. Historical handoffs and reports remain useful
evidence, but they are not current acceptance unless re-verified.

## Decision Log

- 2026-11-21（再開準備・資料整理）: 本人が「現在のVastCoreを再起動したい。次の集中的に取り組むセッションのために事前準備・資料整理」を依頼した。thankのローカル系列（accd587→af36a0e、11-01文書をe810a50でcommit）と、PLANNER007が2026-11-16にpushしたde4d515（4c7f9fa基準、runtime-stateのみ、次の一手はCockpit受入）が分岐していたため統合した。runtime-stateは両版の観測を残して現在地へ書き換え、両版の状態本文は `docs/restart/VC_RESTART_20261101_HISTORY.md` へ原文のまま移した。次の技術出口はSP-017の通常GUI保存・再配置のまま維持し、Cockpitのaccept/reviseは本人判断として並べた。どちらを先にするかを本人が決めた記録はない。次回の依頼文はSP-017「2026-11-21 次回集中セッションの実行用依頼」（未発行）。Unity起動・コード変更・検証の実行はしていない。11-01の検証Editor PID38072は11-20の再起動で正常終了せずに消えており、lockfileが残る。

- 2026-11-01（修正結果とGUI再開条件）: `af36a0e`をローカルcommit。Arch三角化で通常GUI生成が復旧し、続いてExportの一時コピー破棄が元Meshを失わせる実不具合を観測した。元Mesh保全と独立Mesh資産保存へ修正し、配置の親座標無視・元scale上書きも専用テストの実測から限定修正した。scaleは元Prefab localScaleに掛ける倍率と明記し、フィールド/asmdef/依存は変更しない。最終compile成功、EditMode603/603、PlayMode9/9。修正版GUI往復は再起動Editor PID38072の画面取得エラー `foreground window did not report a process id` で保留し、本人へ前面化のみを依頼した。旧失敗候補と新Scene、既存差分を保持。全体合格・作品採用・pushなし。結果と具体的な残操作は `artifacts/restart-20261101/20261101-0430-arch-fix/RESULT.md`。

- 2026-11-01（Arch限定修正へ続行）: 本人が前回の失敗報告を添えて作業続行と方向修正を依頼した。前回の「ソース修正範囲外」を今回の停止条件へ持ち越さず、CreateManualArchの四角面のみを三角化し、同条件の生成・Export・保存再配置を検証する。巨大景観や別形状による代用、ProBuilder更新、UPM修復には広げない。技術fixtureと本人の作品採用は分け、前回runの失敗記録は保持する。今回の証拠は `artifacts/restart-20261101/20261101-0430-arch-fix/`。

- 2026-11-01（本人発行のSP-017検証実行）: 準備時の未発行missionを本人が今回発行した。run `20261101-0328-sp017`。accd587/準備入力hash一致、6000.3.6f1 compile exit 0、EditMode 596/596、PlayMode 9/9、各一回。新Scene/sessionでCockpit無選択Apply不可・Save/New/Load・対象一件Apply/Undoと未選択対象不変を確認。独立ツール窓は本人のドッキング後に操作できた。Arch一回生成はBasicStructureTab.CreateManualArch:314の4インデックスFaceがProBuilder 6.0.8の3の倍数要求に反し例外。Mesh作成前に失敗し、Export/地形配置/保存往復は未実施。
  ソース修正は範囲外として行わず、検証Editorを正常終了した。推奨次作業はArch内quadの限定三角化と同条件の保存/配置検証だが、未採用・次担当未割当。技術出口はSP-017、全工程への返却はSP-020、証拠は `artifacts/restart-20261101/20261101-0328-sp017/RESULT.md`。通常GUIで今回hostにもUPM path undefinedを観測したが修復しない。既存自動UIスキャンのレポート生成日時変更とprofile GiCache作成を範囲相違として保持。既存tracked 1,648ファイルと既存Assets等は全hash一致。ソース・依存・公開・commit/pushなし。前の同日準備範囲を遡って実行済みにはしない。

- 2026-11-01（本人の今回範囲）: VastCoreの再開は調査・設計・準備文書・最小検証仕様・実行用依頼まで。本格実装、Unity/UPM修復、依存変更、公開、pushを含めない。目的はSSOT_WORLDの巨大でユニークな人工構造物。地形整備や運用整理へ置き換えない。
- 2026-11-01（現物照合）: thankのDESKTOP-H53P1T4 checkoutはaccd587、ProjectVersionは6000.3.6f1。2026-08-12のruntime-stateにあったPLANNER007のUPM障害は現端末で再現済みとは扱わない。.serena/project.ymlの開始時差分を保持。現在の観測範囲・未確認はruntime-stateを参照。
- 2026-11-01（継承と未採用の提案）: DualGrid + HeightMap + designer Prefab Stamp、T1、V4を維持。全工程の再開準備をSP-020、最小検証仕様と実行用依頼をSP-017へ接続した。Arch一件の生成→Export→Scene/SO保存→再読込→地形上再配置を推奨するが、まだ実行していない。既存Cockpit smokeは操作安全性の先行確認。新基盤・別モデル・dispatcherは導入しない。
- 2026-11-01（仕様とソースの差）: SP-020の古いGAP-1/3/4/5は接続コードが存在し、GAP-2にはStampDefinition CustomEditorが存在する。実装不存在として再修復しない。生成メッシュの永続化、反復Build、手編集、旧serialized互換、実画面と本人採用は未確認。spec-indexのSP-019 doneと所有仕様partialの相違は受入済みの根拠にしない。

- 2026-07-11: The current `thank` workstation is accepted as a valid local
  development environment for source work: Unity 6000.3.6f1 compile passed,
  EditMode passed 596/596, and PlayMode passed 9/9 after repairing test asset,
  discovery, logger, and UI creation defects. The earlier UPM `path undefined`
  result remains provenance-specific historical evidence, not the current local
  bottleneck.
- 2026-07-11: Development-readiness repairs continue on
  `codex/vc-development-readiness-20260711` from `f145df6`. This preserves draft
  PR #49 as a workflow-only review. The next product gate is the batched Designer
  Cockpit Editor acceptance, followed by one end-to-end structure-to-DualGrid
  proof before broad Phase D expansion.
- 2026-07-10: Supervisor-to-developer work is outcome-based. A Mission Packet
  authorizes one coherent implementation/verification/state-sync batch; only
  concrete destructive or contract boundaries and unresolved expensive creative
  direction require a mid-block stop.
- 2026-07-10: `docs/runtime-state.md` remains the narrative current-state SSOT.
  A pinned GitHub Project Pulse issue is a generated external projection, while
  Wiki/Pages remain candidates for durable reference material rather than live
  status.
- 2026-06-29: VC-RST-2 remote resume handoff was refreshed after the T+2n
  network/proxy/env audit. Network/proxy/env is now weakened as the direct UPM
  root; the next discriminator is user-approved Unity Editor / embedded UPM
  repair or reinstall, followed by a short-path empty-manifest control retest
  before VastCore is reopened.
- 2026-06-29: A local docs-only refresh of
  `docs/04_reports/LEGACY_UI_MIGRATION_REPORT.md` was preserved for remote sync.
  No `Assets`, `Packages`, or `ProjectSettings` changes are part of this
  handoff.
- 2026-06-23: VC-RST-2 package restoration did not enter Unity or Package
  Manager validation because the required clean worktree path
  `C:\Users\PLANNER007\VastCore\VastCore-origin-main-compile` was absent in the
  current environment. The accessible `main` checkout was left as the handoff
  documentation surface only.
- 2026-06-15: Agent instruction authority moved to the modern front-door pattern:
  `AGENTS.md` -> `docs/REPO_LOCAL_RULES.md` -> `docs/runtime-state.md`.
  `CLAUDE.md` is project context, not operational SSOT.
- 2026-06-15: M0 architecture context was captured as evidence docs under
  `docs/architecture/`. These docs record current compile and dependency risks;
  they do not approve a broad refactor or claim Unity acceptance.

## Historical Handoff Index

- Current state, active artifact, bottleneck, and next action always come from
  `docs/runtime-state.md`; historical entries below are evidence only.
- Designer Cockpit UX review handoff:
  `docs/restart/VC_DESIGNER_COCKPIT_UX_REMOTE_REVIEW_HANDOFF.md`.
- VC-RST-2 package restoration history:
  `docs/restart/VC_PACKAGE_MANAGER_RESTORATION_REPORT.md`.
- Earlier remote-resume packets under `docs/restart/` must not override current
  evidence without re-verification.

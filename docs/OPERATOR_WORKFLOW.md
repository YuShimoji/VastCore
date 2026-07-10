# Operator Workflow

人間オペレーター、監修AI、開発AIの実ワークフロー・痛点・品質目標を保持する正本。

## Overall Flow

1. 監修AIは `runtime-state` と owning artifact を読み、今回変える差分だけを
   Mission Packet にする。過去経緯や全手順を Prompt に複製しない。
2. 高コストなUI・ビジュアル・コンテンツ変更だけは、実装前に2〜3方向を
   比較し、ユーザーが選択または混合して design intent を固定する。
3. 開発AIは一つの reviewable outcome までを coherent batch として実装し、
   関連する狭い修正、機械検証、正本同期まで継続する。
4. 進捗共有は、方針が変わった時、意味のある中間成果が出た時、または長い
   ツール待ちの時に行う。ファイル単位・コマンド単位では報告しない。
5. Unity Editor / scene / visual judgement が必要なら、開発AIは機械検証と分離
   した一つの確認バッチとして対象、操作、成功条件を渡す。
6. 開発AIは `runtime-state` と必要な owning docs を同期し、検証済み範囲と
   未検証範囲を分けて commit/push する。
7. GitHub Project Pulse は `runtime-state` から自動更新し、次のPromptは
   その current outcome が閉じたか、stop condition に達した時だけ発行する。

並行枝はそれぞれのtask/spec/PRへ進捗を残し、global `runtime-state` を奪い合わない。
Project Pulse を更新するのは current outcome を所有する一枝だけで、所有枝を変える時は
handoffとして repository variable `PROJECT_PULSE_BRANCH` と
`runtime-state` の branch/outcome/next action を同時に更新する。

## Supervisor To Developer Mission Packet

一つのPromptは一つの成果単位を渡す。実装手順の細分化ではなく、以下の契約を
短く埋める。既知の状態は commit と正本パスで参照し、Prompt本文に複製しない。

```text
[VASTCORE MISSION]
Outcome: 完了時に誰が何をできるようになるか
Why now: 解消する現在の bottleneck
Scope: 今回含む境界 / 明示的に含まない境界
Acceptance: 必要なコード・画面・テスト・手動確認の証拠
Risk tier: routine（既定・自律実行）/ boundary-changing（具体的な危険と判断を記述）
Autonomy: outcome 内の実装、関連修正、検証、正本同期、commit/push は開発AI判断
Stop only if: 破壊的変更、依存/DB/認証/API契約、仕様衝突、未確定の高コスト方向
Creative checkpoint: 不要 / 方向案を先に提示 / 選択済み intent と proof slice
Sync on close: runtime-state と今回変わる owning artifact
Start from: branch/commit と必要最小限の task/spec/正本パス
```

次の作業が Outcome、Scope、Autonomy の内側なら、監修AIは追加のmicro-promptを
発行せず、開発AIも確認待ちにしない。新しいPromptが必要なのは、成果単位が閉じた、
stop condition に触れた、またはユーザーが方向を変えた時だけ。

## Direction Before Production

レイアウト、色、フォント、アニメーション、言語対応、コンテンツ拡張など、後から
直すほど高価になる変更では次の順序を守る。

1. 監修AIまたは開発AIが、表面的な色違いではない2〜3方向を提示する。各案に
   狙い、利用場面、代償、何が可能になるか、推奨案を添える。
2. ユーザーの選択・混合・修正を、短い design intent と anti-goals に固定する。
3. 全面実装ではなく、一画面・一フロー・一代表コンテンツの proof slice を作る。
4. 実物をまとめてレビューし、承認後に横展開する。

同じ懸念の微修正が2巡しても収束しない、または3件以上の修正が同じ原則に集まる
場合は、個別調整を止めて direction checkpoint に戻る。これは実装停止ではなく、
誤った方向への追加投資を止めるための再設計である。

## Creative Exploration Pulse

主要な成果単位を閉じる前に、現在の主レーンとは異なる視点から最大2件だけ提案する。
UIならレイアウト/情報階層、i18n、色/フォント、motion/accessibility、隣接コンテンツを
候補レンズにする。各提案には仮説、利得、コスト、今決める意味を添え、未承認の提案を
成果物へ黙って混ぜない。

## Actor Boundaries

- user: direction checkpoint の選択、final visual/creative judgement、Unity
  Editor acceptance、frozen product frontier の再開。
- supervisor AI: outcome と bottleneck の定義、異なる方向案、stop condition、
  成果レビュー。実装手順の逐次指示は担当しない。
- assistant: source edits, doc sync, static checks, focused tests, readback, gap
  reports, outcome 内の実装判断、creative option の具体化。
- tool: scripted checks, local servers, browser or editor automation when
  available and task-relevant.
- shared: manual Editor verification followed by assistant evidence capture.

## Manual Verification Boundaries

- Unity Editor compile/play/scene behavior is not accepted from prose alone.
- If the assistant cannot run the required Unity verification, it must state the
  missing check and provide the exact user-owned verification target.
- Do not mix manual verification requests with strategic next-direction choices.

## Pain Points to Avoid

- Rebuilding context from long historical handoffs when `runtime-state` and the
  owning spec are enough.
- Treating stale March-era progress summaries as current acceptance.
- Spreading the same rule across `AGENTS.md`, `CLAUDE.md`, docs, and tool config.
- Asking the user to choose between commit/no-commit instead of explaining the
  actual bottleneck.
- Treating every safe implementation choice as a new approval gate.
- Delivering a broad visual result before direction is selected, then spending
  repeated blocks on local polish without revisiting the design intent.
- Writing progress only in chat or a historical handoff while leaving
  `runtime-state` and the external Project Pulse stale.

## Quality Goal

The workflow is healthy when a new agent can read the normal 3-file restart set,
identify the current outcome and bottleneck, finish a coherent batch without a
micro-prompt chain, and leave both the repo state and public Project Pulse ready
for the next decision.

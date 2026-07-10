# VastCore AI Collaboration Workflow Refresh — 2026-07-10

## 何を整えたか

VastCore の最新成果を含む `fd1cb29` を統合基準として保持し、その上に
`codex/vc-ai-workflow-refresh-20260710` を作成した。Git の同期は完了している。
一方、Unity バッチ検証は既知の Package Manager 障害で C# コンパイル前に止まり、
Designer Cockpit の Editor 手動受入も未実施である。したがって現在は
「Git/編集は再開可能、Unity acceptance は未成立」と分けて扱う。

今回の運用変更は、新しい手順書を増やすことではなく、既存の正本に不足していた
実行契約を入れることに絞った。監修AIは一つの reviewable outcome を Mission Packet
として渡し、開発AIはその範囲で実装、関連修正、検証、状態同期、commit/push まで
止まらずに進める。高コストなUI・ビジュアル・コンテンツ変更だけは、2〜3方向を
比較して intent を固定し、小さな proof slice を見てから横展開する。

## リモート同期と開発可能性

| 確認対象 | 実測 | 現在の判断 |
|---|---|---|
| 現行統合基準 | `codex/vc-rst-cockpit-ux-diagnostics-20260706` の `fd1cb29` | `origin` と 0 ahead / 0 behind。リモート上の最新成果を取得済み |
| `origin/main` | `39f790c` | 現行基準の直系祖先で14コミット後方。ローカル `main` はここまで fast-forward 済み |
| GitHub default | `master` の `23319f7` | 現行より349コミット後方。GitHub入口を最新と誤認させるため、受入後に `main` へ変更が必要 |
| 分岐handoff枝 | `origin/codex/vc-rst-remote-handoff-20260622` | Packages/ProjectSettings/大量削除を混在した保存コミット。wholesale merge しない |
| 作業ツリー | clean から専用枝を作成 | stash 5件と別 worktree 2件は触らず保全 |
| Unity toolchain | 6000.3.6f1 導入済み | ソース編集と狭い静的検証は可能 |
| Unity batch compile | `scripts/check-compile.ps1` exit 1 | `path undefined / No packages loaded`。C#診断には到達していない |
| Cockpit acceptance | checklist と実装は存在 | Editor のレイアウト、Apply/Undo、save/load は手動確認待ち |

## なぜ従来運用が減速したか

履歴上も、2026-06-23から07-07までの14コミット中11件が docs-only の
handoff/decisionで、06-25から06-29だけでもUPM関連のdocs-only commitが10件ある。
安全診断と引継ぎが製品artifactより細かく分割され、主作業化していたことを示している。

| 摩擦 | 構造原因 | 今回入れた補正 | 期待する変化 |
|---|---|---|---|
| 安全側へ倒れ続ける | Value、frontier、actor、approval の例外gateは多いが、可逆な通常判断を進める autonomy がなかった | ゲートを例外フィルタと定義し、停止を破壊的変更、依存/DB/認証/API契約、仕様衝突、未確定の高コスト方向へ限定 | 「念のため確認」では止まらず、具体的な危険と意思決定がある時だけ止まる |
| Prompt が細切れになる | 旧 worker prompt 群を削除した後、監修→開発の代替パケットがなかった | Outcome、Why now、Scope、Acceptance、Autonomy、Stop、Creative checkpoint、Sync を一つの Mission Packet に集約 | 1 Prompt が1コマンドではなく1成果単位になり、関連修正と検証を連続実行できる |
| 進捗と次作業が流れる | chat、restart report、古い index に状態が分散し、更新条件が通常読まない深いルールに隠れていた | branch/artifact/bottleneck/evidence/next action の変化時は `runtime-state` 更新を完了条件に昇格 | 次のAIが3ファイルで再開でき、Git同期・検証・人間受入を混同しない |
| Wiki等が更新されない | Wiki/Pages/Issue の自動投影がなく、手作業の二重SSOTしか選べなかった | `runtime-state` を検証し、固定 GitHub Project Pulse Issue へ自動投影する workflow を追加 | 外部から一画面で現在地を確認でき、更新作業を思い出す必要がなくなる |
| 創造提案がなく微修正沼になる | creative judgement をユーザー所有としただけで、AIの方向案提示と実装前checkpointがなかった | 高コスト変更は2〜3方向→intent固定→proof slice→batch review。2巡で収束しなければ原則へ戻る | 大きな成果物の後で意図を発見するのではなく、安い段階で方向を合わせられる |

## 監修AIと開発AIの新しい分担

監修AIは「何を達成するか」「なぜ今か」「どの方向を試すか」「本当に止める境界は
何か」を担当する。開発AIは、その成果単位の内側にある実装順、関連する狭い修正、
検証方法、正本同期を担当する。監修AIがコマンド列を作り、開発AIが一行ずつ消化する
関係には戻さない。

次回以降のPromptは `docs/OPERATOR_WORKFLOW.md` の Mission Packet をそのまま使用する。
最低限、次の意味が一つのPrompt内で閉じていなければ発行しない。

```text
Outcome: 完了時に何が使える・判断できるようになるか
Why now: 解消する現在の bottleneck
Scope: coherent batch の境界と非対象
Acceptance: 機械検証、人間確認、成果物の必要証拠
Risk tier: routine / boundary-changing。後者だけ具体的な危険と判断を記述
Autonomy: outcome 内の実装・関連修正・検証・状態同期・commit/push
Stop only if: 具体的な破壊/契約/仕様衝突、または未選択の高コスト方向
Creative checkpoint: 不要、方向案提示、または選択済み proof slice
Start from / Sync on close: commit と正本パス
```

進捗はコマンドの実況ではなく、方針変更、reviewable な中間成果、長いツール待ちで
共有する。完了時の次案は Advance / Verify / Excise / Explore のように異なる
bottleneck を解き、各案で何が可能になるかを示す。

## 外部から見える現在地

ライブ状態に Wiki を選ばなかった理由は、Wiki が別リポジトリで手動更新と認証を
増やし、再び二重SSOTになりやすいためである。固定・pin した GitHub Issue
[#48 Project Pulse](https://github.com/YuShimoji/VastCore/issues/48) を作成し、
`docs/runtime-state.md` を本文へ自動投影する。
Issue は通知、履歴、コメントによる判断回収に向き、`GITHUB_TOKEN` の
`issues: write` だけで更新できる。

並行枝による後勝ち上書きを防ぐため、repository variable
`PROJECT_PULSE_BRANCH` が公開元を一枝だけ指定する。作業枝を `main` へ昇格する時は、
まずmerge自体を検証し、その後 `main` 上の `runtime-state` と同variableを同じhandoffで
切り替える。これにより同一commitの昇格をbranch名検査で妨げず、未受入枝もPulseを
奪えない。

Wiki は今後、用語集、長期設計、確定した意思決定など更新頻度の低い読み物へ使う。
視覚的ダッシュボードが必要になった時は、同じ正本から GitHub Pages を生成し、
Pulse本文の手動コピーは行わない。

外部公開の完全な正常化には、Cockpit成果の受入後に次の順番が必要になる。

1. 現在のCockpit/運用枝をレビューし、受入済みtipを `main` へ fast-forwardする。
2. GitHub default branch を古い `master` から `main` へ変更する。
3. branch protection と Actions の対象を `main` に揃える。
4. `master`、競合中の旧draft PR、古いstatus/indexを整理する。

## 今回あえて混ぜなかった整理

`docs/WORKFLOW_STATE_SSOT.md`、`docs/DOCS_INDEX.md`、`docs/HANDOVER.md`、大量の
`docs/restart/` は、現在の3ファイル導線と重複または歴史化している。ただし一括削除は
参照切れと証拠喪失を起こし得るため、今回の運用契約と混ぜていない。次の Excise
スライスでは参照graphを検査し、current owner へ必要情報を昇格してから archive/delete
を分けて実施する。

同様に、既存の `markdownlint` / CodeQL は `develop` のみを対象とし、Unity workflow
はプロジェクトと異なる Editor version を指定している。これらは「赤いcheckを増やす」
ためではなく、`main` 切替と一緒に実行可能な最小CIへ直すべきで、今回の文書契約とは
別スライスに残した。

## Cockpitで先に比較する創造方向

新しいcheckpointを実際に適用するなら、現行Cockpitをすぐ微修正せず、次の3方向から
選ぶ。いずれも同じ画面の色違いではなく、誰のどの作業を中心にするかが異なる。

| 方向 | 体験とレイアウト | 色・書体・motion | 言語と隣接コンテンツ | 最初のproof slice |
|---|---|---|---|---|
| **A. Operations Console** | 上部に状態/警告/証拠、中央にmode作業、右にDiagnostics。反復検証を最短化する高密度console | charcoal + terrain amber + verified green。本文は可読性優先、seed/値だけmono。状態遷移は短いfade/pulse | 日英併記はtooltipから開始。Evidence Tiles、check履歴、before/after snapshotへ伸ばす | Overview + Diagnosticsを一画面で再構成し、Cockpit smokeの証拠をその場で残す |
| **B. Generative Atelier** | SceneView/previewを主役にし、Cockpitはrecipeとvariationを選ぶ細いdock。比較と偶発性を重視 | earth/cyanの低彩度palette、太いdisplay見出し。variation適用時にghost previewと差分motion | Recipe Library、seed history、Architecture/Biome kit、favorite比較へ伸ばす | Random Variation だけをnon-destructive preview + 3候補比較にする |
| **C. Guided Forge** | Create→Shape→Compose→Verifyのstep rail。初心者が次の一手を失わない段階開示 | 明るいneutral base + mode固有accent。説明用sans、数値はtabular。完了時だけ穏やかなprogress motion | 日本語/英語切替を最初から想定し、用語集、例付きpreset、context helpへ伸ばす | Session作成からsave/loadまでを1本のguided flowにする |

現時点の推奨は **Aを最初のproof slice** にすること。現在のacceptance bottleneckが
Diagnosticsと手動smokeの可視化だからで、既存mode構造を壊さず実物から判断できる。
その後、Bのpreview/recipeを創造レーン、Cの日英guidanceを導入レーンとして足す余地が
ある。ただしA+B+Cの一括実装はせず、現行スクリーンショットを見て選択・混合してから
着手する。

## 次に開く入口

| 入口 | 解く摩擦 | 必要条件と現在状態 | 完了すると可能になること |
|---|---|---|---|
| **Verify — Cockpit proof**（推奨） | 製品成果の受入が文書だけで止まっている | UPM障害を避けてEditorを開ける環境、または環境修復後。手動checklistは準備済み | workflow改善が製品進捗へ戻り、Cockpit枝を `main` 候補として判断できる |
| **Advance — GitHub baseline** | default `master` と競合PRが外部現在地を壊す | Cockpit/運用枝のレビュー受入 | `main` を唯一の統合基準にし、Pulse・Actions・READMEを正しく動かせる |
| **Excise — SSOT reduction** | 古いSSOT、handoff、indexが再開時のノイズになる | 参照graph監査とarchive方針。大規模削除は未実施 | 読む文書と更新ownerが減り、監修/開発双方のcontext負荷が下がる |
| **Explore — Cockpit visual directions** | layout、i18n、type/color/motion、隣接コンテンツの提案不足 | 現行画面のスクリーンショットまたはEditor観測 | 2〜3方向を比較して次のproof sliceを選べ、完成後の微修正沼を避けられる |

現時点の推奨は Verify である。ただし Unity/UPM がEditor起動自体を妨げる場合は、
その事実を一度で切り分けて Advance または環境修復へ切り替え、再び長い安全診断を
主作業にはしない。

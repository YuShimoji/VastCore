# Orchestration Prompt

## 目的

- このファイルは、プロジェクトでAI開発を回すための **オーケストレーション用プロンプト**です。
- 各プロジェクトでは、本ファイルをプロジェクトルートに `ORCHESTRATION_PROMPT.md` として配置して運用します（任意）。

## 推奨の最小運用（3テンプレで完結）

- 初回（セットアップ / 参照が不安定な場合の立て直し）:
  - `.shared-workflows/prompts/first_time/PROJECT_KICKSTART.txt` をセットアップ担当スレッドに貼る
- 毎回（開発継続 / Orchestratorスレッド起動時）:
  - `.shared-workflows/prompts/every_time/ORCHESTRATOR_METAPROMPT.txt` を Orchestrator スレッドに貼る

運用者の入口（参照。どのフォルダを開く/どれをコピペする）:
- `.shared-workflows/docs/windsurf_workflow/OPEN_HERE.md`

Orchestrator が各担当者（Worker）を起動する際の「タスク分解済みプロンプト」は、毎回チケット内容に合わせて **可変で自動生成**する。
生成のベース（3つ目のテンプレ / 参照用）は以下:

- `.shared-workflows/docs/windsurf_workflow/WORKER_PROMPT_TEMPLATE.md`

本ファイル（プロジェクトルートの `ORCHESTRATION_PROMPT.md`）は任意です。運用の事情で「プロジェクト固有の前提/制約/例外」を残したい場合にのみ使用し、毎回の起動プロンプトは原則 Orchestrator Metaprompt に統一します。

## 最初に必ず読むもの（優先順）

1. SSOT（最新版ルール / 固定参照先）
   - 推奨: `.shared-workflows/docs/Windsurf_AI_Collab_Rules_latest.md`
   - フォールバック: `docs/Windsurf_AI_Collab_Rules_latest.md`
   - 本テンプレート内では、`.shared-workflows/` を `SW_ROOT` と呼ぶ
2. プロジェクトルートの `AI_CONTEXT.md`
3. プロジェクトルートの `ORCHESTRATION_PROMPT.md`（本ファイル。運用している場合）

## 使い分け（役割別プロンプト）

このファイルは「全体の進行役（オーケストレーター）」向けです。基本方針として、担当者用の固定テンプレートは増やさず、必要に応じてオーケストレーターが Worker 起動用の最小プロンプトを動的に生成します。下記の役割別プロンプトは参考/フォールバックとして扱います（運用方針として固定テンプレを増やさない場合は、基本的に使いません）。

- 実装者: `.shared-workflows/templates/ROLE_PROMPT_IMPLEMENTER.md`
- レビュア: `.shared-workflows/templates/ROLE_PROMPT_REVIEWER.md`
- CI対応: `.shared-workflows/templates/ROLE_PROMPT_CI_HANDLER.md`
- リリース担当: `.shared-workflows/templates/ROLE_PROMPT_RELEASE_MANAGER.md`

## 進め方（最小）

- Issue（Goal/DoD/影響/リスク(Tier)）を起点に進める
- 作業の区切りごとに `AI_CONTEXT.md` を更新し、会話に依存せず再開可能にする
- クリーンアップチェック → Pre-flight → コミット（必要に応じてプッシュ）

## 毎回のプロンプト（オーケストレーター用 / コピペ用）

```text
あなたはこのプロジェクトの「オーケストレーター」です。

最優先で読むもの:
- SSOT（latest）: `.shared-workflows/docs/Windsurf_AI_Collab_Rules_latest.md`（推奨。無ければ `docs/Windsurf_AI_Collab_Rules_latest.md`）
- プロジェクトの AI_CONTEXT.md
- （運用していれば）プロジェクトの ORCHESTRATION_PROMPT.md

目的:
- 依頼を「実装/レビュー/CI/リリース/運用」などの役割に割り当て、最短で完了させる。
- エッジケース（CI失敗、権限不足、依存追加が必要、コンフリクト等）を前提に、止まらずに次手を出す。

役割の使い分け:
- 実装が主なら Implementer
- PR差分の評価が主なら Reviewer
- CI失敗の切り分けが主なら CI Handler
- リリース手順/ノート/ロールバックが主なら Release Manager
- 担当者用のプロンプトは固定テンプレのコピペではなく、オーケストレーターが状況（Tier/Focus/Forbidden 等）に合わせて生成して Worker スレッドへ貼り付ける

コマンド実行:
- 原則: ローカルで安全なコマンドは自律実行してよい。
- 例外: 破壊的/復旧困難、依存追加/更新、長時間、外部通信。
  - ただし GitHub 操作が自動承認の運用なら、GitHub操作（push/PR作成/マージ等）で承認待ち停止しない。
  - ただし `force push` / `rebase` / `reset` のような履歴破壊・復旧困難な操作は、必要ならではなく **常に** 方針確認を取る。

ダブルチェック（必須）:
- Push/Merge/テストは「実行した」だけで完了にしない。失敗（エラー/非0終了/拒否/競合/タイムアウト）が出たら「失敗」と明言し、根拠（要点）と次手を提示する。
- Push/Merge 実行後は必ず `git status -sb` を確認し、必要なら `git diff --name-only --diff-filter=U` が空であることを確認する。
- 待機が必要な場合はタイムアウト（上限時間）と打ち切り条件を定義し、超過したらタイムアウトとして扱い次手へ進む（無限待機しない）。
- 実装がうまくいかなかった場合でも、記述だけで完了扱いにしない。完了条件を満たせない場合は「未完了」と明言し、現状/原因/次手を残す。

進め方:
1) 依頼を Issue / Goal / DoD に落とし込む（不足があれば補って明確化）
2) 大項目/中項目/小項目に分解し、今どの役割が必要か宣言
2.5) Bootstrap（初回/環境未整備のみ）: `SW_ROOT` が無い場合は submodule 追加を提案し、必要なら承認を取って実行する（外部通信）
3) 実行（役割別の作法を尊重）
4) 結果を短く報告し、次の中断可能点・決定事項・リスクを AI_CONTEXT.md に反映する

次のユーザー依頼を処理してください:
<USER_REQUEST>
```

## コマンド実行ポリシー（高速化 / 標準）

- **原則**: ローカルで安全なコマンドは AI が自律実行してよい（作業を止めない）
  - 例: 読み取り/検索/差分確認/静的解析/テスト/ビルド/フォーマット（プロジェクト内に閉じる範囲）
- **例外**: 以下に該当する場合は事前承認を取る
  - **外部通信**（例: `git fetch/pull/push`、パッケージ取得、外部API呼び出し。※ GitHub操作を自動承認する運用なら承認待ちで停止しない）
  - **破壊的/復旧困難な操作**（例: 削除、強制上書き、`reset`、`rebase`、`force push`）
  - **依存関係の追加/更新**（例: `npm install`、`pip install`）
  - **長時間/高負荷/大量出力が見込まれる操作**（目安: 数分以上、または大量ログ）

### 運用オプション: GitHub操作を自動承認する

プロジェクトの運用として「普段から push・PR作成・マージまで自動承認」する場合は、外部通信（`git fetch/pull/push` 等）や GitHub 操作について **承認待ちで停止しない** ことを優先します。

- 条件: 実行環境（ツール設定/CI権限/ルール）側で、自動承認が有効
- この場合の扱い: `git fetch/pull/push` や PR作成/マージ等は、都度の確認を省略して自律実行してよい
- ただし、`force push` / `rebase` / `reset` のような履歴破壊・復旧困難な操作は、常に方針確認を取る

### 承認が必要な場合の提示フォーマット（推奨）

AI は次の情報をまとめ、可能な限り **ワンストップ（1回の承認）**で実行できる形で提示します。

- **目的**: 何のために実行するか
- **実行内容**: 何をするか（概要）
- **コマンド一覧**: 実行順に列挙（必要に応じて / 省略可）
- **期待される変更**: ファイル変更の有無、外部通信の有無
- **リスク（Tier）**: 低/中/高（目安）

## レポート（推奨）

- `AI_CONTEXT.md` の `report_style` / `mode` を尊重する
- 重要な結論/決定/リスク/次アクションは、チャットで完結させず `AI_CONTEXT.md` に転記する

## エッジケース早見表（よくある詰まりどころ）

- **AI_CONTEXT.md が無い/古い**: 最初に作る/更新する（会話依存を断つ）
- **PR自動マージが働かない**: ブランチ保護・Required reviews・Auto-merge設定・CI必須チェックを確認
- **CIが失敗する**: 最初の失敗に集中し、ローカル再現→修正→再実行（最大3回）
- **CIが10分以上終わらない**: タイムアウトとしてIssue/PRに状況を記録し、待機/方針確認
- **pushが拒否（non-fast-forward）**: fetchして差分把握。rebase/force pushは原則避け、必要なら方針確認
- **依存追加/更新が必要**: 影響（ロック更新・CVE・互換性）を明記し、最小差分で行う
- **権限不足（Actions/GITHUB_TOKEN）**: workflow permissions / fork制約 / org設定を確認。危険な回避策は提案止まり
- **Secretsが必要**: 値のハードコード禁止。必要な環境変数名と設定場所だけ提示

---

## デモ: 返ってくるメッセージと対応

### デモ1: 外部通信が必要（push）

#### ユーザーからの依頼

- 「変更をコミットしてpushまでお願いします」

#### AIの応答（例）

- 変更の概要と影響範囲を1段落で説明
- 外部通信（`git push`）がある
  - GitHub操作が自動承認の運用なら、そのまま実行する
  - 自動承認でない運用なら、承認を取る
- 1回の承認で「コミット→push」までをまとめて実行する（必要ならコマンド詳細も併記）

#### 実行後に返ってくるメッセージ（例）

```text
## main...origin/main
[main 1234567] docs: ...
 3 files changed, 10 insertions(+), 2 deletions(-)
To github.com:org/repo.git
   abcdef0..1234567  main -> main
```

#### AIの対応（例）

- `git status` が `main...origin/main` であることを確認
- 変更点（どのファイルがどう変わったか）と、次の作業（TODOの次）を提示
- `AI_CONTEXT.md` を更新すべき内容があれば反映を促す

### デモ2: pushが拒否される（behind / non-fast-forward）

#### 実行後に返ってくるメッセージ（例）

```text
! [rejected]        main -> main (non-fast-forward)
error: failed to push some refs
hint: Updates were rejected because the remote contains work that you do
hint: not have locally.
```

#### AIの対応（例）

- 原因: リモートに新しいコミットがあり、手元がbehind
- 方針の選択肢を提示
  - `git pull --rebase`（履歴を直線化）
  - `git pull`（マージコミット）
- どちらも外部通信/履歴変更を含むため、承認を取ってからワンストップで実行する

### デモ3: CIが失敗する（最初の失敗に集中）

#### 実行後に返ってくるメッセージ（例）

```text
Run npm test
FAIL src/foo.test.ts
Expected: 3
Received: 2
```

#### AIの対応（例）

- 最初の失敗（最上段のFAIL）を起点に原因仮説を2つ以内で提示
- ローカルで再現できる最小手順を作る
- 修正→ローカルテスト→コミット→push
- CI再実行を確認し、成功なら自動マージへ

### デモ4: 依存関係の追加/更新が必要（例: npm install）

#### AIの応答（例）

- 目的: 依存追加/更新が必要になった理由（ビルド失敗/セキュリティ対応等）を短く説明
- 期待される変更: lockファイル更新、影響範囲（CI/実行環境）
- リスク: 互換性、CVE、サイズ増
- 実行: 最小差分で追加/更新し、テストで担保

### デモ5: 権限不足（Actions / GitHub API）

#### 実行後に返ってくるメッセージ（例）

```text
Error: Resource not accessible by integration
```

#### AIの対応（例）

- 原因: GITHUB_TOKEN権限不足、fork PR制約、org設定の可能性
- まず確認: workflowの `permissions` / 対象イベント（pull_request vs pull_request_target）
- 危険な回避策（pull_request_target等）は、リスクを明記して提案止まり

### デモ6: PR自動マージが働かない

#### 症状（例）

- CI成功してもマージされない

#### AIの対応（例）

- まず確認:
  - Auto-mergeが有効か（PR側で有効化が必要な設定か）
  - ブランチ保護（Required reviews / Required status checks）の条件
  - CIの必須チェック名が変わっていないか
- 対応:
  - 条件を満たすための最短修正（チェック名修正、権限/設定の追加）を提案
  - 設定変更が必要ならリスク（Tier）を明記して提案止まり

### デモ7: Secrets/環境変数が足りない

#### 実行後に返ってくるメッセージ（例）

```text
Error: Missing required env var: STRIPE_API_KEY
```

#### AIの対応（例）

- 絶対禁止: 値のハードコード、チャットへの秘密の貼り付け
- まずやる: 必要な環境変数名と用途を整理
- 提示:
  - ローカル: `.env` / 環境変数の設定手順
  - CI: GitHub Secrets に登録すべきキー名と参照方法

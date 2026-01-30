# Worker Prompt: TASK_018_MergeConflictResolution

```xml
<instruction>
あなたは分散開発チームの Worker です。割り当てられた 1 タスクだけを完遂し、証跡を残してください。
</instruction>

<context>
<mission_log>
作業開始時に `.cursor/MISSION_LOG.md` を読み込み、現在のフェーズと進捗を確認してください。
作業完了時に MISSION_LOG.md を更新し、進捗を記録してください。

現在のMISSION_LOG状態:
- Mission ID: ORCH_20250112_MERGE_CONFLICT
- 現在のフェーズ: Phase 5: Worker起動用プロンプト生成（完了）
- ステータス: IN_PROGRESS
- 目的: `origin/master`ブランチの更新を`develop`ブランチに取り込む、マージコンフリクトの解決、統合後の動作確認
- 完了済み: `origin/master`ブランチの更新取得完了、マージ実行完了（コンフリクト発生）、マージコンフリクト解決タスク起票完了
- 未完了: マージコンフリクトの解決（約28ファイル）、コンパイルエラーの確認と修正、統合後の動作確認
- 技術的課題: 約28ファイルでマージコンフリクトが発生、主なコンフリクトの種類: コンテンツコンフリクト、追加/追加コンフリクト、変更/削除コンフリクト、名前空間の変更（`Vastcore.Generation` → `Vastcore.Terrain.Map`）に注意が必要
- コンフリクトファイル内訳: Assembly（8ファイル）、Core（4ファイル）、Terrain（10ファイル）、Editor（3ファイル）、Config（1ファイル）、Other（2ファイル）
</mission_log>

<ssot_reference>
Phase 0: 参照と整備
- SSOT: .shared-workflows/docs/Windsurf_AI_Collab_Rules_latest.md（無ければ docs/ 配下を参照し、必ず `ensure-ssot.js` で取得を試す）
- 進捗: docs/HANDOVER.md
- チケット: docs/tasks/TASK_018_MergeConflictResolution.md（**存在確認: `Test-Path docs/tasks/TASK_018_MergeConflictResolution.md` または `ls docs/tasks/TASK_018_MergeConflictResolution.md`**）
- SSOT 未整備・ensure-ssot.js 不在で解決できない場合は停止条件
</ssot_reference>

<preconditions>
Phase 1: 前提の固定
- Tier: 1
- Branch: develop
- Report Target: docs/inbox/REPORT_TASK_018_MergeConflictResolution_20250112.md
- GitHubAutoApprove: docs/HANDOVER.md の記述を参照（未記載なら push 禁止）
- ブランチが異なる場合:
  - `git status -sb` で未コミットが無いことを確認
  - `git switch develop` で切替を試す
  - 破壊的操作が必要なら停止条件
</preconditions>

<boundaries>
Phase 2: 境界
- Focus Area: コンフリクトが発生した全ファイル（約28ファイル）、カテゴリ別に順次処理:
  1. アセンブリ定義ファイル（`.asmdef`、8ファイル）: 最優先
  2. コアシステムファイル（`VastcoreSystemManager.cs`, `VastcoreErrorHandler.cs`等、4ファイル）
  3. テレイン関連ファイル（`Terrain/Map/*.cs`, `Generation/Map/*.cs`、10ファイル）
  4. Editorファイル（3ファイル）
  5. 設定ファイル（`Packages/manifest.json`等、1ファイル）
  6. その他（2ファイル）
  （この範囲のみ変更可能、**存在確認してから参照**、**カテゴリごとに処理し、各カテゴリ完了後に中間報告**）
- Forbidden Area: 既存の正常動作しているロジックの破壊的変更、コンフリクト解決時の機能削除（削除が必要な場合は理由を明確に記録）、テストファイルの無断削除（触れる必要が出たら停止条件）
- DoD: 
  - [ ] すべてのマージコンフリクトが解決されている
  - [ ] マージコンフリクトマーカーが残っていないことを確認
  - [ ] Unityエディターでコンパイルエラーが発生しないことを確認
  - [ ] 名前空間の参照が正しく更新されていることを確認
  - [ ] 削除されたファイルへの参照が残っていないことを確認
  - [ ] マージコミットが作成されている
  - [ ] `git status -sb`がクリーンな状態であることを確認
  （完了時にチェックリストを埋め、根拠を残す）
</boundaries>
</context>

<workflow>
<phase name="Phase 0: 参照と整備">
<step>
**重要: Phase 0では参照ファイルのみを読み込む。コンフリクトファイルは読み込まない。**

1. `.cursor/MISSION_LOG.md` を読み込み、現在のフェーズと進捗を確認。
2. SSOT: .shared-workflows/docs/Windsurf_AI_Collab_Rules_latest.md（無ければ docs/ 配下を参照し、必ず `ensure-ssot.js` で取得を試す）
3. 進捗: docs/HANDOVER.md（**存在確認してから読み込む**）
4. チケット: docs/tasks/TASK_018_MergeConflictResolution.md（**存在確認: `Test-Path docs/tasks/TASK_018_MergeConflictResolution.md` または `ls docs/tasks/TASK_018_MergeConflictResolution.md`**）
5. SSOT 未整備・ensure-ssot.js 不在で解決できない場合は停止条件

**禁止**: Phase 0でコンフリクトファイル（`VastcoreSystemManager.cs`等）を読み込まない。これらはPhase 3で必要に応じて読み込む。
</step>
</phase>

<phase name="Phase 1: 前提の固定">
<step>
1. Tier: 1
2. Branch: develop
3. Report Target: docs/inbox/REPORT_TASK_018_MergeConflictResolution_20250112.md
4. GitHubAutoApprove: docs/HANDOVER.md の記述を参照（未記載なら push 禁止）
5. ブランチが異なる場合:
   - `git status -sb` で未コミットが無いことを確認
   - `git switch develop` で切替を試す
   - 破壊的操作が必要なら停止条件
6. MISSION_LOG.md を更新（Phase 1 完了を記録）。
</step>
</phase>

<phase name="Phase 2: 境界確認">
<step>
1. Focus Area: コンフリクトが発生した全ファイル（約60ファイル）、特に以下の重要なファイル:
   - アセンブリ定義ファイル（`.asmdef`）
   - コアシステムファイル（`VastcoreSystemManager.cs`, `VastcoreErrorHandler.cs`等）
   - テレイン関連ファイル（`Terrain/Map/*.cs`）
   - テストファイル（`Testing/*.cs`）
   - パッケージ設定ファイル（`Packages/manifest.json`, `Packages/packages-lock.json`）
   （この範囲のみ変更可能、**存在確認してから参照**）
2. Forbidden Area: 既存の正常動作しているロジックの破壊的変更、コンフリクト解決時の機能削除（削除が必要な場合は理由を明確に記録）、テストファイルの無断削除（触れる必要が出たら停止条件）
3. DoD: 
   - [ ] すべてのマージコンフリクトが解決されている
   - [ ] マージコンフリクトマーカーが残っていないことを確認
   - [ ] Unityエディターでコンパイルエラーが発生しないことを確認
   - [ ] 名前空間の参照が正しく更新されていることを確認
   - [ ] 削除されたファイルへの参照が残っていないことを確認
   - [ ] マージコミットが作成されている
   - [ ] `git status -sb`がクリーンな状態であることを確認
   （完了時にチェックリストを埋め、根拠を残す）
4. MISSION_LOG.md を更新（Phase 2 完了を記録）。
</step>
</phase>

<phase name="Phase 3: 実行ルール">
<step>
1. **DoD 各項目の実行可能性確認（必須）**:
   - DoD 各項目を確認し、実行可能かどうかを判断する
   - Unityエディターでのコンパイルエラー確認は、Unityエディターが利用可能な環境でのみ実行可能
   - この場合、**停止条件として扱う**か、**代替手段を取る**かを判断する
   - 判断に迷う場合は、停止条件として扱う

2. チャットで完結させない。成果はファイル（docs/tasks / docs/inbox / docs/HANDOVER / git）に残す。

3. コマンドは実行して結果で判断。失敗は「失敗」と明記し、根拠と次手を出す。

4. 指示コマンドが無い場合: `Get-Command <cmd>` 等で確認 → 代替案提示 → それでも依存追加/外部通信が必要なら停止。

5. 「念のため」のテスト/フォールバック/リファクタは禁止（DoD 従属のみ）。

6. ダブルチェック:
   - テスト/Push/長時間待機は結果を確認し、未達なら完了扱いにしない。
   - `git status -sb` で差分を常に把握（Gitリポジトリではない場合はスキップ可能）。

7. タイムアウトを宣言し、無限待機しない。

8. MISSION_LOG.md を更新（Phase 3 完了を記録、実行内容を記録）。

**中間報告ルール（長大作業の安定化）**:
- **ツール呼び出し10回ごと**、または**ファイル編集5回ごと**に、以下の中間報告を出力する:
  - `### 中間報告`
  - 完了したカテゴリ / 残りカテゴリ / 現在のブロッカー
  - **次のメッセージで何を指示すべきか**（選択肢形式で提示）
- 報告後、ユーザーからの確認なしに続行してよいが、**報告を省略してはならない**。

**マージコンフリクト解決の具体的な手順（カテゴリ別順次処理）**:
1. **カテゴリ1: アセンブリ定義ファイル（`.asmdef`、8ファイル）**
   - `git diff --name-only --diff-filter=U | Select-String '\.asmdef$'` でコンフリクトファイルを確認
   - 各ファイルを順次処理（**一度に1ファイルずつ**）
   - コンフリクトマーカー（`<<<<<<<`, `=======`, `>>>>>>>`）を確認
   - コンフリクト解決後、`git add <file>` でステージング
   - 8ファイル完了後に中間報告

2. **カテゴリ2: コアシステムファイル（4ファイル）**
   - `git diff --name-only --diff-filter=U | Select-String 'Core/'` でコンフリクトファイルを確認
   - 各ファイルを順次処理（**一度に1ファイルずつ**）
   - 名前空間の変更（`Vastcore.Generation` → `Vastcore.Terrain.Map`）に注意
   - コンフリクト解決後、`git add <file>` でステージング
   - 4ファイル完了後に中間報告

3. **カテゴリ3: テレイン関連ファイル（10ファイル）**
   - `git diff --name-only --diff-filter=U | Select-String 'Terrain/|Generation/'` でコンフリクトファイルを確認
   - 各ファイルを順次処理（**一度に1ファイルずつ**）
   - 名前空間の変更に注意
   - コンフリクト解決後、`git add <file>` でステージング
   - 10ファイル完了後に中間報告

4. **カテゴリ4-6: Editor/Config/Other（6ファイル）**
   - 同様に順次処理
   - 各カテゴリ完了後に中間報告

**コンフリクト解決の原則**:
- コンテンツコンフリクト: 両方の変更を統合するか、適切な方を選択
- 追加/追加コンフリクト: 両方の追加を統合するか、適切な方を選択
- 変更/削除コンフリクト: 削除の意図を確認してから決定（削除が必要な場合は理由を明確に記録）
- コンフリクト解決後、コンフリクトマーカーを完全に削除
- すべてのコンフリクト解決後、`git commit` でマージコミットを作成
</step>
</phase>

<phase name="Phase 4: 納品 & 検証">
<step>
**必須: DoD の実際の達成確認（表面的な確認ではなく、実際に実施した内容を記録）**

1. **DoD 各項目の達成確認（必須）**:
   - DoD 各項目に対して、**実際に実施した内容**を記録する（「確認済み」などの表面的な記述は禁止）
   - Unityエディターでのコンパイルエラー確認は、Unityエディターが利用可能な環境でのみ実行可能
   - この場合、**停止条件として扱う**か、**代替手段を取る**かを判断する
   - 停止条件として扱う場合: チケットを BLOCKED に更新し、停止時の必須アウトプットを残す
   - 代替手段を取る場合: 代替手段の内容と根拠をレポートに記録する（例: `grep` でコンパイルエラーの可能性を検出、`git diff` で変更内容を確認）
   - DoD 各項目の達成根拠を以下の形式で記録する:
     - 実施したコマンド: `<cmd>=<result>`
     - 実施した調査: `<調査内容>=<結果>`
     - 実施した実装: `<実装内容>=<結果>`
   - **重要**: DoD に「Unityエディターでコンパイルエラーが発生しないことを確認」が含まれている場合、実際にその確認を実施した内容を記録する。実施していない場合は、停止条件として扱うか、代替手段を取る。

2. チケットを DONE に更新する前に、DoD 各項目の達成根拠を確認する:
   - DoD 各項目が実際に達成されているかを確認する
   - 環境依存で実行不可能な項目がある場合、停止条件として扱うか、代替手段を取るかを判断する
   - 判断に迷う場合は、停止条件として扱う

3. チケットを DONE に更新し、DoD 各項目に対して根拠（差分 or テスト結果 or 調査結果）を記入

4. docs/inbox/ にレポート（以下テンプレ）を作成/更新し、`node .shared-workflows/scripts/report-validator.js docs/inbox/REPORT_TASK_018_MergeConflictResolution_20250112.md`（無ければ `node scripts/report-validator.js docs/inbox/REPORT_TASK_018_MergeConflictResolution_20250112.md REPORT_CONFIG.yml .`）を実行。結果をレポートに記載

5. docs/HANDOVER.md の該当セクションを更新し、次回 Orchestrator が把握できるよう記録

6. 実行したテストを `<cmd>=<result>` 形式でレポートとチケットに残す

7. `git status -sb` をクリーンにしてから commit（必要なら push）。push は GitHubAutoApprove=true の場合のみ

8. MISSION_LOG.md を更新（Phase 4 完了を記録、納品物のパスを記録）。
</step>
</phase>

<phase name="Phase 5: チャット出力">
<step>
1. 完了時: `Done: docs/tasks/TASK_018_MergeConflictResolution.md. Report: docs/inbox/REPORT_TASK_018_MergeConflictResolution_20250112.md. Tests: <cmd>=<result>.`
2. ブロッカー継続時: `Blocked: docs/tasks/TASK_018_MergeConflictResolution.md. Reason: <要点>. Next: <候補>. Report: docs/inbox/REPORT_TASK_018_MergeConflictResolution_20250112.md.`
3. MISSION_LOG.md を更新（Phase 5 完了を記録）。
</step>
</phase>
</workflow>

<stop_conditions>
停止条件:
- Forbidden Area に触れないと解決できない
- 仕様仮定が3件以上
- SSOT が取得できない / `ensure-ssot.js` でも解決不可
- 依存追加 / 外部通信（fetch/pull/push 等）が必要で GitHubAutoApprove=true が未確認
- 破壊的・復旧困難操作（rebase/reset/force push 等）が必要
- 数分以上の待機が必須、またはタイムアウト超過が見込まれる
- **環境依存で実行不可能なDoD項目がある場合**:
  - Unityエディターが利用可能でない環境で、Unityエディターでのコンパイルエラー確認が必要なDoD項目がある場合
  - 代替手段が取れない場合、停止条件として扱う
  - 停止時は、環境依存の理由と代替手段の検討結果をレポートに記録する
- コンフリクト解決に必要な情報が不足している
- 破壊的変更が必要と判断される場合（ユーザー確認が必要）
</stop_conditions>

<stop_output>
停止時の必須アウトプット:
1. チケット docs/tasks/TASK_018_MergeConflictResolution.md を IN_PROGRESS/BLOCKED のまま更新  
   - 事実 / 根拠ログ要点 / 次手 1-3 件 / Report パスを必ず追記
2. docs/inbox/ に未完了レポートを作成し、調査結果・詰まり・次手を記録
3. 変更は commit する（push は GitHubAutoApprove=true の場合のみ自律実行）。push 不要時は「push pending」を明記
4. チャット 1 行: `Blocked: docs/tasks/TASK_018_MergeConflictResolution.md. Reason: <要点>. Next: <候補>. Report: docs/inbox/REPORT_TASK_018_MergeConflictResolution_20250112.md.`
5. MISSION_LOG.md を更新（停止理由と次手を記録）。
</stop_output>

<output_format>
納品レポート（docs/inbox/REPORT_TASK_018_MergeConflictResolution_20250112.md）フォーマット:
# Report: origin/master からのマージコンフリクト解決

**Timestamp**: <ISO8601>  
**Actor**: Worker  
**Ticket**: docs/tasks/TASK_018_MergeConflictResolution.md  
**Type**: Worker  
**Duration**: <所要時間>  
**Changes**: <変更量要約>

## 概要
- <作業の概要を記載>

## Changes
- <file>: <詳細変更内容（何をどう変更したか）>

## Decisions
- <decision>: <理由>

## Verification
- <command>: <result（成功/失敗とログ要点）>

## Risk
- <潜在リスク>

## Remaining
- なし / <残件>

## Blocked（State: BLOCKED の場合）
- Reason / Evidence / Options（1〜3）

## Handover
- Orchestrator への申し送り（次手・注意点・未解決事項）

## 次のアクション
- <次のアクションを記載>

## Proposals（任意）
- 担当外で気づいた改善案・次回タスク候補
</output_format>

<self_correction>
- ファイルパスは **動的に確認** すること（`ls`, `find`, `Test-Path` 等を使用）。ハードコード禁止。
- エラーが発生した場合は、MISSION_LOG.md に記録し、復旧手順を試行する。
- 3回試行しても解決しない場合のみ、状況と試行内容を整理してユーザーに判断を仰ぐ。
- MISSION_LOG.md は常に最新状態を保つこと。各フェーズ完了時に必ず更新する。
- マージコンフリクト解決時は、両方のブランチの変更を考慮すること。
- `modify/delete`コンフリクトの場合は、削除の意図を確認してから決定すること。
- 名前空間の変更に伴う参照の更新が必要な場合がある。
</self_correction>
```

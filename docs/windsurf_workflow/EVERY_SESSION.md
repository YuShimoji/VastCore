# EVERY_SESSION（毎回の運用SSOT / これだけ見れば迷わない）

目的: shared-workflows の運用を **毎回ブレずに**実行する。  
このファイルは「運用まとめ」の **Single Source of Truth (SSOT)**。他ドキュメントは詳細/背景/補足であり、矛盾した場合は本ファイルの方針を優先する。

---

## 0. 入口（必ずここから）

- **入口（運用者）**: `docs/windsurf_workflow/OPEN_HERE.md`
- **実運用の詳細（手順/期待結果）**: `docs/windsurf_workflow/OPERATIONS_RUNBOOK.md`
- **具体シナリオ（WritingPage等）**: `docs/windsurf_workflow/DEMO_SCENARIOS.md`

Submodule 運用（推奨）の場合は、上記パスの先頭に `.shared-workflows/` を付けて読む:
- 例: `.shared-workflows/docs/windsurf_workflow/EVERY_SESSION.md`

---

## 1. 非交渉ルール（毎回徹底）

- **変更は必ず commit し、必要なら push する**（作業終了時に「push済み/未push」を曖昧にしない）
- **プロジェクトをクリーンに保つ**（`git status -sb` がクリーンであることを確認してから完了を名乗る）
- **推奨対応で強力に進める**（ただし、破壊的/復旧困難操作は常に停止して合意を取る）
- **作業終了時は必ず**:
  - **完了/未完了**を明言する
  - **次にユーザーが返すべきテンプレ**を提示する（下記「4. 終了時テンプレ」）

---

## 2. 毎回の実行（最短ルート）

### Step A: 更新チェック（推奨）

Submodule を使っている場合（`.shared-workflows/` がある場合）:

- `node .shared-workflows/scripts/sw-update-check.js`

期待結果:
- `Behind origin/main: 0` なら最新
- `Behind origin/main: N` なら更新が必要（更新してから着手）

### Step B: 環境診断（推奨）

- `node .shared-workflows/scripts/sw-doctor.js --profile shared-orch-bootstrap --format text`

期待結果:
- 重大な ERROR が無い
- WARN は「理由が分かっていて許容できる」ものだけ

### Step C: ルール適用（Cursor / 推奨）

- `pwsh -NoProfile -File .shared-workflows/scripts/apply-cursor-rules.ps1 -ProjectRoot .`

期待結果:
- `.cursorrules` と `.cursor/rules.md` が配置される

### Step D: Orchestrator 起動（毎回これだけ）

- `.shared-workflows/prompts/every_time/ORCHESTRATOR_DRIVER.txt` をチャットに貼る  
  （再開も同じ Driver で良い）

---

### Step E: 終了時チェック（推奨）

- `node .shared-workflows/scripts/session-end-check.js --project-root .`

期待結果:
- `Result: OK`

補足:
- `--no-fetch` を付けると外部通信（fetch）を抑制できる

---

## 2.5 Windsurf 側の推奨ユーザー対応（毎回）

目的: 「AIが止まった/終わった気になる/指示無視」を、ユーザー側でも **機械的に判定**できるようにする。

- **ユーザーがやること（毎回）**:
  - `node .shared-workflows/scripts/sw-update-check.js`
  - `node .shared-workflows/scripts/sw-doctor.js --profile shared-orch-bootstrap --format text`
  - （Cursorなら）`pwsh -NoProfile -File .shared-workflows/scripts/apply-cursor-rules.ps1 -ProjectRoot .`
  - チャットに `.shared-workflows/prompts/every_time/ORCHESTRATOR_DRIVER.txt` を貼る

- **期待される結果（成功判定）**:
  - `sw-update-check`: `Behind origin/main: 0`
  - `sw-doctor`: ERROR が無い（WARN は理由が明確）
  - Orchestrator のチャット出力が **固定5セクションのみ**（追加セクション無し）
  - `## 次のアクション` に **ユーザー返信テンプレ（完了判定 + 選択肢1-3）** が含まれる

- **失敗と判断する結果（即STOPして再投入/再試行）**:
  - `Behind origin/main: N (N>0)`（submodule更新が必要）
  - `sw-doctor` が ERROR を出す（環境不備）
  - Orchestrator が固定5セクション以外を出力（例: 結論/作業評価）
  - `## 次のアクション` にユーザー返信テンプレが無い（= report-validator でも ERROR になる）
  - `git status -sb` が汚いのに「完了」と言う（完了条件違反）

- **終了時の推奨ユーザー返信（固定テンプレ）**:
  - 本ファイル「4. 終了時テンプレ」をそのまま使う

---

## 3. “同期が不要な仕組み”へ寄せる方針（散逸対策）

散逸しやすい情報（運用まとめ/ルール/手順/テンプレ）を「手で同期」しないため、次を原則とする:

- **入口は `OPEN_HERE.md` に一本化**（導線だけを短く保つ）
- **運用SSOTは本ファイルに一本化**（毎回の判断/テンプレはここ）
- **詳細は `OPERATIONS_RUNBOOK.md` に寄せる**（手順の背景/例/トラブルシュート）
- **検査はスクリプトに寄せる**（`sw-update-check` / `sw-doctor` / `report-validator`）

この方針に反する新規ドキュメント追加は禁止（必要なら本ファイルか Runbook を更新する）。

---

## 4. 終了時テンプレ（毎回ブレない / ユーザー返信用）

作業終了時（完了でも未完了でも）に、必ずこのテンプレを提示する。  
テンプレは **コピーしてそのまま送れる**ことが要件（内容が薄い場合は「未完了」を選ぶ）。

```text
【確認】完了判定: 完了 / 未完了

【状況】（1-3行）
- いま何が終わっていて、何が残っているか:

【次に私（ユーザー）が返す内容】以下から1つ選んで返信します:
1) 「選択肢1を実行して」: <ここに選択肢1>
2) 「選択肢2を実行して」: <ここに選択肢2>
3) 「選択肢3を実行して」: <ここに選択肢3>

【補足（任意）】
- 制約/優先度/締切:
```

### 完了を名乗る最低条件（強制）

- `git status -sb` がクリーン（`M`/`??` が無い）
- 変更がある場合は commit 済み
- push が必要なら push 済み（または GitHubAutoApprove=false で **push pending を明記**）
- Orchestrator/Worker のレポート保存が要求される場合は `report-validator` を通過済み

---

## 5. 改善提案（提案するべき“類する機能”）

- **(導入済み) session-end-check**: 終了時テンプレの有無 / git clean / push pending を検査し、NOT OK を出す簡易チェッカー（`scripts/session-end-check.js`）
- **(提案) docs-entrypoint-check**: `OPEN_HERE` が本ファイルと Runbook を参照しているか、Driver が固定5セクションと終了テンプレを要求しているかを検査する（散逸の早期検知）



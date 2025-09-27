# Missing Scripts 修復手順

この文書は、シーン/Prefab に残存する Missing Script を検出し、レポート化し、必要に応じて削除（修復）するまでの手順を示します。

- ツール: `Vastcore/Tools/Missing Script Repair`
- レポート出力先: `Documentation/QA/MISSING_SCRIPTS_REPORT.md`

---

## 1. 前提

- バージョン管理で作業ブランチに切り替え、最新に同期しておくこと。
- ツールは Editor 専用です。Unity Editor 上で操作してください。
- Missing Script の削除はコンポーネントを取り外すため、基本的に元に戻せません。実行前に必ずコミットを推奨します。

---

## 2. ツールの起動

1. Unity メニューから `Vastcore > Tools > Missing Script Repair` を開く。
2. ウィンドウ右上に `Missing Script 修復ツール` が表示されます。

---

## 3. 設定項目

- Prefabs をスキャン (t:Prefab)
  - `Assets/` 配下の全ての Prefab を走査します。
- Scenes をスキャン (t:Scene)
  - `Assets/` 配下の全ての Scene を走査します（Additive で一時的に開きます）。
- スキャン後に Missing Script を自動削除
  - ON の場合、検出結果に基づき一括で Missing Script を削除します。
  - リスク: 必要なコンポーネントまで取り外す可能性があります。まずは OFF でスキャン→レポート確認を推奨。
- スキャン結果をレポート出力
  - `Documentation/QA/MISSING_SCRIPTS_REPORT.md` に結果を Markdown で出力します。

---

## 4. 実行手順

1. 設定を確認し、`スキャン実行` を押下。
2. スキャン完了後、ウィンドウ下部に結果が一覧表示されます。
3. レポートを出力したい場合は `レポートを出力` を押下（自動出力をONにしていれば自動保存されます）。
4. Missing Script を削除する場合は `Missing Script を削除 (一括)` を押下。
   - 実行前ダイアログで確認があります。
   - Prefab/Scene を自動で保存します。

---

## 5. レポートの見方

- `Asset Path`: 対象アセット（Prefab/Scene）のパス
- `Object Path`: ヒエラルキー上の GameObject のフルパス
- `Missing Count`: 当該 GameObject にアタッチされていた Missing Script の数

---

## 6. 検証（Acceptance Criteria）

- シーン/Prefab を開いた際、Console に Missing Script/参照切れの警告が出ない。
- `Documentation/QA/MISSING_SCRIPTS_REPORT.md` に Missing Script 0 件、または許容レベル（後続作業で置換予定のもののみ）の状態である。
- Play 実行で Missing Script に起因するエラー/警告が発生しない。

---

## 7. よくある質問 / 注意

- ProBuilder パッケージ配下に差分がある
  - パッケージ配下は変更せず、Package Manager から再インポート（リセット）してください。
- 一部の Missing Script は、旧 UI/旧名前空間に由来
  - 現時点では一括削除を提供しています。今後のスプリントで 1:1 自動置換（旧→新コンポーネント）に対応予定です。

---

## 8. 参考
- `docs/ISSUES_BACKLOG.md`
- `docs/SPRINT_PLAN_01.md`
- `Documentation/Logs/DEV_LOG.md`
- `Documentation/QA/FUNCTION_TEST_STATUS.md`

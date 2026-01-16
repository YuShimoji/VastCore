# AI Context
- 最終更新: 2025-10-08T21:21:32+09:00
- 現在のミッション: フェーズ2: Sprint 02 [A3] 段階適用と検証（限定適用→ビルド/シーン動作確認→レポート反映）
- ブランチ: feat/ui-migration-a3-apply-menus
- 関連: Issue https://github.com/YuShimoji/VastCore/issues/17
- 進捗: 30% / ステータス: 着手（Menusスコープでプレビュー/適用確認、レポート更新）
- 次の中断可能点: PR 作成後、CI 成功待ち
## 決定事項
- 共有ワークフロー `ci-smoke.yml`/`sync-issues.yml` を再利用する。
- Node の簡易 dev server / smoke check を本リポジトリに追加して CI 成功を保証。
- 本プロジェクトの開発規約は中央ルールに準拠し、差分は `DEVELOPMENT_PROTOCOL.md` に記載。
- gh CLI 非導入環境の場合の PR 自動化が難しい → 存在すれば自動、なければ手動 PR 案内。

## Backlog（将来提案）
- Unity Editor 用の headless smoke（起動/アセンブリスキャン）を Actions に追加。
- Deform パッケージの asmdef 参照検証ジョブ追加。

## タスク管理（短期/中期/長期）

### 短期（Next）

- [pending] SW Doctor Rules Configuration Fix (ref: docs/tasks/TASK_019_FixSwDoctorRulesConfig.md, Status: OPEN)
- [pending] Namespace Consistency (Utils vs Utilities) (ref: docs/tasks/TASK_020_NamespaceConsistency.md, Status: OPEN)
- [pending] Merge Integration & Verification (ref: docs/tasks/TASK_021_MergeIntegrationCheck.md, Status: OPEN)

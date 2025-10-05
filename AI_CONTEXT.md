# AI Context
- 最終更新: 2025-10-05T04:18:04+09:00
- 現在のミッション: フェーズ2: Sprint 02 [A1] 旧→新 UI マッピング定義 (#11)
- ブランチ: feat/ui-mapping-a1
- 関連: Issue https://github.com/YuShimoji/VastCore/issues/11, PR TBD
- 進捗: 60% / ステータス: 実装中（設計書/テンプレ追加、PR準備）
- 次の中断可能点: PR 作成後、CI 成功待ち

## 決定事項
- 共有ワークフロー `ci-smoke.yml`/`sync-issues.yml` を再利用する。
- Node の簡易 dev server / smoke check を本リポジトリに追加して CI 成功を保証。
- 本プロジェクトの開発規約は中央ルールに準拠し、差分は `DEVELOPMENT_PROTOCOL.md` に記載。

## リスク/懸念
- gh CLI 非導入環境の場合の PR 自動化が難しい → 存在すれば自動、なければ手動 PR 案内。
- CI が Node 依存のため Unity 専用の検証は別途導入が必要 → 将来の拡張項目に記載。

## Backlog（将来提案）
- Unity Editor 用の headless smoke（起動/アセンブリスキャン）を Actions に追加。
- Deform パッケージの asmdef 参照検証ジョブ追加。

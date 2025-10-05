# AI Context
- 最終更新: 2025-10-05T16:50:19+09:00
- 現在のミッション: フェーズ2: Sprint 02 [A2] 検出スキャナの強化と半自動置換ツールの実装（完了）
- ブランチ: develop
- 関連: Issue https://github.com/YuShimoji/VastCore/issues/13, PR https://github.com/YuShimoji/VastCore/pull/14（マージ済）
- 進捗: 100% / ステータス: 完了（CIパス＆自動マージ）
- 次の予定: [A3] 段階適用と検証（限定適用→ビルド/シーン動作確認→レポート反映）

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

# DEVELOPMENT_PROTOCOL

## 1. ブランチ戦略
- `main`: 常にデプロイ可能・安定版。
- `develop`: 次期リリースの統合ブランチ。
- `feat/*`, `chore/*`, `fix/*`: 課題ごとの短命ブランチ。PR 経由で `develop` へ統合。

## 2. Issue/PR/コミット規約
- Issue: Goal/Scope/DoD/Risk/影響範囲/関連リンクを記載（中央ルール付録B）。
- PR: 概要/変更点/テスト/リスク/関連 Issue/中断可能点を記載（付録B）。
- コミット: Conventional Commits に準拠（例: `feat(ui): add mapping template`）。

## 3. CI/CD
- Node ベースの簡易サーバ（`scripts/dev-server.js`）とスモークチェック（`scripts/dev-check.js`）で CI 成功を保証。
- 将来: Unity Editor の headless 検証を追加予定（Backlog 参照）。

## 4. ドキュメント運用
- `CLAUDE.md`: セッション運用SSOT（プロジェクト文脈・開発ルール・仕様管理）。
- `docs/`: 設計/仕様/タスク。`docs/DOCS_INDEX.md` で全ドキュメントを索引管理。

## 5. セキュリティ/秘密情報
- 秘密情報はリポジトリに含めない。必要時は GitHub Secrets を使用。

## 6. ロールバック/バックアウト
- 重大問題時は PR リバートを優先。リリースタグに基づくロールバック手順を Issue に記録。

## 7. テスト方針（当面）
- CI: スモーク（起動/簡易健全性確認）。
- 手動: Unity を起動してエラーなしを確認、コアシーンの基本動作をチェック。
- 以降: PlayMode テスト/asmdef 参照検証を段階導入。

## 8. 監査/ログ
- PR/Issue に相関 ID を記載可能。重要作業は `CLAUDE.md` の DECISION LOG にメモ化。

## 9. Backlog
- Unity headless smoke の Actions 追加。
- Deform パッケージの asmdef 参照検証ジョブ。

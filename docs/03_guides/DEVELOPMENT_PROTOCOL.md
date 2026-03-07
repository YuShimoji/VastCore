# DEVELOPMENT_PROTOCOL

## 1. ブランチ戦略
- `main`: trunk-based 開発。常にデプロイ可能・安定版を維持。
- feature ブランチや develop ブランチは使用しない。

## 2. タスク管理/コミット規約
- タスク管理: `docs/tasks/TASK_*.md` で管理。GitHub Issue/PR は現在未使用。
- コミット: Conventional Commits に準拠（例: `feat(ui): add mapping template`）。

## 3. ビルド/検証
- 主な検証: Unity Editor でのコンパイル確認（エラー/警告ゼロを維持）。
- EditMode テスト: 75件（全 PASS 維持）。PlayMode テスト: 未着手。
- CI/CD: Node.js ベース CI は存在しない。将来的に Unity headless 検証を検討。

## 4. ドキュメント運用
- セッション SSOT: `CLAUDE.md`（プロジェクト文脈・開発ルール・DECISION LOG）。
- 設計/仕様: `docs/02_design/`, `docs/03_guides/`, `docs/04_reports/`。
- 索引: `docs/DOCS_INDEX.md` で全ドキュメントを管理。
- 仕様閲覧: `docs/spec-viewer.html` + `docs/spec-index.json`（21エントリ）。

## 5. セキュリティ/秘密情報
- 秘密情報はリポジトリに含めない。必要時は GitHub Secrets を使用。

## 6. ロールバック/バックアウト
- 重大問題時は `git revert` でロールバック。リリースタグに基づくロールバック手順を DECISION LOG に記録。

## 7. コンパイル管理
- 詳細: `docs/03_guides/COMPILATION_GUARD_PROTOCOL.md` を参照。
- 原則: コンパイルエラー/警告ゼロを常時維持。versionDefines で条件コンパイル管理。

## 8. 監査/ログ
- 重要な意思決定は `CLAUDE.md` の DECISION LOG に記録。
- タスク完了報告は `docs/04_reports/` に配置（TASK_XXXX_REPORT.md）。

## 9. Backlog
- Unity headless smoke テストの導入検討。
- PlayMode テストの段階的導入。

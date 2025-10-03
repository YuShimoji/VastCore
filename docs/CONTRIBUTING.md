# CONTRIBUTING

## ブランチ運用
- すべての機能開発・修正は専用 `feature/#<issue>-short-desc` ブランチで実施します。
- 統合は Pull Request を通して行います（base: `develop`）。

## CHANGELOG更新
- スプリント完了報告の最後に、プロジェクトルートの `CHANGELOG.md` にエントリを追記してください。
- フォーマット:
```
## [YYYY-MM-DD] - Sprint: [スプリント名]
* **概要:** [完了した作業の簡単な要約]
* **完了Issue:** [docs/issues/xx.md], [...]
* **関連PR:** [Pull RequestのURL（もしあれば）]
```

## コミット/PR
- Conventional Commits を推奨（例: `docs(tools): ...`, `feat(editor): ...`）。
- Issue をクローズする場合は `[closes #<issue>]` を付与。

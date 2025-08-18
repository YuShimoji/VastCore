# ドキュメント整理計画（プレースホルダ/レガシー記述の統合と削除）

## 目的
- 「2024-XX-XX」等のプレースホルダ日付や、過激/不適切な見出し（例: 【重大修正】仕様外実装…）を整理し、正確な履歴とわかりやすい設計意図に統一する。
- 重複・矛盾・古い情報を統合し、参照先を一本化してメンテ負担を減らす。

## 対象
- `DEV_LOG.md`（ルート）
- `Documentation/Logs/DEV_LOG.md`（履歴の重複・表現不統一の疑い）
- `FUNCTION_TEST_STATUS.md`（日付表記の統一と検証手順の更新）

## 問題パターン
- 日付のプレースホルダ: `2024-XX-XX`, `2024-12-XX` など
- 強い断定/非建設的表現: 「【重大修正】仕様外実装の削除…」等
- 内容の重複: ルートの `DEV_LOG.md` と `Documentation/Logs/DEV_LOG.md` の重複セクション

## 方針
1. プレースホルダ日付は原則削除または「年月のみ」へ弱める。確定日がある場合は実日付に置換。
2. 過激な見出しは中立で具体的な表現へ修正（例: 「開発方針の見直しとコード整理」）。
3. 歴史的経緯はルート `DEV_LOG.md` に集約。`Documentation/Logs/DEV_LOG.md` は廃止候補。
4. テスト手順や検証は `FUNCTION_TEST_STATUS.md` に一元化。

## 具体的作業
- ルート `DEV_LOG.md`
  - 2025-08-18のクリーンアップ開始エントリを追加（本計画の要約、ターゲット、検証手順へのリンク）。
  - プレースホルダ日付セクションは、必要に応じて「年-月」または無日付のテーマ見出しへリライト。
- `Documentation/Logs/DEV_LOG.md`
  - 重要な歴史的内容をルート `DEV_LOG.md` に統合（重複は統合先への内部リンクに変更）。
  - 統合作業完了後はファイルを削除（Git履歴に保存されるため復元可能）。
- `FUNCTION_TEST_STATUS.md`
  - 「Documentation Cleanup Verification」セクションを追加し、grepベースの自動検証手順を明文化。

## 検証手順（自動/半自動）
- 自動: リポジトリ直下で以下パターンのヒット件数が0であることを確認
  - `2024-XX-XX|2024-12-XX|重大修正|仕様外実装`（`Library/PackageCache/` は除外）
- 半自動: 
  - 主要ドキュメント（`DEV_LOG.md`, `FUNCTION_TEST_STATUS.md`）を目視確認し、見出し/口調/日付表記が統一されていること。
  - ドキュメント間リンクが存在すること（計画→ログ→テストの往復導線）。

## 検証ベースライン（2025-08-18）
本日取得したプレースホルダ/不適切表現の出現状況。完了時は 0 件を目標。

- `DEV_LOG.md`: 16件
- `Documentation/Logs/DEV_LOG.md`: 16件
- `Documentation/Planning/DOCUMENTATION_CLEANUP_PLAN.md`: 4件（説明用の引用であり許容）
- `Documentation/Planning/DEV_PLAN.md`: 3件
- `DEV_PLAN.md`: 2件
- `Documentation/QA/FUNCTION_TEST_STATUS.md`: 2件
- `FUNCTION_TEST_STATUS.md`: 2件

検索条件: 除外 `Packages/`, `ProjectSettings/`, `Library/`, `.git/`。正規表現 `(2024-XX-XX|2024-12-XX|重大修正|仕様外実装)`。

## 進行ログ / 相互参照
- ルート `DEV_LOG.md` に「ドキュメントクリーンアップ開始」を記録（2025-08-18）。
- `FUNCTION_TEST_STATUS.md` に「Documentation Cleanup Verification」セクションを追加（自動/手動検証手順、ベースライン、完了条件）。
- 本計画書は、検証項目の正本として維持し、完了時に最終サマリを追記。

次アクション:
- `Documentation/Logs/DEV_LOG.md` の重複・トーン不一致をルート `DEV_LOG.md` へ統合・要約。
- `DEV_PLAN.md` 類のプレースホルダ表現の書き換え（具体日付/段階表現へ）。

## ロールバック
- 誤った削除/修正は Git から復元可能。削除は必ず統合コミット後に行う。

## スケジュール
- フェーズ1（本コミット）: 計画作成、ログ/テスト文書のエントリ追加。
- フェーズ2: 内容統合・リライト（PRベース）。
- フェーズ3: `Documentation/Logs/DEV_LOG.md` の削除と検証完了報告。

## 参考
- `DEV_PLAN.md`, `REFACTORING_HANDOVER_DOCUMENT.md`（表現トーンと構成の参考）

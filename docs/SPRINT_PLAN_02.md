# Sprint 02 計画書 (提案)

## 1. 目的 / ゴール
- 旧 UI → `Vastcore.UI` への移行ルールを整備し、実運用の置換を安全に実行できる状態にする。
- ProBuilder 等のパッケージ配下の不要差分をリセットし、リポジトリの健全性を維持する。
- PlayMode テストを復帰し、継続的に実行できる基盤（ローカル/CI）を整える。
- Deform 統合の残課題（asmdef/defineConstraints/参照）を片付け、`DEFORM_AVAILABLE` 条件の挙動を安定化。

## 2. スコープ / 非スコープ
- スコープ
  - 旧 UI → `Vastcore.UI` の置換ルール策定・試験適用（部分的自動化を含む）
  - ProBuilder を含むパッケージ配下の差分リセット手順/スクリプト化
  - PlayMode テスト復帰（Unity Test Runner）、`ENABLE_VASTCORE_TESTS` ゲート整備
  - Deform 統合（asmdef の `references` / `versionDefines` 整備、条件付きコンパイル検証）
  - CI （GitHub Actions）での PlayMode テスト最低限の実行
- 非スコープ（次スプリント候補）
  - UI/置換ツールの完全自動化（GUID マッピング含む広範囲の置換）
  - 大規模なランタイム最適化（Terrain 系のフレーム時間制御・統計拡張の本実装）

## 3. 成果物（Deliverables）
- `docs/` 配下の設計・手順ドキュメント（置換ルール、パッケージリセット、テスト手順）
- Editor ツール（半自動 UI 置換ユーティリティの雛形 / 安全確認付き）
- asmdef 更新（Deform / Testing / UI 関連）
- CI ワークフロー（PlayMode 最低限実行 + レポート保存）

## 4. タスク一覧（WBS）
- A. 旧 UI → `Vastcore.UI` 移行
  1. 置換ルールの定義（旧名前空間/クラス → 新 `Vastcore.UI.*`）
  2. 検出用スキャナ（Guid/クラス名/名前空間の検出）
  3. 半自動置換ツール（安全確認ダイアログ付き）
  4. 一部シーン/Prefab で試験適用（サンプル → コアへ段階的）
  5. ドキュメント化（ロールバック手順含む）
- B. ProBuilder 等パッケージ差分リセット
  1. パッケージ配下の差分抽出・一覧化（自動レポート）
  2. リセット手順（Package Manager 再インポート / `.meta` 差分の扱い）
  3. `.gitignore` / 運用ルール整備（パッケージ配下は基本ノータッチ）
- C. PlayMode テスト復帰
  1. `Assets/Tests/PlayMode` ひな形整備（asmdef + サンプルテスト）
  2. `ENABLE_VASTCORE_TESTS` の管理（EditorUserBuildSettings ではなく asmdef の defineConstraints）
  3. CLI 実行手順の整備（`-runTests -testPlatform PlayMode`）
  4. ログ/レポート出力（`Documentation/QA/` に保存）
- D. Deform 統合安定化
  1. `versionDefines` の確認（`com.beans.deform` → `DEFORM_AVAILABLE`）
  2. asmdef の `references` 整備（参照不足の解消）
  3. 依存コードの条件付きコンパイル導線の再確認
  4. `DeformIntegrationTestScene` の Missing Script の扱い方針（保留 or 代替/スキップ）
- E. CI 最小構成（GitHub Actions）
  1. キャッシュ/ライセンス（Personal モード）を考慮した最小ジョブ
  2. PlayMode テスト実行とレポート保存（Artifacts）

## 5. 受け入れ基準（Acceptance Criteria）
- 旧 UI → `Vastcore.UI` の置換ルールが文書化され、指定シーン/Prefab へ安全に適用できる（差分レビュー可能）
- パッケージ配下の差分が 0（または既知の最小限）であることを自動レポートで確認
- PlayMode テストがローカル/CI の双方で実行でき、失敗時にレポートが確認できる
- Deform 統合が導入有無に応じてコンパイル/テスト継続可能（`DEFORM_AVAILABLE` 有効/無効の双方）

## 6. マイルストーン（目安）
- Week 1: A（ルール定義/スキャナ雛形）、B（差分レポート/リセット手順）、C（PlayMode ひな形/ローカル実行）
- Week 2: A（半自動置換の試験適用）、D（Deform 安定化）、E（CI で PlayMode 実行）

## 7. リスクと緩和策
- UI 置換で GUID が一致せず参照復元できない
  - 対策: クラス名/名前空間ベースの置換は「新規割当」扱い。対象アセットを限定、バックアップ/ロールバック手順を文書化
- パッケージ配下の差分が意図的変更と混在
  - 対策: 差分レポートで可視化し、必要なもののみ例外ルール化
- Deform の外部依存で CI が不安定
  - 対策: `versionDefines` と条件付きコンパイルで最小構成を担保。CI は Deform 無しモードで先行安定

## 8. テスト計画
- UI 置換: 変更前後のスクリーンショット比較 + コンソール警告/エラー 0 を確認
- ProBuilder: 再インポート後にシーンロード/Play で例外 0
- PlayMode: サンプルテスト（Terrain/Player 非依存の軽量ケース）を自動実行
- Deform: Deform 有/無の両構成でコンパイル確認（無の場合もビルド継続）

## 9. 運用・ブランチ戦略
- ブランチ: `restructure-2025-09-12` 派生で `sprint-02/*` ブランチを切り、小粒コミットで PR 単位を小さく運用
- GitHub Issues: タスクは全て Issue 化し、カンバンで進捗管理（本計画の小項目に応じて起票）

## 10. 参考
- `docs/ISSUES_BACKLOG.md`
- `Documentation/QA/FUNCTION_TEST_STATUS.md`
- `Documentation/QA/MISSING_SCRIPTS_REPAIR.md`
- `Assets/Editor/Tools/MissingScriptRepairWindow.cs`

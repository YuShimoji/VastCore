# Sprint 02 計画

- **[A] 旧UI → Vastcore.UI 置換ルール**
  - 目的: 旧 `NarrativeGen.UI` から新 `Vastcore.UI` への安全な移行（GUID維持・最小差分）。
  - 構成:
    - [A1] 旧→新 UI マッピング定義（ドキュメント/JSON雛形）
      - 成果物: `docs/ui-migration/OLD_TO_NEW_UI_MAPPING.md`, `docs/ui-migration/ui_mapping_rules.template.json`
      - DoD: ドキュメントとテンプレ追加、CI パス、Issue/PR 更新
      - 状態: 着手済（本PRで追加）
    - [A2] 検出スキャナの強化と半自動置換ツールの実装（dry-run/レポート出力）
      - JSONルールを読み込み、対象検出と変更プランを生成（適用はdry-run）
      - 状態: 完了（`UIMigrationRulesDryRunWindow.cs` を追加し、JSONルールに基づくドライランを実装、PR #14 マージ済）
    - [A3] 段階適用と検証
      - 限定適用→ビルド/シーン動作確認→レポート反映
      - 状態: 着手済（`UIMigrationApplyWindow.cs` 追加、限定適用プレビュー・適用を実装）

- **管理**
  - 追跡: Issue #11（A1）, Issue #13（A2）, Issue #17（A3）
  - ブランチ: `feat/ui-migration-a3`
  - CI: 共有ワークフロー `ci-smoke` によりスモーク実行
  - 実行メモ（A2）: Unity Editor のメニュー `Vastcore/Tools/UI Migration/Rules Dry-Run (JSON)` からレポート出力。
  - 実行メモ（A3）: Unity Editor のメニュー `Vastcore/Tools/UI Migration/Apply (A3 - staged)` にて、選択範囲またはフォルダに対してプレビュー→適用を実施。
  - CI: PR #14 によりマージ済

- **リスク/緩和**
  - 自動置換の誤変換 → A2はdry-run限定から開始、A3で限定適用
  - asmdef/rootNamespace の整合 → A1はルール定義のみ。適用はA2/A3で段階導入

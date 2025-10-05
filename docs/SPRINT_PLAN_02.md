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
      - 状態: 未着手（A1完了後に開始）
    - [A3] 段階適用と検証
      - 限定適用→ビルド/シーン動作確認→レポート反映
      - 状態: 未着手

- **管理**
  - 追跡: Issue #11（A1）
  - ブランチ: `feat/ui-mapping-a1`
  - CI: 共有ワークフロー `ci-smoke` によりスモーク実行

- **リスク/緩和**
  - 自動置換の誤変換 → A2はdry-run限定から開始、A3で限定適用
  - asmdef/rootNamespace の整合 → A1はルール定義のみ。適用はA2/A3で段階導入

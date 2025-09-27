# Issues Backlog (Vastcore)

本ドキュメントは現在の課題と対応計画のバックログです。GitHub Issues 起票の際は本内容をベースに同期してください。

## 概要（Mermaid）
```mermaid
graph LR
  A[Deform 統合] --> A1[asmdef versionDefines / references 整備]
  A --> A2[DEFORM_AVAILABLE 条件付きコンパイル確認]
  A2 --> A3[DeformIntegrationTest 安定化]

  B[UI/Namespace 整理] --> B1[シーン/Prefab Missing Script 修復]
  B --> B2[Modern UI と PerformanceMonitor 連携]

  C[テスト基盤] --> C1[Unity Test Runner PlayMode 構成]
  C --> C2[CI での TestAssemblies 参照保証]

  D[生成システム強化] --> D1[RuntimeGenerationManager 時間予算化]
  D --> D2[PrimitiveTerrainManager 最適化/統計]

  E[技術負債] --> E1[Obsolete API の一掃 (FindObjectOfType→FindFirst...)]
  E --> E2[不要ログ/ファイル削除]
```

## バックログ

1. Deform パッケージ統合の最終整備（asmdef/defineConstraints/参照確認）
   - 目的: `DEFORM_AVAILABLE` の有効化条件と型解決の完全一致
   - 成果: `Vastcore.Core`/`Vastcore.Testing` に `references: Deform` と `versionDefines: com.beans.deform→DEFORM_AVAILABLE` 追加済み
   - 残課題: シーン単位の Missing Script/Warn の洗い出し

2. シーン/Prefab の Missing Script 修復（UI 名前空間統一の反映）
   - 目的: `Vastcore.UI` への移行に伴う参照切れの根絶
   - 手順: 各シーンを開き、失われたコンポーネントを `ModernUIManager`/`SliderBasedUISystem` 等に差し替え

3. RuntimeGenerationManager/PrimitiveTerrainManager の本実装強化
   - 目的: タスク処理の時間予算・フレーム分散の最適化、統計出力の拡充
   - 受け入れ基準: 60FPS 近傍での安定、PerformanceMonitor とメトリクス連携

4. Test Runner/CI 整備
   - 目的: PlayMode テストの標準化と CI での自動実行
   - 手順: `-runTests -testPlatform PlayMode` での CLI 実行手順整備、レポート出力

5. Obsolete API/未使用フィールドの整理
   - 目的: CS0618/CS0414 の継続的削減
   - 手順: `FindObjectOfType` → `FindFirstObjectByType`、未使用メンバの削除/Conditional 化

6. プロジェクトルートの不要ログの削除（DEBUGLOG_*）
   - 目的: ルート直下の不要ファイルを削除しリポジトリ健全化
   - 実施: 本ドキュメント反映後に git rm 済み

## 参考
- `docs/ASSEMBLY_DESIGN.md`
- `docs/TEST_RUNNER_SETUP.md`
- `Documentation/Logs/DEV_LOG.md`
- `Documentation/QA/FUNCTION_TEST_STATUS.md`

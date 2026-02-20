# Task: PC-1 Deform パッケージ正式導入と統合検証
Status: OPEN
Tier: 2
Branch: feature/PC-1-deform-package-integration
Owner: Worker
Created: 2026-02-19T13:30:00+09:00
Report:
Milestone: LG-1

## Objective
- Deform をスタブ運用から正式パッケージ運用へ移行し、Editor/Runtime の統合動作を検証する。
- Phase C の開始条件を満たす実行可能チケットを先行定義する。

## Context
- 参照: `docs/01_planning/DEVELOPMENT_ROADMAP_2026.md:352`
- 依存: PA-1（完了済み）と PB-5（未着手）
- 本タスクは長期目標 LG-1 の先行起票（着手は依存解消後）。

## Focus Area
- `Packages/manifest.json`
- `Assets/Scripts/Deform/DeformStubs.cs`（削除判断を含む）
- `Assets/Scripts/Generation/DeformIntegration*.cs`
- `Assets/Scripts/Core/DeformPresetLibrary.cs`（PB-5 後は Generation 側）
- `Assets/Editor/StructureGenerator/Tabs/Deform/DeformerTab.cs`
- `Assets/Scripts/Testing/DeformIntegrationTestRunner.cs`

## Forbidden Area
- `.shared-workflows/` 配下（submodule）
- 依存タスク未完了状態での強行実装（PB-5 未完了時の広範囲変更）
- Deform 以外の機能拡張

## Target Assemblies
- `Vastcore.Generation`
- `Vastcore.Editor`
- `Vastcore.Core`（移行期間のみ）
- `Vastcore.Testing`
- 参照: `docs/02_design/ASSEMBLY_ARCHITECTURE.md`

## Constraints
- 依存タスク（特に PB-5）完了前は設計・検証手順の整備までに留める。
- パッケージ導入に伴う asmdef/namespace 変更は最小化し、根拠を記録する。
- 条件コンパイル撤去は段階的に行い、ビルド不能期間を作らない。

## Test Plan
- **テスト対象**: Bend/Noise/Scale Deformer と DeformerTab 操作フロー
- **EditMode テスト**: DeformIntegration 系ユニットテスト
- **PlayMode テスト**: Deformer 適用シーンでの変形・アニメーション確認
- **ビルド検証**: Unity Editor コンパイル成功
- **期待結果**: スタブ不要状態で主要 Deformer が再現可能
- **テスト不要の場合**: 該当なし

## Impact Radar
- **コード**: Deform 統合層と Editor UI
- **テスト**: Integration test 拡充
- **パフォーマンス**: Deformer 実行負荷
- **UX**: StructureGenerator の変形操作性
- **連携**: Package 管理と asmdef 参照
- **アセット**: Deform 関連プリセット
- **プラットフォーム**: Unity バージョン/パッケージ互換性

## DoD
- [ ] 依存条件（PA-1, PB-5）が満たされ着手可能になっている
- [ ] Deform パッケージ導入方針が確定している
- [ ] スタブ撤去可否と移行手順が定義されている
- [ ] Unity Editor で主要 Deformer 動作が確認されている
- [ ] `docs/inbox/REPORT_PC-1_*.md` を作成し Report 欄に反映

## Notes
- 長期タスクのため、着手前に再見積もり（Tier/Size）を必須とする。
- PB-5 完了時に Focus Area を再確定する。

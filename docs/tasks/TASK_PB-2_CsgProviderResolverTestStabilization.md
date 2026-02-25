# Task: PB-2 CsgProviderResolver テスト安定化
Status: CLOSED
Tier: 1
Branch: feature/PB-2-csg-provider-resolver-stabilization
Owner: Worker
Created: 2026-02-24T06:10:00+09:00
Completed: 2026-02-25T14:30:00+09:00
Report: docs/04_reports/REPORT_PB-2_CsgProviderResolverTestStabilization.md
Milestone: MG-1

## Objective
- EditMode の既知失敗 `CsgProviderResolverSmokeTests` を安定化し、MG-1 の「テスト全通過」条件を回復する。

## Context
- 直近実行結果: `75 total / 74 passed / 1 failed`
- 失敗対象: `Assets/Tests/EditMode/CsgProviderResolverSmokeTests.cs`
- 失敗内容: null 期待違反 (`Expected: not null, But was: null`)

## Focus Area
- `Assets/Tests/EditMode/CsgProviderResolverSmokeTests.cs`
- `Assets/Editor/StructureGenerator/Utils/CsgProviderResolver.cs`
- `Assets/Editor/StructureGenerator/Tabs/Editing/CompositionTab.cs`（必要最小の参照整合のみ）

## Forbidden Area
- `.shared-workflows/` 配下（submodule）
- CSG 以外の Editor 機能拡張
- Runtime 機能仕様の変更

## Target Assemblies
- `Vastcore.Tests.EditMode`
- `Vastcore.Editor.StructureGenerator`
- 参照: `docs/02_design/ASSEMBLY_ARCHITECTURE.md`

## Constraints
- まず失敗原因を特定し、場当たり修正を禁止する。
- 既存テストの意図（null入力時のフォールバック検証）を維持する。
- asmdef 変更が発生した場合は `ASSEMBLY_ARCHITECTURE.md` を同時更新する。

## Test Plan
- **Compile**: `./scripts/check-compile.ps1`
- **EditMode**: `./scripts/run-tests.ps1 -TestMode editmode`
- **期待**: `CsgProviderResolverSmokeTests=Passed` かつ全体 Fail=0
- **結果記録**: `class=result` 形式でレポート化
- **PlayMode**: 本タスク対象外（理由をレポートに明記）

## Impact Radar
- **コード**: CSG resolver 周辺のみ
- **テスト**: EditMode 回帰の安定化
- **パフォーマンス**: 影響軽微
- **UX**: 回帰確認の信頼性向上
- **連携**: Editor asmdef 参照整合

## DoD
- [ ] 失敗原因が再現手順付きで特定されている
- [ ] `CsgProviderResolverSmokeTests` が Pass
- [ ] EditMode 全体 Fail=0 を確認
- [ ] Unity Editor コンパイル成功
- [ ] `docs/inbox/REPORT_PB-2_CsgProviderResolverTestStabilization.md` 作成
- [ ] Report 欄が更新されている

## Notes
- PB-1 で追加した基盤テスト群は Pass 維持を前提とする。

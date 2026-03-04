# Worker Prompt: TASK_PB-2_CsgProviderResolverTestStabilization

## Ticket
- `docs/tasks/TASK_PB-2_CsgProviderResolverTestStabilization.md`
- Milestone: `MG-1`

## Scope
- Focus Area のみ変更すること:
  - `Assets/Tests/EditMode/CsgProviderResolverSmokeTests.cs`
  - `Assets/Editor/StructureGenerator/Utils/CsgProviderResolver.cs`
  - `Assets/Editor/StructureGenerator/Tabs/Editing/CompositionTab.cs`（必要最小のみ）
- Forbidden Area は厳守。Runtime 機能仕様変更は禁止。

## Target Assemblies
- `Vastcore.Tests.EditMode`
- `Vastcore.Editor.StructureGenerator`
- 参照: `docs/02_design/ASSEMBLY_ARCHITECTURE.md`

## Test Plan
- 失敗 1 件の原因を先に特定する（場当たり修正禁止）。
- `./scripts/check-compile.ps1` を実行し成功を確認。
- `./scripts/run-tests.ps1 -TestMode editmode` を実行し Fail=0 を確認。
- テスト結果を `class=result` 形式でレポート化。
- PlayMode は対象外。未実施理由を記載。

## Deliverables
1. 実装変更（Focus Area 内）
2. レポート: `docs/inbox/REPORT_PB-2_CsgProviderResolverTestStabilization.md`
3. チケット状態更新（`OPEN` -> `IN_PROGRESS` / `DONE` / `BLOCKED`）

## Stop Conditions
- コンパイル不可のまま終了しない。
- BLOCKED 時は原因、再現手順、次の最小アクションを必ず記載。

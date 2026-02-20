# Worker Prompt: TASK_PB-1_NUnitTestFoundation

## Ticket
- `docs/tasks/TASK_PB-1_NUnitTestFoundation.md`
- Milestone: `MG-1`

## Scope
- Focus Area のみ変更すること:
  - `Assets/Scripts/Testing/Vastcore.Testing.asmdef`
  - `Assets/Scripts/Testing/EditMode/CoreTests/`
  - `Assets/Scripts/Testing/EditMode/GenerationTests/`
  - `Assets/Scripts/Testing/EditMode/TerrainTests/`
  - `Assets/Scripts/Testing/VastcoreIntegrationTestStubs.cs`
- Forbidden Area は厳守。Runtime 機能仕様変更は禁止。

## Target Assemblies
- `Vastcore.Testing`
- `Vastcore.Core`
- `Vastcore.Generation`
- `Vastcore.Terrain`
- 参照: `docs/02_design/ASSEMBLY_ARCHITECTURE.md`

## Test Plan
- EditMode NUnit テストを最小有効セットで追加/整理。
- Unity Editor コンパイル成功を確認。
- テスト実行結果を `class=result` 形式でレポート化。
- PlayMode は対象外。未実施理由をレポートに明記。

## Impact Radar
- Code: テストコードと `Vastcore.Testing.asmdef`
- Test: Test Runner の認識範囲拡大
- Performance: テスト時間増加の可能性
- UX: 回帰確認フローの改善
- Integration: Core/Generation/Terrain 参照整合

## Deliverables
1. 実装変更（Focus Area 内）
2. レポート: `docs/inbox/REPORT_PB-1_NUnitTestFoundation.md`
3. チケット状態更新（`OPEN` -> `IN_PROGRESS` / `DONE` / `BLOCKED`）

## Stop Conditions
- コンパイル不可のまま終了しない。
- BLOCKED 時は原因、再現手順、次の最小アクションを必ず記載。

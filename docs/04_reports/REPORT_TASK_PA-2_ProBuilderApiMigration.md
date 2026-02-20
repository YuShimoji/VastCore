# Report: TASK_PA-2 ProBuilder API Migration

## Metadata
- Task ID: PA-2
- Date: 2026-02-20
- Status: DONE
- Related Ticket: `docs/tasks/TASK_PA-2_ProBuilderApiMigration.md`

## Summary
- ProBuilder API 変更で壊れていた Subdivide / Mesh rebuild 経路を現行 API に移行した。
- `ConnectFaceElements` / `RebuildWithMesh` 依存を除去し、代替 API に統一した。
- Unity batch compile (`scripts/check-compile.ps1`) が最終的に成功し、PA-5 進行条件を満たした。

## Implemented Changes
1. `Assets/Scripts/Generation/Map/PrimitiveModifier.cs`
- `ConnectFaceElements` 呼び出しを `ConnectElements.Connect(mesh, mesh.faces)` に置換。

2. `Assets/Scripts/Generation/Map/PrimitiveTerrainGenerator.cs`
- `RebuildWithMesh` 呼び出しを `MeshImporter(...).Import()` + `ToMesh()/Refresh()` に移行。
- Subdivide 経路を `ConnectElements.Connect` ベースへ更新。

3. `Assets/Scripts/Terrain/Map/HighQualityPrimitiveGenerator.cs`
- Subdivide 経路を `ConnectElements.Connect` へ統一。
- `RebuildWithMesh` 依存を除去し `MeshImporter` 経由へ移行。
- 非公開/非互換 API 呼び出しを現行 API で置換し、処理フローを維持。

## Assembly Integrity Check
- Target Assemblies 内で修正を完結。
- 追加 using と asmdef 参照の整合を確認。
- 同名型重複の追加なし（既存型の置換のみ）。

## Validation
- Command: `.\scripts\check-compile.ps1`
- Result: `Compilation check passed.`（Exit Code 0）
- Log: `artifacts/logs/compile-check.log`

## Test Notes
- EditMode/PlayMode の網羅実行は本タスクでは未実施（コンパイル復旧を優先）。
- 追加回帰テストは PB-1（NUnit 基盤整備）で実施する。

## Risks / Follow-ups
1. 高品質プリミティブ見た目品質の回帰は PlayMode 実確認が必要。
2. PB-1 で最小回帰テストを追加し、API 移行後の安定性を担保する。

# Task: PB-1 NUnit テスト基盤の構築
Status: OPEN
Tier: 1
Branch: feature/PB-1-nunit-test-foundation
Owner: Worker
Created: 2026-02-19T13:30:00+09:00
Report:
Milestone: MG-1

## Objective
- EditMode 中心の NUnit テスト基盤を整備し、Phase B の品質基盤を開始する。
- Core/Generation/Terrain の主要ロジックに最小回帰テストを配置する。

## Context
- 参照: `docs/01_planning/DEVELOPMENT_ROADMAP_2026.md:259`
- Phase A のテストファイル再配置（PA-4）を前提に、テスト実行性を引き上げる。

## Focus Area
- `Assets/Scripts/Testing/Vastcore.Testing.asmdef`
- `Assets/Scripts/Testing/EditMode/CoreTests/`
- `Assets/Scripts/Testing/EditMode/GenerationTests/`
- `Assets/Scripts/Testing/EditMode/TerrainTests/`
- `Assets/Scripts/Testing/VastcoreIntegrationTestStubs.cs`

## Forbidden Area
- `.shared-workflows/` 配下（submodule）
- `ProjectSettings/`, `Packages/`（相談なし変更禁止）
- Runtime 機能の仕様変更

## Target Assemblies
- `Vastcore.Testing`
- `Vastcore.Core`
- `Vastcore.Generation`
- `Vastcore.Terrain`
- 参照: `docs/02_design/ASSEMBLY_ARCHITECTURE.md`

## Constraints
- テストは最小有効セットを優先し、網羅化は PB-2 以降で拡張する。
- テストのために本番ロジックを広範囲改変しない。
- asmdef 参照変更が必要な場合は `ASSEMBLY_ARCHITECTURE.md` 同時更新。
- 条件コンパイルと C# 9.0 制約を遵守。

## Test Plan
- **テスト対象**: SystemManager/ErrorHandler/PrimitiveGenerator/TerrainChunkPool/MarchingSquares
- **EditMode テスト**: 新規 NUnit テストクラスを追加し全件実行
- **PlayMode テスト**: このタスクでは対象外（PB-2 で拡張）。理由をレポートに記載
- **ビルド検証**: Unity Editor コンパイル成功
- **期待結果**: EditMode テストが安定実行可能、失敗時は再現手順と原因を記録
- **テスト不要の場合**: 該当なし

## Impact Radar
- **コード**: テストコードとテスト asmdef
- **テスト**: Test Runner の認識範囲拡大
- **パフォーマンス**: テスト時間増加
- **UX**: 開発者の回帰確認体験改善
- **連携**: Core/Generation/Terrain への参照整合
- **アセット**: 原則影響なし
- **プラットフォーム**: CI 連携時の実行条件に影響

## DoD
- [ ] EditMode テストディレクトリ/クラスが作成されている
- [ ] `Vastcore.Testing.asmdef` が必要参照を満たしている
- [ ] `VastcoreIntegrationTestStubs.cs` の扱いを明確化（置換 or 正式モック）
- [ ] Unity Editor でコンパイル成功
- [ ] EditMode テスト結果を `class=result` 形式で記録
- [ ] `docs/inbox/REPORT_PB-1_*.md` を作成し Report 欄に反映

## Notes
- PB-2/PB-3 の前提タスク。
- PB-1 完了時に MG-1 進捗を更新する。

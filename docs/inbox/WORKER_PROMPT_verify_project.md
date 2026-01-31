# Worker Prompt: Project Verification (TASK_022 & TASK_021)

## 参照
- チケット: 
  - docs/tasks/TASK_022_FixCyclicDependencies.md
  - docs/tasks/TASK_021_MergeIntegrationCheck.md
- SSOT: docs/Windsurf_AI_Collab_Rules_latest.md
- HANDOVER: docs/HANDOVER.md
- MISSION_LOG: .cursor/MISSION_LOG.md

## 目的
プロジェクトのクリーン状態を確認し、検証テストを実行する。

## 手順
1. **Compilation Check (TASK_022)**
   - Unity Editor または `asmdef` の整合性を確認し、コンパイルエラーがないことを検証する。
   - `Assets/MapGenerator/Scripts/TerrainGenerator.cs` など、直近で競合解消されたファイルの正当性を確認する。

2. **Integration Test (TASK_021)**
   - `run_tests.ps1` または Unity Test Runner を実行する。
   - テスト実行インフラの問題（前回 BLOCKED 要因）が解決しているか確認する。
   - 失敗する場合はログを分析し、修正案を提示する（修正が容易なら適用する）。

3. **Report**
   - 検証結果をレポートにまとめる。

## 境界
- Focus Area:
  - Assembly Definitions (`.asmdef`)
  - Testing Infrastructure (`Packages/manifest.json`, `packages-lock.json`)
  - Integration Tests (`Assets/Scripts/Testing/`)
- Forbidden Area:
  - Feature implementation (Voxel, etc.) - Verify ONLY.

## DoD
- [ ] コンパイルが通ることを確認
- [ ] テストが実行可能であることを確認 (All Pass or Report Failures)
- [ ] レポート作成: `docs/inbox/REPORT_VERIFICATION_20260130.md`

## 停止条件
- 重大なコンパイルエラーで修復困難な場合
- 新たな循環依存が見つかり、大規模なリファクタリングが必要な場合

## 納品先
- `docs/inbox/REPORT_VERIFICATION_20260130.md`

# Change: Compilation Guard Cleanup (ProBuilder/Parabox/Tests)

- Change ID: compilation-guard-cleanup
- Tier: 2 (中リスク: ビルド設定/asmdef/条件コンパイルの変更)
- Goal: ProBuilder/Parabox 依存の有無に関わらずプロジェクトが常にビルド可能となるよう、条件コンパイルと asmdef を整理する。テストは通常ビルドから除外する。

## Scope

- ProBuilder, Parabox.CSG に依存する Editor/Tests スクリプトを条件ガード。
- asmdef の `versionDefines` / `defineConstraints` / `autoReferenced` を整理。
- MapGenerator の API 非互換（Trees/Profiler）に伴うビルドエラー修正。

## Changes

- Tests
  - `Assets/Tests/EditMode/AdvancedStructureTestRunner.cs`: `#if UNITY_EDITOR && HAS_PROBUILDER` でガード。
  - `Assets/Tests/EditMode/BooleanTest.cs`: `#if HAS_PROBUILDER && HAS_PARABOX_CSG` でガード、`#endif` 誤配置修正。
  - 新規 `Assets/Tests/EditMode/Vastcore.Tests.EditMode.asmdef`: `defineConstraints: ["UNITY_INCLUDE_TESTS"]`, `versionDefines` で ProBuilder 定義。
- Editor/Structure
  - `Assets/Editor/StructureGenerator/Vastcore.Editor.StructureGenerator.asmdef`: `versionDefines.expression` を `">=6.0.0"` から `"6.0.0"` へ修正。
- Utilities/Logging
  - `Assets/MapGenerator/Scripts/*` で `LoadProfiler` 利用箇所に `using Vastcore.Utils;` を追加。
- Terrain API 整理
  - `HeightmapTerrainGeneratorWindow.cs`・`TerrainGenerator.cs`: `UnityEngine.Terrain(Data)` をフル修飾化。
  - `TreeGenerator.cs`: `AddTreeInstance` → `SetTreeInstances` へ修正。
- asmdef 整理
  - `Vastcore.Camera.asmdef`: `Vastcore.Utilities` 参照へ更新。
  - `Vastcore.Terrain.asmdef`: 不存在参照 `Vastcore.Diagnostics` を削除。

## Acceptance Criteria

- ProBuilder/Parabox が未導入の環境でもコンパイルが成功する。
- `UNITY_INCLUDE_TESTS` 無効時、EditMode テストはコンパイル対象外。
- MapGenerator の `Texture/Tree/Terrain` 生成が API エラーなくコンパイルできる。

## Test Plan

- Unity Editor でスクリプトリロード後、Console にエラーが無いこと。
- `EditMode` テスト asmdef が `UNITY_INCLUDE_TESTS` 有効時のみコンパイルされること。
- `HeightmapTerrainGeneratorWindow` から地形生成が実行可能（簡易手動テスト）。

## Risk & Rollback

- asmdef/条件コンパイルの変更により依存関係の漏れが出る可能性。問題発生時は直前コミットへロールバック可能。

## Notes

- Parabox.CSG は現状導入前提ではないため、`HAS_PARABOX_CSG` で完全ガード。将来導入時は Scripting Define Symbols に追加して有効化する。

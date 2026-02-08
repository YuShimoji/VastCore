# 申し送り: Compilation Guard Cleanup（ProBuilder/Parabox/Tests）

- 変更ID: compilation-guard-cleanup
- フェーズ: 完了（PR 作成・レビュー待ち）
- リスク分類 (Tier): 2（ビルド設定/asmdef/条件コンパイル）

## 目的

ProBuilder/Parabox の有無にかかわらず常にビルド可能な状態を作り、EditMode テストを通常ビルドから除外する。

## 実施内容（サマリ）

- 条件コンパイル
  - `AdvancedStructureTestRunner.cs`: `#if UNITY_EDITOR && HAS_PROBUILDER`
  - `BooleanTest.cs`: `#if HAS_PROBUILDER && HAS_PARABOX_CSG`（誤った `#endif` を修正）
- テスト asmdef 追加
  - `Assets/Tests/EditMode/Vastcore.Tests.EditMode.asmdef`
    - `defineConstraints: ["UNITY_INCLUDE_TESTS"]`
    - `versionDefines` で ProBuilder 導入時のみ `HAS_PROBUILDER` を定義
- Editor asmdef 修正
  - `Vastcore.Editor.StructureGenerator.asmdef` の `versionDefines.expression` を `">=6.0.0"` → `"6.0.0"`
- Utilities/Logging
  - `LoadProfiler` 利用ファイルで `using Vastcore.Utils;` を追記
- Terrain API 整理
  - `HeightmapTerrainGeneratorWindow.cs` / `TerrainGenerator.cs`: `UnityEngine.Terrain(Data)` をフル修飾
  - `TreeGenerator.cs`: `AddTreeInstance` → `SetTreeInstances`
- asmdef 整理
  - `Vastcore.Camera.asmdef`: `Vastcore.Utilities` に参照統一
  - `Vastcore.Terrain.asmdef`: 不在参照 `Vastcore.Diagnostics` を削除

## 影響範囲

- Editor/Tests のみ条件コンパイルで制御。通常ランタイムは非影響。
- MapGenerator（Editor 実行を含む）の API 呼び出しが現行 Unity に整合。

## 動作確認（手順）

1. Unity を開く（スクリプト自動リロード）
2. Console にエラーが無いことを確認
3. Test Runner で `EditMode` アセンブリが `UNITY_INCLUDE_TESTS` 有効時のみコンパイルされることを確認
4. メニュー `Tools/Vastcore/Heightmap Terrain Generator` → 簡易地形生成が完了すること

## 残課題（次回）

- `VastcoreGameManager.cs` での `Vastcore.Utilities`/`using Vastcore.Utils;` 整合の再確認
- 警告整理（CS0414/CS0219）
- Parabox.CSG を導入する場合の Scripting Define Symbols 設定ガイド整備

## ロールバック

- 当該変更は asmdef/条件コンパイル中心。問題発生時は直前コミットへロールバック可能。

## 参照

- OpenSpec: `openspec/changes/compilation-guard-cleanup/specs/overview.md`, `tasks.md`

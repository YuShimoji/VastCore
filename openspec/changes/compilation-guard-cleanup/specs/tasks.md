# Tasks

- [Major] 条件コンパイル/asmdefの再設計
  - ProBuilder 依存コード: `HAS_PROBUILDER` でガード
  - Parabox.CSG 依存コード: `HAS_PARABOX_CSG` でガード
  - EditMode テスト: `UNITY_INCLUDE_TESTS` で除外（asmdef）
  - versionDefines: ProBuilder の式を Unity 正式表記に変更（例: `6.0.0`）

- [Medium] MapGenerator の API 非互換対応
  - Tree: `AddTreeInstance` → `SetTreeInstances`
  - Profiler: `LoadProfiler` の using 整理（`Vastcore.Utils`）
  - Terrain API: `UnityEngine.Terrain(Data)` をフル修飾

- [Medium] asmdef 参照の統一
  - `Vastcore.Utilities` に統一（旧 `Vastcore.Utils` 参照のasmdef修正）
  - 不存在参照の削除（`Vastcore.Diagnostics`）

- [Low] 警告整理計画（後段）
  - CS0414 / CS0219: 未使用フィールド/変数の整理

## Implementation Steps

1) EditMode テスト asmdef 新規作成
2) ProBuilder/Parabox 依存の条件ガード作成・修正
3) versionDefines/defineConstraints/autoReferenced を整備
4) MapGenerator API 修正（Tree/Profiler/Terrain）
5) コンパイル確認、手動テスト（地形生成）
6) PR 作成、CI 通過確認

## Acceptance Checklist

- [ ] ProBuilder/Parabox 無しでコンパイル成功
- [ ] `UNITY_INCLUDE_TESTS` 無効時に EditMode テストが非コンパイル
- [ ] `HeightmapTerrainGeneratorWindow` の地形生成が実行可能
- [ ] CI 成功（ビルド/静的解析/セキュリティ）

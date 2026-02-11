# Report: TASK_033 DualGrid HeightMap Profile Mapping Implementation

## Metadata
- Task ID: TASK_033
- Date: 2026-02-11
- Author: Worker (Claude Code)
- Branch: main (feature branch not created; see Constraints)
- Commit: pending
- Status: DONE

## Goal
Implement profile-driven coordinate mapping in DualGrid height sampling per `DUALGRID_HEIGHTMAP_PROFILE_MAPPING_SPEC.md`.

## Changed Files
1. `Assets/Scripts/Generation/DualGridHeightSamplingEnums.cs` — **新規作成**: `DualGridUvAddressMode` (Clamp/Wrap) と `DualGridHeightQuantization` (FloorToInt/RoundToInt/CeilToInt) enum を追加。
2. `Assets/Scripts/Generation/DualGridHeightSamplingSettings.cs` — **新規作成**: `[Serializable]` 設定クラス。`UseProfileBounds`, `WorldMinXZ`, `WorldMaxXZ`, `UvAddressMode`, `HeightQuantization` フィールドを保持。
3. `Assets/Scripts/Generation/TerrainGenerationProfile.cs` — **変更**: `DualGridHeightSamplingSettings` フィールドを追加。`ResetToDefaults()` と `CopyFrom()` を更新。
4. `Assets/Scripts/Terrain/DualGrid/VerticalExtrusionGenerator.cs` — **変更**: `WorldToSampleIndex()` と `QuantizeHeight()` ヘルパーを抽出。`GenerateFromHeightMap` / `GenerateFromHeightMapArray` にオプショナル `_samplingSettings` パラメータを追加。

## Implementation Summary
1. **Enums**: スペック通り `DualGridUvAddressMode` と `DualGridHeightQuantization` を `Vastcore.Generation` namespace に配置。
2. **Settings container**: `DualGridHeightSamplingSettings` を `[Serializable]` クラスとして作成。デフォルト値はスペックに記載された値 (`WorldMinXZ=(-10,-10)`, `WorldMaxXZ=(10,10)`, Clamp, RoundToInt) を使用。
3. **Profile integration**: `TerrainGenerationProfile` に `m_DualGridHeightSampling` SerializeField を追加。Inspector から設定可能。
4. **Mapping logic**: `VerticalExtrusionGenerator` にプロファイル駆動のマッピング式を実装:
   - `InverseLerp` でワールド→UV 変換
   - `Clamp01` / `Repeat` で UV アドレッシング
   - `FloorToInt(u * width)` でサンプルインデックス算出
   - `QuantizeHeight` で量子化ポリシー適用
5. **Legacy fallback**: `_samplingSettings == null` または `UseProfileBounds == false` の場合、従来の固定 -10～10 レンジ + RoundToInt が使用される。

## Compatibility Notes
- Legacy behavior: 既存の呼び出し元 (`GridDebugVisualizer`, `HeightMapGenerator`) は `_samplingSettings` を渡さないため、自動的にレガシーパスを通る。API 互換性は完全に維持。
- Fallback behavior: `UseProfileBounds = false` を設定すると、プロファイルオブジェクトを渡しても従来の固定レンジにフォールバックする。

## Validation
- Performed: 静的コードレビュー（API 互換性、マッピング式の正確性、enum/settings のスペック準拠）
- Deferred: Unity Editor でのコンパイル確認、ランタイムテスト、エッジケーステスト（Wrap モード、非対称バウンズ、閾値での量子化）
- Reason for deferred items: タスクチケットの Constraints に「Unity Editor compile/test は本タスクでは必須にしない」と記載。

## Risks / Blockers
1. Unity コンパイルが未検証のため、typo やアセンブリ参照の問題が残存する可能性がある。次のタスクで速やかにコンパイル検証を推奨。
2. `GenerateFromNoise` は今回の scope 外（プロファイル駆動化の対象外）。必要に応じて将来タスクで対応。

## Next Actions
1. Unity Editor でコンパイル確認を実施する。
2. `WorldToSampleIndex` / `QuantizeHeight` に対するユニットテストを追加する（Test Matrix 項目: bounds/UV mode/quantization/regression）。
3. 必要に応じて `GenerateFromNoise` にも同様のプロファイル駆動化を適用する。

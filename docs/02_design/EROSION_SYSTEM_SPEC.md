> **上位SSOT**: [SSOT_WORLD.md](../SSOT_WORLD.md) | **索引**: [spec-index.json](../spec-index.json) SP-016

# Erosion System Spec

**Status:** 実装済み (PC-4)
**Version:** v1.0
**Last Updated:** 2026-03-17

---

## 概要

HeightMap にリアルな侵食効果を適用する。TerrainChunk.Build パイプライン内で HeightmapProvider 後に実行。

## アーキテクチャ

```
TerrainChunk.Build
  └── HeightmapProvider.GetHeights()
       └── HydraulicErosion.Apply()  (optional)
       └── ThermalErosion.Apply()    (optional)
            └── terrain.SetHeights()
```

## コンポーネント

### ErosionSettings (ScriptableObject)

| パラメータ | 型 | デフォルト | 説明 |
|-----------|-----|----------|------|
| enabled | bool | true | エロージョン全体の有効/無効 |
| erosionSeed | int | 42 | シード値 |
| enableHydraulic | bool | true | 水力エロージョン有効 |
| hydraulicIterations | int | 50000 | 水力反復回数 (1000-200000) |
| erosionRate | float | 0.3 | 侵食率 (0-1) |
| depositionRate | float | 0.3 | 堆積率 (0-1) |
| enableThermal | bool | true | 熱エロージョン有効 |
| thermalIterations | int | 50 | 熱反復回数 (1-200) |
| talusAngle | float | 0.6 | 安息角タンジェント (0.1-2.0) |

### HydraulicErosion

粒子ベース水力侵食。ドロップレットが地形上を流れ、侵食・堆積を行う。

- 入力: `float[,] heights`, `ErosionSettings`, `int seed`
- Pure C# 実装 (Unity API 非依存)
- 196行

### ThermalErosion

熱風化侵食。安息角を超える勾配を平滑化。

- 入力: `float[,] heights`, `ErosionSettings`
- Pure C# 実装
- 81行

### ErosionPreview (Editor)

Inspector 上でエロージョン結果をプレビュー。146行。

## テスト

- EditMode: `Assets/Tests/EditMode/ErosionTests.cs` (200行)
- 水力・熱エロージョンの基本動作検証

## 統合ポイント

- `TerrainChunk.Build()` 内で `ErosionSettings` が設定されている場合に自動適用
- `TerrainGenerationConfig` から `ErosionSettings` SO を参照

## 制約

- GPU 加速なし (Pure C#)
- チャンク境界のシーム処理なし (将来課題)

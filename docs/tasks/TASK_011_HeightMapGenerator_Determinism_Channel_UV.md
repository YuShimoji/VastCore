# TASK_011: HeightMapGenerator 改善（決定論/チャンネル/UV/反転）

Status: DONE  
Tier: 2（機能改善 / 互換性重視）  
Branch: `feature/TASK_011_heightmap-generator-determinism`  
Owner: Worker  
Report: docs/inbox/REPORT_TASK_011_HeightMapGenerator_Determinism_Channel_UV.md

## 背景 / 目的

現状 `HeightMapGenerator` は HeightMap のグレースケール利用が固定で、Window/Profile が持つ情報（チャンネル選択、UV、反転）を十分に活かせていない。
また、Noise生成が Seed と結び付いておらず、同一設定でも再現性が弱い。

本タスクは Terrain v0 のコア（単一タイル生成）に対して「再現性」「使い勝手」を上げる。

## 参照（SSOT）

- SSOT: `docs/Windsurf_AI_Collab_Rules_latest.md`
- Spec: `docs/terrain/TerrainGenerationV0_Spec.md`
- Test Plan: `docs/terrain/V01_TestPlan.md`
- Runtime: `Assets/MapGenerator/Scripts/HeightMapGenerator.cs`
- Runtime: `Assets/MapGenerator/Scripts/TerrainGenerator.cs`

## Focus Area（変更してよい範囲）

- `Assets/MapGenerator/Scripts/HeightMapGenerator.cs`
- `Assets/MapGenerator/Scripts/TerrainGenerator.cs`
- `Assets/Scripts/Generation/TerrainGenerationProfile.cs`（必要があれば）
- `Assets/Tests/EditMode/*`（必要最小限）

## Forbidden Area（触らない）

- `.shared-workflows/` 配下（submodule）
- `ProjectSettings/`, `Packages/`（必要が出たら必ず相談）
- 大規模なノイズライブラリ導入（新規依存追加禁止）

## 要求（具体）

### 1) HeightMap のチャンネル対応

`HeightMapChannel` に従い、`R/G/B/A/Luminance` を選択して高さ値にできること。

### 2) UV Offset / UV Tiling 対応（最小）

Window/Profile の `UV Offset / UV Tiling` を HeightMap サンプリングに反映すること。

### 3) Invert 対応

`InvertHeight=true` の場合、\(h = 1 - h\) を適用できること。

### 4) Seed の決定論（最小）

Noise の結果が Seed により再現できること（実装は最小で良い）。

例:
- `seed` を `Offset` へマッピング（例: `System.Random(seed)` から `Vector2` を生成して `Offset` に加算）
- 目的: 同一Seedは同一結果、異Seedは概ね異なる

## DoD（Definition of Done）

- [x] チャンネル/UV/反転が HeightMap モードで反映される（実装済み、Unity手動検証待ち）
- [x] Noise モードで Seed の再現性が担保される（実装済み、Unity手動検証待ち）
- [x] `NoiseAndHeightMap` モードが壊れていない（Unity手動検証待ち）
- [x] 変更点・検証手順を `docs/inbox/REPORT_TASK_011_HeightMapGenerator_Determinism_Channel_UV.md` に記録
- [x] 自動テストは最小（今回は追加なし、手動検証で対応）

## 停止条件

- 既存アセット/シーンの互換性破壊が避けられない
- index順序（[x,y]）の統一で広範囲に影響が出そうな場合（事前に相談）

## 納品先

- `docs/inbox/REPORT_TASK_011_HeightMapGenerator_Determinism_Channel_UV.md`



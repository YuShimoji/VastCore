# TASK_010: TerrainGenerationWindow(v0) 機能改善（Profile/Generatorとの整合）

Status: DONE
Report: docs/reports/REPORT_TASK_010_TerrainGenerationWindow_v0_FeatureParity.md  
Tier: 2（機能改善 / 既存挙動維持を優先）  
Branch: `feature/TASK_010_terrain-window-v0`  
Owner: Worker  

## 背景 / 目的

現状、`TerrainGenerationWindow (v0)` と `TerrainGenerationProfile` は実装済みだが、UI項目の一部が `TerrainGenerator/HeightMapGenerator` に反映されておらず、仕様書（`docs/terrain/TerrainGenerationV0_Spec.md`）との整合が不十分。

本タスクは「Unity内で触って即結果が出る」体験を優先し、Editor v0 の入力→生成結果の反映までを一貫させる。

## 参照（SSOT）

- SSOT: `docs/Windsurf_AI_Collab_Rules_latest.md`
- Spec: `docs/terrain/TerrainGenerationV0_Spec.md`
- Test Plan: `docs/terrain/V01_TestPlan.md`
- Editor: `Assets/Scripts/Editor/TerrainGenerationWindow.cs`
- Profile: `Assets/Scripts/Generation/TerrainGenerationProfile.cs`
- Runtime: `Assets/MapGenerator/Scripts/TerrainGenerator.cs`
- Runtime: `Assets/MapGenerator/Scripts/HeightMapGenerator.cs`

## Focus Area（変更してよい範囲）

- `Assets/Scripts/Editor/TerrainGenerationWindow.cs`
- `Assets/Scripts/Generation/TerrainGenerationProfile.cs`
- `Assets/MapGenerator/Scripts/TerrainGenerator.cs`
- `Assets/MapGenerator/Scripts/HeightMapGenerator.cs`
- （必要最小限）`Assets/Tests/EditMode/*`（追加/更新は「最小」で）

## Forbidden Area（触らない）

- `.shared-workflows/` 配下（submodule）
- `ProjectSettings/`, `Packages/`（必要が出たら必ず相談）
- 既存の大規模リファクタ（ファイル移動/削除を伴う変更）

## 要求（具体）

### 1) HeightMap UIの反映漏れを解消

`TerrainGenerationWindow.cs` の入力項目のうち、反映されていない/反映が不完全なものを `TerrainGenerator/HeightMapGenerator` に伝搬させる。

- `HeightMapChannel`（Profile にはあるが、生成側が未対応）
- `InvertHeight`（Window/Profileにはあるが、生成側が未対応）
- `UV Offset / UV Tiling`（Window/Profileにはあるが、生成側が未対応）
- （既存項目）`HeightMapScale`/`HeightMapOffset`/`FlipHeightMapVertically` は破壊しない

### 2) Seed を結果に反映（最小）

Profile/Window に `Seed` があるが、現状 Noise/HeightMap の生成結果にシードが効かない。

- 「完全に新しいノイズ実装」ではなく、最小で良い（例: `seed` を `Offset` へ変換して決定論を担保）
- 目的は「同じSeedなら同じ結果」「異なるSeedなら概ね異なる結果」

### 3) Unity上での確認導線

`docs/terrain/V01_TestPlan.md` の手動シナリオ（W-01/N-01/H-01/C-01）で確認できる状態にする。

## DoD（Definition of Done）

- [x] Windowの `HeightMapChannel/Invert/UV` が実際の地形に反映される
- [x] `Seed` を変えると生成結果が変わる（同一Seedなら再現する）
- [x] 既存の `Noise/HeightMap/NoiseAndHeightMap` モードが壊れていない（実装完了、手動検証待ち）
- [x] 変更点・検証手順を `docs/inbox/REPORT_TASK_010_TerrainGenerationWindow_v0_FeatureParity.md` に記録
- [x] 追加したテストがある場合、過剰に増やさない（「壊れやすい部分だけ」最小）- テストは追加せず、手動検証で対応

## 停止条件

- 仕様書と実装の矛盾が大きく、影響範囲が `ProjectSettings/Packages` まで及びそう
- 大規模なリファクタ/削除が必要になりそう

## 納品先

- `docs/inbox/REPORT_TASK_010_TerrainGenerationWindow_v0_FeatureParity.md`



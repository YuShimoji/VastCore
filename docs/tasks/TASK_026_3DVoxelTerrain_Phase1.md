# Task: 3D Voxel Terrain Hybrid System - Phase 1 実装
Status: OPEN
Tier: 3 (新規機能 / 大規模アーキテクチャ変更)
Branch: feature/TASK_020_3d-voxel-terrain-phase1
Owner: Worker
Created: 2026-01-29T08:50:00+09:00
Report: 

## Objective
Unity 2022 LTS + Job System + Burst Compiler を用いて、「Marching Cubesアルゴリズムを用いたハイブリッド・ボクセル地形生成システム」の**Core Foundation (基盤)**を実装する。
Phase 1では、**Unity Job System**を用いた並列処理基盤の確立と、最小限のSDF（球体）を用いたレンダリングテストの成功を目標とする。

## Context
- **背景**: 既存の2Dハイトマップシステムでは洞窟やオーバーハングが表現できないため、3Dボクセルシステムを導入し、ハイブリッド運用を目指す。
- **全体計画**: `docs/tasks/BACKLOG_3D_VoxelTerrain_HybridSystem.md` (Phase 1-5の計画)
- **Phase 1範囲**: 高速化基盤の確立。Job Systemでのメッシュ生成。SDFによる球体表示。

## Focus Area
- `Assets/Scripts/Terrain/Voxel/` (新規)
  - `VoxelChunk.cs`: チャンク管理 (MonoBehaviour)
  - `MarchingCubesJob.cs`: メッシュ生成ロジック (IJobParallelFor)
  - `DensityCalculationJob.cs`: 密度計算ロジック (Burst)
  - `MarchingCubesTables.cs`: ルックアップテーブル (static readonly)
- `Scenes/Development/VoxelDev.unity` (新規テストシーン)

## Forbidden Area
- `.shared-workflows/` 配下（submodule）
- `ProjectSettings/`, `Packages/`（必要が出たら必ず相談）
- 既存の2Dハイトマップシステム（`MapGenerator`配下）の変更禁止
- メインスレッドでの重い計算処理（必ずJob Systemを使用）

## Constraints
- **Unity version**: Unity 2022 LTS以降
- **Technology Stack**:
  - Unity Job System (`IJobParallelFor`)
  - Burst Compiler (必須)
  - Unity.Mathematics (`float3`, `int3`)
  - Native Collections (`NativeArray`)
- **Memory Management**: GC Allocation 0を目指す（NativeArrayの適切なDispose）
- **Architecture**: MonoBehaviourはView/Event管理に徹し、ロジックはStruct(Job)に分離する

## DoD
- [ ] 目的が達成されている（Job SystemによるMarching Cubes描画）
- [ ] `VoxelChunk` を配置したシーンで、球体のメッシュが生成・描画される
- [ ] Unity Profilerで確認し、メインスレッドがブロックされていない（Worker ThreadでJobが実行されている）
- [ ] Unity Editor上での動作確認が完了している
- [ ] コンパイルエラーがない
- [ ] docs/inbox/ にレポート（REPORT_...md）が作成されている
- [ ] 本チケットの Report 欄にレポートパスが追記されている

## Notes
- `MarchingCubesTables` の Look-up Table 実装は、Paul Bourkeの実装などを参照すること。
- Phase 1では法線計算（Smooth Normal）やテクスチャリングは必須ではない（確認できれば推奨）。
- テストシーン `VoxelDev` を作成し、そこで動作確認を行うこと。

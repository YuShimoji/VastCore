# TASK_013: Dual Grid Terrain System - Phase 1 実装

Status: DONE  
Tier: 2（新規機能 / 既存システムと並行運用）  
Branch: `feature/TASK_013_dual-grid-terrain-phase1`  
Owner: Worker  
Created: 2026-01-05T13:30:00+09:00  
Completed: 2026-01-11T23:56:00+09:00  
Report: `docs/reports/REPORT_TASK_013_DualGridTerrainSystem_Phase1.md`

## Objective

UnityとC#を使用して、『Townscaper』のような「Dual Grid（不規則グリッド）」システムの基盤を実装する。まずは地形生成の基盤となるアルゴリズムと、デバッグ表示までを実装する。

**目標**: Gizmosで歪んだグリッドが画面に表示され、高さ方向の積み上げが可視化されること。

## Context

### 背景

既存のVastCoreは**2Dハイトマップ地形**（`TerrainGenerator` / `HeightMapGenerator`）を基盤としているが、以下の制約がある：

- **洞窟・オーバーハング・浮遊島**が表現できない
- **有機的な歪み**を持つ地形が生成できない

本タスクは、**Dual Grid System**を新たなアルゴリズムとして追加し、既存の2Dハイトマップシステムと**並行運用**することを目的とする。

### コアコンセプト

1. **Base Grid**: 六角形グリッド（Hex Grid）をベースとする。
2. **Subdivision**: 各六角形を中心点で分割し、3つの四角形（Rhombus/菱形）を生成する。
3. **Relaxation**: 頂点座標にランダム性（Noise/Jitter）を加え、有機的な歪みを作る。
4. **Verticality**: 2Dグリッドを高さ方向（Y軸）に積層（Stack）させ、3D地形を表現する。

### 参照（SSOT）

- **Spec**: `docs/Spec/DualGridTerrainSystem_Spec.md`
- **既存2Dシステム**: `docs/terrain/TerrainGenerationV0_Spec.md`
- **既存実装**: `Assets/MapGenerator/Scripts/TerrainGenerator.cs`
- **SSOT**: `docs/Windsurf_AI_Collab_Rules_latest.md`

## Focus Area（変更してよい範囲）

- `Assets/Scripts/Terrain/DualGrid/` フォルダ（新規作成）
  - `Coordinates.cs`: 座標変換ロジック
  - `GridTopology.cs`: グリッドトポロジー生成
  - `IrregularGrid.cs`: グリッド管理クラス
  - `Node.cs`: 頂点データ構造（HasGround/HasCeiling対応）
  - `Cell.cs`: セルデータ構造
  - `ColumnStack.cs`: 垂直データ管理
  - `GridDebugVisualizer.cs`: Gizmos描画（MonoBehaviour）
  - `VerticalExtrusionGenerator.cs`: 高さ生成ロジック

## Forbidden Area（変更禁止）

- `.shared-workflows/` 配下（submodule）
- `ProjectSettings/`, `Packages/`（必要が出たら必ず相談）
- 既存の2Dハイトマップシステム（`Assets/MapGenerator/Scripts/TerrainGenerator.cs` 等）
- 既存のテストコード（新規テストは追加可）

## Constraints / DoD

### 必須要件

1. **座標系の実装** ✅
   - `Coordinates.cs`: Axial座標 `(q, r)` とワールド座標 `(x, z)` の相互変換
   - 六角形グリッドと四角形グリッドの変換ロジック
   - 各六角形の3つのサブセル（四角形）への識別子 `(q, r, index[0-2])`

2. **グリッドトポロジー生成** ✅
   - `GridTopology.cs`: 純粋なC#クラス（MonoBehaviour継承なし）
   - Axial座標系を用いて、隣接関係が正しい「六角形→3分割四角形」のグラフ構造を生成
   - 各四角形（Cell）が、正しく4つの頂点（Node）と4つの隣接セル（Neighbor Cells）を取得できるロジック

3. **グリッド管理** ✅
   - `IrregularGrid.cs`: グリッド全体を管理するクラス
   - `GenerateGrid(int radius)`: 指定された半径で六角形を敷き詰める
   - Nodes (Vertices): 四角形の「角（頂点）」のリスト。ワールド座標 `(x, 0, z)` を持つ
   - Cells (Quads): 4つのNodeへの参照を持つ四角形データのリスト

4. **データ構造** ✅
   - `Node.cs`: `HasGround`, `HasCeiling`, `HeightIndex` を持つ頂点データ構造
   - `Cell.cs`: 4つのNodeと4つの隣接セルへの参照を持つセルデータ構造
   - `ColumnStack.cs`: 高さ方向のデータ管理（`Dictionary<CellID, List<bool>>` または `BitArray`）

5. **Relaxation（形状緩和）** ✅
   - 各Nodeの座標 `(x, z)` にパーリンノイズ、またはランダムなオフセットを加算
   - 制約: セルが裏返らない（凸性を維持する）範囲に留める
   - `System.Random` にシード値を与え、常に同じ形状が再現できるようにする

6. **デバッグ可視化** ✅
   - `GridDebugVisualizer.cs` (MonoBehaviour): `OnDrawGizmos` を使用して以下を描画
     - **Nodes**: 小さいスフィアで描画
     - **Edges**: Node同士を結ぶ線を描画（歪んだグリッドを可視化）
     - **Cells**: セルの中心に色付きの面、またはIDを表示
   - 高さ方向の積み上げ: ワイヤーフレームのボックス（歪んだ柱）を積み上げて表示

7. **高さ生成ロジック** ✅
   - `VerticalExtrusionGenerator.cs`: 単純な高さマップ、またはノイズ関数を使用して、各セルに「高さ（何階層までブロックがあるか）」を設定
   - Gizmosで、その高さの分だけワイヤーフレームのボックスを積み上げて表示

### 非機能要件

- **Modularity**: グリッド生成ロジックと、描画ロジックは分離する
- **Determinism**: `System.Random` にシード値を与え、常に同じ形状が再現できるようにする
- **Avoid GameObject Overhead**: セル一つ一つをGameObjectにしないこと。データは配列やリストで管理し、描画はGizmosで行う（Phase 1はGizmos表示に集中）

### テスト要件

- 手動検証: Unity Editor上でGizmosで歪んだグリッドが画面に表示されることを確認 ⏳
- 手動検証: 高さ方向の積み上げが可視化されることを確認 ⏳
- 既存テストがすべて成功することを確認 ⏳

### Unity固有の制約

- Unity Editor上での手動検証が必要（Gizmos描画の確認）
- EditorOnlyコード（`#if UNITY_EDITOR`）の適切な分離を考慮する
- `OnDrawGizmos` を使用するため、MonoBehaviour継承が必要

## 停止条件

- 既存の2Dハイトマップシステムに破壊的変更が必要になる
- 座標変換ロジックの数学的実装が複雑になりすぎる場合（仕様の見直しが必要）
- グリッド生成に必要な計算リソースが過大になる場合（最適化が必要）

## 実装方針

### Phase 1.1: 座標系とデータ構造

1. `Coordinates.cs`: Axial座標系の実装
2. `Node.cs`: 頂点データ構造（HasGround/HasCeiling対応）
3. `Cell.cs`: セルデータ構造

### Phase 1.2: グリッドトポロジー生成

1. `GridTopology.cs`: 六角形→四角形分割ロジック
2. `IrregularGrid.cs`: グリッド管理クラス
3. `GenerateGrid(int radius)`: グリッド生成メソッド

### Phase 1.3: Relaxation（形状緩和）

1. 各Nodeの座標にパーリンノイズまたはランダムオフセットを加算
2. 凸性を維持する制約チェック

### Phase 1.4: 垂直データと高さ生成

1. `ColumnStack.cs`: 垂直データ管理
2. `VerticalExtrusionGenerator.cs`: 高さ生成ロジック

### Phase 1.5: デバッグ可視化

1. `GridDebugVisualizer.cs`: Gizmos描画
2. Unity Editor上での動作確認

## 関連タスク

- `TASK_010`: TerrainGenerationWindow(v0) 機能改善（完了）
- `TASK_011`: HeightMapGenerator 決定論・チャネル・UV対応（完了）
- `TASK_012`: TerrainGenerationWindow プリセット管理機能（完了）
- `BACKLOG_3D_VoxelTerrain_HybridSystem.md`: 将来的な3Dボクセル地形システム（別アプローチ）

## 備考

- 本タスクは**Phase 1（基盤実装）**に焦点を当てる
- Phase 2以降（メッシュ生成、レンダリング、Marching Squares）は別タスクとして分離
- 既存の2Dハイトマップシステムとは並行運用し、破壊的変更は行わない
- 将来的には「Git Narrative Integration」（ブランチごとの地形状態切り替え）に対応する予定

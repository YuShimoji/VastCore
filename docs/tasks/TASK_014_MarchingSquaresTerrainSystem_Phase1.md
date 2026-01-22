# TASK_014: Marching Squares Terrain System - Phase 1 実装

Status: DONE  
Report: docs/reports/REPORT_TASK_014_MarchingSquaresTerrainSystem_Phase1.md
Tier: 2（新規機能 / 既存システムと並行運用）  
Branch: `feature/TASK_014_marching-squares-terrain-phase1`  
Owner: Worker  
Created: 2026-01-12T00:00:00+09:00  

## Objective

UnityとC#を使用して、デュアルグリッド（頂点データ）+ Marching Squaresアルゴリズムによる地形生成システムの基盤を実装する。まずはデータ構造、Marching Squaresアルゴリズム、プレハブ配置までを実装し、Unityのプリミティブ形状（Cube/Plane）で動作確認を行う。

**目標**: グリッドデータを設定すると、16種類のパターンに対応したプレハブが正しく配置されること。

## Context

### 背景

既存のVastCoreは以下の地形生成システムを持つ：

- **2Dハイトマップシステム**（`TerrainGenerator` / `HeightMapGenerator`）
- **六角形グリッドシステム**（`DualGridTerrainSystem` - TASK_013完了）

本タスクは、**Marching Squares Terrain System**を新たなアルゴリズムとして追加し、既存システムと**並行運用**することを目的とする。

### コアコンセプト

1. **Dual Grid (Vertex Data)**: データは「マスの中」ではなく「格子の交点（頂点）」に保持する
2. **Marching Squares**: 4頂点の状態から16通りのパターンを判定し、適切なメッシュを選択
3. **Prefab Placement**: インデックスに対応する3Dモデルを生成（またはプールから取得）して配置
4. **Spline Input**: UnityのSplineパッケージ等で描かれた曲線を座標データとして取得（Phase 2で実装）

### 参照（SSOT）

- **Spec**: `docs/Spec/MarchingSquaresTerrainSystem_Spec.md`
- **既存2Dシステム**: `docs/terrain/TerrainGenerationV0_Spec.md`
- **既存六角形グリッドシステム**: `docs/Spec/DualGridTerrainSystem_Spec.md`
- **既存実装**: `Assets/MapGenerator/Scripts/TerrainGenerator.cs`
- **SSOT**: `docs/Windsurf_AI_Collab_Rules_latest.md`

## Focus Area（変更してよい範囲）

- `Assets/Scripts/Terrain/MarchingSquares/` フォルダ（新規作成）
  - `MarchingSquaresGrid.cs`: グリッドデータ管理クラス
  - `MarchingSquaresCalculator.cs`: ビットマスク計算ロジック
  - `MarchingSquaresGenerator.cs`: メッシュ生成・配置ロジック
  - `MarchingSquaresDebugVisualizer.cs`: Gizmos描画（MonoBehaviour）

## Forbidden Area（変更禁止）

- `.shared-workflows/` 配下（submodule）
- `ProjectSettings/`, `Packages/`（必要が出たら必ず相談）
- 既存の2Dハイトマップシステム（`Assets/MapGenerator/Scripts/TerrainGenerator.cs` 等）
- 既存の六角形グリッドシステム（`Assets/Scripts/Terrain/DualGrid/` 等）
- 既存のテストコード（新規テストは追加可）

## Constraints / DoD

### 必須要件

1. **データ構造とグリッド管理** ✅
   - `MarchingSquaresGrid.cs`: グリッドデータ管理クラス
     - `bool[,] mapData`: 各頂点が「埋まっている(True)」か「空(False)」かを保持
     - `int width, int height`: グリッドサイズ
     - `float cellSize`: セルサイズ
     - `SetVertex(int x, int y, bool value)`: 頂点データを設定
     - `GetVertex(int x, int y)`: 頂点データを取得

2. **Marching Squares アルゴリズム** ✅
   - `MarchingSquaresCalculator.cs`: ビットマスク計算ロジック
     - `CalculateIndex(int x, int y)`: セルの4頂点からインデックス（0～15）を算出
     - `GetCellCorners(int x, int y)`: セルの4頂点の状態を取得
     - ビット演算: `int index = (tl << 3) | (tr << 2) | (br << 1) | bl;`

3. **プレハブ配置** ✅
   - `MarchingSquaresGenerator.cs`: メッシュ生成・配置ロジック
     - `GameObject[] prefabs`: 16種類のプレハブ配列（Inspectorで設定可能）
     - `GenerateMap()`: 全セルを走査してプレハブを配置
     - セル位置の計算: `new Vector3(x * cellSize, 0, y * cellSize)`
     - 既存のプレハブを破棄してから再生成する機能

4. **デバッグ可視化** ✅
   - `MarchingSquaresDebugVisualizer.cs` (MonoBehaviour): Gizmos描画
     - `OnDrawGizmos`: グリッド、頂点、セルを可視化
     - Inspectorで各種設定（Grid Size, Cell Size等）を調整可能
     - 頂点の状態（True/False）を色分けして表示

### 非機能要件

- **Modularity**: グリッド生成ロジックと、描画ロジックは分離する
- **Determinism**: 同じグリッドデータに対して常に同じ結果が得られること
- **Inspector設定**: グリッドサイズ、セルサイズ、プレハブ配列をInspectorで設定可能にする

### テスト要件

- 手動検証: Unity Editor上でグリッドデータを設定し、16種類のパターンが正しく配置されることを確認
- 手動検証: プリミティブ形状（Cube/Plane）で動作確認
- 既存テストがすべて成功することを確認

### Unity固有の制約

- Unity Editor上での手動検証が必要（プレハブ配置の確認）
- EditorOnlyコード（`#if UNITY_EDITOR`）の適切な分離を考慮する
- `OnDrawGizmos` を使用するため、MonoBehaviour継承が必要
- プレハブ配列はInspectorで設定可能にする（ScriptableObjectまたはMonoBehaviour）

## 停止条件

- 既存の2Dハイトマップシステム、六角形グリッドシステムに破壊的変更が必要になる
- Marching Squaresアルゴリズムの実装が複雑になりすぎる場合（仕様の見直しが必要）
- グリッド生成に必要な計算リソースが過大になる場合（最適化が必要）

## 実装方針

### Phase 1.1: データ構造とグリッド管理

1. `MarchingSquaresGrid.cs`: グリッドデータ管理クラス
   - `bool[,] mapData` の初期化
   - `SetVertex`, `GetVertex` メソッドの実装
   - `Clear()` メソッドの実装

### Phase 1.2: Marching Squares アルゴリズム

1. `MarchingSquaresCalculator.cs`: ビットマスク計算ロジック
   - `CalculateIndex(int x, int y)`: セルの4頂点からインデックス（0～15）を算出
   - `GetCellCorners(int x, int y)`: セルの4頂点の状態を取得
   - ビット演算の実装

### Phase 1.3: プレハブ配置

1. `MarchingSquaresGenerator.cs`: メッシュ生成・配置ロジック
   - `GameObject[] prefabs` のInspector設定
   - `GenerateMap()`: 全セルを走査してプレハブを配置
   - 既存のプレハブを破棄してから再生成する機能

### Phase 1.4: デバッグ可視化

1. `MarchingSquaresDebugVisualizer.cs`: Gizmos描画
   - `OnDrawGizmos`: グリッド、頂点、セルを可視化
   - Inspectorで各種設定を調整可能

### Phase 1.5: 動作確認

1. Unity Editor上での動作確認
   - プリミティブ形状（Cube/Plane）で16種類のパターンをテスト
   - グリッドデータを手動で設定して動作確認

## 関連タスク

- `TASK_010`: TerrainGenerationWindow(v0) 機能改善（完了）
- `TASK_011`: HeightMapGenerator 決定論・チャネル・UV対応（完了）
- `TASK_012`: TerrainGenerationWindow プリセット管理機能（完了）
- `TASK_013`: Dual Grid Terrain System - Phase 1 実装（完了）
- `TASK_015（将来）`: Marching Squares Terrain System - Phase 2 実装（スプライン入力対応）
- `TASK_016（将来）`: Marching Squares Terrain System - Phase 3 実装（レイヤー構造対応）

## 備考

- 本タスクは**Phase 1（基盤実装）**に焦点を当てる
- Phase 2以降（スプライン入力、レイヤー構造、最適化）は別タスクとして分離
- 既存の2Dハイトマップシステム、六角形グリッドシステムとは並行運用し、破壊的変更は行わない
- プレハブはPhase 1ではプリミティブ形状（Cube/Plane）で代用し、Phase 2以降で本格的なアセットを用意する

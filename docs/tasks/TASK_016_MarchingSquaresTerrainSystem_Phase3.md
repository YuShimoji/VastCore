# TASK_016: Marching Squares Terrain System - Phase 3 実装（レイヤー構造対応）

Status: DONE  
Report: docs/reports/REPORT_TASK_016_MarchingSquaresTerrainSystem_Phase3.md
Tier: 2（新規機能 / 既存システムと並行運用）  
Branch: `feature/TASK_016_marching-squares-terrain-phase3`  
Owner: Worker  
Created: 2026-01-18T04:30:00+09:00  
Completed: 2026-01-18  

## Objective

UnityとC#を使用して、Marching Squares Terrain SystemのPhase 3（レイヤー構造対応）を実装する。拡張データ構造（Height, BiomeId, RoadId, BuildingId）を追加し、バイオーム遷移と高さマップ対応を実装する。

**目標**: グリッドデータに高さ・バイオーム・道路・建物の情報を持たせ、それに基づいて適切なプレハブを選択・配置できること。

## Context

### 背景

TASK_014でMarching Squares Terrain SystemのPhase 1（基盤実装）が完了し、TASK_015でPhase 2（スプライン入力対応）が完了した。Phase 3では、単純なbool値（埋まっている/空）から、より複雑なレイヤー構造（高さ・バイオーム・道路・建物）に対応する。

### コアコンセプト

1. **拡張データ構造**: `GridPoint`クラスでHeight, BiomeId, RoadId, BuildingIdを保持
2. **バイオーム遷移**: 隣接バイオームの境界を検出し、適切な境界線モデルを選択
3. **高さマップ対応**: 高さマップからHeightを設定し、崖・スロープモデルを選択
4. **レイヤー優先順位**: 地形ベース → 道路レイヤー → 建物レイヤーの順で処理

### 参照（SSOT）

- **Spec**: `docs/Spec/MarchingSquaresTerrainSystem_Spec.md`（Phase 3セクション）
- **Phase 1実装**: `Assets/Scripts/Terrain/MarchingSquares/`（TASK_014完了）
- **Phase 2実装**: `Assets/Scripts/Terrain/MarchingSquares/`（TASK_015完了）
- **SSOT**: `docs/Windsurf_AI_Collab_Rules_latest.md`

## Focus Area（変更してよい範囲）

- `Assets/Scripts/Terrain/MarchingSquares/` フォルダ（既存フォルダに追加）
  - `GridPoint.cs`: 拡張データ構造（新規作成）
  - `MarchingSquaresGrid.cs`: `GridPoint[,]` サポートの拡張（既存ファイルを拡張）
  - `BiomeTransitionCalculator.cs`: バイオーム遷移ロジック（新規作成）
  - `HeightMapProcessor.cs`: 高さマップ処理ロジック（新規作成）
  - `MarchingSquaresGenerator.cs`: レイヤー構造対応の拡張（既存ファイルを拡張）

## Forbidden Area（変更禁止）

- `.shared-workflows/` 配下（submodule）
- `ProjectSettings/`, `Packages/`（必要が出たら必ず相談）
- 既存の2Dハイトマップシステム（`Assets/MapGenerator/Scripts/TerrainGenerator.cs` 等）
- 既存の六角形グリッドシステム（`Assets/Scripts/Terrain/DualGrid/` 等）
- Phase 1/2で実装した既存のMarching Squaresクラス（`MarchingSquaresCalculator.cs`, `SplineRasterizer.cs`）の破壊的変更
- 既存の`bool[,]`ベースのグリッドデータ（後方互換性のため維持）

## Constraints / DoD

### 必須要件

1. **拡張データ構造** ✅
   - `GridPoint.cs`: 拡張データ構造クラス
     - `Height`: 高さ値（float、デフォルト: 0.0）
     - `BiomeId`: バイオームID（int、デフォルト: 0）
     - `RoadId`: 道路ID（int、デフォルト: 0、0=道路なし）
     - `BuildingId`: 建物ID（int、デフォルト: 0、0=建物なし）
     - `IsFilled`: 埋まっているか（bool、既存のbool値と互換）
   - `MarchingSquaresGrid.cs`: `GridPoint[,]` サポートの拡張
     - `GridPoint[,] m_ExtendedData`: 拡張データ配列（オプション）
     - `bool UseExtendedData`: 拡張データを使用するか（デフォルト: false、後方互換性）
     - `GetGridPoint(int x, int y)`: GridPointを取得
     - `SetGridPoint(int x, int y, GridPoint point)`: GridPointを設定

2. **バイオーム遷移** ✅
   - `BiomeTransitionCalculator.cs`: バイオーム遷移ロジック
     - `CalculateTransition(int x, int y, MarchingSquaresGrid grid)`: セルのバイオーム遷移を計算
     - `GetTransitionType(int biomeId1, int biomeId2)`: 遷移タイプを取得（例: 海→陸、陸→山）
     - `SelectBoundaryModel(int transitionType)`: 境界線モデルを選択
   - 隣接セルのバイオームIDを比較し、境界を検出
   - 境界線モデルの選択ロジック（プレハブ配列から選択）

3. **高さマップ対応** ✅
   - `HeightMapProcessor.cs`: 高さマップ処理ロジック
     - `ProcessHeightMap(Texture2D heightMap, MarchingSquaresGrid grid)`: 高さマップからHeightを設定
     - `CalculateHeightAt(int x, int y, Texture2D heightMap)`: グリッド座標での高さを計算
     - `SelectSlopeModel(float height1, float height2, float height3, float height4)`: スロープモデルを選択
   - 高さマップからHeightを設定
   - 崖・スロープモデルの選択ロジック（高低差に基づく）

4. **MarchingSquaresGenerator の拡張** ✅
   - レイヤー構造対応の拡張
     - `GenerateMapWithLayers()`: レイヤー構造を考慮した地形生成
     - レイヤー優先順位: 地形ベース → 道路レイヤー → 建物レイヤー
   - Inspector設定項目の追加:
     - `UseExtendedData`: 拡張データを使用するか
     - `HeightMap`: 高さマップ（Texture2D、オプション）
     - `BiomeTransitionModels`: バイオーム遷移モデル配列
     - `SlopeModels`: スロープモデル配列

### 非機能要件

- **後方互換性**: 既存の`bool[,]`ベースのグリッドデータは維持（`UseExtendedData = false`の場合）
- **Modularity**: 拡張データ構造、バイオーム遷移、高さマップ処理は独立したクラスとして実装
- **Inspector設定**: 拡張データの使用、高さマップ、遷移モデルをInspectorで設定可能
- **パフォーマンス**: 拡張データ使用時も許容可能な処理時間（最適化はPhase 4で実施）

### テスト要件

- 手動検証: Unity Editor上で拡張データ構造が正しく動作することを確認
- 手動検証: バイオーム遷移が正しく検出され、適切なモデルが選択されることを確認
- 手動検証: 高さマップからHeightが正しく設定され、崖・スロープモデルが選択されることを確認
- 既存テストがすべて成功することを確認

### Unity固有の制約

- Unity Editor上での手動検証が必要（拡張データ構造、バイオーム遷移、高さマップ処理の確認）
- EditorOnlyコード（`#if UNITY_EDITOR`）の適切な分離を考慮する
- ScriptableObject、Texture2D等のUnity API使用時の注意点を明記する

## 停止条件

- 拡張データ構造の実装が複雑になりすぎる場合（仕様の見直しが必要）
- 既存のMarching Squaresシステムに破壊的変更が必要になる
- 高さマップ処理に必要な計算リソースが過大になる場合（最適化が必要）

## 実装方針

### Phase 3.1: 拡張データ構造

1. `GridPoint.cs`: 拡張データ構造クラス
   - Height, BiomeId, RoadId, BuildingIdプロパティ
   - IsFilledプロパティ（既存のbool値と互換）

2. `MarchingSquaresGrid.cs`: `GridPoint[,]` サポートの拡張
   - `UseExtendedData`フラグの追加
   - `GridPoint[,]`配列の追加
   - 既存の`bool[,]`との互換性維持

### Phase 3.2: バイオーム遷移

1. `BiomeTransitionCalculator.cs`: バイオーム遷移ロジック
   - 隣接セルのバイオームID比較
   - 遷移タイプの判定
   - 境界線モデルの選択

### Phase 3.3: 高さマップ対応

1. `HeightMapProcessor.cs`: 高さマップ処理ロジック
   - 高さマップからHeightを設定
   - 崖・スロープモデルの選択ロジック

2. `MarchingSquaresGenerator.cs`: レイヤー構造対応の拡張
   - `GenerateMapWithLayers()`メソッドの実装
   - レイヤー優先順位の実装

### Phase 3.4: 動作確認

1. Unity Editor上での動作確認
   - 拡張データ構造の動作確認
   - バイオーム遷移の動作確認
   - 高さマップ処理の動作確認

## 関連タスク

- `TASK_014`: Marching Squares Terrain System - Phase 1 実装（完了）
- `TASK_015`: Marching Squares Terrain System - Phase 2 実装（完了、実動作確認待ち）
- `TASK_017（将来）`: Marching Squares Terrain System - Phase 4 実装（最適化と統合）

## 備考

- 本タスクは**Phase 3（レイヤー構造対応）**に焦点を当てる
- Phase 4以降（最適化、統合）は別タスクとして分離
- 既存の2Dハイトマップシステム、六角形グリッドシステムとは並行運用し、破壊的変更は行わない
- 後方互換性を維持するため、既存の`bool[,]`ベースのグリッドデータは維持する
- TASK_015の実動作確認結果に依存せず、独立して実装可能

## 実装完了レポート

- **レポート:** `docs/reports/REPORT_TASK_016_MarchingSquaresTerrainSystem_Phase3.md`
- **実装完了日時:** 2026-01-18
- **実装内容:**
  - ✅ `GridPoint.cs`: 拡張データ構造クラスの作成
  - ✅ `MarchingSquaresGrid.cs`: GridPoint[,] サポートの拡張
  - ✅ `BiomeTransitionCalculator.cs`: バイオーム遷移ロジックの実装
  - ✅ `HeightMapProcessor.cs`: 高さマップ処理ロジックの実装
  - ✅ `MarchingSquaresGenerator.cs`: レイヤー構造対応の拡張
  - ✅ 後方互換性の維持
  - ✅ リンターエラーの確認（エラーなし）

## 次のステップ

1. Unity Editor上での手動検証（拡張データ構造、バイオーム遷移、高さマップ処理の確認）
2. Phase 4（最適化と統合）の準備

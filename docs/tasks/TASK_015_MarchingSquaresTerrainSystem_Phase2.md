# TASK_015: Marching Squares Terrain System - Phase 2 実装（スプライン入力対応）

Status: DONE  
Report: docs/reports/REPORT_TASK_015_MarchingSquaresTerrainSystem_Phase2.md
Tier: 2（新規機能 / 既存システムと並行運用）  
Branch: `feature/TASK_015_marching-squares-terrain-phase2`  
Owner: Worker  
Created: 2026-01-12T12:00:00+09:00  

## Objective

UnityとC#を使用して、Marching Squares Terrain SystemのPhase 2（スプライン入力対応）を実装する。Unity Spline Packageを使用してスプライン曲線をグリッドデータに変換し、直感的なレベルデザインを可能にする。

**目標**: Unity Spline Packageで描いた曲線をグリッドデータに焼き込み、Marching Squaresアルゴリズムで地形を生成できること。

## Context

### 背景

TASK_014でMarching Squares Terrain SystemのPhase 1（基盤実装）が完了し、手動でグリッドデータを設定して地形を生成できるようになった。Phase 2では、Unity Spline Packageを使用してスプライン曲線をグリッドデータに変換し、より直感的なレベルデザインを可能にする。

### コアコンセプト

1. **Spline Rasterization**: Unity Spline Packageで描かれた曲線をグリッド座標に変換
2. **Brush System**: スプライン上の点を一定間隔でサンプリングし、ブラシ範囲内の頂点を設定
3. **Grid Data Update**: スプラインラスタライズ結果を `MarchingSquaresGrid` に反映
4. **Real-time Preview**: スプライン編集時にリアルタイムでプレビュー表示（オプション）

### 参照（SSOT）

- **Spec**: `docs/Spec/MarchingSquaresTerrainSystem_Spec.md`（Phase 2セクション）
- **Phase 1実装**: `Assets/Scripts/Terrain/MarchingSquares/`（TASK_014完了）
- **Unity Spline Package**: Unity Package Managerからインストール可能
- **SSOT**: `docs/Windsurf_AI_Collab_Rules_latest.md`

## Focus Area（変更してよい範囲）

- `Assets/Scripts/Terrain/MarchingSquares/` フォルダ（既存フォルダに追加）
  - `SplineRasterizer.cs`: スプラインラスタライズロジック（新規作成）
  - `MarchingSquaresGenerator.cs`: スプライン入力対応の拡張（既存ファイルを拡張）
  - `MarchingSquaresDebugVisualizer.cs`: スプライン可視化の追加（既存ファイルを拡張、オプション）

## Forbidden Area（変更禁止）

- `.shared-workflows/` 配下（submodule）
- `ProjectSettings/`, `Packages/`（Unity Spline Packageの追加は許可、その他の変更は相談）
- 既存の2Dハイトマップシステム（`Assets/MapGenerator/Scripts/TerrainGenerator.cs` 等）
- 既存の六角形グリッドシステム（`Assets/Scripts/Terrain/DualGrid/` 等）
- Phase 1で実装した既存のMarching Squaresクラス（`MarchingSquaresGrid.cs`, `MarchingSquaresCalculator.cs`）の破壊的変更

## Constraints / DoD

### 必須要件

1. **スプラインラスタライズ** ✅
   - `SplineRasterizer.cs`: スプラインをグリッドに焼き込むクラス
     - `RasterizeSpline(Spline spline, float brushRadius, MarchingSquaresGrid grid)`: スプラインをグリッド座標に変換
     - `SetGridDataInRange(int gridX, int gridY, float radius, bool value, MarchingSquaresGrid grid)`: ブラシ範囲内の頂点を設定
     - スプライン上の点を一定間隔でサンプリング（`samplingInterval` パラメータ）
     - ブラシ半径内の頂点を `true` に設定（埋める）または `false` に設定（削除）

2. **Unity Spline Package 統合** ✅
   - Unity Spline Packageを使用してスプラインを取得
   - `Unity.Splines` 名前空間を使用
   - `SplineContainer` コンポーネントからスプラインを取得
   - スプライン上の点を `SplineUtility.GetPointAt` 等でサンプリング

3. **MarchingSquaresGenerator の拡張** ✅
   - `SplineContainer` への参照を追加（Inspectorで設定可能）
   - `RasterizeFromSpline()` メソッドを追加
   - スプラインラスタライズ後に `GenerateMap()` を自動実行するオプション

4. **エラーハンドリング** ✅
   - Unity Spline Packageがインストールされていない場合のエラーメッセージ
   - スプラインが null の場合のエラーハンドリング
   - グリッド範囲外アクセスのチェック

### 非機能要件

- **Modularity**: スプラインラスタライズロジックは独立したクラスとして実装
- **Inspector設定**: ブラシ半径、サンプリング間隔をInspectorで調整可能
- **パフォーマンス**: 大規模スプラインでも許容可能な処理時間（最適化はPhase 4で実施）

### テスト要件

- 手動検証: Unity Editor上でスプラインを描き、グリッドデータに正しく反映されることを確認
- 手動検証: スプラインラスタライズ後に地形が正しく生成されることを確認
- 既存テストがすべて成功することを確認

### Unity固有の制約

- Unity Editor上での手動検証が必要（スプライン描画と地形生成の確認）
- Unity Spline Packageのインストールが必要（Package Managerから追加）
- EditorOnlyコード（`#if UNITY_EDITOR`）の適切な分離を考慮する
- `SplineContainer` コンポーネントの参照はInspectorで設定可能にする

## 停止条件

- Unity Spline Packageがインストールできない、または互換性の問題がある
- スプラインラスタライズの実装が複雑になりすぎる場合（仕様の見直しが必要）
- 既存のMarching Squaresシステムに破壊的変更が必要になる

## 実装方針

### Phase 2.1: スプラインラスタライズロジック

1. `SplineRasterizer.cs`: スプラインラスタライズクラス
   - `RasterizeSpline` メソッドの実装
   - スプライン上の点を一定間隔でサンプリング
   - ブラシ範囲内の頂点を設定

### Phase 2.2: Unity Spline Package 統合

1. Unity Spline Packageのインストール確認
2. `SplineContainer` コンポーネントからスプラインを取得
3. `SplineUtility` を使用してスプライン上の点をサンプリング

### Phase 2.3: MarchingSquaresGenerator の拡張

1. `SplineContainer` への参照を追加
2. `RasterizeFromSpline()` メソッドを実装
3. Inspectorで設定可能にする

### Phase 2.4: 動作確認

1. Unity Editor上での動作確認
   - スプラインを描いてグリッドデータに反映
   - 地形が正しく生成されることを確認

## 関連タスク

- `TASK_014`: Marching Squares Terrain System - Phase 1 実装（完了）
- `TASK_016（将来）`: Marching Squares Terrain System - Phase 3 実装（レイヤー構造対応）

## 備考

- 本タスクは**Phase 2（スプライン入力対応）**に焦点を当てる
- Phase 3以降（レイヤー構造、最適化）は別タスクとして分離
- 既存の2Dハイトマップシステム、六角形グリッドシステムとは並行運用し、破壊的変更は行わない
- リアルタイムプレビュー機能はオプション（実装可能であれば追加）

# Phase D: 最適化・拡張 (Optimization & Extension)

- **最終更新日時**: 2026-03-16
- **ステータス**: Draft
- **前提**: Phase A/B/C 完了、SG-1 Prefabスタンプ単セル配置完了

---

## 目標

Phase 5 (高度合成システム) と Phase 6 (ランダム制御) の実装完了。
パフォーマンス目標達成 (60FPS安定)。巨大ファイルの分割。

## 完了基準

- [ ] PD-1: 高度合成システム実装完了
- [ ] PD-2: ランダム制御システム実装完了
- [ ] PD-3: 60FPS 安定動作 + GC Alloc 1MB/frame 以下
- [ ] PD-4: 巨大ファイル全て 500行以下に分割
- [ ] 全既存テスト (91件) パス維持

---

## PD-1: 高度合成システム (Advanced Composition System)

**サイズ**: XL
**依存**: PC-2 (CSG Blend 完了済み)

### スコープ

- ComposeModels: 複数メッシュ統合パイプライン
- LODGenerator: LODGroup 自動生成
- MeshBlender: 体積ブレンド
- OptimizeMesh: メッシュ最適化

### 対象ファイル

- 新規: `Scripts/Generation/Composition/AdvancedCompositionSystem.cs`
- 新規: `Scripts/Generation/Composition/MeshBlender.cs`
- 新規: `Scripts/Generation/Composition/LODGenerator.cs`
- 修正: `Scripts/Terrain/Map/AdvancedPrimitiveLODSystem.cs`
- テスト: `Scripts/Testing/EditMode/GenerationTests/CompositionSystemTests.cs`

### 検証

- 複数メッシュ入力 → 合成 → LOD生成の一連パイプライン動作確認

---

## PD-2: ランダム制御システム (Controlled Random System)

**サイズ**: L
**依存**: PC-3 (StructureGenerator 完了済み)

### スコープ

- シード管理・再現可能ランダム
- パラメータ制約システム
- RandomPresetLibrary (ScriptableObject ベース)
- BlendShapeRandom

### 対象ファイル

- 新規: `Scripts/Generation/Random/ControlledRandomSystem.cs`
- 新規: `Scripts/Generation/Random/RandomPresetLibrary.cs`
- 新規: `Scripts/Generation/Random/BlendShapeRandom.cs`
- 修正: `Editor/StructureGenerator/Tabs/Editing/RandomControlTab.cs`
- テスト: `Scripts/Testing/EditMode/GenerationTests/RandomSystemTests.cs`

### 検証

- シード固定で同一出力再現
- プリセット保存/読み込み動作確認

---

## PD-3: パフォーマンス最適化 (Performance Optimization)

**サイズ**: L
**依存**: PC-5 (GameManager接続 完了済み)

### スコープ

- Job System 導入 (TerrainHeightJob, MeshGenerationJob)
- Burst Compiler 対応
- プール効率改善
- GC プレッシャー削減

### 対象ファイル

- 修正: `Scripts/Terrain/Map/RuntimeGenerationManager.cs`
- 修正: `Scripts/Terrain/Map/PrimitiveTerrainManager.cs`
- 修正: `Scripts/Terrain/Map/PrimitiveTerrainObjectPool.cs`
- 修正: `Scripts/Terrain/Map/PrimitiveMemoryManager.cs`
- 新規: `Scripts/Terrain/Jobs/TerrainHeightJob.cs`
- 新規: `Scripts/Terrain/Jobs/MeshGenerationJob.cs`

### 検証

- フレームタイム 16ms 以下 (60FPS)
- GC Alloc 1MB/frame 以下
- ベンチマーク結果記録

---

## PD-4: 巨大ファイル分割 (Large File Decomposition)

**サイズ**: L
**依存**: PB-1 (テスト基盤 — 既に75+16=91テスト存在)

### 対象

| ファイル | 現行行数 | 分割先 |
|----------|---------|--------|
| BiomeSpecificTerrainGenerator.cs | ~1706行 | BiomeTerrainCore + BiomeVegetation + BiomeWeathering |
| HighQualityPrimitiveGenerator.cs | ~1498行 | 形状ごと個別クラス (Factoryパターン) |
| CompoundArchitecturalGenerator.cs | ~1309行 | ArchitecturalCore + ArchitecturalDecorator |
| NaturalTerrainFeatures.cs | ~1083行 | RiverGenerator + CliffGenerator + CaveGenerator |

### 検証

- 全既存テストパス
- 新クラスの単体テスト追加
- 各ファイル 500行以下

---

## 着手順序の推奨

1. **PD-4** (巨大ファイル分割) — 他タスクの可読性・編集容易性を先に確保
2. **PD-3** (パフォーマンス最適化) — Job/Burst基盤は PD-1/PD-2 にも恩恵
3. **PD-1** (高度合成) — コア機能追加
4. **PD-2** (ランダム制御) — PD-1完了後にUI拡張と統合

ただしこの順序は確定ではなく、スライス定義時に再検討する。

---

## Quick Wins との関係

POST_PHASE_C_QUICK_WINS.md の QW-A〜D は Phase D とは独立に着手可能。
特に QW-A1/A2 (気候ビジュアル) はローリスク・ハイリターンで Phase D と並行可能。

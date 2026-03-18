# Phase D: オーサリング主体 + 段階的バリエーション

- **最終更新日時**: 2026-03-18
- **ステータス**: Active
- **前提**: Phase A/B/C + SG-1/SG-2 + PD-4 完了
- **方針**: T1 (オーサリング主体) + V4 (段階的 V1→V2/V3)

---

## 目標

デザイナーがEditorで構造物を作成し、パラメトリック変異で個体差を持たせ、
DualGrid地形上に配置するオーサリングワークフローの完成。

## 完了基準

- [x] PD-4: 巨大ファイル全て 500行以下に分割 (2026-03-17完了)
- [ ] SP-018 (V1): パラメトリック変異 — PositionJitter/MaterialVariants/ChildToggleGroups (pct 85)
- [ ] Unity実機検証: SP-018 + SG-1/SG-2 のコンパイル+テスト+目視確認
- [ ] PD-1: 高度合成システム (V3への足がかり)
- [ ] PD-2: ランダム制御システム (V2への足がかり)
- [ ] PD-3: 60FPS 安定動作 + GC Alloc 1MB/frame 以下
- [ ] 全既存テスト パス維持

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

## 着手順序 (T1+V4方針に基づく)

1. **SP-018 V1 完了** (パラメトリック変異) — Unity実機検証で閉じる [pct 85→100]
2. **SP-017 完了** (StampExporter) — 実機検証で閉じる [pct 75→100]
3. **PD-2** (ランダム制御) — RandomControlTab → StampDefinition 転写。V2 (WFC) の基盤
4. **PD-1** (高度合成) — CSG合成のランタイム不要化確認。V3候補の精査
5. **PD-3** (パフォーマンス最適化) — 地形 + 配置パイプラインの60FPS安定

### 段階的バリエーション拡張パス (V4)

```text
V1 (完了中) → V1.5 (RandomControlTab連携) → V2 (WFC検証) → V3 (CSG合成)
```

| 段階 | 内容 | 依存 | バリエーション深度 |
|------|------|------|------------------|
| V1 | PositionJitter / MaterialVariants / ChildToggleGroups | なし | 色違い・配置ずれ・部品差替 |
| V1.5 | RandomControlTab → StampDefinition 転写 | PD-2 | スケール/回転の精密制御 |
| V2 | WFC タイル自動配置 | SP-013 (Composite Rules) | 構造的多様性・建物群レイアウト |
| V3 | CSG コンポジション自動化 | PD-1 (Advanced Composition) | 形状レベルの独自性 |

---

## Quick Wins との関係

POST_PHASE_C_QUICK_WINS.md の QW-A〜D は Phase D とは独立に着手可能。
特に QW-A1/A2 (気候ビジュアル) はローリスク・ハイリターンで Phase D と並行可能。

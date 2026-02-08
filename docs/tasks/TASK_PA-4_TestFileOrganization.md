# Task: PA-4 テストファイルの所属整理

> **Source**: `docs/01_planning/DEVELOPMENT_ROADMAP_2026.md` (L212-233)  
> **Phase**: A - Stabilization  
> **Tier**: 1  
> **Size**: M

---

## 概要

散在しているテストファイルを `Scripts/Testing/` アセンブリに集約し、適切なディレクトリ構造に整理します。

---

## ゴール

- 全テストファイルが `Scripts/Testing/` 配下に集約
- `Vastcore.Testing.asmdef` が全テストを包含
- テストが正しくコンパイル・実行可能

---

## 対象ファイル

### 移動対象（12ファイル）
`Scripts/Terrain/Map/` → `Scripts/Testing/TerrainTests/`
1. `AdvancedTerrainAlgorithmsTest.cs`
2. `ArchitecturalGeneratorTest.cs`
3. `NaturalTerrainFeaturesTest.cs`
4. `NaturalTerrainValidationTest.cs`
5. `PrimitiveTerrainTest.cs`
6. `RuntimeTerrainManagerTest.cs`
7. `RuntimeGenerationManagerTest.cs`
8. `SeamlessConnectionManagerTest.cs`
9. `CrystalStructureGeneratorTest.cs`
10. `ClimateTerrainFeedbackTest.cs`
11. `TerrainTexturingSystemTest.cs`
12. `LODMemorySystemTest.cs` / `AdvancedLODSystemTest.cs`
13. `ComprehensivePrimitiveTest.cs`

### その他の移動
- `Scripts/Core/GeologicalFormationTest.cs` → `Scripts/Testing/CoreTests/`

### 修正対象
- `Scripts/Testing/Vastcore.Testing.asmdef` — 新テストディレクトリが含まれるか確認

### 削除対象
- `Scripts/Testing/VastcoreTesting.cs` (DummyTestのみ→不要)

---

## 新規ディレクトリ構造

```
Scripts/Testing/
├── Vastcore.Testing.asmdef
├── CoreTests/
│   └── GeologicalFormationTest.cs
├── TerrainTests/
│   ├── AdvancedTerrainAlgorithmsTest.cs
│   ├── ArchitecturalGeneratorTest.cs
│   ├── NaturalTerrainFeaturesTest.cs
│   ├── NaturalTerrainValidationTest.cs
│   ├── PrimitiveTerrainTest.cs
│   ├── RuntimeTerrainManagerTest.cs
│   ├── RuntimeGenerationManagerTest.cs
│   ├── SeamlessConnectionManagerTest.cs
│   ├── CrystalStructureGeneratorTest.cs
│   ├── ClimateTerrainFeedbackTest.cs
│   ├── TerrainTexturingSystemTest.cs
│   ├── LODMemorySystemTest.cs
│   ├── AdvancedLODSystemTest.cs
│   └── ComprehensivePrimitiveTest.cs
└── (その他既存テスト)
```

---

## 依存関係

- **Depends-On**: なし（独立タスク）
- **Blocks**: PB-1 (テスト基盤構築)
- **Blocked-By**: なし

---

## Focus Area / Forbidden Area

### Focus Area（編集対象）
- テストファイルの移動（`Scripts/Testing/` 配下へ）
- `Vastcore.Testing.asmdef` の参照範囲更新

### Forbidden Area（編集禁止）
- テストファイルの内容変更（移動のみ）
- 移動元ファイルの削除（参照更新完了まで）
- テストロジックの変更

---

## 検証手順

1. Unity Editor で `Window > General > Test Runner` を開く
2. EditMode テストを実行
3. 全テストがコンパイル・実行可能であることを確認

---

## 完了基準

- [ ] `Scripts/Core/GeologicalFormationTest.cs` → `Scripts/Testing/CoreTests/` 移動
- [ ] `Scripts/Terrain/Map/*Test.cs` (12ファイル) → `Scripts/Testing/TerrainTests/` 移動
- [ ] `Vastcore.Testing.asmdef` が新テストディレクトリを包含
- [ ] `VastcoreTesting.cs` 削除
- [ ] Unity Test Runner で全テストが認識される
- [ ] `MISSION_LOG.md` 更新

---

## ステータス

- **Status**: READY
- **Worker**: 未割り当て
- **Started**: - 
- **Completed**: -

---

## 変更履歴

| 日時 | Actor | 内容 |
|------|-------|------|
| 2026-02-09 | Orchestrator | チケット作成 |

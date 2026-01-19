# Legacy/Shelved コード隔離設計書

**作成日**: 2025-12-07
**作成者**: Cascade (AI)
**ステータス**: Draft

---

## 1. 目的

V01 地形生成コアの開発に集中するため、Legacy/Shelved コードを論理的・物理的に隔離し、
今後の作業対象から明確に除外する。

---

## 2. 分類基準

### 2.1 V01 Core (Active)

**触る対象。機能追加・バグ修正の主戦場。**

| ファイル | 場所 | 行数 | 役割 |
|---------|------|------|------|
| TerrainGenerationProfile.cs | Scripts/Generation | 299 | 生成パラメータ ScriptableObject |
| TerrainGenerationConstants.cs | Scripts/Generation | 117 | 定数管理 |
| TerrainGenerationMode.cs | Scripts/Generation | 19 | 生成モード enum |
| TerrainGenerator.cs | MapGenerator/Scripts | 15034 | 地形生成の核 |
| HeightMapGenerator.cs | MapGenerator/Scripts | 6723 | 高さマップ生成 |
| TerrainGenerationWindow.cs | Scripts/Editor | 19746 | Editor UI (v0) |
| DetailGenerator.cs | MapGenerator/Scripts | 3312 | ディテール設定 |
| TextureGenerator.cs | MapGenerator/Scripts | 5786 | テクスチャ設定 |
| TreeGenerator.cs | MapGenerator/Scripts | 3027 | ツリー設定 |
| TerrainOptimizer.cs | MapGenerator/Scripts | 739 | 最適化設定 |

### 2.2 Phase2 Target (Shelved → 次フェーズでActive化)

**現フェーズでは触らないが、Phase2 で統合予定。**

| ファイル | 場所 | 行数 | 役割 |
|---------|------|------|------|
| DesignerTerrainTemplate.cs | Scripts/Generation/Map | 8342 | テンプレート定義 |
| TerrainEngine.cs | Scripts/Generation/Map | 19984 | 生成エンジン（Lite版統合予定） |
| TerrainSynthesizer.cs | Scripts/Generation/Map | 17045 | テンプレート合成 |
| TerrainTemplateEditor.cs | Scripts/Editor | 30130 | テンプレートエディタ |
| BlendSettings.cs (Generation) | Scripts/Generation/Map | 645 | ブレンド設定（統合先） |

### 2.3 Legacy (段階的削除対象)

**現フェーズでは触らない。将来的に削除または完全再実装。**

#### Primitive/LOD/メモリ管理系

| ファイル | 場所 | 行数 | 理由 |
|---------|------|------|------|
| PrimitiveTerrainObject.cs | Scripts/Terrain | 19196 | 複雑すぎ、V0では不要 |
| PrimitiveTerrainObject.cs | Scripts/Terrain/Map | 10852 | 重複、整理対象 |
| PrimitiveTerrainGenerator.cs | Scripts/Generation/Map | 30416 | Legacyとして定数化済み |
| PrimitiveMemoryManager.cs | Scripts/Terrain/Map | 15675 | V0では不要 |
| PrimitiveTerrainObjectPool.cs | Scripts/Terrain/Map | 16250 | V0では不要 |
| PrimitiveTerrainManager.cs | Scripts/Terrain/Map | 20546 | V0では不要 |
| AdvancedPrimitiveLODSystem.cs | Scripts/Terrain/Map | 34641 | V0では不要 |
| HighQualityPrimitiveGenerator.cs | Scripts/Terrain/Map | 65155 | ProBuilder API問題あり |

#### Biome/Climate系

| ファイル | 場所 | 行数 | 理由 |
|---------|------|------|------|
| BiomeSpecificTerrainGenerator.cs | Scripts/Terrain/Map | 68907 | Phase3以降 |
| ClimateSystem.cs | Scripts/Generation/Map | 30998 | Phase3以降 |
| ClimateDataGenerator.cs | Scripts/Generation/Map | 17797 | Phase3以降 |
| ClimateData.cs | Scripts/Generation/Map | 5633 | Phase3以降 |
| BiomeSystem.cs | Scripts/Generation/Map | 1042 | Phase3以降 |
| BiomeTypes.cs | Scripts/Generation/Map | 2417 | Phase3以降 |

#### 構造物生成系

| ファイル | 場所 | 行数 | 理由 |
|---------|------|------|------|
| ArchitecturalGenerator.cs | Scripts/Terrain/Map | 48784 | Phase3以降 |
| CompoundArchitecturalGenerator.cs | Scripts/Terrain/Map | 58703 | Phase3以降 |
| NaturalTerrainFeatures.cs | Scripts/Terrain/Map | 43701 | Phase3以降 |
| CrystalStructureGenerator.cs | Scripts/Terrain/Map | 36701 | Phase3以降 |
| CrystalGrowthSimulator.cs | Scripts/Terrain/Map | 33112 | Phase3以降 |

#### ストリーミング/タイル管理系

| ファイル | 場所 | 行数 | 理由 |
|---------|------|------|------|
| RuntimeTerrainManager.cs | Scripts/Terrain/Map | 36265 | Phase2.5以降 |
| RuntimeTerrainManager.cs | Scripts/Generation/Map | 11102 | 重複、整理対象 |
| TileManager.cs | Scripts/Terrain/Map | 22065 | Phase2.5以降 |
| TerrainTile.cs | Scripts/Terrain/Map | 25401 | Phase2.5以降 |
| TerrainTile.cs | Scripts/Generation/Map | 7240 | 重複、整理対象 |
| SeamlessConnectionManager.cs | Scripts/Terrain/Map | 21760 | Phase2.5以降 |
| RuntimeGenerationManager.cs | Scripts/Terrain/Map | 22970 | Phase2.5以降 |

#### テスト系（Legacy機能のテスト）

| ファイル | 場所 | 行数 | 理由 |
|---------|------|------|------|
| AdvancedLODSystemTest.cs | Scripts/Terrain/Map | 18270 | Legacy機能のテスト |
| LODMemorySystemTest.cs | Scripts/Terrain/Map | 11760 | Legacy機能のテスト |
| ComprehensivePrimitiveTest.cs | Scripts/Terrain/Map | 27634 | Legacy機能のテスト |
| PrimitiveTerrainTest.cs | Scripts/Terrain/Map | 8492 | Legacy機能のテスト |
| PrimitiveQualityTestRunner.cs | Scripts/Terrain/Map | 21369 | Legacy機能のテスト |
| PrimitiveQualityTestScene.cs | Scripts/Terrain/Map | 20202 | Legacy機能のテスト |
| PrimitiveQualityValidator.cs | Scripts/Terrain/Map | 31121 | Legacy機能のテスト |
| RunPrimitiveQualityTest.cs | Scripts/Terrain/Map | 10929 | Legacy機能のテスト |
| RuntimeTerrainManagerTest.cs | Scripts/Terrain/Map | 19852 | Legacy機能のテスト |
| SeamlessConnectionManagerTest.cs | Scripts/Terrain/Map | 17768 | Legacy機能のテスト |
| ClimateTerrainFeedbackTest.cs | Scripts/Terrain/Map | 15614 | Legacy機能のテスト |
| ArchitecturalGeneratorTest.cs | Scripts/Terrain/Map | 12868 | Legacy機能のテスト |
| NaturalTerrainFeaturesTest.cs | Scripts/Terrain/Map | 17548 | Legacy機能のテスト |
| CrystalStructureGeneratorTest.cs | Scripts/Terrain/Map | 12343 | Legacy機能のテスト |
| AdvancedTerrainAlgorithmsTest.cs | Scripts/Terrain/Map | 12644 | Legacy機能のテスト |
| TerrainTexturingSystemTest.cs | Scripts/Terrain/Map | 26085 | Legacy機能のテスト |
| RuntimeGenerationManagerTest.cs | Scripts/Terrain/Map | 10308 | Legacy機能のテスト |
| NaturalTerrainValidationTest.cs | Scripts/Terrain/Map | 22209 | Legacy機能のテスト |
| TestNaturalTerrainFeatures.cs | Scripts/Terrain/Map | 8052 | Legacy機能のテスト |
| NaturalTerrainTestRunner.cs | Scripts/Terrain/Map | 5524 | Legacy機能のテスト |

---

## 3. 隔離戦略

### 3.1 即時対応（推奨）

**物理的な移動は行わず、論理的な隔離のみ実施。**

1. **asmdef による依存方向の制御**
   - `Vastcore.Generation` (V01 Core) → Legacy への依存を禁止
   - Legacy → V01 Core への依存は許可（既存動作維持）

2. **ドキュメントによる明示**
   - 本ドキュメントで分類を明確化
   - README や CONTRIBUTING に「Legacy フォルダは触らない」旨を記載

3. **コード内コメント**
   - Legacy ファイルの先頭に `// LEGACY: Do not modify. See docs/design/LegacyIsolation_Design.md` を追加

### 3.2 中期対応（Phase2 完了後）

1. **物理的なフォルダ移動**
   ```
   Assets/Scripts/
   ├── Generation/        # V01 Core + Phase2 Target
   ├── MapGenerator/      # V01 Core (Runtime)
   ├── Editor/            # V01 Editor UI
   ├── Legacy/            # 旧コード（新規作成）
   │   ├── Terrain/
   │   │   └── Map/
   │   └── Generation/
   │       └── Map/
   └── ...
   ```

2. **Legacy 専用 asmdef 作成**
   - `Vastcore.Legacy.asmdef`
   - V01 Core からの参照を完全に切断

### 3.3 長期対応（Phase3 以降）

1. **Legacy コードの段階的削除**
   - 使用されていないファイルから順次削除
   - 必要な機能は V01 基盤上で再実装

2. **重複解消**
   - `BlendSettings.cs` の統合
   - `RuntimeTerrainManager.cs` の統合
   - `TerrainTile.cs` の統合

---

## 4. 重複ファイル一覧

| ファイル名 | 場所1 | 場所2 | 対応方針 |
|-----------|-------|-------|---------|
| BlendSettings.cs | Generation/Map | Terrain/Map | Generation に統合 |
| RuntimeTerrainManager.cs | Generation/Map | Terrain/Map | Phase2.5 で再設計 |
| TerrainTile.cs | Generation/Map | Terrain/Map | Phase2.5 で再設計 |
| BoxTerrainGenerator.cs | Generation/Map | Terrain/Map | 要調査 |
| MeshGenerator.cs | Generation/Map | Terrain/Map | 要調査 |
| PrimitiveTerrainObject.cs | Terrain | Terrain/Map | Terrain に統合 |

---

## 5. 次のアクション

### 即時（本セッション）

- [x] 本ドキュメントの作成
- [ ] Legacy ファイルへのコメント追加（代表的なもののみ）
- [ ] V01 Core の動作確認テスト

### 短期（次セッション以降）

- [ ] V01_TestPlan.md に基づく手動テスト実施
- [ ] TerrainGenerator の統合テスト追加
- [ ] Phase2 Template 統合の着手

### 中期（Phase2 完了後）

- [ ] Legacy フォルダへの物理移動
- [ ] Vastcore.Legacy.asmdef 作成

---

## 6. 関連ドキュメント

- [TerrainGenerationV0_Spec.md](../terrain/TerrainGenerationV0_Spec.md)
- [Phase15_RuntimeRefactor_Design.md](./Phase15_RuntimeRefactor_Design.md)
- [Phase2_TemplateIntegration_Spec.md](./Phase2_TemplateIntegration_Spec.md)
- [ProjectAudit_Nov25.md](../progress/ProjectAudit_Nov25.md)
- [Handover_Nov20.md](../progress/Handover_Nov20.md)


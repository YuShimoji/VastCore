# VastCore Terrain Engine — 開発ロードマップ 2026

**作成日**: 2026-02-09
**前提**: AUDIT_2026-02 総点検 + コードベース全件精査

---

## 0. 現状サマリー

### プロジェクト規模
| 指標 | 値 |
|------|-----|
| 総 .cs ファイル数 | ~200 |
| アセンブリ定義 | 10 (Core, Terrain, Generation, Player, Camera, UI, Game, Testing, Editor, Utilities) |
| StructureGenerator タブ | 7 (Basic, Advanced, Distribution, Operations/Composition, Random, Deform, Relationships) |
| 開発フェーズ進捗 | Phase 1,2,4 完了 / Phase 3 進行中 / Phase 5,6 未着手 |

### 健全性スコア (現状 → 目標)
| 領域 | 現状 | 目標 |
|------|------|------|
| コンパイル安定性 | 85 | 95 |
| アーキテクチャ | 70 | 85 |
| コード品質 | 65 | 80 |
| テストカバレッジ | 40 | 70 |
| ドキュメント整合性 | 55 | 75 |

### アセンブリ依存グラフ (現状)
```
Vastcore.Utilities  (依存なし, autoReferenced=false)
    ↑
Vastcore.Core       (→ Utilities, autoReferenced=false)
    ↑
    ├── Vastcore.Generation  (→ Core, Utilities, ProBuilder, autoReferenced=true ⚠)
    │       ↑
    ├── Vastcore.Terrain     (→ Core, Utilities, Generation, ProBuilder, TMPro)
    │       ↑
    ├── Vastcore.Player      (→ Core, Utilities, Terrain, Generation, ProBuilder, TMPro, InputSystem)
    │       ↑
    ├── Vastcore.Camera      (→ 未宣言 ⚠)
    │       ↑
    ├── Vastcore.UI           (→ Core, Utilities, Player, TMPro, DebugUI, InputSystem)
    │       ↑
    └── Vastcore.Game        (→ Core, Utilities, Player, Terrain, Camera, Generation, UI)

Vastcore.Editor   (→ Core, Utilities, Generation, Terrain, TMPro)  [Editor only]
Vastcore.Testing  (→ Core, Utilities, Generation, Terrain, Player, UI, ProBuilder*, TestFramework)
```

---

## 1. 課題一覧 (ファイル双方向マッピング付き)

### 1.1 P0: ブロッカー

| ID | 課題 | 関連ファイル | 影響範囲 |
|----|-------|-------------|----------|
| P0-1 | **Deform スタブ衝突リスク** | `Scripts/Deform/DeformStubs.cs` (10行) | `Scripts/Core/DeformPresetLibrary.cs`, `Scripts/Generation/DeformIntegration.cs`, `Scripts/Generation/DeformIntegrationManager.cs`, `Scripts/Generation/VastcoreDeformManager.cs`, `Editor/StructureGenerator/Tabs/Deform/DeformerTab.cs` |
| P0-2 | **ProBuilder API 11箇所無効化** | `Scripts/Terrain/Map/HighQualityPrimitiveGenerator.cs` (1498行, Subdivide/UV/Optimize) | `Scripts/Generation/Map/PrimitiveTerrainGenerator.cs`, `Scripts/Generation/Map/PrimitiveModifier.cs` — 高品質プリミティブ生成の全系統が劣化 |

### 1.2 P1: 高優先

| ID | 課題 | 関連ファイル | 影響範囲 |
|----|-------|-------------|----------|
| P1-1 | **CompositionTab CSG 実動作未検証** | `Editor/StructureGenerator/Tabs/Editing/CompositionTab.cs`, `Utils/CsgProviderAbstractions.cs`, `ProBuilderInternalCsgProvider.cs`, `ParaboxCsgProvider.cs` | StructureGenerator の Composition 機能全体。Blend未実装 |
| P1-2 | **テストスタブ・空ファイル** | `Scripts/Testing/VastcoreTesting.cs` (18行), `VastcoreIntegrationTestStubs.cs` (35行) | テスト基盤の信頼性 |
| P1-3 | **テストカバレッジ不足** | Player/, UI/, Camera/, Game/ にテストなし | リグレッション検知不能 |
| P1-4 | **ComprehensiveSystemTest 未実装参照** | `Scripts/Testing/ComprehensiveSystemTest.cs:31-33` | RuntimeTerrainManager, PrimitiveTerrainManager, RuntimeGenerationManager への参照がコメントアウト |
| P1-5 | **Camera asmdef 依存未宣言** | `Scripts/Camera/Vastcore.Camera.asmdef` | 他アセンブリから参照されているが依存関係が不明瞭 |

### 1.3 P2: 中優先

| ID | 課題 | 関連ファイル | 影響範囲 |
|----|-------|-------------|----------|
| P2-1 | **Core アセンブリ肥大化** | `VastcoreDiagnostics.cs` (26KB), `VastcoreSystemManager.cs` (20KB), `VastcoreDebugVisualizer.cs` (19KB), `VastcoreErrorHandler.cs` (14KB), `DeformPresetLibrary.cs` (16KB) | Core が診断・デバッグ・Deform プリセットまで担当 → SRP 違反 |
| P2-2 | **巨大ファイル SRP 違反** | `BiomeSpecificTerrainGenerator.cs` (1706行), `HighQualityPrimitiveGenerator.cs` (1498行), `CompoundArchitecturalGenerator.cs` (1309行), `ArchitecturalGenerator.cs` (1137行), `NaturalTerrainFeatures.cs` (1083行) | 保守性・テスタビリティ低下 |
| P2-3 | **Generation asmdef autoReferenced=true** | `Scripts/Generation/Vastcore.Generation.asmdef` | 他アセンブリと不統一。リリースビルドで意図しない参照 |
| P2-4 | **TODO/FIXME 31件** | 上記 Grep 結果参照 | 技術的負債の可視化不足 |
| P2-5 | **ドキュメント SSOT 不在** | `docs/01_planning/` に 14 ファイル散在。DEV_PLAN.md は 2025年1月更新のまま | 計画とコードの乖離 |
| P2-6 | **CI/CD 未稼働** | `.github/workflows/unity-tests.yml` 存在するが UNITY_LICENSE シークレット未設定 | 自動テストが機能していない |
| P2-7 | **Phase 5/6 未着手** | 高度合成システム、ランダム制御システム | DEV_PLAN.md の中核機能 |
| P2-8 | **Editor asmdef に ProBuilder 参照なし** | `Scripts/Editor/Vastcore.Editor.asmdef` | Editor 内の ProBuilder 関連ツールがコンパイルできない可能性 |

### 1.4 P3: 低優先

| ID | 課題 | 関連ファイル |
|----|-------|-------------|
| P3-1 | シーン整理（テストシーン散在） | Scenes/ 配下 |
| P3-2 | uGUI → UI Toolkit 移行 | `Scripts/UI/` 全11ファイル (uGUI + TMPro ベース) |
| P3-3 | Terrain ストリーミングシステム | `Scripts/Terrain/TerrainStreamingController.cs` (未完成) |

---

## 2. ファイル双方向マッピング (主要モジュール)

### 2.1 Core アセンブリ (23ファイル → 分割候補)

| ファイル | 提供する機能 | 依存される側 | 所属すべき場所 |
|---------|-------------|-------------|--------------|
| `VastcoreSystemManager.cs` | システム全体のオーケストレーション | Game, Testing | Core ✓ |
| `VastcoreDiagnostics.cs` | 診断・メトリクス収集 | Testing, Editor | → **新規 Vastcore.Diagnostics** |
| `VastcoreDebugVisualizer.cs` | デバッグ描画 | Editor | → **新規 Vastcore.Diagnostics** |
| `VastcoreErrorHandler.cs` | エラーハンドリング | Core内部, Game | Core ✓ |
| `DeformPresetLibrary.cs` | Deformプリセット管理 | Generation, Editor | → **Generation** (Deform系と同居) |
| `GeologicalFormationGenerator.cs` | 地質形成生成 | Terrain | → **Terrain** |
| `GeologicalFormationTest.cs` | テスト | Testing | → **Testing** |
| `TerrainSynthesizer.cs` | 地形合成 | Terrain, Generation | → **Terrain** |
| `TerrainErrorRecovery.cs` | 地形エラー回復 | Terrain | → **Terrain** |
| `PrimitiveErrorRecovery.cs` | プリミティブエラー回復 | Terrain | → **Terrain** |
| `SceneManager.cs` / `SceneNavigation.cs` | シーン管理 | Game | → **Game** |
| `Interfaces/` (5ファイル) | 共通インターフェース | 全アセンブリ | Core ✓ |
| `PlayerTransformResolver.cs` | プレイヤー位置解決 | Terrain, Player | Core ✓ |
| `LogOutputHandler.cs` | ログ出力 | 全アセンブリ | Core ✓ |
| `GenerationPrimitiveType.cs` | プリミティブ型定義 | Generation, Terrain | Core ✓ |
| `RockLayerPhysicalProperties.cs` | 岩盤物性定義 | Terrain | Core or Terrain |

### 2.2 Terrain アセンブリ (77ファイル — 最大)

**サブモジュール構成:**
| ディレクトリ | ファイル数 | 機能 | 状態 |
|-------------|----------|------|------|
| `Terrain/Map/` | 40+ | プリミティブ地形・LOD・テクスチャ | 主力。ただしテストファイルが混在 ⚠ |
| `Terrain/MarchingSquares/` | 8 | マーチングスクエア地形 | Phase 1的な実装。安定 |
| `Terrain/DualGrid/` | 8 | デュアルグリッドシステム | 実験的。利用箇所不明 ⚠ |
| `Terrain/GPU/` | 2 | GPU地形生成 | GPUTerrainGenerator + Monitor |
| `Terrain/Cache/` | 2 | キャッシュシステム | IntelligentCacheSystem + Manager |
| `Terrain/Config/` | 3 | 設定 | HeightmapProvider/Noise/Generation |
| `Terrain/Providers/` | 2 | ハイトマッププロバイダ | IHeightmapProvider + Noise実装 |
| `Terrain/Optimization/` | 1 | 最適化コントローラ | Editor Inspector あり |
| `Terrain/` ルート | 6 | Bootstrap, Streaming, Chunk等 | |

**問題点:**
- `Map/` に `*Test.cs` が **12ファイル** 混在 → Testing に移動すべき
- `DualGrid/` の利用箇所を要検証（孤立している可能性）

### 2.3 Generation アセンブリ (37ファイル)

| サブモジュール | ファイル数 | 機能 |
|---------------|----------|------|
| `Generation/Map/` | 22 | プリミティブ生成・バイオーム・気候 |
| `Generation/` ルート | 15 | Deform統合、メモリ管理、プロファイル |

**Deform 関連ファイルチェーン:**
```
DeformStubs.cs (Deform/)         ← 削除予定（実パッケージ導入時）
    ↕ 名前空間 Deform で衝突リスク
DeformIntegration.cs (18KB)      ← Deform API ラッパー
DeformIntegrationManager.cs (10KB) ← 静的マネージャ
VastcoreDeformManager.cs (1.7KB) ← MonoBehaviour ラッパー
DeformPresetLibrary.cs (Core/)   ← Core にあるが Generation に属すべき
DeformerTab.cs (Editor/)         ← UI タブ
DeformIntegrationTestRunner.cs (Testing/) ← テスト
```

### 2.4 削除・統合候補ファイル

| ファイル | 理由 | アクション |
|---------|------|----------|
| `Scripts/Deform/DeformStubs.cs` | 実Deformパッケージ導入後に不要 | 条件付き削除 (P0-1) |
| `Scripts/Testing/VastcoreTesting.cs` | DummyTest() のみ | 統合テストに吸収 → 削除 |
| `Scripts/Core/GeologicalFormationTest.cs` | Core にテストファイルが混在 | → Testing/ に移動 |
| `Terrain/Map/*Test.cs` (12件) | Terrain に混在するテストファイル | → Testing/ に移動、または NUnit テストに変換 |
| `docs/01_planning/` 内の重複文書 | REFACTORING_PLAN.md と REFACTORING_ACTION_PLAN.md が類似 | 統合 |

---

## 3. フェーズ別開発プラン

---

### Phase A: 安定化 (Stabilization) — 1-2 スプリント

**ゴール**: コンパイル安定性 95、全ブロッカー解消、ビルドが確実に通る状態

#### PA-1: Deform スタブ整理と条件付きコンパイル統一
- **サイズ**: S
- **対象ファイル**:
  - 修正: `Scripts/Deform/DeformStubs.cs` — `#if !DEFORM_PACKAGE` ガード追加
  - 修正: `Scripts/Generation/DeformIntegration.cs` — `#if DEFORM_PACKAGE` ガード確認
  - 修正: `Scripts/Generation/DeformIntegrationManager.cs` — 同上
  - 修正: `Scripts/Generation/VastcoreDeformManager.cs` — 同上
  - 修正: `Scripts/Core/DeformPresetLibrary.cs` — 同上
  - 確認: `Editor/StructureGenerator/Tabs/Deform/DeformerTab.cs` — 既存ガードの確認
- **依存**: なし
- **検証**: Unity Editor でコンパイルエラー 0 を確認

#### PA-2: ProBuilder API 移行調査と Subdivide 代替実装
- **サイズ**: L
- **対象ファイル**:
  - 調査: ProBuilder 6.0.8 API (`UnityEngine.ProBuilder.MeshOperations`)
  - 修正: `Scripts/Terrain/Map/HighQualityPrimitiveGenerator.cs` — 11箇所の TODO 解消
    - L280, L359, L455, L506, L699: Subdivide
    - L558: RebuildFromMesh
    - L1035: SetSmoothingGroup
    - L1043: UV展開
    - L1057-1060: MeshValidation, Optimize
  - 修正: `Scripts/Generation/Map/PrimitiveTerrainGenerator.cs` — L390(RebuildFromMesh), L555(Subdivide)
  - 修正: `Scripts/Generation/Map/PrimitiveModifier.cs` — L44(Subdivide)
  - 新規(必要時): `Scripts/Utilities/Utils/MeshSubdivider.cs` — カスタム Catmull-Clark 実装（ProBuilder API 不在時のフォールバック）
- **依存**: PA-1
- **検証**: HighQualityPrimitiveGenerator の全形状タイプで Subdivide が動作

#### PA-3: asmdef 依存関係の正規化
- **サイズ**: S
- **対象ファイル**:
  - 修正: `Scripts/Camera/Vastcore.Camera.asmdef` — Core, Utilities への参照追加
  - 修正: `Scripts/Generation/Vastcore.Generation.asmdef` — `autoReferenced: false` に変更
  - 修正: `Scripts/Editor/Vastcore.Editor.asmdef` — `Unity.ProBuilder`, `Unity.ProBuilder.Editor` 参照追加
  - 確認: 全10 asmdef の `autoReferenced` を `false` に統一
- **依存**: なし
- **検証**: Unity Editor リロード後にコンパイルエラー 0

#### PA-4: テストファイルの所属整理
- **サイズ**: M
- **対象ファイル**:
  - 移動: `Scripts/Core/GeologicalFormationTest.cs` → `Scripts/Testing/`
  - 移動: `Scripts/Terrain/Map/*Test.cs` (12ファイル) → `Scripts/Testing/TerrainTests/`
    - AdvancedTerrainAlgorithmsTest.cs
    - ArchitecturalGeneratorTest.cs
    - NaturalTerrainFeaturesTest.cs
    - NaturalTerrainValidationTest.cs
    - PrimitiveTerrainTest.cs
    - RuntimeTerrainManagerTest.cs
    - RuntimeGenerationManagerTest.cs
    - SeamlessConnectionManagerTest.cs
    - CrystalStructureGeneratorTest.cs
    - ClimateTerrainFeedbackTest.cs
    - TerrainTexturingSystemTest.cs
    - LODMemorySystemTest.cs / AdvancedLODSystemTest.cs
    - ComprehensivePrimitiveTest.cs
  - 修正: `Scripts/Testing/Vastcore.Testing.asmdef` — 新テストディレクトリが含まれるか確認
  - 削除: `Scripts/Testing/VastcoreTesting.cs` (DummyTestのみ→不要)
- **依存**: なし
- **検証**: テストが Testing アセンブリから正しくコンパイル・実行可能

#### PA-5: Unity Editor コンパイル完全検証
- **サイズ**: S
- **対象ファイル**:
  - 確認: Unity 6000.2.2f1 で全アセンブリのコンパイル
  - 修正: 検出されたエラーを逐次修正
- **依存**: PA-1, PA-3, PA-4
- **検証**: Unity Console でエラー 0、ワーニング最小化
- **成果物**: `docs/04_reports/COMPILE_VERIFICATION_2026-02.md`

**Phase A 完了基準:**
- [ ] コンパイルエラー 0
- [ ] asmdef 依存が全て明示的
- [ ] テストファイルが Testing アセンブリに集約
- [ ] Deform 条件付きコンパイルが統一
- [ ] 健全性スコア: コンパイル安定性 → 95

---

### Phase B: 品質基盤 (Quality Foundation) — 2-3 スプリント

**ゴール**: テストカバレッジ 70、CI/CD 稼働、コード品質 80

#### PB-1: NUnit テスト基盤の構築
- **サイズ**: M
- **対象ファイル**:
  - 修正: `Scripts/Testing/Vastcore.Testing.asmdef` — EditMode/PlayMode 両対応の確認
  - 新規: `Scripts/Testing/EditMode/CoreTests/`
    - `VastcoreSystemManagerTests.cs` — 初期化・シャットダウン・ヘルスチェック
    - `VastcoreErrorHandlerTests.cs` — エラーハンドリングパス
  - 新規: `Scripts/Testing/EditMode/GenerationTests/`
    - `PrimitiveGeneratorTests.cs` — 各形状の生成正当性
    - `DeformIntegrationTests.cs` — Deform API ラッパーの単体テスト
  - 新規: `Scripts/Testing/EditMode/TerrainTests/`
    - `TerrainChunkPoolTests.cs` — プール取得・返却
    - `MarchingSquaresTests.cs` — グリッド計算正当性
  - 整理: `Scripts/Testing/VastcoreIntegrationTestStubs.cs` — 実マネージャ参照に置換、またはモックに正式化
- **依存**: PA-4
- **検証**: `Unity Test Runner > EditMode` で全テストパス

#### PB-2: Player / UI / Camera / Game テスト追加
- **サイズ**: L
- **対象ファイル**:
  - 新規: `Scripts/Testing/EditMode/PlayerTests/`
    - `PlayerControllerTests.cs` — 移動・入力処理
    - `TranslocationSphereTests.cs` — ワープ計算
  - 新規: `Scripts/Testing/EditMode/UITests/`
    - `ModernUIManagerTests.cs` — UI初期化・更新
    - `SliderBasedUISystemTests.cs` — パラメータ連携
  - 新規: `Scripts/Testing/EditMode/CameraTests/`
    - `CameraControllerTests.cs` — 追従・回転
  - 新規: `Scripts/Testing/EditMode/GameTests/`
    - `VastcoreGameManagerTests.cs` — ライフサイクル
  - 修正: `Scripts/Testing/ComprehensiveSystemTest.cs` — L31-33 のコメントアウトを解除し実マネージャ接続
- **依存**: PB-1
- **検証**: カバレッジレポートで 70% 達成

#### PB-3: CI/CD パイプライン稼働
- **サイズ**: M
- **対象ファイル**:
  - 修正: `.github/workflows/unity-tests.yml` — EditMode/PlayMode テスト分離、アーティファクト出力
  - 修正: `.github/workflows/ci.yml` — ビルド検証パイプライン追加
  - 設定: GitHub リポジトリに `UNITY_LICENSE` シークレット追加
  - 新規: `.github/workflows/pr-check.yml` — PR 時の自動チェック（コンパイル + テスト）
- **依存**: PB-1
- **検証**: GitHub Actions でグリーンバッジ取得

#### PB-4: TODO/FIXME 技術的負債の棚卸しと計画的解消
- **サイズ**: M
- **対象ファイル** (31件の TODO を分類):
  - **即時解消** (PA-2 で対応済みの ProBuilder 系を除く):
    - `Scripts/UI/PerformanceMonitor.cs:92` — DebugUI参照修正
    - `Scripts/Game/Managers/VastcoreGameManager.cs:25,231` — TerrainGenerator 接続
    - `Scripts/Testing/TestCases/MemoryManagementTestCase.cs:144,161` — ObjectPool 実装
    - `Scripts/Testing/TestCases/UISystemTestCase.cs:297` — UIStyle 比較実装
    - `Editor/StructureGenerator/Core/GlobalSettingsTab.cs:31` — 設定ロード/保存
  - **Phase C で対応**:
    - `Scripts/Core/GeologicalFormationGenerator.cs:136,143` — エロージョン処理
    - `Editor/StructureGenerator/Tabs/Editing/CompositionTab.cs:563` — ブレンド実装
    - `Editor/StructureGenerator/Tabs/Generation/BasicStructureTab.cs:137` — Arch/Pyramid
  - **対応不要** (外部ライブラリ):
    - `TextMesh Pro/Examples & Extras/Scripts/` 内の3件
- **依存**: PA-2
- **検証**: `grep -r "TODO\|FIXME" Assets/Scripts/` が 10件以下

#### PB-5: Core アセンブリ分割
- **サイズ**: L
- **対象ファイル**:
  - 新規 asmdef: `Scripts/Core/Diagnostics/Vastcore.Diagnostics.asmdef` (→ Core, Utilities)
  - 移動: `VastcoreDiagnostics.cs` → `Scripts/Core/Diagnostics/`
  - 移動: `VastcoreDebugVisualizer.cs` → `Scripts/Core/Diagnostics/`
  - 移動: `DeformPresetLibrary.cs` → `Scripts/Generation/` (Deform系と統合)
  - 移動: `GeologicalFormationGenerator.cs` → `Scripts/Terrain/`
  - 移動: `TerrainSynthesizer.cs` → `Scripts/Terrain/`
  - 移動: `TerrainErrorRecovery.cs` → `Scripts/Terrain/`
  - 移動: `PrimitiveErrorRecovery.cs` → `Scripts/Terrain/`
  - 移動: `SceneManager.cs`, `SceneNavigation.cs` → `Scripts/Game/`
  - 修正: 移動先の各 asmdef に参照追加
  - 修正: 移動元を参照していた全ファイルの using/namespace 更新
- **依存**: PA-3
- **検証**: Core アセンブリが 10ファイル以下（Interfaces + 共通基盤のみ）

**Phase B 完了基準:**
- [ ] NUnit テスト 70+ 件
- [ ] 全アセンブリにテストが存在
- [ ] GitHub Actions CI がグリーン
- [ ] TODO/FIXME が 10件以下
- [ ] Core アセンブリが純粋な基盤のみ
- [ ] 健全性スコア: テストカバレッジ → 70、コード品質 → 80

---

### Phase C: 機能完成 (Feature Completion) — 3-4 スプリント

**ゴール**: Phase 3 (Deform) 完了、CompositionTab CSG 検証完了、Phase 5 着手

#### PC-1: Deform パッケージ正式導入と統合検証
- **サイズ**: L
- **対象ファイル**:
  - 修正: `Packages/manifest.json` — Deform パッケージ バージョン確定
  - 削除: `Scripts/Deform/DeformStubs.cs` (実パッケージで置換)
  - 修正: `Scripts/Generation/DeformIntegration.cs` — 実 API で全 Deformer 検証
    - BendDeformPreset, NoiseDeformPreset, ScaleDeformPreset 実動作確認
  - 修正: `Scripts/Generation/DeformIntegrationManager.cs` — シングルトンパターン検証
  - 修正: `Scripts/Generation/VastcoreDeformManager.cs` — MonoBehaviour ライフサイクル
  - 修正: `Scripts/Core/DeformPresetLibrary.cs` (PB-5 後は Generation 配下) — 全20+ プリセットの検証
  - 修正: `Editor/StructureGenerator/Tabs/Deform/DeformerTab.cs` — 8種 Deformer UI 実動作
  - 修正: `Scripts/Testing/DeformIntegrationTestRunner.cs` — 自動テスト拡充
- **依存**: PA-1, PB-5
- **検証**: Unity Editor で全 Deformer タイプの適用・アニメーション動作確認
- **成果物**: Phase 3 を **完了** にマーク

#### PC-2: CompositionTab CSG 実動作検証と Blend 実装
- **サイズ**: L
- **対象ファイル**:
  - 修正: `Editor/StructureGenerator/Tabs/Editing/CompositionTab.cs`
    - Union/Intersection/Difference の実動作確認（Undo, 元オブジェクト処理, チェイン）
    - L563: Blend 機能実装 (Layered/Surface/Adaptive/Noise)
  - 修正: `Editor/StructureGenerator/Utils/ProBuilderInternalCsgProvider.cs` — reflection 安定化
  - 修正: `Editor/StructureGenerator/Utils/ParaboxCsgProvider.cs` — フォールバック検証
  - 修正: `Editor/StructureGenerator/Utils/CsgProviderResolver.cs` — 自動選択ロジック
  - 新規: `Scripts/Testing/EditMode/EditorTests/CompositionTabTests.cs`
- **依存**: PA-2
- **検証**: CSG Union/Subtract/Intersect + Blend の全操作が Editor 上で動作

#### PC-3: StructureGenerator 残タスク完了
- **サイズ**: M
- **対象ファイル**:
  - 修正: `Editor/StructureGenerator/Tabs/Generation/BasicStructureTab.cs:137` — Arch, Pyramid 対応
  - 修正: `Editor/StructureGenerator/Core/GlobalSettingsTab.cs:31` — 設定ロード/保存機能
  - 修正: `Editor/StructureGenerator/Tabs/Editing/RandomControlTab.cs` — SG-2 残テスト項目
  - 新規(必要時): `Editor/StructureGenerator/Tabs/Editing/RandomControlPresetManager.cs` — RC-1 プリセット管理
- **依存**: PC-2
- **検証**: StructureGenerator 全7タブの機能テスト完了

#### PC-4: GeologicalFormation エロージョン実装
- **サイズ**: M
- **対象ファイル**:
  - 修正: `Scripts/Core/GeologicalFormationGenerator.cs` (PB-5 後は Terrain 配下)
    - L136: erosionRate パラメータの実装
    - L143: エロージョンシミュレーション実装
  - 新規: `Scripts/Terrain/Erosion/HydraulicErosion.cs` — 水力エロージョン
  - 新規: `Scripts/Terrain/Erosion/ThermalErosion.cs` — 熱エロージョン
  - 新規: `Scripts/Testing/EditMode/TerrainTests/ErosionTests.cs`
- **依存**: PB-5
- **検証**: エロージョンの視覚的出力 + 単体テスト

#### PC-5: VastcoreGameManager TerrainGenerator 接続
- **サイズ**: M
- **対象ファイル**:
  - 修正: `Scripts/Game/Managers/VastcoreGameManager.cs`
    - L25: TerrainGenerator フィールド有効化
    - L231: CinematicCamera.Setup への TerrainGenerator 引数追加
  - 修正: `Scripts/Testing/ComprehensiveSystemTest.cs`
    - L31-33: RuntimeTerrainManager 等の参照を有効化
    - L243, L250, L257: 実装値を返す
  - 新規: `Scripts/Testing/PlayMode/IntegrationTests/GameManagerIntegrationTests.cs`
- **依存**: PB-2
- **検証**: ゲーム起動 → 地形生成 → プレイヤー配置の一連フロー

**Phase C 完了基準:**
- [ ] Phase 3 (Deform統合) 完了
- [ ] CompositionTab CSG + Blend 動作確認
- [ ] StructureGenerator 全タブ機能完了
- [ ] GameManager → TerrainGenerator 接続済み
- [ ] 健全性スコア: アーキテクチャ → 85

---

### Phase D: 最適化・拡張 (Optimization & Extension) — 3-5 スプリント

**ゴール**: Phase 5 (高度合成システム) 実装、パフォーマンス目標達成

#### PD-1: Phase 5 — 高度合成システム実装
- **サイズ**: XL
- **対象ファイル**:
  - 新規: `Scripts/Generation/Composition/AdvancedCompositionSystem.cs`
    - ComposeModels — 複数メッシュ統合
    - GenerateLOD — LODGroup 自動生成
    - OptimizeMesh — メッシュ最適化
  - 新規: `Scripts/Generation/Composition/MeshBlender.cs` — 体積ブレンド
  - 新規: `Scripts/Generation/Composition/LODGenerator.cs` — LOD レベル生成
  - 修正: `Scripts/Terrain/Map/AdvancedPrimitiveLODSystem.cs` — LODGenerator 連携
  - 新規: `Scripts/Testing/EditMode/GenerationTests/CompositionSystemTests.cs`
- **依存**: PC-2
- **検証**: 複数メッシュ → 合成 → LOD 生成の一連パイプライン

#### PD-2: Phase 6 — ランダム制御システム実装
- **サイズ**: L
- **対象ファイル**:
  - 修正: `Editor/StructureGenerator/Tabs/Editing/RandomControlTab.cs` — 高度ランダム制御
  - 新規: `Scripts/Generation/Random/ControlledRandomSystem.cs`
    - シード管理・再現可能ランダム
    - パラメータ制約システム
  - 新規: `Scripts/Generation/Random/RandomPresetLibrary.cs` — ScriptableObject ベース
  - 新規: `Scripts/Generation/Random/BlendShapeRandom.cs` — ブレンドシェイプランダム化
  - 修正: `Editor/StructureGenerator/Tabs/Editing/RandomControlTab.cs` — UI拡張
  - 新規: `Scripts/Testing/EditMode/GenerationTests/RandomSystemTests.cs`
- **依存**: PC-3
- **検証**: シード固定で同一出力再現 + プリセット保存/読み込み

#### PD-3: パフォーマンス最適化
- **サイズ**: L
- **対象ファイル**:
  - 修正: `Scripts/Terrain/Map/RuntimeGenerationManager.cs` — Job System 導入
  - 修正: `Scripts/Terrain/Map/PrimitiveTerrainManager.cs` — LOD 更新の Burst 化
  - 修正: `Scripts/Terrain/Map/PrimitiveTerrainObjectPool.cs` — プール効率改善
  - 修正: `Scripts/Terrain/Map/PrimitiveMemoryManager.cs` — GC プレッシャー削減
  - 新規: `Scripts/Terrain/Jobs/TerrainHeightJob.cs` — Burst コンパイル対応ジョブ
  - 新規: `Scripts/Terrain/Jobs/MeshGenerationJob.cs` — メッシュ生成並列化
  - 修正: `Scripts/Testing/PerformanceTestingSystem.cs` — ベンチマーク更新
- **依存**: PC-5
- **検証**: フレームタイム 16ms 以下 (60FPS)、GC Alloc 1MB/frame 以下

#### PD-4: 巨大ファイル分割リファクタリング
- **サイズ**: L
- **対象ファイル**:
  - 分割: `BiomeSpecificTerrainGenerator.cs` (1706行)
    → `BiomeTerrainCore.cs` + `BiomeVegetation.cs` + `BiomeWeathering.cs`
  - 分割: `HighQualityPrimitiveGenerator.cs` (1498行)
    → 形状ごとに個別クラス化 (Generation/Map/ の Factory パターン活用)
  - 分割: `CompoundArchitecturalGenerator.cs` (1309行)
    → `ArchitecturalCore.cs` + `ArchitecturalDecorator.cs`
  - 分割: `NaturalTerrainFeatures.cs` (1083行)
    → `RiverGenerator.cs` + `CliffGenerator.cs` + `CaveGenerator.cs`
- **依存**: PB-1 (テストが先に存在していること)
- **検証**: 全既存テストがパス + 新クラスの単体テスト追加

**Phase D 完了基準:**
- [ ] Phase 5 (高度合成) 実装完了
- [ ] Phase 6 (ランダム制御) 実装完了
- [ ] 60FPS 安定動作
- [ ] 巨大ファイル全て 500行以下に分割
- [ ] 全 Phase (1-6) 完了

---

### Phase E: 仕上げ (Polish) — 継続的

**ゴール**: ドキュメント整合性 75、UI 近代化、プロダクション品質

#### PE-1: ドキュメント SSOT 確立
- **サイズ**: M
- **対象ファイル**:
  - 修正: `docs/01_planning/DEV_PLAN.md` — 本ロードマップとの整合 (2025年1月 → 最新状態に)
  - 統合: `docs/01_planning/REFACTORING_PLAN.md` + `REFACTORING_ACTION_PLAN.md` → 1ファイルに
  - 修正: `docs/01_planning/ISSUES_BACKLOG.md` — 完了タスクのアーカイブ、新タスクの反映
  - 削除: 陳腐化したドキュメント (重複・古い計画書)
  - 修正: `AI_CONTEXT.md` — 最新アーキテクチャ図
- **依存**: なし
- **検証**: 全ドキュメントが最新コードと一致

#### PE-2: DualGrid サブモジュール要否判定
- **サイズ**: S
- **対象ファイル**:
  - 調査: `Scripts/Terrain/DualGrid/` (8ファイル) の利用箇所
  - 判定: 他モジュールから参照されているか Grep で確認
  - アクション: 未使用なら `_Experimental/` に移動 or 削除
- **依存**: なし
- **検証**: 利用箇所の有無を文書化

#### PE-3: uGUI → UI Toolkit 移行 (オプショナル)
- **サイズ**: XL
- **対象ファイル**:
  - 修正: `Scripts/UI/` 全11ファイル
    - `ModernUIManager.cs` — VisualElement ベースに
    - `SliderBasedUISystem.cs` — UIToolkit Slider に
    - `PerformanceMonitor.cs` — UIToolkit ラベルに
    - 他8ファイル
  - 修正: `Scripts/UI/Vastcore.UI.asmdef` — UIElements 参照追加、uGUI/TMPro 参照削除
  - 新規: `Assets/UI Toolkit/` — USS/UXML アセット
- **依存**: PB-2 (UIテストが先に存在)
- **検証**: 全 UI が UIToolkit で動作、uGUI 依存ゼロ

#### PE-4: シーン整理
- **サイズ**: S
- **対象ファイル**:
  - 整理: `Assets/Scenes/` — テストシーンをサブフォルダに分類
  - 削除: 使われていないシーン
  - 修正: EditorBuildSettings のシーンリスト更新
- **依存**: なし
- **検証**: ビルド設定のシーンが全て有効

#### PE-5: Terrain ストリーミング完成
- **サイズ**: L
- **対象ファイル**:
  - 修正: `Scripts/Terrain/TerrainStreamingController.cs` — 実装完成
  - 修正: `Scripts/Terrain/TerrainChunkPool.cs` — ストリーミング対応
  - 修正: `Scripts/Terrain/TerrainGridBootstrap.cs` — 動的ローディング
  - 新規: `Scripts/Testing/PlayMode/TerrainStreamingTests.cs`
- **依存**: PD-3
- **検証**: 大規模マップでのシームレスなロード/アンロード

**Phase E 完了基準:**
- [ ] ドキュメントが最新コードと完全一致
- [ ] UI 移行 (実施する場合) 完了
- [ ] テストシーン整理完了
- [ ] 健全性スコア: ドキュメント整合性 → 75

---

## 4. リスク評価

| フェーズ | リスク | 影響度 | 対策 |
|---------|--------|--------|------|
| A | ProBuilder 6.0.8 に Subdivide 相当 API がない | 高 | カスタム MeshSubdivider 実装をフォールバック |
| A | Deform パッケージの Unity 6 互換性 | 中 | 条件付きコンパイルで段階的導入 |
| B | テスト追加で既存コードの隠れバグ露出 | 中 | バグ修正をスプリントバックログに追加 |
| C | CSG reflection が ProBuilder バージョンアップで破損 | 高 | ParaboxCsgProvider をフォールバックとして維持 |
| D | Job System 導入でレースコンディション | 中 | 段階的導入 + PlayMode テスト |
| E | uGUI → UITK 移行の工数過大 | 低 | オプショナル。現 uGUI が安定なら後回し |

---

## 5. フェーズ間依存関係

```
Phase A (安定化)
    ├── PA-1 ──→ PC-1 (Deform正式導入)
    ├── PA-2 ──→ PC-2 (CSG/Composition)
    ├── PA-3 ──→ PB-5 (Core分割)
    ├── PA-4 ──→ PB-1 (テスト基盤)
    └── PA-5 ──→ (全後続)

Phase B (品質基盤)
    ├── PB-1 ──→ PB-2, PD-4
    ├── PB-2 ──→ PC-5, PE-3
    ├── PB-3 ──→ (独立)
    ├── PB-4 ──→ (独立)
    └── PB-5 ──→ PC-1, PC-4

Phase C (機能完成)
    ├── PC-1 ──→ PD-1
    ├── PC-2 ──→ PD-1
    ├── PC-3 ──→ PD-2
    ├── PC-4 ──→ (独立)
    └── PC-5 ──→ PD-3

Phase D (最適化) → Phase E (仕上げ)
```

---

## 6. 次のアクション

**即座に開始すべきタスク:**
1. **PA-1**: Deform スタブ整理 (S) — ブロッカー解消
2. **PA-3**: asmdef 正規化 (S) — ブロッカー解消
3. **PA-4**: テストファイル整理 (M) — 並行可能

**その後:**
4. **PA-2**: ProBuilder API 調査 (L) — PA-1 完了後
5. **PA-5**: コンパイル完全検証 (S) — PA-1,3,4 完了後

---

**最終更新**: 2026-02-09
**SSOT**: このファイルが開発ロードマップの唯一の正とする

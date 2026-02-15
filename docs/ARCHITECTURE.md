# VastCore — アーキテクチャ概観

> **上位SSOT**: [SSOT_WORLD.md](SSOT_WORLD.md)
> 本文書はモジュール構成・依存関係・責務を鳥瞰するためのリファレンスである。

**最終更新**: 2026-02-14

---

## 1. アセンブリ依存DAG

```
Vastcore.Utilities  (依存なし)
    ↑
Vastcore.Core       (→ Utilities)
    ↑
    ├── Vastcore.Generation  (→ Core, Utilities, ProBuilder)
    │       ↑
    ├── Vastcore.Terrain     (→ Core, Utilities, Generation, ProBuilder, TMPro)
    │       ↑
    ├── Vastcore.Player      (→ Core, Utilities, Terrain, Generation, ProBuilder, TMPro, InputSystem)
    │       ↑
    ├── Vastcore.Camera      (→ Core, Utilities ※要正規化)
    │       ↑
    ├── Vastcore.UI           (→ Core, Utilities, Player, TMPro, DebugUI, InputSystem)
    │       ↑
    └── Vastcore.Game        (→ Core, Utilities, Player, Terrain, Camera, Generation, UI)

Vastcore.Editor   (→ Core, Utilities, Generation, Terrain, TMPro)  [Editor only]
Vastcore.Testing  (→ Core, Utilities, Generation, Terrain, Player, UI, TestFramework)
```

**既知の課題** (ROADMAP_2026 PA-3 参照):
- `Vastcore.Camera` の依存が未宣言
- `Vastcore.Generation` の `autoReferenced=true` が他と不統一

---

## 2. アセンブリ別責務

### 2.1 Vastcore.Utilities
- 共通ヘルパー、数学ユーティリティ
- 外部依存なし。全アセンブリから参照可能

### 2.2 Vastcore.Core
- 共通インターフェース (`Interfaces/` 5ファイル)
- エラーハンドリング (`VastcoreErrorHandler`)
- システムオーケストレーション (`VastcoreSystemManager`)
- ログ出力 (`LogOutputHandler`)
- 型定義 (`GenerationPrimitiveType`, `RockLayerPhysicalProperties`)

**分割候補** (ROADMAP_2026 PB-5): 診断系 → `Vastcore.Diagnostics`、地形系 → `Vastcore.Terrain`

### 2.3 Vastcore.Generation
- **プロシージャル生成の中核**
- プリミティブ生成 (`Map/` 22ファイル)
- Deform統合 (`DeformIntegration`, `DeformIntegrationManager`, `VastcoreDeformManager`)
- メモリ管理、プロファイル

### 2.4 Vastcore.Terrain
- **地形メッシュの管理と表示** (77ファイル — 最大アセンブリ)
- `Map/` — プリミティブ地形、LOD、テクスチャ (40+ ファイル)
- `MarchingSquares/` — マーチングスクエア地形 (8ファイル)
- `DualGrid/` — デュアルグリッドシステム (8ファイル, 実験的)
- `GPU/` — GPU地形生成 (2ファイル)
- `Cache/` — キャッシュシステム (2ファイル)
- チャンク管理、ストリーミング

### 2.5 Vastcore.Player
- プレイヤー移動制御 (`AdvancedPlayerController`)
- ワープ移動 (`TranslocationSphere`)
- 入力処理

### 2.6 Vastcore.Camera
- カメラ追従・回転
- シネマティックカメラ

### 2.7 Vastcore.UI
- HUD管理 (`ModernUIManager`)
- パラメータ制御 (`SliderBasedUISystem`)
- パフォーマンスモニタ

### 2.8 Vastcore.Game
- ゲームライフサイクル (`VastcoreGameManager`)
- シーン管理・遷移

### 2.9 Vastcore.Editor
- **StructureGenerator** — 7タブ構成のメインEditor拡張
  - Generation: Basic / Advanced / Distribution
  - Editing: Operations (CSG) / Composition / Random
  - Settings: Relationships
- Inspector拡張

### 2.10 Vastcore.Testing
- EditMode / PlayMode テスト
- 統合テスト (`ComprehensiveSystemTest`)
- パフォーマンステスト

---

## 3. StructureGenerator の設計思想

> 出典: [DEV_PLAN_ARCHIVE_2025-01.md](01_planning/DEV_PLAN_ARCHIVE_2025-01.md) — 目的関数のアーカイブ

StructureGenerator は「巨大な人工構造物のプロシージャル生成」という目的関数を実現するためのメインツールである。

**6段階の機能階層**:
1. **Phase 1** (完了): 基本関係性システム — 9種類の関係性による自動配置
2. **Phase 2** (完了): 形状制御システム — ツイスト、テーパー、Boolean演算
3. **Phase 3** (進行中): Deformシステム統合 — 20種類以上のDeformer
4. **Phase 4** (完了): パーティクル的配置 — 8種類の配置パターン
5. **Phase 5** (未着手): 高度合成システム — メッシュ統合、LOD生成
6. **Phase 6** (未着手): ランダム制御システム — シード管理、再現可能ランダム

**重要**: これらのPhaseは DEV_PLAN (2025-01) で定義された「目的関数」である。
現行の開発優先度は DEVELOPMENT_ROADMAP_2026 の Phase A-E（工学的制約条件）に従う。

---

## 4. 依存関係の方向ルール

```
上位（依存される側）          下位（依存する側）
─────────────────          ────────────────
Utilities                  ← すべて
Core                       ← Generation, Terrain, Player, Camera, UI, Game
Generation                 ← Terrain, Player
Terrain                    ← Player, Game
Player                     ← UI, Game
Camera                     ← Game
```

**禁止**: 下位から上位への逆依存、循環依存

---

## 5. 将来のドキュメント分割ポイント

以下の領域は、実装が進んだ段階で独立ドキュメントに分割予定:

| 候補ファイル | 内容 | 分割トリガー |
|------------|------|------------|
| `GENERATION_PIPELINE.md` | Recipe / QualityGate / DerivedFields | Phase C-D 完了時 |
| `MAP_CAPTURE.md` | 観測装置（衛星/タイル/レイヤー） | Terrain ストリーミング完成時 |
| `STRUCTURES_GUIDE.md` | StructureGenerator の問い合わせ/変形規約 | Phase 5-6 完了時 |

---

**参照**: [SSOT_WORLD.md](SSOT_WORLD.md) | [DEVELOPMENT_ROADMAP_2026.md](01_planning/DEVELOPMENT_ROADMAP_2026.md) | [DOCS_INDEX.md](DOCS_INDEX.md)

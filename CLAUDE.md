# VastCore Terrain Engine

GPU 加速テレインエンジン。Unity 6 / C#。DualGrid + HeightMap + デザイナーPrefab(スタンプ) アーキテクチャ。

## Project Context

- プロジェクト名: VastCore
- 環境: Unity 6000.3.3f1 (URP) / C# (.NET Standard 2.1, C# 9.0制約)
- ブランチ戦略: main (trunk-based)
- 現フェーズ: Phase C (機能完成) 着手中
- 直近の状態 (2026-03-17 session 2):
  - ミッション逆算分析を実施 — 構造物生成(StructureGenerator)と地形配置(DualGrid/Stamp)の分断を特定
    - StructureGenerator: 22ファイル/8タブ/ArchitecturalGenerator(8型) — Editor専用、地形との接続なし
    - DualGrid/Stamp: 配置システム完成 — 構造物生成側からの入力パスなし
    - 「構造物を作る工場」と「設置する現場」が分断。運搬パイプラインが欠落
  - 未決定: 最終体験像(オーサリングツール/ランタイム自動生成/ハイブリッド)
  - 未決定: 「ユニーク」の実現手段(パラメトリック変異/WFC/CSGコンポジション/段階的)
  - パイプライン統合完了(前セッション):
    - Erosion→Terrain, Stamp→Terrain高さ, DualGrid+Terrain統合オーケストレータ
  - Phase C PC-1〜PC-5 コア実装完了。Unity実機検証: 未実施
  - Bootstrap資産一括生成: Vastcore > Bootstrap > Create All Required Assets
  - spec-index.json乖離: SP-009(todo/0%だが実装済み), Erosion(エントリなし), SP-007(65%だが実態はそれ以上)

## Key Paths

- Source: `Assets/Scripts/`
- DualGrid: `Assets/Scripts/Terrain/DualGrid/`
- Tests: `Assets/Tests/EditMode/`
- SSOT: `docs/SSOT_WORLD.md` → `docs/WORKFLOW_STATE_SSOT.md`
- Architecture: `docs/02_design/ASSEMBLY_ARCHITECTURE.md`
- Tasks: `docs/tasks/TASK_*.md`
- Spec Index: `docs/spec-index.json`

## Architecture

### SSOT階層

```text
SSOT_WORLD.md (最上位仕様)
  ├── WORKFLOW_STATE_SSOT.md (運用状態)
  ├── DEVELOPMENT_ROADMAP_2026.md (ロードマップ)
  ├── CLAUDE.md (本文書、セッション運用)
  └── docs/tasks/TASK_*.md (個別チケット)
```

### アセンブリ依存方向

```text
Utilities (依存なし)
  ↑
Core (Utilities)
  ↑
Generation / Terrain / WorldGen / MapGenerator
  ↑
Player / Camera / UI / Game
  ↑
Editor (全上位レイヤー参照可)
  ↑
Testing (テスト用スタブ・ヘルパー)
```

### Terrain Architecture (正)

- **DualGrid**: Hexベース不整形グリッド。Phase 1完了 (トポロジ+リラクゼーション)
- **HeightMap**: 2D画像ベース高度。基盤実装済み (NoiseHeightmapProvider)
- **Prefabスタンプ**: デザイナーPrefabをDualGridセルに配置。コア実装完了 (2026-03-16)
- **WFC**: 未実装・未設計
- **バリエーションエンジン**: 仕様検討中

### 凍結項目

- 3D Voxel / Marching Cubes (TASK_026) — 重すぎる
- Marching Squares メッシュ生成 — Prefabスタンプに置き換え
- テスト拡充優先 — 91テストで十分

## Rules

- Respond in Japanese
- No emoji
- Keep responses concise — avoid repeating file contents back to the user
- Do NOT read `docs/archive/` unless explicitly asked

## Code Standards

- Naming: PascalCase (public), _camelCase (params), m_CamelCase (private fields)
- `#region` グループ化、日本語 XML doc コメント
- `[System.Serializable]` 全データ構造
- `VastcoreLogger.Instance.LogInfo()` — Debug.Log 禁止
- 引数なし struct コンストラクタ禁止 (C# 9.0制約)
- 同名型の複数アセンブリ定義禁止
- 下位→上位の asmdef 参照追加禁止 (循環参照)
- `ProjectSettings/`, `Packages/` の相談なしの変更禁止

## Decision Log

| 日付       | 決定事項                                 | 選択肢                               | 決定理由                                         |
| ---------- | ---------------------------------------- | ------------------------------------ | ------------------------------------------------ |
| 2026-03-06 | DualGrid+HeightMap+Prefabスタンプを正式アーキテクチャに | Voxel/MarchingCubes/DualGrid+Prefab | デザイナー介入最大化。低コストバリエーション量産 |
| 2026-03-06 | 3D Voxel/Marching Cubes凍結 (TASK_026) | 凍結/継続 | 重すぎる。Prefabスタンプ方針と合わない |
| 2026-03-06 | テスト拡充→機能実装優先に移行 | テスト拡充継続/機能実装優先 | 75テストで十分。コア機能が0% |
| 2026-03-16 | Prefabスタンプは単セルから開始 | 単セル先行/マルチセル込み | 最小実装で方向性検証。マルチセルは次スライス |
| 2026-03-16 | StampRegistry占有管理はセルID単位 | セルID/Hex座標/空間ハッシュ | Cell.Idで一意識別可能。パフォーマンスは将来課題 |
| 2026-03-17 | マルチセルフットプリントはヘックス単位で全サブセルを占有 | サブセル単位/ヘックス単位/混合 | 大型構造物はヘックス全体をカバーする。単一セルは従来通りサブセル1つ |
| 2026-03-17 | フットプリント回転はSG-2スコープ外 | 回転連動/固定/後送り | ヘックス座標系は60度回転が自然だが90度回転とは不整合。別スライスで対応 |
| 2026-03-17 | Erosion→TerrainはTerrainChunk.Build内で適用 | Build内/別パス/後処理 | Provider→Erosion→SetHeightsの直線フローが最小変更。ErosionSettings SOでオプショナル |
| 2026-03-17 | Stamp高さはIHeightSampler抽象で接続 | 直接Terrain参照/抽象化/固定値 | DualGridがUnity Terrainに直接依存しない設計。将来のVoxel地形等にも対応可能 |
| 2026-03-17 | TerrainWithStampsBootstrapで統合オーケストレーション | 既存Bootstrap拡張/新規/手動配置 | TerrainGridBootstrapとは独立にDualGrid統合版を提供。既存コード非破壊 |
| 2026-03-17 | StructureGenerator→DualGrid/Stamp間のパイプライン欠落を特定 | N/A (分析結果) | ミッション逆算で発見。構造物生成(22ファイル/8タブ)と配置(DualGrid/Stamp)が完全分断。次回要設計判断 |
| 2026-03-17 | 最終体験像・バリエーション手段は未決定のまま中断 | オーサリング/ランタイム/ハイブリッド, パラメトリック/WFC/CSG/段階的 | 次回セッションの最優先HUMAN_AUTHORITY判断事項 |

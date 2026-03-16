# VastCore Terrain Engine

GPU 加速テレインエンジン。Unity 6 / C#。DualGrid + HeightMap + デザイナーPrefab(スタンプ) アーキテクチャ。

## Project Context

- プロジェクト名: VastCore
- 環境: Unity 6000.3.3f1 (URP) / C# (.NET Standard 2.1, C# 9.0制約)
- ブランチ戦略: main (trunk-based)
- 現フェーズ: Phase C 完了 + SG-1 完了。Phase D 設計中
- 直近の状態 (2026-03-16):
  - Phase C品質監査完了: HIGH指摘7件・MEDIUM指摘4件修正済み、BlendSettings.csデッドコード削除
  - MG-1 MapGenerator統合完了 (21→19 asmdef)
  - SG-1 Prefabスタンプ配置コア実装完了+push済み
  - EditModeテスト: 91件 (75既存 + 16新規)
  - 次: Phase D仕様棚卸し + spec整備

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

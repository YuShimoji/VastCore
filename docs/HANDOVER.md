> **上位SSOT**: [SSOT_WORLD.md](SSOT_WORLD.md) | **索引**: [DOCS_INDEX.md](DOCS_INDEX.md)

# VastCore — Handover / ワークフロー仕様書

**最終更新**: 2026-03-09
**Actor**: Claude Code (Opus 4.6)
**Type**: Phase C完了 + コード品質監査 + MapGenerator統合

---

## 1. 現在の状態

| 項目 | 値 |
|------|-----|
| **ブランチ** | `main` (trunk-based, origin/main と同期済み) |
| **Unity** | 6000.3.3f1 (URP) |
| **C#制約** | .NET Standard 2.1, C# 9.0 |
| **コンパイル** | PASS |
| **EditModeテスト** | 75/75 PASS |
| **PlayModeテスト** | 0件 (未着手、ゲート対象) |
| **現フェーズ** | Phase A/B/C 完了 → Phase D 未着手 |

---

## 2. プロジェクト構造（2026-03-08 整理後）

```text
VastCore/
├── CLAUDE.md                  ← 運用SSOT（セッション運用プロトコル + Project Context）
├── AGENTS.md                  ← AIエージェント行動規約
├── README.md                  ← プロジェクト概要
├── CHANGELOG.md               ← 変更履歴
│
├── docs/                      ← ドキュメント正本
│   ├── SSOT_WORLD.md          ← 最上位仕様（憲法）
│   ├── WORKFLOW_STATE_SSOT.md ← 実行状態SSOT
│   ├── HANDOVER.md            ← 本文書（成果物SSOT）
│   ├── DOCS_INDEX.md          ← 全ドキュメント索引
│   ├── ARCHITECTURE.md        ← モジュール/依存/責務の鳥瞰
│   ├── MILESTONE_PLAN.md      ← マイルストーン追跡
│   ├── 01_planning/           ← 計画・ロードマップ
│   ├── 02_design/             ← 設計仕様
│   ├── 03_guides/             ← ガイド・手順書
│   ├── 04_reports/            ← レポート・検証記録
│   ├── tasks/                 ← タスクチケット
│   ├── terrain/               ← 地形システム固有
│   ├── inbox/                 ← 一時レポート受信箱
│   └── EXAMPLES/              ← Mermaid図テンプレート
│
├── Assets/
│   ├── Scripts/               ← C#ソースコード（19 asmdef）
│   ├── Editor/                ← Editorツール（StructureGenerator等）
│   ├── Resources/MapGenerator/ ← MapGenerator用リソース（MG-1統合後）
│   ├── Tests/                 ← EditMode/PlayModeテスト
│   └── _Scenes/              ← シーンファイル
│
├── Documentation/
│   └── Concept Arts/          ← コンセプトアート画像のみ（24枚）
│
└── .serena/                   ← Serena MCP設定（git管理外）
```

### 削除済みディレクトリ（git履歴に保存）

| ディレクトリ | 削除理由 | 移行先 |
|-------------|---------|--------|
| `.shared-workflows` | gitサブモジュール廃止 | CLAUDE.md に運用プロトコル移行 |
| `prompts/` | Windsurf Orchestrator/Worker フレームワーク | CLAUDE.md のサブエージェント委譲 |
| `openspec/` | OpenSpec変更提案プロセス | CLAUDE.md SPEC FIRST |
| `docs/windsurf_workflow/` | Windsurf IDE固有ワークフロー | CLAUDE.md |
| `Documentation/Design,Guides,Planning,QA/` | 転送スタブ | docs/ 配下に移行済み |
| `AI_CONTEXT.md` | Orchestrator Protocol 前提の旧管理 | CLAUDE.md に移行 |
| `REPORT_CONFIG.yml` | Orchestrator レポート設定 | 廃止 |
| `.cursorrules`, `.cursor/` | Cursor IDE 固有設定 | CLAUDE.md に移行 |
| `docs/03_guides/ORCHESTRATION_PROMPT.md` | Orchestrator/Worker プロンプト | CLAUDE.md に移行 |
| `docs/inbox/WORKER_PROMPT_*` | Worker用プロンプト（全タスク完了済み） | 廃止 |
| `Assets/Docs/`, `Assets/Documentation/*.md` | レガシー仕様書・旧ロードマップ | docs/ に集約済み |
| `.github/workflows/ci.yml, sync-issues.yml` | 無機能化済み Shared Workflows | 廃止 |

---

## 3. SSOT階層と運用フロー

```
SSOT_WORLD.md                       ← プロジェクトの目的・構造・優先順位
  │
  ├── DEVELOPMENT_ROADMAP_2026.md   ← Phase A-E の計画と優先度
  ├── WORKFLOW_STATE_SSOT.md        ← 現在の実行状態（Done条件、Mission）
  ├── CLAUDE.md                     ← セッション運用（HUMAN_AUTHORITY、検証ポリシー等）
  ├── HANDOVER.md                   ← 成果物SSOT（本文書）
  └── docs/tasks/TASK_*.md          ← 個別実装チケット
```

### 変更管理フロー（CLAUDE.md準拠）

1. **SPEC FIRST**: 仕様を先に言語化、docs/ に記録
2. **HUMAN_AUTHORITY**: UX/設計/仕様/ビジネスに関わる変更は選択肢提示→承認
3. **PLAN MODE**: 複数ファイル/レイヤーにまたがる変更はプラン提示→承認
4. **VERIFICATION POLICY**: 完了条件を先に定義 → 実行 → 機械的検証
5. **サブエージェント委譲**: 軽量タスクはSonnetに委譲しコスト効率化

### ドキュメント運用ルール

- 新規ドキュメントは `docs/` 配下に配置
- `DOCS_INDEX.md` に必ず追記
- SSOT階層での位置を明示、上位への逆リンクを先頭に記載
- 数値・構成の情報源は実コード（ドキュメント間の相互参照だけで整合性を取らない）

---

## 4. アセンブリアーキテクチャ概要

詳細は [ASSEMBLY_ARCHITECTURE.md](02_design/ASSEMBLY_ARCHITECTURE.md) を参照。

### 依存方向

```
Utilities (依存なし)
  ↑
Core (Utilities)
  ↑
Generation / Terrain / WorldGen
  ↑
Player / Camera / UI / Game
  ↑
Editor (全上位レイヤー参照可)
  ↑
Testing (テスト用スタブ・ヘルパー)
```

### 禁止事項（AGENTS.md より）

- `Debug.Log` → `VastcoreLogger.Instance.LogInfo(...)`
- 下位→上位の asmdef 参照追加（循環参照）
- 同名型の複数アセンブリ定義
- C# 9.0 非対応構文（引数なし struct コンストラクタ等）

---

## 5. 開発フェーズ

| Phase | 名称 | 状態 | ゴール |
|-------|------|------|-------|
| Phase A | 安定化 | **完了** | コンパイル安定性、ブロッカー解消 |
| Phase B | 品質基盤 | **完了** | EditMode 75テスト、TODO 20→3 削減 |
| Phase C | 機能完成 | **完了** | Deform正式統合、CSG検証、PC-1~PC-5 完了 |
| Phase D | 最適化 | 未着手 | 60FPS安定、パフォーマンスチューニング |
| Phase E | 仕上げ | 未着手 | ドキュメント整合性、UI近代化 |

### Phase D の次期タスク候補

| タスクID | 内容 | 優先度 |
|---------|------|--------|
| TASK_PM-1 | PlayMode Smoke Test 導入 | Medium |
| TASK_PD-1 | パフォーマンスプロファイリング | High |
| TASK_PB-3 | Terrain 3D Composition プロトタイプ | Medium |

---

## 6. 検証済みスクリプト

```powershell
# コンパイルチェック
./scripts/check-compile.ps1

# EditModeテスト実行
./scripts/run-tests.ps1 -TestMode editmode
```

---

## 7. セッション履歴

### 2026-03-09: コード品質監査 + MapGenerator統合

| # | コミット | 内容 |
|---|---------|------|
| 1 | `e55ecba` | MG-1: MapGenerator を Generation に完全統合 (21→19 asmdef) |
| 2 | `d172be5` | spec-index.json ステータス更新 |
| 3 | `9841fe6` | Phase C HIGH指摘修正 (Material leak, バリデーション, tag, MeshFilter) |
| 4 | `b6fac95` | spec-index.json に新規仕様5件追加 |
| 5 | `3c2dade` | MEDIUM指摘修正 (DeformerType整理, RemoveDeform改善, Manager実装) |
| 6 | `7a75004` | PC-2 BlendSettings.cs デッドコード削除 + 仕様書修正 |

### 2026-03-08: Phase C全完了 + レガシー一掃

| # | コミット | 内容 |
|---|---------|------|
| 1 | `6391e0a` | PC-2 CSG Blend 4モード + PC-3 StructureGenerator 実装 |
| 2 | `6e2f414` | レガシー Shared Workflows era ファイル 33件削除、用語統一 |
| 3 | `80de8b9` | DEVELOPMENT_PROTOCOL.md + docs/README.md 全面刷新 |
| 4 | `b1ccbea` | UI Migration レポート更新 + 欠落 meta 追加 |
| 5 | `e68f1cc` | PC-5 GameManager → TerrainGenerator 接続 (asmdef参照追加) |
| 6 | `7d60110` | DOCS_INDEX.md 全検査 (9エントリ追加、Addendum削除) |
| 7 | `2376f1f` | HANDOVER.md Phase C完了更新 |

---

**参照**: [SSOT_WORLD.md](SSOT_WORLD.md) | [CLAUDE.md](../CLAUDE.md) | [DOCS_INDEX.md](DOCS_INDEX.md) | [ARCHITECTURE.md](ARCHITECTURE.md)

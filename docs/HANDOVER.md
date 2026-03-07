> **上位SSOT**: [SSOT_WORLD.md](SSOT_WORLD.md) | **索引**: [DOCS_INDEX.md](DOCS_INDEX.md)

# VastCore — Handover / ワークフロー仕様書

**最終更新**: 2026-03-07
**Actor**: Claude Code (Opus 4.6)
**Type**: ドキュメント負債一掃完了ハンドオフ

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
| **現フェーズ** | Phase A/B 完了 → Phase C 未着手 |

---

## 2. プロジェクト構造（2026-03-07 整理後）

```text
VastCore/
├── CLAUDE.md                  ← 運用SSOT（セッション運用プロトコル + Project Context）
├── AGENTS.md                  ← AIエージェント行動規約
├── AI_CONTEXT.md              ← AI向け軽量コンテキスト
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
│   ├── 04_reports/            ← レポート・検証記録（21ファイル、整理済み）
│   ├── tasks/                 ← タスクチケット
│   ├── terrain/               ← 地形システム固有
│   ├── inbox/                 ← 一時レポート受信箱
│   └── EXAMPLES/              ← Mermaid図テンプレート
│
├── Assets/
│   ├── Scripts/               ← C#ソースコード（283ファイル, 20 asmdef）
│   ├── Editor/                ← Editorツール
│   ├── MapGenerator/          ← MapGenerator固有コード
│   ├── Tests/                 ← EditMode/PlayModeテスト
│   └── _Scenes/              ← シーンファイル
│
├── Documentation/
│   └── Concept Arts/          ← コンセプトアート画像のみ（24枚）
│
├── .cursor/                   ← Cursor IDE設定
│   ├── rules.md               ← CLAUDE.md + SSOT_WORLD.md へのポインタ
│   └── MISSION_LOG.md         ← 履歴的ミッションログ
│
├── .cursorrules               ← CLAUDE.md + SSOT_WORLD.md へのポインタ
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
Generation / Terrain / WorldGen / MapGenerator
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
| Phase C | 機能完成 | **未着手** | Deform正式統合、CSG検証 |
| Phase D | 最適化 | 未着手 | 60FPS安定、パフォーマンスチューニング |
| Phase E | 仕上げ | 未着手 | ドキュメント整合性、UI近代化 |

### Phase C の次期タスク候補

| タスクID | 内容 | 優先度 |
|---------|------|--------|
| TASK_PM-1 | PlayMode Smoke Test 導入 | Medium |
| TASK_PC-1 | Deform Package 正式統合 | Critical |
| TASK_PB-3 | Terrain 3D Composition プロトタイプ | High |

---

## 6. 検証済みスクリプト

```powershell
# コンパイルチェック
./scripts/check-compile.ps1

# EditModeテスト実行
./scripts/run-tests.ps1 -TestMode editmode
```

---

## 7. 本セッションの実施内容（2026-03-07）

### ドキュメント負債一掃 — 8コミット, 219ファイル, -14,976行

| # | コミット | 内容 | 影響 |
|---|---------|------|------|
| 1 | `7644e00` | shared-workflows + windsurf_workflow 削除、SSOT修正 | 42 files |
| 2 | `d60eafa` | prompts/ 削除、cursor rules修正 | 16 files |
| 3 | `445a369` | Unity.Rendering.DebugUI asmdef修正 | 2 files |
| 4 | `bf75572` | docs/04_reports/ 整理 (116→20) | 97 files |
| 5 | `c0e1fc9` | Documentation/QA/ スタブ削除、TASK_014参照修正 | 14 files |
| 6 | `b68cec3` | openspec/ 削除、参照修正 | 31 files |
| 7 | `f000a82` | Documentation/ スタブ削除、AGENTS.md近代化 | 17 files |
| 8 | (本コミット) | HANDOVER.md / CLAUDE.md 仕様書更新 | — |

### 主要な構造変更

- **Windsurf → Claude Code 移行完了**: Orchestrator/Worker フレームワーク全廃止、CLAUDE.md + サブエージェントモデルに移行
- **OpenSpec 廃止**: `openspec/` 削除、CLAUDE.md SPEC FIRST に移行
- **Documentation/ 統合完了**: 転送スタブ全削除、残存はConcept Artsのみ
- **レポート整理**: docs/04_reports/ を116→21ファイルに削減
- **C#パス修正**: UIMigrationツールのハードコードパスを docs/04_reports/ に更新

---

**参照**: [SSOT_WORLD.md](SSOT_WORLD.md) | [CLAUDE.md](../CLAUDE.md) | [DOCS_INDEX.md](DOCS_INDEX.md) | [ARCHITECTURE.md](ARCHITECTURE.md)

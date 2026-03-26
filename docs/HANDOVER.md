> **上位SSOT**: [SSOT_WORLD.md](SSOT_WORLD.md) | **索引**: [DOCS_INDEX.md](DOCS_INDEX.md)

# VastCore — Handover / ワークフロー仕様書

**最終更新**: 2026-03-23
**Actor**: Claude Code (Opus 4.6)
**Type**: Phase D 進行中 — SP-019 Phase 1-5完了 + Pipeline仕様策定準備

---

## 1. 現在の状態

| 項目 | 値 |
|------|-----|
| **ブランチ** | `main` (trunk-based, local/origin統合済み 2026-03-17) |
| **Unity** | 6000.3.6f1 (URP) |
| **C#制約** | .NET Standard 2.1, C# 9.0 |
| **コンパイル** | 要確認 (session 4/5 で修正済みだが Unity 実機未検証) |
| **EditModeテスト** | 91件+ (session 4 で 3 件追加: StructureTagAdapter/Profile/ComponentSelector) |
| **PlayModeテスト** | 0件 (未着手、ゲート対象) |
| **現フェーズ** | Phase D 進行中 (T1 オーサリング + V4 段階的バリエーション) |

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
| Phase D | オーサリング + バリエーション | **進行中** | T1オーサリング主体 + V4段階的バリエーション。SP-018(V1)/SP-019(建物定義) 実装中 |
| Phase E | 仕上げ | 未着手 | ドキュメント整合性、UI近代化 |

### Phase D 進行状況

| 項目 | 状態 | pct |
|------|------|-----|
| PD-4: 巨大ファイル分割 | **完了** | 100 |
| SP-018: パラメトリック変異 (V1) | 実装済み、実機検証待ち | 85 |
| SP-017: StampExporter | 実装済み、実機検証待ち | 75 |
| SP-019: 建物定義 (タグ重み複合体) | Phase 1-5 実装済み (Phase 6 Inspector未着手) | 85 |
| PD-2: ランダム制御 (V1.5) | 未着手 | 0 |
| PD-1: 高度合成 (V3) | 未着手 | 0 |
| PD-3: パフォーマンス最適化 | 未着手 | 0 |

### 次ステップ

1. Unity実機検証: コンパイル確認 → QUICKSTART Step 1-3b (SP-017/018/019 目視)
2. SP-019 Phase 6 Inspector (BuildingDefinition/AdjacencyRuleSet/PlacementZone/MaterialPalette)
3. Pipeline仕様策定: end-to-endデザイナーワークフローの明文化
4. PD-2 ランダム制御 → V1.5 (RandomControlTab→StampDefinition転写)

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

### 2026-03-23: docs debt解消 + Pipeline仕様ドラフト (session 10 nightshift)

| # | コミット | 内容 |
|---|---------|------|
| (pending) | — | docs debt解消 (SSOT_WORLD/HANDOVER/WORKFLOW同期) |
| (pending) | — | Pipeline仕様ドラフト (SP-020) |

### 2026-03-22: SP-019 Phase 4-5 + コード品質監査 (session 8/9)

| # | コミット | 内容 |
|---|---------|------|
| 1 | `2904699` | docs: session 8 nightshift — コード品質監査 + 仕様ステータス修正 |
| 2 | `54712ab` | docs: HANDOVER.md SP-019進捗修正 |
| 3 | `e202de2` | feat(SP-019): Phase 4 配置ルール — AdjacencyRuleSet + PlacementZone + StructurePlacementSolver |
| 4 | `2d789f4` | docs: CLAUDE.md session 8 SP-019 Phase 4 反映 |
| 5 | (unstaged) | feat(SP-019): Phase 5 StructureMaterialPalette SO + StructureMaterialSelector + テスト15件 |

### 2026-03-18: Phase D 進行 — SP-018/019 実装 + 棚卸し (session 4/5)

| # | コミット | 内容 |
|---|---------|------|
| 1 | `ee18873` | Unity 6000.3.6f1 + package upgrades |
| 2 | `e5bc669` | feat(SP-018): V1 parametric variation (PositionJitter/MaterialVariants/ChildToggleGroups) |
| 3 | `7290a4a` | feat(SP-018): PrefabStampDefinition Custom Inspector + variation preview |
| 4 | `ff0cd6a` | docs: Phase D scope — T1+V4方針反映 + V1→V3拡張パス定義 |
| 5 | `a65655e` | feat: TerrainWithStampsBootstrap 複数StampDef対応 |
| 6 | `ce5aae0` | feat(SP-019): Building Definition Tag-Weight Composite Phase 1-3 |
| 7 | `903e190` | docs: session 5 handoff (REFRESH + コンパイル修正 + 棚卸し) |

### 2026-03-18: パイプライン貫通 + タスク整理 + push (夜間自走 session 2)

| # | コミット | 内容 |
|---|---------|------|
| 4 | `57ad88a` | feat(SP-017): StampExporter — StructureGenerator→DualGrid配置ブリッジ |
| 5 | `45b8e39` | docs: TASK_026(FROZEN), 027(DEFERRED), 029(OBSOLETE) クローズ + cascade 31ブランチ削除 |
| 6 | (pending) | docs: DOCS_INDEX 04_reports 21件登録 |

### 2026-03-17: local/origin統合 + spec-index整合 (夜間自走 session 1)

| # | コミット | 内容 |
|---|---------|------|
| 1 | `3b90526` | merge: origin/main統合 (SG-2, Erosion, Bootstrap, TerrainFacade) + local (PD-4, Phase C audit) — 12競合解決 |
| 2 | `79ef68d` | fix: SP-010重複解消 (Climate Visual → SP-015) |
| 3 | `c0115f8` | docs: spec-index sync — SP-009更新, SP-016 Erosion追加, DOCS_INDEX 10件追記 |

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

## 8. HANDOFF SNAPSHOT (2026-03-23)

| 項目 | 値 |
|------|-----|
| 主レーン | Authoring / Tooling |
| 現在スライス | Pipeline仕様策定 (SP-020 ドラフト完了) |
| 今回変更した対象 | SSOT_WORLD.md, HANDOVER.md, WORKFLOW_STATE_SSOT.md, CLAUDE.md, DOCS_INDEX.md, spec-index.json, DESIGNER_PIPELINE_SPEC.md (新規) |
| 次回最初に確認すべきファイル | docs/02_design/DESIGNER_PIPELINE_SPEC.md (SP-020 ドラフト、Q1-Q5 の設計判断待ち) |
| 未確定の設計論点 | GAP修復順序(Q-1)、智能配置統合方法(Q-2)、マテリアル適用タイミング(Q-3)、タグUI形式(Q-4)、自動化度合い(Q-5) |
| 今は触らない範囲 | Phase E (仕上げ)、SP-011/012/013/015 (todo仕様) |

### 未コミットの変更内容

**変更ファイル (8件):**
- CLAUDE.md: 直近の状態を session 10 nightshift に更新
- BUILDING_DEFINITION_SPEC.md: 軽微修正 (前セッションからの残り)
- LEGACY_UI_MIGRATION_REPORT.md: 軽微修正 (前セッションからの残り)
- DOCS_INDEX.md: SP-020追加、ファイル数更新、最終更新日
- HANDOVER.md: SP-019進捗更新、session履歴追加、次ステップ更新、HANDOFF SNAPSHOT追加
- SSOT_WORLD.md: Phase C/D 状態修正、最終更新日
- WORKFLOW_STATE_SSOT.md: Current Focus更新、SP-019 pct同期
- spec-index.json: SP-020エントリ追加

**未追跡ファイル (6件):**
- Assets/Scripts/Editor/AdjacencyRuleSetCreator.cs — SP-019 Phase 4: AdjacencyRuleSet初期データ生成
- Assets/Scripts/Editor/StructureMaterialPaletteCreator.cs — SP-019 Phase 5: MaterialPalette初期データ生成
- Assets/Scripts/Generation/StructureMaterialPalette.cs — SP-019 Phase 5: マテリアルパレットSO
- Assets/Scripts/Generation/StructureMaterialSelector.cs — SP-019 Phase 5: ブレンドスコアルーレット選択
- Assets/Tests/EditMode/StructureMaterialSelectorTests.cs — SP-019 Phase 5: テスト15件
- docs/02_design/DESIGNER_PIPELINE_SPEC.md — SP-020: デザイナーパイプライン仕様ドラフト

### パイプライン調査で発見した5件のGAP (SP-020 §4に詳述)

| GAP | 断絶箇所 | 要点 |
|-----|----------|------|
| GAP-1 | StructureGeneratorWindow → StampExporter | Export時にTagProfileが渡されない (UIなし) |
| GAP-2 | BuildingDefinition SO群の編集UI | SP-019 Phase 6 Inspector未着手 |
| GAP-3 | PlaceStampsAuto → StructurePlacementSolver | 智能配置エンジンが未接続 (ランダムのみ) |
| GAP-4 | PlacementZone/AdjacencyRuleSet | Bootstrap Inspector に公開されていない |
| GAP-5 | StructureMaterialSelector | 配置フロー内で呼ばれていない |

---

**参照**: [SSOT_WORLD.md](SSOT_WORLD.md) | [CLAUDE.md](../CLAUDE.md) | [DOCS_INDEX.md](DOCS_INDEX.md) | [ARCHITECTURE.md](ARCHITECTURE.md)

# VastCore — World SSOT (Single Source of Truth)

**ステータス**: 最上位仕様（憲法）
**最終更新**: 2026-11-21
**スコープ**: プロジェクト全体の目的・構造・優先順位の最終権威

> **本文書が VastCore プロジェクトの最上位 SSOT である。**
> 他のすべてのドキュメントは本文書に従属する。矛盾がある場合は本文書を優先すること。

---

## 1. プロジェクトの目的関数

**広大な景観に映える、ユニークで巨大な人工構造物をプロシージャルに生成する。**

生成される構造物は、ミニマルでありながら単体で芸術的価値を持ち、印象的な景観の一部となることを目指す。多様な幾何学形状の組み合わせによるユニークで複雑な形状の自動生成が中核である。

この目的関数（aesthetic output）と、工学的制約条件（engineering constraints: コンパイル安定性、テストカバレッジ、CI/CD）は対立するものではなく、前者が「何を作るか」、後者が「どう作るか」を定義する。

### 1.1 2026-11-21 の本人の方向（全体の作り直し）

本人が、開発が Arch の往復検証に局所化していたとして全体の作り直しを依頼した。プロシージャルなマップ生成とオブジェクト生成という軸は変わらない。

- **目標**: 地域一帯の詳細な鳥瞰図を自動で作成できること。
- **開発区切り**: その鳥瞰図の上に、ポイントクリック的な宝探しの探索ゲームを載せられること（座標を指定してサーチし、隠れた地点を発見する仕組み）。

原文・全体工程・実行方法は [SP-021](02_design/REGION_MAP_EXPLORATION_SPEC.md) が持つ。上の「巨大な人工構造物」は、地図の上で探し当てる地点として接続するという開発担当の解釈で進めており、構造物の形を地点へ結ぶ方式（SP-021 の段5）は本人が決める。DualGrid + HeightMap + Prefab Stamp（SP-017 / SP-020）のコードと記録は残し、段5の候補として保留している。

---

## 2. SSOT 階層

2026-11-21に現行の所有者へ揃えた。本書は目的・構造・優先順位を持ち、それ以外は下の各所有者が持つ。

```
SSOT_WORLD.md                          ← 最上位仕様（目的・構造。本文書）
  │
  ├── runtime-state.md                 ← **現在地・ボトルネック・次の作業**
  │     従属先: docs/runtime-state.md
  │
  ├── Agent operating rules            ← AI/Agent 入口と repo-local 運用ルール
  │     従属先: AGENTS.md → docs/REPO_LOCAL_RULES.md、docs/INVARIANTS.md、docs/ai/*.md
  │
  ├── project-context.md               ← 決定と引継ぎの履歴
  │     従属先: docs/project-context.md
  │
  ├── 機能仕様                          ← 各機能の仕様と検証手順（現役はSP-021。SP-017・SP-020は保留）
  │     従属先: docs/02_design/*.md、索引 docs/spec-index.json
  │
  └── DEVELOPMENT_ROADMAP_2026.md      ← Phase A-E の長期計画（2026-03-17時点）
        従属先: docs/01_planning/DEVELOPMENT_ROADMAP_2026.md
```

HANDOVER.md、WORKFLOW_STATE_SSOT.md、MILESTONE_PLAN.md、docs/tasks/ は2026-03時点の履歴資料。文書の役割の一覧は [NAV.md](NAV.md)。

**矛盾解決ルール**: 上位文書が下位文書に優先する。現在の状態はruntime-stateが、運用規則はAGENTS.mdが指す文書が持つ。

---

## 3. モジュール構成（責務レイヤー）

### 3.1 コアレイヤー

| レイヤー | 責務 | 主要アセンブリ |
|---------|------|--------------|
| **Utilities** | 共通ユーティリティ（依存なし） | `Vastcore.Utilities` |
| **Core** | インターフェース、エラーハンドリング、ログ | `Vastcore.Core` |

### 3.2 ドメインレイヤー

| レイヤー | 責務 | 主要アセンブリ |
|---------|------|--------------|
| **Generation** | プロシージャル生成、Deform統合、プリミティブ生成 | `Vastcore.Generation` |
| **Terrain** | 地形メッシュ、DualGrid、Prefabスタンプ配置、LOD、チャンク管理 | `Vastcore.Terrain` |
| **RegionMap** | 地域データ生成、鳥瞰図の描画、探索地点、探索の判定と画面（他のレイヤーに依存しない） | `Vastcore.RegionMap` |

### 3.3 プレゼンテーションレイヤー

| レイヤー | 責務 | 主要アセンブリ |
|---------|------|--------------|
| **Player** | 移動、入力、ワープ | `Vastcore.Player` |
| **Camera** | 追従、回転、シネマティック | `Vastcore.Camera` |
| **UI** | HUD、パラメータ制御、パフォーマンスモニタ | `Vastcore.UI` |
| **Game** | ライフサイクル管理、シーン遷移 | `Vastcore.Game` |

### 3.4 ツールレイヤー

| レイヤー | 責務 | 主要アセンブリ |
|---------|------|--------------|
| **Editor** | StructureGenerator（7タブ）、Inspector拡張 | `Vastcore.Editor` |
| **Testing** | EditMode/PlayMode テスト、統合テスト | `Vastcore.Testing` |

### 3.5 将来の分割ポイント

以下のドメインは、情報が十分に蓄積された段階で独立ドキュメントに分割する:

- **GENERATION_PIPELINE.md** — Recipe / QualityGate / DerivedFields
- **MAP_CAPTURE.md** — 観測装置（衛星/タイル/レイヤー）
- **STRUCTURES_GUIDE.md** — StructureGenerator の問い合わせ/変形レイヤー規約

---

## 4. 開発フェーズ（サマリー）

詳細は [DEVELOPMENT_ROADMAP_2026.md](01_planning/DEVELOPMENT_ROADMAP_2026.md) を参照。

| Phase | 名称 | 状態 | ゴール |
|-------|------|------|-------|
| **Phase 1-2, 4** | 基本関係性 / 形状制御 / パーティクル配置 | 完了 | 基本生成パイプライン |
| **Phase A** | 安定化 (Stabilization) | 完了 | コンパイル安定性 95、ブロッカー解消 |
| **Phase B** | 品質基盤 (Quality Foundation) | 完了 | EditMode 100+テスト、TODO 20→3 削減 |
| **Phase C** | 機能完成 (Feature Completion) | **完了** | Deform統合、Prefabスタンプ(SG-1+SG-2)、CSG検証 |
| **Phase D** | オーサリング + バリエーション | 保留（2026-11-21） | T1オーサリング主体 + V4段階的バリエーション。SP-021 の段5の判断まで保留 |
| **Region Map** | 地域鳥瞰図と探索の土台（SP-021） | **進行中**（2026-11-21〜） | seed から鳥瞰図を作り、探索ゲームを載せられる |
| **Phase E** | 仕上げ (Polish) | 未着手 | ドキュメント整合性 75、UI 近代化 |

---

## 5. ドキュメント体系

| ファイル | 役割 | リンク |
|---------|------|--------|
| **SSOT_WORLD.md** | 最上位仕様（本文書） | — |
| **ARCHITECTURE.md** | モジュール/依存/責務の鳥瞰 | [docs/ARCHITECTURE.md](ARCHITECTURE.md) |
| **DOCS_INDEX.md** | 全ドキュメント索引 | [docs/DOCS_INDEX.md](DOCS_INDEX.md) |
| **DEVELOPMENT_ROADMAP_2026.md** | ロードマップ正本 | [docs/01_planning/DEVELOPMENT_ROADMAP_2026.md](01_planning/DEVELOPMENT_ROADMAP_2026.md) |
| **ROADMAP.md** | ロードマップ導線 | [docs/01_planning/ROADMAP.md](01_planning/ROADMAP.md) |
| **AGENTS.md / REPO_LOCAL_RULES.md** | AI/Agent 入口と運用ルール | [AGENTS.md](../AGENTS.md) / [docs/REPO_LOCAL_RULES.md](REPO_LOCAL_RULES.md) |
| **CLAUDE.md** | プロジェクト文脈（運用ルールではない） | [CLAUDE.md](../CLAUDE.md) |
| **HANDOVER.md** | 成果物SSOT | [docs/HANDOVER.md](HANDOVER.md) |
| **DEV_PLAN_ARCHIVE_2025-01.md** | 目的関数のアーカイブ | [docs/01_planning/DEV_PLAN_ARCHIVE_2025-01.md](01_planning/DEV_PLAN_ARCHIVE_2025-01.md) |

---

## 6. AI Agent 向けガイドライン

1. **実装判断に迷ったら**: 本文書 → ROADMAP_2026 → 該当タスクチケットの順に参照する。
2. **目的関数 vs 制約条件**: 美的出力（何を作るか）は DEV_PLAN_ARCHIVE に思想がある。工学的制約（どう作るか）は ROADMAP_2026 に従う。
3. **Agent 再開時**: `AGENTS.md` → `docs/REPO_LOCAL_RULES.md` → `docs/runtime-state.md` を先に読む。詳細探索は `docs/NAV.md` を使う。
4. **SSOT 矛盾時**: 上位文書を優先。矛盾を発見したら owning doc を特定し、現在状態は `runtime-state.md`、永続判断は `project-context.md` が存在する場合はそこへ記録する。
5. **新規ドキュメント作成時**: 必ず DOCS_INDEX.md に追記し、SSOT 階層内での位置を明示する。

---

**SSOT宣言**: 本文書が VastCore プロジェクトの最上位仕様 (World SSOT) である。

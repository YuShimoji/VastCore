# VastCore — World SSOT (Single Source of Truth)

**ステータス**: 最上位仕様（憲法）
**最終更新**: 2026-02-14
**スコープ**: プロジェクト全体の目的・構造・優先順位の最終権威

> **本文書が VastCore プロジェクトの最上位 SSOT である。**
> 他のすべてのドキュメントは本文書に従属する。矛盾がある場合は本文書を優先すること。

---

## 1. プロジェクトの目的関数

**広大な景観に映える、ユニークで巨大な人工構造物をプロシージャルに生成する。**

生成される構造物は、ミニマルでありながら単体で芸術的価値を持ち、印象的な景観の一部となることを目指す。多様な幾何学形状の組み合わせによるユニークで複雑な形状の自動生成が中核である。

この目的関数（aesthetic output）と、工学的制約条件（engineering constraints: コンパイル安定性、テストカバレッジ、CI/CD）は対立するものではなく、前者が「何を作るか」、後者が「どう作るか」を定義する。

---

## 2. SSOT 階層

```
SSOT_WORLD.md                          ← 最上位仕様（本文書）
  │
  ├── DEVELOPMENT_ROADMAP_2026.md      ← ロードマップ SSOT（Phase A-E の計画と優先度）
  │     従属先: docs/01_planning/DEVELOPMENT_ROADMAP_2026.md
  │
  ├── EVERY_SESSION.md                 ← 運用 SSOT（セッション運用プロトコル）
  │     従属先: docs/windsurf_workflow/EVERY_SESSION.md
  │
  ├── HANDOVER.md                      ← 成果物 SSOT（フェーズ完了と引き継ぎ）
  │     従属先: docs/HANDOVER.md
  │
  └── タスクチケット                    ← 個別実装チケット
        従属先: docs/tasks/TASK_*.md
```

**矛盾解決ルール**: 上位文書が下位文書に優先する。

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
| **Terrain** | 地形メッシュ、LOD、チャンク管理、ストリーミング | `Vastcore.Terrain` |

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
| **Phase A** | 安定化 (Stabilization) | 進行中 | コンパイル安定性 95、ブロッカー解消 |
| **Phase B** | 品質基盤 (Quality Foundation) | 未着手 | テストカバレッジ 70、CI/CD 稼働 |
| **Phase C** | 機能完成 (Feature Completion) | 未着手 | Deform完了、CSG検証、Phase 5 着手 |
| **Phase D** | 最適化・拡張 (Optimization) | 未着手 | Phase 5-6 実装、60FPS 安定 |
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
| **EVERY_SESSION.md** | 運用SSOT | [docs/windsurf_workflow/EVERY_SESSION.md](windsurf_workflow/EVERY_SESSION.md) |
| **HANDOVER.md** | 成果物SSOT | [docs/HANDOVER.md](HANDOVER.md) |
| **DEV_PLAN_ARCHIVE_2025-01.md** | 目的関数のアーカイブ | [docs/01_planning/DEV_PLAN_ARCHIVE_2025-01.md](01_planning/DEV_PLAN_ARCHIVE_2025-01.md) |

---

## 6. AI Agent 向けガイドライン

1. **実装判断に迷ったら**: 本文書 → ROADMAP_2026 → 該当タスクチケットの順に参照する。
2. **目的関数 vs 制約条件**: 美的出力（何を作るか）は DEV_PLAN_ARCHIVE に思想がある。工学的制約（どう作るか）は ROADMAP_2026 に従う。
3. **SSOT 矛盾時**: 上位文書を優先。矛盾を発見したら HANDOVER.md に記録し、次セッションで解決する。
4. **新規ドキュメント作成時**: 必ず DOCS_INDEX.md に追記し、SSOT 階層内での位置を明示する。

---

**SSOT宣言**: 本文書が VastCore プロジェクトの最上位仕様 (World SSOT) である。

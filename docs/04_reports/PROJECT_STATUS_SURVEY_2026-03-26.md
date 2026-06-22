# VastCore Project Status Survey

**調査日**: 2026-03-26 (session 9, post-cleanup)
**調査者**: Claude Code (Opus 4.6)
**対象**: VastCore Terrain Engine 全体

---

## 1. 定量メトリクス

| 項目 | 値 | 備考 |
|------|-----|------|
| ソースファイル (.cs) | **349** | Scripts/316 + Editor/33 (Legacy 5件削除済み) |
| テストファイル (NUnit) | **44** | Assets/Tests/ (EditMode 41 + PlayMode 3) |
| ランタイムテスト系 | **45** | Assets/Scripts/Testing/ (要精査) |
| asmdef | **18** | Source 14 + Test 2 + Editor 3 - Legacy 1 |
| ドキュメント (.md) | **72+** | docs/ 配下 (15件削除済み) |
| 仕様エントリ (spec-index.json) | **34** | done 14 / partial 11 / todo 7 / removed 4 |
| TODO/FIXME/HACK | **2** | 1件内部 (Billboard), 1件TMP第三者 |
| レガシーコード行数 | **0** | 全削除済み (session 9) |
| モックファイル | **0** | 良好 |

---

## 2. 懸念点一覧

| # | 重要度 | 懸念 | 影響範囲 | 対応方針 |
|---|--------|------|---------|---------|
| C-1 | CRITICAL | Unity 実機検証の未実施 | SP-001/003/007/009/010/017/018/019 | ユーザーが Unity Editor でコンパイル+テスト+Gizmo目視 |
| C-2 | HIGH | Testing/ 45件のランタイムテスト堆積 | コンパイル単位・テスト二重管理 | NUnit移動+MonoBehaviour手動テスト精査+無効化ファイル削除 |
| C-3 | MEDIUM | WORKFLOW_STATE_SSOT Done条件と SP-010 pct 不整合 | Done条件「SG-1+SG-2完了」 vs SP-010 partial/90 | Unity実機検証後に SP-010 を 100 に更新、または Done条件に「実機検証」を追記 |
| C-4 | LOW | リモートブランチのゴミ (3件) | origin/master, develop, feature/TASK_036 | GitHub上で削除 (HUMAN_AUTHORITY) |
| C-5 | LOW | Vastcore.DeformStubs autoReferenced=true | Deform有無の環境差 | 現時点で実害なし。SSOT「変更禁止」指定 |

---

## 3. 機能ステータス表

### 3a. 実装済み機能 (done / pct 100) — 14件

| ID | 機能 | カテゴリ | 確認手段 | 検証状況 |
|----|------|---------|---------|---------|
| SP-002 | Marching Squares Terrain System | core | EditMode テスト | 検証済み |
| SP-006 | Terrain Generation v0 | core | EditMode テスト | 検証済み |
| SP-008 | WorldGen Architecture (M0-M3) | core | EditMode テスト | 検証済み |
| SP-016 | Erosion System | core | EditMode テスト | 検証済み |
| DS-001 | Assembly Architecture (SSOT) | infra | asmdef 整合確認 | session 8 検証済み |
| DS-003 | Phase 3 Deform Technical Investigation | core | 調査完了 | N/A (調査文書) |
| DS-004 | Legacy Isolation Design | infra | コード確認 | 検証済み (対象削除で役目完了) |
| DS-006 | Terrain Algorithm Notes | core | 文書完了 | N/A (文書) |
| DS-008 | Deform Usage Documentation | core | 文書完了 | N/A (文書) |
| DS-009 | Building & Structure Inventory | system | 棚卸し完了 | N/A (文書) |
| AR-001 | Architecture Overview | infra | 文書完了 | N/A (文書) |
| AR-002 | SSOT World | system | 文書完了 | N/A (文書) |
| AR-003 | Workflow State SSOT | system | 文書完了 | N/A (文書) |
| PD-005 | Large File Decomposition (PD-4) | infra | コード確認 | 検証済み |

### 3b. 未確認機能 — 確認手段別

#### Unity 実機必要 (コンパイル + Gizmo/Inspector 目視)

| ID | 機能 | pct | 未完了部分 | 確認手段 | 検証状況 |
|----|------|-----|-----------|---------|---------|
| SP-001 | Dual Grid Terrain System | 85 | Hex検索最適化 | Unity実機 Gizmo目視 | **未検証** |
| SP-003 | DualGrid HeightMap Profile Mapping | 85 | 微調整 | Unity実機 Inspector | **未検証** |
| SP-009 | DualGrid Terrain Integration | 75 | EditorWindow統合 | Unity実機 EditorWindow | **未検証** |
| SP-010 | Prefab Stamp Placement | 90 | Unity実機検証のみ | Unity実機 Gizmo + テスト | **未検証** |
| SP-017 | Stamp Export Pipeline | 75 | Unity実機検証のみ | Unity実機 + Inspector | **未検証** |
| SP-018 | Parametric Variation (V1) | 85 | Unity実機検証のみ | Unity実機 Inspector + 目視 | **未検証** |

#### EditMode テスト + Unity 実機

| ID | 機能 | pct | 未完了部分 | 確認手段 | 検証状況 |
|----|------|-----|-----------|---------|---------|
| SP-019 | Building Definition (Tag-Weight) | 75 | Phase 5(スタイル) / Phase 6(Inspector) | EditMode テスト + Unity実機 | Phase 1-4 テスト通過、5-6 **未実装** |

#### PlayMode テスト

| ID | 機能 | pct | 未完了部分 | 確認手段 | 検証状況 |
|----|------|-----|-----------|---------|---------|
| SP-007 | Terrain Engine Design | 70 | PlayModeテスト | PlayMode テスト実行 | **未検証** |

#### 管理文書・設計

| ID | 機能 | pct | 未完了部分 | 確認手段 | 検証状況 |
|----|------|-----|-----------|---------|---------|
| DS-002 | RandomControl Modern UI Design | 40 | UI実装全般 | Unity実機 EditorWindow | **未検証** |
| DS-010 | Post Phase C Quick Wins | 5 | 14タスク中13件未着手 | 個別タスク依存 | **未着手** |
| PD-001 | Phase D Scope Definition | 75 | PD-2/3 未着手 | 管理文書 | N/A |

### 3c. 未実装機能 (todo) — 7件

| ID | 機能 | 優先度 | 依存 | 備考 |
|----|------|-------|------|------|
| SP-011 | Ecosystem Generation | 将来 | SP-001, SP-016 | バイオーム/気候駆動植生配置 |
| SP-012 | Destructible Architecture | 将来 | SP-013 | 密度減算ベース構造物破壊 |
| SP-013 | Composite Structure Assembly Rules | 将来 | SP-019 | GrammarEngine最小実装 |
| SP-015 | Climate Visual Integration | 将来 | SP-016 | Shader Globals/季節色調/風/天候 |
| PD-002 | Advanced Composition System (PD-1) | Phase D後半 | SP-018 | 複数メッシュ統合+LOD自動生成 |
| PD-003 | Controlled Random System (PD-2) | Phase D後半 | SP-018 | シード駆動再現可能ランダム |
| PD-004 | Performance Optimization (PD-3) | Phase D後半 | 全コア機能 | Job System/Burst統合 |

### 3d. 削除済み仕様 (removed) — 4件

| ID | 旧タイトル | 削除理由 | 削除日 |
|----|----------|---------|--------|
| SP-004 | Advanced Procedural Structure Generation | SP-019 に置き換え | 2026-03-26 |
| SP-005 | Phase 2 Template Integration | 設計方針転換で陳腐化 | 2026-03-26 |
| DS-005 | Phase 1.5 Runtime Refactor | Phase A-C で代替完了 | 2026-03-26 |
| DS-007 | Refactoring Handover | Phase A-C で代替完了 | 2026-03-26 |

---

## 4. Session 9 レガシー根絶実績

### 4a. 削除したコード

| 対象 | ファイル数 | 行数 | 理由 |
|------|----------|------|------|
| Assets/_Scripts/*.cs | 5 | 574 | 参照ゼロ、Vastcore.Legacy (autoReferenced=false) で隔離済み |
| Assets/_Scripts/Vastcore.Legacy.asmdef | 1 | - | 上記に付随 |
| Assets/_Scripts/ .meta ファイル | 8 | - | Unity メタデータ |

### 4b. 削除したドキュメント

| # | ファイル | カテゴリ | 理由 |
|---|---------|---------|------|
| 1 | docs/01_planning/DEV_PLAN.md | planning | 転送スタブ (Archive へ) |
| 2 | docs/01_planning/PROJECT_RESTRUCTURE_PLAN.md | planning | Phase A 完了済み |
| 3 | docs/01_planning/REFACTORING_ACTION_PLAN.md | planning | Phase A/B 達成済み |
| 4 | docs/01_planning/REFACTORING_PLAN.md | planning | Phase A 完了済み |
| 5 | docs/01_planning/RESTRUCTURE_PLAN.md | planning | Phase A 完了済み (重複) |
| 6 | docs/01_planning/SPRINT_PLAN_02.md | planning | Sprint 02 完了済み |
| 7 | docs/01_planning/WEB_DEVELOPMENT_ROADMAP.md | planning | Cursor Web 廃止 |
| 8 | docs/01_planning/PHASE_A_DEPENDENCY_MAP.md | planning | Phase A 完了済み |
| 9 | docs/02_design/ADVANCED_STRUCTURE_DESIGN_DOCUMENT.md | design | SP-019 に置き換え (SP-004) |
| 10 | docs/02_design/PREFAB_STAMP_PLACEMENT_SPEC.md | design | SP010_ が正式版 (旧版) |
| 11 | docs/02_design/REFACTORING_HANDOVER_DOCUMENT.md | design | Phase A-C で代替 (DS-007) |
| 12 | docs/02_design/Phase2_TemplateIntegration_Spec.md | design | 方針転換 (SP-005) |
| 13 | docs/02_design/Phase15_RuntimeRefactor_Design.md | design | Phase A-C で代替 (DS-005) |
| 14 | docs/02_design/BUILDING_SPEC_HANDOFF.md | design | Handoff Packet 用途終了 |
| 15 | docs/02_design/PHASE_C_SCOPE_DEFINITION.md | design | Phase C 完了済み |

### 4c. 更新したドキュメント

| ファイル | 変更内容 |
|---------|---------|
| docs/spec-index.json | SP-004/005, DS-005/007 を legacy → removed、file を null に |
| docs/DOCS_INDEX.md | 削除ファイル 13 エントリ除去、件数修正、日付更新 |
| docs/WORKFLOW_STATE_SSOT.md | session 9 実績追記、次ステップ更新 |
| docs/02_design/ASSEMBLY_ARCHITECTURE.md | Vastcore.Legacy サブグラフ・テーブル行削除 |
| docs/02_design/DualGridTerrainSystem_Spec.md | 旧版リンク → SP-010 正式版リンクに修正 |
| docs/README.md | PHASE_C_SCOPE_DEFINITION 参照削除、asmdef数修正 |
| docs/01_planning/ROADMAP.md | PHASE_A_DEPENDENCY_MAP 参照削除 |
| CLAUDE.md | session 9 状態更新、asmdef 19→18 |
| docs/runtime-state.md | 全面更新 (post-cleanup メトリクス) |

---

## 5. テスト健全性

| 項目 | 値 | 評価 |
|------|-----|------|
| NUnit テストファイル (Assets/Tests/) | 44 | 適正 |
| ランタイムテスト (Assets/Scripts/Testing/) | 45 | **要精査** |
| モックファイル | 0 | 良好 |
| テスト/実装比率 (NUnit) | 44/349 = 0.13 | 適正 |
| TODO/FIXME | 2 (1件サードパーティ) | 良好 |

### Testing/ 45件の問題分類

| 分類 | 件数 | 説明 | 推奨処置 |
|------|------|------|---------|
| A. コンパイル無効化済み | 2 | #if ガードで永続無効 | 削除 |
| B. Tests/PlayMode と機能重複 | 1 | PlayModeSmokeTest (別namespace同目的) | 統合または削除 |
| C. Tests/EditMode と配置分散 | 4 | NUnit テストが Testing/EditMode/ にも存在 | Assets/Tests/EditMode/ に移動 |
| D. MonoBehaviour 手動テスト群 | ~32 | NUnit 外、シーン依存の手動テスト | 精査後、不要分削除 |
| E. 独自テスト基盤 | 6 | TestManager, ITestCase 等 | D の削除に伴い不要化 |

---

## 6. asmdef 構成 (18件)

| # | asmdef | レイヤー | 状態 |
|---|--------|---------|------|
| 1 | Vastcore.Utilities | Utilities | OK |
| 2 | Vastcore.Core | Core | OK |
| 3 | Vastcore.Generation | Generation | OK |
| 4 | Vastcore.Terrain | Terrain | OK |
| 5 | Vastcore.WorldGen | WorldGen | OK |
| 6 | Vastcore.DeformStubs | Deform | OK (autoReferenced=true, 変更禁止) |
| 7 | Vastcore.Camera | Camera | OK |
| 8 | Vastcore.Player | Player | OK |
| 9 | Vastcore.UI | UI | OK |
| 10 | Vastcore.Game | Game | OK |
| 11 | Vastcore.Editor | Editor (Scripts/Editor) | OK |
| 12 | Vastcore.Editor.Root | Editor (Assets/Editor) | OK |
| 13 | Vastcore.Editor.StructureGenerator | Editor (StructureGenerator) | OK |
| 14 | Vastcore.Editor.Tools | Editor (Tools) | OK |
| 15 | Vastcore.Testing | Testing (MonoBehaviour系) | 要精査 |
| 16 | Vastcore.Testing.Runtime | Testing (RuntimeTests) | 要精査 |
| 17 | Vastcore.Tests.EditMode | Tests (NUnit EditMode) | OK |
| 18 | Vastcore.Tests.PlayMode | Tests (NUnit PlayMode) | OK |

---

**参照**: [WORKFLOW_STATE_SSOT.md](../WORKFLOW_STATE_SSOT.md) | [spec-index.json](../spec-index.json) | [CLAUDE.md](../../CLAUDE.md) | [runtime-state.md](../runtime-state.md)

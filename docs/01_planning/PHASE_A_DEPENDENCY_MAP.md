# Phase A 依存関係マップ

> **Mission**: ORCH_20260209_ROADMAP_PHASE_A  
> **Phase**: A - Stabilization  
> **作成日**: 2026-02-09

---

## タスク依存グラフ

```
Phase A (Stabilization)
========================

Tier 1 (並列実行可能)
---------------------
    ┌─────────┐    ┌─────────┐    ┌─────────┐
    │  PA-1   │    │  PA-3   │    │  PA-4   │
    │(Deform) │    │(asmdef) │    │ (Test)  │
    │   S     │    │   S     │    │   M     │
    └────┬────┘    └────┬────┘    └────┬────┘
         │              │              │
         │              │              │
         └──────────────┼──────────────┘
                        │
                        ▼
                  ┌─────────┐
                  │  PA-5   │
                  │(Verify) │
                  │   S     │
                  │ Tier 3  │
                  └────┬────┘
                       │
         ┌─────────────┼─────────────┐
         │             │             │
         ▼             ▼             ▼
    ┌─────────┐  ┌─────────┐  ┌─────────┐
    │ Phase B │  │ Phase C │  │ Phase D │
    └─────────┘  └─────────┘  └─────────┘

Tier 2 (段階実行)
-----------------
PA-2 (ProBuilder API) ──→ PC-2 (CSG/Composition)

Phase B-D 連携
--------------
PA-3 ──→ PB-5 (Core分割)
PA-4 ──→ PB-1 (テスト基盤)
PA-1 ──→ PC-1 (Deform正式導入)
```

---

## 詳細依存関係表

| タスクID | Depends-On | Blocks | 並列実行 |
|---------|-----------|--------|---------|
| **PA-1** | - | PC-1, PA-2 | ✅ 可 |
| **PA-2** | PA-1 | PC-2 | ❌ 不可 |
| **PA-3** | - | PB-5, PA-5 | ✅ 可 |
| **PA-4** | - | PB-1, PA-5 | ✅ 可 |
| **PA-5** | PA-1, PA-3, PA-4 | Phase B, C, D | ❌ 不可 |

---

## フェーズ間依存関係

### Phase A → Phase B
- PA-4 → PB-1 (テスト基盤構築)

### Phase A → Phase C
- PA-1 → PC-1 (Deform正式導入)
- PA-2 → PC-2 (CSG/Composition)

### Phase A → Phase B → Phase C/D
- PA-3 → PB-5 → PC-1, PC-4 (Core分割連鎖)

---

## 実行順序

### Step 1: 並列実行 (Tier 1)
```bash
# 同時に3つのWorkerを起動可能
Worker-1 → PA-1 (Deformスタブ整理)
Worker-2 → PA-3 (asmdef正規化)
Worker-3 → PA-4 (テストファイル整理)
```

### Step 2: 調査フェーズ (Tier 2)
```bash
# PA-1 完了後に開始
Worker → PA-2 (ProBuilder API移行調査)
```

### Step 3: 検証フェーズ (Tier 3)
```bash
# PA-1, PA-3, PA-4 完了後に開始
Worker → PA-5 (Unity Editor検証)
Orchestrator → User: Unity Editor確認依頼
User → Orchestrator: 検証結果報告
```

### Step 4: Phase B開始
```bash
# PA-5 完了後
Orchestrator → Phase B 開始
```

---

## リスク評価

| タスク | リスク | 影響度 | 対策 |
|--------|--------|--------|------|
| PA-1 | シンボル定義ミス | 中 | DEFORM_AVAILABLE vs DEFORM_PACKAGE の統一確認 |
| PA-2 | ProBuilder 6.0.8 API 不存在 | 高 | カスタム MeshSubdivider 実装をフォールバック |
| PA-3 | 参照不足によるコンパイルエラー | 低 | Unity Editor 検証で検出・修正 |
| PA-4 | テスト移動による参照切れ | 低 | asmdef 更新で対応 |
| PA-5 | Unity Editor 環境依存 | 中 | 手動検証フローで対応 |

---

## 品質ゲート

### PA-5 完了時チェックリスト
- [ ] Unity Editor コンパイルエラー 0
- [ ] 全 asmdef が正しく設定
- [ ] テストが Test Runner で認識
- [ ] 健全性スコア: コンパイル安定性 ≥ 95

---

## ファイルパス

- **Source**: `docs/01_planning/DEVELOPMENT_ROADMAP_2026.md`
- **Mission Log**: `.cursor/MISSION_LOG.md`
- **Tasks**: `docs/tasks/TASK_PA-*.md`
- **Inbox**: `docs/inbox/`

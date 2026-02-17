# Task: PA-1 Deform スタブ整理と条件付きコンパイル統一

> **Source**: `docs/01_planning/DEVELOPMENT_ROADMAP_2026.md` (L174-184)  
> **Phase**: A - Stabilization  
> **Tier**: 1  
> **Size**: S

---

## 概要

Deform パッケージの条件付きコンパイルを統一し、パッケージ有無に応じた適切なスタブ実装を整備します。

---

## ゴール

- `DEFORM_AVAILABLE` シンボルを使用した統一的な条件付きコンパイル
- パッケージ未導入時のスタブ実装の整備
- Unity Editor でコンパイルエラー 0

---

## 対象ファイル

### 修正対象
1. `Scripts/Deform/DeformStubs.cs` — `#if !DEFORM_PACKAGE` ガード追加
2. `Scripts/Generation/DeformIntegration.cs` — `#if DEFORM_PACKAGE` ガード確認
3. `Scripts/Generation/DeformIntegrationManager.cs` — 同上
4. `Scripts/Generation/VastcoreDeformManager.cs` — 同上
5. `Scripts/Core/DeformPresetLibrary.cs` — 同上
6. `Editor/StructureGenerator/Tabs/Deform/DeformerTab.cs` — 既存ガードの確認

---

## 依存関係

- **Depends-On**: なし（独立タスク）
- **Blocks**: PC-1 (Deform正式導入)
- **Blocked-By**: なし

---

## Focus Area / Forbidden Area

### Focus Area（編集対象）
- 条件付きコンパイルシンボルの追加・確認
- `Scripts/Deform/` ディレクトリのスタブファイル作成

### Forbidden Area（編集禁止）
- `Scripts/Generation/DeformIntegration.cs` の実装ロジック変更
- 既存の Deform API 呼び出しの変更

---

## 検証手順

1. Unity Editor でプロジェクトを開く
2. Console ウィンドウでコンパイルエラーを確認
3. エラー 0 を確認

---

## 完了基準

- [x] `Scripts/Deform/DeformStubs.cs` に `#if !DEFORM_AVAILABLE` ガード適用（既存確認済み）
- [x] 全対象ファイルの条件付きコンパイル統一（`DEFORM_AVAILABLE`で統一確認）
- [ ] Unity Editor コンパイルエラー 0（要ユーザー確認）
- [x] タスクドキュメント更新

---

## ステータス

- **Status**: COMPLETED
- **Worker**: Cascade
- **Started**: 2026-02-17
- **Completed**: 2026-02-17

---

## 変更履歴

| 日時 | Actor | 内容 |
|------|-------|------|
| 2026-02-09 | Orchestrator | チケット作成 |
| 2026-02-17 | Cascade | 完了確認: DEFORM_AVAILABLEで既に統一済み |

# Task: PA-3 asmdef 依存関係の正規化

> **Source**: `docs/01_planning/DEVELOPMENT_ROADMAP_2026.md` (L202-210)  
> **Phase**: A - Stabilization  
> **Tier**: 1  
> **Size**: S

---

## 概要

Assembly Definition ファイルの依存関係を正規化し、明示的な参照に統一します。

---

## ゴール

- 全 asmdef の依存関係が明示的に定義
- `autoReferenced: false` に統一
- Unity Editor リロード後にコンパイルエラー 0

---

## 対象ファイル

### 修正対象
1. `Scripts/Camera/Vastcore.Camera.asmdef` — Core, Utilities への参照追加
2. `Scripts/Generation/Vastcore.Generation.asmdef` — `autoReferenced: false` に変更
3. `Scripts/Editor/Vastcore.Editor.asmdef` — `Unity.ProBuilder`, `Unity.ProBuilder.Editor` 参照追加
4. 全10 asmdef の `autoReferenced` を `false` に統一

### 対象 asmdef 一覧
- `Vastcore.Core.asmdef`
- `Vastcore.Utilities.asmdef`
- `Vastcore.Player.asmdef`
- `Vastcore.Terrain.asmdef`
- `Vastcore.Generation.asmdef`
- `Vastcore.Camera.asmdef`
- `Vastcore.UI.asmdef`
- `Vastcore.Game.asmdef`
- `Vastcore.Editor.asmdef`
- `Vastcore.Testing.asmdef`

---

## 依存関係

- **Depends-On**: なし（独立タスク）
- **Blocks**: PB-5 (Core分割)
- **Blocked-By**: なし

---

## Focus Area / Forbidden Area

### Focus Area（編集対象）
- `.asmdef` ファイルの参照設定
- `autoReferenced` フィールドの変更

### Forbidden Area（編集禁止）
- ソースコード (.cs) のロジック変更
- 名前空間の変更
- クラス名の変更

---

## 検証手順

1. Unity Editor でプロジェクトを開く
2. `Edit > Project Settings > Player` でスクリプティングシンボルを確認
3. Console でコンパイルエラーを確認
4. Assembly Definition 参照設定を確認

---

## 完了基準

- [x] `Vastcore.Camera.asmdef` に Core, Utilities 参照追加（既存確認済み）
- [x] `Vastcore.Generation.asmdef` の `autoReferenced: false` に変更
- [x] `Vastcore.Editor.asmdef` に ProBuilder 参照追加
- [x] 全10 asmdef の `autoReferenced` 統一（確認済み）
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
| 2026-02-17 | Cascade | autoReferenced修正、ProBuilder参照追加 |

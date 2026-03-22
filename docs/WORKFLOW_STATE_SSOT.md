# WORKFLOW STATE SSOT

Last Updated: 2026-03-22

## Mission

広大な景観に映える、ユニークで巨大な人工構造物をプロシージャルに生成する基盤の確立。
Phase A/B/C 完了。SG-1/SG-2 + PD-4 完了。Phase D 実装進行中。

## Current Focus

**Phase D: オーサリング主体 + 段階的バリエーション** — T1+V4方針決定済み。
SP-018 (pct 85) + SP-017 (pct 75) + SP-019 Phase 1-3 (pct 65) 実装済み。
コード品質・asmdef整合・meta欠落・仕様整合は全てクリーン (session 8 検証)。
次: Unity実機検証 (QUICKSTART Step 1-3b) → SP-017/018 pct更新 → SP-019 Phase 4-6設計。

## Done 条件

- [x] `TASK_037_TerrainVerticalSlice_CloseoutSummary` の完了
- [x] SSOT駆動ワークフローへの完全移行と legacy docs の整理
- [x] Unity Editor でのコンパイル安定性 95% 以上 (PA-5)
- [x] SG-1: DualGrid Prefab配置の単一セル実装完了
- [x] SG-2: マルチセルフットプリント実装完了
- [ ] SG-1 + SG-2: Unity実機検証 (コンパイル確認 + Gizmo目視)
- [x] Phase D スコープ承認 (T1オーサリング + V4段階的バリエーション) — 2026-03-18
- [x] V1: パラメトリック変異の最初のスライス着手 — SP-018 pct 85
- [ ] SP-018 + SP-017: Unity実機検証 (コンパイル + EditModeテスト + 目視)

## 選別規則

当面は以下の作業分類に従い、D（将来のための品質や汎化）は凍結とします。

- A. コア機能・目的の達成
- B. 制作/開発速度の向上・互換設定
- C. 失敗からの復旧しやすさ
- D. テスト拡充、過度なレポート、当面に直結しないリファクタリング → **凍結**

## 禁止事項

- `Scripts/Deform/DeformStubs.cs` のロジック変更（ガード追加のみ可）
- ProBuilder API への直接依存の無差別な追加（`MeshSubdivider.cs` などの抽象化を優先）
- `ProjectSettings/`, `Packages/` の相談なしの変更
- 非同期処理における `CancellationToken` の省略
- 本番ロジックを広範囲に破壊するテスト用改変

# WORKFLOW STATE SSOT

Last Updated: 2026-03-18

## Mission

広大な景観に映える、ユニークで巨大な人工構造物をプロシージャルに生成する基盤の確立。
Phase A/B/C 完了。SG-1/SG-2 + PD-4 完了。Phase D スコープ策定中。

## Current Focus

**Phase D: オーサリング主体 + 段階的バリエーション** — 最終体験像(T1)とバリエーション手段(V4)を決定 (2026-03-18)。
StructureGeneratorはEditorツールとして深化。バリエーションはまずV1(パラメトリック変異)をPrefabStampDefinitionに追加、検証後にWFC/CSGへ拡張。
SP-019 建物定義仕様策定完了 (タグ重み複合体方式)。Phase 1 コアデータ構造の実装中。
次: SP-019 Phase 1 実装 → Unity実機検証 (SG-1/SG-2 + SP-017 + SP-018 + SP-019)。

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

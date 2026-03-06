# WORKFLOW STATE SSOT

## Mission

広大な景観に映える、ユニークで巨大な人工構造物をプロシージャルに生成する基盤の確立。
Phase A/B 完了。現在のフォーカス：Phase C 機能完成（Deform正式統合 + CSG検証）およびドキュメント負債の解消。

## Done 条件

- [x] `TASK_037_TerrainVerticalSlice_CloseoutSummary` の完了
- [ ] SSOT駆動ワークフローへの完全移行と legacy docs の整理
- [x] Unity Editor でのコンパイル安定性 95% 以上 (PA-5)

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

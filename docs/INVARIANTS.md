# Invariants

VastCore の非交渉条件、責任境界、禁止ショートカットを保持する正本。

## Product Invariants

- 目的関数は「広大な景観に映える、ユニークで巨大な人工構造物を
  プロシージャルに生成する」こと。
- 現行の主軸は DualGrid + HeightMap + designer Prefab Stamp placement。
- 3D Voxel / Marching Cubes を主経路へ戻すには、ユーザーの明示的な再承認が必要。
- Marching Squares を主経路へ戻すには、Prefab Stamp 方針との衝突を先に解決する。

## Engineering Invariants

- Unity 6000.3.x / URP / C# 9.0 制約を前提にする。
- asmdef 依存は下位から上位へ逆流させない。
- 同名型または同一責務を複数アセンブリに増やさない。
- Unity `.meta` は資産本体と同じ意味で扱い、移動・削除時に整合させる。
- `ProjectSettings/` と `Packages/` は高影響面。明確な必要性なしに触らない。
- コンパイル成功や Unity Editor 上の挙動は、文書上の推測ではなく検証結果として扱う。

## Responsibility Boundaries

- user: 作品性、最終体験、Unity Editor 上の実機感、凍結 frontier の再開判断。
- supervisor AI: outcome、bottleneck、creative direction、stop condition の定義と成果レビュー。
- development AI: repo 調査、実装、機械検証、差分説明、正本同期、成果単位内の判断。
- tool: テスト、静的チェック、スクリプト実行、ブラウザ/Unity 補助が利用可能な場合の観測。
- shared: Editor 手動確認が必要な acceptance と、その証拠の記録。

## Prohibited Shortcuts

- `rejected` / `frozen` / `hold` を「未着手だから次に進める」と解釈しない。
- テストや docs 整備を、ユーザー可視の terrain / structure artifact 進捗として水増ししない。
- 旧セッションの handover や historical report を、現在の acceptance 証拠として扱わない。
- ユーザー未指定の外部プロジェクトから規約や成果物を移植しない。比較・参照する場合も、
  明示された範囲だけに限る。

## Operating Rule

ユーザーが一度説明した非交渉条件は、同じブロック内でこの正本へ固定する。
理由や経緯は `docs/project-context.md`、現在状態は `docs/runtime-state.md` に置く。

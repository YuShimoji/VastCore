# Task: PA-2 ProBuilder API 移行調査と Subdivide 代替実装
Status: DONE
Tier: 2
Branch: feature/PA-2-probuilder-api-migration
Owner: Worker
Created: 2026-02-19T13:30:00+09:00
Report: docs/04_reports/REPORT_TASK_PA-2_ProBuilderApiMigration.md
Milestone: SG-1 / MG-1

## Objective
- ProBuilder API 変更で残っている Subdivide 系 TODO を解消し、高品質プリミティブ生成パスを復旧する。
- API 非互換がある場合は最小フォールバック実装でコンパイル/動作を維持する。

## Context
- 参照: `docs/01_planning/DEVELOPMENT_ROADMAP_2026.md:188`
- 依存元 PA-1 は `COMPLETED`。
- 本タスクは Phase A 完了と PA-5 実行の前提。

## Focus Area
- `Assets/Scripts/Terrain/Map/HighQualityPrimitiveGenerator.cs`
- `Assets/Scripts/Generation/Map/PrimitiveTerrainGenerator.cs`
- `Assets/Scripts/Generation/Map/PrimitiveModifier.cs`
- `Assets/Scripts/Utilities/Utils/`（必要時のみ `MeshSubdivider.cs` を追加）

## Forbidden Area
- `.shared-workflows/` 配下（submodule）
- `ProjectSettings/`, `Packages/`（必要が出たら必ず相談）
- 大規模リファクタ（ファイル一括移動/削除）
- Terrain 以外の機能追加

## Target Assemblies
- `Vastcore.Terrain`
- `Vastcore.Generation`
- `Vastcore.Utilities`（フォールバック実装が必要な場合のみ）
- 参照: `docs/02_design/ASSEMBLY_ARCHITECTURE.md`

## Constraints
- 変更対象アセンブリは Target Assemblies に限定する。
- `using` 追加前に asmdef `references` を確認する。
- 同名型追加時は `rg "class TypeName" Assets/ --glob "*.cs"` で重複確認する。
- C# 9.0 制約を順守する（引数なし struct コンストラクタ等は禁止）。
- `#if` 外にオプショナルシンボル参照を漏らさない。
- エラー発生時は `docs/03_guides/COMPILATION_GUARD_PROTOCOL.md` の診断フローに従う。

## Test Plan
- **テスト対象**: Subdivide / RebuildFromMesh / UV 展開 / smoothing 変更の呼び出し経路
- **EditMode テスト**: 既存 `TerrainTests` から関連テスト実行、なければ最小の回帰テストを追加
- **PlayMode テスト**: Primitive 生成シーンで代表形状の生成確認（Sphere/Cube/Cylinder など）
- **ビルド検証**: Unity Editor コンパイル成功（`Unity Editor=コンパイル成功`）
- **期待結果**: TODO 箇所で実行パスが有効化され、例外/未実装エラーなく動作
- **テスト不要の場合**: なし（本タスクは挙動修復のためテスト必須）

## Impact Radar
- **コード**: Primitive 生成器の API 呼び出し点
- **テスト**: Terrain/Generation 系テストの期待値
- **パフォーマンス**: Subdivide 処理コスト増加の可能性
- **UX**: 高品質プリミティブの見た目品質
- **連携**: ProBuilder パッケージ依存/フォールバック分岐
- **アセット**: 生成メッシュ形状への影響
- **プラットフォーム**: Editor/CI バッチ実行時の差異

## DoD
- [x] HighQualityPrimitiveGenerator の TODO 箇所が解消されている
- [x] PrimitiveTerrainGenerator / PrimitiveModifier の Subdivide 経路が有効
- [x] 必要時のみ `MeshSubdivider` フォールバックが追加されている（今回は追加不要）
- [x] **コンパイルエラーがない**（Unity Editorで確認: `Unity Editor=コンパイル成功`）
- [x] **アセンブリ整合性チェック**:
  - [x] 追加 using に対応する asmdef 参照が存在
  - [x] 同名型重複なし
  - [x] asmdef 変更時は `ASSEMBLY_ARCHITECTURE.md` 更新済み
- [x] EditMode/PlayMode の検証結果を記録
- [x] `docs/04_reports/REPORT_TASK_PA-2_ProBuilderApiMigration.md` を作成し、Report 欄に追記

## Notes
- PA-2 完了後に PA-5（全体コンパイル検証）へ進む。
- Status は OPEN / IN_PROGRESS / BLOCKED / DONE を使用する。

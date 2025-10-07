# LEGACY UI Migration Report (Dry Run)

最終更新: 2025-10-06

## 概要

- 対象: アセット/スクリプト内の `NarrativeGen.UI` 参照
- 目的: `Vastcore.UI` への安全な置換と、シーン/Prefab 参照の保全

## スキャン結果（サマリ）

- 件数: TBD
- 影響シーン/Prefab: TBD
- 未移行/保留: TBD

## 詳細ログ

- スキャナ実装後に、該当箇所一覧（パス/行/抜粋）を追記します。

## 次のアクション

- `UIMigrationScanner.cs` の Dry Run 実装
- `MenuManager` の扱い（A3-2）に関する設計判断の反映

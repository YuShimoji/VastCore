# TASK_023: Merge Conflict Resolution (origin/main into develop)

## 概要
`origin/main` から `develop` へのマージで発生した60+件のコンフリクトを解決する。

## Status: DONE
## Priority: 1 (Critical)
## Tier: 2

## コンフリクトカテゴリ

### 1. Submodule (1件)
- `.shared-workflows` - submodule の更新が必要

### 2. Assembly Definition (5件)
- `Assets/Scripts/Game/Vastcore.Game.asmdef`
- `Assets/Scripts/Testing/Vastcore.Testing.asmdef`
- `Assets/Scripts/UI/Vastcore.UI.asmdef`
- `Assets/Tests/EditMode/Vastcore.Tests.EditMode.asmdef`
- `Assets/Tests/PlayMode/Vastcore.Tests.PlayMode.asmdef`

### 3. Terrain Scripts (20+件)
- `Assets/Scripts/Terrain/Map/*.cs` - 主要なコンフリクト
- `Assets/Scripts/Terrain/Cache/*.cs`
- `Assets/Scripts/Terrain/TerrainChunk.cs`

### 4. Generation Scripts (6件)
- `Assets/Scripts/Generation/*.cs`
- `Assets/Scripts/Generation/Map/*.cs`

### 5. Testing Scripts (7件)
- `Assets/Scripts/Testing/*.cs`
- `Assets/Scripts/Testing/TestCases/*.cs`

### 6. Core & UI (4件)
- `Assets/Scripts/Core/*.cs`
- `Assets/Scripts/UI/*.cs`

### 7. Documentation (8件)
- `docs/*.md`
- `DEV_LOG.md`
- `Documentation/QA/*.md`

### 8. Config (3件)
- `.cursor/MISSION_LOG.md`
- `.cursorrules`
- `Packages/packages-lock.json`

### 9. Modify/Delete Conflicts (7件)
- ファイルが一方で削除、他方で修正されたケース

## DoD (Definition of Done)
- [x] Submodule `.shared-workflows` を最新に更新
- [x] 全てのコンフリクトマーカー (`<<<<<<<`, `=======`, `>>>>>>>`) を解消
- [x] `.asmdef` ファイルの参照が正しく設定されている
- [x] `git status` がクリーン（マージコミット完了）
- [x] Unity Editor でプロジェクトが正常にロードされる
- [x] コンパイルエラーがない

## 停止条件
- 依存関係の再設計が必要な場合
- 削除されたファイルの復元が必要で、どちらを採用すべきか不明な場合

## 作業順序（推奨）
1. Submodule 更新
2. Config ファイル解決
3. Assembly Definition 解決
4. Core → Generation → Terrain → Testing → UI の順でスクリプト解決
5. Documentation 解決
6. マージコミット

## 関連タスク
- TASK_022: Fix Cyclic Dependencies (マージ後に実行)

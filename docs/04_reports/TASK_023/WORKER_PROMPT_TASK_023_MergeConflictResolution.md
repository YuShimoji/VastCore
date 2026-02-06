# Worker Prompt: TASK_023_MergeConflictResolution

## 参照
- チケット: docs/tasks/TASK_023_MergeConflictResolution.md
- SSOT: docs/Windsurf_AI_Collab_Rules_latest.md
- HANDOVER: docs/HANDOVER.md

## 境界
- **Focus Area**:
  - `git diff --name-only --diff-filter=U` で表示される全コンフリクトファイル
  - `.shared-workflows` submodule
  - `*.asmdef` files
  - `Assets/Scripts/` 配下のスクリプト
  - `docs/` 配下のドキュメント
- **Forbidden Area**:
  - 機能改変を伴うリファクタリング
  - 新規ファイルの追加（コンフリクト解決に必要な場合を除く）

## DoD (Definition of Done)
- [ ] Submodule `.shared-workflows` を最新に更新 (`git submodule update --remote`)
- [ ] 全てのコンフリクトマーカー解消
- [ ] マージコミット完了
- [ ] Unity Editor でのロード確認（可能なら）
- [ ] コンパイルエラーがない

## 停止条件
- 削除ファイルの復元判断が必要な場合（ユーザー確認が必要）
- 依存関係の再設計が必要な場合

## 納品先
- docs/inbox/REPORT_TASK_023_MergeConflictResolution.md

## 中間報告ルール
- **ツール呼び出し10回ごと**、または**ファイル編集5回ごと**に中間報告を出力する。

## 作業順序
1. **Submodule**: `.shared-workflows` を更新し、`git add .shared-workflows`
2. **Config**: `.cursor/MISSION_LOG.md`, `.cursorrules`, `Packages/packages-lock.json`
3. **Assembly Definition**: `*.asmdef` ファイル (5件)
4. **Core Scripts**: `Assets/Scripts/Core/*.cs`
5. **Generation Scripts**: `Assets/Scripts/Generation/**/*.cs`
6. **Terrain Scripts**: `Assets/Scripts/Terrain/**/*.cs` (最大のコンフリクト)
7. **Testing Scripts**: `Assets/Scripts/Testing/**/*.cs`
8. **UI Scripts**: `Assets/Scripts/UI/*.cs`
9. **Documentation**: `docs/*.md`, `DEV_LOG.md`, `Documentation/QA/*.md`
10. **Final**: `git commit -m "merge: origin/main into develop - resolve all conflicts"`

## コンフリクト解決の原則
- **両方の変更が必要な場合**: 両方を統合
- **一方のみ必要な場合**: `develop` (HEAD) を優先（ただし、origin/main の新機能は保持）
- **Modify/Delete**: 削除が意図的か確認し、不明な場合は停止

## ヒント
- `git checkout --ours <file>` で develop 側を採用
- `git checkout --theirs <file>` で origin/main 側を採用
- コンフリクトマーカーを手動で編集する場合は、`<<<<<<<`, `=======`, `>>>>>>>` を全て削除

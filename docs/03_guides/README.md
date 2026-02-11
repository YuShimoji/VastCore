# 03_guides

セットアップガイド・運用ガイド等を配置します。

## 収容対象（移行マッピング）
  
- GIT_SETUP_GUIDE.md → docs/03_guides/GIT_SETUP_GUIDE.md
- UI_MIGRATION_NOTES.md → docs/03_guides/UI_MIGRATION_NOTES.md（参照修正済み）

## テスト実行手順（EditMode / PlayMode）

- ローカル（PowerShell）

  ```powershell
  # プロジェクトルートで実行
  ./scripts/run-tests.ps1 -TestMode editmode
  ./scripts/run-tests.ps1 -TestMode playmode
  ```

  - Unity Editor のパスは `ProjectSettings/ProjectVersion.txt` から自動解決します。
  - 結果は `artifacts/test-results/*.xml`、ログは `artifacts/logs/*.log` に出力されます。

- CI（GitHub Actions）

  - `/.github/workflows/unity-tests.yml` が `pull_request` / `workflow_dispatch` で起動。
  - リポジトリに `UNITY_LICENSE` シークレットが設定されている場合のみ実行します。
  - EditMode/PlayMode をマトリクスで並列実行します。

## New Docs (2026-02)
- `docs/03_guides/REDEVELOPMENT_LOCAL_SETUP.md`
- `docs/03_guides/TERRAIN_VERTICAL_SLICE_RUNBOOK.md`

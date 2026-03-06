# VastCore Terrain Engine

GPU 加速テレインエンジン。Unity 6 / C#。v1.0.0 安定版。6バイオーム / LOD / マネージャーアーキテクチャ。

## Project Context

- 環境: Unity 6 (URP) / C# (.NET Standard 2.1, C# 9.0制約)
- ブランチ戦略: main (trunk-based)
- 現フェーズ: Phase A/B 完了 → Phase C（Deform正式統合 + CSG検証）
- 直近の状態: ドキュメント負債一掃完了（shared-workflows除去、windsurf_workflow削除、SSOT整合性修正）。EditModeテスト100+追加済み、TODO残3件。

## Key Paths

- Source: `Assets/Scripts/`
- SSOT: `docs/SSOT_WORLD.md` → `docs/WORKFLOW_STATE_SSOT.md`
- Architecture: `docs/02_design/ASSEMBLY_ARCHITECTURE.md`
- Tasks: `docs/tasks/TASK_*.md`

## Rules

- Respond in Japanese
- No emoji
- Use Serena's symbolic tools (find_symbol, get_symbols_overview) instead of reading entire .cs files
- When exploring code, start with get_symbols_overview, then read only the specific symbols needed
- Keep responses concise — avoid repeating file contents back to the user

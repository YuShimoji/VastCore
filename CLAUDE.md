# VastCore Terrain Engine

GPU 加速テレインエンジン。Unity 6 / C#。v1.0.0 安定版。6バイオーム / LOD / マネージャーアーキテクチャ。

## Project Context

- 環境: Unity 6000.3.3f1 (URP) / C# (.NET Standard 2.1, C# 9.0制約)
- ブランチ戦略: main (trunk-based)
- 現フェーズ: Phase A/B 完了 → Phase C（Deform正式統合 + CSG検証）
- 直近の状態: 2026-03-07 ドキュメント負債一掃完了。8コミットで219ファイル変更、約15,000行削除。shared-workflows/windsurf_workflow/prompts/openspec/ 全て削除済み。Documentation/ 転送スタブ全削除。AGENTS.md をClaude Code向けに近代化。EditModeテスト75件PASS。

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

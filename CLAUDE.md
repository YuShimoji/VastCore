# VastCore Terrain Engine

GPU 加速テレインエンジン。Unity 6 / C#。v1.0.0 安定版。6バイオーム / LOD / マネージャーアーキテクチャ。

## Project Context

- 環境: Unity 6000.3.3f1 (URP) / C# (.NET Standard 2.1, C# 9.0制約)
- ブランチ戦略: main (trunk-based)
- 現フェーズ: Phase C 完了 (PC-1,2,3,4,5 ALL DONE)
- 直近の状態: 2026-03-09 Phase C品質監査完了 + MG-1 MapGenerator統合完了。HIGH指摘7件・MEDIUM指摘4件修正済み、BlendSettings.csデッドコード削除。19 asmdef体制確立。次: Phase D設計 or 既存仕様の棚卸し。18コミット未push。

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

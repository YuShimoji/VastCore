# VastCore Terrain Engine

GPU 加速テレインエンジン。Unity 6 / C#。v1.0.0 安定版。6バイオーム / LOD / マネージャーアーキテクチャ。

## Project Context

- 環境: Unity 6000.3.3f1 (URP) / C# (.NET Standard 2.1, C# 9.0制約)
- ブランチ戦略: main (trunk-based)
- 現フェーズ: Phase C ほぼ完了（PC-1,2,3,4 DONE / PC-5のみ残）
- 直近の状態: 2026-03-08 PC-2(CSG Blend 4モード実装) + PC-3(Arch/Pyramid生成、GlobalSettings保存/読込) 完了。Phase C 残りは PC-5(GameManager接続、TerrainGenerator未存在でブロック)のみ。

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

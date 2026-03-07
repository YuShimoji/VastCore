# Task Index

> 最終更新: 2026-03-07

## Summary
- Total: 36 tasks
- Done: 31 | In Progress: 0 | Pending: 0 | Legacy: 5

## Phase A (Terrain Core Stabilization)
| ID | Title | Status | Summary |
|---|---|---|---|
| PA-1 | Deform スタブ整理と条件付きコンパイル統一 | COMPLETED | DEFORM_AVAILABLEシンボルを使用した統一的な条件付きコンパイル |
| PA-2 | ProBuilder API 移行調査と Subdivide 代替実装 | DONE | ProBuilder API 変更で残っている Subdivide 系 TODO を解消 |
| PA-3 | asmdef 依存関係の正規化 | COMPLETED | Assembly Definition ファイルの依存関係を正規化し、autoReferenced統一 |
| PA-4 | テストファイルの所属整理 | COMPLETED | 散在しているテストファイルを Scripts/Testing/ アセンブリに集約 |
| PA-5 | Unity Editor コンパイル完全検証 | DONE | Phase A の成果に対して Unity Editor 上で全アセンブリのコンパイル成功を確認 |

## Phase B (Test Baseline)
| ID | Title | Status | Summary |
|---|---|---|---|
| PB-1 | NUnit テスト基盤の構築 | DONE | EditMode 中心の NUnit テスト基盤を整備し、Phase B の品質基盤を開始 |
| PB-2 | CsgProviderResolver テスト安定化 | DONE | EditMode の既知失敗 CsgProviderResolverSmokeTests を安定化 |

## Phase C (Deform + CSG)
| ID | Title | Status | Summary |
|---|---|---|---|
| PC-1 | Deform パッケージ正式導入と統合検証 | DONE | Deform をスタブ運用から正式パッケージ運用へ移行し統合動作を検証 |

## Terrain Vertical Slice
| ID | Title | Status | Summary |
|---|---|---|---|
| 031 | Terrain Vertical Slice Kickoff | DONE | M0-M1 execution of the terrain vertical slice with concrete deliverables |
| 032 | DualGrid HeightMap Profile Mapping Design | DONE | profile-driven coordinate mapping for VerticalExtrusionGenerator設計 |
| 033 | DualGrid HeightMap Profile Mapping Implementation | DONE | Profile-driven coordinate mapping for DualGrid height sampling実装 |
| 034 | Unity Validation for DualGrid Profile Mapping | DONE | TASK_033 implementation in Unity Editorの検証 |
| 035 | Auto Compile Validation Automation | DONE | Unity compile validation via headless/batchmode scriptの自動化 |
| 036 | DualGrid Inspector Profile Preview | DONE | profile-driven DualGrid sampling preview from Inspectorの有効化 |
| 037 | Terrain Vertical Slice Closeout Summary | DONE | TASK_031-036のcloseout summaryを作成 |

## Dual Grid System
| ID | Title | Status | Summary |
|---|---|---|---|
| 013 | Dual Grid Terrain System - Phase 1 | DONE | 六角形グリッドベースの不規則グリッドシステムの基盤実装 |

## Marching Squares System
| ID | Title | Status | Summary |
|---|---|---|---|
| 014 | Marching Squares Terrain System - Phase 1 | DONE | デュアルグリッド+Marching Squaresアルゴリズムの基盤実装 |
| 015 | Marching Squares Terrain System - Phase 2 | DONE | スプライン入力対応（Unity Spline Package統合） |
| 016 | Marching Squares Terrain System - Phase 3 | DONE | レイヤー構造対応（Height, BiomeId, RoadId, BuildingId） |

## Legacy / Unnumbered (TASK_010 through TASK_030, TASK_026)
| ID | Title | Status | Summary |
|---|---|---|---|
| 010 | TerrainGenerationWindow(v0) 機能改善 | DONE | Profile/Generatorとの整合性改善、HeightMap UIの反映漏れ解消 |
| 011 | HeightMapGenerator 改善 | DONE | 決定論/チャンネル/UV/反転対応 |
| 012 | TerrainGenerationWindow プリセット管理機能 | DONE | よく使う地形設定をプリセットとして保存・適用 |
| 014b | unity-mcp パッケージ Git リポジトリエラー修正 | DONE | com.justinpbarnett.unity-mcp パッケージのGitリポジトリエラー解消 |
| 018 | origin/master からのマージコンフリクト解決 | DONE | 約60ファイルのマージコンフリクトを解決 |
| 019 | SW Doctor Rules Configuration Fix | DONE | sw-doctor の SSOT ファイル不一致エラー解消 |
| 020 | Namespace Consistency (Utils vs Utilities) | DONE | Vastcore.Utils と Vastcore.Utilities の名前空間を統一 |
| 021 | Merge Integration & Verification | BLOCKED | マージ後の統合検証（テスト実行インフラ問題でBLOCKED） |
| 022 | Release Cyclic Dependencies | DONE | Assembly-CSharp と Vastcore.* 間の循環依存を解消 |
| 023 | Merge Conflict Resolution (origin/main into develop) | DONE | origin/main から develop への60+件のマージコンフリクト解決 |
| 026 | 3D Voxel Terrain Hybrid System - Phase 1 | OPEN | Marching Cubesアルゴリズムを用いたハイブリッド・ボクセル地形生成システムの基盤実装 |
| 027 | MCP Unity Verification | OPEN | Model Context Protocol (MCP) パッケージの動作検証 |
| 028 | Fix PrimitiveTerrain Compilation | DONE | PrimitiveTerrainObject の IPoolable 実装エラー修正 |
| 028b | MCPForUnity 重複アセンブリ定義の解消 | DONE | Assets/ と Packages/ の MCPForUnity 重複を解消 |
| 029 | Unity Editorコンパイルエラー修正の検証 | BLOCKED | develop ブランチのコンパイルエラー修正3件をUnity Editorで検証 |
| 030 | Worktree整理とPush統合 | DONE | 複数ブランチでの未Pushコミットを整理しworktree状態をクリーンに |

## Notes
- Legacy tasks (010-030) are historical records and may contain duplicate IDs (e.g., two TASK_014, two TASK_028)
- Tasks marked as BLOCKED have documented blockers in their respective task files
- Phase C (Deform + CSG): PC-1 完了 (2026-03-07)。次は CSG 検証またはランタイム変形実装

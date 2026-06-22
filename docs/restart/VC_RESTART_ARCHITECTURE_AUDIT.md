# VastCore Restart Architecture Audit

[ROUTE: VastCore | AGENT->SUPERVISOR | VC-RST-0-restart-architecture-audit | T+0 | target:YuShimoji/VastCore@main | artifact:docs/restart/VC_RESTART_ARCHITECTURE_AUDIT.md | reply:ChatGPT逶｣菫ｮ繧ｹ繝ｬ繝・ラ | confidence:medium]

作成日: 2026-06-22  
範囲: リポジトリ根拠の再始動監査と次スライス判断。runtime/editor 実装、scene、package、asset import は変更しない。

この監査は、VastCore を「完成済みゲーム」ではなく「Terrain Generation Engine と、それを検証する Simulator Harness」として再始動するための判断資料である。結論を先に置くと、現時点では DualGrid を即実装拡張するより、Terrain Engine / Simulator Harness / EditorTools / Tests の境界を明文化し、compile と asset integrity を戻してから、地形アルゴリズム PoC を選ぶのが最短で安全である。

## 1. Current State Capsule

### Git / remote state

| 観測項目 | 結果 | 判断 |
|---|---|---|
| Local branch | `main` | 添付 Prompt は `master` 指定だが、実リポジトリは `main` が追跡ブランチ |
| Local HEAD | `a9e7142` | `origin/main` より 11 commit behind |
| Remote after fetch | `origin/main = 3893388` | `git fetch --all --prune` 済み |
| Pull / merge | 未実施 | working tree に既存変更が多く、上流変更と重なるため安全に fast-forward できる状態ではない |
| Worktree | dirty | 既存の docs / Packages / ProjectSettings / Assets 変更と削除、未追跡 `spec-wiki.html` などあり |

`origin/main` は取得済みだが、ローカル working tree は変更済みファイルが多い。以降の監査は「現在の working tree」と「fetch 済み `origin/main`」を分けて扱う。今回追加したのは新規 doc / OpenSpec scaffolding のみで、既存 runtime/editor 実装、scene、package、ProjectSettings は触っていない。

### Unity / package state

| 項目 | local working tree | `origin/main` | 判断 |
|---|---:|---:|---|
| Unity Editor | `6000.4.9f1` | `6000.3.6f1` | local に未統合またはユーザー側更新あり。compile claim は不可 |
| URP | `17.4.0` | `17.3.0` | local と origin で差分あり |
| ProBuilder | `6.0.9` | `6.0.8` | CSG / primitive generation のリスク評価に影響 |
| Input System | `1.19.0` | `1.18.0` | Simulator 側依存 |
| Splines | `2.8.4` | `2.8.2` | road/path 連携候補だが core ではない |
| Deform | git `https://github.com/keenanwoodall/Deform.git` | 同左 | optional integration として存在 |

最新 `origin/main` の `docs/runtime-state.md` は、次の product bottleneck を compile / asset-integrity restoration としている。`docs/architecture/current-dependency-map.md` は 2026-06-15 時点の compile failure を記録しており、`StructureTagAdapter.cs` の CS0234 と 5 件の invalid `.meta` GUID を current evidence として扱う。今回 Unity Editor compile は実行していないため、「コンパイル済み」とは言わない。

### Assembly / module surface

| 領域 | 観測 | 判断 |
|---|---|---|
| Terrain | `Assets/Scripts/Terrain` に 96 C# files、`Vastcore.Terrain` | engine core と extension が混在 |
| WorldGen | `Assets/Scripts/WorldGen` に 51 C# files、`Vastcore.WorldGen` | density field / graph / recipe / stamps の engine extension |
| Generation | `Assets/Scripts/Generation` に 57 C# files、`Vastcore.Generation` | legacy/current heightmap + ProBuilder/Deform primitive path が混在 |
| Player | `Assets/Scripts/Player` に 8 C# files、`Vastcore.Player` | simulator/game harness。Terrain / Generation に依存 |
| EditorTools | `Assets/Editor/StructureGenerator` に 23 C# files | authoring / CSG / inspector / generator tools |
| Tests | EditMode 41 files、PlayMode 3 files | broad coverage はあるが compile 未確認 |

`Vastcore.Player` は `Vastcore.Terrain` と `Vastcore.Generation` に依存している。Terrain / Generation 側は `IPlayerController` や Player tag 検索で player position を参照する箇所があるが、Player 具象 assembly への直接参照は避けられている。今後は target / position provider interface を Terrain 側 contract とし、Simulator がそれを供給する形に寄せるのが安全である。

### Terrain-related systems

| 系統 | 実体 | 現在の意味 |
|---|---|---|
| Classic heightmap / Unity Terrain | `TerrainGenerator`, `HeightMapGenerator`, `TerrainChunk`, `TerrainStreamingController`, `IHeightmapProvider` | 最小 Terrain Engine spine 候補 |
| DualGrid + Prefab Stamp | `Assets/Scripts/Terrain/DualGrid/*`, `DualGridHeightSamplingSettings`, `GridDebugVisualizer` | 現在の主軸だが、surface extraction ではなく grid / height / stamp placement 系 |
| WorldGen density field | `WorldGen.FieldEngine`, `WorldGen.Recipe`, `WorldGen.Stamps`, `Terrain.Volumetric` | 3D density / volumetric path。main path に戻すには user reapproval が必要 |
| MarchingCubesMeshExtractor | 実装名は MarchingCubes、内容は marching tetrahedra 分割 | caves / overhangs / mining PoC 候補。main foundation では未承認 |
| MarchingSquares | `Assets/Scripts/Terrain/MarchingSquares` | hold / experiment。Prefab Stamp 方針との衝突解決が先 |
| Erosion / GPU / Cache / Optimization | Terrain extension 群 | core の後に検証すべき品質・性能拡張 |

### Movement / Trail / simulator systems

| 系統 | 実体 | 現在の意味 |
|---|---|---|
| Player controllers | `AdvancedPlayerController`, `PlayerController`, `SimplePlayerController` | Simulator Harness / traversal validation |
| Movement extensions | climbing, grinding, translocation | terrain traversal test harness として価値あり |
| Trail | `TranslocationSphere` の `TrailRenderer` | game/player visual。Terrain Engine の core ではない |
| Game manager / camera / UI | `Vastcore.Game`, `Vastcore.Camera`, `Vastcore.UI` | engine consumer。Terrain core から参照禁止 |

### Boolean / CSG state

| 系統 | 実体 | 現在の意味 |
|---|---|---|
| Editor CSG provider | `CsgProviderResolver`, `ProBuilderInternalCsgProvider`, `ParaboxCsgProvider` | Editor-only optional provider abstraction |
| Reflection PoC | `Assets/Editor/Tools/ProBuilderCsg/ProBuilderInternalCsgPoC.cs` | ProBuilder internal CSG 調査/診断 |
| CSG smoke test | `CsgProviderResolverSmokeTests` | null input / resolver existence smoke。実 mesh acceptance ではない |
| Manual Boolean test | `Assets/Tests/EditMode/BooleanTest.cs` under `#if HAS_PROBUILDER && HAS_PARABOX_CSG` | Parabox がなければ無効 |
| SDF boolean | `WorldGen.Recipe.BooleanOp`, `SdfMath` | density field boolean。ProBuilder CSG とは別物 |

### Asset inventory relevant to terrain / roads / deformation / mining

| Asset / package | Present? | Classification | Decision use |
|---|---:|---|---|
| Deform package | yes, package manifest and `Assets/Deform` | optional integration | visual/high-quality mesh deformation。mining foundation ではない |
| EasyRoads3D / Easy Road 3D | not found in `Assets`, `Packages`, `docs` search | absent | 採用判断は postponed。import しない |
| ProBuilder | yes | integration / editor authoring | primitive generation and editor CSG。core dependency にしない |
| Splines | yes | possible route/path input | road/path extension 候補。core には入れない |
| Internal road graph | yes, `WorldGen.GraphEngine.RoadGraphGenerator` | engine extension | external road asset の代替/adapter 接点 |
| Voxel/SDF code | yes, WorldGen + Volumetric + MeshExtraction | experiment / hold | mining PoC 候補。main path には reapproval と tests が必要 |
| Unity Terrain modules | yes | minimum output target | heightfield / holes / TerrainData mutation の検証対象 |

### Source classification

| Source | Classification | Reason |
|---|---|---|
| `docs/REPO_LOCAL_RULES.md`, `docs/runtime-state.md` from `origin/main` | current source of truth | latest repo-local restart rules and current bottleneck |
| `docs/INVARIANTS.md` from `origin/main` | current source of truth | product / engineering non-negotiables |
| `docs/architecture/*.md` from `origin/main` | current source of truth for M0 evidence | compile failure and boundary risks are explicitly current |
| `docs/02_design/ASSEMBLY_ARCHITECTURE.md` | useful but stale / needs reconciliation | local file includes newer WorldGen addendum but is dirty and partially mojibake; asmdef readback must win |
| `docs/02_design/DualGridTerrainSystem_Spec.md` | useful but partially stale | current DualGrid intent is documented, but not acceptance proof for runtime terrain algorithm feasibility |
| `docs/02_design/TERRAIN_ALGORITHM_NOTES_DUALGRID_HEIGHTMAP.md` | useful but needs reconciliation | describes current heightmap / DualGrid limits, including missing finalized mesh output |
| `docs/tasks/TASK_INDEX.md` and task tickets | useful but historical status needs verification | many tasks marked DONE, but compile/runtime acceptance is not current |
| `docs/HANDOVER.md`, `docs/WORKFLOW_STATE_SSOT.md` | useful but stale/contradictory | older progress claims conflict with latest compile failure |
| Prompt-listed root logs such as `CSG_INVESTIGATION_LOG.md`, `FUNCTION_TEST_STATUS.md` | legacy pointer / missing | not present at requested paths in local or `origin/main`; some equivalents exist under docs |
| `docs/01_planning/DEVELOPMENT_ROADMAP_2026.md` | useful but stale | broad plan predates latest M0 architecture front-door and compile evidence |

## 2. Product Boundary Decision

Recommended boundary:

- Primary product: Terrain Generation Engine.
- Secondary product: Simulator Harness for traversal, Trail, movement, and validation.
- Non-goal: finished game loop, content-complete game, story, combat, production gameplay.

### Proposed layers

| Layer | Belongs here | Does not belong here |
|---|---|---|
| `Vastcore.TerrainEngine` / immediate `Vastcore.Terrain` spine | heightmap/noise providers, chunk/tile management, Unity Terrain output, mesh/density adapters behind interfaces, generation reports | PlayerController, UI, camera, TrailRenderer, combat/story/game loop |
| `Vastcore.Terrain.Extensions` | DualGrid, prefab stamp terrain placement, erosion, roads/routes as data, biome layers, optional volumetric prototypes | external concrete ProBuilder/Deform public APIs, simulator-only movement |
| `Vastcore.Simulator` / current `Vastcore.Player` + `Vastcore.Game` | traversal physics, player controller, Trail visual, route testing, harness scenes | owning terrain generation algorithms or terrain asset adapters |
| `Vastcore.EditorTools` | StructureGenerator windows/tabs, generation inspectors, CSG provider UI, validation/scanner windows, asset inventory tools | runtime terrain core and simulator runtime logic |
| `Vastcore.Tests` | EditMode / PlayMode tests, algorithm PoCs, compile gates, asset-adoption gates | production dependencies on test helpers |

### Proposed dependency rules

```text
Utilities -> Core -> TerrainEngine -> Terrain.Extensions -> Simulator/Game
                         ^
                         |
                 EditorTools / Tests may inspect
```

Allowed:

- Simulator depends on TerrainEngine APIs.
- EditorTools depends on TerrainEngine and optional Integration providers.
- Tests depend broadly enough to verify contracts.
- TerrainEngine exposes provider/streaming/traversal data contracts.

Forbidden:

- TerrainEngine depending on Player, Camera, UI, TrailRenderer, or Game.
- TerrainEngine public APIs exposing ProBuilder, Deform, Parabox, or EasyRoads concrete types.
- Boolean/CSG becoming a required runtime terrain dependency.
- Voxel/Marching Cubes becoming primary without explicit user reapproval and PoC gates.

## 3. Dual-Grid / Dual Contouring Feasibility Matrix

Important distinction: the repo's `DualGrid` is not a proven dual contouring implementation. It is currently a hex/cell topology, corner/node data model, vertical extrusion from heightmap/noise, and prefab stamp placement path. The volumetric surface extraction path exists separately under WorldGen / Volumetric / MeshExtraction and currently uses a marching-tetrahedra-style extractor. Therefore "DualGrid feasibility" has two meanings:

1. Current VastCore DualGrid as layout/stamp/height extrusion: feasible as a terrain-layout and designer-placement system, with medium confidence.
2. Dual contouring / dual grid surface extraction for caves, overhangs, and mining: not proven in repo; prototype only, low-to-medium confidence until PoC.

| Option | VastCore fit | Caves / overhangs / mining | Performance risk | Implementation complexity | Editor tooling complexity | Runtime mutation | Mobile viability | Dependency risk | Recommended role | Smallest PoC |
|---|---|---|---|---|---|---|---|---|---|---|
| A. Unity Terrain heightmap | High for baseline engine and broad landscape | Weak. holes possible, true overhangs absent | Low-medium | Low | Low | Height mutation possible; holes need test | High | Low | Primary baseline | Generate 3x3 chunks from `IHeightmapProvider`, mutate one area, verify seams |
| B. Mesh chunk terrain | High for custom surfaces and chunk control | Medium if mesh regenerated; caves require more | Medium | Medium | Medium | Good if chunk rebuild pipeline exists | Medium | Low-medium | Secondary / engine extension | One chunk mesh from height provider, rebuild dirty patch under frame budget |
| C. Voxel density field + marching cubes/tetrahedra | High for caves/mining, but conflicts with current main invariant if made default | Strong | High | High | High | Strong with dirty regions | Low-medium | Low external, high internal complexity | Prototype only unless reapproved | Use `DensityGrid` + `MarchingCubesMeshExtractor` for subtract-sphere cave, measure mesh and dirty rebuild |
| D. Dual contouring / dual grid surface extraction | Potentially high for sharp features and cell-based topology | Strong if QEF / Hermite data are implemented | High | Very high | Very high | Strong if chunked carefully | Low-medium | Low external, high algorithm risk | Prototype only | Implement one cube-cell dual contouring test with sign samples and normals; compare against marching extractor |
| E. Hybrid heightfield + local mesh deformation | High for current product direction | Medium. local caves/mining can be mesh-only pockets | Medium | Medium-high | Medium | Good for bounded mutation | Medium | Medium if Deform/ProBuilder leak | Secondary | Heightfield world plus local mesh crater/cave patch behind interface |
| F. External terrain tool / asset-assisted workflow | Medium as authoring acceleration | Depends on asset | Medium unknown | Low-medium if asset is stable | Medium-high | Unknown | Unknown | High licensing/API risk | Postponed / asset gate only | Import-free paper gate: does asset export plain mesh/data and support chunk streaming? |

Decision: Do not implement or promote dual contouring before a PoC definition and acceptance tests exist. Retain current DualGrid as the designer/stamp/height-layout path, but do not claim it solves mining or volumetric deformation.

## 4. Asset Use Decision: Easy Road 3D and Other Assets

Easy Road 3D / EasyRoads3D was not found in `Assets/`, `Packages/`, docs, `.unitypackage` remnants, or matching meta names during this slice. The repo does contain internal road/path concepts under `WorldGen.GraphEngine`:

- `RoadGraphGenerator`
- `GraphGenerationSettings`
- `GraphFieldBurner`
- graph overlay / gizmo tooling

### Road / path asset classification

| Use mode | Fit | Decision |
|---|---|---|
| Part of Terrain Engine core | Weak | Reject for now. Road assets should not be required to generate terrain |
| Terrain extension / route data | Medium | Possible if converted to plain graph/spline/mesh data |
| Simulator Harness helper | Medium | Could validate traversal, but must not become game-feature scope creep |
| Editor-only authoring aid | Strongest | Acceptable only if exported data is plain and engine remains reusable |
| Not used / postponed | Strong | Current default |

Asset adoption criteria before any road asset:

- It helps terrain generation or traversal validation beyond what `WorldGen.GraphEngine` already provides.
- It can export or be adapted to plain mesh, spline, graph, or density-field data.
- It works with chunked/streamed terrain and does not require monolithic scene authoring.
- It does not expose proprietary concrete types through Terrain Engine public APIs.
- License/source-control behavior is compatible with the repo.
- It passes a no-import paper review, then a throwaway-branch import gate, then a compile/test gate.

Decision: Do not adopt Easy Road 3D now. If roads become a bottleneck, first test the internal graph path and Splines adapter possibility.

## 5. Mining / Digging / Deformation Asset Gap Analysis

| Capability | Current repo evidence | Gap | Decision |
|---|---|---|---|
| Deform integration | Package present, `DEFORM_AVAILABLE`, Deform refs in Generation/Terrain, `Assets/Deform` | Visual/mesh deformation helper, not topology/mining foundation | Keep optional; isolate behind adapter |
| Unity Terrain holes / TerrainData | Terrain modules present; `SetHeights` evidence; holes not proven | Heightfield cannot overhang; holes need TerrainData tests | Baseline-only, not core mining |
| Mesh Boolean / CSG | Editor CSG providers and manual tests | Runtime stability and API compatibility not proven | Editor optional only |
| Voxel/SDF mining | `DensityGrid`, `SdfMath`, `CaveDensityField`, `DirtyRegion`, `VolumetricStreamingController`, marching extractor | Deformation engine is stub; subtract-sphere dirty rebuild not implemented | Best mining PoC candidate, not default |
| Chunked mesh regeneration | Volumetric dirty tracking and chunk pooling exist | Needs compile, perf, seam tests | PoC gate required |
| Decals / visual-only damage | No clear dedicated system | Useful fallback for simulator validation | Non-destructive fallback candidate |
| Third-party mining asset | None present | Need capability definition first | Do not shop/import before acceptance tests |

Asset gap conclusion: the missing capability is not "a mining asset" in general. The missing capability is an accepted, tested mutation contract:

```text
Input: world-space edit volume
Effect: terrain data/mesh/density changes
Output: dirty region, regenerated surface, collision update, visual feedback
Gate: compile green, bounded frame cost, seam-safe, undo/editor policy if editor-side
```

Any future asset must pass that contract without forcing external concrete types into Terrain Engine APIs.

## 6. Boolean / CSG Strategy

### Current Boolean / CSG history and state

| Item | State | Classification |
|---|---|---|
| `docs/01_planning/SG1_TEST_VERIFICATION_PLAN.md` | local doc references older CSG pending state | useful but stale |
| `docs/04_reports/REPORT_PB-2_CsgProviderResolverTestStabilization.md` | CSG resolver smoke stabilization | useful but not runtime acceptance |
| `Assets/Editor/StructureGenerator/Utils/*Csg*.cs` | provider abstraction exists | current implementation state |
| `Assets/Editor/Tools/ProBuilderCsg/*` | scanner/PoC reflection tools | diagnostic/editor-only |
| `Assets/Tests/EditMode/CsgProviderResolverSmokeTests.cs` | reflection smoke test | current test but narrow |
| `Assets/Tests/EditMode/BooleanTest.cs` | Parabox gated manual menu tests | legacy/manual-only unless symbols/packages present |

### Strategy comparison

| Strategy | Stability | API compatibility risk | Testability | Runtime suitability | Editor suitability | Dependency impact | Recommendation |
|---|---|---|---|---|---|---|---|
| A. Avoid Boolean as core engine dependency | High | Low | High | High | Neutral | Low | Default |
| B. Keep Boolean editor-only optional tool | Medium-high | Medium | Medium | Low | High | Contained | Recommended |
| C. Use ProBuilder internal CSG via reflection | Low-medium | High | Medium | Low | Medium | Medium if isolated | Experimental provider only |
| D. Use Parabox/pb_CSG behind provider abstraction | Medium unknown | Medium-high | Medium | Low-medium | Medium | High if package added | Optional provider only after gate |
| E. Replace Boolean with voxel/SDF/chunk regeneration for mining | Medium | Low external, high algorithm | Medium-high after PoC | High | Medium | Internal complexity | Best mining direction after PoC |

Decision: Boolean/CSG is not the core terrain engine foundation. Keep it behind an optional editor/provider abstraction. For mining, prefer SDF/voxel/chunk regeneration PoC if the user explicitly reapproves volumetric work for that use case.

## 7. Terrain Engine vs Simulator Harness Split

### Keep as-is for now

| Area | Reason |
|---|---|
| Current asmdefs | compile is already not trusted; do not bulk-move before restoration |
| `Vastcore` namespace casing | latest target architecture recommends avoiding a case-only migration while compile is failing |
| CSG provider files | already editor-only; leave until CSG slice |
| Player/controller code | useful simulator harness; do not delete or split project |
| WorldGen/Volumetric code | valuable experiment/extension; keep parked behind explicit gates |

### Later moves to consider

| Future move | Purpose | Gate |
|---|---|---|
| `Vastcore.TerrainEngine` or cleaned `Vastcore.Terrain` core | reusable terrain generation spine | compile green and source ownership map |
| `Vastcore.Terrain.Extensions` | DualGrid / erosion / route / volumetric extensions | core provider/output tests pass |
| `Vastcore.Integrations` | ProBuilder / Deform / Splines / CSG adapters | external types removed from public core APIs |
| `Vastcore.Simulator` | player traversal and Trail harness | terrain API can be consumed without circular refs |
| Narrow tests by owner | reduce broad asmdef dependencies | after module split proposal accepted |

### Interface boundary sketch

Terrain Engine should expose:

- `IHeightmapProvider`
- terrain generation config / recipe boundary
- chunk/tile stream update API
- generated mesh / Unity Terrain output adapter
- dirty region contract
- traversal surface query, if needed, as data contract

Simulator should provide:

- target transform or target position provider
- traversal sampling requests
- player movement / Trail visual state
- test harness scenes and PlayMode validation

Forbidden dependency:

```text
TerrainEngine -> PlayerController / AdvancedPlayerController / TrailRenderer / UI / Camera
```

Allowed dependency:

```text
Simulator -> TerrainEngine
EditorTools -> TerrainEngine + optional Integration providers
Tests -> owning surfaces under test
```

## 8. Recommended Next 3-5 Slices

| turn | owner | weight | estimate | goal | deliverable | gate | status | branch |
|---|---|---|---|---|---|---|---|---|
| T+1 | assistant | W2 [##---] | 1 focused slice | architecture boundary proposal refinement | accepted module boundary doc + compile-restoration plan | no code moves, exact affected asmdefs named | proposed | `codex/restart-architecture-boundaries` |
| T+2 | assistant | W3 [###--] | 1-2 slices | terrain algorithm PoC selection | PoC spec choosing heightmap baseline vs mesh chunk vs volumetric | acceptance tests defined before code | proposed | `codex/terrain-algorithm-poc` |
| T+3 | assistant/tool | W3 [###--] | 1 slice | CSG / deformation provider test decision | provider gate for ProBuilder/Parabox/Deform/SDF | editor-only vs runtime role decided | proposed | `codex/csg-deformation-gate` |
| T+4 | assistant | W4 [####-] | 2 slices | simulator harness isolation | player/trail traversal harness boundary proposal and first narrow cleanup | Terrain no longer grows player-specific APIs | proposed | `codex/simulator-harness-boundary` |
| T+5 | shared | W2 [##---] | 1 slice | asset adoption gate | EasyRoads/mining/road asset acceptance checklist and no-import review | asset has concrete value over internal path | proposed | `codex/asset-adoption-gate` |

## Decision Packet

### Recommended immediate default

| Decision | Confidence | Reason |
|---|---|---|
| Treat VastCore as Terrain Generation Engine + Simulator Harness | high | repo-local invariant and user concern align; Player/Game should consume terrain |
| Do not pursue full game completion | high | current bottleneck is engine architecture and compile integrity, not game loop |
| Do not implement dual-grid/dual-contouring before PoC definition | high | repo DualGrid is not dual contouring; volumetric path is unaccepted as main route |
| Do not make Boolean/CSG a hard engine dependency | high | CSG is editor/provider/reflection-oriented and not runtime-proven |
| Do not adopt Easy Road 3D or mining asset until asset gate passes | high | EasyRoads absent; asset adoption would be premature |

### Alternatives

| Alternative | Pros | Cons | Recommendation |
|---|---|---|---|
| Keep current mixed architecture | least immediate movement | keeps compile/boundary reasoning hard | reject as default |
| Split into separate Unity projects | strong isolation | high overhead, asset duplication, slower iteration | reject for now |
| Split inside one Unity project using asmdefs/folders | good isolation with low operational cost | needs careful sequence after compile | prefer |
| Build terrain as package-like module inside same repo | reusable and testable | requires stable API and clean dependencies | target shape after compile |

Recommendation: stay in one Unity project, split by asmdefs/namespaces/folders, and make Simulator depend on Terrain Engine rather than the reverse.

## Completion Matrix / Done Gates

| gate | done | total | unknown | meter | missing |
|---|---:|---:|---:|---|---|
| Repo state verified | 5 | 5 | 0 | [#####] 5/5 | none |
| Source docs classified | 5 | 6 | 1 | [#####?] 5/6 +1? | old root CSG/FUNCTION docs absent; equivalents classified |
| Asset inventory | 5 | 5 | 0 | [#####] 5/5 | none |
| Architecture split proposal | 6 | 6 | 0 | [######] 6/6 | none |
| Algorithm decision packet | 6 | 6 | 0 | [######] 6/6 | none |
| Report hygiene | 8 | 8 | 0 | [########] 8/8 | none |

Metric Change Note: meters use exact slot counts because all totals are <= 12. Unknown slots use `?`.

## Work Performed vs Expected

| Expected | Performed | Current state |
|---|---|---|
| Verify git/worktree | branch, HEAD, origin/main, dirty status checked; fetch completed | done; merge intentionally not performed |
| Inventory project structure | top-level folders, script counts, asmdefs, assets, packages inspected | done |
| Create audit artifact | this file | done |
| Create OpenSpec change proposal | `openspec/changes/restart-architecture-boundaries/` | done |
| Avoid implementation changes | no runtime/editor code edited | done |
| Safe validation | package/version/asmdef/script inspection done; Unity compile not run | partial by design |

## Changed Files

| File | Purpose |
|---|---|
| `docs/restart/VC_RESTART_ARCHITECTURE_AUDIT.md` | repo-grounded restart architecture audit and decision packet |
| `openspec/changes/restart-architecture-boundaries/proposal.md` | proposal for future architecture boundary work |
| `openspec/changes/restart-architecture-boundaries/tasks.md` | task checklist for the future proposal |
| `openspec/changes/restart-architecture-boundaries/specs/architecture-boundaries/spec.md` | draft requirements for module boundaries |

Existing dirty files were not modified by this slice.

## Artifacts / Review Access

Primary artifact:

- `docs/restart/VC_RESTART_ARCHITECTURE_AUDIT.md`

Proposal artifacts:

- `openspec/changes/restart-architecture-boundaries/proposal.md`
- `openspec/changes/restart-architecture-boundaries/tasks.md`
- `openspec/changes/restart-architecture-boundaries/specs/architecture-boundaries/spec.md`

## Review Card / Review Debt

| Item | Why it matters | Current state | Next move |
|---|---|---|---|
| origin/main not merged | local worktree has overlapping dirty changes | fetch only | decide whether to stash/merge in a dedicated sync slice |
| Unity compile unknown today | docs-only slice did not run Unity | latest M0 says compile failing | run compile restoration slice before feature work |
| old root docs absent | prompt listed historical files not present at root | equivalents exist under docs | keep classification in this audit; do not recreate old root docs |
| OpenSpec convention absent | no existing `openspec/` found | minimal scaffold created | refine only if the repo adopts OpenSpec workflow |

## Command / Action Ledger

| Action | Result |
|---|---|
| `git fetch --all --prune` | `origin/main` advanced to `3893388` |
| `git status --short --branch` | local `main` dirty and behind origin |
| `git diff --name-only HEAD..origin/main` | upstream overlaps many docs/assets |
| inspected `ProjectSettings/ProjectVersion.txt` | local `6000.4.9f1`, origin `6000.3.6f1` |
| inspected `Packages/manifest.json` and lock | package diffs recorded |
| inspected `Assets/**/*.asmdef` | assembly dependency snapshot recorded |
| searched DualGrid / Deform / CSG / EasyRoads / Trail | evidence summarized above |
| inspected latest repo-local rules from `origin/main` | `AGENTS.md`, `REPO_LOCAL_RULES`, `runtime-state`, `INVARIANTS`, `docs/ai` |

## User-Side Work

No immediate user action is required to use this audit. User decisions become useful at the next gate:

| Entry | Friction reduced | Selecting it enables |
|---|---|---|
| Advance: same-project module split | removes architecture ambiguity | compile-safe boundary proposal and later asmdef cleanup |
| Audit: merge/sync working tree | removes remote/local uncertainty | implementing on top of `origin/main` without hidden conflicts |
| Verify: Unity compile restoration | removes acceptance uncertainty | real tests and implementation slices |
| Explore: terrain algorithm PoC | removes dual-grid feasibility ambiguity | choosing heightmap/mesh/volumetric route with evidence |

## Agent-Side Work

Recommended assistant-owned next action: do not start feature implementation. First create a narrow compile-restoration proposal or sync plan based on the fetched `origin/main` and local dirty state. After compile/asset integrity is known, proceed to T+1 boundary proposal refinement.

## Goal Stack

| level | goal | success signal | contribution |
|---|---|---|---|
| Immediate slice | Create repo-grounded restart audit | this doc and OpenSpec proposal exist | prevents restarting from stale assumptions |
| Short-term | Separate Terrain Engine from Simulator Harness | documented asmdef/folder/namespace rules | enables parallel terrain and movement work |
| Mid-term | Decide terrain algorithm before coding | feasibility matrix and PoC gate exist | avoids impossible or over-scoped generator |
| Long-term | Keep VastCore reusable as terrain engine | engine API and simulator harness are distinct | prevents drift into full game production |

## Turn Calendar

| turn | default | stop condition |
|---|---|---|
| T+1 | refine architecture boundary and sync/compile plan | if merge conflicts or package changes require user choice |
| T+2 | terrain algorithm PoC selection | if user reopens voxel/marching cubes as main path without explicit gate |
| T+3 | CSG/deformation provider decision | if asset import or package dependency is required |
| T+4 | simulator harness isolation | if movement behavior changes require design approval |
| T+5 | asset adoption gate | if license/import/source-control risk appears |

## Visual Summary

```text
Repo readback       [#####] 5/5
Docs classified     [#####?] 5/6 +1?
Assets inventoried  [#####] 5/5
Boundary proposal   [######] 6/6
Algorithm packet    [######] 6/6
Report hygiene      [########] 8/8

Recommended default:
Terrain Engine core first
Simulator Harness as consumer
CSG/Deform/Road assets behind optional gates
DualGrid retained as layout/stamp path, not mining proof
```

## Continuation State / Handoff Gate

Handoff Gate: false.

Reason: the next move is still in the same project context and can proceed from this audit. A next-agent prompt is intentionally not included.

## Follow-up Status

The sync/compile-restoration gate for T+1 is recorded in `docs/restart/VC_SYNC_COMPILE_RESTORATION_PLAN.md`.

Current follow-up result: a clean `origin/main` worktree was created and Unity `6000.3.6f1` was launched there, but validation stopped at Package Manager resolution before C# compilation. Architecture/product implementation remains blocked until package resolution, invalid `.meta` GUIDs, and the `StructureTagAdapter.cs` compile boundary are handled in that order.

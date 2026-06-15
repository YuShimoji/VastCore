# VastCore M0 Refactor Risk Register

Last updated: 2026-06-15

This register lists risks found during M0. It does not authorize immediate code
movement; each code change still needs a proposed-change block and a narrow
rollback plan.

| Risk | Purpose | Effect | Requirements | Current state | Owner | Next move |
|---|---|---|---|---|---|---|
| Compile failure in `StructureTagAdapter.cs` | Restore compile as the first gate. | Blocks tests and reliable Unity validation. | Decide whether `CompoundArchitecturalGenerator` belongs in Generation, Terrain, or Integration. | Current Unity compile fails with CS0234. | assistant | Create a minimal compile-restoration proposal before editing. |
| Invalid `.meta` GUIDs | Preserve Unity asset identity. | Unity ignores affected assets and tests; compile/test evidence may be incomplete. | Repair/regenerate `.meta` files with Unity-safe handling. | Five `.meta` files have blank `guid:` values. | assistant/tool, with Unity verification if needed | Fix as a narrow asset-integrity task before broad refactors. |
| External package leakage into runtime core paths | Keep terrain engine removable from ProBuilder/Deform. | Core/terrain cannot be validated independently of optional packages. | Introduce adapter/integration boundaries and move concrete APIs out of core contracts. | `Vastcore.Generation` and `Vastcore.Terrain` reference ProBuilder/Deform. | assistant | After compile restoration, split external concrete code into Integration/Editor plan. |
| Namespace/folder/asmdef mismatch | Make ownership visible and enforceable. | Type lookup and dependency reasoning fail even when namespaces look correct. | One chosen root namespace and folder/asmdef map. | Many `Assets/Scripts/Terrain/**` files declare `Vastcore.Generation`. | assistant | Draft namespace migration sequence; do not bulk rename in M0. |
| Core folder contains UI/Generation/Utilities namespaces | Keep core low-level and stable. | Core ownership is ambiguous; low-level code may carry UI/generation assumptions. | Identify each misplaced file's real owner. | Examples: `PerformanceMonitor.cs`, `GeologicalFormationGenerator.cs`, `IPoolable.cs`, `ILoggable.cs`. | assistant | Add to compile-restoration follow-up after P0 failures. |
| `Vastcore.Core` references TextMeshPro | Keep core independent of UI packages. | Core may require UI package when pure terrain core should not. | Locate actual TextMeshPro usage and move or abstract it. | Confirmed in `Vastcore.Core.asmdef`. | assistant | Inspect usage before editing; remove only if safe. |
| Terrain depends on WorldGen | Keep M1 minimal terrain core independent. | Minimal provider/output may pull in broader graph/stamp systems. | Decide whether WorldGen is extension or separate engine layer. | `Vastcore.Terrain` references `Vastcore.WorldGen`. | assistant | Keep for now; plan decoupling only after compile is green. |
| Editor tool dependencies are broad | Protect runtime/editor boundary. | Editor-only ProBuilder/CSG code could leak into runtime when asmdefs move. | Keep all CSG/authoring code editor-only. | Editor asmdefs reference ProBuilder/Deform and tests reference editor structure generator. | assistant/tool | Validate editor-only asmdef include platforms before any move. |
| Trail scope drift | Prevent game feature from distorting terrain core. | Terrain API could grow around movement visuals rather than terrain data. | Treat current trail as game/player behavior. | Only observed Trail evidence is `TrailRenderer` in Player movement. | assistant/user | Leave out of M0/M1; spec RouteField only if user explicitly approves. |
| Historical docs conflict with current evidence | Avoid progress laundering. | README claims zero compile errors while current compile fails. | Treat historical docs as context, not acceptance. | Current compile log contradicts old status text. | assistant | Update docs only through owning docs when fixing actual state. |

## Priority Order

1. Restore compile or document the exact compile blocker.
2. Repair invalid `.meta` GUIDs safely.
3. Remove or quarantine external concrete API leakage from runtime terrain/generation.
4. Normalize namespace/folder/asmdef ownership.
5. Re-run EditMode tests, then PlayMode or manual Unity verification where needed.

## Quarantine Candidates

| Candidate | Why it is risky | Immediate treatment |
|---|---|---|
| `Assets/TextMesh Pro/Examples & Extras` | Vendor samples add noise to scans and scenes. | Exclude from M0 scans; do not delete without Unity asset review. |
| `Assets/test`, `Assets/test_Terrain`, `Assets/TerrainPresets 1` | Purpose and owner are unclear. | Inventory only; quarantine plan before deletion. |
| Volumetric/MarchingCubes path | Frozen by invariant unless explicitly reapproved. | Keep out of active M1 route. |
| MarchingSquares path | Needs conflict resolution with Prefab Stamp route. | Keep as hold/experiment. |
| ProBuilder/Deform runtime primitive generation | Useful but overcoupled with core generation. | Adapter/integration candidate, not core. |

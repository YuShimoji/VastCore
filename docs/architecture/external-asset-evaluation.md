# VastCore M0 External Asset Evaluation

Last updated: 2026-06-15

This is an M0 inventory, not an adoption decision. No new external assets were
introduced during this pass.

## Evaluation Table

| Package / Asset | Current Use | Category | Core Risk | Adapter Feasibility | Verdict |
|---|---|---|---|---|---|
| Unity Terrain modules | Terrain output and terrain physics modules are present. | Engine Core output target | Low; Unity Terrain is the intended minimum output. | Direct output adapter is feasible. | Adopt as minimum output path. |
| URP `17.3.0` | Project render pipeline. | Platform/render infrastructure | Low for terrain core; high only if generation logic depends on rendering. | No terrain adapter needed. | Keep. |
| ProBuilder `6.0.8` | Runtime primitive generation and editor CSG/authoring code. | Integration / Editor Tool | High; concrete `UnityEngine.ProBuilder` appears in runtime generation/terrain files. | Feasible if moved behind adapter/editor boundaries. | Adapter-only; do not treat as core. |
| Deform git package | Runtime deformation helpers and high-quality primitive generation. | Integration | High; `Deform` is referenced by runtime generation/terrain asmdefs. | Feasible via optional adapter with `DEFORM_AVAILABLE`. | Adapter-only; isolate before core work. |
| CSG / ProBuilder internal CSG | Editor StructureGenerator CSG providers. | Editor Integration | Medium; acceptable only editor-side. | Feasible as editor-only provider/resolver. | Prototype/editor-only; requires issue `#44` verification. |
| Splines `2.8.2` | Package present; no direct M0 terrain core evidence. | Possible Engine Extension | Medium if pulled into core prematurely. | Feasible later as RouteField/road/river adapter. | Defer. |
| Unity AI packages | Assistant/generators/inference packages present. | Tooling | High if runtime game/engine depends on them. | Keep outside terrain API. | Defer/tooling only. |
| MCPForUnity | Unity tool integration package. | Tooling | Medium if project runtime depends on tool package. | Tool-only boundary. | Keep as tooling, not core. |
| TextMeshPro/uGUI/InputSystem | UI/player/game support. | Game Feature support | High if referenced by Core/Terrain. | Keep in UI/Player/Game assemblies. | Keep game-side; remove from core/terrain where possible. |
| WorldGen internal modules | Graph, field, grammar, recipe, stamps. | Engine Extension / Experiment | Medium; can make M1 depend on broader worldgen. | Internal facade already exists but dependency direction needs review. | Defer from core; evaluate after compile restore. |
| TextMesh Pro Examples & Extras | Vendor sample scenes/scripts. | Quarantine candidate | Low compile risk but high scan noise. | Not needed. | Exclude from architecture decisions; do not delete without asset review. |

## Adoption Rules For Future Reviews

1. Do not adopt an external asset into core if a minimum adapter cannot be
   planned within 10 work steps.
2. Do not expose external concrete types through `Vastcore.Core` or
   `Vastcore.Terrain` public APIs.
3. Separate runtime generation needs from editor authoring convenience.
4. If an external package is removed, Noise / HeightMap / Unity Terrain output
   must still remain possible.
5. Existing dependencies are evaluated by the same rules as new dependencies.

## Immediate External-Asset Findings

- ProBuilder and Deform are already runtime assembly dependencies, not merely
  optional authoring tools.
- CSG is currently editor-oriented and should stay editor-only.
- TextMeshPro appears in `Vastcore.Core` and `Vastcore.Terrain` references; that
  should be treated as a boundary smell until actual use is inspected.
- No package should be newly introduced before compile and `.meta` integrity are
  restored.

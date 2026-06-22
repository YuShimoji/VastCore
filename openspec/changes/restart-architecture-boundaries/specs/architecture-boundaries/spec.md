# Spec: Architecture Boundaries

## ADDED Requirements

### Requirement: VastCore product boundary

VastCore shall be treated as a Terrain Generation Engine with a Simulator Harness, not as a finished game project.

#### Scenario: Terrain work is selected

- WHEN a terrain feature is proposed
- THEN the proposal shall state whether it belongs to Terrain Engine core, Terrain extension, EditorTools, Simulator Harness, Integration, or Tests.

#### Scenario: game feature pressure appears

- WHEN a task would add story, combat, content-complete gameplay, or production game loop behavior
- THEN the task shall be rejected or re-scoped unless the user explicitly approves full game scope.

### Requirement: Terrain Engine dependency direction

Terrain Engine code shall not depend on Player, Camera, UI, Game, TrailRenderer, or simulator-specific concrete types.

#### Scenario: simulator needs terrain data

- WHEN Player, Trail, or traversal validation needs terrain information
- THEN Simulator Harness shall call Terrain Engine APIs or provide a target/position interface to Terrain Engine.

#### Scenario: Terrain Engine needs target position

- WHEN Terrain Engine needs a moving target for streaming or caching
- THEN it shall depend on a neutral transform/position provider contract, not a player controller concrete type.

### Requirement: external asset isolation

External terrain, deformation, road, CSG, or mesh packages shall not expose concrete types through Terrain Engine public APIs.

#### Scenario: ProBuilder or Deform is used

- WHEN ProBuilder or Deform functionality is required
- THEN it shall be placed behind an integration/editor/provider boundary and guarded by compile/test gates.

#### Scenario: EasyRoads or another road asset is considered

- WHEN a road/path asset is proposed
- THEN the adoption gate shall verify plain data export, chunk/streaming compatibility, license/source-control safety, and value over internal `WorldGen.GraphEngine` roads before import.

### Requirement: algorithm PoC before main-path adoption

Dual contouring, voxel/SDF mining, Marching Cubes/Tetrahedra, and Boolean/CSG shall not become Terrain Engine foundations without a PoC and acceptance tests.

#### Scenario: DualGrid is discussed

- WHEN DualGrid is used as a term
- THEN the proposal shall distinguish current VastCore DualGrid layout/stamp extrusion from dual contouring surface extraction.

#### Scenario: mining or digging is requested

- WHEN mining/digging modifies physical terrain
- THEN the candidate implementation shall define edit input, dirty region output, regenerated surface/collision behavior, seam handling, and performance gate before code implementation.

### Requirement: verification language

Compile, test, or Unity Editor acceptance shall only be claimed when an actual command or Editor validation has been run in the current relevant state.

#### Scenario: docs-only audit

- WHEN a slice only reads and writes documentation
- THEN Unity compile status shall be reported as unknown unless the compile command was actually run and passed.

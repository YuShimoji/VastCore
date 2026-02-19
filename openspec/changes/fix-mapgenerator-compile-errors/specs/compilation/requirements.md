# Spec: Compilation Requirements for MapGenerator Runtime

## Requirement 1: Assembly-CSharp access to generation channel types
MapGenerator runtime scripts in `Assets/MapGenerator/Scripts` MUST be able to resolve `Vastcore.Generation.HeightMapChannel`.

### Acceptance
- `TerrainGenerator.cs` and `HeightMapGenerator.cs` compile without `HeightMapChannel` missing-type errors.

## Requirement 2: C# 9 compatibility in runtime structs
Runtime structs in `Vastcore.Generation` MUST avoid C# 10-only features when the project language version is C# 9.

### Acceptance
- `PrimitiveTerrainObject.cs` compiles without CS8773.

## Requirement 3: Namespace imports must match actual assembly availability
Runtime scripts in Assembly-CSharp MUST NOT import namespaces that are not required and not referenced.

### Acceptance
- `TextureGenerator.cs` compiles without CS0234 for `Vastcore.Utilities`.
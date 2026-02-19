# Spec: Terrain Runtime Compilation Requirements

## Requirement 1: Player controller lookup compatibility
Scripts MUST not call Unity generic object lookup APIs with interface type parameters.

### Acceptance
- Terrain runtime scripts resolve player transform without `FindFirstObjectByType<IPlayerController>()`.

## Requirement 2: Template synthesis API availability
`BiomeSpecificTerrainGenerator` MUST be able to call `TerrainSynthesizer.SynthesizeTerrain(...)`.

### Acceptance
- `BiomeSpecificTerrainGenerator.cs` compiles without missing member errors.

## Requirement 3: Single BiomePreset type identity
Runtime systems MUST use one `Vastcore.Generation.BiomePreset` type across assemblies.

### Acceptance
- No implicit conversion errors between distinct `BiomePreset` assemblies.

## Requirement 4: Deform integration guards
Deform-specific symbols MUST compile correctly when `DEFORM_AVAILABLE` is undefined.

### Acceptance
- `HighQualityPrimitiveGenerator.cs` compiles without unresolved `VastcoreDeformManager` or `deformQuality` references.
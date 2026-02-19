# Fix Terrain Runtime Compile Errors Proposal

## Overview

Resolve remaining compile errors in terrain runtime scripts caused by interface lookup misuse, missing API surface, duplicate type definitions, and conditional Deform integration mismatches.

## Goals

- Restore clean compilation for `Vastcore.Terrain` and related runtime assemblies.
- Keep runtime behavior stable with low-risk changes.

## Acceptance Criteria

- No `CS0246` for `IPlayerController`.
- No `CS0311` from `FindFirstObjectByType<IPlayerController>()`.
- No `CS0117` for `TerrainSynthesizer.SynthesizeTerrain`.
- No `CS0029` conversion errors between duplicate `BiomePreset` definitions.
- No `CS1503`, `CS0103`, `CS1061` in `HighQualityPrimitiveGenerator`.
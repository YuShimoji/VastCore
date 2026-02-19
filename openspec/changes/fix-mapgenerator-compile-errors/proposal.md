# Fix MapGenerator Compile Errors Proposal

## Overview

Resolve current Unity compile failures introduced by recent assembly/reference and language-feature changes.

## Problems

- `HeightMapChannel` cannot be resolved from `Assets/MapGenerator/Scripts/*`.
- `Vastcore.Utilities` namespace is unresolved in `TextureGenerator.cs`.
- C# 9 compiler rejects parameterless struct constructor in `PrimitiveTerrainObject.LODStatistics`.
- Bee script updater error appears as a secondary symptom when script compilation fails.

## Goals

- Restore successful script compilation in Unity without broad architecture rollback.
- Keep runtime assembly boundaries explicit while applying minimal-risk fixes.

## Acceptance Criteria

- No CS8773 in `PrimitiveTerrainObject.cs`.
- No CS0234 in `TextureGenerator.cs`.
- No CS0246 (`HeightMapChannel`) in MapGenerator runtime scripts.
- Unity script compilation completes (no `updates.txt` updater failure caused by compile errors).
# Terrain Visual Improvement Proposal

## Overview

Improve terrain primitive visuals focusing on readability and performance: URP-compatible materials, shadows, and simple distance-based LOD scaling for primitives.

## Goals

- URP "Lit" material by default with fallback to Standard
- Enable shadow casting and receiving on primitives
- Distance-based LOD scaling with two thresholds (L1, L2)
- Keep system lightweight and easy to tune from Inspector

## Acceptance Criteria

- Scene renders with URP Lit when pipeline available; no pink materials
- Shadows on by default, no excessive overdraw
- LOD transitions are smooth (lerped), no popping
- Parameters are serialized and tunable at runtime

## Risks

- Excessive shadow cost on low-end GPUs
- Scaling-only LOD may not reduce triangle count; mitigated by large-distance cleanup

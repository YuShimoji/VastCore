# Redevelopment Local Setup Guide

## 1. Repository acquisition (Windows PowerShell)
```powershell
git clone https://github.com/YuShimoji/VastCore.git
cd VastCore
git fetch --all --prune
git merge --ff-only origin/main
git submodule update --init --recursive
```

Notes:
- This repository contains `.shared-workflows` as submodule.
- Prefer `merge --ff-only origin/main` over `git pull` when local branch config has multiple merge targets.

## 2. Required toolchain
- Unity Editor: `6000.3.3f1` (from `ProjectSettings/ProjectVersion.txt`)
- Git: current stable version
- Optional: Visual Studio 2022 / Rider for C# navigation

## 3. Local preparation for redevelopment restart
1. Verify clean workspace:
```powershell
git status --short --branch
```
2. If Unity cache is stale or branch switched heavily, remove cache folders (Unity will regenerate):
- `Library/`
- `Logs/`
- `Temp/` (if exists)
3. Open `VastCore` with Unity Hub using `6000.3.3f1`.
4. Wait for package/domain reload completion, then inspect Console for compile errors.

## 4. Baseline verification checklist
1. Open core docs:
- `docs/HANDOVER.md`
- `docs/01_planning/DEVELOPMENT_ROADMAP_2026.md`
2. Run EditMode tests (Unity Test Runner):
- `Vastcore.Tests.EditMode.HeightMapGeneratorTests`
- `Vastcore.Tests.EditMode.TerrainGeneratorIntegrationTests`
3. Manual smoke:
- Confirm `TerrainGenerator` can create terrain in Noise / HeightMap / NoiseAndHeightMap modes.

## 5. Operational conventions for this repo
- Follow OpenSpec flow before implementation (`openspec/changes/...`).
- Use `VastcoreLogger` for new runtime logging instead of `Debug.Log`.
- Keep namespaces under `Vastcore.*` for new code.

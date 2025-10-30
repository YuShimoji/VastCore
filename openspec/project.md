# VastCore Project Context

## Overview
VastCore is a Unity-based terrain generation and exploration game engine. It provides procedural terrain generation, dynamic loading, and interactive gameplay features.

## Tech Stack
- **Engine**: Unity 6000.3.0b2
- **Language**: C#
- **Architecture**: Modular assembly structure (Vastcore.*.asmdef)
- **UI Framework**: Custom UI system (Vastcore.UI)
- **Rendering**: Universal Render Pipeline (URP)
- **Version Control**: Git
- **CI/CD**: GitHub Actions with auto-merge for quality gates

## Project Structure
- `Assets/Scripts/`: Core gameplay code
  - `Core/`: Base utilities and managers
  - `Generation/`: Terrain generation systems
  - `Terrain/`: Terrain rendering and management
  - `Player/`: Player controllers and movement
  - `UI/`: User interface components
- `Assets/Editor/`: Editor tools and inspectors
- `docs/`: Documentation and reports
- `Packages/`: Unity package dependencies

## Coding Conventions
- **Namespaces**: `Vastcore.*` for all components
- **Naming**: PascalCase for classes, camelCase for variables
- **Documentation**: XML comments for public APIs
- **Error Handling**: Custom logger system (`VastcoreLogger`)
- **Async**: Coroutines for Unity-specific async operations

## Current Status
- ✅ Compilation errors resolved
- ✅ UI migration completed (NarrativeGen.UI → Vastcore.UI)
- ✅ Basic terrain generation working
- ✅ Player movement and camera systems implemented
- ✅ LOD and performance optimizations added

## Development Phases
1. **Phase 1-2**: Compilation fixes and UI migration (✅ Completed)
2. **Phase 3**: Deform system integration (🔄 In Progress)
3. **Phase 4**: Terrain streaming system
4. **Phase 5**: Advanced terrain synthesis
5. **Phase 6**: Random control extensions

## Quality Gates
- Unit tests: 80% coverage minimum
- Security scans: No high/critical vulnerabilities
- Performance: 60 FPS target
- Code review: Required for Tier 2+ changes

## AI Development Guidelines
- Use OpenSpec for all feature changes
- Maintain backward compatibility
- Test all changes in Unity editor
- Update documentation for API changes
- Follow existing code patterns and architecture

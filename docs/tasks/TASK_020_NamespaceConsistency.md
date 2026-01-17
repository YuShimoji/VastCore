# Task: Namespace Consistency (Utils vs Utilities)

## Phase
Target Phase: Refactoring

## Status
Status: DONE

## Goal
Investigate the usage of `Vastcore.Utils` and `Vastcore.Utilities` namespaces, determine if they should be unified, and if so, perform the refactoring.

## Context
- HANDOVER.md indicated a potential issue/conflict between `Vastcore.Utils` and `Vastcore.Utilities` after the merge.
- Inconsistent namespaces can lead to confusion, circular dependencies, and maintainability issues.

## Proposed Strategy
1. Search the codebase for all occurrences of `namespace Vastcore.Utils` and `namespace Vastcore.Utilities`.
2. Analyze the contents of each. Are they duplicate concepts? Distinct modules?
3. Propose a unification plan (likely preferring `Vastcore.Utilities` or `Vastcore.Utils` based on existing convention).
4. Update the code and references (using regex or IDE refactoring tools).
5. Ensure assembly definitions (`.asmdef`) are updated if necessary.

## DoD (Definition of Done)
- [x] Map of usage for both namespaces created.
- [x] Unification plan approved (if unification is decided).
- [x] Code refactored to use a single namespace (or distinct ones if justified).
- [x] Compilation passes.

## Constraints
- Be careful of breaking references in Scenes/Prefabs if scripts are moved/renamed (namespace change usually breaks script references in serialized data if the script name itself or assembly changes, but namespace change on Monobehaviours needs `MovedFrom` attribute or text-based asset replacement).
- Use `[MovedFrom]` attribute if applicable to avoid serialisation data loss.

## Resolution Summary (2026-01-16)
- Unified all code to use `Vastcore.Utilities` namespace (matching the assembly definition).
- Updated 24 files total (3 namespace declarations, 21 using directives).
- Deleted stale `Vastcore.Utils.csproj`.
- No scene/prefab references to `VastcoreLogger` were found, so no `[MovedFrom]` attribute was needed.


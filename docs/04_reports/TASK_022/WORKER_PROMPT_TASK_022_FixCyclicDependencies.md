# Worker Prompt: TASK_022_FixCyclicDependencies

## 参照
- チケット: docs/tasks/TASK_022_FixCyclicDependencies.md
- SSOT: docs/Windsurf_AI_Collab_Rules_latest.md
- HANDOVER: docs/HANDOVER.md

## 境界
- **Focus Area**:
  - `*.asmdef` files (Assembly Definition Files)
  - `Assets/Scripts/` (only for moving files if necessary to break cycles)
  - `Packages/manifest.json` / `packages-lock.json` (if package dependencies are involved)
- **Forbidden Area**:
  - Deep logic changes in functional components (unless strictly necessary for interface extraction)
  - Modifying external assets unrelated to the dependency structure

## DoD (Definition of Done)
- [ ] Dependency graph analyzed and specific cycles identified.
- [ ] `.asmdef` references updated to remove cycles.
- [ ] Scripts moved if necessary to respect dependency layers (e.g., Editor -> Runtime).
- [ ] Project compiles without "Cyclic dependencies detected" error.
- [ ] No new runtime errors introduced.

## 停止条件
- If a major re-architecture is required (e.g., splitting a massive assembly into >3 new assemblies).
- If `sw-doctor` or compilation checks fail consistently despite fixes (suggesting environment issue).

## 納品先
- docs/inbox/REPORT_TASK_022_FixCyclicDependencies.md

## 手順のヒント
1. **Analyze**: Use `grep` or `find` to locate all `.asmdef` files and map their references.
2. **identify**: Pinpoint the cycle (A -> B -> A).
3. **Fix**:
   - Check if `Assembly-CSharp` is implicitly referenced.
   - Check if `Editor` assemblies are referencing other `Editor` assemblies in a cycle.
   - Move interfaces to a lower-level assembly (e.g., `Vastcore.Core`) if needed.
4. **Verify**: Run compilation check regularly.

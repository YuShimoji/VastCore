# Task: Release Cyclic Dependencies

Report: docs/reports/REPORT_TASK_022_FixCyclicDependencies.md

## Phase

Target Phase: Refactoring / Bugfix

## Status

Status: DONE

## Goal

Resolve cyclic dependencies between `Assembly-CSharp`, `Assembly-CSharp-Editor`, and multiple `Vastcore.*` assemblies to fix compilation errors.

## Context

- User reported a "Cyclic dependencies detected" error involving:
  - `Assembly-CSharp-Editor`
  - `Assembly-CSharp`
  - `Vastcore.Camera`
  - `Vastcore.Editor.StructureGenerator`
  - `Vastcore.Editor`
  - `Vastcore.Game`
  - `Vastcore.Generation`
  - `Vastcore.Player`
  - `Vastcore.Terrain`
  - `Vastcore.Testing`
  - `Vastcore.Tests.EditMode`
  - `Vastcore.UI`
- This prevents script compilation and build.

## Proposed Strategy

1. **Analyze Dependency Graph**:
   - Map out the references in `.asmdef` files involved in the error.
   - Identify specific cycles (e.g., A -> B -> C -> A).
   - Check for "Auto Referenced" settings in `.asmdef` files (Assembly-CSharp often implicitly references everything if not carefully managed).

2. **Break Cycles**:
   - **Isolate Editor Code**: Ensure `Vastcore.Editor` and `*-Editor` assemblies strictly depend on Runtime assemblies, never the other way around.
   - **Interface Segregation**: Move shared interfaces to a core assembly (e.g., `Vastcore.Core`) if two assemblies depend on each other for types.
   - **Splitting Assemblies**: If an assembly is too monolithic and causes cycles, consider splitting it (though minimizing change is preferred).
   - **Fix `Assembly-CSharp`**: `Assembly-CSharp` is the default assembly. If custom `.asmdef` assemblies reference it, and it implicitly references them (due to file location or auto-ref), a cycle occurs. Usually, code should either be ALL in `.asmdef`s or ALL in `Assembly-CSharp`, not mixed in a way that creates bidirectional deps.
     - *Hypothesis*: Some scripts in `Assembly-CSharp` (no asmdef) are referencing `Vastcore.*` asmdefs, while `Vastcore.*` asmdefs might be referencing back or `Assembly-CSharp` is trying to reference everything.

3. **Verify**:
   - Compile successfully.
   - Ensure no new runtime errors are introduced.

## DoD (Definition of Done)

- [x] Dependency graph analyzed and cycle identified.
- [x] `.asmdef` references updated to remove cycles.
- [x] Scripts moved if necessary to respect dependency layers. (不要と判断)
- [x] Project compiles without "Cyclic dependencies detected" error.

## Resolution (2026-01-30)

- **修正内容**: `Vastcore.Player.asmdef` に `Vastcore.Generation` 参照を追加
- **理由**: `PrimitiveInteractionSystem.cs` が `Vastcore.Generation.PrimitiveTerrainObject` を使用
- **循環依存**: 発生しない（Generation は Player/Terrain を参照しない）
- **レポート**: `docs/inbox/REPORT_TASK_022_20260130.md`

## Constraints

- Minimize moving files if simple `.asmdef` configuration changes can fix it.
- Maintain existing architecture intent where possible (Core -> Logic -> UI/Editor).

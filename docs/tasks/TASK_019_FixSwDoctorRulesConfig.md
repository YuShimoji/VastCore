# Task: SW Doctor Rules Configuration Fix

## Phase
Target Phase: Maintenance / Process Improvement

## Status
Status: OPEN

## Goal
Resolve the `sw-doctor` error reporting `docs/Windsurf_AI_Collab_Rules_v1.1.md` as missing by ensuring the configuration points to the correct SSOT file (`docs/Windsurf_AI_Collab_Rules_latest.md` or similar) or restoring the missing file.

## Context
- `sw-doctor` reported a critical issue: "SSOT docs/Windsurf_AI_Collab_Rules_v1.1.md not found".
- The project seems to use `docs/Windsurf_AI_Collab_Rules_latest.md`.
- We need to align the checker tool with the actual project structure.

## Proposed Strategy
1. Identify where `sw-doctor` defines the expected SSOT filename.
   - Likely in `.shared-workflows/scripts/sw-doctor.js` or a config file (e.g., `.shared-workflows/REPORT_CONFIG.yml` or local `.cursorrules`?).
2. Determine if we should:
   - Rename the local file to match the tool's expectation.
   - Update the tool/config to match the local file.
   - Create a symlink or copy.
3. Execute the fix so `sw-doctor` passes.

## DoD (Definition of Done)
- [ ] `sw-doctor --profile shared-orch-doctor` runs without the "SSOT not found" error.
- [ ] No regression in other checks.

## Constraints
- If modifying `.shared-workflows`, ensure it's a general fix or use a local override if available.
- Do not hardcode project-specific paths in shared scripts if avoidable.

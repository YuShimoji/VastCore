# OpenSpec Workflow for VastCore

This document outlines the OpenSpec workflow for the VastCore Unity project. All AI assistants should follow this process for any code changes or feature implementations.

## Workflow Steps

### 1. Change Proposal
When requested to implement a feature or make changes:
- Create an OpenSpec change proposal using `/openspec:proposal` or natural language
- Define clear specifications in `openspec/changes/{change-name}/specs/`
- Break down into actionable tasks in `tasks.md`

### 2. Specification Review
- Ensure specs align with VastCore architecture and conventions
- Include Unity-specific considerations (URP, assemblies, etc.)
- Define acceptance criteria and test requirements

### 3. Implementation
- Implement changes following the task breakdown
- Maintain code quality and documentation standards
- Test in Unity editor before completion

### 4. Validation
- Run compilation checks
- Verify no breaking changes to existing functionality
- Update documentation if needed

### 5. Archive
- Archive completed changes using `/openspec:archive`
- Update project specs with new changes

## Project-Specific Guidelines

### Unity Development
- Always test changes in Unity editor
- Respect assembly definitions and dependencies
- Use VastcoreLogger for logging instead of Debug.Log
- Follow namespace conventions (Vastcore.*)

### Code Quality
- Add XML documentation for public APIs
- Follow existing naming conventions
- Handle errors appropriately
- Consider performance implications

### Testing
- Write unit tests for new functionality
- Test scene loading and gameplay
- Verify UI components work correctly

### Documentation
- Update relevant docs/ files for changes
- Maintain README.md accuracy
- Document API changes clearly

## Quality Gates
Before archiving any change:
- ✅ Compiles without errors
- ✅ No breaking changes
- ✅ Tests pass (if applicable)
- ✅ Documentation updated
- ✅ Code review completed (for complex changes)

## Emergency Procedures
If compilation breaks:
1. Revert the problematic change immediately
2. Analyze the error in Unity editor
3. Fix the issue and re-implement properly
4. Test thoroughly before proceeding

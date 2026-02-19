# Tasks: Fix MapGenerator Compile Errors

- [x] Identify root causes from Unity console errors and source references
- [x] Replace C# 10-only parameterless struct constructor with C# 9-safe initialization
- [x] Restore `Vastcore.Generation` asmdef visibility for Assembly-CSharp consumers
- [x] Remove unnecessary `Vastcore.Utilities` using from runtime MapGenerator script
- [ ] Recompile in Unity Editor and confirm all listed errors are gone
- [ ] Archive the change after validation
# Legacy UI Migration Report (Dry Run)

最終更新: 2025-11-25

## 概要

- 対象: アセット/スクリプト内の `NarrativeGen.UI` 参照
- 目的: `Vastcore.UI` への安全な置換と、シーン/Prefab 参照の保全

## スキャン結果（サマリ）

- Scripts scanned: 263
- Scenes scanned: 51
- Prefabs scanned: 5
- C# using UnityEngine.UI: 8
- C# OnGUI occurrences: 24
- C# TMPro references: 10
- C# UI Toolkit references: 1
- Scenes with uGUI markers: 3
- Scenes with UI Toolkit: 0
- Prefabs with uGUI markers: 0
- Prefabs with UI Toolkit: 0

## 詳細ログ

### C# files using UnityEngine.UI (8)
- Assets/Editor/Tools/UIMigration/UIMigrationScannerWindow.cs
- Assets/Scripts/Camera/Cinematic/CinematicCameraController.cs
- Assets/Scripts/Testing/TestManager.cs
- Assets/Scripts/UI/InGameDebugUI.cs
- Assets/Scripts/UI/MenuManager.cs
- Assets/Scripts/UI/ModernUIStyleSystem.cs
- Assets/Scripts/UI/SliderBasedUISystem.cs
- Assets/Scripts/UI/SliderUIElement.cs

### C# files with OnGUI (24)
- Assets/Editor/StructureGenerator/Core/StructureGeneratorWindow.cs
- Assets/Editor/Tests/PrimitiveErrorRecoveryTester.cs
- Assets/Editor/Tools/UIMigration/UIMigrationApplyWindow.cs
- Assets/Editor/Tools/UIMigration/UIMigrationRulesDryRunWindow.cs
- Assets/Editor/Tools/UIMigration/UIMigrationScannerWindow.cs
- Assets/MapGenerator/Scripts/Editor/HeightmapTerrainGeneratorWindow.cs
- Assets/Scripts/Core/VastcoreDebugVisualizer.cs
- Assets/Scripts/Editor/TerrainAssetBrowser.cs
- Assets/Scripts/Editor/TerrainGenerationWindow.cs
- Assets/Scripts/Editor/TerrainTemplateEditor.cs
- Assets/Scripts/Player/AdvancedPlayerController.cs
- Assets/Scripts/Player/EnhancedGrindSystem.cs
- Assets/Scripts/Player/Movement/EnhancedClimbingSystem.cs
- Assets/Scripts/Terrain/GPU/GPUPerformanceMonitor.cs
- Assets/Scripts/Terrain/Map/ClimateTerrainFeedbackTest.cs
- Assets/Scripts/Terrain/Map/LODMemorySystemTest.cs
- Assets/Scripts/Terrain/Map/RuntimeGenerationManagerTest.cs
- Assets/Scripts/Terrain/Map/RuntimeTerrainManagerTest.cs
- Assets/Scripts/Terrain/Map/TerrainTexturingSystemTest.cs
- Assets/Scripts/Terrain/Optimization/PerformanceOptimizationController.cs
- Assets/Scripts/Terrain/PrimitiveTerrainObject.cs
- Assets/Scripts/Testing/PerformanceTestingSystem.cs
- Assets/Scripts/Testing/TestSceneManager.cs
- Assets/Scripts/Utilities/Utils/VastcoreLogger.cs

### C# files referencing TMPro (10)
- Assets/Editor/Tools/UIMigration/UIMigrationScannerWindow.cs
- Assets/Scripts/Terrain/Map/AdvancedTerrainAlgorithmsTest.cs
- Assets/Scripts/UI/InGameDebugUI.cs
- Assets/Scripts/UI/MenuManager.cs
- Assets/Scripts/UI/Menus/TitleScreenManager.cs
- Assets/Scripts/UI/ModernUIStyleSystem.cs
- Assets/Scripts/UI/PerformanceMonitor.cs
- Assets/Scripts/UI/SliderBasedUISystem.cs
- Assets/Scripts/UI/SliderUIElement.cs
- Assets/Scripts/UI/TextClickHandler.cs

### C# files referencing UI Toolkit (1)
- Assets/Editor/Tools/UIMigration/UIMigrationScannerWindow.cs

### Scenes with uGUI markers (3)
- Assets/_Scenes/2d/DestructibleTerrain2d.unity
- Assets/_Scenes/2d/MeshBool2d.unity
- Assets/Scenes/MenuScene.unity

### Scenes with UI Toolkit (UIDocument) (0)
- None

### Prefabs with uGUI markers (0)
- None

### Prefabs with UI Toolkit (UIDocument) (0)
- None

## 次のアクション

- `UIMigrationScanner.cs` の Dry Run 実装
- `MenuManager` の扱い（A3-2）に関する設計判断の反映

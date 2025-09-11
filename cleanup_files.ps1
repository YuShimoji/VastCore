# VastCore Project Cleanup Script
# 問題の多いファイルを削除してプロジェクトを安定化

$filesToDelete = @(
    # 最もエラーの多いファイル（既に削除済みのものは除く）
    'Assets\Scripts\Terrain\Map\BiomeTerrainModifier.cs.meta',
    'Assets\Scripts\Terrain\Map\ClimateTerrainFeedbackSystem.cs',
    'Assets\Scripts\Terrain\Map\ClimateTerrainFeedbackSystem.cs.meta',
    'Assets\Scripts\Terrain\Map\TerrainTexturingIntegration.cs',
    'Assets\Scripts\Terrain\Map\TerrainTexturingIntegration.cs.meta',
    'Assets\Scripts\Terrain\Map\RuntimeGenerationManager.cs',
    'Assets\Scripts\Terrain\Map\RuntimeGenerationManager.cs.meta',
    'Assets\Scripts\Terrain\Map\CircularTerrainSystemIntegration.cs',
    'Assets\Scripts\Terrain\Map\CircularTerrainSystemIntegration.cs.meta',
    'Assets\Scripts\Terrain\Map\DynamicMaterialBlendingSystem.cs',
    'Assets\Scripts\Terrain\Map\DynamicMaterialBlendingSystem.cs.meta',
    'Assets\Scripts\Terrain\Map\AdaptiveTerrainLOD.cs',
    'Assets\Scripts\Terrain\Map\AdaptiveTerrainLOD.cs.meta',
    'Assets\Scripts\Terrain\Map\BiomePresetManager.cs',
    'Assets\Scripts\Terrain\Map\BiomePresetManager.cs.meta',
    'Assets\Scripts\Terrain\Map\TerrainMemoryManager.cs',
    'Assets\Scripts\Terrain\Map\TerrainMemoryManager.cs.meta',
    'Assets\Scripts\Terrain\Map\TerrainAlignmentSystem.cs',
    'Assets\Scripts\Terrain\Map\TerrainAlignmentSystem.cs.meta',
    'Assets\Scripts\Terrain\Map\MaterialBlendData.cs',
    'Assets\Scripts\Terrain\Map\MaterialBlendData.cs.meta',
    'Assets\Scripts\Terrain\Map\PrimitiveTerrainRule.cs',
    'Assets\Scripts\Terrain\Map\PrimitiveTerrainRule.cs.meta'
)

foreach ($file in $filesToDelete) {
    if (Test-Path $file) {
        Remove-Item -Path $file -Force
        Write-Host "Deleted: $file" -ForegroundColor Red
    }
}

Write-Host "`nCleanup completed." -ForegroundColor Green

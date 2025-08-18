using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Testing
{
    /// <summary>
    /// AdvancedTerrainAlgorithmsのテストクラス
    /// 水力浸食、熱浸食、風化システムの動作確認
    /// </summary>
    public class AdvancedTerrainAlgorithmsTest : MonoBehaviour
    {
        [Header("テスト設定")]
        public bool runTestOnStart = true;
        public bool enableDetailedLogging = true;
        
        [Header("地形生成設定")]
        public int testResolution = 256;
        public float testSize = 1000f;
        public float testMaxHeight = 100f;
        
        [Header("浸食テスト設定")]
        public bool testHydraulicErosion = true;
        public bool testThermalErosion = true;
        public bool testWeatheringSystem = true;
        public bool testIntegratedSystem = true;
        public bool testLongTermEvolution = true;
        
        [Header("結果表示")]
        public GameObject terrainDisplayPrefab;
        public Transform displayParent;
        public float displaySpacing = 1200f;
        
        private void Start()
        {
            if (runTestOnStart)
            {
                RunAllTests();
            }
        }
        
        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            Debug.Log("=== AdvancedTerrainAlgorithms テスト開始 ===");
            
            // 基本地形を生成
            var baseParams = MeshGenerator.TerrainGenerationParams.Default();
            baseParams.resolution = testResolution;
            baseParams.size = testSize;
            baseParams.maxHeight = testMaxHeight;
            baseParams.enableErosion = false; // 基本地形では浸食を無効化
            
            float[,] baseHeightmap = MeshGenerator.GenerateHeightmap(baseParams);
            
            if (enableDetailedLogging)
            {
                LogHeightmapStats("Base Terrain", baseHeightmap);
            }
            
            // 各テストを実行
            int displayIndex = 0;
            
            // 基本地形を表示
            DisplayTerrain("Base Terrain", baseHeightmap, baseParams, displayIndex++);
            
            if (testHydraulicErosion)
            {
                TestHydraulicErosion(baseHeightmap, baseParams, displayIndex++);
            }
            
            if (testThermalErosion)
            {
                TestThermalErosion(baseHeightmap, baseParams, displayIndex++);
            }
            
            if (testWeatheringSystem)
            {
                TestWeatheringSystem(baseHeightmap, baseParams, displayIndex++);
            }
            
            if (testIntegratedSystem)
            {
                TestIntegratedSystem(baseHeightmap, baseParams, displayIndex++);
            }
            
            if (testLongTermEvolution)
            {
                TestLongTermEvolution(baseHeightmap, baseParams, displayIndex++);
            }
            
            Debug.Log("=== AdvancedTerrainAlgorithms テスト完了 ===");
        }
        
        private void TestHydraulicErosion(float[,] baseHeightmap, MeshGenerator.TerrainGenerationParams baseParams, int displayIndex)
        {
            Debug.Log("水力浸食テスト開始...");
            
            var hydraulicParams = AdvancedTerrainAlgorithms.HydraulicErosionParams.Default();
            hydraulicParams.iterations = 10000; // テスト用に少なめに設定
            
            float[,] erodedHeightmap = AdvancedTerrainAlgorithms.ApplyHydraulicErosion(
                (float[,])baseHeightmap.Clone(), hydraulicParams);
            
            if (enableDetailedLogging)
            {
                LogHeightmapStats("Hydraulic Erosion", erodedHeightmap);
                LogErosionEffectiveness(baseHeightmap, erodedHeightmap, "Hydraulic");
            }
            
            DisplayTerrain("Hydraulic Erosion", erodedHeightmap, baseParams, displayIndex);
            
            Debug.Log("水力浸食テスト完了");
        }
        
        private void TestThermalErosion(float[,] baseHeightmap, MeshGenerator.TerrainGenerationParams baseParams, int displayIndex)
        {
            Debug.Log("熱浸食テスト開始...");
            
            var thermalParams = AdvancedTerrainAlgorithms.ThermalErosionParams.Default();
            
            float[,] erodedHeightmap = AdvancedTerrainAlgorithms.ApplyThermalErosion(
                (float[,])baseHeightmap.Clone(), thermalParams);
            
            if (enableDetailedLogging)
            {
                LogHeightmapStats("Thermal Erosion", erodedHeightmap);
                LogErosionEffectiveness(baseHeightmap, erodedHeightmap, "Thermal");
            }
            
            DisplayTerrain("Thermal Erosion", erodedHeightmap, baseParams, displayIndex);
            
            Debug.Log("熱浸食テスト完了");
        }
        
        private void TestWeatheringSystem(float[,] baseHeightmap, MeshGenerator.TerrainGenerationParams baseParams, int displayIndex)
        {
            Debug.Log("風化システムテスト開始...");
            
            var thermalParams = AdvancedTerrainAlgorithms.ThermalErosionParams.Default();
            
            float[,] weatheredHeightmap = AdvancedTerrainAlgorithms.ApplyWeatheringSimulation(
                (float[,])baseHeightmap.Clone(), thermalParams);
            
            if (enableDetailedLogging)
            {
                LogHeightmapStats("Weathering System", weatheredHeightmap);
                LogErosionEffectiveness(baseHeightmap, weatheredHeightmap, "Weathering");
            }
            
            DisplayTerrain("Weathering System", weatheredHeightmap, baseParams, displayIndex);
            
            Debug.Log("風化システムテスト完了");
        }
        
        private void TestIntegratedSystem(float[,] baseHeightmap, MeshGenerator.TerrainGenerationParams baseParams, int displayIndex)
        {
            Debug.Log("統合システムテスト開始...");
            
            var hydraulicParams = AdvancedTerrainAlgorithms.HydraulicErosionParams.Default();
            var thermalParams = AdvancedTerrainAlgorithms.ThermalErosionParams.Default();
            var climate = AdvancedTerrainAlgorithms.ClimateConditions.Temperate();
            
            // テスト用にパラメータを調整
            hydraulicParams.iterations = 5000;
            
            float[,] integratedHeightmap = AdvancedTerrainAlgorithms.ApplyIntegratedErosion(
                (float[,])baseHeightmap.Clone(), hydraulicParams, thermalParams, climate);
            
            if (enableDetailedLogging)
            {
                LogHeightmapStats("Integrated System", integratedHeightmap);
                LogErosionEffectiveness(baseHeightmap, integratedHeightmap, "Integrated");
            }
            
            DisplayTerrain("Integrated System", integratedHeightmap, baseParams, displayIndex);
            
            Debug.Log("統合システムテスト完了");
        }
        
        private void TestLongTermEvolution(float[,] baseHeightmap, MeshGenerator.TerrainGenerationParams baseParams, int displayIndex)
        {
            Debug.Log("長期地形変化テスト開始...");
            
            float[,] evolvedHeightmap = AdvancedTerrainAlgorithms.ApplyLongTermTerrainEvolution(
                (float[,])baseHeightmap.Clone(), 5); // 5ステップで実行
            
            if (enableDetailedLogging)
            {
                LogHeightmapStats("Long Term Evolution", evolvedHeightmap);
                LogErosionEffectiveness(baseHeightmap, evolvedHeightmap, "Long Term");
            }
            
            DisplayTerrain("Long Term Evolution", evolvedHeightmap, baseParams, displayIndex);
            
            Debug.Log("長期地形変化テスト完了");
        }
        
        private void LogHeightmapStats(string name, float[,] heightmap)
        {
            var stats = MeshGenerator.GetTerrainStats(heightmap);
            Debug.Log($"{name} Stats - Min: {stats.minHeight:F3}, Max: {stats.maxHeight:F3}, Avg: {stats.averageHeight:F3}");
        }
        
        private void LogErosionEffectiveness(float[,] before, float[,] after, string erosionType)
        {
            int resolution = before.GetLength(0);
            float totalChange = 0f;
            float maxChange = 0f;
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float change = Mathf.Abs(after[y, x] - before[y, x]);
                    totalChange += change;
                    maxChange = Mathf.Max(maxChange, change);
                }
            }
            
            float averageChange = totalChange / (resolution * resolution);
            Debug.Log($"{erosionType} Erosion Effectiveness - Avg Change: {averageChange:F4}, Max Change: {maxChange:F4}");
        }
        
        private void DisplayTerrain(string name, float[,] heightmap, MeshGenerator.TerrainGenerationParams parameters, int index)
        {
            if (terrainDisplayPrefab == null || displayParent == null)
                return;
            
            // メッシュを生成
            Mesh terrainMesh = MeshGenerator.GenerateMeshFromHeightmap(heightmap, parameters);
            
            // 表示オブジェクトを作成
            GameObject displayObject = Instantiate(terrainDisplayPrefab, displayParent);
            displayObject.name = name;
            
            // 位置を設定
            Vector3 position = new Vector3(index * displaySpacing, 0, 0);
            displayObject.transform.position = position;
            
            // メッシュを設定
            MeshFilter meshFilter = displayObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.mesh = terrainMesh;
            }
            
            // ラベルを追加（TextMeshProがある場合）
            var textComponent = displayObject.GetComponentInChildren<TMPro.TextMeshPro>();
            if (textComponent != null)
            {
                textComponent.text = name;
            }
            
            Debug.Log($"地形表示作成: {name} at position {position}");
        }
        
        [ContextMenu("Clear Display")]
        public void ClearDisplay()
        {
            if (displayParent != null)
            {
                for (int i = displayParent.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(displayParent.GetChild(i).gameObject);
                }
            }
        }
        
        [ContextMenu("Test Performance")]
        public void TestPerformance()
        {
            Debug.Log("=== パフォーマンステスト開始 ===");
            
            var baseParams = MeshGenerator.TerrainGenerationParams.Default();
            baseParams.resolution = 512; // 高解像度でテスト
            
            float[,] baseHeightmap = MeshGenerator.GenerateHeightmap(baseParams);
            
            // 水力浸食のパフォーマンステスト
            var hydraulicParams = AdvancedTerrainAlgorithms.HydraulicErosionParams.Default();
            
            float startTime = Time.realtimeSinceStartup;
            AdvancedTerrainAlgorithms.ApplyHydraulicErosion((float[,])baseHeightmap.Clone(), hydraulicParams);
            float hydraulicTime = Time.realtimeSinceStartup - startTime;
            
            // 熱浸食のパフォーマンステスト
            var thermalParams = AdvancedTerrainAlgorithms.ThermalErosionParams.Default();
            
            startTime = Time.realtimeSinceStartup;
            AdvancedTerrainAlgorithms.ApplyThermalErosion((float[,])baseHeightmap.Clone(), thermalParams);
            float thermalTime = Time.realtimeSinceStartup - startTime;
            
            Debug.Log($"水力浸食処理時間: {hydraulicTime:F3}秒");
            Debug.Log($"熱浸食処理時間: {thermalTime:F3}秒");
            
            Debug.Log("=== パフォーマンステスト完了 ===");
        }
    }
}
using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework;
using Vastcore.Generation;
using Vastcore.Core;
namespace Vastcore.Generation.Map
{
    /// <summary>
    /// 鬮伜ｺｦ縺ｪLOD繧ｷ繧ｹ繝・Β縺ｮ繝・せ繝医け繝ｩ繧ｹ
    /// 蝨ｰ蠖｢LOD縺ｨ繝励Μ繝溘ユ繧｣繝豊OD縺ｮ邨ｱ蜷医ユ繧ｹ繝・
    /// </summary>
    public class AdvancedLODSystemTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runTestsOnStart = false;
        [SerializeField] private bool enablePerformanceTest = true;
        [SerializeField] private int testObjectCount = 100;
        [SerializeField] private float testDuration = 10f;
        
        [Header("Test Results")]
        [SerializeField] private bool testsCompleted = false;
        [SerializeField] private string testResults = "";
        
        private AdaptiveTerrainLOD terrainLODSystem;
        private AdvancedPrimitiveLODSystem primitiveLODSystem;
        private List<GameObject> testObjects;
        private Transform testPlayer;
        
        private void Start()
        {
            if (runTestsOnStart)
            {
                StartCoroutine(RunAllTests());
            }
        }
        
        /// <summary>
        /// 蜈ｨ繝・せ繝医ｒ螳溯｡・
        /// </summary>
        public System.Collections.IEnumerator RunAllTests()
        {
            Debug.Log("Starting advanced LOD system tests...");
            
            SetupTestEnvironment();
            
            yield return StartCoroutine(TestTerrainLODSystem());
            yield return StartCoroutine(TestPrimitiveLODSystem());
            yield return StartCoroutine(TestLODIntegration());
            
            if (enablePerformanceTest)
            {
                yield return StartCoroutine(TestPerformance());
            }
            
            CleanupTestEnvironment();
            
            testsCompleted = true;
            Debug.Log($"LOD繧ｷ繧ｹ繝・Β繝・せ繝亥ｮ御ｺ・ {testResults}");
        }
        
        /// <summary>
        /// 繝・せ繝育腸蠅・ｒ繧ｻ繝・ヨ繧｢繝・・
        /// </summary>
        private void SetupTestEnvironment()
        {
            // 繝・せ繝医・繝ｬ繧､繝､繝ｼ繧剃ｽ懈・
            var playerObject = new GameObject("TestPlayer");
            testPlayer = playerObject.transform;
            testPlayer.position = Vector3.zero;
            
            // 繧ｫ繝｡繝ｩ繧定ｿｽ蜉
            var camera = playerObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            
            // LOD繧ｷ繧ｹ繝・Β繧剃ｽ懈・
            var lodSystemObject = new GameObject("LODSystem");
            terrainLODSystem = lodSystemObject.AddComponent<AdaptiveTerrainLOD>();
            primitiveLODSystem = lodSystemObject.AddComponent<AdvancedPrimitiveLODSystem>();
            
            // 繝・せ繝医が繝悶ず繧ｧ繧ｯ繝医Μ繧ｹ繝医ｒ蛻晄悄蛹・
            testObjects = new List<GameObject>();
            
            Debug.Log("Test environment setup complete.");
        }
        
        /// <summary>
        /// 蝨ｰ蠖｢LOD繧ｷ繧ｹ繝・Β縺ｮ繝・せ繝・
        /// </summary>
        private System.Collections.IEnumerator TestTerrainLODSystem()
        {
            Debug.Log("Starting terrain LOD system test...");
            
            // 繝・せ繝亥慍蠖｢繧ｿ繧､繝ｫ繧剃ｽ懈・
            var testTiles = CreateTestTerrainTiles(10);
            
            // LOD繧ｷ繧ｹ繝・Β縺ｫ繝励Ξ繧､繝､繝ｼ繧定ｨｭ螳・
            terrainLODSystem.SetPlayerTransform(testPlayer);
            terrainLODSystem.StartLODSystem();
            
            // 蝨ｰ蠖｢繧ｿ繧､繝ｫ繧堤匳骭ｲ
            foreach (var tile in testTiles)
            {
                terrainLODSystem.RegisterTerrainTile(tile);
            }
            
            // 繝励Ξ繧､繝､繝ｼ繧堤ｧｻ蜍輔＆縺帙※LOD螟牙喧繧偵ユ繧ｹ繝・
            for (int i = 0; i < 5; i++)
            {
                testPlayer.position = new Vector3(i * 500f, 0, 0);
                yield return new WaitForSeconds(1f);
                
                // LOD邨ｱ險医ｒ遒ｺ隱・
                var stats = terrainLODSystem.GetLODStatistics();
                Debug.Log($"蝨ｰ蠖｢LOD邨ｱ險・(霍晞屬{i * 500f}m): {string.Join(", ", stats)}");
            }
            
            // 繧ｯ繝ｪ繝ｼ繝ｳ繧｢繝・・
            foreach (var tile in testTiles)
            {
                terrainLODSystem.UnregisterTerrainTile(tile);
                if (tile.terrainObject != null)
                {
                    DestroyImmediate(tile.terrainObject);
                }
            }
            
            terrainLODSystem.StopLODSystem();
            testResults += "蝨ｰ蠖｢LOD繝・せ繝・ 謌仙粥\n";
            
            Debug.Log("Terrain LOD system test completed.");
        }
        
        /// <summary>
        /// 繝励Μ繝溘ユ繧｣繝豊OD繧ｷ繧ｹ繝・Β縺ｮ繝・せ繝・
        /// </summary>
        private System.Collections.IEnumerator TestPrimitiveLODSystem()
        {
            Debug.Log("Starting primitive LOD system test...");
            
            // 繝・せ繝医・繝ｪ繝溘ユ繧｣繝悶が繝悶ず繧ｧ繧ｯ繝医ｒ菴懈・
            var testPrimitives = CreateTestPrimitiveObjects(20);
            
            // LOD繧ｷ繧ｹ繝・Β繧帝幕蟋・
            primitiveLODSystem.StartLODSystem();
            
            // 繝励Μ繝溘ユ繧｣繝悶が繝悶ず繧ｧ繧ｯ繝医ｒ逋ｻ骭ｲ
            foreach (var primitive in testPrimitives)
            {
                primitiveLODSystem.RegisterPrimitiveObject(primitive);
            }
            
            // 繝励Ξ繧､繝､繝ｼ繧堤ｧｻ蜍輔＆縺帙※LOD螟牙喧繧偵ユ繧ｹ繝・
            for (int i = 0; i < 8; i++)
            {
                testPlayer.position = new Vector3(i * 300f, 0, 0);
                yield return new WaitForSeconds(1f);
                
                // LOD邨ｱ險医ｒ遒ｺ隱・
                var stats = primitiveLODSystem.GetSystemStatistics();
                Debug.Log($"繝励Μ繝溘ユ繧｣繝豊OD邨ｱ險・(霍晞屬{i * 300f}m): " +
                         $"邱乗焚:{stats.totalObjects}, 蜿ｯ隕・{stats.visibleObjects}, " +
                         $"繧､繝ｳ繝昴せ繧ｿ繝ｼ:{stats.impostorObjects}, 蜩∬ｳｪ:{stats.qualityMultiplier:F2}");
            }
            
            // 繧､繝ｳ繝昴せ繧ｿ繝ｼ繧ｷ繧ｹ繝・Β縺ｮ繝・せ繝・
            testPlayer.position = new Vector3(2000f, 0, 0); // 驕霍晞屬
            yield return new WaitForSeconds(2f);
            
            var finalStats = primitiveLODSystem.GetSystemStatistics();
            bool impostorWorking = finalStats.impostorObjects > 0;
            
            // 繧ｯ繝ｪ繝ｼ繝ｳ繧｢繝・・
            foreach (var primitive in testPrimitives)
            {
                primitiveLODSystem.UnregisterPrimitiveObject(primitive);
                if (primitive != null)
                {
                    DestroyImmediate(primitive.gameObject);
                }
            }
            
            primitiveLODSystem.StopLODSystem();
            testResults += $"Primitive LOD test: PASSED (Impostor: {(impostorWorking ? "Enabled" : "Disabled")})\n";
            
            Debug.Log("Primitive LOD system test completed.");
        }
        
        /// <summary>
        /// LOD繧ｷ繧ｹ繝・Β邨ｱ蜷医ユ繧ｹ繝・
        /// </summary>
        private System.Collections.IEnumerator TestLODIntegration()
        {
            Debug.Log("Starting integrated LOD system test...");
            
            // 蝨ｰ蠖｢縺ｨ繝励Μ繝溘ユ繧｣繝悶ｒ蜷梧凾縺ｫ菴懈・
            var testTiles = CreateTestTerrainTiles(5);
            var testPrimitives = CreateTestPrimitiveObjects(10);
            
            // 荳｡繧ｷ繧ｹ繝・Β繧帝幕蟋・
            terrainLODSystem.SetPlayerTransform(testPlayer);
            terrainLODSystem.StartLODSystem();
            primitiveLODSystem.StartLODSystem();
            
            // 繧ｪ繝悶ず繧ｧ繧ｯ繝医ｒ逋ｻ骭ｲ
            foreach (var tile in testTiles)
            {
                terrainLODSystem.RegisterTerrainTile(tile);
            }
            
            foreach (var primitive in testPrimitives)
            {
                primitiveLODSystem.RegisterPrimitiveObject(primitive);
            }
            
            // 邨ｱ蜷亥虚菴懊ユ繧ｹ繝・
            for (int i = 0; i < 5; i++)
            {
                testPlayer.position = new Vector3(i * 400f, 0, 0);
                yield return new WaitForSeconds(1f);
                
                var terrainStats = terrainLODSystem.GetLODStatistics();
                var primitiveStats = primitiveLODSystem.GetSystemStatistics();
                
                Debug.Log($"邨ｱ蜷医ユ繧ｹ繝・(霍晞屬{i * 400f}m): " +
                         $"蝨ｰ蠖｢LOD謨ｰ:{terrainStats.Count}, " +
                         $"繝励Μ繝溘ユ繧｣繝門庄隕・{primitiveStats.visibleObjects}/{primitiveStats.totalObjects}");
            }
            
            // 繧ｯ繝ｪ繝ｼ繝ｳ繧｢繝・・
            foreach (var tile in testTiles)
            {
                terrainLODSystem.UnregisterTerrainTile(tile);
                if (tile.terrainObject != null)
                {
                    DestroyImmediate(tile.terrainObject);
                }
            }
            
            foreach (var primitive in testPrimitives)
            {
                primitiveLODSystem.UnregisterPrimitiveObject(primitive);
                if (primitive != null)
                {
                    DestroyImmediate(primitive.gameObject);
                }
            }
            
            terrainLODSystem.StopLODSystem();
            primitiveLODSystem.StopLODSystem();
            
            testResults += "LOD邨ｱ蜷医ユ繧ｹ繝・ 謌仙粥\n";
            Debug.Log("LOD integration test completed.");
        }
        
        /// <summary>
        /// 繝代ヵ繧ｩ繝ｼ繝槭Φ繧ｹ繝・せ繝・
        /// </summary>
        private System.Collections.IEnumerator TestPerformance()
        {
            Debug.Log("Starting LOD performance test...");
            
            // 螟ｧ驥上・繧ｪ繝悶ず繧ｧ繧ｯ繝医ｒ菴懈・
            var testTiles = CreateTestTerrainTiles(testObjectCount / 2);
            var testPrimitives = CreateTestPrimitiveObjects(testObjectCount / 2);
            
            // 繧ｷ繧ｹ繝・Β繧帝幕蟋・
            terrainLODSystem.SetPlayerTransform(testPlayer);
            terrainLODSystem.StartLODSystem();
            primitiveLODSystem.StartLODSystem();
            
            // 繧ｪ繝悶ず繧ｧ繧ｯ繝医ｒ逋ｻ骭ｲ
            foreach (var tile in testTiles)
            {
                terrainLODSystem.RegisterTerrainTile(tile);
            }
            
            foreach (var primitive in testPrimitives)
            {
                primitiveLODSystem.RegisterPrimitiveObject(primitive);
            }
            
            // 繝代ヵ繧ｩ繝ｼ繝槭Φ繧ｹ貂ｬ螳・
            float startTime = Time.realtimeSinceStartup;
            float totalFrameTime = 0f;
            int frameCount = 0;
            float minFrameRate = float.MaxValue;
            float maxFrameRate = 0f;
            
            float endTime = startTime + testDuration;
            
            while (Time.realtimeSinceStartup < endTime)
            {
                float frameTime = Time.deltaTime;
                float frameRate = 1f / frameTime;
                
                totalFrameTime += frameTime;
                frameCount++;
                
                minFrameRate = Mathf.Min(minFrameRate, frameRate);
                maxFrameRate = Mathf.Max(maxFrameRate, frameRate);
                
                // 繝励Ξ繧､繝､繝ｼ繧堤ｧｻ蜍・
                float t = (Time.realtimeSinceStartup - startTime) / testDuration;
                testPlayer.position = new Vector3(Mathf.Sin(t * Mathf.PI * 2) * 1000f, 0, Mathf.Cos(t * Mathf.PI * 2) * 1000f);
                
                yield return null;
            }
            
            float averageFrameRate = frameCount / totalFrameTime;
            
            // 邨先棡繧定ｨ倬鹸
            testResults += $"繝代ヵ繧ｩ繝ｼ繝槭Φ繧ｹ繝・せ繝・ 蟷ｳ蝮⑦PS:{averageFrameRate:F1}, " +
                          $"譛蟆洲PS:{minFrameRate:F1}, 譛螟ｧFPS:{maxFrameRate:F1}\n";
            
            // 繧ｯ繝ｪ繝ｼ繝ｳ繧｢繝・・
            foreach (var tile in testTiles)
            {
                terrainLODSystem.UnregisterTerrainTile(tile);
                if (tile.terrainObject != null)
                {
                    DestroyImmediate(tile.terrainObject);
                }
            }
            
            foreach (var primitive in testPrimitives)
            {
                primitiveLODSystem.UnregisterPrimitiveObject(primitive);
                if (primitive != null)
                {
                    DestroyImmediate(primitive.gameObject);
                }
            }
            
            terrainLODSystem.StopLODSystem();
            primitiveLODSystem.StopLODSystem();
            
            Debug.Log($"繝代ヵ繧ｩ繝ｼ繝槭Φ繧ｹ繝・せ繝亥ｮ御ｺ・ 蟷ｳ蝮⑦PS {averageFrameRate:F1}");
        }
        
        /// <summary>
        /// 繝・せ繝亥慍蠖｢繧ｿ繧､繝ｫ繧剃ｽ懈・
        /// </summary>
        private List<TerrainTile> CreateTestTerrainTiles(int count)
        {
            var tiles = new List<TerrainTile>();
            
            for (int i = 0; i < count; i++)
            {
                var tileObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
                tileObject.name = $"TestTerrainTile_{i}";
                tileObject.transform.position = new Vector3(i * 200f, 0, 0);
                tileObject.transform.localScale = Vector3.one * 20f;
                
                var tile = new TerrainTile
                {
                    coordinate = new Vector2Int(i, 0),
                    tileObject = tileObject,
                    terrainMesh = tileObject.GetComponent<MeshFilter>().mesh,
                    heightmap = GenerateTestHeightData(64),
                    state = TerrainTile.TileState.Active
                };
                
                tiles.Add(tile);
                testObjects.Add(tileObject);
            }
            
            return tiles;
        }
        
        /// <summary>
        /// 繝・せ繝医・繝ｪ繝溘ユ繧｣繝悶が繝悶ず繧ｧ繧ｯ繝医ｒ菴懈・
        /// </summary>
        private List<PrimitiveTerrainObject> CreateTestPrimitiveObjects(int count)
        {
            var primitives = new List<PrimitiveTerrainObject>();
            
            for (int i = 0; i < count; i++)
            {
                var primitiveObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                primitiveObject.name = $"TestPrimitive_{i}";
                primitiveObject.transform.position = new Vector3(i * 150f, 10f, Random.Range(-100f, 100f));
                primitiveObject.transform.localScale = Vector3.one * Random.Range(5f, 20f);
                
                var primitive = primitiveObject.AddComponent<PrimitiveTerrainObject>();
                primitive.primitiveType = (GenerationPrimitiveType)((PrimitiveTerrainGenerator.PrimitiveType)(i % 4));
                primitive.scale = primitiveObject.transform.localScale;
                primitive.enableLOD = true;
                
                primitives.Add(primitive);
                testObjects.Add(primitiveObject);
            }
            
            return primitives;
        }
        
        /// <summary>
        /// 繝・せ繝育畑鬮倥＆繝・・繧ｿ繧堤函謌・
        /// </summary>
        private float[,] GenerateTestHeightData(int resolution)
        {
            var heightData = new float[resolution, resolution];
            
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    float height = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 10f;
                    heightData[x, y] = height;
                }
            }
            
            return heightData;
        }
        
        /// <summary>
        /// 繝・せ繝育腸蠅・ｒ繧ｯ繝ｪ繝ｼ繝ｳ繧｢繝・・
        /// </summary>
        private void CleanupTestEnvironment()
        {
            // 繝・せ繝医が繝悶ず繧ｧ繧ｯ繝医ｒ蜑企勁
            foreach (var obj in testObjects)
            {
                if (obj != null)
                {
                    DestroyImmediate(obj);
                }
            }
            testObjects.Clear();
            
            // 繝・せ繝医・繝ｬ繧､繝､繝ｼ繧貞炎髯､
            if (testPlayer != null)
            {
                DestroyImmediate(testPlayer.gameObject);
            }
            
            // LOD繧ｷ繧ｹ繝・Β繧貞炎髯､
            if (terrainLODSystem != null)
            {
                DestroyImmediate(terrainLODSystem.gameObject);
            }
            
            Debug.Log("Test environment cleanup complete.");
        }
        
        /// <summary>
        /// 謇句虚縺ｧ繝・せ繝医ｒ螳溯｡・
        /// </summary>
        [ContextMenu("Run LOD System Tests")]
        public void RunTestsManually()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Run this test only in play mode.");
                return;
            }
            
            StartCoroutine(RunAllTests());
        }
        
        /// <summary>
        /// 繝・せ繝育ｵ先棡繧偵Ο繧ｰ縺ｫ蜃ｺ蜉・
        /// </summary>
        [ContextMenu("Log Test Results")]
        public void LogTestResults()
        {
            if (testsCompleted)
            {
                Debug.Log($"LOD繧ｷ繧ｹ繝・Β繝・せ繝育ｵ先棡:\n{testResults}");
            }
            else
            {
                Debug.Log("繝・せ繝医′縺ｾ縺螳御ｺ・＠縺ｦ縺・∪縺帙ｓ");
            }
        }
    }
}

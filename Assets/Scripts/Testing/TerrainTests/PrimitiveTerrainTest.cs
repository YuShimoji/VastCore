using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// プリミティブ地形生成システムのテストスクリプト
    /// </summary>
    public class PrimitiveTerrainTest : MonoBehaviour
    {
        [Header("テスト設定")]
        [SerializeField] private bool runTestOnStart = true;
        [SerializeField] private PrimitiveTerrainGenerator.PrimitiveType testPrimitiveType = PrimitiveTerrainGenerator.PrimitiveType.Cube;
        [SerializeField] private Vector3 testPosition = Vector3.zero;
        [SerializeField] private Vector3 testScale = new Vector3(100f, 100f, 100f);
        
        [Header("生成テスト")]
        [SerializeField] private bool testAllPrimitives = false;
        [SerializeField] private float spacingBetweenPrimitives = 200f;

        void Start()
        {
            if (runTestOnStart)
            {
                if (testAllPrimitives)
                {
                    TestAllPrimitiveTypes();
                }
                else
                {
                    TestSinglePrimitive();
                }
            }
        }

        /// <summary>
        /// 単一プリミティブのテスト
        /// </summary>
        [ContextMenu("Test Single Primitive")]
        public void TestSinglePrimitive()
        {
            Debug.Log($"Testing primitive generation: {testPrimitiveType}");
            
            var parameters = PrimitiveTerrainGenerator.PrimitiveGenerationParams.Default(testPrimitiveType);
            parameters.position = testPosition;
            parameters.scale = testScale;
            
            GameObject result = PrimitiveTerrainGenerator.GeneratePrimitiveTerrain(parameters);
            
            if (result != null)
            {
                Debug.Log($"Successfully generated {testPrimitiveType} at {testPosition}");
            }
            else
            {
                Debug.LogError($"Failed to generate {testPrimitiveType}");
            }
        }

        /// <summary>
        /// すべてのプリミティブタイプをテスト
        /// </summary>
        [ContextMenu("Test All Primitives")]
        public void TestAllPrimitiveTypes()
        {
            Debug.Log("Testing all primitive types...");
            
            var primitiveTypes = System.Enum.GetValues(typeof(PrimitiveTerrainGenerator.PrimitiveType));
            int successCount = 0;
            int totalCount = primitiveTypes.Length;
            
            for (int i = 0; i < totalCount; i++)
            {
                var primitiveType = (PrimitiveTerrainGenerator.PrimitiveType)primitiveTypes.GetValue(i);
                
                // 配置位置を計算（グリッド状に配置）
                int gridSize = Mathf.CeilToInt(Mathf.Sqrt(totalCount));
                int x = i % gridSize;
                int z = i / gridSize;
                Vector3 position = testPosition + new Vector3(x * spacingBetweenPrimitives, 0f, z * spacingBetweenPrimitives);
                
                // パラメータを設定
                var parameters = PrimitiveTerrainGenerator.PrimitiveGenerationParams.Default(primitiveType);
                parameters.position = position;
                parameters.scale = PrimitiveTerrainGenerator.GetDefaultScale(primitiveType);
                
                // 生成を試行
                GameObject result = PrimitiveTerrainGenerator.GeneratePrimitiveTerrain(parameters);
                
                if (result != null)
                {
                    result.name = $"Test_{primitiveType}";
                    successCount++;
                    Debug.Log($"✓ Generated {primitiveType} at {position}");
                }
                else
                {
                    Debug.LogError($"✗ Failed to generate {primitiveType}");
                }
            }
            
            Debug.Log($"Primitive generation test completed: {successCount}/{totalCount} successful");
        }

        /// <summary>
        /// 地形整列システムのテスト
        /// </summary>
        [ContextMenu("Test Terrain Alignment")]
        public void TestTerrainAlignment()
        {
            Debug.Log("Testing terrain alignment system...");
            
            // テスト用の地形法線（45度傾斜）
            Vector3 testNormal = new Vector3(0.707f, 0.707f, 0f).normalized;
            
            // プリミティブを生成
            var parameters = PrimitiveTerrainGenerator.PrimitiveGenerationParams.Default(testPrimitiveType);
            parameters.position = testPosition;
            parameters.scale = testScale;
            
            GameObject primitive = PrimitiveTerrainGenerator.GeneratePrimitiveTerrain(parameters);
            
            if (primitive != null)
            {
                // 整列設定
                var alignmentSettings = TerrainAlignmentSystem.AlignmentSettings.Default();
                
                // 地形に整列
                TerrainAlignmentSystem.AlignPrimitiveToTerrain(primitive, testNormal, alignmentSettings);
                
                Debug.Log($"Applied terrain alignment with normal {testNormal}");
            }
        }

        /// <summary>
        /// 配置検証システムのテスト
        /// </summary>
        [ContextMenu("Test Placement Validation")]
        public void TestPlacementValidation()
        {
            Debug.Log("Testing placement validation system...");
            
            var alignmentSettings = TerrainAlignmentSystem.AlignmentSettings.Default();
            
            // 複数の位置をテスト
            Vector3[] testPositions = {
                testPosition,
                testPosition + Vector3.right * 100f,
                testPosition + Vector3.forward * 100f,
                testPosition + Vector3.up * 1000f // 高すぎる位置
            };
            
            foreach (var position in testPositions)
            {
                bool isValid = TerrainAlignmentSystem.IsValidPlacementPosition(position, 50f, alignmentSettings);
                Debug.Log($"Position {position} is {(isValid ? "valid" : "invalid")} for placement");
            }
        }

        /// <summary>
        /// パフォーマンステスト
        /// </summary>
        [ContextMenu("Performance Test")]
        public void PerformanceTest()
        {
            Debug.Log("Running performance test...");
            
            int testCount = 10;
            float startTime = Time.realtimeSinceStartup;
            
            for (int i = 0; i < testCount; i++)
            {
                var parameters = PrimitiveTerrainGenerator.PrimitiveGenerationParams.Default(testPrimitiveType);
                parameters.position = testPosition + Random.insideUnitSphere * 100f;
                parameters.scale = testScale;
                
                GameObject result = PrimitiveTerrainGenerator.GeneratePrimitiveTerrain(parameters);
                if (result != null)
                {
                    // テスト後すぐに削除
                    DestroyImmediate(result);
                }
            }
            
            float endTime = Time.realtimeSinceStartup;
            float totalTime = endTime - startTime;
            float averageTime = totalTime / testCount;
            
            Debug.Log($"Performance test completed: {testCount} primitives generated in {totalTime:F3}s (avg: {averageTime:F3}s per primitive)");
        }

        /// <summary>
        /// すべてのテストプリミティブを削除
        /// </summary>
        [ContextMenu("Clear Test Primitives")]
        public void ClearTestPrimitives()
        {
            GameObject[] testObjects = GameObject.FindGameObjectsWithTag("Untagged");
            int removedCount = 0;
            
            foreach (var obj in testObjects)
            {
                if (obj.name.StartsWith("Primitive_") || obj.name.StartsWith("Test_"))
                {
                    DestroyImmediate(obj);
                    removedCount++;
                }
            }
            
            Debug.Log($"Cleared {removedCount} test primitives");
        }
    }
}
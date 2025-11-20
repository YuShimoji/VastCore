using System.Collections;
using UnityEngine;
using Vastcore.Generation;
using GeneratorPrimitiveType = Vastcore.Generation.PrimitiveTerrainGenerator.PrimitiveType;

namespace VastCore.Testing
{
    /// <summary>
    /// プリミティブ品質テストスイート
    /// </summary>
    public class PrimitiveTestSuite : BaseTestSuite
    {
        [SerializeField] private int primitiveTestIterations = 5;
        [SerializeField] private float primitiveQualityThreshold = 0.8f;

        void Start()
        {
            InitializeTestSuite();
        }

        public override IEnumerator RunTests()
        {
            testStartTime = System.DateTime.Now;
            LogMessage($"Starting primitive quality tests (threshold: {primitiveQualityThreshold})...");

            // 全16種類のプリミティブをテスト
            GeneratorPrimitiveType[] allPrimitives = (GeneratorPrimitiveType[])System.Enum.GetValues(typeof(GeneratorPrimitiveType));

            foreach (var primitiveType in allPrimitives)
            {
                yield return StartCoroutine(TestPrimitiveQuality(primitiveType));
            }

            LogMessage($"Primitive tests completed. Tested {allPrimitives.Length} primitive types.");
        }

        private IEnumerator TestPrimitiveQuality(GeneratorPrimitiveType primitiveType)
        {
            float startTime = Time.realtimeSinceStartup;

            for (int i = 0; i < primitiveTestIterations; i++)
            {
                var parameters = Vastcore.Generation.PrimitiveTerrainGenerator.PrimitiveGenerationParams.Default(primitiveType);
                parameters.position = Random.insideUnitSphere * 100f;
                parameters.scale = Vastcore.Generation.PrimitiveTerrainGenerator.GetDefaultScale(primitiveType) * Random.Range(0.8f, 1.2f);

                var primitiveObject = Vastcore.Generation.PrimitiveTerrainGenerator.GeneratePrimitiveTerrain(parameters);

                if (primitiveObject != null)
                {
                    // 品質検証
                    var meshFilter = primitiveObject.GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        var mesh = meshFilter.sharedMesh;
                        bool hasValidMesh = mesh.vertices.Length > 0 && mesh.triangles.Length > 0;

                        if (hasValidMesh)
                        {
                            // 追加の品質チェック
                            bool hasReasonableVertexCount = mesh.vertices.Length >= 8; // 最低8頂点
                            bool hasReasonableTriangleCount = mesh.triangles.Length >= 12; // 最低12三角形

                            if (hasReasonableVertexCount && hasReasonableTriangleCount)
                            {
                                AddTestResult(CreateTestResult(
                                    $"{primitiveType}_Iteration_{i}",
                                    true,
                                    $"Quality check passed for {primitiveType}",
                                    Time.realtimeSinceStartup - startTime
                                ));
                            }
                            else
                            {
                                AddTestResult(CreateTestResult(
                                    $"{primitiveType}_Iteration_{i}",
                                    false,
                                    $"Insufficient geometry: vertices={mesh.vertices.Length}, triangles={mesh.triangles.Length}",
                                    Time.realtimeSinceStartup - startTime
                                ));
                            }
                        }
                        else
                        {
                            AddTestResult(CreateTestResult(
                                $"{primitiveType}_Iteration_{i}",
                                false,
                                $"Invalid mesh generated for {primitiveType}",
                                Time.realtimeSinceStartup - startTime
                            ));
                        }
                    }
                    else
                    {
                        AddTestResult(CreateTestResult(
                            $"{primitiveType}_Iteration_{i}",
                            false,
                            $"No mesh filter found for {primitiveType}",
                            Time.realtimeSinceStartup - startTime
                        ));
                    }

                    // テストオブジェクトをクリーンアップ
                    Object.Destroy(primitiveObject);
                }
                else
                {
                    AddTestResult(CreateTestResult(
                        $"{primitiveType}_Iteration_{i}",
                        false,
                        $"Failed to generate {primitiveType}",
                        Time.realtimeSinceStartup - startTime
                    ));
                }

                yield return null; // フレームをスキップ
            }
        }
    }
}

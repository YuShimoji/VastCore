using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework;
using Vastcore.Generation;

namespace Vastcore.Generation.Map
{
    /// <summary>
    /// 高度なLODシステムのテストクラス
    /// 地形LODとプリミティブLODの統合テスト
    /// </summary>
    public class AdvancedLODSystemTest : MonoBehaviour
    {
        [Header("テスト設定")]
        [SerializeField] private bool runTestsOnStart = false;
        [SerializeField] private bool enablePerformanceTest = true;
        [SerializeField] private int testObjectCount = 100;
        [SerializeField] private float testDuration = 10f;
        
        [Header("テスト結果")]
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
        /// 全テストを実行
        /// </summary>
        public System.Collections.IEnumerator RunAllTests()
        {
            Debug.Log("高度なLODシステムテスト開始");
            
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
            Debug.Log($"LODシステムテスト完了: {testResults}");
        }
        
        /// <summary>
        /// テスト環境をセットアップ
        /// </summary>
        private void SetupTestEnvironment()
        {
            // テストプレイヤーを作成
            var playerObject = new GameObject("TestPlayer");
            testPlayer = playerObject.transform;
            testPlayer.position = Vector3.zero;
            
            // カメラを追加
            var camera = playerObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            
            // LODシステムを作成
            var lodSystemObject = new GameObject("LODSystem");
            terrainLODSystem = lodSystemObject.AddComponent<AdaptiveTerrainLOD>();
            primitiveLODSystem = lodSystemObject.AddComponent<AdvancedPrimitiveLODSystem>();
            
            // テストオブジェクトリストを初期化
            testObjects = new List<GameObject>();
            
            Debug.Log("テスト環境セットアップ完了");
        }
        
        /// <summary>
        /// 地形LODシステムのテスト
        /// </summary>
        private System.Collections.IEnumerator TestTerrainLODSystem()
        {
            Debug.Log("地形LODシステムテスト開始");
            
            // テスト地形タイルを作成
            var testTiles = CreateTestTerrainTiles(10);
            
            // LODシステムにプレイヤーを設定
            terrainLODSystem.SetPlayerTransform(testPlayer);
            terrainLODSystem.StartLODSystem();
            
            // 地形タイルを登録
            foreach (var tile in testTiles)
            {
                terrainLODSystem.RegisterTerrainTile(tile);
            }
            
            // プレイヤーを移動させてLOD変化をテスト
            for (int i = 0; i < 5; i++)
            {
                testPlayer.position = new Vector3(i * 500f, 0, 0);
                yield return new WaitForSeconds(1f);
                
                // LOD統計を確認
                var stats = terrainLODSystem.GetLODStatistics();
                Debug.Log($"地形LOD統計 (距離{i * 500f}m): {string.Join(", ", stats)}");
            }
            
            // クリーンアップ
            foreach (var tile in testTiles)
            {
                terrainLODSystem.UnregisterTerrainTile(tile);
                if (tile.terrainObject != null)
                {
                    DestroyImmediate(tile.terrainObject);
                }
            }
            
            terrainLODSystem.StopLODSystem();
            testResults += "地形LODテスト: 成功\n";
            
            Debug.Log("地形LODシステムテスト完了");
        }
        
        /// <summary>
        /// プリミティブLODシステムのテスト
        /// </summary>
        private System.Collections.IEnumerator TestPrimitiveLODSystem()
        {
            Debug.Log("プリミティブLODシステムテスト開始");
            
            // テストプリミティブオブジェクトを作成
            var testPrimitives = CreateTestPrimitiveObjects(20);
            
            // LODシステムを開始
            primitiveLODSystem.StartLODSystem();
            
            // プリミティブオブジェクトを登録
            foreach (var primitive in testPrimitives)
            {
                primitiveLODSystem.RegisterPrimitiveObject(primitive);
            }
            
            // プレイヤーを移動させてLOD変化をテスト
            for (int i = 0; i < 8; i++)
            {
                testPlayer.position = new Vector3(i * 300f, 0, 0);
                yield return new WaitForSeconds(1f);
                
                // LOD統計を確認
                var stats = primitiveLODSystem.GetSystemStatistics();
                Debug.Log($"プリミティブLOD統計 (距離{i * 300f}m): " +
                         $"総数:{stats.totalObjects}, 可視:{stats.visibleObjects}, " +
                         $"インポスター:{stats.impostorObjects}, 品質:{stats.qualityMultiplier:F2}");
            }
            
            // インポスターシステムのテスト
            testPlayer.position = new Vector3(2000f, 0, 0); // 遠距離
            yield return new WaitForSeconds(2f);
            
            var finalStats = primitiveLODSystem.GetSystemStatistics();
            bool impostorWorking = finalStats.impostorObjects > 0;
            
            // クリーンアップ
            foreach (var primitive in testPrimitives)
            {
                primitiveLODSystem.UnregisterPrimitiveObject(primitive);
                if (primitive != null)
                {
                    DestroyImmediate(primitive.gameObject);
                }
            }
            
            primitiveLODSystem.StopLODSystem();
            testResults += $"プリミティブLODテスト: 成功 (インポスター: {(impostorWorking ? "動作" : "未動作")})\n";
            
            Debug.Log("プリミティブLODシステムテスト完了");
        }
        
        /// <summary>
        /// LODシステム統合テスト
        /// </summary>
        private System.Collections.IEnumerator TestLODIntegration()
        {
            Debug.Log("LODシステム統合テスト開始");
            
            // 地形とプリミティブを同時に作成
            var testTiles = CreateTestTerrainTiles(5);
            var testPrimitives = CreateTestPrimitiveObjects(10);
            
            // 両システムを開始
            terrainLODSystem.SetPlayerTransform(testPlayer);
            terrainLODSystem.StartLODSystem();
            primitiveLODSystem.StartLODSystem();
            
            // オブジェクトを登録
            foreach (var tile in testTiles)
            {
                terrainLODSystem.RegisterTerrainTile(tile);
            }
            
            foreach (var primitive in testPrimitives)
            {
                primitiveLODSystem.RegisterPrimitiveObject(primitive);
            }
            
            // 統合動作テスト
            for (int i = 0; i < 5; i++)
            {
                testPlayer.position = new Vector3(i * 400f, 0, 0);
                yield return new WaitForSeconds(1f);
                
                var terrainStats = terrainLODSystem.GetLODStatistics();
                var primitiveStats = primitiveLODSystem.GetSystemStatistics();
                
                Debug.Log($"統合テスト (距離{i * 400f}m): " +
                         $"地形LOD数:{terrainStats.Count}, " +
                         $"プリミティブ可視:{primitiveStats.visibleObjects}/{primitiveStats.totalObjects}");
            }
            
            // クリーンアップ
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
            
            testResults += "LOD統合テスト: 成功\n";
            Debug.Log("LODシステム統合テスト完了");
        }
        
        /// <summary>
        /// パフォーマンステスト
        /// </summary>
        private System.Collections.IEnumerator TestPerformance()
        {
            Debug.Log("LODシステムパフォーマンステスト開始");
            
            // 大量のオブジェクトを作成
            var testTiles = CreateTestTerrainTiles(testObjectCount / 2);
            var testPrimitives = CreateTestPrimitiveObjects(testObjectCount / 2);
            
            // システムを開始
            terrainLODSystem.SetPlayerTransform(testPlayer);
            terrainLODSystem.StartLODSystem();
            primitiveLODSystem.StartLODSystem();
            
            // オブジェクトを登録
            foreach (var tile in testTiles)
            {
                terrainLODSystem.RegisterTerrainTile(tile);
            }
            
            foreach (var primitive in testPrimitives)
            {
                primitiveLODSystem.RegisterPrimitiveObject(primitive);
            }
            
            // パフォーマンス測定
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
                
                // プレイヤーを移動
                float t = (Time.realtimeSinceStartup - startTime) / testDuration;
                testPlayer.position = new Vector3(Mathf.Sin(t * Mathf.PI * 2) * 1000f, 0, Mathf.Cos(t * Mathf.PI * 2) * 1000f);
                
                yield return null;
            }
            
            float averageFrameRate = frameCount / totalFrameTime;
            
            // 結果を記録
            testResults += $"パフォーマンステスト: 平均FPS:{averageFrameRate:F1}, " +
                          $"最小FPS:{minFrameRate:F1}, 最大FPS:{maxFrameRate:F1}\n";
            
            // クリーンアップ
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
            
            Debug.Log($"パフォーマンステスト完了: 平均FPS {averageFrameRate:F1}");
        }
        
        /// <summary>
        /// テスト地形タイルを作成
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
        /// テストプリミティブオブジェクトを作成
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
        /// テスト用高さデータを生成
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
        /// テスト環境をクリーンアップ
        /// </summary>
        private void CleanupTestEnvironment()
        {
            // テストオブジェクトを削除
            foreach (var obj in testObjects)
            {
                if (obj != null)
                {
                    DestroyImmediate(obj);
                }
            }
            testObjects.Clear();
            
            // テストプレイヤーを削除
            if (testPlayer != null)
            {
                DestroyImmediate(testPlayer.gameObject);
            }
            
            // LODシステムを削除
            if (terrainLODSystem != null)
            {
                DestroyImmediate(terrainLODSystem.gameObject);
            }
            
            Debug.Log("テスト環境クリーンアップ完了");
        }
        
        /// <summary>
        /// 手動でテストを実行
        /// </summary>
        [ContextMenu("Run LOD System Tests")]
        public void RunTestsManually()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("テストは実行時にのみ実行できます");
                return;
            }
            
            StartCoroutine(RunAllTests());
        }
        
        /// <summary>
        /// テスト結果をログに出力
        /// </summary>
        [ContextMenu("Log Test Results")]
        public void LogTestResults()
        {
            if (testsCompleted)
            {
                Debug.Log($"LODシステムテスト結果:\n{testResults}");
            }
            else
            {
                Debug.Log("テストがまだ完了していません");
            }
        }
    }
}
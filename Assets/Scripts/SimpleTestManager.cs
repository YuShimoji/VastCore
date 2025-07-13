using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Testing
{
    /// <summary>
    /// 高度地形生成システムのテストマネージャー
    /// </summary>
    public class SimpleTestManager : MonoBehaviour
    {
        [Header("テスト設定")]
        [SerializeField] private bool autoGenerate = false;
        [SerializeField] private bool generateOnStart = true;
        
        [Header("地形生成パラメータ")]
        [SerializeField] private MeshGenerator.TerrainGenerationParams terrainParams = MeshGenerator.TerrainGenerationParams.Default();
        
        [Header("プレビュー設定")]
        [SerializeField] private Material terrainMaterial;
        [SerializeField] private bool showWireframe = false;
        
        [Header("生成された地形情報")]
        [SerializeField] private MeshGenerator.TerrainStats lastGeneratedStats;
        
        private GameObject generatedTerrain;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;

        #region Unity生命周期
        private void Start()
        {
            if (generateOnStart)
            {
                GenerateTerrain();
            }
        }

        private void Update()
        {
            if (autoGenerate)
            {
                GenerateTerrain();
            }
        }
        #endregion

        #region 地形生成
        /// <summary>
        /// 地形を生成
        /// </summary>
        [ContextMenu("Generate Terrain")]
        public void GenerateTerrain()
        {
            // 既存の地形を削除
            if (generatedTerrain != null)
            {
                DestroyImmediate(generatedTerrain);
            }

            // 新しい地形オブジェクトを作成
            generatedTerrain = new GameObject("Generated Terrain");
            generatedTerrain.transform.SetParent(transform);

            // 必要なコンポーネントを追加
            meshFilter = generatedTerrain.AddComponent<MeshFilter>();
            meshRenderer = generatedTerrain.AddComponent<MeshRenderer>();
            meshCollider = generatedTerrain.AddComponent<MeshCollider>();

            // 地形メッシュを生成
            Mesh terrainMesh = MeshGenerator.GenerateAdvancedTerrain(terrainParams);
            meshFilter.mesh = terrainMesh;
            meshCollider.sharedMesh = terrainMesh;

            // マテリアルを適用
            if (terrainMaterial != null)
            {
                meshRenderer.material = terrainMaterial;
            }
            else
            {
                // デフォルトマテリアルを作成
                meshRenderer.material = CreateDefaultTerrainMaterial();
            }

            // ワイヤーフレーム表示設定
            if (showWireframe)
            {
                meshRenderer.material.SetFloat("_WireframeWidth", 0.1f);
            }

            // 統計情報を取得（デバッグ用）
            Debug.Log($"地形生成完了: {terrainMesh.vertexCount} vertices, {terrainMesh.triangles.Length / 3} triangles");
        }

        /// <summary>
        /// 複数の地形タイプをテスト
        /// </summary>
        [ContextMenu("Test All Terrain Types")]
        public void TestAllTerrainTypes()
        {
            // 矩形地形
            terrainParams.terrainType = MeshGenerator.TerrainType.Rectangular;
            GenerateTerrain();
            
            // 円形地形
            terrainParams.terrainType = MeshGenerator.TerrainType.Circular;
            GenerateTerrain();
            
            // シームレス地形
            terrainParams.terrainType = MeshGenerator.TerrainType.Seamless;
            GenerateTerrain();
        }

        /// <summary>
        /// 全てのノイズタイプをテスト
        /// </summary>
        [ContextMenu("Test All Noise Types")]
        public void TestAllNoiseTypes()
        {
            MeshGenerator.NoiseType[] noiseTypes = {
                MeshGenerator.NoiseType.Perlin,
                MeshGenerator.NoiseType.Simplex,
                MeshGenerator.NoiseType.Ridged,
                MeshGenerator.NoiseType.Fractal,
                MeshGenerator.NoiseType.Voronoi
            };

            foreach (var noiseType in noiseTypes)
            {
                terrainParams.noiseType = noiseType;
                GenerateTerrain();
                
                // 少し待機
                System.Threading.Thread.Sleep(1000);
            }
        }
        #endregion

        #region ユーティリティ
        /// <summary>
        /// デフォルトの地形マテリアルを作成
        /// </summary>
        private Material CreateDefaultTerrainMaterial()
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.name = "Default Terrain Material";
            
            // 地形らしい色を設定
            mat.color = new Color(0.5f, 0.7f, 0.3f, 1f); // 緑っぽい色
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.2f);
            
            return mat;
        }

        /// <summary>
        /// パラメータをリセット
        /// </summary>
        [ContextMenu("Reset Parameters")]
        public void ResetParameters()
        {
            terrainParams = MeshGenerator.TerrainGenerationParams.Default();
        }

        /// <summary>
        /// ランダムパラメータを生成
        /// </summary>
        [ContextMenu("Generate Random Parameters")]
        public void GenerateRandomParameters()
        {
            terrainParams.noiseScale = Random.Range(0.005f, 0.02f);
            terrainParams.maxHeight = Random.Range(50f, 200f);
            terrainParams.octaves = Random.Range(3, 8);
            terrainParams.persistence = Random.Range(0.3f, 0.8f);
            terrainParams.lacunarity = Random.Range(1.5f, 3f);
            terrainParams.offset = new Vector2(Random.Range(-100f, 100f), Random.Range(-100f, 100f));
            
            if (terrainParams.terrainType == MeshGenerator.TerrainType.Circular)
            {
                terrainParams.falloffStrength = Random.Range(1f, 4f);
            }
            
            Debug.Log("ランダムパラメータを生成しました");
        }
        #endregion

        #region デバッグ情報
        private void OnDrawGizmos()
        {
            if (generatedTerrain != null)
            {
                // 地形の境界を表示
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(generatedTerrain.transform.position, 
                    new Vector3(terrainParams.size, terrainParams.maxHeight, terrainParams.size));
                
                // 円形地形の場合、円を表示
                if (terrainParams.terrainType == MeshGenerator.TerrainType.Circular)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(generatedTerrain.transform.position, terrainParams.radius);
                }
            }
        }

        private void OnValidate()
        {
            // パラメータが変更されたら自動生成
            if (autoGenerate && Application.isPlaying)
            {
                GenerateTerrain();
            }
        }
        #endregion
    }
}
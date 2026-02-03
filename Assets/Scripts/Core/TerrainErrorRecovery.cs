using System;
using System.Collections;
using UnityEngine;
using Vastcore.Utilities;

namespace Vastcore.Core
{
    /// <summary>
    /// 地形生成専用のエラー回復システム
    /// 地形生成失敗時の詳細なフォールバック処理を提供
    /// </summary>
    public class TerrainErrorRecovery : MonoBehaviour
    {
        [Header("地形エラー回復設定")]
        public bool enableProgressiveFallback = true;
        public bool enableEmergencyTerrain = true;
        public float recoveryTimeout = 30f;
        
        [Header("フォールバック品質設定")]
        [Range(0.1f, 1f)]
        public float minQualityLevel = 0.2f;
        public int maxFallbackAttempts = 5;
        
        [Header("緊急地形設定")]
        public Material emergencyTerrainMaterial;
        public float emergencyTerrainSize = 1000f;
        
        private static TerrainErrorRecovery instance;
        public static TerrainErrorRecovery Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<TerrainErrorRecovery>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("TerrainErrorRecovery");
                        instance = go.AddComponent<TerrainErrorRecovery>();
                    }
                }
                return instance;
            }
        }
        
        /// <summary>
        /// 段階的フォールバック地形生成
        /// </summary>
        public IEnumerator RecoverTerrainGeneration(TerrainGenerationParams originalParams, 
            System.Action<GameObject> onSuccess, System.Action onFailure)
        {
            VastcoreLogger.Instance.LogInfo("TerrainRecovery", "段階的地形回復を開始");
            
            for (int attempt = 0; attempt < maxFallbackAttempts; attempt++)
            {
                float qualityLevel = 1f - (attempt * 0.2f);
                qualityLevel = Mathf.Max(qualityLevel, minQualityLevel);
                
                var fallbackParams = CreateFallbackParams(originalParams, qualityLevel);
                GameObject recoveredTerrain = null;
                yield return StartCoroutine(GenerateRecoveryTerrain(fallbackParams, attempt, terrain => { recoveredTerrain = terrain; }));
                
                if (recoveredTerrain != null)
                {
                    VastcoreLogger.Instance.LogInfo("TerrainRecovery", 
                        $"地形回復成功 (品質レベル: {qualityLevel:F2}, 試行: {attempt + 1})");
                    onSuccess?.Invoke(recoveredTerrain);
                    yield break;
                }
                
                yield return new WaitForSeconds(1f); // 次の試行まで待機
            }
            
            // 全ての試行が失敗した場合、緊急地形を生成
            if (enableEmergencyTerrain)
            {
                var emergencyTerrain = GenerateEmergencyTerrain(originalParams);
                if (emergencyTerrain != null)
                {
                    VastcoreLogger.Instance.LogWarning("TerrainRecovery", "緊急地形を生成しました");
                    onSuccess?.Invoke(emergencyTerrain);
                    yield break;
                }
            }
            
            VastcoreLogger.Instance.LogError("TerrainRecovery", "全ての地形回復試行が失敗しました");
            onFailure?.Invoke();
        }
        
        private TerrainGenerationParams CreateFallbackParams(TerrainGenerationParams original, float qualityLevel)
        {
            return new TerrainGenerationParams
            {
                terrainSize = original.terrainSize * qualityLevel,
                resolution = Mathf.RoundToInt(original.resolution * qualityLevel),
                heightScale = original.heightScale * qualityLevel,
                noiseScale = original.noiseScale,
                octaves = Mathf.Max(1, Mathf.RoundToInt(original.octaves * qualityLevel)),
                persistence = original.persistence,
                lacunarity = original.lacunarity
            };
        }
        
        private IEnumerator GenerateRecoveryTerrain(TerrainGenerationParams parameters, int attemptNumber, System.Action<GameObject> onComplete)
        {
            float startTime = Time.time;
            GameObject recoveryTerrain = null;
            
            // 非同期で地形生成を試行（try/catch を含まないブロックで yield）
            yield return StartCoroutine(GenerateTerrainAsync(parameters, (terrain) => {
                recoveryTerrain = terrain;
            }));

            // タイムアウトチェック（yield を含まないため try/catch 可能）
            try
            {
                if (Time.time - startTime > recoveryTimeout)
                {
                    VastcoreLogger.Instance.LogWarning("TerrainRecovery", 
                        $"回復試行 {attemptNumber + 1} がタイムアウトしました");
                    if (recoveryTerrain != null)
                    {
                        Destroy(recoveryTerrain);
                    }
                    recoveryTerrain = null;
                }
            }
            catch (Exception error)
            {
                VastcoreLogger.Instance.LogError("TerrainRecovery", 
                    $"回復地形生成中にエラー: {error.Message}", error);
                if (recoveryTerrain != null)
                {
                    Destroy(recoveryTerrain);
                }
                recoveryTerrain = null;
            }

            onComplete?.Invoke(recoveryTerrain);
        }
        
        private IEnumerator GenerateTerrainAsync(TerrainGenerationParams parameters, System.Action<GameObject> callback)
        {
            // 簡単な地形生成（実際の実装では既存のMeshGeneratorを使用）
            GameObject terrain = new GameObject($"RecoveryTerrain_{parameters.resolution}");
            
            // 平面メッシュの生成
            var meshFilter = terrain.AddComponent<MeshFilter>();
            var meshRenderer = terrain.AddComponent<MeshRenderer>();
            var meshCollider = terrain.AddComponent<MeshCollider>();
            
            // 簡単な平面メッシュを生成
            Mesh planeMesh = CreateSimplePlaneMesh(parameters);
            meshFilter.mesh = planeMesh;
            meshCollider.sharedMesh = planeMesh;
            
            // マテリアルの設定
            if (emergencyTerrainMaterial != null)
            {
                meshRenderer.material = emergencyTerrainMaterial;
            }
            else
            {
                meshRenderer.material = CreateDefaultTerrainMaterial();
            }
            
            yield return new WaitForEndOfFrame();
            
            callback?.Invoke(terrain);
        }
        
        private GameObject GenerateEmergencyTerrain(TerrainGenerationParams originalParams)
        {
            try
            {
                VastcoreLogger.Instance.LogWarning("TerrainRecovery", "緊急地形の生成を開始");
                
                GameObject emergencyTerrain = GameObject.CreatePrimitive(PrimitiveType.Plane);
                emergencyTerrain.name = "EmergencyTerrain";
                
                // 緊急地形のスケール設定
                float scale = emergencyTerrainSize / 10f; // Planeのデフォルトサイズは10x10
                emergencyTerrain.transform.localScale = new Vector3(scale, 1f, scale);
                
                // 緊急用マテリアルの適用
                var renderer = emergencyTerrain.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.material = emergencyTerrainMaterial ?? CreateEmergencyMaterial();
                }
                
                // 警告表示用のテキストを追加
                CreateEmergencyWarningText(emergencyTerrain);
                
                return emergencyTerrain;
            }
            catch (Exception error)
            {
                VastcoreLogger.Instance.LogError("TerrainRecovery", 
                    $"緊急地形生成中にエラー: {error.Message}", error);
                return null;
            }
        }
        
        private Mesh CreateSimplePlaneMesh(TerrainGenerationParams parameters)
        {
            int resolution = Mathf.Max(2, parameters.resolution);
            Vector3[] vertices = new Vector3[resolution * resolution];
            int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
            Vector2[] uvs = new Vector2[vertices.Length];
            
            float size = parameters.terrainSize;
            float step = size / (resolution - 1);
            
            // 頂点の生成
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int index = z * resolution + x;
                    vertices[index] = new Vector3(
                        (x - resolution / 2f) * step,
                        0f,
                        (z - resolution / 2f) * step
                    );
                    uvs[index] = new Vector2((float)x / (resolution - 1), (float)z / (resolution - 1));
                }
            }
            
            // 三角形の生成
            int triangleIndex = 0;
            for (int z = 0; z < resolution - 1; z++)
            {
                for (int x = 0; x < resolution - 1; x++)
                {
                    int bottomLeft = z * resolution + x;
                    int bottomRight = bottomLeft + 1;
                    int topLeft = (z + 1) * resolution + x;
                    int topRight = topLeft + 1;
                    
                    // 最初の三角形
                    triangles[triangleIndex] = bottomLeft;
                    triangles[triangleIndex + 1] = topLeft;
                    triangles[triangleIndex + 2] = bottomRight;
                    
                    // 二番目の三角形
                    triangles[triangleIndex + 3] = bottomRight;
                    triangles[triangleIndex + 4] = topLeft;
                    triangles[triangleIndex + 5] = topRight;
                    
                    triangleIndex += 6;
                }
            }
            
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        private Material CreateDefaultTerrainMaterial()
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.5f, 0.7f, 0.3f); // 緑っぽい色
            material.name = "DefaultRecoveryTerrain";
            return material;
        }
        
        private Material CreateEmergencyMaterial()
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = Color.red; // 緊急を示す赤色
            material.name = "EmergencyTerrain";
            return material;
        }
        
        private void CreateEmergencyWarningText(GameObject terrain)
        {
            try
            {
                GameObject textObject = new GameObject("EmergencyWarning");
                textObject.transform.SetParent(terrain.transform);
                textObject.transform.localPosition = Vector3.up * 10f;
                
                var textMesh = textObject.AddComponent<TextMesh>();
                textMesh.text = "EMERGENCY TERRAIN\n地形生成エラーが発生しました";
                textMesh.fontSize = 20;
                textMesh.color = Color.yellow;
                textMesh.anchor = TextAnchor.MiddleCenter;
                
                // テキストを常にカメラの方向に向ける
                var billboard = textObject.AddComponent<Billboard>();
            }
            catch (Exception error)
            {
                VastcoreLogger.Instance.LogWarning("TerrainRecovery", 
                    $"警告テキスト作成中にエラー: {error.Message}");
            }
        }
        
        /// <summary>
        /// 地形の健全性チェック
        /// </summary>
        public bool ValidateTerrain(GameObject terrain)
        {
            if (terrain == null) return false;
            
            try
            {
                var meshFilter = terrain.GetComponent<MeshFilter>();
                var meshRenderer = terrain.GetComponent<MeshRenderer>();
                var meshCollider = terrain.GetComponent<MeshCollider>();
                
                // 必要なコンポーネントの存在確認
                if (meshFilter == null || meshRenderer == null)
                {
                    VastcoreLogger.Instance.LogWarning("TerrainValidation", "必要なコンポーネントが不足しています");
                    return false;
                }
                
                // メッシュの有効性確認
                if (meshFilter.mesh == null || meshFilter.mesh.vertexCount == 0)
                {
                    VastcoreLogger.Instance.LogWarning("TerrainValidation", "無効なメッシュが検出されました");
                    return false;
                }
                
                // マテリアルの確認
                if (meshRenderer.material == null)
                {
                    VastcoreLogger.Instance.LogWarning("TerrainValidation", "マテリアルが設定されていません");
                    meshRenderer.material = CreateDefaultTerrainMaterial();
                }
                
                return true;
            }
            catch (Exception error)
            {
                VastcoreLogger.Instance.LogError("TerrainValidation", 
                    $"地形検証中にエラー: {error.Message}", error);
                return false;
            }
        }
    }
    
    /// <summary>
    /// テキストを常にカメラに向けるコンポーネント
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        private Camera mainCamera;
        
        private void Start()
        {
            mainCamera = Camera.main;
        }
        
        private void Update()
        {
            if (mainCamera != null)
            {
                transform.LookAt(mainCamera.transform);
                transform.Rotate(0, 180, 0); // テキストが逆向きにならないよう調整
            }
        }
    }
}
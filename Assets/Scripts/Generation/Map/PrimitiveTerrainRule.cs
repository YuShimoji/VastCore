using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// プリミティブ地形生成ルール
    /// 各プリミティブタイプの生成条件と設定を定義
    /// </summary>
    [System.Serializable]
    [CreateAssetMenu(fileName = "PrimitiveTerrainRule", menuName = "Vastcore/Primitive Terrain Rule")]
    public class PrimitiveTerrainRule : ScriptableObject
    {
        [Header("プリミティブ設定")]
        public string primitiveName = "Unknown Primitive";
        public PrimitiveTerrainGenerator.PrimitiveType primitiveType = PrimitiveTerrainGenerator.PrimitiveType.Cube;
        [Range(0f, 1f)]
        public float spawnProbability = 0.05f;
        public Vector2 scaleRange = new Vector2(50f, 500f);
        
        [Header("配置条件")]
        public float minDistanceFromPlayer = 200f;
        public float maxDistanceFromPlayer = 2000f;
        public float minTerrainHeight = 10f;
        public float maxTerrainHeight = 150f;
        [Range(0f, 90f)]
        public float maxTerrainSlope = 45f;
        
        [Header("形状変形")]
        public bool enableDeformation = true;
        public Vector3 deformationRange = Vector3.one;
        [Range(0f, 1f)]
        public float noiseIntensity = 0.1f;
        [Range(0, 5)]
        public int subdivisionLevel = 2;
        
        [Header("材質設定")]
        public Material[] possibleMaterials;
        public bool randomizeMaterial = true;
        public Color colorVariation = Color.white;
        
        [Header("環境条件")]
        [Range(0f, 1f)]
        public float preferredMoisture = 0.5f; // 将来の拡張用
        [Range(0f, 1f)]
        public float preferredTemperature = 0.5f; // 将来の拡張用
        public string[] requiredBiomes; // 将来の拡張用

        /// <summary>
        /// 指定位置に生成可能かどうかを判定
        /// </summary>
        public bool CanSpawnAt(Vector3 position, float terrainHeight, float terrainSlope)
        {
            // 地形高度チェック
            if (terrainHeight < minTerrainHeight || terrainHeight > maxTerrainHeight)
            {
                return false;
            }
            
            // 地形傾斜チェック
            if (terrainSlope > maxTerrainSlope)
            {
                return false;
            }
            
            // プレイヤーからの距離チェック（プレイヤーが存在する場合）
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float distanceFromPlayer = Vector3.Distance(position, player.transform.position);
                if (distanceFromPlayer < minDistanceFromPlayer || distanceFromPlayer > maxDistanceFromPlayer)
                {
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// デフォルトルールを作成
        /// </summary>
        public static PrimitiveTerrainRule CreateDefault(PrimitiveTerrainGenerator.PrimitiveType type)
        {
            var rule = CreateInstance<PrimitiveTerrainRule>();
            rule.primitiveType = type;
            rule.primitiveName = PrimitiveTerrainGenerator.GetPrimitiveDescription(type);
            
            // タイプに応じたデフォルト設定
            switch (type)
            {
                case PrimitiveTerrainGenerator.PrimitiveType.Cube:
                    rule.spawnProbability = 0.08f;
                    rule.scaleRange = new Vector2(80f, 150f);
                    rule.maxTerrainSlope = 30f;
                    break;
                    
                case PrimitiveTerrainGenerator.PrimitiveType.Sphere:
                    rule.spawnProbability = 0.06f;
                    rule.scaleRange = new Vector2(60f, 120f);
                    rule.maxTerrainSlope = 45f;
                    break;
                    
                case PrimitiveTerrainGenerator.PrimitiveType.Cylinder:
                    rule.spawnProbability = 0.05f;
                    rule.scaleRange = new Vector2(50f, 100f);
                    rule.maxTerrainSlope = 25f;
                    break;
                    
                case PrimitiveTerrainGenerator.PrimitiveType.Pyramid:
                    rule.spawnProbability = 0.04f;
                    rule.scaleRange = new Vector2(100f, 200f);
                    rule.maxTerrainSlope = 20f;
                    rule.minTerrainHeight = 20f;
                    break;
                    
                case PrimitiveTerrainGenerator.PrimitiveType.Crystal:
                    rule.spawnProbability = 0.03f;
                    rule.scaleRange = new Vector2(40f, 80f);
                    rule.maxTerrainSlope = 35f;
                    rule.enableDeformation = true;
                    rule.noiseIntensity = 0.15f;
                    break;
                    
                case PrimitiveTerrainGenerator.PrimitiveType.Monolith:
                    rule.spawnProbability = 0.02f;
                    rule.scaleRange = new Vector2(30f, 60f);
                    rule.maxTerrainSlope = 15f;
                    rule.minTerrainHeight = 30f;
                    break;
                    
                case PrimitiveTerrainGenerator.PrimitiveType.Mesa:
                    rule.spawnProbability = 0.03f;
                    rule.scaleRange = new Vector2(200f, 400f);
                    rule.maxTerrainSlope = 10f;
                    rule.minTerrainHeight = 50f;
                    break;
                    
                case PrimitiveTerrainGenerator.PrimitiveType.Boulder:
                    rule.spawnProbability = 0.1f;
                    rule.scaleRange = new Vector2(30f, 80f);
                    rule.maxTerrainSlope = 60f;
                    rule.enableDeformation = true;
                    rule.noiseIntensity = 0.2f;
                    break;
                    
                case PrimitiveTerrainGenerator.PrimitiveType.Spire:
                    rule.spawnProbability = 0.02f;
                    rule.scaleRange = new Vector2(20f, 40f);
                    rule.maxTerrainSlope = 10f;
                    rule.minTerrainHeight = 40f;
                    break;
                    
                case PrimitiveTerrainGenerator.PrimitiveType.Arch:
                    rule.spawnProbability = 0.01f;
                    rule.scaleRange = new Vector2(150f, 300f);
                    rule.maxTerrainSlope = 15f;
                    rule.minTerrainHeight = 30f;
                    break;
                    
                case PrimitiveTerrainGenerator.PrimitiveType.Ring:
                    rule.spawnProbability = 0.015f;
                    rule.scaleRange = new Vector2(200f, 400f);
                    rule.maxTerrainSlope = 20f;
                    rule.minTerrainHeight = 50f;
                    break;
                    
                case PrimitiveTerrainGenerator.PrimitiveType.Formation:
                    rule.spawnProbability = 0.04f;
                    rule.scaleRange = new Vector2(100f, 250f);
                    rule.maxTerrainSlope = 40f;
                    rule.enableDeformation = true;
                    rule.noiseIntensity = 0.1f;
                    break;
                    
                default:
                    rule.spawnProbability = 0.05f;
                    rule.scaleRange = new Vector2(50f, 150f);
                    break;
            }
            
            return rule;
        }

        /// <summary>
        /// ルールの妥当性を検証
        /// </summary>
        public bool ValidateRule()
        {
            if (spawnProbability < 0f || spawnProbability > 1f)
            {
                Debug.LogError($"Invalid spawn probability for {primitiveName}: {spawnProbability}");
                return false;
            }
            
            if (scaleRange.x <= 0f || scaleRange.y <= 0f || scaleRange.x > scaleRange.y)
            {
                Debug.LogError($"Invalid scale range for {primitiveName}: {scaleRange}");
                return false;
            }
            
            if (minDistanceFromPlayer < 0f || maxDistanceFromPlayer < minDistanceFromPlayer)
            {
                Debug.LogError($"Invalid distance range for {primitiveName}");
                return false;
            }
            
            if (minTerrainHeight > maxTerrainHeight)
            {
                Debug.LogError($"Invalid terrain height range for {primitiveName}");
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// ルール情報を文字列として取得
        /// </summary>
        public override string ToString()
        {
            return $"{primitiveName} (Type: {primitiveType}, Probability: {spawnProbability:P1}, Scale: {scaleRange.x}-{scaleRange.y}m)";
        }

        /// <summary>
        /// エディタでの検証
        /// </summary>
        void OnValidate()
        {
            // 値の範囲を制限
            spawnProbability = Mathf.Clamp01(spawnProbability);
            scaleRange.x = Mathf.Max(1f, scaleRange.x);
            scaleRange.y = Mathf.Max(scaleRange.x, scaleRange.y);
            minDistanceFromPlayer = Mathf.Max(0f, minDistanceFromPlayer);
            maxDistanceFromPlayer = Mathf.Max(minDistanceFromPlayer, maxDistanceFromPlayer);
            maxTerrainSlope = Mathf.Clamp(maxTerrainSlope, 0f, 90f);
            noiseIntensity = Mathf.Clamp01(noiseIntensity);
            subdivisionLevel = Mathf.Clamp(subdivisionLevel, 0, 5);
            
            // 名前が空の場合はタイプから生成
            if (string.IsNullOrEmpty(primitiveName))
            {
                primitiveName = PrimitiveTerrainGenerator.GetPrimitiveDescription(primitiveType);
            }
        }
    }
}
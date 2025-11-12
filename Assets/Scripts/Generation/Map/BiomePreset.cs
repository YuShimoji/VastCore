using UnityEngine;
using System.Collections.Generic;
using Vastcore.Terrain.Map;

namespace Vastcore.Generation
{
    /// <summary>
    /// バイオームプリセット - 地形とプリミティブ設定を統合管理
    /// ScriptableObjectベースで設定の保存・読み込みに対応
    /// </summary>
    [System.Serializable]
    [CreateAssetMenu(fileName = "BiomePreset", menuName = "Vastcore/Biome Preset")]
    public class BiomePreset : ScriptableObject
    {
        [Header("プリセット基本情報")]
        public string presetName = "New Biome";
        [TextArea(3, 5)]
        public string description = "バイオームの説明を入力してください";
        public Texture2D previewImage;
        
        [Header("地形生成パラメータ")]
        public MeshGenerator.TerrainGenerationParams terrainParams = MeshGenerator.TerrainGenerationParams.Default();
        
        [Header("プリミティブ地形設定")]
        public List<PrimitiveTerrainRule> primitiveRules = new List<PrimitiveTerrainRule>();
        [Range(0f, 1f)]
        public float primitiveSpawnDensity = 0.1f;
        public float minPrimitiveDistance = 200f;
        public float maxPrimitiveDistance = 2000f;
        
        [Header("材質・環境設定")]
        public MaterialSettings materialSettings = new MaterialSettings();
        public EnvironmentSettings environmentSettings = new EnvironmentSettings();
        
        [Header("構造物生成設定")]
        public StructureSpawnSettings structureSettings = new StructureSpawnSettings();
        
        [Header("バイオーム特性")]
        [Range(0f, 1f)]
        public float moisture = 0.5f;           // 湿度
        [Range(0f, 1f)]
        public float temperature = 0.5f;        // 温度
        [Range(0f, 1f)]
        public float fertility = 0.5f;          // 肥沃度
        [Range(0f, 1f)]
        public float rockiness = 0.5f;          // 岩石度
        
        /// <summary>
        /// プリセットの妥当性を検証
        /// </summary>
        public bool ValidatePreset()
        {
            if (string.IsNullOrEmpty(presetName))
            {
                Debug.LogWarning("BiomePreset: プリセット名が設定されていません");
                return false;
            }
            
            if (terrainParams.resolution <= 0 || terrainParams.size <= 0)
            {
                Debug.LogWarning("BiomePreset: 地形パラメータが無効です");
                return false;
            }
            
            if (primitiveSpawnDensity < 0f || primitiveSpawnDensity > 1f)
            {
                Debug.LogWarning("BiomePreset: プリミティブ生成密度が範囲外です");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// デフォルト設定でプリセットを初期化
        /// </summary>
        public void InitializeDefault()
        {
            presetName = "Default Biome";
            description = "デフォルトバイオーム設定";
            terrainParams = MeshGenerator.TerrainGenerationParams.Default();
            primitiveSpawnDensity = 0.1f;
            minPrimitiveDistance = 200f;
            maxPrimitiveDistance = 2000f;
            
            materialSettings = new MaterialSettings();
            environmentSettings = new EnvironmentSettings();
            structureSettings = new StructureSpawnSettings();
            
            moisture = 0.5f;
            temperature = 0.5f;
            fertility = 0.5f;
            rockiness = 0.5f;
        }
        
        /// <summary>
        /// プリセットの深いコピーを作成
        /// </summary>
        public BiomePreset CreateCopy()
        {
            var copy = CreateInstance<BiomePreset>();
            copy.presetName = presetName + " (Copy)";
            copy.description = description;
            copy.previewImage = previewImage;
            copy.terrainParams = terrainParams;
            copy.primitiveRules = new List<PrimitiveTerrainRule>(primitiveRules);
            copy.primitiveSpawnDensity = primitiveSpawnDensity;
            copy.minPrimitiveDistance = minPrimitiveDistance;
            copy.maxPrimitiveDistance = maxPrimitiveDistance;
            copy.materialSettings = materialSettings.CreateCopy();
            copy.environmentSettings = environmentSettings.CreateCopy();
            copy.structureSettings = structureSettings.CreateCopy();
            copy.moisture = moisture;
            copy.temperature = temperature;
            copy.fertility = fertility;
            copy.rockiness = rockiness;
            
            return copy;
        }
    }
    
    /// <summary>
    /// 材質設定
    /// </summary>
    [System.Serializable]
    public class MaterialSettings
    {
        [Header("地形材質")]
        public Material terrainMaterial;
        public Material[] terrainTextures;
        public Color terrainTint = Color.white;
        
        [Header("プリミティブ材質")]
        public Material[] primitiveMaterials;
        public bool randomizePrimitiveMaterials = true;
        public Color primitiveColorVariation = Color.white;
        
        [Header("環境色調")]
        public Color fogColor = Color.gray;
        public Color ambientColor = Color.gray;
        public Color skyboxTint = Color.white;
        
        public MaterialSettings CreateCopy()
        {
            var copy = new MaterialSettings();
            copy.terrainMaterial = terrainMaterial;
            copy.terrainTextures = terrainTextures != null ? (Material[])terrainTextures.Clone() : null;
            copy.terrainTint = terrainTint;
            copy.primitiveMaterials = primitiveMaterials != null ? (Material[])primitiveMaterials.Clone() : null;
            copy.randomizePrimitiveMaterials = randomizePrimitiveMaterials;
            copy.primitiveColorVariation = primitiveColorVariation;
            copy.fogColor = fogColor;
            copy.ambientColor = ambientColor;
            copy.skyboxTint = skyboxTint;
            return copy;
        }
    }
    
    /// <summary>
    /// 環境設定
    /// </summary>
    [System.Serializable]
    public class EnvironmentSettings
    {
        [Header("照明設定")]
        public Color sunColor = Color.white;
        [Range(0f, 8f)]
        public float sunIntensity = 1f;
        [Range(0f, 360f)]
        public float sunRotation = 45f;
        
        [Header("霧設定")]
        public bool enableFog = true;
        public Color fogColor = Color.gray;
        [Range(0f, 1000f)]
        public float fogStartDistance = 100f;
        [Range(0f, 5000f)]
        public float fogEndDistance = 1000f;
        
        [Header("風設定")]
        public Vector3 windDirection = Vector3.right;
        [Range(0f, 10f)]
        public float windStrength = 1f;
        
        [Header("パーティクル効果")]
        public GameObject[] ambientParticles;
        [Range(0f, 1f)]
        public float particleDensity = 0.1f;
        
        public EnvironmentSettings CreateCopy()
        {
            var copy = new EnvironmentSettings();
            copy.sunColor = sunColor;
            copy.sunIntensity = sunIntensity;
            copy.sunRotation = sunRotation;
            copy.enableFog = enableFog;
            copy.fogColor = fogColor;
            copy.fogStartDistance = fogStartDistance;
            copy.fogEndDistance = fogEndDistance;
            copy.windDirection = windDirection;
            copy.windStrength = windStrength;
            copy.ambientParticles = ambientParticles != null ? (GameObject[])ambientParticles.Clone() : null;
            copy.particleDensity = particleDensity;
            return copy;
        }
    }
    
    /// <summary>
    /// 構造物生成設定
    /// </summary>
    [System.Serializable]
    public class StructureSpawnSettings
    {
        [Header("構造物生成")]
        public bool enableStructureSpawn = false;
        public GameObject[] structurePrefabs;
        [Range(0f, 1f)]
        public float structureSpawnProbability = 0.01f;
        public float minStructureDistance = 500f;
        public float maxStructureDistance = 3000f;
        
        [Header("建築物生成（将来拡張）")]
        public bool enableBuildingGeneration = false;
        public int maxBuildingsPerTile = 3;
        public Vector2 buildingSizeRange = new Vector2(10f, 50f);
        
        public StructureSpawnSettings CreateCopy()
        {
            var copy = new StructureSpawnSettings();
            copy.enableStructureSpawn = enableStructureSpawn;
            copy.structurePrefabs = structurePrefabs != null ? (GameObject[])structurePrefabs.Clone() : null;
            copy.structureSpawnProbability = structureSpawnProbability;
            copy.minStructureDistance = minStructureDistance;
            copy.maxStructureDistance = maxStructureDistance;
            copy.enableBuildingGeneration = enableBuildingGeneration;
            copy.maxBuildingsPerTile = maxBuildingsPerTile;
            copy.buildingSizeRange = buildingSizeRange;
            return copy;
        }
    }
}
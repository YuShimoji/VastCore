using UnityEngine;

namespace Vastcore.Generation
{
    public class BiomeSystem : MonoBehaviour
    {
        public void Initialize() {}
        public BiomeType DetermineBiome(Vector3 position) => BiomeType.Grassland;
        
        public BiomeDefinition GetBiomeDefinition(BiomeType biomeType)
        {
            // 簡易実装：デフォルトのバイオーム定義を返す
            return new BiomeDefinition
            {
                biomeType = biomeType,
                name = biomeType.ToString(),
                temperatureRange = new Vector2(10f, 25f),
                moistureRange = new Vector2(300f, 1000f),
                elevationRange = new Vector2(0f, 1000f),
                terrainModifiers = new TerrainModificationData
                {
                    heightMultiplier = 1f,
                    roughnessMultiplier = 0.5f,
                    erosionStrength = 0.2f,
                    sedimentationRate = 0.1f
                }
            };
        }
    }
    
    // 簡易的なバイオーム定義クラス
    public class BiomeDefinition
    {
        public BiomeType biomeType;
        public string name;
        public Vector2 temperatureRange;
        public Vector2 moistureRange;
        public Vector2 elevationRange;
        public TerrainModificationData terrainModifiers;
    }
    
    // 地形修正データクラス
    public class TerrainModificationData
    {
        public float heightMultiplier = 1f;
        public float roughnessMultiplier = 1f;
        public float erosionStrength = 0f;
        public float sedimentationRate = 0f;
    }
}

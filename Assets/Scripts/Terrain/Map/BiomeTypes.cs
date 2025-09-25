using UnityEngine;

namespace Vastcore.Generation
{
    public enum BiomeType
    {
        Desert,
        Forest,
        Mountain,
        Coastal,
        Polar,
        Grassland
    }

    [System.Serializable]
    public class TerrainModificationData
    {
        public float heightMultiplier = 1f;
        public float roughnessMultiplier = 1f;
        public float erosionStrength = 0.5f;
        public float sedimentationRate = 0.3f;
    }

    [System.Serializable]
    public class BiomeDefinition
    {
        public BiomeType biomeType;
        public string name;
        public Vector2 temperatureRange;
        public Vector2 moistureRange;
        public Vector2 elevationRange;
        public TerrainModificationData terrainModifiers = new TerrainModificationData();
    }
}

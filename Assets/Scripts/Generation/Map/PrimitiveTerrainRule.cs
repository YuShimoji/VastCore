using UnityEngine;
using System.Collections.Generic;

namespace Vastcore.Generation
{
    [System.Serializable]
    public class PrimitiveTerrainRule
    {
        public string primitiveName;
        public PrimitiveTerrainGenerator.PrimitiveType primitiveType;
        
        [Range(0f, 1f)]
        public float spawnProbability = 0.5f;
        
        public Vector2 scaleRange = new Vector2(10f, 50f);
        
        [Header("Deformation")]
        public bool enableDeformation = false;
        public Vector3 deformationRange = Vector3.zero;
        public float noiseIntensity = 0f;
        public int subdivisionLevel = 0;
        
        [Header("Materials")]
        public Material[] possibleMaterials;
        public bool randomizeMaterial = false;
        public Color colorVariation = Color.white;

        [Header("Placement Constraints")]
        public float minHeight = -1000f;
        public float maxHeight = 1000f;
        public float maxSlope = 45f;

        public static PrimitiveTerrainRule CreateDefault(PrimitiveTerrainGenerator.PrimitiveType type)
        {
            return new PrimitiveTerrainRule
            {
                primitiveName = type.ToString(),
                primitiveType = type,
                spawnProbability = 0.1f,
                scaleRange = new Vector2(10f, 50f),
                minHeight = 0f,
                maxHeight = 1000f,
                maxSlope = 30f
            };
        }

        public bool CanSpawnAt(Vector3 position, float height, float slope)
        {
            if (height < minHeight || height > maxHeight) return false;
            if (slope > maxSlope) return false;
            return true;
        }
    }
}

using UnityEngine;

namespace Vastcore.Generation
{
    public static class HighQualityPrimitiveGenerator
    {
        [System.Serializable]
        public struct QualitySettings
        {
            public int subdivisionLevel;
            public bool enableSmoothNormals;
            public bool enablePreciseColliders;
            public bool enableAdvancedDeformation;
            public bool enableDeformSystem;
            public bool enableGeologicalDeformation;
            public bool enableOrganicDeformation;

            // Optional descriptive enum for tests/logs
            public enum DeformQuality { Low, Medium, High }
            public DeformQuality deformQuality;

            public static QualitySettings Low => new QualitySettings
            {
                subdivisionLevel = 0,
                enableSmoothNormals = false,
                enablePreciseColliders = false,
                enableAdvancedDeformation = false,
                enableDeformSystem = false,
                enableGeologicalDeformation = false,
                enableOrganicDeformation = false,
                deformQuality = DeformQuality.Low
            };

            public static QualitySettings Medium => new QualitySettings
            {
                subdivisionLevel = 1,
                enableSmoothNormals = true,
                enablePreciseColliders = false,
                enableAdvancedDeformation = false,
                enableDeformSystem = false,
                enableGeologicalDeformation = false,
                enableOrganicDeformation = false,
                deformQuality = DeformQuality.Medium
            };

            public static QualitySettings High => new QualitySettings
            {
                subdivisionLevel = 2,
                enableSmoothNormals = true,
                enablePreciseColliders = true,
                enableAdvancedDeformation = true,
                enableDeformSystem = false,
                enableGeologicalDeformation = true,
                enableOrganicDeformation = true,
                deformQuality = DeformQuality.High
            };
        }

        public static GameObject GenerateHighQualityPrimitive(
            PrimitiveTerrainGenerator.PrimitiveType type,
            Vector3 position,
            Vector3 scale,
            QualitySettings quality)
        {
            var p = PrimitiveTerrainGenerator.PrimitiveGenerationParams.Default(type);
            p.position = position;
            p.scale = scale;
            p.subdivisionLevel = quality.subdivisionLevel;
            p.generateCollider = quality.enablePreciseColliders;
            var go = PrimitiveTerrainGenerator.GeneratePrimitiveTerrain(p);
            if (go != null && quality.enableSmoothNormals)
            {
                var meshFilter = go.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    meshFilter.sharedMesh.RecalculateNormals();
                }
            }
            return go;
        }

        public static GameObject GeneratePrimitive(
            PrimitiveTerrainGenerator.PrimitiveType type,
            Vector3 scale,
            QualitySettings quality)
        {
            return GenerateHighQualityPrimitive(type, Vector3.zero, scale, quality);
        }
    }
}

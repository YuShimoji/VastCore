using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Linq;

// このファイルは廃止予定です。
// より完全な実装が Assets/Scripts/Terrain/Map/PrimitiveTerrainGenerator.cs に存在します。
// CS0436警告（型衝突）を解消するため、このクラスをコメントアウトしています。
// 将来的にこのファイルは削除される予定です。

/* DEPRECATED - DO NOT USE
namespace Vastcore.Generation
{
    public static class PrimitiveTerrainGenerator
    {
        public enum PrimitiveType
        {
            Cube, Sphere, Cylinder, Pyramid, Torus, Prism, Cone, Octahedron,
            Crystal, Monolith, Arch, Ring, Mesa, Spire, Boulder, Formation
        }

        [System.Serializable]
        public struct PrimitiveGenerationParams
        {
            public PrimitiveType primitiveType;
            public Vector3 position;
            public Vector3 scale;
            public Quaternion rotation;
            public bool enableDeformation;
            public Vector3 deformationRange;
            public float noiseIntensity;
            public int subdivisionLevel;
            public Material material;
            public Color colorVariation;
            public bool randomizeMaterial;
            public bool generateCollider;
            public bool isClimbable;
            public bool isGrindable;

            public static PrimitiveGenerationParams Default(PrimitiveType type)
            {
                return new PrimitiveGenerationParams
                {
                    primitiveType = type,
                    position = Vector3.zero,
                    scale = Vector3.one * 100f,
                    rotation = Quaternion.identity,
                    enableDeformation = true,
                    deformationRange = Vector3.one * 0.1f,
                    noiseIntensity = 0.05f,
                    subdivisionLevel = 2,
                    material = null,
                    colorVariation = Color.white,
                    randomizeMaterial = false,
                    generateCollider = true,
                    isClimbable = true,
                    isGrindable = true
                };
            }
        }

        public static GameObject GeneratePrimitiveTerrain(PrimitiveGenerationParams parameters)
        {
            try
            {
                ProBuilderMesh proBuilderMesh = GenerateBasePrimitive(parameters.primitiveType, parameters.scale);
                if (proBuilderMesh == null)
                {
                    Debug.LogError($"Failed to generate base primitive: {parameters.primitiveType}");
                    return null;
                }

                GameObject primitiveObject = proBuilderMesh.gameObject;
                primitiveObject.name = $"Primitive_{parameters.primitiveType}";
                primitiveObject.transform.position = parameters.position;
                primitiveObject.transform.rotation = parameters.rotation;

                if (parameters.enableDeformation)
                {
                    ApplyDeformation(proBuilderMesh, parameters);
                }

                if (parameters.subdivisionLevel > 0)
                {
                    ApplySubdivision(proBuilderMesh, parameters.subdivisionLevel);
                }

                proBuilderMesh.ToMesh();
                proBuilderMesh.Refresh();

                SetupMaterial(primitiveObject, parameters);

                if (parameters.generateCollider)
                {
                    GenerateCollider(primitiveObject, parameters);
                }

                SetupInteractionComponents(primitiveObject, parameters);

                Debug.Log($"Successfully generated primitive terrain: {parameters.primitiveType} at {parameters.position}");
                return primitiveObject;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error generating primitive terrain {parameters.primitiveType}: {e.Message}");
                return null;
            }
        }

        private static ProBuilderMesh GenerateBasePrimitive(PrimitiveType type, Vector3 scale)
        {
            ProBuilderMesh mesh = null;
            switch (type)
            {
                case PrimitiveType.Cube: mesh = ShapeGenerator.CreateShape(ShapeType.Cube); break;
                case PrimitiveType.Sphere: mesh = ShapeGenerator.CreateShape(ShapeType.Sphere); break;
                case PrimitiveType.Cylinder: mesh = ShapeGenerator.CreateShape(ShapeType.Cylinder); break;
                case PrimitiveType.Pyramid: mesh = ShapeGenerator.CreateShape(ShapeType.Cube); break; // Custom logic needed
                case PrimitiveType.Torus: mesh = ShapeGenerator.CreateShape(ShapeType.Torus); break;
                case PrimitiveType.Prism: mesh = ShapeGenerator.CreateShape(ShapeType.Prism); break;
                case PrimitiveType.Cone: mesh = ShapeGenerator.CreateShape(ShapeType.Cone); break;
                case PrimitiveType.Octahedron: mesh = ShapeGenerator.CreateShape(ShapeType.Cube); break; // Custom logic needed
                default: mesh = ShapeGenerator.CreateShape(ShapeType.Cube); break;
            }
            mesh.transform.localScale = scale;
            return mesh;
        }

        private static void ApplyDeformation(ProBuilderMesh mesh, PrimitiveGenerationParams parameters)
        {
            var vertices = mesh.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float noiseX = Mathf.PerlinNoise(vertex.x * 0.1f, vertex.z * 0.1f);
                float noiseY = Mathf.PerlinNoise(vertex.y * 0.1f, vertex.x * 0.1f);
                float noiseZ = Mathf.PerlinNoise(vertex.z * 0.1f, vertex.y * 0.1f);
                Vector3 deformation = new Vector3(noiseX - 0.5f, noiseY - 0.5f, noiseZ - 0.5f) * parameters.noiseIntensity;
                deformation = Vector3.Scale(deformation, parameters.deformationRange);
                vertices[i] = vertex + deformation;
            }
            mesh.positions = vertices;
        }

        private static void ApplySubdivision(ProBuilderMesh mesh, int subdivisionLevel)
        {
            // TODO: Subdivide機能は一時的に無効化（ProBuilderのAPI変更により）
            // 将来的にProBuilderの正しいAPIを調査して再実装予定
            Debug.LogWarning($"Subdivision feature is temporarily disabled due to ProBuilder API changes. Requested level: {subdivisionLevel}");
        }

        private static void SetupMaterial(GameObject primitiveObject, PrimitiveGenerationParams parameters)
        {
            var renderer = primitiveObject.GetComponent<MeshRenderer>();
            if (renderer != null && parameters.material != null)
            {
                renderer.material = parameters.material;
                if (parameters.randomizeMaterial)
                {
                    var materialInstance = new Material(parameters.material);
                    materialInstance.color = parameters.colorVariation;
                    renderer.material = materialInstance;
                }
            }
        }

        private static void GenerateCollider(GameObject primitiveObject, PrimitiveGenerationParams parameters)
        {
            var existingCollider = primitiveObject.GetComponent<Collider>();
            if (existingCollider != null) { Object.DestroyImmediate(existingCollider); }
            var meshCollider = primitiveObject.AddComponent<MeshCollider>();
            meshCollider.convex = false;
            var meshFilter = primitiveObject.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                meshCollider.sharedMesh = meshFilter.sharedMesh;
            }
        }

        private static void SetupInteractionComponents(GameObject primitiveObject, PrimitiveGenerationParams parameters)
        {
            var primitiveComponent = primitiveObject.AddComponent<PrimitiveTerrainObject>();
            primitiveComponent.primitiveType = (GenerationPrimitiveType)parameters.primitiveType;
            primitiveComponent.isClimbable = parameters.isClimbable;
            primitiveComponent.isGrindable = parameters.isGrindable;
            primitiveComponent.hasCollision = parameters.generateCollider;
            primitiveObject.layer = LayerMask.NameToLayer("Default");
        }
    }
}
*/

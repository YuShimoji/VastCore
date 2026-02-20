using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Linq;

namespace Vastcore.Generation.Map
{
    /// <summary>
    /// プリミティブ形状変形処理クラス
    /// </summary>
    public static class PrimitiveModifier
    {
        /// <summary>
        /// ノイズベースの形状変形を適用
        /// </summary>
        public static void ApplyDeformation(ProBuilderMesh mesh, Vector3 deformationRange, float noiseIntensity)
        {
            if (mesh == null) return;

            var vertices = mesh.positions.ToArray();

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];

                // Perlinノイズによる変形
                float noiseX = Mathf.PerlinNoise(vertex.x * 0.1f, vertex.z * 0.1f);
                float noiseY = Mathf.PerlinNoise(vertex.y * 0.1f, vertex.x * 0.1f);
                float noiseZ = Mathf.PerlinNoise(vertex.z * 0.1f, vertex.y * 0.1f);

                Vector3 deformation = new Vector3(noiseX - 0.5f, noiseY - 0.5f, noiseZ - 0.5f) * noiseIntensity;
                deformation = Vector3.Scale(deformation, deformationRange);

                vertices[i] = vertex + deformation;
            }

            mesh.positions = vertices;
        }

        /// <summary>
        /// メッシュの細分化を適用
        /// </summary>
        public static void ApplySubdivision(ProBuilderMesh mesh, int subdivisionLevel)
        {
            if (mesh == null) return;
            for (int i = 0; i < subdivisionLevel; i++)
            {
                ConnectElements.Connect(mesh, mesh.faces);
            }
        }

        /// <summary>
        /// 高度な変形を適用
        /// </summary>
        public static void ApplyAdvancedDeformation(ProBuilderMesh mesh, float intensity, int seed = 0)
        {
            Random.InitState(seed);
            var vertices = mesh.positions.ToArray();

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];

                // 複数のノイズレイヤーを組み合わせ
                float noise1 = Mathf.PerlinNoise(vertex.x * 0.05f, vertex.z * 0.05f);
                float noise2 = Mathf.PerlinNoise(vertex.x * 0.1f, vertex.z * 0.1f) * 0.5f;
                float noise3 = Mathf.PerlinNoise(vertex.x * 0.2f, vertex.z * 0.2f) * 0.25f;

                float combinedNoise = (noise1 + noise2 + noise3) / 1.75f;

                // 法線方向に変形
                Vector3 normal = vertex.normalized;
                vertices[i] = vertex + normal * (combinedNoise - 0.5f) * intensity;
            }

            mesh.positions = vertices;
        }
    }
}

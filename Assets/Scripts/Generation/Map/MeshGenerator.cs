using UnityEngine;
using System.Collections.Generic;

namespace Vastcore.Generation
{
    /// <summary>
    /// 最小構成の地形メッシュ生成ユーティリティ
    /// </summary>
    public static class MeshGenerator
    {
        public static float[,] GenerateHeightmap(int resolution, float noiseScale, Vector2 offset)
        {
            float[,] samples = new float[resolution, resolution];

            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float sampleX = (offset.x + x) * noiseScale;
                    float sampleZ = (offset.y + z) * noiseScale;
                    samples[z, x] = Mathf.PerlinNoise(sampleX, sampleZ);
                }
            }

            return samples;
        }

        public static Mesh BuildMesh(float[,] heightmap, float tileSize, float heightScale)
        {
            int resolution = heightmap.GetLength(0);
            Vector3[] vertices = new Vector3[resolution * resolution];
            Vector2[] uvs = new Vector2[resolution * resolution];
            int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];

            int vertexIndex = 0;
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float normalizedX = x / (float)(resolution - 1);
                    float normalizedZ = z / (float)(resolution - 1);

                    float posX = (normalizedX - 0.5f) * tileSize;
                    float posZ = (normalizedZ - 0.5f) * tileSize;
                    float posY = heightmap[z, x] * heightScale;

                    vertices[vertexIndex] = new Vector3(posX, posY, posZ);
                    uvs[vertexIndex] = new Vector2(normalizedX, normalizedZ);
                    vertexIndex++;
                }
            }

            int triangleIndex = 0;
            for (int z = 0; z < resolution - 1; z++)
            {
                for (int x = 0; x < resolution - 1; x++)
                {
                    int topLeft = z * resolution + x;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + resolution;
                    int bottomRight = bottomLeft + 1;

                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = bottomRight;
                    triangles[triangleIndex++] = bottomLeft;

                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = topRight;
                    triangles[triangleIndex++] = bottomRight;
                }
            }

            var mesh = new Mesh
            {
                vertices = vertices,
                triangles = triangles,
                uv = uvs
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}

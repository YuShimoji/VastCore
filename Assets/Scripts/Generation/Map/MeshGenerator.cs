using UnityEngine;

namespace Vastcore.Generation
{
    public static class MeshGenerator
    {
        public static Mesh GenerateTerrainMesh(Texture2D heightMap, float heightMultiplier, AnimationCurve heightCurve)
        {
            if (heightMap == null)
            {
                Debug.LogError("HeightMap is null in MeshGenerator.");
                return new Mesh();
            }

            int width = heightMap.width;
            int height = heightMap.height;

            Vector3[] vertices = new Vector3[width * height];
            int[] triangles = new int[(width - 1) * (height - 1) * 6];
            Vector2[] uvs = new Vector2[width * height];

            int triangleIndex = 0;
            int vertexIndex = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float grayValue = heightMap.GetPixel(x, y).grayscale;
                    float evaluatedHeight = heightCurve.Evaluate(grayValue);

                    vertices[vertexIndex] = new Vector3(x, evaluatedHeight * heightMultiplier, y);
                    uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);

                    if (x < width - 1 && y < height - 1)
                    {
                        AddTriangle(triangles, triangleIndex, vertexIndex, vertexIndex + width + 1, vertexIndex + width);
                        triangleIndex += 3;
                        AddTriangle(triangles, triangleIndex, vertexIndex, vertexIndex + 1, vertexIndex + width + 1);
                        triangleIndex += 3;
                    }
                    vertexIndex++;
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.name = "Generated Terrain Mesh";

            return mesh;
        }

        private static void AddTriangle(int[] triangles, int index, int a, int b, int c)
        {
            triangles[index] = a;
            triangles[index + 1] = b;
            triangles[index + 2] = c;
        }
    }
}
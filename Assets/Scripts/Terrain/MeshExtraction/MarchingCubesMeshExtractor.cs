using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Vastcore.WorldGen.Common;

namespace Vastcore.Terrain.MeshExtraction
{
    /// <summary>
    /// CPU 実装の簡易等値面抽出器。
    /// 実装は立方体を四面体へ分割する Marching Tetrahedra 方式。
    /// </summary>
    public sealed class MarchingCubesMeshExtractor : IMeshExtractor
    {
        private static readonly Vector3[] CubeCornerOffsets =
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(1f, 0f, 1f),
            new Vector3(0f, 0f, 1f),
            new Vector3(0f, 1f, 0f),
            new Vector3(1f, 1f, 0f),
            new Vector3(1f, 1f, 1f),
            new Vector3(0f, 1f, 1f)
        };

        // 立方体の対角 (0-6) を軸に 6 四面体へ分割
        private static readonly int[,] Tetrahedra =
        {
            { 0, 5, 1, 6 },
            { 0, 1, 2, 6 },
            { 0, 2, 3, 6 },
            { 0, 3, 7, 6 },
            { 0, 7, 4, 6 },
            { 0, 4, 5, 6 }
        };

        /// <inheritdoc />
        public Mesh ExtractMesh(DensityGrid grid, float isoLevel, float voxelSize)
        {
            if (grid == null || grid.Resolution < 2)
                return null;

            int cellCount = grid.Resolution - 1;
            float cellSize = Mathf.Max(0.0001f, voxelSize);

            List<Vector3> vertices = new List<Vector3>(8192);
            List<int> triangles = new List<int>(12288);

            Vector3[] cornerPositions = new Vector3[8];
            float[] cornerValues = new float[8];

            for (int z = 0; z < cellCount; z++)
            {
                for (int y = 0; y < cellCount; y++)
                {
                    for (int x = 0; x < cellCount; x++)
                    {
                        Vector3 cellOrigin = new Vector3(x, y, z) * cellSize;

                        for (int c = 0; c < 8; c++)
                        {
                            Vector3 p = cellOrigin + CubeCornerOffsets[c] * cellSize;
                            cornerPositions[c] = p;

                            int gx = x + (int)CubeCornerOffsets[c].x;
                            int gy = y + (int)CubeCornerOffsets[c].y;
                            int gz = z + (int)CubeCornerOffsets[c].z;
                            cornerValues[c] = grid[gx, gy, gz];
                        }

                        for (int t = 0; t < 6; t++)
                        {
                            PolygoniseTetra(
                                cornerPositions,
                                cornerValues,
                                Tetrahedra[t, 0],
                                Tetrahedra[t, 1],
                                Tetrahedra[t, 2],
                                Tetrahedra[t, 3],
                                isoLevel,
                                vertices,
                                triangles);
                        }
                    }
                }
            }

            if (triangles.Count == 0 || vertices.Count == 0)
                return null;

            Mesh mesh = new Mesh();
            if (vertices.Count > 65000)
                mesh.indexFormat = IndexFormat.UInt32;

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void PolygoniseTetra(
            Vector3[] cubePos,
            float[] cubeVal,
            int i0,
            int i1,
            int i2,
            int i3,
            float iso,
            List<Vector3> verts,
            List<int> tris)
        {
            int[] ids = { i0, i1, i2, i3 };
            bool[] inside = new bool[4];
            int insideCount = 0;
            for (int i = 0; i < 4; i++)
            {
                inside[i] = cubeVal[ids[i]] >= iso;
                if (inside[i]) insideCount++;
            }

            if (insideCount == 0 || insideCount == 4)
                return;

            List<int> insideIds = new List<int>(4);
            List<int> outsideIds = new List<int>(4);
            for (int i = 0; i < 4; i++)
            {
                if (inside[i]) insideIds.Add(ids[i]);
                else outsideIds.Add(ids[i]);
            }

            if (insideCount == 1 || insideCount == 3)
            {
                bool invert = insideCount == 3;
                int solid = invert ? outsideIds[0] : insideIds[0];
                List<int> others = invert ? insideIds : outsideIds;

                Vector3 p0 = VertexLerp(iso, cubePos[solid], cubePos[others[0]], cubeVal[solid], cubeVal[others[0]]);
                Vector3 p1 = VertexLerp(iso, cubePos[solid], cubePos[others[1]], cubeVal[solid], cubeVal[others[1]]);
                Vector3 p2 = VertexLerp(iso, cubePos[solid], cubePos[others[2]], cubeVal[solid], cubeVal[others[2]]);

                if (invert)
                    AddTriangle(verts, tris, p0, p2, p1);
                else
                    AddTriangle(verts, tris, p0, p1, p2);
                return;
            }

            if (insideCount == 2)
            {
                int a = insideIds[0];
                int b = insideIds[1];
                int c = outsideIds[0];
                int d = outsideIds[1];

                Vector3 p0 = VertexLerp(iso, cubePos[a], cubePos[c], cubeVal[a], cubeVal[c]);
                Vector3 p1 = VertexLerp(iso, cubePos[a], cubePos[d], cubeVal[a], cubeVal[d]);
                Vector3 p2 = VertexLerp(iso, cubePos[b], cubePos[c], cubeVal[b], cubeVal[c]);
                Vector3 p3 = VertexLerp(iso, cubePos[b], cubePos[d], cubeVal[b], cubeVal[d]);

                AddTriangle(verts, tris, p0, p1, p2);
                AddTriangle(verts, tris, p2, p1, p3);
            }
        }

        private static Vector3 VertexLerp(float iso, Vector3 p1, Vector3 p2, float v1, float v2)
        {
            float delta = v2 - v1;
            if (Mathf.Abs(delta) < 1e-6f)
                return p1;
            float t = (iso - v1) / delta;
            t = Mathf.Clamp01(t);
            return Vector3.Lerp(p1, p2, t);
        }

        private static void AddTriangle(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b, Vector3 c)
        {
            int index = verts.Count;
            verts.Add(a);
            verts.Add(b);
            verts.Add(c);
            tris.Add(index);
            tris.Add(index + 1);
            tris.Add(index + 2);
        }
    }
}

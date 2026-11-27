using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.ModernCity.Editor
{
    // One mesh per module or prop. Faces share a submesh when they use the same material.
    // UV distances are in metres so different parts can use the same tiling material.
    internal sealed class CityModel
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector2> uv = new List<Vector2>();
        private readonly List<Material> materials = new List<Material>();
        private readonly List<List<int>> triangles = new List<List<int>>();
        private readonly List<(Vector3 centre, Vector3 size, Quaternion rotation)> colliders =
            new List<(Vector3, Vector3, Quaternion)>();

        internal void Box(Material material, Vector3 centre, Vector3 size, bool solid = false)
        {
            Box(material, centre, size, Quaternion.identity, solid);
        }

        internal void Box(Material material, Vector3 centre, Vector3 size, Quaternion rotation, bool solid = false)
        {
            Vector3 half = size * 0.5f;
            Vector3 Corner(float x, float y, float z) =>
                centre + rotation * new Vector3(x * half.x, y * half.y, z * half.z);
            var nnn = Corner(-1, -1, -1);
            var nnp = Corner(-1, -1, 1);
            var npn = Corner(-1, 1, -1);
            var npp = Corner(-1, 1, 1);
            var pnn = Corner(1, -1, -1);
            var pnp = Corner(1, -1, 1);
            var ppn = Corner(1, 1, -1);
            var ppp = Corner(1, 1, 1);
            Quad(material, nnn, npn, ppn, pnn);
            Quad(material, pnp, ppp, npp, nnp);
            Quad(material, npn, npp, ppp, ppn);
            Quad(material, nnp, nnn, pnn, pnp);
            Quad(material, nnn, nnp, npp, npn);
            Quad(material, pnn, ppn, ppp, pnp);
            if (solid)
                CollisionBox(centre, size, rotation);
        }

        internal void CollisionBox(Vector3 centre, Vector3 size) =>
            CollisionBox(centre, size, Quaternion.identity);

        // Independent face, edge and corner normals leave a highlight on the chamfer.
        internal void BeveledBox(Material material, Vector3 centre, Vector3 size, float bevel, bool solid = false)
        {
            Vector3 half = size * 0.5f;
            float edge = Mathf.Min(bevel, 0.95f * Mathf.Min(half.x, Mathf.Min(half.y, half.z)));
            Vector3 inner = half - Vector3.one * edge;

            Vector3 Corner(Vector3 signs, int face)
            {
                Vector3 extents = inner;
                extents[face] = half[face];
                return centre + Vector3.Scale(signs, extents);
            }

            bool FacesOut(Vector3 a, Vector3 b, Vector3 c) =>
                Vector3.Dot(Vector3.Cross(b - a, c - a), (a + b + c) / 3f - centre) > 0;

            void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                if (FacesOut(a, b, c))
                    Quad(material, a, b, c, d);
                else
                    Quad(material, a, d, c, b);
            }

            for (int axis = 0; axis < 3; axis++)
            for (int side = -1; side <= 1; side += 2)
            {
                int u = (axis + 1) % 3;
                int v = (axis + 2) % 3;
                Vector3 signs = -Vector3.one;
                signs[axis] = side;
                var a = Corner(signs, axis);
                signs[u] = 1;
                var b = Corner(signs, axis);
                signs[v] = 1;
                var c = Corner(signs, axis);
                signs[u] = -1;
                Face(a, b, c, Corner(signs, axis));
            }

            for (int a = 0; a < 3; a++)
            for (int b = a + 1; b < 3; b++)
            for (int firstSide = -1; firstSide <= 1; firstSide += 2)
            for (int secondSide = -1; secondSide <= 1; secondSide += 2)
            {
                Vector3 signs = -Vector3.one;
                signs[a] = firstSide;
                signs[b] = secondSide;
                var startA = Corner(signs, a);
                var startB = Corner(signs, b);
                signs[3 - a - b] = 1;
                Face(startA, startB, Corner(signs, b), Corner(signs, a));
            }

            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                var signs = new Vector3(x, y, z);
                var a = Corner(signs, 0);
                var b = Corner(signs, 1);
                var c = Corner(signs, 2);
                if (FacesOut(a, b, c))
                    Triangle(material, a, b, c);
                else
                    Triangle(material, a, c, b);
            }
            if (solid)
                CollisionBox(centre, size);
        }

        internal void CollisionBox(Vector3 centre, Vector3 size, Quaternion rotation)
        {
            colliders.Add((centre, size, rotation));
        }

        internal void Quad(Material material, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int first = vertices.Count;
            var indices = Indices(material);
            indices.Add(first);
            indices.Add(first + 1);
            indices.Add(first + 2);
            indices.Add(first);
            indices.Add(first + 2);
            indices.Add(first + 3);
            float width = Vector3.Distance(a, d);
            float height = Vector3.Distance(a, b);
            Add(a, Vector2.zero);
            Add(b, new Vector2(0, height));
            Add(c, new Vector2(width, height));
            Add(d, new Vector2(width, 0));
        }

        internal void Tube(Material material, Vector3 start, Vector3 end, float radius, int sides = 10)
        {
            Frustum(material, start, end, radius, radius, sides);
        }

        internal void Frustum(Material material, Vector3 start, Vector3 end, float startRadius, float endRadius,
            int sides = 10)
        {
            Vector3 axis = (end - start).normalized;
            Vector3 tangent = Vector3.Cross(axis, Mathf.Abs(axis.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            Vector3 bitangent = Vector3.Cross(axis, tangent);
            Vector3 Radial(int index)
            {
                float angle = index * (2 * Mathf.PI / sides);
                return Mathf.Cos(angle) * tangent + Mathf.Sin(angle) * bitangent;
            }

            for (int i = 0; i < sides; i++)
            {
                Vector3 a = Radial(i);
                Vector3 b = Radial(i + 1);
                Vector3 lowerA = start + a * startRadius;
                Vector3 lowerB = start + b * startRadius;
                Vector3 upperA = end + a * endRadius;
                Vector3 upperB = end + b * endRadius;
                Quad(material, lowerA, lowerB, upperB, upperA);
                Triangle(material, start, lowerB, lowerA);
                Triangle(material, end, upperA, upperB);
            }
        }

        internal void Ellipsoid(Material material, Vector3 centre, Vector3 radius, int sides = 10, int rings = 5)
        {
            List<int> indices = Indices(material);
            int first = vertices.Count;
            for (int row = 0; row <= rings; row++)
            {
                float latitude = Mathf.PI * (row / (float)rings - 0.5f);
                for (int column = 0; column <= sides; column++)
                {
                    float longitude = column * (2 * Mathf.PI / sides);
                    var point = new Vector3(Mathf.Cos(latitude) * Mathf.Cos(longitude),
                        Mathf.Sin(latitude), Mathf.Cos(latitude) * Mathf.Sin(longitude));
                    Add(centre + Vector3.Scale(radius, point),
                        new Vector2(column / (float)sides, row / (float)rings));
                    if (row == rings || column == sides)
                        continue;
                    int index = first + row * (sides + 1) + column;
                    int next = index + sides + 1;
                    if (row > 0)
                        AddTriangle(indices, index, next, index + 1);
                    if (row < rings - 1)
                        AddTriangle(indices, index + 1, next, next + 1);
                }
            }
        }

        private void Triangle(Material material, Vector3 a, Vector3 b, Vector3 c)
        {
            AddTriangle(Indices(material), vertices.Count, vertices.Count + 1, vertices.Count + 2);
            Add(a, Vector2.zero);
            Add(b, Vector2.up);
            Add(c, Vector2.right);
        }

        private static void AddTriangle(List<int> indices, int a, int b, int c)
        {
            indices.Add(a);
            indices.Add(b);
            indices.Add(c);
        }

        private void Add(Vector3 vertex, Vector2 coordinate)
        {
            vertices.Add(vertex);
            uv.Add(coordinate);
        }

        private List<int> Indices(Material material)
        {
            int index = materials.IndexOf(material);
            if (index >= 0)
                return triangles[index];
            var newSubmesh = new List<int>();
            materials.Add(material);
            triangles.Add(newSubmesh);
            return newSubmesh;
        }

        internal Mesh CreateMesh(string name)
        {
            if (vertices.Count == 0)
                throw new InvalidOperationException("Empty city mesh: " + name);
            var mesh = new Mesh { name = name };
            if (vertices.Count > ushort.MaxValue)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.subMeshCount = triangles.Count;
            for (int i = 0; i < triangles.Count; i++)
                mesh.SetTriangles(triangles[i], i, false);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            return mesh;
        }

        internal MeshRenderer Attach(GameObject root, Mesh mesh)
        {
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials.ToArray();
            renderer.receiveShadows = true;
            for (int i = 0; i < colliders.Count; i++)
            {
                var (centre, size, rotation) = colliders[i];
                if (rotation == Quaternion.identity)
                {
                    var box = root.AddComponent<BoxCollider>();
                    box.center = centre;
                    box.size = size;
                }
                else
                {
                    var child = new GameObject($"Angled collider {i + 1}");
                    child.transform.SetParent(root.transform, false);
                    child.transform.SetLocalPositionAndRotation(centre, rotation);
                    child.AddComponent<BoxCollider>().size = size;
                }
            }
            return renderer;
        }
    }
}

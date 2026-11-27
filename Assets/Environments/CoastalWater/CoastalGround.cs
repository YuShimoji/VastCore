using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Vastcore.Water;

namespace Vastcore.CoastalWater
{
    internal sealed class CoastalGround : IDisposable
    {
        private readonly GameObject root;
        private readonly Shoreline shore;
        private readonly float headlands;
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private static readonly int TextureId = Shader.PropertyToID("_Surface");
        private static readonly int TurfId = Shader.PropertyToID("_Turf");
        private static readonly int KindId = Shader.PropertyToID("_Kind");
        private static readonly int ScaleId = Shader.PropertyToID("_Scale");
        private static readonly int BedId = Shader.PropertyToID("_CrossSection");

        internal Transform Transform => root.transform;

        internal CoastalGround(Scene scene, Shoreline shore, float headlands, Shader shader,
            Texture2D sand, Texture2D asphalt, Texture2D stone, Texture2D turf)
        {
            this.shore = shore;
            this.headlands = headlands;
            root = new GameObject("Coastal environment");
            SceneManager.MoveGameObjectToScene(root, scene);

            Material land = MaterialFor(shader, sand, turf, 0, 2.1f);
            Material paving = MaterialFor(shader, asphalt, turf, 1, 2.2f);
            Material wall = MaterialFor(shader, stone, turf, 2, 2.5f);
            Material rocks = MaterialFor(shader, stone, turf, 3, 1.7f);
            float outerRoad = shore.BarrierWidth + shore.ReceivingWidth;
            if (shore.Bank == BankType.Barrier)
            {
                MakeStrip("Seawall", wall, new Vector2[]
                {
                    new Vector2(0f, shore.SeaLevel - 5.2f),
                    new Vector2(0f, shore.SeaLevel + shore.CrestHeight - 0.12f),
                    new Vector2(0.11f, shore.SeaLevel + shore.CrestHeight),
                    new Vector2(shore.BarrierWidth - 0.08f, shore.SeaLevel + shore.CrestHeight),
                    new Vector2(shore.BarrierWidth, shore.SeaLevel + shore.CrestHeight - 0.10f),
                    new Vector2(shore.BarrierWidth, shore.ReceivingY(0f))
                });
                MakeStrip("Asphalt and lane paint", paving, new Vector2[]
                {
                    new Vector2(shore.BarrierWidth, shore.ReceivingY(0f)),
                    new Vector2(outerRoad, shore.ReceivingY(0f))
                });
            }

            float start = shore.Bank == BankType.Barrier ? outerRoad : -11f;
            var distances = new List<float> { start };
            while (distances[distances.Count - 1] < 360f)
            {
                float d = distances[distances.Count - 1];
                distances.Add(d + (d < 24f ? 1.3f : d < 95f ? 3.8f : 8.5f));
            }
            var section = new Vector2[distances.Count];
            for (int i = 0; i < section.Length; i++) section[i].x = distances[i];
            MakeStrip("Beach and headlands", land, section, terrain: true);
            MakeRocks(rocks);
        }

        public void Dispose()
        {
            if (!root) return;
            root.SetActive(false);
            Release(root);
            foreach (UnityEngine.Object item in owned) Release(item);
            owned.Clear();
        }

        private static void Release(UnityEngine.Object item)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(item);
            else UnityEngine.Object.DestroyImmediate(item);
        }

        private Material MaterialFor(Shader shader, Texture2D source, Texture2D turf, int kind, float scale)
        {
            var material = new Material(shader);
            if (source) material.SetTexture(TextureId, source);
            if (turf) material.SetTexture(TurfId, turf);
            material.SetFloat(KindId, kind);
            material.SetFloat(ScaleId, scale);
            material.SetVector(BedId, new Vector4(shore.SeaLevel, shore.BarrierWidth, shore.ReceivingWidth,
                shore.Bank == BankType.Barrier ? 1f : 0f));
            owned.Add(material);
            return material;
        }

        private float Elevation(float d, float z)
        {
            bool barrier = shore.Bank == BankType.Barrier;
            float start = barrier ? shore.BarrierWidth + shore.ReceivingWidth : Mathf.Max(12f, shore.ReceivingWidth);
            if (!barrier && d <= start) return shore.ReceivingY(d);
            float inland = Mathf.Max(0f, d - start);
            float join = shore.ReceivingY(start);
            float shoulder = 1f - Mathf.Exp(-inland * 0.037f);
            float rise = Mathf.Min(39f, inland * 0.36f);
            float ridge = 0.64f + 0.29f * Mathf.Sin(z * 0.031f - d * 0.048f)
                + 0.16f * Mathf.Sin(z * 0.081f + d * 0.034f) + 0.074f * Mathf.Sin(z * 0.23f - d * 0.21f);
            float gully = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(z * 0.049f + d * 0.049f)), 9f);
            float tooth = (Mathf.Sin(d * 1.08f + z * 0.93f) + Mathf.Sin(d * 0.72f - z * 0.63f)) * 0.34f;
            return join + (ridge * rise * headlands - gully * rise * 0.21f + tooth) * shoulder;
        }

        private void MakeStrip(string name, Material material, Vector2[] section, bool terrain = false)
        {
            var vertices = new List<Vector3>();
            var data = new List<Vector2>();
            var indices = new List<int>();
            float z = shore.StartZ;
            int rows = 0;
            while (true)
            {
                foreach (Vector2 point in section)
                {
                    float y = terrain ? Elevation(point.x, z) : point.y;
                    vertices.Add(new Vector3(shore.X(point.x, z), y, z));
                    data.Add(new Vector2(point.x, z));
                }
                rows++;
                if (z >= shore.EndZ) break;
                z = Mathf.Min(shore.EndZ, z + (z < 100f ? 1.2f : z < 240f ? 2.5f : 5f));
            }
            for (int r = 0; r < rows - 1; r++)
            for (int c = 0; c < section.Length - 1; c++)
            {
                int start = r * section.Length + c;
                int next = start + section.Length;
                indices.Add(start); indices.Add(next); indices.Add(start + 1);
                indices.Add(start + 1); indices.Add(next); indices.Add(next + 1);
            }
            AddMesh(name, material, vertices, data, indices);
        }

        private void MakeRocks(Material material)
        {
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var indices = new List<int>();
            var random = new System.Random(1827);
            bool barrier = shore.Bank == BankType.Barrier;
            int count = barrier ? 340 : 860;
            for (int i = 0; i < count; i++)
            {
                float z = Mathf.Lerp(Mathf.Max(shore.StartZ, -24f), Mathf.Min(shore.EndZ, 134f), (float)random.NextDouble());
                float reach = (float)random.NextDouble();
                float d = barrier ? shore.BarrierWidth + shore.ReceivingWidth + Mathf.Pow(reach, 1.7f) * 36f
                    : Mathf.Lerp(1.2f, 27f, Mathf.Pow(reach, 1.55f));
                float size = Mathf.Lerp(0.029f, barrier ? 1.05f : 0.73f, Mathf.Pow((float)random.NextDouble(), 4.6f));
                float flatten = Mathf.Lerp(0.17f, 0.65f, (float)random.NextDouble());
                float yaw = (float)random.NextDouble() * Mathf.PI * 2f;
                float height = Elevation(d, z);
                Vector3 center = new Vector3(shore.X(d, z), height - size * 0.047f, z);
                AddRock(center, d, size, flatten, yaw, vertices, uv, indices);
            }
            AddMesh("Shingle and shore rocks", material, vertices, uv, indices);
        }

        private static void AddRock(Vector3 center, float d, float size, float flatten, float yaw,
            List<Vector3> vertices, List<Vector2> uv, List<int> indices)
        {
            const int sides = 7;
            int start = vertices.Count;
            vertices.Add(center + new Vector3(size * 0.08f, size * flatten, -size * 0.16f));
            uv.Add(new Vector2(d, center.z));
            for (int ring = 0; ring < 2; ring++)
            for (int side = 0; side < sides; side++)
            {
                float a = side * (2f * Mathf.PI / sides) + yaw + ring * 0.18f;
                float radius = size * (0.73f + Mathf.Sin(a * 3f + yaw) * 0.17f + ring * 0.29f);
                float y = size * (ring == 0 ? flatten * (0.71f + 0.22f * Mathf.Sin(a * 2f)) : -0.06f);
                vertices.Add(center + new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius * 0.76f));
                uv.Add(new Vector2(d, center.z));
            }
            for (int i = 0; i < sides; i++)
            {
                int a = start + 1 + i;
                int b = start + 1 + (i + 1) % sides;
                indices.Add(start); indices.Add(b); indices.Add(a);
                indices.Add(a); indices.Add(b); indices.Add(a + sides);
                indices.Add(b); indices.Add(b + sides); indices.Add(a + sides);
            }
        }

        private void AddMesh(string name, Material material, List<Vector3> vertices, List<Vector2> values, List<int> indices)
        {
            var mesh = new Mesh { name = name };
            if (vertices.Count > ushort.MaxValue) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, values);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();
            var item = new GameObject(name);
            item.transform.SetParent(root.transform, false);
            item.AddComponent<MeshFilter>().sharedMesh = mesh;
            item.AddComponent<MeshRenderer>().sharedMaterial = material;
            owned.Add(mesh);
        }
    }
}

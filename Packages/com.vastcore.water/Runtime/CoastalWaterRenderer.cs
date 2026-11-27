using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.Water
{
    /// <summary>Owns the meshes and materials for one local coast. Call Render with a shared water clock.</summary>
    public sealed class CoastalWaterRenderer : IDisposable
    {
        private const int SprayQuads = 2450;
        private static readonly int WaveId = Shader.PropertyToID("_Wave");
        private static readonly int MotionId = Shader.PropertyToID("_Motion");
        private static readonly int BankId = Shader.PropertyToID("_Bank");
        private static readonly int ShoreId = Shader.PropertyToID("_Shore");
        private static readonly int DeepId = Shader.PropertyToID("_DeepWater");
        private static readonly int ShallowId = Shader.PropertyToID("_ShallowWater");
        private static readonly int BedTintId = Shader.PropertyToID("_BedTint");
        private static readonly int RippleId = Shader.PropertyToID("_RippleNormal");
        private static readonly int FoamMaskId = Shader.PropertyToID("_FoamMask");
        private static readonly int TimeId = Shader.PropertyToID("_WaterTime");

        private readonly GameObject root;
        private readonly GameObject contacts;
        private readonly Mesh surfaceMesh;
        private readonly Mesh contactsMesh;
        private readonly Material waterMaterial;
        private readonly Material contactMaterial;
        private bool disposed;

        public CoastalWaterRenderer(Transform parent, Shoreline shore, Shader surface, Shader contact)
        {
            if (shore == null || shore.EndZ <= shore.StartZ) throw new ArgumentException("Shoreline needs a positive length.", nameof(shore));
            if (!surface || !contact) throw new ArgumentException("Assign both VastCore water shaders.");

            root = new GameObject("Water");
            root.transform.SetParent(parent, false);
            waterMaterial = new Material(surface) { name = "Sea surface (instance)" };
            contactMaterial = new Material(contact) { name = "Shore contact (instance)" };

            surfaceMesh = MakeSurface(shore, Alongshore(shore, 0.85f));
            contactsMesh = MakeContacts(shore, Alongshore(shore, 0.43f));
            AddMesh(root.transform, "Sea surface", surfaceMesh, waterMaterial);
            contacts = AddMesh(root.transform, "Breaker, wave runup and spray", contactsMesh, contactMaterial);

            Vector4 bank = new Vector4(shore.Bank == BankType.Barrier ? 1f : 0f, Mathf.Max(0.012f, shore.BeachSlope),
                Mathf.Max(0.1f, shore.CrestHeight), shore.ReceivingHeight);
            Vector4 section = new Vector4(shore.SeaLevel, Mathf.Max(0.1f, shore.BarrierWidth),
                Mathf.Max(0.5f, shore.ReceivingWidth), shore.SafeBreakerDistance);
            waterMaterial.SetVector(BankId, bank);
            waterMaterial.SetVector(ShoreId, section);
            waterMaterial.SetColor(BedTintId, shore.BedTint);
            contactMaterial.SetVector(BankId, bank);
            contactMaterial.SetVector(ShoreId, section);
        }

        public void Render(SeaState state, float seconds, bool flat = false)
        {
            if (disposed) throw new ObjectDisposedException(nameof(CoastalWaterRenderer));
            if (!state) throw new ArgumentNullException(nameof(state));

            float amplitude = flat ? 0f : state.SafeAmplitude;
            Vector2 direction = state.Direction;
            Vector4 wave = new Vector4(amplitude, state.WaveNumber, 2f * Mathf.PI / state.SafePeriod,
                flat ? 0f : Mathf.Clamp(state.Steepness, 0f, 0.85f));
            Vector4 motion = new Vector4(direction.x, direction.y, flat ? 0f : Mathf.Clamp01(state.Chop),
                flat ? 0f : Mathf.Clamp01(state.Foam));
            Apply(waterMaterial, state, wave, motion, seconds);
            Apply(contactMaterial, state, wave, motion, seconds);
            bool showContacts = amplitude > 0.001f;
            if (contacts.activeSelf != showContacts) contacts.SetActive(showContacts);
        }

        private static void Apply(Material material, SeaState state, Vector4 wave, Vector4 motion, float seconds)
        {
            material.SetVector(WaveId, wave);
            material.SetVector(MotionId, motion);
            material.SetColor(DeepId, state.DeepWater);
            material.SetColor(ShallowId, state.ShallowWater);
            material.SetTexture(RippleId, state.RippleNormal);
            material.SetTexture(FoamMaskId, state.FoamMask);
            material.SetFloat(TimeId, seconds);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            root.SetActive(false);
            Release(root);
            Release(surfaceMesh);
            Release(contactsMesh);
            Release(waterMaterial);
            Release(contactMaterial);
        }

        private static void Release(UnityEngine.Object item)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(item);
            else UnityEngine.Object.DestroyImmediate(item);
        }

        private static GameObject AddMesh(Transform parent, string name, Mesh mesh, Material material)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return obj;
        }

        private static float[] Alongshore(Shoreline shore, float nearSpacing)
        {
            var samples = new List<float> { shore.StartZ };
            float z = shore.StartZ;
            while (z < shore.EndZ)
            {
                float step = z < 105f ? nearSpacing : z < 240f ? 2f : 6f;
                z = Mathf.Min(z + step, shore.EndZ);
                samples.Add(z);
            }
            return samples.ToArray();
        }

        private static Mesh MakeSurface(Shoreline shore, float[] along)
        {
            var offsets = new List<float> { -2350f };
            float d = offsets[0];
            while (d < 0f)
            {
                float step = d < -280f ? 35f : d < -100f ? 6f : d < -25f ? 1.4f : 0.48f;
                d = Mathf.Min(0f, d + step);
                offsets.Add(d);
            }

            var vertices = new List<Vector3>(along.Length * offsets.Count);
            var uv = new List<Vector2>(vertices.Capacity);
            var triangles = new List<int>((along.Length - 1) * (offsets.Count - 1) * 6);
            foreach (float z in along)
            {
                foreach (float distance in offsets)
                {
                    vertices.Add(new Vector3(shore.X(distance, z), shore.SeaLevel, z));
                    uv.Add(new Vector2(distance, z));
                }
            }
            AddGrid(triangles, 0, offsets.Count, along.Length);
            return CreateMesh("Sea surface", vertices, uv, null, triangles, 16f);
        }

        private static Mesh MakeContacts(Shoreline shore, float[] along)
        {
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var parts = new List<Vector4>();
            var triangles = new List<int>();
            AddContactStrip(shore, along, 11, 0, vertices, uv, parts, triangles);
            if (shore.Bank == BankType.Barrier)
                AddContactStrip(shore, along, 6, 3, vertices, uv, parts, triangles);
            AddContactStrip(shore, along, 17, 1, vertices, uv, parts, triangles);

            var random = new System.Random(17273);
            float sprayEnd = Mathf.Min(shore.EndZ, shore.StartZ + 205f);
            for (int i = 0; i < SprayQuads; i++)
            {
                float r = (float)random.NextDouble();
                float z = Mathf.Lerp(shore.StartZ, sprayEnd, r);
                float seedA = (float)random.NextDouble();
                float seedB = (float)random.NextDouble();
                float seedC = (float)random.NextDouble();
                int start = vertices.Count;
                for (int j = 0; j < 4; j++)
                {
                    vertices.Add(new Vector3(shore.X(0f, z), shore.SeaLevel, z));
                    uv.Add(new Vector2(j % 2, j / 2));
                    parts.Add(new Vector4(2f, seedA, seedB, seedC));
                }
                AddGrid(triangles, start, 2, 2);
            }

            return CreateMesh("Shore contact", vertices, uv, parts, triangles, Mathf.Max(28f, shore.ReceivingWidth * 2f));
        }

        private static void AddContactStrip(Shoreline shore, float[] along, int columns, int mode,
            List<Vector3> vertices, List<Vector2> uv, List<Vector4> parts, List<int> triangles)
        {
            int start = vertices.Count;
            foreach (float z in along)
            {
                for (int i = 0; i < columns; i++)
                {
                    vertices.Add(new Vector3(shore.X(0f, z), shore.SeaLevel, z));
                    uv.Add(new Vector2(i / (float)(columns - 1), z));
                    parts.Add(new Vector4(mode, 0f, 0f, 0f));
                }
            }
            AddGrid(triangles, start, columns, along.Length);
        }

        private static void AddGrid(List<int> triangles, int start, int columns, int rows)
        {
            for (int row = 0; row < rows - 1; row++)
            for (int column = 0; column < columns - 1; column++)
            {
                int a = start + row * columns + column;
                int b = a + columns;
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
            }
        }

        private static Mesh CreateMesh(string name, List<Vector3> vertices, List<Vector2> uv,
            List<Vector4> parts, List<int> triangles, float boundsPadding)
        {
            var mesh = new Mesh { name = name };
            if (vertices.Count > ushort.MaxValue) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            if (parts != null) mesh.SetUVs(1, parts);
            mesh.SetTriangles(triangles, 0);
            Bounds bounds = mesh.bounds;
            bounds.Expand(boundsPadding);
            mesh.bounds = bounds;
            return mesh;
        }
    }
}

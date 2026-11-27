using System;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// RegionChunkData を Unity の Mesh / Texture2D / Material / MeshCollider として実体化するビュー。
    /// Floating Origin に対応し、破棄時に生成リソースを解放する。
    /// </summary>
    public sealed class RegionChunkView
    {
        public readonly RegionChunkData Data;
        public readonly GameObject Root;
        public readonly MeshFilter Filter;
        public readonly MeshRenderer Renderer;
        public MeshCollider Collider { get; private set; }

        private Mesh _mesh;
        private Texture2D _albedo;
        private Material _material;
        private RegionChunkData[] _coveredCells;
        private List<int> _visibleIndices;
        private bool _meshClipped;
        private JobHandle? _colliderBake;
        private Mesh _seamMesh;
        private MeshRenderer _seamRenderer;
        private readonly List<Vector3> _seamVertices = new List<Vector3>();
        private readonly List<Vector3> _seamNormals = new List<Vector3>();
        private readonly List<Vector2> _seamUv = new List<Vector2>();
        private readonly List<int> _seamIndices = new List<int>();

        private struct BakeCollider : IJob
        {
            public EntityId Mesh;
            public void Execute() => Physics.BakeMesh(Mesh, false);
        }

        private RegionChunkView(RegionChunkData data, GameObject root, MeshFilter filter,
            MeshRenderer renderer, Mesh mesh, Texture2D albedo, Material material)
        {
            Data = data;
            Root = root;
            Filter = filter;
            Renderer = renderer;
            _mesh = mesh;
            _albedo = albedo;
            _material = material;
        }

        public static RegionChunkView Create(Transform parent, RegionChunkData data, RegionWorldCoordinates coords)
        {
            var go = new GameObject("Chunk " + data.Id);
            go.transform.SetParent(parent, false);
            coords.WorldToLocal(data.MinEastM, 0f, data.MinSouthM, out float lx, out float ly, out float lz);
            go.transform.localPosition = new Vector3(lx, ly, lz);

            Mesh mesh = BuildChunkMesh(data);

            Texture2D albedo;
            if (data.AlbedoPixels != null && data.AlbedoSize > 0)
            {
                albedo = RegionMapTexture.Create(data.AlbedoPixels, data.AlbedoSize);
            }
            else
            {
                albedo = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                var c = new Color32(180, 175, 165, 255);
                albedo.SetPixels32(new[] { c, c, c, c });
                albedo.Apply();
            }
            albedo.name = "Chunk Albedo " + data.Id;
            albedo.hideFlags = HideFlags.DontSave;

            Shader shader = Resources.Load<Shader>(RegionTerrainView.ShaderResource);
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(shader) { name = "Chunk Mat " + data.Id, hideFlags = HideFlags.DontSave };
            material.SetTexture("_BaseMap", albedo);
            RegionBiomeSurfaceProfile profile = data.BiomeSurface ?? RegionBiomeSurface.ProfileFor(data.PrimaryBiome);
            material.SetColor("_RockColor", new Color(profile.CliffR, profile.CliffG, profile.CliffB, 1f));
            material.SetFloat("_BandAmount", RegionLookScene.BandAmount);
            material.EnableKeyword("REGION_BIOME_SURFACE");

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = data.Id.Ring == RegionChunkRing.Near
                ? ShadowCastingMode.On
                : ShadowCastingMode.Off;
            renderer.receiveShadows = true;

            var view = new RegionChunkView(data, go, filter, renderer, mesh, albedo, material);
            if (data.Spec != null && data.Spec.HasCollider)
                view._colliderBake = new BakeCollider { Mesh = mesh.GetEntityId() }.Schedule();

            return view;
        }

        internal void AttachColliderWhenReady()
        {
            if (!_colliderBake.HasValue || !_colliderBake.Value.IsCompleted) return;
            _colliderBake.Value.Complete();
            _colliderBake = null;
            Collider = Root.AddComponent<MeshCollider>();
            Collider.sharedMesh = _mesh;
        }

        /// <summary>
        /// スカート頂点を含む RegionChunkData から Unity Mesh を構築する。
        /// Core の (X 東, Y 上, Z 南) を Unity の (X 東, Y 上, Z 北) へ変換する。
        /// </summary>
        public static Mesh BuildChunkMesh(RegionChunkData chunk)
        {
            RegionLookPart part = chunk.MeshPart;
            int count = part.VertexCount;
            int n = chunk.Resolution;
            int gridCount = n * n;
            float[] p = part.Positions;
            float[] norm = part.Normals;

            var vertices = new Vector3[count];
            var normals = new Vector3[count];
            var uv = new Vector2[count];
            var colors = new Color32[count];

            for (int v = 0; v < count; v++)
            {
                vertices[v] = new Vector3(p[v * 3], p[v * 3 + 1], -p[v * 3 + 2]);
                normals[v] = new Vector3(norm[v * 3], norm[v * 3 + 1], -norm[v * 3 + 2]);
                uv[v] = new Vector2(part.Uv[v * 2], 1f - part.Uv[v * 2 + 1]);
                colors[v] = new Color32(
                    (byte)(Mathf.Clamp01(part.Sky[v]) * 255f),
                    (byte)(Mathf.Clamp01(part.Sun[v]) * 255f),
                    (byte)((1f - Mathf.Clamp01(part.Rock[v])) * 255f),
                    (byte)(Mathf.Clamp01(part.Tint[v]) * 255f));
            }

            var mesh = new Mesh { name = part.Name, indexFormat = IndexFormat.UInt32, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);

            if (part.Data != null)
            {
                RegionData data = part.Data;
                bool hasSandstone = data.MesaContactHeight != null;
                var biomeChannels = new Vector4[count];
                for (int v = 0; v < gridCount && v < count; v++)
                {
                    RegionBiomeSurface.ComputeTerrainChannels(data, v, hasSandstone,
                        out float bx, out float by, out float bz, out float bw);
                    biomeChannels[v] = new Vector4(bx, by, bz, bw);
                }
                // スカート頂点は対応する境界頂点の UV からセルを引いて設定する
                for (int v = gridCount; v < count; v++)
                {
                    int ci = Mathf.Clamp(Mathf.RoundToInt(part.Uv[v * 2] * (n - 1)), 0, n - 1);
                    int cj = Mathf.Clamp(Mathf.RoundToInt(part.Uv[v * 2 + 1] * (n - 1)), 0, n - 1);
                    biomeChannels[v] = biomeChannels[ci + cj * n];
                }
                mesh.SetUVs(2, biomeChannels);
            }

            mesh.SetColors(colors);
            mesh.SetTriangles(part.Indices, 0, true);
            return mesh;
        }

        public void ApplyOriginShift(Vector3 localDelta)
        {
            if (Root != null)
                Root.transform.localPosition += localDelta;
        }

        internal bool Overlaps(RegionChunkView other)
        {
            return Data.MinEastM < other.Data.MinEastM + other.Data.SizeMeters &&
                   Data.MinEastM + Data.SizeMeters > other.Data.MinEastM &&
                   Data.MinSouthM < other.Data.MinSouthM + other.Data.SizeMeters &&
                   Data.MinSouthM + Data.SizeMeters > other.Data.MinSouthM;
        }

        internal void UpdateTerrainCoverage(IEnumerable<RegionChunkView> loaded)
        {
            int cells = Data.Resolution - 1;
            if (_coveredCells != null) Array.Clear(_coveredCells, 0, _coveredCells.Length);
            bool anyCovered = false;
            foreach (RegionChunkView finer in loaded)
            {
                if (finer.Data.Id.Ring >= Data.Id.Ring || !Overlaps(finer)) continue;
                int firstX = CellBoundary(finer.Data.MinEastM - Data.MinEastM, cells);
                int afterX = CellBoundary(finer.Data.MinEastM + finer.Data.SizeMeters - Data.MinEastM, cells);
                int firstZ = CellBoundary(finer.Data.MinSouthM - Data.MinSouthM, cells);
                int afterZ = CellBoundary(finer.Data.MinSouthM + finer.Data.SizeMeters - Data.MinSouthM, cells);
                if (firstX == afterX || firstZ == afterZ) continue;

                // A completely covered tile remains loaded so it can serve as a fallback when the finer tile leaves.
                if (firstX == 0 && firstZ == 0 && afterX == cells && afterZ == cells)
                {
                    Renderer.enabled = false;
                    if (_seamRenderer != null) _seamRenderer.enabled = false;
                    return;
                }
                _coveredCells ??= new RegionChunkData[cells * cells];
                anyCovered = true;
                for (int z = firstZ; z < afterZ; z++)
                {
                    for (int x = firstX; x < afterX; x++)
                    {
                        int cell = z * cells + x;
                        if (_coveredCells[cell] == null || finer.Data.Id.Ring < _coveredCells[cell].Id.Ring)
                            _coveredCells[cell] = finer.Data;
                    }
                }
            }

            int[] allIndices = Data.MeshPart.Indices;
            if (!anyCovered)
            {
                if (_meshClipped) _mesh.SetTriangles(allIndices, 0, false);
                _meshClipped = false;
                Renderer.enabled = true;
                if (_seamRenderer != null) _seamRenderer.enabled = false;
                return;
            }

            _visibleIndices ??= new List<int>(allIndices.Length);
            _visibleIndices.Clear();
            for (int cell = 0; cell < _coveredCells.Length; cell++)
                if (_coveredCells[cell] == null) AppendQuad(allIndices, cell);
            Renderer.enabled = _visibleIndices.Count != 0;
            if (!Renderer.enabled)
            {
                if (_seamRenderer != null) _seamRenderer.enabled = false;
                return;
            }

            // The four skirt strips follow the corresponding outer row or column of surface quads.
            if (Data.Spec.SkirtDepthMeters > 0f)
            {
                int skirt = cells * cells;
                for (int edge = 0; edge < cells; edge++)
                {
                    if (_coveredCells[edge] == null) AppendQuad(allIndices, skirt + edge);
                    if (_coveredCells[(cells - 1) * cells + edge] == null) AppendQuad(allIndices, skirt + cells + edge);
                    if (_coveredCells[edge * cells] == null) AppendQuad(allIndices, skirt + 2 * cells + edge);
                    if (_coveredCells[edge * cells + cells - 1] == null) AppendQuad(allIndices, skirt + 3 * cells + edge);
                }
            }
            _mesh.SetTriangles(_visibleIndices, 0, false);
            _meshClipped = true;
            UpdateSeamMesh(cells);
        }

        private void UpdateSeamMesh(int cells)
        {
            _seamVertices.Clear();
            _seamNormals.Clear();
            _seamUv.Clear();
            _seamIndices.Clear();
            for (int z = 0; z < cells; z++)
            {
                for (int x = 0; x < cells; x++)
                {
                    int cell = z * cells + x;
                    if (_coveredCells[cell] != null) continue;
                    if (x > 0) AddSeam(_coveredCells[cell - 1], x, z, x, z + 1);
                    if (x + 1 < cells) AddSeam(_coveredCells[cell + 1], x + 1, z, x + 1, z + 1);
                    if (z > 0) AddSeam(_coveredCells[cell - cells], x, z, x + 1, z);
                    if (z + 1 < cells) AddSeam(_coveredCells[cell + cells], x, z + 1, x + 1, z + 1);
                }
            }
            if (_seamIndices.Count == 0)
            {
                if (_seamRenderer != null) _seamRenderer.enabled = false;
                return;
            }
            if (_seamMesh == null)
            {
                var seam = new GameObject("Terrain LOD seam", typeof(MeshFilter), typeof(MeshRenderer));
                seam.transform.SetParent(Root.transform, false);
                _seamMesh = new Mesh { name = Data.Id + " seams", hideFlags = HideFlags.DontSave };
                seam.GetComponent<MeshFilter>().sharedMesh = _seamMesh;
                _seamRenderer = seam.GetComponent<MeshRenderer>();
                _seamRenderer.sharedMaterial = _material;
                _seamRenderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            _seamMesh.Clear();
            _seamMesh.SetVertices(_seamVertices);
            _seamMesh.SetNormals(_seamNormals);
            _seamMesh.SetUVs(0, _seamUv);
            _seamMesh.SetTriangles(_seamIndices, 0);
            _seamRenderer.enabled = true;
        }

        private void AddSeam(RegionChunkData finer, int firstX, int firstZ, int lastX, int lastZ)
        {
            if (finer == null) return;
            int segments = Mathf.Max(1, Mathf.CeilToInt(Data.CellMeters / finer.CellMeters));
            int start = _seamVertices.Count;
            for (int index = 0; index <= segments; index++)
            {
                float t = index / (float)segments;
                float x = Mathf.Lerp(firstX * Data.CellMeters, lastX * Data.CellMeters, t);
                float z = Mathf.Lerp(firstZ * Data.CellMeters, lastZ * Data.CellMeters, t);
                double east = Data.MinEastM + x, south = Data.MinSouthM + z;
                // The surface meshes and the near collider stay intact; only vertical gaps are covered.
                _seamVertices.Add(new Vector3(x, Data.HeightAt(east, south), -z));
                _seamVertices.Add(new Vector3(x, finer.HeightAt(east, south), -z));
                var uv = new Vector2(x / Data.SizeMeters, 1f - z / Data.SizeMeters);
                _seamUv.Add(uv);
                _seamUv.Add(uv);
                _seamNormals.Add(Vector3.up);
                _seamNormals.Add(Vector3.up);
            }
            for (int index = start; index < start + 2 * segments; index += 2)
            {
                _seamIndices.Add(index);
                _seamIndices.Add(index + 1);
                _seamIndices.Add(index + 2);
                _seamIndices.Add(index + 2);
                _seamIndices.Add(index + 1);
                _seamIndices.Add(index + 3);
                _seamIndices.Add(index + 2);
                _seamIndices.Add(index + 1);
                _seamIndices.Add(index);
                _seamIndices.Add(index + 3);
                _seamIndices.Add(index + 1);
                _seamIndices.Add(index + 2);
            }
        }

        private int CellBoundary(double offset, int cells)
        {
            return Mathf.Clamp(Mathf.RoundToInt((float)(offset / Data.SizeMeters * cells)), 0, cells);
        }

        private void AppendQuad(int[] indices, int quad)
        {
            int start = quad * 6;
            for (int index = start; index < start + 6; index++) _visibleIndices.Add(indices[index]);
        }

        public void Dispose()
        {
            if (_colliderBake.HasValue)
            {
                _colliderBake.Value.Complete();
                _colliderBake = null;
            }
            if (Root != null)
            {
                Root.SetActive(false);
                UnityEngine.Object.Destroy(Root);
            }
            if (_mesh != null) UnityEngine.Object.Destroy(_mesh);
            if (_seamMesh != null) UnityEngine.Object.Destroy(_seamMesh);
            if (_albedo != null) UnityEngine.Object.Destroy(_albedo);
            if (_material != null) UnityEngine.Object.Destroy(_material);
            _mesh = null;
            _albedo = null;
            _material = null;
        }
    }
}

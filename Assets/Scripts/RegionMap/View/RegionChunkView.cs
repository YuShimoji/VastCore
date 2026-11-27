using System;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.RegionMap
{
    /// <summary>Instantiates streamed terrain, and joins the borders of its different mesh resolutions.</summary>
    public sealed class RegionChunkView
    {
        private static readonly ProfilerMarker CoverageMarker = new ProfilerMarker(ProfilerCategory.Scripts, "VastCore.Region.UpdateLod");
        internal struct Vertex
        {
            public Vector3 Position, Normal;
            public Color32 Color;
            public Vector2 Uv;
            public Vector4 Biome;

            public static Vertex Lerp(Vertex a, Vertex b, float t)
            {
                return new Vertex
                {
                    Position = Vector3.LerpUnclamped(a.Position, b.Position, t),
                    Normal = Vector3.LerpUnclamped(a.Normal, b.Normal, t),
                    Color = Color32.LerpUnclamped(a.Color, b.Color, t),
                    Uv = Vector2.LerpUnclamped(a.Uv, b.Uv, t),
                    Biome = Vector4.LerpUnclamped(a.Biome, b.Biome, t),
                };
            }
        }

        internal sealed class PreparedChunk
        {
            public RegionChunkData Data;
            public Vertex[] Vertices;
            public MeshChannels Channels;
            public byte[] Albedo;
        }

        internal sealed class MeshChannels
        {
            private readonly Vector3[] _positions, _normals;
            private readonly Vector2[] _uv;
            private readonly Vector4[] _biome;
            private readonly Color32[] _colors;

            public MeshChannels(IList<Vertex> vertices)
            {
                int count = vertices.Count;
                _positions = new Vector3[count];
                _normals = new Vector3[count];
                _uv = new Vector2[count];
                _biome = new Vector4[count];
                _colors = new Color32[count];
                for (int i = 0; i < count; i++)
                {
                    Vertex vertex = vertices[i];
                    _positions[i] = vertex.Position;
                    _normals[i] = vertex.Normal;
                    _uv[i] = vertex.Uv;
                    _biome[i] = vertex.Biome;
                    _colors[i] = vertex.Color;
                }
            }

            public void Apply(Mesh mesh)
            {
                // A smaller frontier can drop vertices referenced by the previous triangles.
                if (mesh.vertexCount > _positions.Length) mesh.Clear();
                mesh.SetVertices(_positions);
                mesh.SetNormals(_normals);
                mesh.SetUVs(0, _uv);
                mesh.SetUVs(2, _biome);
                mesh.SetColors(_colors);
            }
        }

        private struct BakeCollider : IJob
        {
            public EntityId Mesh;
            public void Execute() => Physics.BakeMesh(Mesh, false);
        }

        public readonly RegionChunkData Data;
        public readonly GameObject Root;
        public readonly MeshFilter Filter;
        public readonly MeshRenderer Renderer;
        public MeshCollider Collider { get; private set; }

        private readonly Mesh _mesh;
        private readonly Texture2D _albedo;
        private readonly Material _material;
        private readonly Vertex[] _sourceVertices;
        private JobHandle? _colliderBake;

        private RegionChunkData[] _coveredCells;
        private readonly List<RegionChunkData> _finerChunks = new List<RegionChunkData>();
        private readonly List<Vertex> _surfaceVertices = new List<Vertex>();
        private readonly List<int> _surfaceIndices = new List<int>();
        private readonly List<int> _cellBorder = new List<int>();
        private bool _surfaceChanged;

        private RegionChunkView(PreparedChunk prepared, GameObject root, MeshFilter filter,
            MeshRenderer renderer, Mesh mesh, Texture2D albedo, Material material)
        {
            Data = prepared.Data;
            Root = root;
            Filter = filter;
            Renderer = renderer;
            _mesh = mesh;
            _albedo = albedo;
            _material = material;
            if (Data.Id.Ring != RegionChunkRing.Near && (Data.Spec == null || !Data.Spec.HasCollider))
                _sourceVertices = prepared.Vertices;
        }

        /// <summary>Pack managed mesh and texture buffers before reaching Unity's main thread.</summary>
        internal static PreparedChunk Prepare(RegionChunkData data)
        {
            var vertices = new Vertex[data.MeshPart.VertexCount];
            for (int v = 0; v < vertices.Length; v++)
                vertices[v] = ReadVertex(data, v);
            return new PreparedChunk
            {
                Data = data,
                Vertices = vertices,
                Channels = new MeshChannels(vertices),
                Albedo = data.AlbedoPixels != null && data.AlbedoSize > 0
                    ? RegionMapTexture.FlipRows(data.AlbedoPixels, data.AlbedoSize)
                    : null,
            };
        }

        public static RegionChunkView Create(Transform parent, RegionChunkData data, RegionWorldCoordinates coords)
            => Create(parent, Prepare(data), coords);

        internal static RegionChunkView Create(Transform parent, PreparedChunk prepared, RegionWorldCoordinates coords)
        {
            RegionChunkData data = prepared.Data;
            var go = new GameObject("Chunk " + data.Id);
            go.transform.SetParent(parent, false);
            coords.WorldToLocal(data.MinEastM, 0f, data.MinSouthM, out float lx, out float ly, out float lz);
            go.transform.localPosition = new Vector3(lx, ly, lz);

            Mesh mesh = CreateMesh(data, prepared.Channels);
            Texture2D albedo;
            if (prepared.Albedo != null)
            {
                albedo = new Texture2D(data.AlbedoSize, data.AlbedoSize, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                };
                albedo.LoadRawTextureData(prepared.Albedo);
                albedo.Apply(false, true);
            }
            else
            {
                albedo = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                var c = new Color32(180, 175, 165, 255);
                albedo.SetPixels32(new[] { c, c, c, c });
                albedo.Apply(false, true);
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

            var view = new RegionChunkView(prepared, go, filter, renderer, mesh, albedo, material);
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

        public static Mesh BuildChunkMesh(RegionChunkData data) => CreateMesh(data, Prepare(data).Channels);

        private static Mesh CreateMesh(RegionChunkData data, MeshChannels channels)
        {
            var mesh = new Mesh { name = data.MeshPart.Name, indexFormat = IndexFormat.UInt32, hideFlags = HideFlags.DontSave };
            channels.Apply(mesh);
            mesh.SetTriangles(data.MeshPart.Indices, 0, true);
            return mesh;
        }

        private static Vertex ReadVertex(RegionChunkData chunk, int v)
        {
            RegionLookPart part = chunk.MeshPart;
            int n = chunk.Resolution;
            int source = v;
            if (v >= n * n)
            {
                int x = Mathf.Clamp(Mathf.RoundToInt(part.Uv[v * 2] * (n - 1)), 0, n - 1);
                int z = Mathf.Clamp(Mathf.RoundToInt(part.Uv[v * 2 + 1] * (n - 1)), 0, n - 1);
                source = x + z * n;
            }

            Vector4 biome = Vector4.zero;
            if (part.Data != null)
            {
                RegionBiomeSurface.ComputeTerrainChannels(part.Data, source, part.Data.MesaContactHeight != null,
                    out float bx, out float by, out float bz, out float bw);
                biome = new Vector4(bx, by, bz, bw);
            }

            return new Vertex
            {
                Position = new Vector3(part.Positions[v * 3], part.Positions[v * 3 + 1], -part.Positions[v * 3 + 2]),
                Normal = new Vector3(part.Normals[v * 3], part.Normals[v * 3 + 1], -part.Normals[v * 3 + 2]),
                Uv = new Vector2(part.Uv[v * 2], 1f - part.Uv[v * 2 + 1]),
                Color = new Color32(
                    (byte)(Mathf.Clamp01(part.Sky[v]) * 255f),
                    (byte)(Mathf.Clamp01(part.Sun[v]) * 255f),
                    (byte)((1f - Mathf.Clamp01(part.Rock[v])) * 255f),
                    (byte)(Mathf.Clamp01(part.Tint[v]) * 255f)),
                Biome = biome,
            };
        }

        public void ApplyOriginShift(Vector3 localDelta)
        {
            if (Root != null) Root.transform.localPosition += localDelta;
        }

        internal bool TouchesOrOverlaps(RegionChunkView other)
        {
            return Data.MinEastM <= other.Data.MinEastM + other.Data.SizeMeters &&
                   Data.MinEastM + Data.SizeMeters >= other.Data.MinEastM &&
                   Data.MinSouthM <= other.Data.MinSouthM + other.Data.SizeMeters &&
                   Data.MinSouthM + Data.SizeMeters >= other.Data.MinSouthM;
        }

        internal void UpdateTerrainCoverage(IEnumerable<RegionChunkView> loaded)
        {
            if (_sourceVertices == null) return;
            using var coverageScope = CoverageMarker.Auto();
            _finerChunks.Clear();
            foreach (RegionChunkView finer in loaded)
            {
                if (finer.Data.Id.Ring < Data.Id.Ring && TouchesOrOverlaps(finer))
                    _finerChunks.Add(finer.Data);
            }
            if (_finerChunks.Count == 0)
            {
                if (_surfaceChanged)
                {
                    new MeshChannels(_sourceVertices).Apply(_mesh);
                    _mesh.SetTriangles(Data.MeshPart.Indices, 0, true);
                    _surfaceChanged = false;
                }
                Renderer.enabled = true;
                return;
            }

            // Stable ordering gives the same height/attributes to a vertex shared by neighbouring coarse tiles.
            _finerChunks.Sort(CompareFinerChunks);
            int n = Data.Resolution;
            int cells = n - 1;
            _coveredCells ??= new RegionChunkData[cells * cells];
            Array.Clear(_coveredCells, 0, _coveredCells.Length);
            _surfaceVertices.Clear();
            _surfaceVertices.AddRange(_sourceVertices);
            _surfaceIndices.Clear();

            foreach (RegionChunkData finer in _finerChunks)
            {
                int firstX = CellBoundary(finer.MinEastM - Data.MinEastM, cells);
                int afterX = CellBoundary(finer.MinEastM + finer.SizeMeters - Data.MinEastM, cells);
                int firstZ = CellBoundary(finer.MinSouthM - Data.MinSouthM, cells);
                int afterZ = CellBoundary(finer.MinSouthM + finer.SizeMeters - Data.MinSouthM, cells);
                for (int z = firstZ; z < afterZ; z++)
                    for (int x = firstX; x < afterX; x++)
                        _coveredCells[z * cells + x] = finer;

                // Covered interior vertices are unused. Conform only the shared border vertices.
                for (int x = firstX; x <= afterX; x++)
                {
                    MatchVertex(finer, x + firstZ * n);
                    MatchVertex(finer, x + afterZ * n);
                }
                for (int z = firstZ + 1; z < afterZ; z++)
                {
                    MatchVertex(finer, firstX + z * n);
                    MatchVertex(finer, afterX + z * n);
                }
            }

            for (int z = 0; z < cells; z++)
            {
                for (int x = 0; x < cells; x++)
                {
                    int cell = x + z * cells;
                    if (_coveredCells[cell] != null) continue;
                    RegionChunkData north = FinerAtCell(x, z - 1, cells);
                    RegionChunkData east = FinerAtCell(x + 1, z, cells);
                    RegionChunkData south = FinerAtCell(x, z + 1, cells);
                    RegionChunkData west = FinerAtCell(x - 1, z, cells);
                    if (north == null && east == null && south == null && west == null)
                        AppendQuad(cell);
                    else
                        JoinQuad(x + z * n, n, north, east, south, west);
                }
            }
            Renderer.enabled = _surfaceIndices.Count != 0;
            if (!Renderer.enabled) return;
            AppendExposedSkirts(cells);
            new MeshChannels(_surfaceVertices).Apply(_mesh);
            _mesh.SetTriangles(_surfaceIndices, 0, true);
            _surfaceChanged = true;
        }

        private static int CompareFinerChunks(RegionChunkData a, RegionChunkData b)
        {
            int ring = b.Id.Ring.CompareTo(a.Id.Ring);
            if (ring != 0) return ring;
            int x = a.Id.X.CompareTo(b.Id.X);
            return x != 0 ? x : a.Id.Z.CompareTo(b.Id.Z);
        }

        private void MatchVertex(RegionChunkData finer, int index)
        {
            Vector3 at = _sourceVertices[index].Position;
            double east = Data.MinEastM + at.x, south = Data.MinSouthM - at.z;
            if (east < finer.MinEastM - 0.002 || east > finer.MinEastM + finer.SizeMeters + 0.002 ||
                south < finer.MinSouthM - 0.002 || south > finer.MinSouthM + finer.SizeMeters + 0.002)
                return;
            _surfaceVertices[index] = VertexAt(finer, at.x, -at.z);
        }

        private RegionChunkData FinerAtCell(int x, int z, int cells)
        {
            if (x >= 0 && x < cells && z >= 0 && z < cells)
                return _coveredCells[x + z * cells];

            double east = Data.MinEastM + (x + 0.5) * Data.CellMeters;
            double south = Data.MinSouthM + (z + 0.5) * Data.CellMeters;
            for (int i = _finerChunks.Count - 1; i >= 0; i--)
                if (_finerChunks[i].Contains(east, south)) return _finerChunks[i];
            return null;
        }

        private void JoinQuad(int a, int n, RegionChunkData north, RegionChunkData east,
            RegionChunkData south, RegionChunkData west)
        {
            int b = a + 1, c = a + n, d = c + 1;
            _cellBorder.Clear();
            AppendBorder(a, b, north);
            AppendBorder(b, d, east);
            AppendBorder(d, c, south);
            AppendBorder(c, a, west);

            int center = _surfaceVertices.Count;
            Vertex top = Vertex.Lerp(_surfaceVertices[a], _surfaceVertices[b], 0.5f);
            Vertex bottom = Vertex.Lerp(_surfaceVertices[c], _surfaceVertices[d], 0.5f);
            _surfaceVertices.Add(Vertex.Lerp(top, bottom, 0.5f));
            for (int i = 0; i < _cellBorder.Count; i++)
            {
                _surfaceIndices.Add(center);
                _surfaceIndices.Add(_cellBorder[i]);
                _surfaceIndices.Add(_cellBorder[(i + 1) % _cellBorder.Count]);
            }
        }

        private void AppendBorder(int first, int last, RegionChunkData finer)
        {
            _cellBorder.Add(first);
            if (finer == null) return;
            Vector3 from = _sourceVertices[first].Position, to = _sourceVertices[last].Position;
            bool eastwardAxis = from.x != to.x;
            double start = eastwardAxis ? Data.MinEastM + from.x : Data.MinSouthM - from.z;
            double end = eastwardAxis ? Data.MinEastM + to.x : Data.MinSouthM - to.z;
            double fineStart = eastwardAxis ? finer.MinEastM : finer.MinSouthM;
            int low = Math.Max(1, (int)Math.Floor((Math.Min(start, end) - fineStart + 0.002) / finer.CellMeters) + 1);
            int high = Math.Min(finer.Resolution - 2, (int)Math.Ceiling((Math.Max(start, end) - fineStart - 0.002) / finer.CellMeters) - 1);
            int direction = start < end ? 1 : -1;
            for (int i = 0; i <= high - low; i++)
            {
                double at = fineStart + (direction > 0 ? low + i : high - i) * (double)finer.CellMeters;
                float t = (float)((at - start) / (end - start));
                Vector3 position = Vector3.LerpUnclamped(from, to, t);
                _cellBorder.Add(_surfaceVertices.Count);
                _surfaceVertices.Add(VertexAt(finer, position.x, -position.z));
            }
        }

        private Vertex VertexAt(RegionChunkData finer, float x, float z)
        {
            double east = Data.MinEastM + x, south = Data.MinSouthM + z;
            float fx = Mathf.Clamp((float)((east - finer.MinEastM) / finer.CellMeters), 0, finer.Resolution - 1);
            float fz = Mathf.Clamp((float)((south - finer.MinSouthM) / finer.CellMeters), 0, finer.Resolution - 1);
            int ix = Math.Min((int)fx, finer.Resolution - 2), iz = Math.Min((int)fz, finer.Resolution - 2);
            int a = ix + iz * finer.Resolution;
            Vertex top = Vertex.Lerp(ReadVertex(finer, a), ReadVertex(finer, a + 1), fx - ix);
            Vertex bottom = Vertex.Lerp(ReadVertex(finer, a + finer.Resolution), ReadVertex(finer, a + finer.Resolution + 1), fx - ix);
            Vertex vertex = Vertex.Lerp(top, bottom, fz - iz);
            vertex.Position = new Vector3(x, finer.HeightAt(east, south), -z);
            vertex.Uv = new Vector2(x / Data.SizeMeters, 1f - z / Data.SizeMeters);
            return vertex;
        }

        private void AppendExposedSkirts(int cells)
        {
            if (Data.Spec.SkirtDepthMeters <= 0f) return;
            int skirt = cells * cells;
            for (int edge = 0; edge < cells; edge++)
            {
                if (_coveredCells[edge] == null && FinerAtCell(edge, -1, cells) == null) AppendQuad(skirt + edge);
                if (_coveredCells[(cells - 1) * cells + edge] == null && FinerAtCell(edge, cells, cells) == null)
                    AppendQuad(skirt + cells + edge);
                if (_coveredCells[edge * cells] == null && FinerAtCell(-1, edge, cells) == null)
                    AppendQuad(skirt + 2 * cells + edge);
                if (_coveredCells[edge * cells + cells - 1] == null && FinerAtCell(cells, edge, cells) == null)
                    AppendQuad(skirt + 3 * cells + edge);
            }
        }

        private int CellBoundary(double offset, int cells)
            => Mathf.Clamp(Mathf.RoundToInt((float)(offset / Data.SizeMeters * cells)), 0, cells);

        private void AppendQuad(int quad)
        {
            int[] indices = Data.MeshPart.Indices;
            int start = quad * 6;
            for (int index = start; index < start + 6; index++) _surfaceIndices.Add(indices[index]);
        }

        public void Dispose()
        {
            if (_colliderBake.HasValue) _colliderBake.Value.Complete();
            if (Root != null)
            {
                Root.SetActive(false);
                UnityEngine.Object.Destroy(Root);
            }
            if (_mesh != null) UnityEngine.Object.Destroy(_mesh);
            if (_albedo != null) UnityEngine.Object.Destroy(_albedo);
            if (_material != null) UnityEngine.Object.Destroy(_material);
        }
    }
}

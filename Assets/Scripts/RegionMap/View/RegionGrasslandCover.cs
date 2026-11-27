using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.RegionMap
{
    /// <summary>Grounded grass ribbons shared by the fixed look and the streamed ground camera.</summary>
    [ExecuteAlways]
    internal sealed class RegionGrasslandCover : MonoBehaviour
    {
        private const string ShaderResource = "RegionMap3D/RegionGrasslandCover";
        private const int TileSize = 16;
        private const float MeadowRadius = 88f;
        private const float DetailRadius = 34f;
        private const float ShortGrassRadius = 17f;
        private const int ShortGrassLayer = 2;
        private const int TilesPerFrame = 2;

        private sealed class Tile
        {
            public RegionLookPart Surface;
            public Mesh Mesh;
            public GameObject Object;
        }

        internal Camera Camera;
        private RegionChunkStreamer _streamer;
        private RegionLookScene _look;
        private Material _material;
        private bool _visible = true;
        private readonly Dictionary<Vector3Int, Tile> _tiles = new Dictionary<Vector3Int, Tile>();
        private readonly List<Vector3Int> _remove = new List<Vector3Int>();
        private readonly List<(Vector3Int Key, float Distance)> _pending = new List<(Vector3Int, float)>();
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector4> _bladeData = new List<Vector4>();
        private readonly List<Color32> _colors = new List<Color32>();
        private readonly List<int> _indices = new List<int>();

        internal static RegionGrasslandCover AddTo(Transform parent, RegionLookScene look)
        {
            var cover = Create(parent);
            cover._look = look;
            return cover;
        }

        internal static RegionGrasslandCover AddTo(Transform parent, Camera camera, RegionChunkStreamer streamer)
        {
            var cover = Create(parent);
            cover.Camera = camera;
            cover._streamer = streamer;
            return cover;
        }

        private static RegionGrasslandCover Create(Transform parent)
        {
            var holder = new GameObject("Grassland Cover") { hideFlags = HideFlags.DontSave };
            holder.transform.SetParent(parent, false);
            return holder.AddComponent<RegionGrasslandCover>();
        }

        private void OnEnable() => RenderPipelineManager.beginCameraRendering += Prepare;

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= Prepare;
            Clear();
            if (_material != null) Release(_material);
            _material = null;
        }

        internal void Clear()
        {
            foreach (Tile tile in _tiles.Values) Release(tile);
            _tiles.Clear();
        }

        internal void SetVisible(bool visible)
        {
            _visible = visible;
            foreach (Tile tile in _tiles.Values)
                if (tile.Object != null) tile.Object.SetActive(visible);
        }

        private void Prepare(ScriptableRenderContext context, Camera camera)
        {
            if (!_visible) return;
            if (camera != Camera && !(_look != null && camera.cameraType == CameraType.SceneView)) return;
            if (_look == null && (_streamer == null || _streamer.Coordinates == null)) return;
            Vector3 eye = camera.transform.position;
            RegionWorldCoordinates coordinates = _streamer != null ? _streamer.Coordinates : null;
            double originEast = coordinates?.OriginEastM ?? _look.Outer.Data.DevelopmentOrigin.X;
            double originSouth = coordinates?.OriginSouthM ?? _look.Outer.Data.DevelopmentOrigin.Y;
            double east = originEast + eye.x, south = originSouth - eye.z;
            float ground = _look != null ? _look.HeightAt(eye.x, -eye.z) : _streamer.SampleGroundHeight(east, south);
            float above = Mathf.Max(0f, eye.y - ground);

            _remove.Clear();
            foreach (var pair in _tiles)
            {
                Vector3Int key = pair.Key;
                Tile tile = pair.Value;
                float radius = Radius(key.z);
                if (Distance(key, east, south, above) >= radius * radius || Surface(key).Part != tile.Surface)
                {
                    _remove.Add(key);
                    Release(tile);
                }
                else if (tile.Object != null)
                {
                    PlaceTile(tile.Object, key, originEast, originSouth);
                }
            }
            foreach (Vector3Int key in _remove) _tiles.Remove(key);
            if (above >= MeadowRadius) return;

            _pending.Clear();
            for (int layer = 0; layer <= ShortGrassLayer; layer++)
            {
                float radius = Radius(layer);
                if (above >= radius) continue;
                for (int z = (int)Math.Floor((south - radius) / TileSize); z <= (int)Math.Floor((south + radius) / TileSize); z++)
                for (int x = (int)Math.Floor((east - radius) / TileSize); x <= (int)Math.Floor((east + radius) / TileSize); x++)
                {
                    var key = new Vector3Int(x, z, layer);
                    float distance = Distance(key, east, south, above);
                    if (distance < radius * radius && !_tiles.ContainsKey(key))
                        _pending.Add((key, distance));
                }
            }
            _pending.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            int remaining = _streamer != null ? TilesPerFrame : int.MaxValue;
            foreach (var request in _pending)
            {
                var surface = Surface(request.Key);
                if (surface.Part == null) continue;
                Tile tile = BuildTile(request.Key, surface.Part, surface.East, surface.South, surface.Cell);
                _tiles.Add(request.Key, tile);
                if (tile.Object == null) continue;
                PlaceTile(tile.Object, request.Key, originEast, originSouth);
                if (--remaining == 0) break;
            }
        }

        private (RegionLookPart Part, double East, double South, float Cell) Surface(Vector3Int key)
        {
            double east = (key.x + 0.5) * TileSize;
            double south = (key.y + 0.5) * TileSize;
            if (_streamer != null)
            {
                RegionChunkData chunk = _streamer.NearChunkAt(east, south);
                return chunk != null ? (chunk.MeshPart, chunk.MinEastM, chunk.MinSouthM, chunk.CellMeters) : default;
            }

            RegionPosition origin = _look.Outer.Data.DevelopmentOrigin;
            if (east < origin.X + _look.InnerMinX || east >= origin.X + _look.InnerMaxX ||
                south < origin.Y + _look.InnerMinZ || south >= origin.Y + _look.InnerMaxZ) return default;
            return (_look.InnerTerrain, origin.X, origin.Y, _look.InnerCell);
        }

        private static float Radius(int layer) => layer == ShortGrassLayer ? ShortGrassRadius
            : layer == 1 ? DetailRadius : MeadowRadius;

        private static float Distance(Vector3Int key, double east, double south, float above)
        {
            float x = Mathf.Max(0f, Mathf.Abs((float)((key.x + 0.5) * TileSize - east)) - TileSize * 0.5f);
            float z = Mathf.Max(0f, Mathf.Abs((float)((key.y + 0.5) * TileSize - south)) - TileSize * 0.5f);
            return x * x + z * z + above * above;
        }

        private static void PlaceTile(GameObject tile, Vector3Int key, double east, double south)
        {
            var offset = new Vector3((float)(key.x * (double)TileSize - east), 0f, (float)(south - key.y * (double)TileSize));
            if (tile.transform.localPosition != offset) tile.transform.localPosition = offset;
        }

        private Tile BuildTile(Vector3Int key, RegionLookPart part, double originEast, double originSouth, float cell)
        {
            var tile = new Tile { Surface = part };
            _vertices.Clear();
            _bladeData.Clear();
            _colors.Clear();
            _indices.Clear();
            RegionData data = part.Data;
            int layer = key.z;
            bool shortGrass = layer == ShortGrassLayer;
            int count = shortGrass ? 80 : layer == 1 ? 44 : 20;
            float spacing = (float)TileSize / count;
            uint salt = unchecked((uint)data.Spec.Seed * 2654435761u + 6947u + (uint)layer * 1124u);
            float east = key.x * (float)TileSize, south = key.y * (float)TileSize;
            float localX = (float)(east - originEast), localZ = (float)(south - originSouth);
            float firstX = part.Positions[0], firstZ = part.Positions[2];
            if (!HasGrassland(data, (localX - firstX) / cell, (localZ - firstZ) / cell, TileSize / cell))
                return tile;
            int n = data.Resolution;
            var props = _look?.PropsSlice;
            float trailEast = (float)(originEast + (props?.CratesCenterX ?? 0f));
            float trailSouth = (float)(originSouth + (props?.CratesCenterZ ?? 0f));
            for (int j = 0; j < count; j++)
            for (int i = 0; i < count; i++)
            {
                int ix = key.x * count + i, iz = key.y * count + j;
                float x = (i + RegionNoise.Hash01(ix, iz, salt)) * spacing;
                float z = (j + RegionNoise.Hash01(ix, iz, salt + 1)) * spacing;
                float px = localX + x, pz = localZ + z;
                float gx = (px - firstX) / cell, gz = (pz - firstZ) / cell;
                if (gx < 0f || gz < 0f || gx >= n - 1 || gz >= n - 1) continue;
                int v = Mathf.RoundToInt(gx) + Mathf.RoundToInt(gz) * n;
                if (data.Biome[v] != RegionBiome.Grassland || data.Height[v] <= 0f || data.LakeDepth[v] > 0f ||
                    (data.WaterCoverage?[v] ?? 0f) > 0.24f || (data.StructureCoverage?[v] ?? 0f) > 0.12f)
                    continue;
                float normalY = part.Normals[3 * v + 1];
                float patch = Patch(east + x, south + z);
                float moisture = Mathf.Clamp01((data.Moisture[v] - 0.14f) / 0.46f);
                float growth = RegionNoise.Smooth(0.16f, 0.83f, patch * 0.78f + moisture * 0.22f);
                float trail = props != null ? Trail(east + x - trailEast, south + z - trailSouth) : 0f;
                float coverage = Mathf.Lerp(shortGrass ? 0.76f : 0.47f, 0.98f, growth);
                float cover = coverage * RegionNoise.Smooth(0.76f, 0.96f, normalY) * (1f - trail);
                if (RegionNoise.Hash01(ix, iz, salt + 2) >= cover) continue;
                if (props != null && Mathf.Abs(east + x - trailEast) < 9f && Mathf.Abs(south + z - trailSouth) < 7f &&
                    OnRock(props, px, pz)) continue;
                if (!part.TryGridHeight(px, pz, (int)gx + (int)gz * (n - 1), out float height)) continue;
                float size = RegionNoise.Hash01(ix, iz, salt + 3);
                float bladeHeight = shortGrass ? Mathf.Lerp(0.085f, 0.18f, growth) : Mathf.Lerp(0.14f, 0.42f, growth);
                bladeHeight *= Mathf.Lerp(0.58f, 1.45f, size) * (1f - trail * 0.72f);
                var foot = new Vector3(x, height - 0.025f, -z);
                int blades = layer == 1 ? 7 : 3;
                for (int b = 0; b < blades; b++)
                {
                    uint bladeSalt = salt + (uint)(11 + 7 * b);
                    float variation = RegionNoise.Hash01(ix, iz, bladeSalt);
                    float azimuth = 6.2831853f * RegionNoise.Hash01(ix, iz, bladeSalt + 1);
                    float length = bladeHeight * Mathf.Lerp(0.60f, 1.36f, variation);
                    float width = shortGrass ? 0.022f : layer == 1 ? 0.019f : 0.066f;
                    bool dry = layer == 1 && variation > 0.958f;
                    var along = new Vector3(Mathf.Cos(azimuth), 0f, Mathf.Sin(azimuth));
                    Vector3 offset = along * (spacing * (variation - 0.5f) * 0.86f);
                    offset.y = (-part.Normals[3 * v] * offset.x + part.Normals[3 * v + 2] * offset.z) / normalY;
                    AddBlade(foot + offset, along,
                        dry ? Mathf.Min(0.79f, 0.22f + length * 1.1f) : length, width * (dry ? 0.31f : 0.65f + 0.62f * variation),
                        growth, variation, part.Sun[v], layer, dry);
                }
            }

            if (_indices.Count == 0) return tile;
            if (_material == null)
            {
                Shader shader = Resources.Load<Shader>(ShaderResource);
                if (shader == null || !shader.isSupported) return tile;
                _material = new Material(shader) { name = "Meadow grass", hideFlags = HideFlags.DontSave };
            }
            var mesh = new Mesh { name = "Grassland blades", hideFlags = HideFlags.DontSave };
            if (_vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(_vertices);
            mesh.SetUVs(0, _bladeData);
            mesh.SetColors(_colors);
            mesh.SetTriangles(_indices, 0);
            mesh.UploadMeshData(true);
            var go = new GameObject("Grass blades") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            tile.Mesh = mesh;
            tile.Object = go;
            return tile;
        }

        private static bool HasGrassland(RegionData data, float gridX, float gridZ, float span)
        {
            int n = data.Resolution;
            int lastX = Mathf.Min(n - 1, Mathf.CeilToInt(gridX + span));
            int lastZ = Mathf.Min(n - 1, Mathf.CeilToInt(gridZ + span));
            int firstX = Mathf.Max(0, Mathf.FloorToInt(gridX));
            for (int z = Mathf.Max(0, Mathf.FloorToInt(gridZ)); z <= lastZ; z++)
            for (int x = firstX; x <= lastX; x++)
                if (data.Biome[x + z * n] == RegionBiome.Grassland) return true;
            return false;
        }

        private void AddBlade(Vector3 root, Vector3 along, float height, float width,
            float growth, float variation, float sun, int layer, bool dry)
        {
            Vector3 side = new Vector3(along.z, 0f, -along.x) * (width * 0.5f);
            Vector3 lean = (along * (dry ? 0.15f : 0.68f) + new Vector3(0.20f, 0f, 0.13f)) * height;
            Vector3 tip = root + Vector3.up * height + lean;
            var color = new Color32((byte)(255 * growth), (byte)(255 * variation), (byte)(255 * sun), dry ? (byte)255 : (byte)0);
            var rootAndLayer = new Vector4(root.x, root.y, root.z, layer);
            int start = _vertices.Count;
            if (layer == ShortGrassLayer)
            {
                _vertices.Add(root - side);
                _vertices.Add(root + side);
                _vertices.Add(tip);
                for (int v = 0; v < 3; v++)
                {
                    _colors.Add(color);
                    _bladeData.Add(rootAndLayer);
                    _indices.Add(start + v);
                }
                return;
            }

            Vector3 mid = root + Vector3.up * (height * 0.61f) + lean * 0.29f;
            _vertices.Add(root - side * 0.6f);
            _vertices.Add(root + side * 0.6f);
            _vertices.Add(mid - side);
            _vertices.Add(mid + side);
            _vertices.Add(tip);
            for (int v = 0; v < 5; v++)
            {
                _colors.Add(color);
                _bladeData.Add(rootAndLayer);
            }
            _indices.Add(start);
            _indices.Add(start + 2);
            _indices.Add(start + 1);
            _indices.Add(start + 1);
            _indices.Add(start + 2);
            _indices.Add(start + 3);
            _indices.Add(start + 2);
            _indices.Add(start + 4);
            _indices.Add(start + 3);

            if (!dry) return;
            // A slim seed head, above the green leaf layer.
            int head = _vertices.Count;
            _vertices.Add(tip - Vector3.up * 0.055f - side * 2.6f);
            _vertices.Add(tip - Vector3.up * 0.060f + side * 2.6f);
            _vertices.Add(tip + Vector3.up * 0.035f + lean * 0.14f);
            for (int v = 0; v < 3; v++)
            {
                _colors.Add(color);
                _bladeData.Add(rootAndLayer);
                _indices.Add(head + v);
            }
        }

        private static bool OnRock(RegionPropProofSlice props, float x, float z)
        {
            foreach (RegionPropInstance prop in props.Instances)
            {
                if (prop.PropId != "nat_grassland_outcrop" && prop.PropId != "nat_dry_root_log") continue;
                float dx = x - prop.X, dz = z - prop.Z;
                float r = Mathf.Min(prop.DimensionsX * prop.ScaleX, prop.DimensionsZ * prop.ScaleZ) * 0.43f;
                if (dx * dx + dz * dz < r * r) return true;
            }
            return false;
        }

        // Match RegionGrasslandHabitat.hlsl so the meadow colour and the blades describe the same patches.
        private static float Patch(float x, float z)
            => 0.64f * Noise(x / 37f, z / 37f) + 0.36f * Noise(x / 8f + 17f, z / 8f - 9f);

        private static float Noise(float x, float z)
        {
            int a = Mathf.FloorToInt(x), b = Mathf.FloorToInt(z);
            float u = RegionNoise.Smooth(0f, 1f, x - a), v = RegionNoise.Smooth(0f, 1f, z - b);
            return Mathf.Lerp(Mathf.Lerp(RegionNoise.Hash01(a, b, 617u), RegionNoise.Hash01(a + 1, b, 617u), u),
                Mathf.Lerp(RegionNoise.Hash01(a, b + 1, 617u), RegionNoise.Hash01(a + 1, b + 1, 617u), u), v);
        }

        private static float Trail(float x, float z)
        {
            float crossTrack = Mathf.Abs(z - (2f - 0.48f * x + 0.65f * Mathf.Sin(x * 0.29f)));
            crossTrack += (Noise(x * 1.3f, z * 1.3f) - 0.5f) * 0.27f;
            return (1f - RegionNoise.Smooth(0.30f, 0.85f, crossTrack)) *
                (1f - RegionNoise.Smooth(14f, 25f, Mathf.Abs(x)));
        }

        internal static void BindGround(Material material, float east, float south, RegionPropProofSlice props = null)
        {
            material.SetVector("_GrasslandSurface", new Vector4(east, south, 1f, 0f));
            if (props != null)
                material.SetVector("_GrasslandTrail", new Vector4(east + props.CratesCenterX, south + props.CratesCenterZ, 1f, 0f));
        }

        private static void Release(Tile tile)
        {
            if (tile.Object != null)
            {
                tile.Object.SetActive(false);
                Release(tile.Object);
            }
            if (tile.Mesh != null) Release(tile.Mesh);
        }

        private static void Release(UnityEngine.Object target)
        {
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }
}

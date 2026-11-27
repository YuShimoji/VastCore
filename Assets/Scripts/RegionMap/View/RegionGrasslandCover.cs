using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.RegionMap
{
    /// <summary>Grounded grass cover shared by the fixed look and the streamed ground camera.</summary>
    [ExecuteAlways]
    internal sealed class RegionGrasslandCover : MonoBehaviour
    {
        private const string ShaderResource = "RegionMap3D/RegionGrasslandCover";
        private const string TussockAtlas = "RegionMap3D/MeadowTussocks";
        private const int TileSize = 16;
        private const float MeadowRadius = 88f;
        private const float DetailRadius = 34f;
        private const float ShortGrassRadius = 17f;
        private const int ShortGrassLayer = 2;
        private const int DistantLayer = 3;
        private const float DistantRadius = 306f;
        private const float TussockDistantRadius = 468f;
        private const int ColonySpacing = 4;
        private const float ColonyMaxHeight = 2f;
        private const int TilesPerFrame = 2;

        private sealed class Tile
        {
            public RegionLookPart Surface;
            public Mesh Mesh;
            public GameObject Object;
        }

        [Serializable]
        private sealed class AtlasLayout
        {
            public Vector4[] frames = Array.Empty<Vector4>();
        }

        internal Camera Camera;
        private RegionChunkStreamer _streamer;
        private RegionLookScene _look;
        private Material _material;
        private Vector4[] _tussockFrames;
        private bool _visible = true;
        private RegionGrasslandStyle Style => _look?.Spec.GrasslandStyle ?? RegionGrasslandStyle.Colonies;
        private readonly Dictionary<Vector3Int, Tile> _tiles = new Dictionary<Vector3Int, Tile>();
        private readonly List<Vector3Int> _remove = new List<Vector3Int>();
        private readonly List<(Vector3Int Key, float Distance)> _pending = new List<(Vector3Int, float)>();
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector3> _surfaceNormals = new List<Vector3>();
        private readonly List<Vector4> _bladeData = new List<Vector4>();
        private readonly List<Vector3> _tussockData = new List<Vector3>();
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

        internal void SetCoverageMask(bool enabled)
        {
            if (_material == null) return;
            if (enabled) _material.EnableKeyword("GRASSLAND_COVERAGE_MASK");
            else _material.DisableKeyword("GRASSLAND_COVERAGE_MASK");
        }

        internal (int tiles, long triangles) GeometryInView(Camera camera)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            int tiles = 0;
            long triangles = 0;
            foreach (Tile tile in _tiles.Values)
            {
                if (tile.Object == null || !GeometryUtility.TestPlanesAABB(planes, tile.Object.GetComponent<MeshRenderer>().bounds))
                    continue;
                tiles++;
                triangles += tile.Mesh.GetIndexCount(0) / 3;
            }
            return (tiles, triangles);
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
            RegionGrasslandStyle style = Style;
            bool canopy = HasCanopy(style);
            bool tussocks = UsesTussocks(style);
            int lastLayer = canopy ? DistantLayer : ShortGrassLayer;
            if (above >= Radius(canopy ? DistantLayer : 0)) return;

            _pending.Clear();
            for (int layer = 0; layer <= lastLayer; layer++)
            {
                float radius = Radius(layer);
                int tileSize = TileMetres(layer);
                if (above >= radius) continue;
                for (int z = (int)Math.Floor((south - radius) / tileSize); z <= (int)Math.Floor((south + radius) / tileSize); z++)
                for (int x = (int)Math.Floor((east - radius) / tileSize); x <= (int)Math.Floor((east + radius) / tileSize); x++)
                {
                    var key = new Vector3Int(x, z, layer);
                    float distance = Distance(key, east, south, above);
                    if (distance < radius * radius && !_tiles.ContainsKey(key))
                        _pending.Add((key, distance));
                }
            }
            _pending.Sort((a, b) =>
            {
                if (tussocks)
                {
                    // Fill the close cover first, then advance the medium and distant rings together.
                    float aProgress = a.Distance * (a.Key.z == DistantLayer ? 0.14f : 1f);
                    float bProgress = b.Distance * (b.Key.z == DistantLayer ? 0.14f : 1f);
                    return aProgress.CompareTo(bProgress);
                }
                int detailOrder = (a.Key.z == DistantLayer).CompareTo(b.Key.z == DistantLayer);
                return detailOrder != 0 ? detailOrder : a.Distance.CompareTo(b.Distance);
            });

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
            if (_material != null && style == RegionGrasslandStyle.Colonies)
            {
                _material.SetVector("_GrasslandOrigin", new Vector4((float)originEast, (float)originSouth, 0f, 0f));
                var props = _look?.PropsSlice;
                if (props != null)
                    _material.SetVector("_GrasslandTrail", new Vector4((float)(originEast + props.CratesCenterX),
                        (float)(originSouth + props.CratesCenterZ), 1f, 0f));
            }
        }

        private (RegionLookPart Part, double East, double South, float Cell) Surface(Vector3Int key)
        {
            int tileSize = TileMetres(key.z);
            double east = (key.x + 0.5) * tileSize;
            double south = (key.y + 0.5) * tileSize;
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

        private static int TileMetres(int layer) => layer == DistantLayer ? 64 : TileSize;

        private static bool UsesTussocks(RegionGrasslandStyle style) =>
            style is RegionGrasslandStyle.Tussocks or RegionGrasslandStyle.TussockGeometry or RegionGrasslandStyle.Colonies;

        internal static bool HasCanopy(RegionGrasslandStyle style) =>
            style == RegionGrasslandStyle.Continuous || UsesTussocks(style);

        private float Radius(int layer)
        {
            if (UsesTussocks(Style))
                return layer switch
                {
                    ShortGrassLayer => 24f,
                    1 => 30f,
                    DistantLayer => TussockDistantRadius,
                    _ => Style == RegionGrasslandStyle.Colonies ? 146f : 177f
                };
            if (layer == DistantLayer) return DistantRadius;
            bool continuous = Style == RegionGrasslandStyle.Continuous;
            if (layer == ShortGrassLayer) return continuous ? 23f : ShortGrassRadius;
            if (layer == 1) return continuous ? 44f : DetailRadius;
            return continuous ? 128f : MeadowRadius;
        }

        private static float Distance(Vector3Int key, double east, double south, float above)
        {
            int tileSize = TileMetres(key.z);
            float x = Mathf.Max(0f, Mathf.Abs((float)((key.x + 0.5) * tileSize - east)) - tileSize * 0.5f);
            float z = Mathf.Max(0f, Mathf.Abs((float)((key.y + 0.5) * tileSize - south)) - tileSize * 0.5f);
            return x * x + z * z + above * above;
        }

        private static void PlaceTile(GameObject tile, Vector3Int key, double east, double south)
        {
            int tileSize = TileMetres(key.z);
            var offset = new Vector3((float)(key.x * (double)tileSize - east), 0f, (float)(south - key.y * (double)tileSize));
            if (tile.transform.localPosition != offset) tile.transform.localPosition = offset;
        }

        private Tile BuildTile(Vector3Int key, RegionLookPart part, double originEast, double originSouth, float cell)
        {
            var tile = new Tile { Surface = part };
            _vertices.Clear();
            _surfaceNormals.Clear();
            _bladeData.Clear();
            _tussockData.Clear();
            _colors.Clear();
            _indices.Clear();
            RegionData data = part.Data;
            int layer = key.z;
            int tileSize = TileMetres(layer);
            RegionGrasslandStyle style = Style;
            bool colonies = style == RegionGrasslandStyle.Colonies;
            bool tussocks = UsesTussocks(style);
            bool continuous = HasCanopy(style);
            if (tussocks && _tussockFrames == null)
            {
                TextAsset layout = Resources.Load<TextAsset>(TussockAtlas + "Layout");
                AtlasLayout atlas = layout != null ? JsonUtility.FromJson<AtlasLayout>(layout.text) : null;
                if (atlas?.frames == null || atlas.frames.Length != 12)
                    throw new InvalidOperationException("MeadowTussocks の投影範囲には12面が必要です。");
                _tussockFrames = atlas.frames;
            }
            bool overlap = continuous || style == RegionGrasslandStyle.Overlapping;
            bool shortGrass = layer == ShortGrassLayer;
            bool distant = layer == DistantLayer;
            bool colonySurface = colonies && distant;
            int count = distant ? 27 : shortGrass ? (overlap ? 73 : 80) : layer == 1 ? 44 : 20;
            if (tussocks)
                count = layer switch { ShortGrassLayer => 62, 1 => 32, DistantLayer => colonySurface ? 21 : 25, _ => 17 };
            int blades = layer == 1 ? 7 : 3;
            if (continuous && shortGrass) blades = 4;
            if (style == RegionGrasslandStyle.Denser && layer != 0) blades += shortGrass ? 3 : 5;
            float spacing = (float)tileSize / count;
            uint salt = unchecked((uint)data.Spec.Seed * 2654435761u + 6947u + (uint)layer * 1124u);
            float east = key.x * (float)tileSize, south = key.y * (float)tileSize;
            float localX = (float)(east - originEast), localZ = (float)(south - originSouth);
            float firstX = part.Positions[0], firstZ = part.Positions[2];
            if (!HasGrassland(data, (localX - firstX) / cell, (localZ - firstZ) / cell, tileSize / cell))
                return tile;
            int n = data.Resolution;
            var props = _look?.PropsSlice;
            float trailEast = (float)(originEast + (props?.CratesCenterX ?? 0f));
            float trailSouth = (float)(originSouth + (props?.CratesCenterZ ?? 0f));
            if (colonySurface) BuildColonySurface(part, localX, localZ, cell, tileSize, props);
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
                float moisture = Mathf.Clamp01((data.Moisture[v] - 0.14f) / 0.46f);
                float growth = Growth(east + x, south + z, moisture, style);
                float trail = props != null ? Trail(east + x - trailEast, south + z - trailSouth) : 0f;
                float coverage = shortGrass && overlap ? Mathf.Lerp(0.90f, 0.995f, growth)
                    : Mathf.Lerp(shortGrass ? 0.76f : 0.47f, 0.98f, growth);
                if (tussocks && !shortGrass) coverage = colonySurface ? 0.99f : Mathf.Lerp(0.69f, 0.995f, growth);
                float cover = coverage * RegionNoise.Smooth(0.76f, 0.96f, normalY) * (1f - trail);
                if (RegionNoise.Hash01(ix, iz, salt + 2) >= cover) continue;
                if (props != null && Mathf.Abs(east + x - trailEast) < 9f && Mathf.Abs(south + z - trailSouth) < 7f &&
                    OnRock(props, px, pz)) continue;
                if (!part.TryGridHeight(px, pz, (int)gx + (int)gz * (n - 1), out float height)) continue;
                float size = RegionNoise.Hash01(ix, iz, salt + 3);
                var foot = new Vector3(x, height - 0.025f, -z);
                float tuftAngle = 6.2831853f * RegionNoise.Hash01(ix, iz, salt + 9);
                if (tussocks)
                {
                    var slope = new Vector2(-part.Normals[3 * v], part.Normals[3 * v + 2]) / normalY;
                    float tuft = Noise((east + x) / 2.65f + 31f, (south + z) / 2.1f - 16f);
                    bool meshLeaves = layer == 1 || (style == RegionGrasslandStyle.TussockGeometry && shortGrass);
                    bool denseCrown = layer == 1 && style != RegionGrasslandStyle.TussockGeometry && tuft > 0.38f;
                    if (colonySurface)
                    {
                        float alongSlope = slope.sqrMagnitude > 0.015f ? Mathf.Atan2(slope.x, -slope.y) : -0.64f;
                        tuftAngle = alongSlope + (size - 0.5f) * 1.5f;
                    }
                    AddTussock(foot, slope, layer, growth, part.Sun[v], trail, size, tuft, tuftAngle,
                        RegionNoise.Hash01(ix, iz, salt + 8), meshLeaves, denseCrown, ix, iz, salt,
                        colonies && (layer == 0 || distant) ? moisture : 0f, colonySurface);
                    if (colonySurface)
                    {
                        Vector3 normal = new Vector3(part.Normals[3 * v], normalY, -part.Normals[3 * v + 2]);
                        while (_surfaceNormals.Count < _vertices.Count) _surfaceNormals.Add(normal);
                    }
                    continue;
                }
                float bladeHeight = shortGrass ? Mathf.Lerp(0.085f, 0.18f, growth) : Mathf.Lerp(0.14f, 0.42f, growth);
                if (distant) bladeHeight = Mathf.Lerp(0.18f, 0.72f, growth);
                bladeHeight *= Mathf.Lerp(0.58f, 1.45f, size) * (1f - trail * 0.72f);
                for (int b = 0; b < blades; b++)
                {
                    uint bladeSalt = salt + (uint)(11 + 7 * b);
                    float variation = RegionNoise.Hash01(ix, iz, bladeSalt);
                    float azimuth = 6.2831853f * RegionNoise.Hash01(ix, iz, bladeSalt + 1);
                    if (overlap) azimuth = tuftAngle + (b + 0.55f * variation) * (6.2831853f / blades);
                    float length = bladeHeight * Mathf.Lerp(0.60f, 1.36f, variation);
                    float width = distant ? Mathf.Lerp(0.28f, 0.68f, growth)
                        : shortGrass ? (continuous ? 0.032f : overlap ? 0.053f : 0.022f) : layer == 1 ? 0.019f : 0.066f;
                    float spread = distant ? 2.15f : shortGrass && overlap ? Mathf.Lerp(3.6f, 2.4f, growth)
                        : overlap ? 1.15f : 0.68f;
                    bool dry = layer == 1 && variation > 0.958f;
                    var along = new Vector3(Mathf.Cos(azimuth), 0f, Mathf.Sin(azimuth));
                    Vector3 offset = along * (spacing * (variation - 0.5f) * 0.86f);
                    offset.y = (-part.Normals[3 * v] * offset.x + part.Normals[3 * v + 2] * offset.z) / normalY;
                    AddBlade(foot + offset, along,
                        dry ? Mathf.Min(0.79f, 0.22f + length * 1.1f) : length, width * (dry ? 0.31f : 0.65f + 0.62f * variation),
                        growth, variation, part.Sun[v], layer, dry, spread);
                }
            }

            return CreateTile(tile, style, colonySurface);
        }

        private void BuildColonySurface(RegionLookPart part, float localX, float localZ, float cell, int tileSize,
            RegionPropProofSlice props)
        {
            RegionData data = part.Data;
            int n = data.Resolution;
            int side = tileSize / ColonySpacing + 1;
            // The tiles share edge samples. Sample the actual triangles to keep the turf on the terrain.
            for (int j = 0; j < side; j++)
            for (int i = 0; i < side; i++)
            {
                float x = i * ColonySpacing, z = j * ColonySpacing;
                float px = localX + x, pz = localZ + z;
                float gx = (px - part.Positions[0]) / cell, gz = (pz - part.Positions[2]) / cell;
                var normal = Vector3.zero;
                var surface = new Color32(0, 0, 0, 0);
                float height = 0f;
                if (gx >= 0f && gz >= 0f && gx <= n - 1 && gz <= n - 1)
                {
                    int ix = Mathf.Min((int)gx, n - 2), iz = Mathf.Min((int)gz, n - 2);
                    if (part.TryGridHeight(px, pz, ix + iz * (n - 1), out height))
                    {
                        int v = Mathf.RoundToInt(gx) + Mathf.RoundToInt(gz) * n;
                        normal = new Vector3(part.Normals[3 * v], part.Normals[3 * v + 1], -part.Normals[3 * v + 2]);
                        float land = data.Biome[v] == RegionBiome.Grassland && data.Height[v] > 0f && data.LakeDepth[v] <= 0f &&
                            (data.WaterCoverage?[v] ?? 0f) <= 0.24f && (data.StructureCoverage?[v] ?? 0f) <= 0.12f
                            ? RegionNoise.Smooth(0.78f, 0.97f, normal.y) : 0f;
                        float moisture = Mathf.Clamp01((data.Moisture[v] - 0.14f) / 0.46f);
                        if (props != null && Mathf.Abs(px - props.CratesCenterX) < 9f &&
                            Mathf.Abs(pz - props.CratesCenterZ) < 7f && OnRock(props, px, pz)) land = 0f;
                        surface = new Color32((byte)(255 * moisture), (byte)(255 * land), (byte)(255 * part.Sun[v]), 0);
                    }
                }
                var root = new Vector4(x, height, -z, DistantLayer);
                AddTussockVertex(new Vector3(x, height, -z), root, surface, new Vector3(0f, 0f, 3f));
                _surfaceNormals.Add(normal);
            }

            for (int j = 0; j < side - 1; j++)
            for (int i = 0; i < side - 1; i++)
            {
                int a = i + j * side;
                AddColonyTriangle(a, a + 1, a + side);
                AddColonyTriangle(a + 1, a + side + 1, a + side);
            }
        }

        private void AddColonyTriangle(int a, int b, int c)
        {
            if (_surfaceNormals[a].y <= 0f || _surfaceNormals[b].y <= 0f || _surfaceNormals[c].y <= 0f ||
                (_colors[a].g == 0 && _colors[b].g == 0 && _colors[c].g == 0)) return;
            _indices.Add(a);
            _indices.Add(b);
            _indices.Add(c);
        }

        private Tile CreateTile(Tile tile, RegionGrasslandStyle style, bool colonySurface)
        {
            if (_indices.Count == 0) return tile;
            bool tussocks = UsesTussocks(style);
            if (_material == null)
            {
                Shader shader = Resources.Load<Shader>(ShaderResource);
                if (shader == null || !shader.isSupported) return tile;
                _material = new Material(shader) { name = "Meadow grass", hideFlags = HideFlags.DontSave };
                _material.SetFloat("_GrasslandContinuous", HasCanopy(style) ? 1f : 0f);
                _material.SetFloat("_GrasslandColonies", style == RegionGrasslandStyle.Colonies ? 1f : 0f);
                if (tussocks)
                {
                    Texture2D atlas = Resources.Load<Texture2D>(TussockAtlas);
                    if (atlas == null) throw new InvalidOperationException("草株のアトラス MeadowTussocks が見つかりません。");
                    _material.SetTexture("_MeadowTussocks", atlas);
                    _material.SetFloat("_GrasslandAlphaToMask", 1f);
                    _material.EnableKeyword("GRASSLAND_TUSSOCKS");
                    _material.renderQueue = (int)RenderQueue.AlphaTest;
                }
            }
            var mesh = new Mesh { name = colonySurface ? "Grassland canopy" : "Grassland blades", hideFlags = HideFlags.DontSave };
            if (_vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(_vertices);
            mesh.SetUVs(0, _bladeData);
            if (tussocks) mesh.SetUVs(1, _tussockData);
            mesh.SetColors(_colors);
            mesh.SetTriangles(_indices, 0);
            if (colonySurface)
            {
                mesh.SetNormals(_surfaceNormals);
                // The shader raises the canopy from these grounded vertices.
                Bounds bounds = mesh.bounds;
                bounds.Expand(new Vector3(0f, 2f * ColonyMaxHeight, 0f));
                mesh.bounds = bounds;
            }
            mesh.UploadMeshData(true);
            var go = new GameObject(colonySurface ? "Grass canopy" : "Grass blades") { hideFlags = HideFlags.DontSave };
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

        private void AddTussock(Vector3 foot, Vector2 slope, int layer, float growth, float sun, float trail,
            float size, float colony, float yaw, float variantPick, bool meshLeaves, bool denseCrown, int ix, int iz, uint salt,
            float moisture, bool colonySurface)
        {
            bool low = layer == ShortGrassLayer;
            float crown = Mathf.Lerp(0.73f, 1.28f, size);
            float height = layer switch
            {
                ShortGrassLayer => Mathf.Lerp(0.24f, 0.37f, growth),
                1 => Mathf.Lerp(0.32f, 0.97f, RegionNoise.Smooth(0.27f, 0.73f, 0.44f * growth + 0.56f * colony)),
                DistantLayer => Mathf.Lerp(0.48f, 1.69f, 0.62f * growth + 0.38f * colony),
                _ => Mathf.Lerp(0.40f, 1.18f, 0.55f * growth + 0.45f * colony),
            };
            height *= crown * (1f - trail * 0.72f);
            float width = layer switch { ShortGrassLayer => 0.86f, 1 => 0.94f, DistantLayer => 3.48f, _ => 1.83f };
            if (colonySurface)
            {
                height = Mathf.Lerp(0.70f, 0.87f, size);
                width = 5.36f;
            }
            width *= Mathf.Lerp(0.87f, 1.15f, size) * (1f - trail * 0.72f);
            int variant = variantPick < 0.65f ? 1
                : variantPick < 0.94f || low || layer == DistantLayer ? 0 : variantPick < 0.97f ? 2 : 3;
            var light = new Color32((byte)(255 * growth), (byte)(255 * size), (byte)(255 * sun), (byte)(255 * moisture));
            var root = new Vector4(foot.x, foot.y, foot.z, layer);
            if (meshLeaves)
            {
                int leaves = low ? 18 : Mathf.RoundToInt(Mathf.Lerp(14f, 25f, colony));
                AddFineLeaves(foot, root, light, slope, low, width, height, leaves - (denseCrown ? 5 : 0), yaw, ix, iz, salt);
                if (!denseCrown) return;
                // The dense scan supplies the inner foliage; the curved ribbons keep the outline three-dimensional.
                variant = 2;
                width *= 0.86f;
                height *= 0.94f;
            }
            else AddTussockTop(foot, root, light, layer, slope, variant, yaw, width, height);

            if (low) return;

            int sides = layer == DistantLayer ? 1 : 2;
            for (int face = 0; face < sides; face++)
            {
                float angle = yaw + face * Mathf.PI * 0.5f;
                float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                var across = new Vector3(cos, slope.x * cos + slope.y * sin, sin) * (width * 0.5f);
                var lean = new Vector3(-sin * 0.32f, 1f, cos * 0.32f) * height;
                Vector3 basePoint = foot - Vector3.up * (height * 0.115f);
                Vector4 f = _tussockFrames[variant * 3 + face];
                Vector3 left = basePoint + across * (2f * f.x - 1f);
                Vector3 right = basePoint + across * (2f * f.z - 1f);
                int first = _vertices.Count;
                AddTussockVertex(left + lean * f.y, root, light, AtlasUv(face, variant, f.x, f.y, 1f));
                AddTussockVertex(right + lean * f.y, root, light, AtlasUv(face, variant, f.z, f.y, 1f));
                AddTussockVertex(left + lean * f.w, root, light, AtlasUv(face, variant, f.x, f.w, 1f));
                AddTussockVertex(right + lean * f.w, root, light, AtlasUv(face, variant, f.z, f.w, 1f));
                AddQuad(first);
            }
        }

        private void AddTussockTop(Vector3 foot, Vector4 root, Color32 light, int layer, Vector2 slope,
            int variant, float yaw, float width, float height)
        {
            const int face = 2;
            Vector4 f = _tussockFrames[variant * 3 + face];
            float cos = Mathf.Cos(yaw), sin = Mathf.Sin(yaw);
            var across = new Vector3(cos, slope.x * cos + slope.y * sin, sin) * (width * 0.5f);
            var length = new Vector3(-sin, slope.y * cos - slope.x * sin, cos) * (width * 0.47f);
            Vector3 center = foot + Vector3.up * (height * (layer == ShortGrassLayer ? 0.13f : 0.18f));
            Vector3 left = center + across * (2f * f.x - 1f);
            Vector3 right = center + across * (2f * f.z - 1f);
            Vector3 bottom = length * (2f * f.y - 1f);
            Vector3 top = length * (2f * f.w - 1f);
            int first = _vertices.Count;
            AddTussockVertex(left + bottom, root, light, AtlasUv(face, variant, f.x, f.y, 2f));
            AddTussockVertex(right + bottom, root, light, AtlasUv(face, variant, f.z, f.y, 2f));
            AddTussockVertex(right + top, root, light, AtlasUv(face, variant, f.z, f.w, 2f));
            AddTussockVertex(left + top, root, light, AtlasUv(face, variant, f.x, f.w, 2f));
            AddTussockVertex(center + Vector3.up * (height * 0.36f), root, light, AtlasUv(face, variant, 0.5f, 0.5f, 2f));
            for (int edge = 0; edge < 4; edge++)
            {
                _indices.Add(first + edge);
                _indices.Add(first + 4);
                _indices.Add(first + ((edge + 1) % 4));
            }
        }

        private static Vector3 AtlasUv(int face, int variant, float u, float v, float projection) =>
            new Vector3((face + u) * 0.25f, (3f - variant + v) * 0.25f, projection);

        private void AddFineLeaves(Vector3 foot, Vector4 root, Color32 light, Vector2 slope, bool low,
            float width, float height, int leaves, float yaw, int ix, int iz, uint salt)
        {
            var rake = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw)) * (width * 0.16f);
            for (int blade = 0; blade < leaves; blade++)
            {
                float len = RegionNoise.Hash01(ix, iz, salt + (uint)(21 + 8 * blade));
                float jitter = RegionNoise.Hash01(ix, iz, salt + (uint)(22 + 8 * blade));
                float angle = yaw + blade * 2.399963f + 0.57f * jitter;
                var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                outward.y = slope.x * outward.x + slope.y * outward.z;
                var side = new Vector3(-outward.z, 0f, outward.x) * Mathf.Lerp(0.0022f, 0.0062f, jitter);
                float rise = height * Mathf.Lerp(0.26f, low ? 0.66f : 0.98f, len);
                float spread = width * Mathf.Lerp(0.21f, 0.47f, 1f - len);
                Vector3 start = foot + outward * (width * (0.020f + 0.073f * jitter));
                Vector3 curve = outward * spread + rake * len;
                light.g = (byte)Mathf.Lerp(68f, 236f, jitter);
                int first = _vertices.Count;
                for (int row = 0; row < 3; row++)
                {
                    float t = row * 0.34f;
                    Vector3 center = start + curve * (t * t) + Vector3.up * (rise * t * (1.93f - 0.94f * t));
                    Vector3 edge = side * (1f - 0.68f * t);
                    AddTussockVertex(center - edge, root, light, Vector3.zero);
                    AddTussockVertex(center + edge, root, light, Vector3.zero);
                }
                Vector3 tip = start + curve + Vector3.up * (rise * (low ? 0.76f : 0.89f));
                AddTussockVertex(tip, root, light, Vector3.zero);
                AddQuad(first);
                AddQuad(first + 2);
                _indices.Add(first + 4);
                _indices.Add(first + 6);
                _indices.Add(first + 5);
            }
        }

        private void AddTussockVertex(Vector3 position, Vector4 root, Color32 light, Vector3 atlas)
        {
            _vertices.Add(position);
            _bladeData.Add(root);
            _colors.Add(light);
            _tussockData.Add(atlas);
        }

        private void AddQuad(int first)
        {
            _indices.Add(first);
            _indices.Add(first + 2);
            _indices.Add(first + 1);
            _indices.Add(first + 1);
            _indices.Add(first + 2);
            _indices.Add(first + 3);
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
            float growth, float variation, float sun, int layer, bool dry, float spread)
        {
            Vector3 side = new Vector3(along.z, 0f, -along.x) * (width * 0.5f);
            Vector3 lean = (along * (dry ? 0.15f : spread) + new Vector3(0.20f, 0f, 0.13f)) * height;
            bool basalLeaf = layer == ShortGrassLayer && spread > 1f;
            Vector3 tip = root + Vector3.up * (basalLeaf ? 0.71f * height : height) + lean;
            var color = new Color32((byte)(255 * growth), (byte)(255 * variation), (byte)(255 * sun), dry ? (byte)255 : (byte)0);
            var rootAndLayer = new Vector4(root.x, root.y, root.z, layer);
            int start = _vertices.Count;
            if ((layer == ShortGrassLayer && !basalLeaf) || layer == DistantLayer)
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

            Vector3 mid = root + Vector3.up * (height * (basalLeaf ? 1.05f : 0.61f)) + lean * 0.29f;
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
        private static float Growth(float x, float z, float moisture, RegionGrasslandStyle style)
        {
            float along = 0.8f * x + 0.6f * z, across = -0.6f * x + 0.8f * z;
            if (UsesTussocks(style))
            {
                float local = 0.63f * Noise(x / 51f + 8f, z / 37f - 4f)
                    + 0.37f * Noise(along / 12f + 17f, across / 7.4f - 9f);
                return RegionNoise.Smooth(0.18f, 0.80f, local * 0.74f + moisture * 0.26f);
            }
            if (style != RegionGrasslandStyle.Continuous)
            {
                float previous = 0.64f * Noise(x / 37f, z / 37f) + 0.36f * Noise(x / 8f + 17f, z / 8f - 9f);
                return RegionNoise.Smooth(0.16f, 0.83f, previous * 0.78f + moisture * 0.22f);
            }
            float patch = 0.57f * Noise(along / 62f, across / 29f) +
                0.43f * Noise(along / 15f + 17f, across / 5.5f - 9f);
            return RegionNoise.Smooth(0.23f, 0.76f, patch * 0.71f + moisture * 0.29f);
        }

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

        internal static void BindGround(Material material, float east, float south, RegionPropProofSlice props = null,
            RegionGrasslandStyle style = RegionGrasslandStyle.Colonies)
        {
            float canopy = style == RegionGrasslandStyle.Colonies ? 3f
                : UsesTussocks(style) ? 2f : style == RegionGrasslandStyle.Continuous ? 1f : 0f;
            material.SetVector("_GrasslandSurface", new Vector4(east, south, 1f, canopy));
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

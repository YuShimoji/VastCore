using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// 最小構成の地形タイルコンポーネント
    /// </summary>
    public class TerrainTileComponent : MonoBehaviour
    {
        #region Fields
        [Header("タイル基本情報")]
        [SerializeField] private Vector2Int _coordinate;
        [SerializeField] private float _tileSize = 100f;
        [SerializeField] private TileState _state = TileState.Unloaded;

        [Header("地形データ")]
        [SerializeField] private float[,] _heightData;
        [SerializeField] private int _heightResolution;
        [SerializeField] private float _heightScale;
        [SerializeField] private Mesh _terrainMesh;
        [SerializeField] private Material _terrainMaterial;

        [Header("参照キャッシュ")]
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;

        private System.DateTime _lastAccessedAt = System.DateTime.Now;
        private int _accessCount;
        private string _appliedBiome = "Default";

        public enum TileState
        {
            Unloaded,
            Loading,
            Loaded,
            Active,
            Inactive
        }

        #endregion

        #region Properties
        public Vector2Int Coordinate => _coordinate;
        public Vector3 WorldPosition => transform.position;
        public float TileSize => _tileSize;
        public TileState State => _state;
        public bool IsActive => _state == TileState.Active;
        public float[,] HeightData => _heightData;
        public Mesh TerrainMesh => _terrainMesh;
        public Material TerrainMaterial => _terrainMaterial;
        public int HeightResolution => _heightResolution;
        public float HeightScale => _heightScale;
        public System.DateTime LastAccessTime => _lastAccessedAt;
        public int AccessCount => _accessCount;
        public string AppliedBiome => _appliedBiome;

        // 旧インターフェース互換用
        public Vector2Int coordinate
        {
            get => _coordinate;
            set => _coordinate = value;
        }
        public Vector3 worldPosition
        {
            get => transform.position;
            set => transform.position = value;
        }
        public float tileSize => _tileSize;
        public TileState state => _state;
        public float[,] heightData
        {
            get => _heightData;
            set => _heightData = value;
        }
        public Mesh terrainMesh => _terrainMesh;
        public Material terrainMaterial => _terrainMaterial;
        public System.DateTime lastAccessTime => _lastAccessedAt;
        public string appliedBiome
        {
            get => _appliedBiome;
            set => _appliedBiome = value;
        }
        public GameObject terrainObject => gameObject;

        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();

            if (_meshFilter == null) _meshFilter = gameObject.AddComponent<MeshFilter>();
            if (_meshRenderer == null) _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            if (_meshCollider == null) _meshCollider = gameObject.AddComponent<MeshCollider>();
        }

        #endregion

        #region Public API
        public void Initialize(Vector2Int tileCoordinate, float size, float[,] heights, int resolution, float maxHeight, Mesh mesh, Material material)
        {
            _coordinate = tileCoordinate;
            _tileSize = size;
            transform.position = new Vector3(tileCoordinate.x * size, 0f, tileCoordinate.y * size);

            _heightData = heights;
            _heightResolution = resolution;
            _heightScale = maxHeight;
            _terrainMesh = mesh;
            _terrainMaterial = material;

            _meshFilter.sharedMesh = _terrainMesh;
            _meshCollider.sharedMesh = _terrainMesh;
            if (_terrainMaterial != null)
            {
                _meshRenderer.sharedMaterial = _terrainMaterial;
            }

            _state = TileState.Loaded;
            _lastAccessedAt = System.DateTime.Now;
            _accessCount = 0;
        }

        public void Initialize(Vector2Int tileCoordinate, int size, int resolution, float maxHeight, Vastcore.Generation.Map.RuntimeTerrainManager.TerrainGenerationParams genParams)
        {
            _coordinate = tileCoordinate;
            _tileSize = size;
            transform.position = new Vector3(tileCoordinate.x * size, 0f, tileCoordinate.y * size);

            _heightResolution = resolution;
            _heightScale = maxHeight;
            _heightData = GenerateHeightmapForInit(tileCoordinate, size, resolution, genParams);
            _terrainMesh = BuildMeshForInit(_heightData, size, maxHeight);
            _terrainMaterial = _terrainMaterial; // keep as is if already assigned

            _meshFilter.sharedMesh = _terrainMesh;
            _meshCollider.sharedMesh = _terrainMesh;
            if (_terrainMaterial != null)
            {
                _meshRenderer.sharedMaterial = _terrainMaterial;
            }

            _state = TileState.Loaded;
            _lastAccessedAt = System.DateTime.Now;
            _accessCount = 0;
        }

        public void UpdateTerrain(Vastcore.Generation.Map.RuntimeTerrainManager.TerrainGenerationParams newParams)
        {
            if (_heightResolution <= 0)
            {
                return;
            }
            _heightData = GenerateHeightmapForInit(_coordinate, (int)_tileSize, _heightResolution, newParams);
            _terrainMesh = BuildMeshForInit(_heightData, (int)_tileSize, _heightScale);
            _meshFilter.sharedMesh = _terrainMesh;
            _meshCollider.sharedMesh = _terrainMesh;
            _lastAccessedAt = System.DateTime.Now;
            _accessCount++;
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            _state = active ? TileState.Active : TileState.Inactive;
            if (active)
            {
                _lastAccessedAt = System.DateTime.Now;
                _accessCount++;
            }
        }

        public void SetColliderEnabled(bool enabled)
        {
            if (_meshCollider != null)
            {
                _meshCollider.enabled = enabled;
            }
        }

        public void Unload()
        {
            _state = TileState.Unloaded;
            _heightData = null;
            _heightResolution = 0;
            _heightScale = 0f;
            if (_meshCollider != null) _meshCollider.sharedMesh = null;
            if (_meshFilter != null) _meshFilter.sharedMesh = null;
            _terrainMesh = null;
            _terrainMaterial = null;
        }

        public bool ContainsWorldPosition(Vector3 worldPos)
        {
            Vector3 local = worldPos - transform.position;
            return Mathf.Abs(local.x) <= _tileSize * 0.5f && Mathf.Abs(local.z) <= _tileSize * 0.5f;
        }

        public float GetHeightAtWorldPosition(Vector3 worldPos)
        {
            Vector3 local = worldPos - transform.position;
            return GetHeightAtLocalPosition(local);
        }

        public float GetHeightAtLocalPosition(Vector3 localPos)
        {
            if (_heightData == null || _heightResolution <= 1)
            {
                return transform.position.y;
            }

            float normalizedX = Mathf.Clamp01((localPos.x / _tileSize) + 0.5f);
            float normalizedZ = Mathf.Clamp01((localPos.z / _tileSize) + 0.5f);

            float gridX = normalizedX * (_heightResolution - 1);
            float gridZ = normalizedZ * (_heightResolution - 1);

            int x0 = Mathf.FloorToInt(gridX);
            int z0 = Mathf.FloorToInt(gridZ);
            int x1 = Mathf.Min(x0 + 1, _heightResolution - 1);
            int z1 = Mathf.Min(z0 + 1, _heightResolution - 1);

            float tx = gridX - x0;
            float tz = gridZ - z0;

            float h00 = _heightData[z0, x0];
            float h10 = _heightData[z0, x1];
            float h01 = _heightData[z1, x0];
            float h11 = _heightData[z1, x1];

            float h0 = Mathf.Lerp(h00, h10, tx);
            float h1 = Mathf.Lerp(h01, h11, tz);

            return transform.position.y + Mathf.Lerp(h0, h1, tz) * _heightScale;
        }

        public void SetAppliedBiome(string biomeId)
        {
            _appliedBiome = biomeId;
        }

        private float[,] GenerateHeightmapForInit(Vector2Int tileCoord, int size, int resolution, Vastcore.Generation.Map.RuntimeTerrainManager.TerrainGenerationParams p)
        {
            float[,] heights = new float[resolution, resolution];
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float amplitude = p.amplitude;
                    float frequency = p.frequency;
                    float noiseValue = 0f;
                    float weight = 1f;

                    for (int octave = 0; octave < Mathf.Max(1, p.octaves); octave++)
                    {
                        float sampleX = (tileCoord.x * size + (x / (float)(resolution - 1) * size)) * frequency + p.offset.x;
                        float sampleZ = (tileCoord.y * size + (z / (float)(resolution - 1) * size)) * frequency + p.offset.y;
                        float perlin = Mathf.PerlinNoise(sampleX, sampleZ);
                        noiseValue += perlin * amplitude * weight;
                        weight *= p.persistence;
                        frequency *= p.lacunarity;
                    }

                    heights[z, x] = Mathf.Clamp01(noiseValue);
                }
            }
            return heights;
        }

        private Mesh BuildMeshForInit(float[,] heights, int size, float maxHeight)
        {
            int resolution = heights.GetLength(0);
            Vector3[] vertices = new Vector3[resolution * resolution];
            Vector2[] uvs = new Vector2[resolution * resolution];
            int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];

            int v = 0;
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float nx = x / (float)(resolution - 1);
                    float nz = z / (float)(resolution - 1);
                    float px = (nx - 0.5f) * size;
                    float pz = (nz - 0.5f) * size;
                    float py = heights[z, x] * maxHeight;
                    vertices[v] = new Vector3(px, py, pz);
                    uvs[v] = new Vector2(nx, nz);
                    v++;
                }
            }

            int t = 0;
            for (int z = 0; z < resolution - 1; z++)
            {
                for (int x = 0; x < resolution - 1; x++)
                {
                    int topLeft = z * resolution + x;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + resolution;
                    int bottomRight = bottomLeft + 1;

                    triangles[t++] = topLeft;
                    triangles[t++] = bottomRight;
                    triangles[t++] = bottomLeft;

                    triangles[t++] = topLeft;
                    triangles[t++] = topRight;
                    triangles[t++] = bottomRight;
                }
            }

            var mesh = new Mesh
            {
                vertices = vertices,
                uv = uvs,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
        #endregion
    }
}
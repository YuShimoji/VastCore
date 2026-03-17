using UnityEngine;

namespace Vastcore.Terrain.Erosion
{
    /// <summary>
    /// エロージョン適用結果をシーンで可視化する MonoBehaviour。
    /// ノイズハイトマップを生成し、HydraulicErosion / ThermalErosion を適用して
    /// Plane メッシュの頂点を変位させる。
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ErosionPreview : MonoBehaviour
    {
        [Header("Heightmap")]
        [SerializeField] private int m_Resolution = 64;
        [SerializeField] private float m_HeightScale = 5f;
        [SerializeField] private float m_NoiseScale = 0.05f;
        [SerializeField] private int m_Seed = 42;

        [Header("Hydraulic Erosion")]
        [SerializeField] private bool m_EnableHydraulic = true;
        [SerializeField] private int m_HydraulicIterations = 30000;
        [SerializeField, Range(0f, 1f)] private float m_ErosionRate = 0.3f;
        [SerializeField, Range(0f, 1f)] private float m_DepositionRate = 0.3f;

        [Header("Thermal Erosion")]
        [SerializeField] private bool m_EnableThermal = true;
        [SerializeField] private int m_ThermalIterations = 30;
        [SerializeField, Range(0.1f, 2f)] private float m_TalusAngle = 0.6f;

        [Header("Display")]
        [SerializeField] private float m_MeshSize = 20f;

        private float[,] m_Heightmap;

        private void Start()
        {
            Generate();
        }

        /// <summary>
        /// ハイトマップ生成 → エロージョン適用 → メッシュ生成の一連フロー
        /// </summary>
        public void Generate()
        {
            m_Heightmap = GenerateNoiseHeightmap();

            if (m_EnableHydraulic)
            {
                var hydraulic = new HydraulicErosion
                {
                    Iterations = m_HydraulicIterations,
                    ErosionRate = m_ErosionRate,
                    DepositionRate = m_DepositionRate
                };
                hydraulic.Apply(m_Heightmap, m_Seed);
            }

            if (m_EnableThermal)
            {
                var thermal = new ThermalErosion
                {
                    Iterations = m_ThermalIterations,
                    TalusAngle = m_TalusAngle
                };
                thermal.Apply(m_Heightmap);
            }

            ApplyToMesh();
        }

        private float[,] GenerateNoiseHeightmap()
        {
            float[,] map = new float[m_Resolution, m_Resolution];
            float offsetX = m_Seed * 100f;
            float offsetZ = m_Seed * 37f;

            for (int x = 0; x < m_Resolution; x++)
            {
                for (int z = 0; z < m_Resolution; z++)
                {
                    float nx = x * m_NoiseScale + offsetX;
                    float nz = z * m_NoiseScale + offsetZ;

                    float h = Mathf.PerlinNoise(nx, nz);
                    h += 0.5f * Mathf.PerlinNoise(nx * 2f, nz * 2f);
                    h += 0.25f * Mathf.PerlinNoise(nx * 4f, nz * 4f);
                    map[x, z] = h * m_HeightScale;
                }
            }
            return map;
        }

        private void ApplyToMesh()
        {
            int res = m_Resolution;
            Vector3[] vertices = new Vector3[res * res];
            int[] triangles = new int[(res - 1) * (res - 1) * 6];
            Vector2[] uvs = new Vector2[res * res];

            float step = m_MeshSize / (res - 1);
            float halfSize = m_MeshSize * 0.5f;

            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    int i = z * res + x;
                    vertices[i] = new Vector3(
                        x * step - halfSize,
                        m_Heightmap[x, z],
                        z * step - halfSize);
                    uvs[i] = new Vector2((float)x / (res - 1), (float)z / (res - 1));
                }
            }

            int tri = 0;
            for (int z = 0; z < res - 1; z++)
            {
                for (int x = 0; x < res - 1; x++)
                {
                    int bl = z * res + x;
                    int br = bl + 1;
                    int tl = bl + res;
                    int tr = tl + 1;

                    triangles[tri++] = bl;
                    triangles[tri++] = tl;
                    triangles[tri++] = br;
                    triangles[tri++] = br;
                    triangles[tri++] = tl;
                    triangles[tri++] = tr;
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "ErosionPreviewMesh";
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GetComponent<MeshFilter>().mesh = mesh;
        }
    }
}

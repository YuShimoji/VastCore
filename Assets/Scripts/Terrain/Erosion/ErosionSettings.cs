using UnityEngine;

namespace Vastcore.Terrain.Erosion
{
    /// <summary>
    /// エロージョンパラメータの ScriptableObject。
    /// TerrainGenerationConfig から参照され、TerrainChunk.Build で適用される。
    /// </summary>
    [CreateAssetMenu(fileName = "ErosionSettings", menuName = "Vastcore/Terrain/Erosion Settings")]
    public class ErosionSettings : ScriptableObject
    {
        [Header("General")]
        [Tooltip("エロージョンを有効にする")]
        public bool enabled = true;

        [Tooltip("エロージョンのシード値")]
        public int erosionSeed = 42;

        [Header("Hydraulic Erosion")]
        [Tooltip("水力エロージョンを有効にする")]
        public bool enableHydraulic = true;

        [Tooltip("水力エロージョンの反復回数")]
        [Range(1000, 200000)]
        public int hydraulicIterations = 50000;

        [Tooltip("侵食率")]
        [Range(0f, 1f)]
        public float erosionRate = 0.3f;

        [Tooltip("堆積率")]
        [Range(0f, 1f)]
        public float depositionRate = 0.3f;

        [Header("Thermal Erosion")]
        [Tooltip("熱エロージョンを有効にする")]
        public bool enableThermal = true;

        [Tooltip("熱エロージョンの反復回数")]
        [Range(1, 200)]
        public int thermalIterations = 50;

        [Tooltip("安息角（タンジェント値）")]
        [Range(0.1f, 2f)]
        public float talusAngle = 0.6f;
    }
}

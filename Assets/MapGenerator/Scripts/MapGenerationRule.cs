
using UnityEngine;

namespace MapGenerator
{
    [CreateAssetMenu(fileName = "NewMapGenerationRule", menuName = "MapGenerator/Map Generation Rule")]
    public class MapGenerationRule : ScriptableObject
    {
        [Header("Shape Settings")]
        [Tooltip("生成するマップの頂点の最小数")]
        public int minVertices = 5;

        [Tooltip("生成するマップの頂点の最大数")]
        public int maxVertices = 12;

        [Header("Noise Settings")]
        [Tooltip("形状のランダム性（値が大きいほど複雑になる）")]
        [Range(0.1f, 2.0f)]
        public float noiseScale = 1.0f;

        [Tooltip("中心からの平均半径")]
        [Range(5f, 50f)]
        public float averageRadius = 10f;
    }
}

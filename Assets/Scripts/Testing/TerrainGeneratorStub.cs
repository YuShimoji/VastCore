using System.Collections;
using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// 旧 TerrainSpawner などから参照されるレガシー TerrainGenerator のスタブ実装。
    /// 実際の地形生成ロジックは持たず、プロパティとコルーチンのみを提供します。
    /// </summary>
    public class TerrainGenerator : MonoBehaviour
    {
        public enum TerrainGenerationMode
        {
            Noise
        }

        public int Width { get; set; }
        public int Height { get; set; }
        public int Depth { get; set; }
        public int Resolution { get; set; }
        public float Scale { get; set; }
        public int Octaves { get; set; }
        public float Persistence { get; set; }
        public float Lacunarity { get; set; }
        public Material TerrainMaterial { get; set; }
        public TerrainGenerationMode GenerationMode { get; set; }

        public IEnumerator GenerateTerrain()
        {
            // スタブ: 何も生成せずに 1 フレームだけ待機
            yield return null;
        }
    }
}

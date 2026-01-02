using System;
using UnityEditor;
using UnityEngine;

namespace Vastcore.Editor.Terrain
{
    /// <summary>
    /// Terrain 生成の手動検証を支援するデバッグツール。
    /// 目的: 同一Seedで同一結果になっているか等を、見た目だけでなく数値で確認できるようにする。
    /// </summary>
    public static class TerrainHeightmapDebugTools
    {
        #region Menu
        [MenuItem("Tools/Vastcore/Terrain/Debug/Log Heightmap Hash")]
        private static void LogHeightmapHash()
        {
            UnityEngine.Terrain terrain = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<UnityEngine.Terrain>()
                : null;

            if (terrain == null)
            {
                // 選択から取れない場合、シーン内の Terrain を拾う（安全のため1つ目のみ）
                terrain = UnityEngine.Object.FindFirstObjectByType<UnityEngine.Terrain>();
            }

            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogWarning("[TerrainHeightmapDebugTools] Terrain が見つかりません。Terrain を選択してから再実行してください。");
                return;
            }

            try
            {
                TerrainData data = terrain.terrainData;
                int res = data.heightmapResolution;
                float[,] heights = data.GetHeights(0, 0, res, res);

                ulong fnv1a = ComputeFNV1a64(heights);
                Debug.Log($"[TerrainHeightmapDebugTools] HeightmapHash(FNV1a64)=0x{fnv1a:X16}, res={res}, size={data.size}, terrain={terrain.name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TerrainHeightmapDebugTools] ハッシュ計算に失敗: {ex}");
            }
        }
        #endregion

        #region Private Methods
        private static ulong ComputeFNV1a64(float[,] heights)
        {
            // Terrainの高さは0..1前提。微小誤差で差分判定がブレないよう ushort に量子化してハッシュ化する。
            const ulong offsetBasis = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;

            int h = heights.GetLength(0);
            int w = heights.GetLength(1);

            ulong hash = offsetBasis;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    ushort q = (ushort)Mathf.Clamp(Mathf.RoundToInt(heights[y, x] * 65535f), 0, 65535);
                    // little-endian 2 bytes
                    hash ^= (byte)(q & 0xFF);
                    hash *= prime;
                    hash ^= (byte)((q >> 8) & 0xFF);
                    hash *= prime;
                }
            }

            return hash;
        }
        #endregion
    }
}



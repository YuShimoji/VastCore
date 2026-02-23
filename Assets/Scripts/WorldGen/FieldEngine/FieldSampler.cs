using UnityEngine;
using Vastcore.WorldGen.Common;

namespace Vastcore.WorldGen.FieldEngine
{
    /// <summary>
    /// DensityGrid 充填ユーティリティ。
    /// </summary>
    public static class FieldSampler
    {
        /// <summary>
        /// 指定 bounds 内を均等サンプリングして grid に書き込む。
        /// </summary>
        public static void Fill(IDensityField field, DensityGrid grid, ChunkBounds bounds)
        {
            if (field == null || grid == null)
                return;

            int resolution = grid.Resolution;
            int last = Mathf.Max(1, resolution - 1);
            Vector3 size = bounds.Size;

            for (int z = 0; z < resolution; z++)
            {
                float tz = (float)z / last;
                float wz = bounds.Min.z + size.z * tz;

                for (int y = 0; y < resolution; y++)
                {
                    float ty = (float)y / last;
                    float wy = bounds.Min.y + size.y * ty;

                    for (int x = 0; x < resolution; x++)
                    {
                        float tx = (float)x / last;
                        float wx = bounds.Min.x + size.x * tx;

                        grid[x, y, z] = field.Sample(new Vector3(wx, wy, wz));
                    }
                }
            }
        }
    }
}

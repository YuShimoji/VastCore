using UnityEngine;

namespace Vastcore.Terrain.Erosion
{
    /// <summary>
    /// 熱エロージョン（タルス斜面形成）。
    /// 急傾斜の地形を安息角以下に崩し、タルス堆積物を形成する。
    /// Pure C# クラス（MonoBehaviour 非依存）。
    /// </summary>
    public class ThermalErosion
    {
        #region Parameters
        /// <summary>シミュレーション反復回数</summary>
        public int Iterations { get; set; } = 50;

        /// <summary>安息角 (タンジェント値)。これを超える勾配は崩壊する</summary>
        public float TalusAngle { get; set; } = 0.6f;

        /// <summary>1反復あたりの移動量係数 (0-1)</summary>
        public float TransferRate { get; set; } = 0.5f;
        #endregion

        #region Public Methods
        /// <summary>
        /// ハイトマップに熱エロージョンを適用する。
        /// 急斜面のセルから隣接する低いセルへ高さを移動させ、安息角以下にする。
        /// </summary>
        /// <param name="_heightmap">ハイトマップ (width x height)。in-place で変更される</param>
        public void Apply(float[,] _heightmap)
        {
            if (_heightmap == null) return;

            int width = _heightmap.GetLength(0);
            int height = _heightmap.GetLength(1);
            if (width < 3 || height < 3) return;

            // 4方向の隣接オフセット
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };

            for (int iter = 0; iter < Iterations; iter++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        float h = _heightmap[x, y];

                        // 最も勾配が急な隣接セルを探す
                        float maxDiff = 0f;
                        int bestDir = -1;

                        for (int d = 0; d < 4; d++)
                        {
                            int nx = x + dx[d];
                            int ny = y + dy[d];
                            float diff = h - _heightmap[nx, ny];
                            if (diff > maxDiff)
                            {
                                maxDiff = diff;
                                bestDir = d;
                            }
                        }

                        // 安息角を超えている場合、材料を移動
                        if (maxDiff > TalusAngle && bestDir >= 0)
                        {
                            float transfer = (maxDiff - TalusAngle) * TransferRate * 0.5f;
                            int bnx = x + dx[bestDir];
                            int bny = y + dy[bestDir];

                            _heightmap[x, y] -= transfer;
                            _heightmap[bnx, bny] += transfer;
                        }
                    }
                }
            }
        }
        #endregion
    }
}

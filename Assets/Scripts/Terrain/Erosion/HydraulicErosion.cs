using UnityEngine;

namespace Vastcore.Terrain.Erosion
{
    /// <summary>
    /// ドロップレットベースの水力エロージョン。
    /// ハイトマップ上に仮想的な水滴を落とし、勾配に沿って侵食・堆積を行う。
    /// Pure C# クラス（MonoBehaviour 非依存）。
    /// </summary>
    public class HydraulicErosion
    {
        #region Parameters
        /// <summary>シミュレーション反復回数</summary>
        public int Iterations { get; set; } = 50000;

        /// <summary>1ドロップレットあたりの最大ステップ数</summary>
        public int MaxDropletLifetime { get; set; } = 64;

        /// <summary>侵食率 (0-1)</summary>
        public float ErosionRate { get; set; } = 0.3f;

        /// <summary>堆積率 (0-1)</summary>
        public float DepositionRate { get; set; } = 0.3f;

        /// <summary>蒸発率 (0-1)</summary>
        public float EvaporationRate { get; set; } = 0.01f;

        /// <summary>重力加速度</summary>
        public float Gravity { get; set; } = 4.0f;

        /// <summary>初期水量</summary>
        public float InitialWater { get; set; } = 1.0f;

        /// <summary>初期速度</summary>
        public float InitialSpeed { get; set; } = 1.0f;

        /// <summary>運搬容量係数</summary>
        public float SedimentCapacityFactor { get; set; } = 4.0f;

        /// <summary>慣性 (0=勾配方向のみ, 1=前フレーム方向維持)</summary>
        public float Inertia { get; set; } = 0.05f;

        /// <summary>最小勾配 (0除算防止)</summary>
        public float MinSlope { get; set; } = 0.01f;
        #endregion

        #region Public Methods
        /// <summary>
        /// ハイトマップにドロップレットベースの水力エロージョンを適用する。
        /// </summary>
        /// <param name="_heightmap">ハイトマップ (width x height)。in-place で変更される</param>
        /// <param name="_seed">乱数シード</param>
        public void Apply(float[,] _heightmap, int _seed = 0)
        {
            if (_heightmap == null) return;

            int width = _heightmap.GetLength(0);
            int height = _heightmap.GetLength(1);
            if (width < 3 || height < 3) return;

            System.Random rng = new System.Random(_seed);

            for (int iter = 0; iter < Iterations; iter++)
            {
                // ランダムな開始位置
                float posX = (float)(rng.NextDouble() * (width - 2) + 1);
                float posY = (float)(rng.NextDouble() * (height - 2) + 1);
                float dirX = 0f;
                float dirY = 0f;
                float speed = InitialSpeed;
                float water = InitialWater;
                float sediment = 0f;

                for (int step = 0; step < MaxDropletLifetime; step++)
                {
                    int cellX = (int)posX;
                    int cellY = (int)posY;

                    if (cellX < 1 || cellX >= width - 1 || cellY < 1 || cellY >= height - 1)
                        break;

                    // バイリニア補間で勾配を計算
                    float offsetX = posX - cellX;
                    float offsetY = posY - cellY;

                    float h00 = _heightmap[cellX, cellY];
                    float h10 = _heightmap[cellX + 1, cellY];
                    float h01 = _heightmap[cellX, cellY + 1];
                    float h11 = _heightmap[cellX + 1, cellY + 1];

                    // 勾配
                    float gradX = (h10 - h00) * (1 - offsetY) + (h11 - h01) * offsetY;
                    float gradY = (h01 - h00) * (1 - offsetX) + (h11 - h10) * offsetX;

                    // 方向の更新 (慣性考慮)
                    dirX = dirX * Inertia - gradX * (1 - Inertia);
                    dirY = dirY * Inertia - gradY * (1 - Inertia);

                    float dirLen = Mathf.Sqrt(dirX * dirX + dirY * dirY);
                    if (dirLen < 0.0001f)
                    {
                        // 勾配ゼロ — ランダム方向
                        float angle = (float)(rng.NextDouble() * Mathf.PI * 2);
                        dirX = Mathf.Cos(angle);
                        dirY = Mathf.Sin(angle);
                    }
                    else
                    {
                        dirX /= dirLen;
                        dirY /= dirLen;
                    }

                    // 移動
                    float newPosX = posX + dirX;
                    float newPosY = posY + dirY;

                    int newCellX = (int)newPosX;
                    int newCellY = (int)newPosY;

                    if (newCellX < 0 || newCellX >= width - 1 || newCellY < 0 || newCellY >= height - 1)
                        break;

                    // 高さの差分
                    float newOffX = newPosX - newCellX;
                    float newOffY = newPosY - newCellY;
                    float newHeight = _heightmap[newCellX, newCellY] * (1 - newOffX) * (1 - newOffY)
                                    + _heightmap[newCellX + 1, newCellY] * newOffX * (1 - newOffY)
                                    + _heightmap[newCellX, newCellY + 1] * (1 - newOffX) * newOffY
                                    + _heightmap[newCellX + 1, newCellY + 1] * newOffX * newOffY;

                    float oldHeight = h00 * (1 - offsetX) * (1 - offsetY)
                                    + h10 * offsetX * (1 - offsetY)
                                    + h01 * (1 - offsetX) * offsetY
                                    + h11 * offsetX * offsetY;

                    float heightDiff = newHeight - oldHeight;

                    // 運搬容量
                    float capacity = Mathf.Max(-heightDiff, MinSlope) * speed * water * SedimentCapacityFactor;

                    if (sediment > capacity || heightDiff > 0)
                    {
                        // 堆積
                        float depositAmount = (heightDiff > 0)
                            ? Mathf.Min(heightDiff, sediment)
                            : (sediment - capacity) * DepositionRate;

                        sediment -= depositAmount;
                        DepositAt(_heightmap, cellX, cellY, offsetX, offsetY, depositAmount);
                    }
                    else
                    {
                        // 侵食
                        float erodeAmount = Mathf.Min((capacity - sediment) * ErosionRate, -heightDiff);
                        sediment += erodeAmount;
                        ErodeAt(_heightmap, cellX, cellY, offsetX, offsetY, erodeAmount);
                    }

                    // 速度と水量の更新
                    speed = Mathf.Sqrt(Mathf.Max(0, speed * speed + heightDiff * Gravity));
                    water *= (1 - EvaporationRate);

                    if (water < 0.001f) break;

                    posX = newPosX;
                    posY = newPosY;
                }
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// バイリニア補間で堆積を分配
        /// </summary>
        private void DepositAt(float[,] _map, int _cx, int _cy, float _ox, float _oy, float _amount)
        {
            _map[_cx, _cy] += _amount * (1 - _ox) * (1 - _oy);
            _map[_cx + 1, _cy] += _amount * _ox * (1 - _oy);
            _map[_cx, _cy + 1] += _amount * (1 - _ox) * _oy;
            _map[_cx + 1, _cy + 1] += _amount * _ox * _oy;
        }

        /// <summary>
        /// バイリニア補間で侵食を分配
        /// </summary>
        private void ErodeAt(float[,] _map, int _cx, int _cy, float _ox, float _oy, float _amount)
        {
            _map[_cx, _cy] -= _amount * (1 - _ox) * (1 - _oy);
            _map[_cx + 1, _cy] -= _amount * _ox * (1 - _oy);
            _map[_cx, _cy + 1] -= _amount * (1 - _ox) * _oy;
            _map[_cx + 1, _cy + 1] -= _amount * _ox * _oy;
        }
        #endregion
    }
}

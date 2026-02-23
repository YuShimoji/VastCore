using UnityEngine;

namespace Vastcore.WorldGen.Common
{
    /// <summary>
    /// 3D 密度グリッド。resolution^3 の float 配列を保持する。
    /// 正 = solid（地形内部）、負 = air（空中）。
    /// </summary>
    public sealed class DensityGrid
    {
        private readonly float[] _data;

        /// <summary>
        /// 各軸のボクセル数。
        /// </summary>
        public int Resolution { get; }

        /// <summary>
        /// 内部配列の総要素数 (Resolution^3)。
        /// </summary>
        public int Length => _data.Length;

        public DensityGrid(int resolution)
        {
            Resolution = resolution;
            _data = new float[resolution * resolution * resolution];
        }

        /// <summary>
        /// 3D インデックスで直接アクセス。
        /// </summary>
        public float this[int x, int y, int z]
        {
            get => _data[FlatIndex(x, y, z)];
            set => _data[FlatIndex(x, y, z)] = value;
        }

        /// <summary>
        /// フラットインデックスで直接アクセス。
        /// </summary>
        public float GetFlat(int index)
        {
            return _data[index];
        }

        /// <summary>
        /// フラットインデックスで直接設定。
        /// </summary>
        public void SetFlat(int index, float value)
        {
            _data[index] = value;
        }

        /// <summary>
        /// 全要素をゼロにリセット。
        /// </summary>
        public void Clear()
        {
            System.Array.Clear(_data, 0, _data.Length);
        }

        /// <summary>
        /// 正規化座標 [0,1] からトリリニア補間でサンプル。
        /// </summary>
        public float SampleNormalized(Vector3 normalized)
        {
            float fx = normalized.x * (Resolution - 1);
            float fy = normalized.y * (Resolution - 1);
            float fz = normalized.z * (Resolution - 1);

            int x0 = Mathf.Clamp((int)fx, 0, Resolution - 2);
            int y0 = Mathf.Clamp((int)fy, 0, Resolution - 2);
            int z0 = Mathf.Clamp((int)fz, 0, Resolution - 2);

            float tx = fx - x0;
            float ty = fy - y0;
            float tz = fz - z0;

            // Trilinear interpolation
            float c000 = this[x0, y0, z0];
            float c100 = this[x0 + 1, y0, z0];
            float c010 = this[x0, y0 + 1, z0];
            float c110 = this[x0 + 1, y0 + 1, z0];
            float c001 = this[x0, y0, z0 + 1];
            float c101 = this[x0 + 1, y0, z0 + 1];
            float c011 = this[x0, y0 + 1, z0 + 1];
            float c111 = this[x0 + 1, y0 + 1, z0 + 1];

            float c00 = Mathf.Lerp(c000, c100, tx);
            float c10 = Mathf.Lerp(c010, c110, tx);
            float c01 = Mathf.Lerp(c001, c101, tx);
            float c11 = Mathf.Lerp(c011, c111, tx);

            float c0 = Mathf.Lerp(c00, c10, ty);
            float c1 = Mathf.Lerp(c01, c11, ty);

            return Mathf.Lerp(c0, c1, tz);
        }

        /// <summary>
        /// 内部配列への直接参照（バッチ処理用）。
        /// </summary>
        public float[] GetRawData()
        {
            return _data;
        }

        private int FlatIndex(int x, int y, int z)
        {
            return x + y * Resolution + z * Resolution * Resolution;
        }
    }
}

using UnityEngine;

namespace Vastcore.WorldGen.Common
{
    /// <summary>
    /// 決定論的な乱数生成器。同一 seed から同一の乱数列を生成する。
    /// WorldGen パイプライン内の全ての乱数源はこのクラスを通す。
    /// </summary>
    public sealed class DeterministicRng
    {
        private uint _state;

        public DeterministicRng(int seed)
        {
            // seed=0 を避ける（xorshift のゼロ固定防止）
            _state = (uint)seed;
            if (_state == 0) _state = 0x6E624EB7u;
            // 初期状態のウォームアップ
            Next();
            Next();
        }

        /// <summary>
        /// seed と追加コンテキストからサブ RNG を派生させる。
        /// 各エンジンやレイヤーに固有の RNG を与えるために使用する。
        /// </summary>
        public DeterministicRng Fork(int context)
        {
            return new DeterministicRng((int)(_state ^ (uint)(context * 0x45D9F3B)));
        }

        /// <summary>
        /// 次の uint 値を生成する (xorshift32)。
        /// </summary>
        public uint Next()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }

        /// <summary>
        /// [0, 1) の float を返す。
        /// </summary>
        public float NextFloat()
        {
            return (Next() & 0xFFFFFF) / (float)0x1000000;
        }

        /// <summary>
        /// [min, max) の float を返す。
        /// </summary>
        public float NextFloat(float min, float max)
        {
            return min + NextFloat() * (max - min);
        }

        /// <summary>
        /// [min, max) の int を返す。
        /// </summary>
        public int NextInt(int min, int max)
        {
            if (min >= max) return min;
            return min + (int)(Next() % (uint)(max - min));
        }

        /// <summary>
        /// [-1, 1] の範囲で各成分を持つ正規化されていない Vector3 を返す。
        /// </summary>
        public Vector3 NextVector3()
        {
            return new Vector3(
                NextFloat(-1f, 1f),
                NextFloat(-1f, 1f),
                NextFloat(-1f, 1f));
        }

        /// <summary>
        /// 単位球面上の点を返す。
        /// </summary>
        public Vector3 NextOnUnitSphere()
        {
            // Marsaglia method
            float u, v, s;
            do
            {
                u = NextFloat(-1f, 1f);
                v = NextFloat(-1f, 1f);
                s = u * u + v * v;
            } while (s >= 1f || s < 1e-8f);

            float factor = 2f * Mathf.Sqrt(1f - s);
            return new Vector3(u * factor, v * factor, 1f - 2f * s);
        }

        /// <summary>
        /// 整数から [0,1) の決定論的ハッシュ値を返す（静的ユーティリティ）。
        /// NoiseHeightmapProvider.HashTo01 と同一アルゴリズム。
        /// </summary>
        public static float HashTo01(int v)
        {
            unchecked
            {
                uint x = (uint)v;
                x ^= x >> 16;
                x *= 0x7feb352d;
                x ^= x >> 15;
                x *= 0x846ca68b;
                x ^= x >> 16;
                return (x & 0xFFFFFF) / (float)0x1000000;
            }
        }
    }
}

using System;

namespace Vastcore.RegionMap
{
    public static class RegionOceanMask
    {
        /// <summary>標高が海面以下で、地域の外周、または指定した外周セルにつながる水域。</summary>
        public static bool[] ConnectedSea(RegionData data, Func<int, bool> openBoundary = null)
        {
            int n = data.Resolution;
            var sea = new bool[data.Height.Length];
            var queue = new int[sea.Length];
            int count = 0;
            for (int i = 0; i < n; i++)
            {
                Boundary(i);
                Boundary(i + (n - 1) * n);
                Boundary(i * n);
                Boundary(i * n + n - 1);
            }
            for (int head = 0; head < count; head++)
            {
                int current = queue[head];
                int x = current % n;
                if (x > 0) Enqueue(current - 1);
                if (x + 1 < n) Enqueue(current + 1);
                if (current >= n) Enqueue(current - n);
                if (current < (n - 1) * n) Enqueue(current + n);
            }
            return sea;

            void Boundary(int i)
            {
                if (openBoundary == null || openBoundary(i)) Enqueue(i);
            }

            void Enqueue(int i)
            {
                if (sea[i] || data.IsLand(i)) return;
                sea[i] = true;
                queue[count++] = i;
            }
        }
    }
}

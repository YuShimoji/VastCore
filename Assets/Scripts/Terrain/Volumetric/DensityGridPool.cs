using System.Collections.Generic;
using Vastcore.WorldGen.Common;

namespace Vastcore.Terrain.Volumetric
{
    /// <summary>
    /// DensityGrid の簡易プール。
    /// チャンク再生成時の GC 発生を抑制する。
    /// </summary>
    public sealed class DensityGridPool
    {
        private readonly Dictionary<int, Stack<DensityGrid>> _poolByResolution =
            new Dictionary<int, Stack<DensityGrid>>();

        /// <summary>
        /// グリッドを取得する。
        /// </summary>
        public DensityGrid Acquire(int resolution)
        {
            if (_poolByResolution.TryGetValue(resolution, out Stack<DensityGrid> stack) && stack.Count > 0)
                return stack.Pop();

            return new DensityGrid(resolution);
        }

        /// <summary>
        /// グリッドを返却する。
        /// </summary>
        public void Release(DensityGrid grid)
        {
            if (grid == null)
                return;

            int resolution = grid.Resolution;
            if (!_poolByResolution.TryGetValue(resolution, out Stack<DensityGrid> stack))
            {
                stack = new Stack<DensityGrid>();
                _poolByResolution.Add(resolution, stack);
            }

            grid.Clear();
            stack.Push(grid);
        }

        /// <summary>
        /// プールを全クリアする。
        /// </summary>
        public void Clear()
        {
            _poolByResolution.Clear();
        }
    }
}

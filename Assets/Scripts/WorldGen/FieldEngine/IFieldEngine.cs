using Vastcore.WorldGen.Common;
using Vastcore.WorldGen.Recipe;

namespace Vastcore.WorldGen.FieldEngine
{
    /// <summary>
    /// Field Engine の入口インターフェース。
    /// </summary>
    public interface IFieldEngine
    {
        /// <summary>
        /// Heightmap レイヤー解決ファクトリ。
        /// </summary>
        IHeightmapFieldFactory HeightmapFieldFactory { get; set; }

        /// <summary>
        /// Recipe から合成密度場を構築する。
        /// </summary>
        IDensityField BuildField(WorldGenRecipe recipe);

        /// <summary>
        /// 密度場からチャンク領域をサンプルして DensityGrid を充填する。
        /// </summary>
        void FillDensityGrid(IDensityField field, DensityGrid grid, ChunkBounds bounds);
    }
}

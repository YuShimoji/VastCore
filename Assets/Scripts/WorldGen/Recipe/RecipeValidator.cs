using System.Collections.Generic;

namespace Vastcore.WorldGen.Recipe
{
    /// <summary>
    /// WorldGenRecipe の整合性検証ユーティリティ。
    /// </summary>
    public static class RecipeValidator
    {
        /// <summary>
        /// Recipe の検証エラー一覧を返す。
        /// </summary>
        public static List<string> Validate(WorldGenRecipe recipe)
        {
            List<string> errors = new List<string>();
            if (recipe == null)
            {
                errors.Add("Recipe is null.");
                return errors;
            }

            if (recipe.worldScale <= 0f)
                errors.Add("worldScale must be > 0.");
            if (recipe.chunkResolution < 8)
                errors.Add("chunkResolution must be >= 8.");
            if (recipe.chunkWorldSize <= 0f)
                errors.Add("chunkWorldSize must be > 0.");
            if (recipe.chunkVerticalCount < 1)
                errors.Add("chunkVerticalCount must be >= 1.");

            if (recipe.layers == null || recipe.layers.Count == 0)
            {
                errors.Add("At least one field layer is required.");
                return errors;
            }

            for (int i = 0; i < recipe.layers.Count; i++)
            {
                FieldLayer layer = recipe.layers[i];
                if (layer == null)
                {
                    errors.Add($"layers[{i}] is null.");
                    continue;
                }

                if (layer.weight < 0f)
                    errors.Add($"layers[{i}].weight must be >= 0.");

                if (layer.layerType == FieldLayerType.Heightmap && layer.heightmapSettings == null)
                    errors.Add($"layers[{i}] heightmapSettings is required for Heightmap type.");
            }

            GraphGenerationSettings graphSettings = recipe.graphSettings;
            if (graphSettings != null && graphSettings.enableGraph)
            {
                if (graphSettings.domainSize.x <= 0f || graphSettings.domainSize.z <= 0f)
                    errors.Add("graphSettings.domainSize x/z must be > 0.");

                if (graphSettings.roadWidthMin <= 0f || graphSettings.roadWidthMax <= 0f)
                    errors.Add("graphSettings road widths must be > 0.");

                if (graphSettings.riverWidthMin <= 0f || graphSettings.riverWidthMax <= 0f)
                    errors.Add("graphSettings river widths must be > 0.");

                if (graphSettings.riverDepth <= 0f)
                    errors.Add("graphSettings.riverDepth must be > 0.");
            }

            return errors;
        }

        /// <summary>
        /// Recipe が有効なら true。
        /// </summary>
        public static bool IsValid(WorldGenRecipe recipe)
        {
            return Validate(recipe).Count == 0;
        }
    }
}

using System.Collections.Generic;

namespace Vastcore.Generation
{
    /// <summary>
    /// マテリアルパレットの選択を行うシステム。
    /// 建物タグプロファイルとの BlendScore を確率重みとしてルーレット選択する。
    /// ComponentSelector と同じアルゴリズムを使用。
    /// </summary>
    public class StructureMaterialSelector
    {
        /// <summary>
        /// 建物タグプロファイルに基づいてマテリアルパレットを選択する。
        /// ブレンドスコアを確率重みとしてルーレット選択を行う。
        /// </summary>
        /// <param name="_buildingProfile">建物のタグプロファイル</param>
        /// <param name="_palettes">選択候補のパレット配列</param>
        /// <param name="_random">乱数生成器</param>
        /// <returns>選択されたパレット。候補がなければ null</returns>
        public StructureMaterialPalette Select(StructureTagProfile _buildingProfile,
            StructureMaterialPalette[] _palettes, System.Random _random)
        {
            if (_buildingProfile == null || _palettes == null || _palettes.Length == 0 || _random == null)
            {
                return null;
            }

            // 各パレットのブレンドスコアを算出
            var candidates = new List<(StructureMaterialPalette palette, float score)>();
            float totalScore = 0f;

            for (int i = 0; i < _palettes.Length; i++)
            {
                if (_palettes[i] == null) continue;
                if (_palettes[i].Affinity == null) continue;

                float score = _buildingProfile.BlendScore(_palettes[i].Affinity);
                if (score > 0f)
                {
                    candidates.Add((_palettes[i], score));
                    totalScore += score;
                }
            }

            if (candidates.Count == 0)
            {
                // スコア 0 の候補も含めて等確率フォールバック
                for (int i = 0; i < _palettes.Length; i++)
                {
                    if (_palettes[i] == null) continue;
                    candidates.Add((_palettes[i], 1f));
                    totalScore += 1f;
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            // ルーレット選択
            float roll = (float)_random.NextDouble() * totalScore;
            float cumulative = 0f;

            for (int i = 0; i < candidates.Count; i++)
            {
                cumulative += candidates[i].score;
                if (roll <= cumulative)
                {
                    return candidates[i].palette;
                }
            }

            return candidates[candidates.Count - 1].palette;
        }

        /// <summary>
        /// 建物タグプロファイルに基づいて最高スコアのパレットを選択する (決定論的)。
        /// ランダム要素なしで最もマッチするパレットを返す。
        /// </summary>
        /// <param name="_buildingProfile">建物のタグプロファイル</param>
        /// <param name="_palettes">選択候補のパレット配列</param>
        /// <returns>最高スコアのパレット。候補がなければ null</returns>
        public StructureMaterialPalette SelectBest(StructureTagProfile _buildingProfile,
            StructureMaterialPalette[] _palettes)
        {
            if (_buildingProfile == null || _palettes == null || _palettes.Length == 0)
            {
                return null;
            }

            StructureMaterialPalette best = null;
            float bestScore = -1f;

            for (int i = 0; i < _palettes.Length; i++)
            {
                if (_palettes[i] == null) continue;

                float score = _palettes[i].BlendScore(_buildingProfile);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = _palettes[i];
                }
            }

            return best;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.RegionMap
{
    /// <summary>実モデルの1変種。LOD1 は無いこともある。</summary>
    public sealed class RegionPropModel
    {
        public string Variant = "";
        public GameObject Lod0;
        public GameObject Lod1;
    }

    /// <summary>実モデルのファイルの組（ファイル名だけ。読み込みは IRegionPropAssetSource）。</summary>
    public sealed class RegionPropModelFiles
    {
        public string Variant = "";
        public string Lod0 = "";
        public string Lod1 = "";
    }

    /// <summary>
    /// 実アセットの読み出し口（DS-011 の差し替え口）。Editor では RegionPropAssetDatabase が
    /// <c>Assets/Art/Models/&lt;ドメイン&gt;/&lt;カタログ ID&gt;/</c> の Unity が取り込めるモデル（FBX・OBJ など）と
    /// <c>Assets/Art/Textures/</c> の画像を返す。
    /// </summary>
    public interface IRegionPropAssetSource
    {
        /// <summary>そのカタログ ID の変種。無ければ空（プレースホルダのまま描く）。</summary>
        IReadOnlyList<RegionPropModel> Models(string domain, string catalogId);

        /// <summary>プロジェクトの中の画像（<c>Assets/</c> からのパス）。無ければ null。</summary>
        Texture2D Texture(string assetPath);
    }

    /// <summary>元のモデルの材質の名前から決める、共有の材質（写真素材の組と色の調子）。</summary>
    public sealed class RegionPropSurface
    {
        public string Key = "";
        public string ColorPath = "";
        public string NormalPath = "";
        public string RoughnessPath = "";
        /// <summary>写真素材の無い材質の色（線形）。</summary>
        public Color Linear = Color.white;
        /// <summary>写真の色に掛けるチャンネルごとの倍率（線形）。地形の色へ寄せる時に使う。</summary>
        public Color Match = Color.white;
        /// <summary>写真の色に掛ける明るさと暖かさ（-1〜1）。</summary>
        public float Value = 1f;
        public float Warmth;
        public float Roughness = 0.85f;
        public float Metallic;
    }

    /// <summary>
    /// 実アセットの選び方。どの変種を置くかと、元の材質をどの共有の材質へ読み替えるかを、Unity の物を作らずに決める（EditMode で試す）。
    /// </summary>
    public static class RegionPropAssets
    {
        /// <summary>Editor が登録する読み出し口。null の間は実アセットを使わない。</summary>
        public static IRegionPropAssetSource Source;

        public const string ModelsRoot = "Assets/Art/Models";
        public const string WoodSet = "Assets/Art/Textures/CNT/weathered_pine_timber/";
        public const string StoneSet = "Assets/Art/Textures/RUB/sandstone_rubble_weathered/";
        public const string GraniteSet = "Assets/Art/Textures/NAT/mossy_granite_outcrop/";
        public const string MeadowTurfSet = "Assets/Art/Textures/NAT/grassland_meadow_turf/";
        public const string MeadowLoamSet = "Assets/Art/Textures/NAT/grassland_loam_soil/";
        public const string MeadowFoliageSet = "Assets/Art/Textures/NAT/meadow_foliage_blade/";
        // 砂は既にある ambientCG Ground054 の色だけを使う（MapGenerator の法線は取り込みの設定が legacy のままなので使わない）
        public const string SandColor = "Assets/Resources/MapGenerator/Textures/Ground054_1K-JPG/Ground054_1K-JPG_Color.jpg";

        /// <summary>
        /// `worn_rock_natural_01_diff_2k` の画素の平均（線形、2048² 全部）。黄白で r/g 1.44。
        /// </summary>
        public static readonly Color StoneTextureMean = new Color(0.3968f, 0.2761f, 0.1449f, 1f);

        /// <summary>
        /// 地形の転がる岩の色の平均（線形、r/g 2.14）。`RegionSandstone.hlsl` の block の式
        /// （(0.56,0.37,0.28)〜(0.85,0.62,0.44) × 鉱物 0.79〜1.22 × むら 0.74〜1.10、sRGB）を一様な乱数で平均して線形にした値。
        /// 本人の選択（2026-11-27「赤い砂岩に合わせる」）で、砂岩の Props の写真の平均をこの色へ寄せる。
        /// </summary>
        public static readonly Color LooseRockMean = new Color(0.4051f, 0.1889f, 0.0957f, 1f);

        /// <summary>Ground054 の色の画素の平均（線形、r/g 1.29）と、地形の土の色の平均（`RegionSandstone.hlsl` の soil の式、線形、r/g 1.73）。</summary>
        public static readonly Color SandTextureMean = new Color(0.3331f, 0.2588f, 0.1562f, 1f);
        public static readonly Color SoilMean = new Color(0.4798f, 0.2779f, 0.1370f, 1f);

        /// <summary>瓦礫の裾の砂（Dryland_Skirt・Fine_Sand）に掛ける倍率。Ground054 の黄色い砂を地形の土の色へそろえる。</summary>
        public static Color SandMatch => new Color(SoilMean.r / SandTextureMean.r, SoilMean.g / SandTextureMean.g, SoilMean.b / SandTextureMean.b, 1f);

        /// <summary>
        /// 画面での補正（線形）。平均の色をそろえただけでは、砂塵・足元のなじみ（V7）と材質の変種で Props の岩が地形の岩より黄色く残った。
        /// 瓦礫の視点（seed 9・8）で、同じ絵の地形の岩と Props の岩の画素の平均の比（チャンネルごと、4 通りの平均）を掛ける（DS-011 8.1節）。
        /// </summary>
        public static readonly Color StoneRenderCalibration = new Color(0.950f, 0.771f, 0.731f, 1f);

        /// <summary>砂岩の写真に掛けるチャンネルごとの倍率（線形）。写真の模様は残し、平均の色だけを地形の岩にそろえる。</summary>
        public static Color StoneMatch => new Color(
            LooseRockMean.r / StoneTextureMean.r * StoneRenderCalibration.r,
            LooseRockMean.g / StoneTextureMean.g * StoneRenderCalibration.g,
            LooseRockMean.b / StoneTextureMean.b * StoneRenderCalibration.b, 1f);

        /// <summary>カタログ ID の頭（nat_・rub_ …）をドメインのフォルダの名前（NAT・RUB …）にする。</summary>
        public static string DomainOf(string catalogId)
        {
            if (string.IsNullOrEmpty(catalogId)) return "";
            int cut = catalogId.IndexOf('_');
            return (cut > 0 ? catalogId.Substring(0, cut) : catalogId).ToUpperInvariant();
        }

        /// <summary>
        /// ファイル名（拡張子つき・なしどちらでも）を変種ごとにまとめる。<c>名前_lod0</c>・<c>名前_lod1</c> は同じ変種の LOD、
        /// 末尾に lod の無いファイルは LOD0 だけの変種。並びは変種の名前の順。
        /// </summary>
        public static List<RegionPropModelFiles> GroupModelFiles(IEnumerable<string> fileNames)
        {
            var byVariant = new SortedDictionary<string, RegionPropModelFiles>(StringComparer.Ordinal);
            foreach (string file in fileNames)
            {
                if (string.IsNullOrEmpty(file)) continue;
                string name = System.IO.Path.GetFileNameWithoutExtension(file);
                string lower = name.ToLowerInvariant();
                int lod = 0;
                string variant = name;
                if (lower.EndsWith("_lod0", StringComparison.Ordinal) || lower.EndsWith("_lod1", StringComparison.Ordinal))
                {
                    lod = lower[lower.Length - 1] - '0';
                    variant = name.Substring(0, name.Length - 5);
                }
                if (!byVariant.TryGetValue(variant, out RegionPropModelFiles entry))
                {
                    entry = new RegionPropModelFiles { Variant = variant };
                    byVariant[variant] = entry;
                }
                if (lod == 0) entry.Lod0 = file;
                else entry.Lod1 = file;
            }
            var result = new List<RegionPropModelFiles>();
            foreach (RegionPropModelFiles entry in byVariant.Values)
                if (entry.Lod0.Length > 0) result.Add(entry);
            return result;
        }

        /// <summary>
        /// 置く変種の番号。損傷・組み方・形の値（intact・broken_mid・fallen_drum など）が変種の名前に含まれればその中から、
        /// 含まれなければ全部から選ぶ。同じ群れ（ClusterIndex）の中では MemberIndex が隣の物どうしで別の変種になる
        /// （瓦礫を2つ以上並べても同じ物に見えないように）。
        /// </summary>
        public static int PickVariant(RegionPropInstance inst, IReadOnlyList<string> variants)
        {
            if (variants == null || variants.Count == 0) return -1;
            var candidates = new List<int>();
            foreach (string token in MatchTokens(inst))
            {
                for (int i = 0; i < variants.Count; i++)
                    if (variants[i].IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0 && !candidates.Contains(i))
                        candidates.Add(i);
                if (candidates.Count > 0) break;
            }
            if (candidates.Count == 0)
                for (int i = 0; i < variants.Count; i++) candidates.Add(i);
            uint h = RegionNoise.Hash(inst.ClusterIndex, StableHash(inst.PropId), 4127u);
            int start = (int)(h % (uint)candidates.Count);
            return candidates[(start + Math.Max(0, inst.MemberIndex) + Math.Max(0, inst.StackLayer)) % candidates.Count];
        }

        private static IEnumerable<string> MatchTokens(RegionPropInstance inst)
        {
            // 直接の値を先に、言い換え（pristine→intact、broken→damaged など）を後に試す
            // 木箱は正確な形の指定を組み方の部分一致より先に見る（slat_open は破損型の名前にも入る）。
            string[] direct = inst.PropId == "cnt_wood_crate"
                ? new[] { inst.DamageState, inst.MeshVariant, inst.ConstructionVariant }
                : new[] { inst.DamageState, inst.ConstructionVariant, inst.MeshVariant };
            foreach (string value in direct)
                if (!string.IsNullOrEmpty(value) && value != "default") yield return value;
            string damage = (inst.DamageState ?? "").ToLowerInvariant();
            if (damage.StartsWith("collapsed", StringComparison.Ordinal) || damage.StartsWith("ruined", StringComparison.Ordinal))
                yield return "collapsed";
            if (damage.Length > 0 && damage != "pristine" && damage != "intact")
                yield return "damaged";
            if (damage == "pristine" || damage == "worn_chipped" || damage == "worn")
                yield return "intact";
        }

        private static int StableHash(string s)
        {
            unchecked
            {
                int h = (int)2166136261;
                if (s != null)
                    foreach (char c in s) h = (h ^ c) * 16777619;
                return h;
            }
        }

        /// <summary>
        /// 元のモデルの材質の名前（GameProjects の GLB の材質名。wood・Sandstone_PBR_Pale・P0_Fine_Sand など）から共有の材質を決める。
        /// 色の調子は名前の Pale・Warm・Deep・Dark などで変える。
        /// </summary>
        public static RegionPropSurface SurfaceFor(string sourceMaterialName)
        {
            string raw = sourceMaterialName ?? "";
            int instanceSuffix = raw.IndexOf(" (Instance)", StringComparison.Ordinal);
            if (instanceSuffix >= 0) raw = raw.Substring(0, instanceSuffix);
            string n = raw.ToLowerInvariant();
            var s = new RegionPropSurface { Key = n };
            if (n.Contains("iron") || n.Contains("metal"))
            {
                s.Key = "iron";
                s.Linear = new Color(0.103f, 0.086f, 0.071f, 1f);
                s.Metallic = 0.72f;
                s.Roughness = 0.58f;
                return s;
            }
            if (n.Contains("pigment") || n.Contains("paint"))
            {
                s.Key = "pigment";
                s.Linear = new Color(0.16f, 0.079f, 0.033f, 1f);
                s.Roughness = 0.83f;
                return s;
            }
            if (n.Contains("wood") || n.Contains("plank") || n.Contains("timber") || n.Contains("bark"))
            {
                s.ColorPath = WoodSet + "raw_plank_wall_diff_2k.jpg";
                s.NormalPath = WoodSet + "raw_plank_wall_nor_gl_2k.jpg";
                s.RoughnessPath = WoodSet + "raw_plank_wall_rough_2k.jpg";
                if (n.Contains("charred")) s.Value = 0.36f;
                else if (n.Contains("bark")) { s.Value = 0.62f; s.Warmth = 0.1f; s.Roughness = 0.92f; }
                else if (n.Contains("bleached") || n.Contains("drift")) { s.Value = 1.22f; s.Warmth = -0.6f; }
                else if (n.Contains("light")) s.Value = 1.12f;
                else if (n.Contains("split")) s.Value = 1.05f;
                else if (n.Contains("end")) { s.Value = 0.88f; s.Warmth = 0.2f; }
                return s;
            }
            if (n.Contains("rope") || n.Contains("lashing"))
            {
                s.ColorPath = WoodSet + "raw_plank_wall_diff_2k.jpg";
                s.NormalPath = WoodSet + "raw_plank_wall_nor_gl_2k.jpg";
                s.RoughnessPath = WoodSet + "raw_plank_wall_rough_2k.jpg";
                s.Value = 1.18f;
                s.Warmth = 0.35f;
                s.Roughness = 0.94f;
                return s;
            }
            if (n.Contains("granite"))
            {
                s.ColorPath = GraniteSet + "mossy_granite_outcrop_diff_1k.jpg";
                s.NormalPath = GraniteSet + "mossy_granite_outcrop_nor_gl_1k.jpg";
                s.RoughnessPath = GraniteSet + "mossy_granite_outcrop_rough_1k.jpg";
                s.Roughness = 0.85f;
                if (n.Contains("pale") || n.Contains("limestone")) { s.Value = 1.16f; s.Warmth = 0.15f; }
                else if (n.Contains("shaded")) { s.Value = 0.78f; s.Warmth = -0.15f; }
                else if (n.Contains("mossy")) { s.Value = 0.98f; s.Warmth = -0.10f; }
                return s;
            }
            if (n.Contains("turf"))
            {
                s.ColorPath = MeadowTurfSet + "Grass005_1K-JPG_Color.jpg";
                s.NormalPath = MeadowTurfSet + "Grass005_1K-JPG_NormalGL.jpg";
                s.RoughnessPath = MeadowTurfSet + "Grass005_1K-JPG_Roughness.jpg";
                s.Roughness = 0.90f;
                return s;
            }
            if (n.Contains("loam"))
            {
                s.ColorPath = MeadowLoamSet + "Ground009_1K-JPG_Color.jpg";
                s.NormalPath = MeadowLoamSet + "Ground009_1K-JPG_NormalGL.jpg";
                s.RoughnessPath = MeadowLoamSet + "Ground009_1K-JPG_Roughness.jpg";
                s.Roughness = 0.95f;
                return s;
            }
            if (n.Contains("grass") || n.Contains("foliage") || n.Contains("shrub") || n.Contains("leaf"))
            {
                s.ColorPath = MeadowFoliageSet + "meadow_foliage_blade_diff_1k.jpg";
                s.NormalPath = MeadowFoliageSet + "meadow_foliage_blade_nor_gl_1k.jpg";
                s.RoughnessPath = MeadowFoliageSet + "meadow_foliage_blade_rough_1k.jpg";
                s.Roughness = 0.68f;
                if (n.Contains("seeded") || n.Contains("ochre")) { s.Value = 1.14f; s.Warmth = 0.85f; }
                else if (n.Contains("shrub") || n.Contains("leaf")) { s.Value = 0.86f; s.Warmth = -0.15f; }
                else { s.Value = 1.02f; s.Warmth = -0.05f; }
                return s;
            }
            if ((n.Contains("sand") && !n.Contains("sandstone")) || n.Contains("skirt") || n.Contains("ash"))
            {
                s.ColorPath = SandColor;
                s.Roughness = 0.95f;
                if (n.Contains("ash")) { s.Value = 0.55f; s.Warmth = -0.8f; }
                else s.Match = SandMatch;
                return s;
            }
            // 残りは砂岩（sandstone・Sandstone_PBR_*・P0_Sandstone*・P0_Monolith_Stone・P0_Charred_Stone）
            s.ColorPath = StoneSet + "worn_rock_natural_01_diff_2k.jpg";
            s.NormalPath = StoneSet + "worn_rock_natural_01_nor_gl_2k.jpg";
            s.Match = StoneMatch;
            s.Roughness = 0.88f;
            if (n.Contains("charred")) s.Value = 0.42f;
            else if (n.Contains("pale")) { s.Value = 1.14f; s.Warmth = -0.2f; }
            else if (n.Contains("light")) s.Value = 1.10f;
            else if (n.Contains("warm")) { s.Value = 1.0f; s.Warmth = 0.6f; }
            else if (n.Contains("deep")) { s.Value = 0.80f; s.Warmth = 0.3f; }
            else if (n.Contains("dark")) s.Value = 0.74f;
            else if (n.Contains("monolith")) { s.Value = 0.9f; s.Warmth = -0.5f; }
            return s;
        }

        /// <summary>
        /// モデル1つが群れ全体（大きな塊・中くらいの欠片・細かな破片の3層）を持つ Props。GameProjects の瓦礫の GLB は
        /// 1ファイルが1配置（clusterRole CorePile、50 前後の部品）なので、配置の CorePile にだけ置き、中と細かい層の個体はモデルが受け持つ。
        /// </summary>
        public static bool IsClusterModel(string catalogId)
        {
            return catalogId == "rub_stone_rubble_pile" || catalogId == "rub_masonry_blocks";
        }

        /// <summary>群れ全体のモデルの時、この個体は CorePile のモデルが受け持つので置かない。</summary>
        public static bool CoveredByClusterModel(RegionPropInstance inst)
        {
            return IsClusterModel(inst.PropId) && inst.Role != "CorePile";
        }

        /// <summary>
        /// 実モデルを置く時の倍率。モデルの横の大きい方の幅を、カタログの寸法×個体の倍率の横の大きい方へ合わせ（配置の計算と同じ大きさにする）、
        /// 個体の縦横比（V1 の不均一な倍率）は残す。
        /// </summary>
        public static Vector3 FitScale(RegionPropInstance inst, Vector3 modelSize)
        {
            // 群れ全体のモデルは作った大きさのまま（個体の倍率の平均だけ掛ける）。CorePile の寸法は塊1つの大きさなので合わせない
            if (IsClusterModel(inst.PropId))
            {
                float mean = (inst.ScaleX + inst.ScaleY + inst.ScaleZ) / 3f;
                return new Vector3(mean, mean, mean);
            }
            float targetXZ = Math.Max(inst.DimensionsX * inst.ScaleX, inst.DimensionsZ * inst.ScaleZ);
            float modelXZ = Math.Max(modelSize.x, modelSize.z);
            float s = modelXZ > 1e-4f && targetXZ > 1e-4f ? Mathf.Clamp(targetXZ / modelXZ, 0.05f, 8f) : 1f;
            float meanXZ = Math.Max(1e-4f, 0.5f * (inst.ScaleX + inst.ScaleZ));
            return new Vector3(s * inst.ScaleX / meanXZ, s * inst.ScaleY / meanXZ, s * inst.ScaleZ / meanXZ);
        }
    }
}

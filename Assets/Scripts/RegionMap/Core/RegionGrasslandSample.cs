using System;
using System.Collections.Generic;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 草原バイオーム（RegionBiome.Grassland）向け DS-012 環境セットの配置生成。
    /// Mesa=0・Sediment=0 の草原座標上で、8 ファミリ・19 変種（花崗岩露頭・草株・低木・ターフ裾・小石・苔流木・木柵・道標）を
    /// 地形高さ（scene.HeightAt）と局所斜面に沿わせて決定的に組む。
    /// </summary>
    public static class RegionGrasslandSample
    {
        private readonly struct SampleItem
        {
            public readonly string PropId;
            public readonly string Variant;
            public readonly string Role;
            public readonly float Dx, Dz;
            public readonly float YawDeg;
            public readonly float DimX, DimY, DimZ;
            public readonly float EmbedRatio;
            public readonly float SkirtM;
            public readonly float TopDust;
            public readonly float MossMask;

            public SampleItem(string propId, string variant, string role, float dx, float dz, float yawDeg,
                float dimX, float dimY, float dimZ, float embedRatio, float skirtM, float topDust, float mossMask)
            {
                PropId = propId;
                Variant = variant;
                Role = role;
                Dx = dx;
                Dz = dz;
                YawDeg = yawDeg;
                DimX = dimX;
                DimY = dimY;
                DimZ = dimZ;
                EmbedRatio = embedRatio;
                SkirtM = skirtM;
                TopDust = topDust;
                MossMask = mossMask;
            }
        }

        public static RegionPropProofSlice BuildSlice(RegionLookScene scene, int seedOffset = 0)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));
            uint seed = unchecked((uint)(scene.RegionSeed + seedOffset) * 2654435761u + 907u);

            float baseX = scene.CenterX;
            float baseZ = scene.CenterZ;
            FindBestMeadowSpot(scene, baseX, baseZ, 28f, out float anchorX, out float anchorZ);

            RegionBiomeSurfaceProfile profile = scene.BiomeSurface ?? RegionBiomeSurface.ProfileFor(RegionBiome.Grassland);

            var slice = new RegionPropProofSlice
            {
                Biome = scene.PrimaryBiome,
                RenderedHeightAt = scene.RenderedHeightAt,
                UseGrasslandVariants = true,
                HasGroundColor = true,
                GroundRed = profile.GroundSkirtR,
                GroundGreen = profile.GroundSkirtG,
                GroundBlue = profile.GroundSkirtB,
                CratesCenterX = anchorX,
                CratesCenterZ = anchorZ,
                RubbleCenterX = anchorX - 6f,
                RubbleCenterZ = anchorZ - 4f,
            };

            var items = new List<SampleItem>
            {
                // 1. 花崗岩露頭（主役クラスタ + 草地ターフ襟）
                new SampleItem("nat_grassland_outcrop", "nat_grassland_outcrop__moss_capped_boulder", "Primary", -4.4f, -3.0f, 22f, 2.35f, 1.42f, 1.95f, 0.24f, 0.24f, 0.04f, 0.72f),
                new SampleItem("nat_grassland_outcrop", "nat_grassland_outcrop__rounded_granite_shelf", "Secondary", -6.6f, -2.1f, 68f, 2.0f, 1.02f, 1.62f, 0.22f, 0.20f, 0.04f, 0.58f),
                new SampleItem("nat_grassland_outcrop", "nat_grassland_outcrop__split_fieldstone_cluster", "Accent", -2.5f, -4.5f, 134f, 1.65f, 0.92f, 1.42f, 0.20f, 0.18f, 0.03f, 0.54f),

                // 2. 地面と構造物の馴染ませ用ターフ裾・小道縁
                new SampleItem("nat_meadow_turf_skirt", "nat_meadow_turf_skirt__turf_mound_ring", "SkirtCollar", -4.4f, -3.0f, 24f, 3.25f, 0.42f, 2.85f, 0.14f, 0.06f, 0.01f, 0.25f),
                new SampleItem("nat_meadow_turf_skirt", "nat_meadow_turf_skirt__worn_trail_verge", "TrailVerge", 1.2f, 1.8f, 14f, 3.3f, 0.30f, 2.2f, 0.14f, 0.06f, 0.02f, 0.18f),

                // 3. 小石・砂利パッチ（小道と露頭裾）
                new SampleItem("nat_pebble_cluster", "nat_pebble_cluster__mossy_field_cobbles", "CobblePatch", -5.2f, -1.3f, 40f, 2.0f, 0.28f, 1.75f, 0.26f, 0.10f, 0.02f, 0.62f),
                new SampleItem("nat_pebble_cluster", "nat_pebble_cluster__trail_gravel_patch", "TrailGravel", 0.3f, 1.3f, 108f, 2.25f, 0.22f, 1.65f, 0.26f, 0.08f, 0.04f, 0.22f),

                // 4. 苔むした倒木（石積み道標と視覚的に重ならない位置へ配置）
                new SampleItem("nat_dry_root_log", "nat_dry_root_log__mossy_fallen_branch", "FallenLog", -2.9f, -0.4f, 48f, 3.0f, 0.52f, 0.68f, 0.22f, 0.12f, 0.03f, 0.68f),

                // 5. 木柵 4 変種（12度方向に等間隔 2.02m で連続する牧草地の柵）
                new SampleItem("str_wooden_fence", "str_wooden_fence__post_end_cairn", "FenceAnchor", -1.45f, 4.45f, 12f, 0.76f, 1.18f, 0.76f, 0.14f, 0.14f, 0.02f, 0.42f),
                new SampleItem("str_wooden_fence", "str_wooden_fence__rail_2bar_intact", "FenceSpanA", 0.52f, 4.03f, 12f, 2.08f, 1.14f, 0.28f, 0.10f, 0.14f, 0.01f, 0.22f),
                new SampleItem("str_wooden_fence", "str_wooden_fence__rail_3bar_pasture", "FenceSpanB", 2.50f, 3.61f, 12f, 2.08f, 1.18f, 0.28f, 0.10f, 0.14f, 0.01f, 0.22f),
                new SampleItem("str_wooden_fence", "str_wooden_fence__sagging_weathered_span", "FenceSpanC", 4.48f, 3.19f, 14f, 2.08f, 1.08f, 0.34f, 0.10f, 0.14f, 0.02f, 0.36f),

                // 6. 道標 2 変種（奥の小道標柱 + 右前方の苔ケルン）
                new SampleItem("set_trail_signpost", "set_trail_signpost__meadow_wood_post", "TrailPost", 5.9f, 1.6f, -26f, 0.92f, 1.78f, 0.50f, 0.11f, 0.14f, 0.01f, 0.24f),
                new SampleItem("set_trail_signpost", "set_trail_signpost__mossy_stone_cairn", "TrailCairn", 1.7f, 1.1f, 18f, 0.96f, 1.32f, 0.96f, 0.16f, 0.16f, 0.03f, 0.64f),

                // 7. 低木 2 変種
                new SampleItem("nat_meadow_shrub", "nat_meadow_shrub__leafy_hedge_shrub", "ShrubA", -6.2f, -4.4f, 48f, 1.65f, 1.25f, 1.55f, 0.14f, 0.12f, 0.01f, 0.08f),
                new SampleItem("nat_meadow_shrub", "nat_meadow_shrub__windswept_low_bush", "ShrubB", 5.2f, 4.5f, 128f, 1.55f, 1.08f, 1.42f, 0.14f, 0.12f, 0.01f, 0.08f),
            };

            // 8. 草原の草株 3 変種を柵・露頭・小道沿いへ自然に密生させる
            string[] grassVariants =
            {
                "nat_grass_tussock__lush_meadow_clump",
                "nat_grass_tussock__seeded_tall_grass",
                "nat_grass_tussock__trailside_short_tuft"
            };
            float[,] grassOffsets =
            {
                { -3.1f, -2.0f, 1.05f }, { -5.3f, -4.7f, 1.12f }, { -1.9f, -5.2f, 0.96f },
                { -7.1f, -3.1f, 1.15f }, { -3.8f, 0.9f, 0.92f }, { -0.8f, 4.7f, 1.08f },
                { 1.2f, 4.4f, 1.02f }, { 3.3f, 3.9f, 1.12f }, { 5.4f, 3.3f, 1.04f },
                { 4.3f, 1.3f, 0.96f }, { 6.6f, 0.8f, 1.16f }, { -4.4f, 2.8f, 0.95f },
                { -0.5f, -0.3f, 0.90f }, { 2.6f, -1.5f, 1.10f }, { 0.8f, -3.4f, 1.06f },
                { -7.5f, -0.4f, 1.18f }, { 7.2f, 2.5f, 1.08f }, { 3.1f, 5.4f, 1.14f },
                { -2.0f, 3.8f, 1.02f }, { 0.1f, 3.5f, 0.94f }, { 2.0f, 2.9f, 1.06f },
                { 4.1f, 2.4f, 0.98f }, { -5.9f, 0.4f, 1.08f }, { -1.3f, -2.2f, 1.12f },
                { 2.5f, 0.3f, 0.92f }, { 4.8f, -0.6f, 1.08f }, { -0.8f, 5.5f, 1.04f },
                { 1.9f, 5.1f, 1.10f },
            };
            for (int g = 0; g < grassOffsets.GetLength(0); g++)
            {
                string variant = grassVariants[g % grassVariants.Length];
                float gx = grassOffsets[g, 0];
                float gz = grassOffsets[g, 1];
                float sc = grassOffsets[g, 2];
                float yaw = RegionNoise.Hash01(g, 31, seed) * 360f;
                bool tall = variant.Contains("seeded");
                bool shortTuft = variant.Contains("short");
                float h = tall ? 0.88f * sc : (shortTuft ? 0.44f * sc : 0.64f * sc);
                float w = (shortTuft ? 0.84f : 1.02f) * sc;
                items.Add(new SampleItem("nat_grass_tussock", variant, "MeadowTuft_" + g, gx, gz, yaw, w, h, w, 0.16f, 0.06f, 0.0f, 0.05f));
            }

            for (int i = 0; i < items.Count; i++)
            {
                SampleItem item = items[i];
                float px = anchorX + item.Dx;
                float pz = anchorZ + item.Dz;
                float py = scene.RenderedHeightAt(px, pz);
                RegionPropPlacementSolver.SampleSlope(scene, px, pz, out float slopeDeg, out float sx, out float sz);

                bool upright = item.PropId == "str_wooden_fence" || item.PropId == "set_trail_signpost";
                float maxTilt = upright ? 4f : (item.PropId == "nat_grass_tussock" || item.PropId == "nat_meadow_shrub" ? 12f : 18f);
                float pitch = 0f, roll = 0f;
                if (!upright)
                {
                    float yawRad = item.YawDeg * (float)(Math.PI / 180.0);
                    float fwdX = (float)Math.Sin(yawRad), fwdZ = -(float)Math.Cos(yawRad);
                    float rightX = (float)Math.Cos(yawRad), rightZ = (float)Math.Sin(yawRad);
                    float follow = Math.Min(slopeDeg, maxTilt) * 0.72f;
                    pitch = (sx * fwdX + sz * fwdZ) * follow;
                    roll = (sx * rightX + sz * rightZ) * follow;
                }

                float embed = item.DimY * item.EmbedRatio;
                slice.Instances.Add(new RegionPropInstance
                {
                    PropId = item.PropId,
                    Role = item.Role,
                    ClusterIndex = i + 1,
                    MemberIndex = 0,
                    X = px,
                    Y = py - embed,
                    Z = pz,
                    YawDeg = item.YawDeg,
                    PitchDeg = pitch,
                    RollDeg = roll,
                    ScaleX = 1f,
                    ScaleY = 1f,
                    ScaleZ = 1f,
                    DimensionsX = item.DimX,
                    DimensionsY = item.DimY,
                    DimensionsZ = item.DimZ,
                    EmbedDepthM = embed,
                    PivotRule = upright ? RegionPropPivotRule.ModularSnap : RegionPropPivotRule.NaturalEmbed,
                    MeshVariant = item.Variant,
                    MaterialVariant = item.PropId,
                    DamageState = "sample",
                    ConstructionVariant = item.PropId,
                    TintShift = (RegionNoise.Hash01(i, 71, seed) - 0.5f) * 0.16f,
                    TopDust = item.TopDust * profile.TopDustScale,
                    GroundSkirtBlendM = item.SkirtM,
                    MossMask = Math.Min(1f, item.MossMask * profile.MossMaskScale),
                });
            }

            float relE = anchorX - scene.CenterX;
            float relS = anchorZ - scene.CenterZ;
            slice.GrasslandEyeViewpoint = new RegionLookViewpoint(
                "grassland-eye",
                relE - 3.6f, relS + 14.1f, 1.55f,
                relE + 0.8f, relS - 10f, -1.8f,
                44f);
            slice.GrasslandPanoramaViewpoint = new RegionLookViewpoint(
                "grassland-panorama",
                relE - 14.5f, relS + 16.0f, 5.2f,
                relE - 0.4f, relS - 0.2f, 0.7f,
                44f);
            slice.GrasslandOutcropViewpoint = new RegionLookViewpoint(
                "grassland-outcrop",
                relE - 0.4f, relS + 2.8f, 2.2f,
                relE - 4.1f, relS - 3.3f, 0.65f,
                46f);
            slice.GrasslandTrailViewpoint = new RegionLookViewpoint(
                "grassland-trail",
                relE - 6.8f, relS + 9.2f, 2.1f,
                relE + 2.2f, relS + 2.6f, 0.85f,
                44f);
            slice.CratesViewpoint = slice.GrasslandTrailViewpoint;
            slice.RubbleViewpoint = slice.GrasslandOutcropViewpoint;

            return slice;
        }

        private static void FindBestMeadowSpot(RegionLookScene scene, float startX, float startZ, float radius, out float bestX, out float bestZ)
        {
            bestX = startX;
            bestZ = startZ;
            float bestScore = float.NegativeInfinity;
            for (int dz = -3; dz <= 3; dz++)
            {
                for (int dx = -3; dx <= 3; dx++)
                {
                    float cx = startX + dx * (radius / 3f);
                    float cz = startZ + dz * (radius / 3f);
                    float h = scene.HeightAt(cx, cz);
                    if (h < 15f) continue;
                    RegionPropPlacementSolver.SampleSlope(scene, cx, cz, out float slopeDeg, out _, out _);
                    // 3〜10 度の穏やかな草地斜面を優先
                    float slopeScore = 18f - Math.Abs(slopeDeg - 6f);
                    float distPenalty = 0.08f * (float)Math.Sqrt((cx - startX) * (cx - startX) + (cz - startZ) * (cz - startZ));
                    float score = slopeScore - distPenalty;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestX = cx;
                        bestZ = cz;
                    }
                }
            }
        }
    }
}

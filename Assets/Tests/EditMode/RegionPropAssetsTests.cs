using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Vastcore.RegionMap;

namespace Vastcore.Tests.EditMode
{
    /// <summary>DS-011 の実アセットの差し替え口（RegionPropAssets）の選び方。</summary>
    public class RegionPropAssetsTests
    {
        [Test]
        public void GroupModelFiles_PairsLodsAndKeepsSingleFiles()
        {
            List<RegionPropModelFiles> groups = RegionPropAssets.GroupModelFiles(new[]
            {
                "x/str_ruin_pillar__intact_column_lod1.fbx",
                "x/str_ruin_pillar__intact_column_lod0.fbx",
                "x/wall-intact.fbx",
                "x/only_lod1_lod1.fbx"
            });
            Assert.AreEqual(2, groups.Count, "LOD0 の無い組は使わない");
            Assert.AreEqual("str_ruin_pillar__intact_column", groups[0].Variant);
            Assert.AreEqual("x/str_ruin_pillar__intact_column_lod0.fbx", groups[0].Lod0);
            Assert.AreEqual("x/str_ruin_pillar__intact_column_lod1.fbx", groups[0].Lod1);
            Assert.AreEqual("wall-intact", groups[1].Variant);
            Assert.AreEqual("", groups[1].Lod1);
        }

        [Test]
        public void PickVariant_MatchesDamageStateToVariantName()
        {
            string[] pillars =
            {
                "str_ruin_pillar__broken_mid_diagonal", "str_ruin_pillar__fallen_drum_segment",
                "str_ruin_pillar__intact_column", "str_ruin_pillar__stump_base_only"
            };
            Assert.AreEqual(1, RegionPropAssets.PickVariant(new RegionPropInstance { PropId = "str_ruin_pillar", DamageState = "fallen_drum" }, pillars));
            Assert.AreEqual(0, RegionPropAssets.PickVariant(new RegionPropInstance { PropId = "str_ruin_pillar", DamageState = "broken_mid" }, pillars));
            string[] markers = { "marker-damaged", "marker-intact" };
            Assert.AreEqual(1, RegionPropAssets.PickVariant(new RegionPropInstance { PropId = "set_trail_signpost", DamageState = "pristine" }, markers));
            Assert.AreEqual(0, RegionPropAssets.PickVariant(new RegionPropInstance { PropId = "set_trail_signpost", DamageState = "broken_board" }, markers));
        }

        [Test]
        public void PickVariant_NeighbouringClusterMembersGetDifferentVariants()
        {
            string[] rubble = { "rubble_cone_a", "rubble_cone_b", "rubble_low_radial", "rubble_split_mound" };
            var seen = new HashSet<int>();
            for (int member = 0; member < 4; member++)
            {
                var inst = new RegionPropInstance { PropId = "rub_stone_rubble_pile", ClusterIndex = 10, MemberIndex = member, DamageState = "scattered" };
                int pick = RegionPropAssets.PickVariant(inst, rubble);
                Assert.That(pick, Is.InRange(0, 3));
                seen.Add(pick);
            }
            Assert.AreEqual(4, seen.Count, "同じ群れの4つが4種に分かれる");
            var again = new RegionPropInstance { PropId = "rub_stone_rubble_pile", ClusterIndex = 10, MemberIndex = 2, DamageState = "scattered" };
            Assert.AreEqual(RegionPropAssets.PickVariant(again, rubble), RegionPropAssets.PickVariant(again, rubble), "決定的");
            Assert.AreEqual(-1, RegionPropAssets.PickVariant(again, new string[0]));
        }

        [Test]
        public void SurfaceFor_MapsGameProjectsMaterialNames()
        {
            Assert.AreEqual(RegionPropAssets.WoodSet + "raw_plank_wall_diff_2k.jpg", RegionPropAssets.SurfaceFor("wood-light").ColorPath);
            Assert.AreEqual(RegionPropAssets.WoodSet + "raw_plank_wall_rough_2k.jpg", RegionPropAssets.SurfaceFor("P0_Charred_Wood").RoughnessPath);
            Assert.Less(RegionPropAssets.SurfaceFor("P0_Charred_Wood").Value, 0.5f);
            RegionPropSurface pale = RegionPropAssets.SurfaceFor("Sandstone_PBR_Pale (Instance)");
            RegionPropSurface deep = RegionPropAssets.SurfaceFor("Sandstone_PBR_Deep");
            Assert.AreEqual(RegionPropAssets.StoneSet + "worn_rock_natural_01_diff_2k.jpg", pale.ColorPath);
            Assert.AreEqual("", pale.RoughnessPath, "worn_rock_natural_01 は粗さの画像が無い");
            Assert.Greater(pale.Value, deep.Value);
            // 砂岩の写真の平均に倍率を掛けると、地形の転がる岩の色（r/g 2.14・b/g 0.51）に画面での補正を掛けた色になる
            Color matched = RegionPropAssets.StoneTextureMean * pale.Match;
            Color target = RegionPropAssets.LooseRockMean * RegionPropAssets.StoneRenderCalibration;
            Assert.AreEqual(target.r / target.g, matched.r / matched.g, 1e-3f);
            Assert.AreEqual(target.b / target.g, matched.b / matched.g, 1e-3f);
            Assert.Greater(matched.r / matched.g, 2f, "黄白の写真（r/g 1.44）を赤い砂岩へ寄せる");
            Assert.AreEqual(Color.white, RegionPropAssets.SurfaceFor("wood-light").Match, "木は写真の色のまま");
            Color sand = RegionPropAssets.SandTextureMean * RegionPropAssets.SurfaceFor("Dryland_Skirt_PBR").Match;
            Assert.AreEqual(RegionPropAssets.SoilMean.r / RegionPropAssets.SoilMean.g, sand.r / sand.g, 1e-3f, "裾の砂は地形の土の色");
            Assert.AreEqual(Color.white, RegionPropAssets.SurfaceFor("P0_Ash").Match, "灰は暖かさで灰色にする");
            Assert.AreEqual(RegionPropAssets.SandColor, RegionPropAssets.SurfaceFor("Dryland_Skirt_PBR").ColorPath);
            Assert.AreEqual(RegionPropAssets.SandColor, RegionPropAssets.SurfaceFor("P0_Fine_Sand").ColorPath);
            Assert.AreEqual(RegionPropAssets.StoneSet + "worn_rock_natural_01_diff_2k.jpg", RegionPropAssets.SurfaceFor("sandstone-dark").ColorPath);
            RegionPropSurface iron = RegionPropAssets.SurfaceFor("iron");
            Assert.AreEqual("", iron.ColorPath);
            Assert.Greater(iron.Metallic, 0.5f);
        }

        [Test]
        public void ResolveTilt_LeansTowardTheDownhillSideInUnity()
        {
            RegionPropCatalog catalog = RegionPropCatalog.LoadDefault();
            RegionPropDefinition rubble = catalog.GetProp("rub_stone_rubble_pile");
            var v1 = rubble.VariationRules.V1_Transform;
            float[,] downhill = { { 0f, 1f }, { 1f, 0f }, { 0f, -1f }, { -0.6f, 0.8f } };
            for (int d = 0; d < downhill.GetLength(0); d++)
            for (float yaw = 0f; yaw < 360f; yaw += 45f)
            {
                float dx = downhill[d, 0], dz = downhill[d, 1];
                RegionPropPlacementSolver.ResolveTilt(rubble, v1, 16f, dx, dz, yaw, 28f, 0.5f, 0.5f, 1f, out float pitch, out float roll);
                Vector3 up = Quaternion.Euler(pitch, yaw, -roll) * Vector3.up;
                // Core の下り (dx, dz) は Unity で (dx, 0, -dz)
                float lean = Vector3.Dot(up, new Vector3(dx, 0f, -dz));
                Assert.AreEqual(Mathf.Sin(16f * 0.7f * Mathf.Deg2Rad), lean, 0.02f, "下りへ傾く d=" + d + " yaw=" + yaw);
            }
        }

        [Test]
        public void FitScale_MatchesCatalogFootprintAndKeepsAspect()
        {
            var inst = new RegionPropInstance { DimensionsX = 2f, DimensionsZ = 1f, ScaleX = 1.2f, ScaleY = 0.9f, ScaleZ = 1.2f };
            Vector3 s = RegionPropAssets.FitScale(inst, new Vector3(1.2f, 3f, 0.6f));
            Assert.AreEqual(2f, s.x, 1e-4f, "横の大きい方 2×1.2 を 1.2 へ合わせる");
            Assert.AreEqual(2f * 0.9f / 1.2f, s.y, 1e-4f);
            var core = new RegionPropInstance { PropId = "rub_stone_rubble_pile", Role = "CorePile", DimensionsX = 3f, DimensionsZ = 3f, ScaleX = 1.1f, ScaleY = 1.1f, ScaleZ = 1.1f };
            Assert.AreEqual(1.1f, RegionPropAssets.FitScale(core, new Vector3(2f, 0.4f, 2f)).x, 1e-4f, "群れ全体のモデルは作った大きさのまま");
            Assert.IsFalse(RegionPropAssets.CoveredByClusterModel(core));
            Assert.IsTrue(RegionPropAssets.CoveredByClusterModel(new RegionPropInstance { PropId = "rub_stone_rubble_pile", Role = "MicroScatter" }));
            Assert.IsFalse(RegionPropAssets.CoveredByClusterModel(new RegionPropInstance { PropId = "nat_stratified_slab", Role = "MicroScatter" }));
            Assert.AreEqual("NAT", RegionPropAssets.DomainOf("nat_stratified_slab"));
            Assert.AreEqual("", RegionPropAssets.DomainOf(""));
        }
    }
}

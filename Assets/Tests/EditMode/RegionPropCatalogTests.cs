using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Vastcore.Generation;
using Vastcore.RegionMap;
using Vastcore.Terrain.DualGrid;

namespace Vastcore.Tests.EditMode
{
    public class RegionPropCatalogTests
    {
        [Test]
        public void Catalog_LoadsDs011Json_WithAllDomainsAnd38Props()
        {
            RegionPropCatalog catalog = RegionPropCatalog.LoadDefault();
            Assert.AreEqual("DS-011", catalog.SpecId);
            Assert.AreEqual("Linear", catalog.TechnicalStandards.ColorSpace);
            Assert.AreEqual(32, catalog.TechnicalStandards.AlbedoMinimumSrgb);
            Assert.AreEqual(7, catalog.Domains.Count);
            Assert.AreEqual(7, catalog.VariationAxes.Count);
            Assert.AreEqual(38, catalog.Props.Count);

            Assert.AreEqual(17, catalog.Filter(RegionPropPriority.P0).Count);
            Assert.AreEqual(10, catalog.Filter(RegionPropPriority.P1).Count);
            Assert.AreEqual(11, catalog.Filter(RegionPropPriority.P2).Count);

            RegionPropDefinition crate = catalog.GetProp("cnt_wood_crate");
            Assert.AreEqual(RegionPropPriority.P0, crate.Priority);
            Assert.AreEqual(5, crate.VariationRules.V3_Construction.AssemblyTypes.Length);
            Assert.AreEqual(4, crate.MaterialVariants.Length);
            Assert.AreEqual(RegionPropClusterPattern.StackedGrid, crate.VariationRules.V6_ClusterSpread.Pattern);

            RegionPropDefinition rubble = catalog.GetProp("rub_stone_rubble_pile");
            Assert.AreEqual(RegionPropClusterPattern.DirectionalCone, rubble.VariationRules.V6_ClusterSpread.Pattern);
            Assert.AreEqual(3, rubble.VariationRules.V6_ClusterSpread.Layers.Count);

            RegionPropDefinition slab = catalog.GetProp("nat_stratified_slab");
            Assert.AreEqual(RegionPropPivotRule.NaturalEmbed, slab.PivotRule);
            Assert.AreEqual(14f, slab.VariationRules.V1_Transform.MaxTiltDeg, 1e-4f);
        }

        [Test]
        public void Catalog_MapsCleanly_ToSp018AndSp019Contracts()
        {
            RegionPropCatalog catalog = RegionPropCatalog.LoadDefault();
            foreach (RegionPropDefinition prop in catalog.Props)
            {
                // SP-019 StructureTagProfile 互換性
                var profile = new StructureTagProfile();
                foreach (var tag in prop.StructureTags)
                {
                    Assert.That(tag.Value, Is.InRange(0f, 1f), prop.Id + " tag " + tag.Key);
                    profile.SetWeight(tag.Key, tag.Value);
                    Assert.AreEqual(tag.Value, profile.GetWeight(tag.Key), 1e-4f);
                }

                // SP-018 RotationMode / ScaleRange 互換性
                string rotName = prop.Sp018RotationMode();
                Assert.IsTrue(Enum.TryParse<StampRotationMode>(rotName, out _), prop.Id + " invalid rotation mode " + rotName);
                Assert.LessOrEqual(prop.VariationRules.V1_Transform.ScaleMin, prop.VariationRules.V1_Transform.ScaleMax, prop.Id);
            }
        }

        [Test]
        public void Solver_SameSeed_IsDeterministic_AndRespectsV1PostureLimits()
        {
            RegionPropCatalog catalog = RegionPropCatalog.LoadDefault();
            RegionPropDefinition crate = catalog.GetProp("cnt_wood_crate");
            RegionPropDefinition slab = catalog.GetProp("nat_stratified_slab");

            // 急斜面 (30度) でも木箱は maxTiltDeg = 4度以内に制限され、パレット要求フラグが立つ
            List<RegionPropInstance> a = RegionPropPlacementSolver.PlaceCluster(
                catalog, crate, 9001u, 1, 10f, 5f, 20f, 30f, 1f, 0f, (x, z) => 5f);
            List<RegionPropInstance> b = RegionPropPlacementSolver.PlaceCluster(
                catalog, crate, 9001u, 1, 10f, 5f, 20f, 30f, 1f, 0f, (x, z) => 5f);

            Assert.AreEqual(a.Count, b.Count);
            Assert.GreaterOrEqual(a.Count, 2);
            Assert.IsTrue(a.Exists(i => i.PropId == "cnt_supply_pallet" && i.Role == "PalletBase"));

            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].PropId, b[i].PropId);
                Assert.AreEqual(a[i].X, b[i].X, 1e-5f);
                Assert.AreEqual(a[i].Y, b[i].Y, 1e-5f);
                Assert.AreEqual(a[i].Z, b[i].Z, 1e-5f);
                Assert.AreEqual(a[i].YawDeg, b[i].YawDeg, 1e-5f);
                Assert.AreEqual(a[i].ConstructionVariant, b[i].ConstructionVariant);
                Assert.AreEqual(a[i].MaterialVariant, b[i].MaterialVariant);

                if (a[i].PropId == "cnt_wood_crate")
                {
                    Assert.LessOrEqual(Mathf.Abs(a[i].PitchDeg), 4.001f);
                    Assert.LessOrEqual(Mathf.Abs(a[i].RollDeg), 4.001f);
                    float maxNu = Mathf.Max(
                        Mathf.Abs(a[i].ScaleX / a[i].ScaleY - 1f),
                        Mathf.Abs(a[i].ScaleZ / a[i].ScaleY - 1f));
                    Assert.LessOrEqual(maxNu, 0.05f);
                    Assert.LessOrEqual(a[i].StackLayer, 1);
                }
            }

            // 平地 (2度) ではパレットを敷かない
            List<RegionPropInstance> flat = RegionPropPlacementSolver.PlaceCluster(
                catalog, crate, 9001u, 1, 10f, 5f, 20f, 2f, 1f, 0f, (x, z) => 5f);
            Assert.IsFalse(flat.Exists(i => i.Role == "PalletBase"));

            // 層状岩の埋没深度が 20〜35% 以内
            RegionPropInstance slabInst = RegionPropPlacementSolver.ResolveSingle(slab, 42u, 0, 0f, 10f, 0f, 12f, 0f, 1f);
            float ratio = slabInst.EmbedDepthM / slabInst.EffectiveHeightM;
            Assert.That(ratio, Is.InRange(0.199f, 0.351f));
            Assert.LessOrEqual(Mathf.Abs(slabInst.PitchDeg), 14.001f);
        }

        [Test]
        public void Solver_WoodCrate_ExercisesAllFiveConstructionsAndMaterials()
        {
            RegionPropCatalog catalog = RegionPropCatalog.LoadDefault();
            RegionPropDefinition crate = catalog.GetProp("cnt_wood_crate");
            var constructions = new HashSet<string>();
            var materials = new HashSet<string>();

            for (int i = 0; i < 64; i++)
            {
                RegionPropInstance inst = RegionPropPlacementSolver.ResolveSingle(crate, 12345u, i, 0f, 0f, 0f, 1f);
                constructions.Add(inst.ConstructionVariant);
                materials.Add(inst.MaterialVariant);
            }

            foreach (string expected in crate.VariationRules.V3_Construction.AssemblyTypes)
                Assert.IsTrue(constructions.Contains(expected), "未到達の組み方: " + expected);
            foreach (string expected in crate.MaterialVariants)
                Assert.IsTrue(materials.Contains(expected), "未到達の材質: " + expected);
        }

        [Test]
        public void Solver_RubbleDirectionalCone_Builds3LayersAlignedToFlow()
        {
            RegionPropCatalog catalog = RegionPropCatalog.LoadDefault();
            RegionPropDefinition rubble = catalog.GetProp("rub_stone_rubble_pile");

            // 東向き (+X) の流れに沿った扇状広がり
            List<RegionPropInstance> placed = RegionPropPlacementSolver.PlaceCluster(
                catalog, rubble, 777u, 5, 0f, 0f, 0f, 18f, 1f, 0f, (x, z) => 0f, 1f, 0f, "sandstone_rubble");

            var cores = placed.FindAll(p => p.Role == "CorePile");
            var mids = placed.FindAll(p => p.Role == "MidFragments");
            var micros = placed.FindAll(p => p.Role == "MicroScatter");

            Assert.That(cores.Count, Is.InRange(1, 2));
            Assert.That(mids.Count, Is.InRange(3, 8));
            Assert.That(micros.Count, Is.InRange(10, 24));

            float avgCoreDist = AvgDist(cores);
            float avgMicroDist = AvgDist(micros);
            float avgCoreScale = AvgScale(cores);
            float avgMicroScale = AvgScale(micros);

            Assert.Less(avgCoreDist, avgMicroDist);
            Assert.Greater(avgCoreScale, avgMicroScale * 2f);

            foreach (RegionPropInstance inst in placed)
            {
                Assert.AreEqual("sandstone_rubble", inst.MaterialVariant);
                float dist = Mathf.Sqrt(inst.X * inst.X + inst.Z * inst.Z);
                if (dist > 0.05f)
                {
                    float angleDeg = Mathf.Abs(Mathf.Atan2(inst.Z, inst.X) * Mathf.Rad2Deg);
                    Assert.LessOrEqual(angleDeg, 35.5f, "コーン角度 70° (±35°) を超えています");
                }
            }

            float AvgDist(List<RegionPropInstance> items)
            {
                float sum = 0f;
                foreach (var item in items) sum += Mathf.Sqrt(item.X * item.X + item.Z * item.Z);
                return sum / items.Count;
            }

            float AvgScale(List<RegionPropInstance> items)
            {
                float sum = 0f;
                foreach (var item in items) sum += item.ScaleY;
                return sum / items.Count;
            }
        }

        [Test]
        public void Solver_RuinPillar_AttachesDebrisSkirt_AndProofSliceBuildsMeshes()
        {
            RegionPropCatalog catalog = RegionPropCatalog.LoadDefault();
            RegionPropDefinition pillar = catalog.GetProp("str_ruin_pillar");
            List<RegionPropInstance> cluster = RegionPropPlacementSolver.PlaceCluster(
                catalog, pillar, 333u, 1, 0f, 0f, 0f, 6f, 0f, 1f, (x, z) => 0f, 0f, 1f, "sandstone_carved");

            Assert.IsTrue(cluster.Exists(p => p.PropId == "str_ruin_pillar"));
            Assert.IsTrue(cluster.Exists(p => p.PropId == "rub_stone_rubble_pile" && p.Role.StartsWith("DebrisSkirt_")));

            // Unity View で実際に Mesh / GameObject が構築できることを検証
            var slice = new RegionPropProofSlice();
            string[] constructions = { "plank_closed", "slat_open", "framed_reinforced", "lid_ajar", "long_box" };
            RegionPropDefinition crate = catalog.GetProp("cnt_wood_crate");
            for (int i = 0; i < constructions.Length; i++)
            {
                slice.Instances.Add(RegionPropPlacementSolver.ResolveSingle(
                    crate, 99u, i, i * 1.5f, 0f, 0f, 0f, 0f, 1f, null, "Showcase", 1f, 0f, constructions[i]));
            }
            slice.Instances.AddRange(cluster);

            var parent = new GameObject("PropsTestHost");
            try
            {
                RegionPropView.Result view = RegionPropView.AddTo(parent.transform, slice);
                Assert.AreEqual(slice.Instances.Count, view.PlacedCount);
                MeshFilter[] filters = view.Root.GetComponentsInChildren<MeshFilter>();
                Assert.Greater(filters.Length, slice.Instances.Count);
                foreach (MeshFilter mf in filters)
                {
                    Assert.IsNotNull(mf.sharedMesh);
                    Assert.Greater(mf.sharedMesh.vertexCount, 0);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }
    }
}

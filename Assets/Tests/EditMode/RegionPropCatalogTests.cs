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

        [Test]
        public void Catalog_ValidatesAll38Props_AndP0IdsMatchSpec()
        {
            RegionPropCatalog catalog = RegionPropCatalog.LoadDefault();
            RegionPropCatalogValidationResult validation = catalog.Validate();
            Assert.IsTrue(validation.IsValid, string.Join("\n", validation.Errors));
            Assert.DoesNotThrow(() => catalog.ValidateOrThrow());

            string[] expectedP0Ids =
            {
                "nat_stratified_slab", "nat_sand_drift", "nat_pebble_cluster", "nat_dry_root_log", "nat_cliff_overhang_cap",
                "rub_stone_rubble_pile", "rub_masonry_blocks",
                "str_ruin_pillar", "str_ruin_wall_segment", "cnt_wood_crate", "cnt_wood_barrel", "cnt_fabric_sack",
                "set_expedition_tent", "set_campfire_hearth", "set_trail_signpost", "exp_monolith_fragment", "exp_survey_marker"
            };
            List<RegionPropDefinition> p0 = catalog.Filter(RegionPropPriority.P0);
            Assert.AreEqual(expectedP0Ids.Length, p0.Count);
            foreach (string id in expectedP0Ids)
                Assert.IsTrue(catalog.TryGetProp(id, out _), "P0 ID がカタログに不足: " + id);
        }

        [Test]
        public void Planner_PlacesAll17P0Props_Deterministically_WithinV1AndSpawningLimits()
        {
            RegionPropCatalog catalog = RegionPropCatalog.LoadDefault();
            RegionData region = RegionGenerator.Generate(new RegionSpec { Seed = 9, Resolution = 256 });
            var lookSpec = new RegionLookSpec { Resolution = 256, Props = true, PropsShowAllP0 = true };

            RegionLookScene sceneA = RegionLookScene.Build(region, lookSpec);
            RegionLookScene sceneB = RegionLookScene.Build(region, lookSpec);
            RegionPropProofSlice sliceA = sceneA.PropsSlice;
            RegionPropProofSlice sliceB = sceneB.PropsSlice;

            Assert.IsNotNull(sliceA);
            Assert.IsNotNull(sliceB);
            Assert.AreEqual(sliceA.Instances.Count, sliceB.Instances.Count);
            Assert.Greater(sliceA.Instances.Count, 17);

            var coveredP0 = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < sliceA.Instances.Count; i++)
            {
                RegionPropInstance a = sliceA.Instances[i];
                RegionPropInstance b = sliceB.Instances[i];
                Assert.AreEqual(a.PropId, b.PropId);
                Assert.AreEqual(a.X, b.X, 1e-4f);
                Assert.AreEqual(a.Y, b.Y, 1e-4f);
                Assert.AreEqual(a.Z, b.Z, 1e-4f);
                Assert.AreEqual(a.YawDeg, b.YawDeg, 1e-4f);
                Assert.AreEqual(a.PitchDeg, b.PitchDeg, 1e-4f);
                Assert.AreEqual(a.RollDeg, b.RollDeg, 1e-4f);
                Assert.AreEqual(a.ScaleX, b.ScaleX, 1e-4f);
                Assert.AreEqual(a.ScaleY, b.ScaleY, 1e-4f);
                Assert.AreEqual(a.ScaleZ, b.ScaleZ, 1e-4f);

                RegionPropDefinition def = catalog.GetProp(a.PropId);
                if (def.Priority == RegionPropPriority.P0)
                    coveredP0.Add(def.Id);

                float tiltLimit = def.VariationRules.V1_Transform.MaxTiltDeg;
                if ((a.DamageState == "leaning_weathered" || a.DamageState == "leaning_faded") &&
                    def.VariationRules.V1_Transform.DamagedTiltDeg > tiltLimit)
                    tiltLimit = def.VariationRules.V1_Transform.DamagedTiltDeg;
                if (a.DamageState == "fallen_drum" && def.VariationRules.V1_Transform.FallenMaxTiltDeg > tiltLimit)
                    tiltLimit = def.VariationRules.V1_Transform.FallenMaxTiltDeg;

                float totalTilt = Mathf.Sqrt(a.PitchDeg * a.PitchDeg + a.RollDeg * a.RollDeg);
                Assert.LessOrEqual(totalTilt, tiltLimit + 0.01f, def.Id + " 合成傾斜が上限超過");

                if (def.VariationRules.V1_Transform.NonUniformMax <= 1e-5f && a.DamageState != "collapsed_wreck")
                {
                    Assert.AreEqual(a.ScaleX, a.ScaleY, 1e-4f, def.Id + " 非均一禁止違反");
                    Assert.AreEqual(a.ScaleZ, a.ScaleY, 1e-4f, def.Id + " 非均一禁止違反");
                }

                if (def.SpawningRules.EmbedDepthMaxRatio > 0f && a.StackLayer == 0)
                {
                    float ratio = a.EmbedDepthM / a.EffectiveHeightM;
                    Assert.That(ratio, Is.InRange(def.SpawningRules.EmbedDepthMinRatio - 0.002f, def.SpawningRules.EmbedDepthMaxRatio + 0.002f),
                        def.Id + " 埋没率範囲外");
                }
            }

            List<RegionPropDefinition> allP0 = catalog.Filter(RegionPropPriority.P0);
            foreach (RegionPropDefinition p0 in allP0)
                Assert.IsTrue(coveredP0.Contains(p0.Id), "P0 計画配置に未含: " + p0.Id);
        }

        [Test]
        public void Planner_ExercisesAll6ClusterPatterns_AndEnforcesSlopeAndWaterAndRockBounds()
        {
            RegionPropCatalog catalog = RegionPropCatalog.LoadDefault();
            RegionData steepData = CreateSyntheticData(32, 2f, (x, z) => x * 1.4f + 10f); // 約 54.5 度斜面
            var steepField = RegionPropFieldContext.FromData(steepData);

            // 急斜面で maxSlopeDeg 上限が機能すること
            Assert.IsFalse(RegionPropPlacementPlanner.TryPlanCluster(
                catalog, catalog.GetProp("cnt_wood_crate"), steepField, 100u, 1, 32f, 32f, out RegionPropClusterPlan crateReject));
            Assert.AreEqual("SlopeExceedsMax", crateReject.SkipReason);
            Assert.IsFalse(RegionPropPlacementPlanner.TryPlanCluster(
                catalog, catalog.GetProp("set_expedition_tent"), steepField, 101u, 2, 32f, 32f, out RegionPropClusterPlan tentReject));
            Assert.AreEqual("SlopeExceedsMax", tentReject.SkipReason);
            Assert.IsTrue(RegionPropPlacementPlanner.TryPlanCluster(
                catalog, catalog.GetProp("nat_cliff_overhang_cap"), steepField, 102u, 3, 32f, 32f, out RegionPropClusterPlan capAccept));
            Assert.IsTrue(capAccept.Accepted);

            // 水面セルでは WaterBlocked で拒否されること
            RegionData waterData = CreateSyntheticData(32, 2f, (x, z) => 10f);
            for (int i = 0; i < waterData.LakeDepth.Length; i++) waterData.LakeDepth[i] = 2f;
            var waterField = RegionPropFieldContext.FromData(waterData);
            Assert.IsFalse(RegionPropPlacementPlanner.TryPlanCluster(
                catalog, catalog.GetProp("nat_pebble_cluster"), waterField, 103u, 4, 32f, 32f, out RegionPropClusterPlan waterReject));
            Assert.AreEqual("WaterBlocked", waterReject.SkipReason);

            // 岩重複の検出と探索退避
            RegionData flatData = CreateSyntheticData(32, 2f, (x, z) => 12f);
            RegionRockField rocks = CreateSingleRockField(32f, 32f, 4f);
            var rockFieldCtx = RegionPropFieldContext.FromData(flatData, rocks);
            Assert.IsFalse(RegionPropPlacementPlanner.TryPlanCluster(
                catalog, catalog.GetProp("cnt_wood_crate"), rockFieldCtx, 104u, 5, 32f, 32f, out RegionPropClusterPlan rockReject, searchRadiusM: 0f));
            Assert.AreEqual("RockOverlap", rockReject.SkipReason);
            Assert.IsTrue(RegionPropPlacementPlanner.TryPlanCluster(
                catalog, catalog.GetProp("cnt_wood_crate"), rockFieldCtx, 104u, 5, 32f, 32f, out RegionPropClusterPlan rockEvaded, searchRadiusM: 8f));
            Assert.Greater(Vector2.Distance(new Vector2(rockEvaded.AnchorX, rockEvaded.AnchorZ), new Vector2(32f, 32f)), 1.5f);

            // V6 全 6 パターンの展開確認
            string[] patternProps =
            {
                "set_trail_signpost",      // Single
                "cnt_wood_crate",          // StackedGrid
                "cnt_fabric_sack",         // Huddled
                "nat_pebble_cluster",      // RadialScatter
                "rub_stone_rubble_pile",   // DirectionalCone
                "str_ruin_wall_segment"    // WallSkirt
            };
            var seenPatterns = new HashSet<RegionPropClusterPattern>();
            for (int i = 0; i < patternProps.Length; i++)
            {
                RegionPropDefinition p = catalog.GetProp(patternProps[i]);
                Assert.IsTrue(RegionPropPlacementPlanner.TryPlanCluster(
                    catalog, p, RegionPropFieldContext.FromData(flatData), 200u, i, 32f, 32f, out RegionPropClusterPlan plan));
                Assert.GreaterOrEqual(plan.Instances.Count, 1, p.Id);
                seenPatterns.Add(plan.Pattern);
            }
            Assert.AreEqual(6, seenPatterns.Count);
        }

        [Test]
        public void Planner_ReadsRegionSedimentAndRegionData_WithoutModifyingThem()
        {
            RegionPropCatalog catalog = RegionPropCatalog.LoadDefault();
            RegionData region = RegionGenerator.Generate(new RegionSpec { Seed = 9 });
            RegionDetail detail = RegionDetailBuilder.Build(region, new RegionDetailSpec
            {
                CenterMapX = 43.61f,
                CenterMapY = 76.92f,
                SizeKm = 0.6f,
                Resolution = 128,
                Mesa = 1f,
                Sediment = 1f
            });

            Assert.IsNotNull(detail.Data.Deposit);
            Assert.IsNotNull(detail.Data.Scree);
            Assert.IsNotNull(detail.Data.Wear);
            Assert.IsNotNull(detail.Data.SedimentFlow);

            float[] heightCopy = (float[])detail.Data.Height.Clone();
            float[] screeCopy = (float[])detail.Data.Scree.Clone();
            float[] depositCopy = (float[])detail.Data.Deposit.Clone();

            // Scree または Deposit が存在するセルを探す
            int n = detail.Data.Resolution;
            float cell = detail.Data.CellMeters;
            float targetX = n * cell * 0.5f;
            float targetZ = n * cell * 0.5f;
            for (int z = 8; z < n - 8; z++)
            {
                bool found = false;
                for (int x = 8; x < n - 8; x++)
                {
                    if (detail.Data.Scree[x + z * n] > 0.1f)
                    {
                        targetX = x * cell;
                        targetZ = z * cell;
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }

            var field = RegionPropFieldContext.FromDetail(detail);
            bool hasSedimentFlow = field.TrySampleSedimentFlowDir(targetX, targetZ, out float fx, out float fz);
            Assert.IsTrue(hasSedimentFlow);
            Assert.AreEqual(1f, Mathf.Sqrt(fx * fx + fz * fz), 1e-3f);

            RegionPropDefinition rubble = catalog.GetProp("rub_stone_rubble_pile");
            Assert.IsTrue(RegionPropPlacementPlanner.TryPlanCluster(
                catalog, rubble, field, 505u, 1, targetX, targetZ, out RegionPropClusterPlan plan, enforceMaxSlope: false));
            Assert.GreaterOrEqual(plan.Instances.Count, 14);

            // 入力の地形・土砂配列が書き換わっていないことを検証
            Assert.AreEqual(heightCopy, detail.Data.Height);
            Assert.AreEqual(screeCopy, detail.Data.Scree);
            Assert.AreEqual(depositCopy, detail.Data.Deposit);
        }

        [Test]
        public void View_BuildsMeshesAndGizmoBounds_ForAll17P0Props()
        {
            RegionPropCatalog catalog = RegionPropCatalog.LoadDefault();
            RegionData region = RegionGenerator.Generate(new RegionSpec { Seed = 9, Resolution = 256 });
            RegionLookScene scene = RegionLookScene.Build(region, new RegionLookSpec { Resolution = 256, Props = true, PropsShowAllP0 = true });
            RegionPropProofSlice slice = scene.PropsSlice;

            var host = new GameObject("P0AllHost");
            try
            {
                RegionPropView.Result view = RegionPropView.AddTo(host.transform, slice, attachGizmos: true);
                Assert.AreEqual(slice.Instances.Count, view.PlacedCount);

                RegionPropGizmo[] gizmos = view.Root.GetComponentsInChildren<RegionPropGizmo>();
                Assert.AreEqual(slice.Instances.Count, gizmos.Length);

                var viewPropIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (RegionPropGizmo g in gizmos)
                {
                    viewPropIds.Add(g.PropId);
                    Assert.Greater(g.ScaledDimensionsM.x, 0f);
                    Assert.Greater(g.ScaledDimensionsM.y, 0f);
                    Assert.Greater(g.ScaledDimensionsM.z, 0f);
                }
                foreach (RegionPropDefinition p0 in catalog.Filter(RegionPropPriority.P0))
                    Assert.IsTrue(viewPropIds.Contains(p0.Id), "View Gizmo に P0 Prop が不足: " + p0.Id);

                foreach (MeshFilter mf in view.Root.GetComponentsInChildren<MeshFilter>())
                {
                    Assert.IsNotNull(mf.sharedMesh);
                    Assert.Greater(mf.sharedMesh.vertexCount, 0);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static RegionData CreateSyntheticData(int res, float cellMeters, Func<float, float, float> heightFn)
        {
            float sizeKm = res * cellMeters / 1000f;
            var spec = new RegionSpec { Seed = 1, Resolution = res, SizeKm = sizeKm };
            var data = new RegionData(spec);
            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                    data.Height[x + z * res] = heightFn(x * cellMeters, z * cellMeters);
            }
            return data;
        }

        private static RegionRockField CreateSingleRockField(float rx, float rz, float size)
        {
            return new RegionRockField
            {
                Positions = Array.Empty<float>(),
                Normals = Array.Empty<float>(),
                Indices = Array.Empty<int>(),
                Tint = Array.Empty<float>(),
                Lift = Array.Empty<float>(),
                Rocks = new[]
                {
                    new RegionRock { X = rx, Y = 12f, Z = rz, Size = size, Top = size * 0.5f, FirstVertex = 0, VertexCount = 0 }
                }
            };
        }
    }
}

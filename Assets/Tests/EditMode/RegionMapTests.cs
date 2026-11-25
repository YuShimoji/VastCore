using NUnit.Framework;
using UnityEngine;
using Vastcore.RegionMap;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// 地域生成と探索の、後から依存される約束だけを確かめる
    /// （同じ seed は同じ地域、外周は海、地点は離して置く、サーチの判定）。見た目は対象外。
    /// </summary>
    public class RegionMapTests
    {
        private static RegionSpec SmallSpec(int seed)
        {
            return new RegionSpec { Seed = seed, Resolution = 128, ErosionIterations = 8, SiteCount = 10 };
        }

        [Test]
        public void Generate_SameSeed_ProducesSameRegion()
        {
            RegionData a = RegionGenerator.Generate(SmallSpec(11));
            RegionData b = RegionGenerator.Generate(SmallSpec(11));
            Assert.IsNull(a.Development);
            CollectionAssert.AreEqual(a.Height, b.Height);
            Assert.AreEqual(a.Sites.Count, b.Sites.Count);
            for (int s = 0; s < a.Sites.Count; s++)
            {
                Assert.AreEqual(a.Sites[s].MapX, b.Sites[s].MapX);
                Assert.AreEqual(a.Sites[s].MapY, b.Sites[s].MapY);
                Assert.AreEqual(a.Sites[s].Name, b.Sites[s].Name);
            }
        }

        [Test]
        public void TerrainMesh_FollowsHeightField_AndFlattensSea()
        {
            RegionData data = RegionGenerator.Generate(SmallSpec(11));
            RegionTerrainMesh mesh = RegionTerrainMesh.Build(data, 2, 3f);
            int n = data.Resolution;
            Assert.AreEqual((n - 1) / 2 + 1, mesh.Columns);
            Assert.AreEqual(mesh.VertexCount * 3, mesh.Positions.Length);
            Assert.AreEqual((mesh.Columns - 1) * (mesh.Rows - 1) * 2, mesh.TriangleCount);
            for (int j = 0; j < mesh.Rows; j++)
            {
                for (int i = 0; i < mesh.Columns; i++)
                {
                    float height = data.Height[i * 2 + j * 2 * n];
                    float y = mesh.Positions[(i + j * mesh.Columns) * 3 + 1];
                    Assert.AreEqual(height > 0f ? height * 3f : 0f, y, 1e-3f);
                }
            }
            foreach (int index in mesh.Indices) Assert.That(index, Is.InRange(0, mesh.VertexCount - 1));
        }

        [Test]
        public void Detail_Strata_IsOffByDefault_AndAddsCliffFacesWithoutMovingWater()
        {
            RegionData region = RegionGenerator.Generate(new RegionSpec { Seed = 9 });
            var spec = new RegionDetailSpec { CenterMapX = 38.8f, CenterMapY = 60.9f, SizeKm = 2.5f, Resolution = 256 };
            Assert.AreEqual(0f, spec.Strata);
            RegionData plain = RegionDetailBuilder.Build(region, spec).Data;
            Assert.IsNull(plain.Cliff);

            spec.Strata = 1f;
            RegionData layered = RegionDetailBuilder.Build(region, spec).Data;
            Assert.IsNotNull(layered.Cliff);
            CollectionAssert.AreEqual(plain.WaterCoverage, layered.WaterCoverage);

            int faces = 0;
            for (int i = 0; i < layered.Cliff.Length; i++)
            {
                Assert.AreEqual(plain.Height[i] > 0f, layered.Height[i] > 0f);
                if (layered.Cliff[i] > 0.5f) faces++;
            }
            Assert.Greater(faces, 0);
            Assert.Greater(SteepCells(layered), SteepCells(plain) * 2);

            // 60 度より急なセルの数
            int SteepCells(RegionData data)
            {
                int n = data.Resolution;
                int steep = 0;
                float span = 2f * data.CellMeters;
                for (int y = 1; y < n - 1; y++)
                {
                    for (int x = 1; x < n - 1; x++)
                    {
                        int i = x + y * n;
                        if (data.Height[i] <= 0f) continue;
                        float gx = (data.Height[i + 1] - data.Height[i - 1]) / span;
                        float gy = (data.Height[i + n] - data.Height[i - n]) / span;
                        if (gx * gx + gy * gy > 3f) steep++;
                    }
                }
                return steep;
            }
        }

        [Test]
        public void Traversal_RoutesGoThroughTheGapInACliff_AndStopWhenItIsClosed()
        {
            // 西が 10 m、東が 40 m の台地。境は崖で、行 14〜17 にだけ、ゆるい坂（150 m で 30 m 登る）がある
            RegionData data = Plateau(true);
            RegionTraversal traversal = RegionTraversal.Build(data, null);
            int n = data.Resolution;
            Assert.AreEqual(RegionGround.Cliff, traversal.Ground[15 + 5 * n]);
            Assert.AreEqual(RegionGround.Walk, traversal.Ground[4 + 4 * n]);

            RegionTravelField west = traversal.Travel(2 + 16 * n);
            RegionRoute up = west.RouteTo(29 + 16 * n);
            Assert.IsNotNull(up);
            Assert.GreaterOrEqual(up.AscentMeters, 29.9f);
            foreach (int cell in up.Cells)
            {
                int x = cell % n;
                if (x == 15 || x == 16) Assert.That(cell / n, Is.InRange(14, 17));
            }

            // 坂から離れた所どうしは、坂まで回り道をする
            RegionRoute around = traversal.Travel(2 + 2 * n).RouteTo(29 + 2 * n);
            Assert.IsNotNull(around);
            Assert.Greater(around.Detour, 1.3f);

            // 下りは上りより早い
            RegionRoute down = traversal.Travel(29 + 16 * n).RouteTo(2 + 16 * n);
            Assert.Less(down.Hours, up.Hours);

            // 坂を崖でふさぐと、東へは着けない
            RegionTravelField closed = RegionTraversal.Build(Plateau(false), null).Travel(2 + 16 * n);
            Assert.IsFalse(closed.CanReach(29 + 16 * n));
            Assert.IsNull(closed.RouteTo(29 + 16 * n));
            Assert.Less(closed.Reached, west.Reached);

            RegionData Plateau(bool withRamp)
            {
                var plateau = new RegionData(new RegionSpec { Resolution = 32, SizeKm = 0.32f });
                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        float height = x < 16 ? 10f : 40f;
                        if (withRamp && y >= 14 && y <= 17 && x > 8 && x < 23) height = 10f + 30f * (x - 8) / 15f;
                        plateau.Height[x + y * 32] = height;
                    }
                }
                return plateau;
            }
        }

        [Test]
        public void VolumePatch_FollowsTheHeightField_AndACavityAddsRockOverhead()
        {
            // 東へ 45 度で登る斜面（1 セル 1 m、64 m 四方）
            var data = new RegionData(new RegionSpec { Resolution = 64, SizeKm = 0.064f });
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                    data.Height[x + y * 64] = 20f + x;
            var spec = new RegionVolumeSpec { CenterX = 32f, CenterZ = 32f, SizeMeters = 32f, VoxelMeters = 1f };

            // 空洞が無ければ、面は高さ場と同じ位置に出て、下を向いた面は無い
            RegionVolumePatch plain = RegionVolumePatch.Build(data, spec);
            Assert.Greater(plain.VertexCount, 500);
            Assert.AreEqual(0f, plain.DownFacingShare);
            for (int v = 0; v < plain.VertexCount; v++)
                Assert.AreEqual(plain.HeightAt(plain.Positions[v * 3], plain.Positions[v * 3 + 2]), plain.Positions[v * 3 + 1], 0.6f);
            foreach (int index in plain.Indices) Assert.That(index, Is.InRange(0, plain.VertexCount - 1));

            // 斜面へ水平に入る横穴を足すと、天井（下を向いた面）と、岩の下の頂点ができる
            spec.Cavities.Add(new RegionVolumeCavity { Ax = 30f, Ay = 52f, Az = 32f, Bx = 44f, By = 52f, Bz = 32f, RadiusA = 2.5f, RadiusB = 2.5f });
            RegionVolumePatch carved = RegionVolumePatch.Build(data, spec);
            Assert.Greater(carved.VertexCount, plain.VertexCount);
            Assert.Greater(carved.DownFacingShare, 0.02f);
            Assert.Less(carved.Density(40f, 52f, 32f), 0f);
            Assert.Greater(carved.Density(40f, 56f, 32f), 0f);
            Assert.Contains(0f, carved.Sky);
            foreach (int index in carved.Indices) Assert.That(index, Is.InRange(0, carved.VertexCount - 1));
        }

        [Test]
        public void TerrainView_MeshesFaceOutward_AndTheTerrainIsCutWhereTheVolumeIs()
        {
            var data = new RegionData(new RegionSpec { Resolution = 64, SizeKm = 0.064f });
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                    data.Height[x + y * 64] = 20f + x;
            var spec = new RegionVolumeSpec { CenterX = 32f, CenterZ = 32f, SizeMeters = 32f, VoxelMeters = 1f };
            spec.Cavities.Add(new RegionVolumeCavity { Ax = 30f, Ay = 52f, Az = 32f, Bx = 44f, By = 52f, Bz = 32f, RadiusA = 2.5f, RadiusB = 2.5f });
            RegionVolumePatch patch = RegionVolumePatch.Build(data, spec);
            RegionTerrainMesh grid = RegionTerrainMesh.Build(data, 1, 1f);

            UnityEngine.Mesh whole = RegionTerrainView.TerrainMesh(grid, null);
            UnityEngine.Mesh cut = RegionTerrainView.TerrainMesh(grid, patch);
            UnityEngine.Mesh volume = RegionTerrainView.PatchMesh(patch, 63f);
            try
            {
                // 体積の箱の範囲は、地形から抜かれる
                Assert.AreEqual(grid.TriangleCount * 3, whole.triangles.Length);
                Assert.Less(cut.triangles.Length, whole.triangles.Length);
                // 地形の面は上を向き、北（Core の Z が小さい側）が Unity の +Z になる
                foreach (UnityEngine.Vector3 normal in whole.normals) Assert.Greater(normal.y, 0f);
                Assert.AreEqual(0f, whole.vertices[0].z, 1e-4f);
                Assert.Less(whole.vertices[whole.vertexCount - 1].z, 0f);

                // 体積の面は、頂点の外向きと同じ側が表になる
                int[] triangles = volume.triangles;
                UnityEngine.Vector3[] vertices = volume.vertices;
                UnityEngine.Vector3[] normals = volume.normals;
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];
                    UnityEngine.Vector3 face = UnityEngine.Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                    Assert.GreaterOrEqual(UnityEngine.Vector3.Dot(face, normals[a] + normals[b] + normals[c]), 0f);
                }
                Assert.AreEqual(patch.VertexCount, volume.colors32.Length);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(whole);
                UnityEngine.Object.DestroyImmediate(cut);
                UnityEngine.Object.DestroyImmediate(volume);
            }
        }

        [Test]
        public void VolumeFeatures_PlaceAShelterAndACaveAtACliff()
        {
            RegionData region = RegionGenerator.Generate(new RegionSpec { Seed = 9 });
            var detailSpec = new RegionDetailSpec { CenterMapX = 38.8f, CenterMapY = 60.9f, SizeKm = 0.5f, Resolution = 512 };
            Assert.IsNull(RegionVolumeFeatures.CliffShelterAndCave(RegionDetailBuilder.Build(region, detailSpec).Data, 2f));

            detailSpec.Strata = 1f;
            RegionData terrain = RegionDetailBuilder.Build(region, detailSpec).Data;
            RegionVolumeSpec spec = RegionVolumeFeatures.CliffShelterAndCave(terrain, 2f);
            Assert.IsNotNull(spec);
            Assert.AreEqual(3, spec.Cavities.Count);
            RegionVolumePatch patch = RegionVolumePatch.Build(terrain, spec);
            Assert.Greater(patch.DownFacingShare, 0.01f);
            // 広間の中は空気で、その上には岩がある
            RegionVolumeCavity chamber = spec.Cavities[2];
            Assert.Less(patch.Density(chamber.Bx, chamber.By, chamber.Bz), 0f);
            Assert.Greater(patch.HeightAt(chamber.Bx, chamber.Bz), chamber.By + 8f);
        }

        [Test]
        public void MesaAlcove_CutsOneContact_AndSharesEveryRimVertexWithTheGrid()
        {
            const int n = 192;
            var data = new RegionData(new RegionSpec { Resolution = n, SizeKm = n / 1000f });
            data.MesaContactHeight = new float[n * n];
            data.Cliff = new float[n * n];
            data.Caprock = new byte[n * n];
            Assert.IsNull(RegionMesaAlcove.Find(data));
            for (int z = 0; z < n; z++)
            for (int x = 0; x < n; x++)
            {
                int i = x + z * n;
                data.Height[i] = x < 96 ? 50f : 99f;
                data.MesaContactHeight[i] = 50f;
                data.Caprock[i] = (byte)(x >= 96 ? 1 : 0);
                data.Cliff[i] = x == 96 ? 1f : 0f;
            }
            var site = RegionMesaAlcove.Find(data);
            Assert.IsNotNull(site);
            Assert.AreEqual(1, site.Volume.Cavities.Count);
            var patch = RegionVolumePatch.Build(data, site.Volume);
            site.Point(0f, 5f, out float sx, out float sz);
            Assert.Less(patch.Density(sx, site.Floor + 2f, sz), 0f);
            Assert.Greater(patch.Density(sx, site.Floor + 12f, sz), 0f);
            Assert.Greater(patch.DownFacingShare, 0f);
            int rim = 0;
            for (int v = 0; v < patch.VertexCount; v++)
            {
                float x = patch.Positions[v * 3], z = patch.Positions[v * 3 + 2];
                if (x - patch.MinX > 0.75f && patch.MaxX - x > 0.75f &&
                    z - patch.MinZ > 0.75f && patch.MaxZ - z > 0.75f) continue;
                int ix = Mathf.RoundToInt(x), iz = Mathf.RoundToInt(z);
                Assert.AreEqual(ix, x, 0.001f);
                Assert.AreEqual(iz, z, 0.001f);
                Assert.AreEqual(data.Height[ix + iz * n], patch.Positions[v * 3 + 1], 0.001f);
                rim++;
            }
            Assert.AreEqual(4 * (patch.SamplesX - 2), rim);
            // The same lip across a 48 m wide pinnacle would remove its entire footing.
            for (int z = 0; z < n; z++)
            {
                if (Mathf.Abs(z - 96) <= 24) continue;
                for (int x = 96; x < n; x++)
                {
                    data.Height[x + z * n] = 50f;
                    data.Caprock[x + z * n] = 0;
                }
            }
            Assert.IsNull(RegionMesaAlcove.Find(data));
        }

        private static RegionData _dryRegion;

        /// <summary>seed 9 の地域（南に、台地が立つ乾いた平原がある）。台地の試験で使い回す。</summary>
        private static RegionData DryRegion()
        {
            if (_dryRegion == null) _dryRegion = RegionGenerator.Generate(new RegionSpec { Seed = 9 });
            return _dryRegion;
        }

        [Test]
        public void Detail_Mesa_IsOffByDefault_AndRaisesFlatToppedCliffsWithoutMovingWater()
        {
            RegionData region = DryRegion();
            var spec = new RegionDetailSpec { CenterMapX = 43.61f, CenterMapY = 76.92f, SizeKm = 1.5f, Resolution = 256 };
            Assert.AreEqual(0f, spec.Mesa);
            RegionDetail plainDetail = RegionDetailBuilder.Build(region, spec);
            RegionData plain = plainDetail.Data;
            Assert.IsNull(plain.Caprock);
            Assert.IsNull(plain.Talus);
            Assert.IsNull(plain.MesaContactHeight);

            spec.Mesa = 1f;
            RegionDetail mesaDetail = RegionDetailBuilder.Build(region, spec);
            RegionData mesa = mesaDetail.Data;
            Assert.IsNotNull(mesa.Caprock);
            Assert.IsNotNull(mesa.Talus);
            Assert.AreEqual(mesa.Height.Length, mesa.MesaContactHeight.Length);
            // 海岸と川は動かず、広域の地図より高くなった分だけ、標高の差が増える
            CollectionAssert.AreEqual(plain.WaterCoverage, mesa.WaterCoverage);
            Assert.AreEqual(plainDetail.CoastAgreement, mesaDetail.CoastAgreement);
            Assert.AreEqual(plainDetail.RiverAgreement, mesaDetail.RiverAgreement);
            Assert.Greater(mesaDetail.MeanHeightDifference, plainDetail.MeanHeightDifference + 5f);

            var tops = new System.Collections.Generic.List<float>();
            int rubble = 0;
            for (int i = 0; i < mesa.Height.Length; i++)
            {
                Assert.AreEqual(plain.Height[i] > 0f, mesa.Height[i] > 0f);
                // 床より下げない。水面のセルは変えない
                Assert.GreaterOrEqual(mesa.Height[i], plain.Height[i]);
                if (mesa.WaterCoverage[i] > 0f) Assert.AreEqual(plain.Height[i], mesa.Height[i]);
                if (mesa.Caprock[i] != 0) tops.Add(mesa.Height[i]);
                if (mesa.Talus[i] > 0.4f) rubble++;
            }
            Assert.Greater(tops.Count, mesa.Height.Length / 50);
            Assert.Greater(rubble, mesa.Height.Length / 50);
            // 上面は平ら：帽子の岩のセルの 8 割が、高さ 40 m の幅に収まる（崖は 86 m）
            tops.Sort();
            Assert.Less(tops[tops.Count * 9 / 10] - tops[tops.Count / 10], 40f);
            // 崖ができる
            Assert.Greater(SteepLandCells(mesa), SteepLandCells(plain) + 100);
        }

        [Test]
        public void RockField_PutsAngularRocksOnTheTalus_AndIsRepeatable()
        {
            RegionData region = DryRegion();
            var spec = new RegionDetailSpec { CenterMapX = 43.61f, CenterMapY = 76.92f, SizeKm = 0.6f, Resolution = 256, Mesa = 1f };
            RegionData terrain = RegionDetailBuilder.Build(region, spec).Data;
            int n = terrain.Resolution;
            float cell = terrain.CellMeters;
            RegionRockField rocks = RegionRockField.Build(terrain, 1000.0, 2000.0, 0f, 0f, 0.8f, null);
            Assert.Greater(rocks.Rocks.Length, 50);
            Assert.AreEqual(rocks.VertexCount * 3, rocks.Normals.Length);
            Assert.AreEqual(rocks.VertexCount, rocks.Tint.Length);
            Assert.AreEqual(rocks.VertexCount, rocks.Lift.Length);
            Assert.AreEqual(rocks.VertexCount, rocks.Indices.Length);

            float smallest = float.MaxValue, largest = 0f;
            foreach (RegionRock rock in rocks.Rocks)
            {
                Assert.GreaterOrEqual(rock.Size, 0.8f);
                if (rock.Size < smallest) smallest = rock.Size;
                if (rock.Size > largest) largest = rock.Size;
                // 岩屑の斜面（とその先の転石が届く範囲）にだけ置く
                int x = (int)(rock.X / cell), z = (int)(rock.Z / cell);
                float amount = Mathf.Max(Mathf.Max(terrain.Talus[x + z * n], terrain.Talus[x + 1 + z * n]),
                    Mathf.Max(terrain.Talus[x + (z + 1) * n], terrain.Talus[x + 1 + (z + 1) * n]));
                Assert.Greater(amount, 0f);
                Assert.Greater(rock.VertexCount, 11);
            }
            // 大きさが揃っていない
            Assert.Greater(largest, 2.5f * smallest);
            for (int v = 0; v < rocks.VertexCount; v += 97)
            {
                var normal = new Vector3(rocks.Normals[v * 3], rocks.Normals[v * 3 + 1], rocks.Normals[v * 3 + 2]);
                Assert.AreEqual(1f, normal.magnitude, 1e-3f);
            }

            RegionRockField again = RegionRockField.Build(terrain, 1000.0, 2000.0, 0f, 0f, 0.8f, null);
            CollectionAssert.AreEqual(rocks.Positions, again.Positions);

            // 台地の無い地形には置かない
            spec.Mesa = 0f;
            Assert.AreEqual(0, RegionRockField.Build(RegionDetailBuilder.Build(region, spec).Data, 1000.0, 2000.0, 0f, 0f, 0.8f, null).Rocks.Length);
        }

        [Test]
        public void LookScene_HasThreeFixedViewpoints_AndTheInnerTerrainMeetsTheOuterAtItsEdge()
        {
            var spec = new RegionLookSpec { Resolution = 128 };
            RegionLookScene scene = RegionLookScene.Build(DryRegion(), spec);
            RegionLookViewpoint[] views = spec.Viewpoints();
            Assert.AreEqual(3, views.Length);
            Assert.AreEqual("far", views[0].Name);
            Assert.AreEqual("mid", views[1].Name);
            Assert.AreEqual("near", views[2].Name);

            // 視点は、場面の中の、地面より上にある
            float extent = scene.Outer.Data.SizeKm * 1000f;
            var eye = new float[3];
            var target = new float[3];
            foreach (RegionLookViewpoint view in views)
            {
                scene.Resolve(view, eye, target);
                Assert.That(eye[0], Is.InRange(0f, extent));
                Assert.That(eye[2], Is.InRange(0f, extent));
                Assert.AreEqual(scene.HeightAt(eye[0], eye[2]) + view.EyeAbove, eye[1], 1e-3f);
                Assert.Greater(view.EyeAbove, 0f);
            }

            // 内側の地形の縁の頂点は、外側の地形の面に乗る（継ぎ目に段が無い）
            RegionLookPart inner = scene.InnerTerrain;
            int n = scene.Inner.Data.Resolution;
            int outerN = scene.Outer.Data.Resolution;
            for (int k = 0; k < n; k += 5)
            {
                foreach (int v in new[] { k, k + (n - 1) * n, k * n, n - 1 + k * n })
                {
                    float fx = inner.Positions[v * 3] / scene.OuterCell - 0.5f, fz = inner.Positions[v * 3 + 2] / scene.OuterCell - 0.5f;
                    int x0 = Mathf.Min((int)fx, outerN - 2), z0 = Mathf.Min((int)fz, outerN - 2);
                    float tx = fx - x0, tz = fz - z0;
                    float[] h = scene.Outer.Data.Height;
                    float top = Mathf.Lerp(h[x0 + z0 * outerN], h[x0 + 1 + z0 * outerN], tx);
                    float bottom = Mathf.Lerp(h[x0 + (z0 + 1) * outerN], h[x0 + 1 + (z0 + 1) * outerN], tx);
                    Assert.AreEqual(Mathf.Max(0f, Mathf.Lerp(top, bottom, tz)), inner.Positions[v * 3 + 1], 0.05f);
                }
            }
            // 外側の地形は、内側の範囲を抜いてある
            Assert.Less(scene.OuterTerrain.TriangleCount, (outerN - 1) * (outerN - 1) * 2);

            // 水を含む頂点では砂岩の材質を抑え、乾いた陸には地形と同じ崖錐と層の境界を渡す。
            inner.Data.WaterCoverage[0] = 1f;
            inner.Data.Moisture[1] = 0.24f;
            inner.Data.WaterCoverage[1] = 0f;
            Mesh terrainMesh = RegionTerrainView.LookMesh(inner);
            try
            {
                var surface = new System.Collections.Generic.List<Vector4>();
                terrainMesh.GetUVs(1, surface);
                Assert.AreEqual(inner.VertexCount, surface.Count);
                Assert.AreEqual(0f, surface[0].w);
                Assert.AreEqual(1f, surface[1].w);
                Assert.AreEqual(inner.Data.Talus[1], surface[1].x);
                Assert.AreEqual(inner.Positions[4] - inner.Data.MesaContactHeight[1], surface[1].y);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(terrainMesh);
            }

            foreach (RegionLookPart part in scene.Parts())
            {
                Assert.AreEqual(part.VertexCount, part.Sun.Length);
                Assert.AreEqual(part.VertexCount, part.Rock.Length);
                for (int v = 0; v < part.VertexCount; v++)
                {
                    Assert.That(part.Sun[v], Is.InRange(0f, 1f));
                    Assert.That(part.Sky[v], Is.InRange(0f, 1f));
                }
                foreach (int index in part.Indices) Assert.That(index, Is.InRange(0, part.VertexCount - 1));
            }

            // Unity の Mesh にしても、頂点と色の数は同じ
            Assert.IsNotNull(scene.Rocks);
            UnityEngine.Mesh mesh = RegionTerrainView.LookMesh(scene.Rocks);
            try
            {
                Assert.AreEqual(scene.Rocks.VertexCount, mesh.vertexCount);
                Assert.AreEqual(scene.Rocks.VertexCount, mesh.colors32.Length);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void MesaPlants_AreRepeatable_AndSkipWaterAndOccupiedGroundInBothWindows()
        {
            var scene = RegionLookScene.Build(DryRegion(), new RegionLookSpec
            {
                OuterKm = 1.5f, InnerKm = 0.5f, Resolution = 128, Rocks = false
            });
            float waterEnd = scene.CenterX - 75f, occupiedStart = scene.CenterX + 65f;
            foreach (RegionLookPart part in new[] { scene.InnerTerrain, scene.OuterTerrain })
            {
                part.Data.StructureCoverage = new float[part.VertexCount];
                for (int i = 0; i < part.VertexCount; i++)
                {
                    float x = part.Positions[3 * i];
                    if (x < waterEnd) part.Data.WaterCoverage[i] = 1f;
                    if (x > occupiedStart) part.Data.StructureCoverage[i] = 1f;
                }
            }
            var plants = RegionPlantField.Build(scene);
            Assert.IsNotEmpty(plants);
            CollectionAssert.AreEqual(plants, RegionPlantField.Build(scene));
            foreach (RegionPlant plant in plants)
            {
                Assert.That(plant.X, Is.InRange(waterEnd, occupiedStart + scene.OuterCell * 0.5f));
                Assert.That(plant.Variant, Is.InRange(0, 3));
                Assert.Greater(plant.Height, 0f);
            }
            scene.Spec.Vegetation = false;
            Assert.IsEmpty(RegionPlantField.Build(scene));
        }

        /// <summary>60 度より急な陸のセルの数。</summary>
        private static int SteepLandCells(RegionData data)
        {
            int n = data.Resolution;
            int steep = 0;
            float span = 2f * data.CellMeters;
            for (int y = 1; y < n - 1; y++)
            {
                for (int x = 1; x < n - 1; x++)
                {
                    int i = x + y * n;
                    if (data.Height[i] <= 0f) continue;
                    float gx = (data.Height[i + 1] - data.Height[i - 1]) / span;
                    float gy = (data.Height[i + n] - data.Height[i - n]) / span;
                    if (gx * gx + gy * gy > 3f) steep++;
                }
            }
            return steep;
        }

        [Test]
        public void Generate_DifferentSeed_ProducesDifferentRegion()
        {
            RegionData a = RegionGenerator.Generate(SmallSpec(11));
            RegionData b = RegionGenerator.Generate(SmallSpec(12));
            CollectionAssert.AreNotEqual(a.Height, b.Height);
        }

        [Test]
        public void Generate_OuterEdgeIsSea_AndLandExists()
        {
            RegionData data = RegionGenerator.Generate(SmallSpec(7));
            int n = data.Resolution;
            for (int k = 0; k < n; k++)
            {
                Assert.LessOrEqual(data.Height[data.Index(k, 0)], 0f);
                Assert.LessOrEqual(data.Height[data.Index(k, n - 1)], 0f);
                Assert.LessOrEqual(data.Height[data.Index(0, k)], 0f);
                Assert.LessOrEqual(data.Height[data.Index(n - 1, k)], 0f);
            }
            Assert.Greater(data.LandFraction, 0.05f);
            Assert.Greater(data.MaxHeight, 100f);
        }

        [Test]
        public void Lake_KeepsOneWaterLevelAcrossTheRegionAndTheDetailWindow()
        {
            RegionData region = RegionGenerator.Generate(new RegionSpec { Seed = 8 });
            int center = 0;
            for (int i = 1; i < region.LakeDepth.Length; i++)
                if (region.LakeDepth[i] > region.LakeDepth[center]) center = i;
            Assert.Greater(region.LakeDepth[center], 2f);
            float lakeLevel = region.Height[center] + region.LakeDepth[center];
            for (int i = 0; i < region.LakeDepth.Length; i++)
            {
                if (region.LakeDepth[i] <= 0f) continue;
                Assert.AreEqual(RegionBiome.Lake, region.Biome[i]);
                Assert.AreEqual(lakeLevel, region.Height[i] + region.LakeDepth[i], 0.001f);
                int receiver = region.Downstream[i];
                if (receiver >= 0 && region.LakeDepth[receiver] <= 0f)
                    Assert.LessOrEqual(region.Height[receiver], lakeLevel);
            }

            RegionDetail detail = RegionDetailBuilder.Build(region, new RegionDetailSpec
            {
                CenterMapX = region.MapXFromCell(center),
                CenterMapY = region.MapYFromCell(center),
                SizeKm = 8f,
                Resolution = 256
            });
            int detailCenter = detail.Data.Index(128, 128);
            Assert.AreEqual(RegionBiome.Lake, detail.Data.Biome[detailCenter]);
            Assert.AreEqual(lakeLevel, detail.Data.Height[detailCenter] + detail.Data.LakeDepth[detailCenter], 0.001f);
            Assert.AreEqual(RegionBiome.Ocean, region.Biome[0]);
        }

        [Test]
        public void ModernDevelopment_DrawsSiteDetailWhenACellIsFinerThanThreeMeters()
        {
            RegionData region = RegionGenerator.Generate(new RegionSpec { Seed = 9, ModernDevelopment = true });
            RegionPosition city = region.Development.SecondaryCity;
            var spec = new RegionDetailSpec
            {
                CenterMapX = (city.X + 2800f) / (region.KmPerMapUnit * 1000f),
                CenterMapY = city.Y / (region.KmPerMapUnit * 1000f),
                SizeKm = 0.6f,
                Resolution = 256,
            };
            var options = new RegionRenderOptions { Style = RegionMapStyle.Satellite, CloudCover = 0f };
            byte[] first = RegionMapRenderer.Render(RegionDetailBuilder.Build(region, spec).Data, options);
            byte[] second = RegionMapRenderer.Render(RegionDetailBuilder.Build(region, spec).Data, options);
            CollectionAssert.AreEqual(first, second);

            // 同じ場所を 1 画素 3 m より粗く描くと、街区と屋根の面だけになる。細部の描画は色の種類が大きく増える
            spec.SizeKm = 1.6f;
            byte[] coarse = RegionMapRenderer.Render(RegionDetailBuilder.Build(region, spec).Data, options);
            Assert.Greater(Colors(first), Colors(coarse) * 2);

            int Colors(byte[] pixels)
            {
                var seen = new System.Collections.Generic.HashSet<int>();
                for (int p = 0; p < pixels.Length; p += 4)
                    seen.Add((pixels[p] >> 3) | (pixels[p + 1] >> 3) << 5 | (pixels[p + 2] >> 3) << 10);
                return seen.Count;
            }
        }

        [Test]
        public void ModernDevelopment_ConnectsTwoCitiesToAnAirportAndSurvivesZoom()
        {
            RegionData region = RegionGenerator.Generate(new RegionSpec { Seed = 9, ModernDevelopment = true });
            RegionDevelopment plan = region.Development;
            Assert.IsNotNull(plan);
            Assert.AreEqual(plan.PrimaryCity, plan.Highway[0]);
            Assert.AreEqual(plan.SecondaryCity, plan.Highway[plan.Highway.Count - 1]);
            Assert.Contains(plan.AirportAccess[0], new System.Collections.Generic.List<RegionPosition>(plan.Highway));

            bool primaryStreet = false;
            bool secondaryStreet = false;
            foreach (var street in plan.Streets)
            {
                primaryStreet |= street.Start.Equals(plan.PrimaryCity);
                secondaryStreet |= street.Start.Equals(plan.SecondaryCity);
            }
            Assert.IsTrue(primaryStreet && secondaryStreet);
            AssertDryPath(plan.Highway);
            AssertDryPath(plan.AirportAccess);

            int runways = 0;
            int ruralBuildings = 0;
            foreach (RegionDevelopmentFeature feature in plan.Features)
            {
                if (feature.Kind == RegionDevelopmentFeatureKind.Runway) runways++;
                if (feature.Kind == RegionDevelopmentFeatureKind.RuralRoof) ruralBuildings++;
                AssertDry(feature.Center);
            }
            Assert.AreEqual(1, runways);
            Assert.Greater(ruralBuildings, 1);

            float xMap = plan.Airport.Center.X / (region.KmPerMapUnit * 1000f);
            float yMap = plan.Airport.Center.Y / (region.KmPerMapUnit * 1000f);
            RegionDetail detail = RegionDetailBuilder.Build(region, new RegionDetailSpec
            {
                CenterMapX = xMap, CenterMapY = yMap, Resolution = 256,
            });
            Assert.AreSame(plan, detail.Data.Development);
            Assert.IsTrue(detail.Contains(xMap, yMap));

            void AssertDryPath(System.Collections.Generic.IReadOnlyList<RegionPosition> points)
            {
                for (int i = 1; i < points.Count; i++)
                {
                    RegionPosition a = points[i - 1], b = points[i];
                    int steps = Mathf.CeilToInt(Vector2.Distance(new Vector2(a.X, a.Y), new Vector2(b.X, b.Y)) / 125f);
                    for (int step = 0; step <= steps; step++)
                    {
                        float t = step / (float)steps;
                        AssertDry(new RegionPosition(Mathf.Lerp(a.X, b.X, t), Mathf.Lerp(a.Y, b.Y, t)));
                    }
                }
            }

            void AssertDry(RegionPosition p)
            {
                int cell = region.Index((int)(p.X / region.CellMeters), (int)(p.Y / region.CellMeters));
                Assert.Greater(region.Height[cell], 0f);
                Assert.AreEqual(0f, region.LakeDepth[cell]);
            }
        }

        [Test]
        public void Sites_AreSpacedApart_AndInsideMap()
        {
            RegionData data = RegionGenerator.Generate(SmallSpec(7));
            Assert.Greater(data.Sites.Count, 0);
            for (int a = 0; a < data.Sites.Count; a++)
            {
                RegionSite site = data.Sites[a];
                Assert.That(site.MapX, Is.InRange(0f, RegionData.MapUnits));
                Assert.That(site.MapY, Is.InRange(0f, RegionData.MapUnits));
                Assert.AreEqual(a + 1, site.Id);
                for (int b = a + 1; b < data.Sites.Count; b++)
                {
                    float dx = site.MapX - data.Sites[b].MapX;
                    float dy = site.MapY - data.Sites[b].MapY;
                    Assert.Greater(dx * dx + dy * dy, 8.9f * 8.9f);
                }
            }
        }

        [Test]
        public void Search_ReportsSignalByDistance_AndRecordsDiscovery()
        {
            RegionData data = RegionGenerator.Generate(SmallSpec(7));
            var session = new ExplorationSession(data);
            RegionSite site = data.Sites[0];

            // 他の地点は 9 以上離れているので、東西どちらかへずらした位置では site がいちばん近い
            float direction = site.MapX < 50f ? 1f : -1f;
            Assert.AreEqual(SearchSignal.Strong, session.Search(site.MapX + direction * 4f, site.MapY).Signal);
            Assert.AreEqual(0, session.Discovered.Count);

            SearchResult found = session.Search(site.MapX + direction * 1f, site.MapY);
            Assert.AreEqual(SearchSignal.Found, found.Signal);
            Assert.AreSame(site, found.Site);
            Assert.AreEqual(1, session.Discovered.Count);
            Assert.IsTrue(session.IsDiscovered(site));
            Assert.AreEqual(2, session.SearchCount);

            // 発見済みの地点には二度と反応しない
            Assert.AreNotEqual(SearchSignal.Found, session.Search(site.MapX, site.MapY).Signal);
        }

        [Test]
        public void Session_RestoresDiscoveredSites()
        {
            RegionData data = RegionGenerator.Generate(SmallSpec(7));
            var first = new ExplorationSession(data);
            first.Search(data.Sites[0].MapX, data.Sites[0].MapY);
            first.Search(data.Sites[1].MapX, data.Sites[1].MapY);

            var second = new ExplorationSession(data);
            second.Restore(first.SaveDiscoveredIds());
            Assert.AreEqual(2, second.Discovered.Count);
            Assert.IsTrue(second.IsDiscovered(data.Sites[0]));
            Assert.IsTrue(second.IsDiscovered(data.Sites[1]));
        }

        [Test]
        public void Render_ReturnsOpaqueImageOfRegionSize()
        {
            RegionData data = RegionGenerator.Generate(SmallSpec(7));
            foreach (RegionMapStyle style in System.Enum.GetValues(typeof(RegionMapStyle)))
            {
                byte[] pixels = RegionMapRenderer.Render(data, new RegionRenderOptions { Style = style });
                Assert.AreEqual(data.Resolution * data.Resolution * 4, pixels.Length);
                Assert.AreEqual(255, pixels[3]);
                Assert.AreEqual(255, pixels[pixels.Length - 1]);
            }
        }

        [Test]
        public void Clouds_SameSeedSameLayer_AndMoreCoverMeansMoreCloud()
        {
            const int size = 96;
            CollectionAssert.AreEqual(RegionClouds.BuildField(size, 7, 0.58f), RegionClouds.BuildField(size, 7, 0.58f));
            Assert.AreEqual(0f, RegionClouds.Coverage(RegionClouds.BuildField(size, 7, 0f), 0f));
            float few = RegionClouds.Coverage(RegionClouds.BuildField(size, 7, 0.3f), 0.2f);
            float many = RegionClouds.Coverage(RegionClouds.BuildField(size, 7, 0.7f), 0.2f);
            Assert.Greater(many, few);
        }

        /// <summary>探索画面は雲の層をずらして繰り返すので、左右と上下の端がつながっていること。</summary>
        [Test]
        public void Clouds_LayerRepeatsWithoutSeam()
        {
            const int size = 128;
            float[] field = RegionClouds.BuildField(size, 7, 0.58f);
            double inside = 0;
            double acrossX = 0;
            double acrossY = 0;
            for (int k = 0; k < size; k++)
            {
                for (int x = 1; x < size; x++) inside += System.Math.Abs(field[x + k * size] - field[x - 1 + k * size]);
                acrossX += System.Math.Abs(field[k * size] - field[size - 1 + k * size]);
                acrossY += System.Math.Abs(field[k] - field[k + (size - 1) * size]);
            }
            inside /= size * (size - 1);
            acrossX /= size;
            acrossY /= size;
            // 端をまたぐ隣どうしの差が、中の隣どうしの差と同じくらいに収まる（継ぎ目があると何倍にもなる）
            Assert.Less(acrossX, inside * 2.5 + 0.01);
            Assert.Less(acrossY, inside * 2.5 + 0.01);
        }

        /// <summary>雲の流れは場所ごとに違い、渦のずれは小さな上限に収まる（伸び続けると雲の絵が筋になる）。</summary>
        [Test]
        public void CloudFlow_DiffersByPlace_AndSwirlStaysBounded()
        {
            const float time = 90f;
            const float cross = 420f;
            for (int layer = RegionCloudFlow.Low; layer <= RegionCloudFlow.High; layer++)
            {
                float driftU;
                float driftV;
                RegionCloudFlow.Drift(layer, time, cross, out driftU, out driftV);
                float least = float.MaxValue;
                float most = float.MinValue;
                for (int gy = 0; gy <= 16; gy++)
                {
                    for (int gx = 0; gx <= 16; gx++)
                    {
                        float du;
                        float dv;
                        float fade;
                        RegionCloudFlow.Sample(layer, gx / 16f, gy / 16f, time, cross, out du, out dv, out fade);
                        float swirlU = du - driftU;
                        float swirlV = dv - driftV;
                        Assert.Less(System.Math.Abs(swirlU), 0.05f);
                        Assert.Less(System.Math.Abs(swirlV), 0.05f);
                        Assert.That(fade, Is.InRange(0f, 1f));
                        if (swirlU < least) least = swirlU;
                        if (swirlU > most) most = swirlU;
                    }
                }
                Assert.Greater(most - least, 0.002f);
            }
        }

        /// <summary>エリア：陸のセルはどれかのエリアに入り、名前は重ならず、同じ seed なら同じ区分けになる。</summary>
        [Test]
        public void Areas_CoverLand_HaveUniqueNames_AndAreDeterministic()
        {
            RegionData a = RegionGenerator.Generate(SmallSpec(7));
            RegionData b = RegionGenerator.Generate(SmallSpec(7));
            CollectionAssert.AreEqual(a.Area, b.Area);
            Assert.Greater(a.Areas.Count, 0);
            Assert.AreEqual(a.Areas.Count, b.Areas.Count);

            var names = new System.Collections.Generic.HashSet<string>();
            for (int k = 0; k < a.Areas.Count; k++)
            {
                RegionArea area = a.Areas[k];
                Assert.AreEqual(area.Name, b.Areas[k].Name);
                Assert.IsTrue(names.Add(area.Name), "同じ名前のエリアがある: " + area.Name);
                Assert.AreSame(area, a.AreaById(area.Id));
                Assert.That(area.LabelX, Is.InRange(0f, RegionData.MapUnits));
                Assert.That(area.LabelY, Is.InRange(0f, RegionData.MapUnits));
            }

            for (int i = 0; i < a.Height.Length; i++)
            {
                if (a.Height[i] > 0f) Assert.IsNotNull(a.AreaById(a.Area[i]), "エリアに入っていない陸のセルがある");
            }

            for (int s = 0; s < a.Sites.Count; s++)
            {
                RegionSite site = a.Sites[s];
                Assert.IsFalse(string.IsNullOrEmpty(site.Reason));
                Assert.GreaterOrEqual(site.Rank, 1);
                if (site.AreaId != 0) Assert.IsNotNull(a.AreaById(site.AreaId));
            }
        }

        /// <summary>seed から作る呼び名：地域の中で重ならず、同じ seed なら同じ名前になり、機械的な呼び名へ戻せる。</summary>
        [Test]
        public void Names_GeneratedStyles_AreUniqueAndDeterministic_AndCanBeRestored()
        {
            RegionData a = RegionGenerator.Generate(SmallSpec(7));
            RegionData b = RegionGenerator.Generate(SmallSpec(7));
            string firstArea = a.Areas[0].Name;
            string firstSite = a.Sites[0].Name;

            var styles = new[] { RegionNameStyle.SoftKana, RegionNameStyle.HardKana, RegionNameStyle.Kanji };
            for (int s = 0; s < styles.Length; s++)
            {
                RegionNames.Apply(a, styles[s]);
                RegionNames.Apply(b, styles[s]);
                var names = new System.Collections.Generic.HashSet<string>();
                for (int k = 0; k < a.Areas.Count; k++)
                {
                    Assert.AreEqual(a.Areas[k].Name, b.Areas[k].Name);
                    Assert.IsTrue(names.Add(a.Areas[k].Name), styles[s] + " で同じ名前のエリアがある: " + a.Areas[k].Name);
                }
                for (int k = 0; k < a.Sites.Count; k++)
                {
                    Assert.AreEqual(a.Sites[k].Name, b.Sites[k].Name);
                    Assert.IsTrue(names.Add(a.Sites[k].Name), styles[s] + " で同じ名前の地点がある: " + a.Sites[k].Name);
                }
                Assert.AreNotEqual(firstArea, a.Areas[0].Name);
            }

            RegionNames.Apply(a, RegionNameStyle.Descriptive);
            Assert.AreEqual(firstArea, a.Areas[0].Name);
            Assert.AreEqual(firstSite, a.Sites[0].Name);
        }

        /// <summary>
        /// 拡大した地形：同じ入力からは同じ地形になり、海か陸かは広域の同じ場所とほぼ一致し、広域の川は拡大しても水面として通る。
        /// 構造物を置くと、その場所の地面が高くなる。
        /// </summary>
        [Test]
        public void Detail_IsDeterministic_KeepsCoastAndRivers_AndTakesStructure()
        {
            RegionData region = RegionGenerator.Generate(SmallSpec(7));
            Assert.IsNotNull(region.Downstream);
            RegionSite onLand = null;
            for (int s = 0; s < region.Sites.Count && onLand == null; s++)
            {
                if (region.Sites[s].HeightMeters > 20f) onLand = region.Sites[s];
            }
            Assert.IsNotNull(onLand, "陸の上の地点が無い");

            var spec = new RegionDetailSpec { CenterMapX = onLand.MapX, CenterMapY = onLand.MapY, SizeKm = 24f, Resolution = 192 };
            RegionDetail a = RegionDetailBuilder.Build(region, spec);
            RegionDetail b = RegionDetailBuilder.Build(region, spec);
            Assert.AreEqual(192 * 192, a.Data.Height.Length);
            CollectionAssert.AreEqual(a.Data.Height, b.Data.Height);
            CollectionAssert.AreEqual(a.Data.WaterCoverage, b.Data.WaterCoverage);
            Assert.Greater(a.CoastAgreement, 0.9f, "海陸が広域と合っていない");
            Assert.Greater(a.RiverAgreement, 0.9f, "広域の川が拡大した地形に通っていない");
            Assert.IsTrue(a.Contains(onLand.MapX, onLand.MapY));

            int n = a.Data.Resolution;
            int center = (int)a.LocalX(onLand.MapX) + (int)a.LocalY(onLand.MapY) * n;
            float before = a.Data.Height[center];
            Assert.Greater(before, 0f);
            RegionStructure placed = RegionStructureStamp.Place(a, onLand.MapX, onLand.MapY, onLand.Kind, RegionStructureForm.Stepped);
            Assert.IsNotNull(placed);
            Assert.Greater(a.Data.Height[center], before + 100f);
            Assert.Greater(a.Data.StructureCoverage[center], 0f);
            Assert.AreEqual(a.Data.Height.Length * 4, RegionMapRenderer.Render(a.Data, new RegionRenderOptions { CloudCover = 0f }).Length);
        }

        [Test]
        public void Skin_NothingPlaced_LoadsNull_AndTextureBecomesCenteredSprite()
        {
            // 差し替えの入口: 置いていない名前は null（探索画面は既定の絵を使う）
            Assert.IsNull(RegionMapSkin.Load("NoSuchImage"));
            Assert.IsNull(RegionMapSkin.FromTexture(null));
            CollectionAssert.AllItemsAreUnique(RegionMapSkin.Names);
            Assert.AreEqual(4, RegionMapSkin.Names.Length);

            var texture = new Texture2D(8, 4, TextureFormat.RGBA32, false);
            try
            {
                Sprite sprite = RegionMapSkin.FromTexture(texture);
                Assert.AreEqual(8f, sprite.rect.width);
                Assert.AreEqual(4f, sprite.rect.height);
                Assert.AreEqual(4f, sprite.pivot.x, 0.001f);
                Assert.AreEqual(2f, sprite.pivot.y, 0.001f);
                Object.DestroyImmediate(sprite);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}

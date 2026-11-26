using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// DS-011 P0 代表 Props のプレースホルダ／変換済み造形を Unity シーンへ構築する。
    /// Core 座標 (X 東, Y 上, Z 南) を Unity 座標 (X 東, Y 上, -Z 北) へ写して配置する。
    /// </summary>
    public static class RegionPropView
    {
        public sealed class Result
        {
            public GameObject Root;
            public int PlacedCount;
        }

        private sealed class Palette
        {
            public Shader Shader;
            public readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>(StringComparer.Ordinal);
        }

        public static Result AddTo(Transform parent, RegionPropProofSlice slice)
        {
            var root = new GameObject("DS011_Props_Proof");
            root.transform.SetParent(parent, false);
            var result = new Result { Root = root };
            if (slice == null || slice.Instances.Count == 0) return result;

            var palette = new Palette { Shader = FindPropShader() };

            for (int i = 0; i < slice.Instances.Count; i++)
            {
                RegionPropInstance inst = slice.Instances[i];
                GameObject go = BuildInstanceObject(inst, palette, i);
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3(inst.X, inst.Y, -inst.Z);
                go.transform.localRotation = Quaternion.Euler(inst.PitchDeg, inst.YawDeg, -inst.RollDeg);
                result.PlacedCount++;
            }

            return result;
        }

        private static GameObject BuildInstanceObject(RegionPropInstance inst, Palette palette, int index)
        {
            var go = new GameObject(string.Format("{0:D2}_{1}_{2}", index, inst.PropId, inst.Role));
            switch (inst.PropId)
            {
                case "cnt_wood_crate":
                    BuildWoodCrate(go.transform, inst, palette);
                    break;
                case "cnt_supply_pallet":
                    BuildSupplyPallet(go.transform, inst, palette);
                    break;
                case "rub_wood_splinters":
                    BuildWoodSplinters(go.transform, inst, palette);
                    break;
                case "rub_stone_rubble_pile":
                    BuildStoneRubble(go.transform, inst, palette, index);
                    break;
                case "nat_stratified_slab":
                    BuildStratifiedSlab(go.transform, inst, palette, index);
                    break;
                case "str_ruin_pillar":
                    BuildRuinPillar(go.transform, inst, palette, index);
                    break;
                case "cnt_wood_barrel":
                    BuildWoodBarrel(go.transform, inst, palette);
                    break;
                case "set_trail_signpost":
                    BuildTrailSignpost(go.transform, inst, palette);
                    break;
                default:
                    AddBox(go.transform, "Box", Vector3.zero,
                        new Vector3(inst.DimensionsX * inst.ScaleX, inst.DimensionsY * inst.ScaleY, inst.DimensionsZ * inst.ScaleZ),
                        Vector3.zero, GetMaterial(palette, inst.MaterialVariant, inst, 0.14f, 0f));
                    break;
            }
            return go;
        }

        private static void BuildWoodCrate(Transform parent, RegionPropInstance inst, Palette palette)
        {
            float w = inst.DimensionsX * inst.ScaleX;
            float h = inst.DimensionsY * inst.ScaleY;
            float d = inst.DimensionsZ * inst.ScaleZ;
            Material wood = GetMaterial(palette, inst.MaterialVariant, inst, 0.16f, 0f);
            Material darkWood = GetMaterial(palette, inst.MaterialVariant + "_trim", inst, 0.14f, 0f, -0.07f);
            Material iron = GetMaterial(palette, "iron_metal", inst, 0.38f, 0.72f);

            if (inst.DamageState == "collapsed_wreck")
            {
                AddBox(parent, "BasePlank", new Vector3(0f, h * 0.25f, 0f), new Vector3(w * 0.95f, 0.06f, d * 0.95f), Vector3.zero, wood);
                AddBox(parent, "BrokenSideA", new Vector3(-w * 0.22f, h * 0.55f, 0.06f), new Vector3(w * 0.7f, 0.05f, d * 0.35f), new Vector3(12f, 18f, -24f), darkWood);
                AddBox(parent, "BrokenSideB", new Vector3(w * 0.24f, h * 0.45f, -0.1f), new Vector3(w * 0.65f, 0.05f, d * 0.3f), new Vector3(-16f, -32f, 19f), wood);
                return;
            }

            bool isSlat = inst.ConstructionVariant == "slat_open";
            bool isFramed = inst.ConstructionVariant == "framed_reinforced";
            bool isLidOpen = inst.ConstructionVariant == "lid_ajar";
            bool isLong = inst.ConstructionVariant == "long_box";
            float post = Math.Min(w, d) * 0.09f;

            if (isSlat)
            {
                // 隙間のあるすのこ枠組み + 中身の荷物
                AddBox(parent, "InnerCargo", new Vector3(0f, h * 0.36f, 0f), new Vector3(w * 0.68f, h * 0.64f, d * 0.68f), Vector3.zero,
                    GetMaterial(palette, "cargo_cloth", inst, 0.10f, 0f));
                AddCrateFramePosts(parent, w, h, d, post, darkWood);
                for (int r = 0; r < 3; r++)
                {
                    if (inst.DamageState == "broken_slat_open" && r == 1) continue;
                    float ry = h * (0.18f + r * 0.31f);
                    AddBox(parent, "SlatF_" + r, new Vector3(0f, ry, d * 0.47f), new Vector3(w * 0.94f, h * 0.14f, post * 0.45f), Vector3.zero, wood);
                    AddBox(parent, "SlatB_" + r, new Vector3(0f, ry, -d * 0.47f), new Vector3(w * 0.94f, h * 0.14f, post * 0.45f), Vector3.zero, wood);
                    AddBox(parent, "SlatL_" + r, new Vector3(-w * 0.47f, ry, 0f), new Vector3(post * 0.45f, h * 0.14f, d * 0.94f), Vector3.zero, wood);
                    AddBox(parent, "SlatR_" + r, new Vector3(w * 0.47f, ry, 0f), new Vector3(post * 0.45f, h * 0.14f, d * 0.94f), Vector3.zero, wood);
                }
                AddBox(parent, "TopSlats", new Vector3(0f, h - post * 0.45f, 0f), new Vector3(w * 0.96f, post * 0.6f, d * 0.6f), Vector3.zero, wood);
            }
            else if (isLidOpen)
            {
                // 蓋開き型: 中が空洞・中身入り + 斜めに立て掛けた蓋
                float wallT = Math.Min(w, d) * 0.08f;
                AddBox(parent, "Floor", new Vector3(0f, wallT * 0.5f, 0f), new Vector3(w, wallT, d), Vector3.zero, darkWood);
                AddBox(parent, "WallF", new Vector3(0f, h * 0.5f, d * 0.5f - wallT * 0.5f), new Vector3(w, h, wallT), Vector3.zero, wood);
                AddBox(parent, "WallB", new Vector3(0f, h * 0.5f, -d * 0.5f + wallT * 0.5f), new Vector3(w, h, wallT), Vector3.zero, wood);
                AddBox(parent, "WallL", new Vector3(-w * 0.5f + wallT * 0.5f, h * 0.5f, 0f), new Vector3(wallT, h, d - wallT * 2f), Vector3.zero, wood);
                AddBox(parent, "WallR", new Vector3(w * 0.5f - wallT * 0.5f, h * 0.5f, 0f), new Vector3(wallT, h, d - wallT * 2f), Vector3.zero, wood);
                AddCrateFramePosts(parent, w, h, d, post, darkWood);
                AddBox(parent, "InnerGoods", new Vector3(-w * 0.1f, h * 0.34f, 0f), new Vector3(w * 0.55f, h * 0.5f, d * 0.55f), new Vector3(0f, 18f, 0f),
                    GetMaterial(palette, "packing_straw", inst, 0.08f, 0f));
                AddBox(parent, "LeaningLid", new Vector3(w * 0.58f, h * 0.48f, 0.05f), new Vector3(w * 0.96f, wallT * 0.8f, d * 0.96f), new Vector3(4f, 8f, -62f), darkWood);
            }
            else
            {
                // 密閉箱 / 筋交い補強箱 / 長尺箱
                AddBox(parent, "CoreBody", new Vector3(0f, h * 0.5f, 0f), new Vector3(w * 0.90f, h * 0.90f, d * 0.90f), Vector3.zero, wood);
                AddCrateFramePosts(parent, w, h, d, post, darkWood);
                // 横板の目地アクセント
                AddBox(parent, "MidSeam", new Vector3(0f, h * 0.5f, 0f), new Vector3(w * 0.94f, h * 0.04f, d * 0.94f), Vector3.zero, darkWood);

                if (isFramed || inst.MaterialVariant == "iron_banded_dark_wood")
                {
                    float diag = (float)Math.Sqrt(w * w * 0.72f + h * h * 0.72f);
                    float ang = Mathf.Atan2(h, w) * Mathf.Rad2Deg;
                    AddBox(parent, "BraceF", new Vector3(0f, h * 0.5f, d * 0.48f), new Vector3(diag, post * 0.75f, post * 0.5f), new Vector3(0f, 0f, ang), darkWood);
                    AddBox(parent, "BraceB", new Vector3(0f, h * 0.5f, -d * 0.48f), new Vector3(diag, post * 0.75f, post * 0.5f), new Vector3(0f, 0f, -ang), darkWood);
                    // 四隅補強金具
                    AddBox(parent, "BandTop", new Vector3(0f, h * 0.92f, 0f), new Vector3(w * 1.02f, post * 0.55f, d * 1.02f), Vector3.zero, iron);
                    AddBox(parent, "BandBot", new Vector3(0f, h * 0.08f, 0f), new Vector3(w * 1.02f, post * 0.55f, d * 1.02f), Vector3.zero, iron);
                }
                if (isLong)
                {
                    AddBox(parent, "CleatL", new Vector3(-w * 0.25f, h * 0.5f, 0f), new Vector3(post, h * 1.03f, d * 1.04f), Vector3.zero, darkWood);
                    AddBox(parent, "CleatR", new Vector3(w * 0.25f, h * 0.5f, 0f), new Vector3(post, h * 1.03f, d * 1.04f), Vector3.zero, darkWood);
                }
            }

            if (inst.StencilDecal)
            {
                Material stencil = GetMaterial(palette, "stencil_stripe", inst, 0.12f, 0f);
                AddBox(parent, "StencilMark", new Vector3(0f, h * 0.65f, d * 0.485f), new Vector3(w * 0.52f, h * 0.16f, 0.02f), Vector3.zero, stencil);
                AddBox(parent, "StencilMarkBack", new Vector3(0f, h * 0.65f, -d * 0.485f), new Vector3(w * 0.52f, h * 0.16f, 0.02f), Vector3.zero, stencil);
            }

            for (int a = 0; a < inst.ActiveAttachments.Length; a++)
            {
                if (inst.ActiveAttachments[a] == "rope_tie")
                {
                    Material rope = GetMaterial(palette, "rope_hemp", inst, 0.08f, 0f);
                    AddBox(parent, "RopeTie", new Vector3(w * 0.16f, h * 0.5f, 0f), new Vector3(0.035f, h * 1.03f, d * 1.03f), Vector3.zero, rope);
                }
            }
        }

        private static void AddCrateFramePosts(Transform parent, float w, float h, float d, float post, Material mat)
        {
            float hx = w * 0.5f - post * 0.5f;
            float hz = d * 0.5f - post * 0.5f;
            AddBox(parent, "PostFL", new Vector3(-hx, h * 0.5f, hz), new Vector3(post, h, post), Vector3.zero, mat);
            AddBox(parent, "PostFR", new Vector3(hx, h * 0.5f, hz), new Vector3(post, h, post), Vector3.zero, mat);
            AddBox(parent, "PostBL", new Vector3(-hx, h * 0.5f, -hz), new Vector3(post, h, post), Vector3.zero, mat);
            AddBox(parent, "PostBR", new Vector3(hx, h * 0.5f, -hz), new Vector3(post, h, post), Vector3.zero, mat);
            AddBox(parent, "RimTopF", new Vector3(0f, h - post * 0.5f, hz), new Vector3(w, post, post), Vector3.zero, mat);
            AddBox(parent, "RimTopB", new Vector3(0f, h - post * 0.5f, -hz), new Vector3(w, post, post), Vector3.zero, mat);
            AddBox(parent, "RimBotF", new Vector3(0f, post * 0.5f, hz), new Vector3(w, post, post), Vector3.zero, mat);
            AddBox(parent, "RimBotB", new Vector3(0f, post * 0.5f, -hz), new Vector3(w, post, post), Vector3.zero, mat);
        }

        private static void BuildSupplyPallet(Transform parent, RegionPropInstance inst, Palette palette)
        {
            float w = inst.DimensionsX * inst.ScaleX;
            float h = inst.DimensionsY * inst.ScaleY;
            float d = inst.DimensionsZ * inst.ScaleZ;
            Material wood = GetMaterial(palette, inst.MaterialVariant, inst, 0.12f, 0f);
            for (int i = -1; i <= 1; i++)
                AddBox(parent, "Runner_" + i, new Vector3(i * w * 0.42f, h * 0.35f, 0f), new Vector3(0.09f, h * 0.7f, d), Vector3.zero, wood);
            for (int i = -2; i <= 2; i++)
                AddBox(parent, "Deck_" + i, new Vector3(0f, h * 0.85f, i * d * 0.21f), new Vector3(w, h * 0.3f, d * 0.15f), Vector3.zero, wood);
        }

        private static void BuildWoodSplinters(Transform parent, RegionPropInstance inst, Palette palette)
        {
            Material wood = GetMaterial(palette, inst.MaterialVariant, inst, 0.12f, 0f);
            AddBox(parent, "SplinterA", new Vector3(-0.12f, 0.04f, 0.04f), new Vector3(0.65f, 0.04f, 0.12f), new Vector3(4f, 22f, -6f), wood);
            AddBox(parent, "SplinterB", new Vector3(0.14f, 0.06f, -0.05f), new Vector3(0.55f, 0.035f, 0.1f), new Vector3(-7f, -40f, 8f), wood);
        }

        private static void BuildStoneRubble(Transform parent, RegionPropInstance inst, Palette palette, int seedIndex)
        {
            Material rock = GetMaterial(palette, inst.MaterialVariant, inst, 0.10f, 0f);
            int subCount = inst.Role == "CorePile" ? 3 : (inst.Role == "MidFragments" ? 2 : 3);
            float baseRx = inst.DimensionsX * inst.ScaleX * 0.5f;
            float baseRy = inst.DimensionsY * inst.ScaleY;
            float baseRz = inst.DimensionsZ * inst.ScaleZ * 0.5f;

            for (int s = 0; s < subCount; s++)
            {
                uint h = RegionNoise.Hash(seedIndex, s + 11, 8821u);
                float ang = (h & 255) / 255f * Mathf.PI * 2f;
                float off = s == 0 ? 0f : baseRx * 0.45f;
                Vector3 center = new Vector3(Mathf.Cos(ang) * off, baseRy * (s == 0 ? 0.36f : 0.24f), Mathf.Sin(ang) * off);
                Vector3 radii = new Vector3(
                    baseRx * (s == 0 ? 0.82f : 0.52f),
                    baseRy * (s == 0 ? 0.62f : 0.42f),
                    baseRz * (s == 0 ? 0.82f : 0.52f));
                Mesh chunk = CreateFacetedRockMesh("Rubble_" + s, radii, unchecked((uint)(seedIndex * 97 + s * 31)), 0.34f);
                AddMesh(parent, "Chunk_" + s, chunk, center, new Vector3((s * 17f) % 24f - 12f, (h % 360), (s * 13f) % 20f - 10f), rock);
            }
        }

        private static void BuildStratifiedSlab(Transform parent, RegionPropInstance inst, Palette palette, int seedIndex)
        {
            Material rock = GetMaterial(palette, inst.MaterialVariant, inst, 0.10f, 0f);
            float w = inst.DimensionsX * inst.ScaleX;
            float h = inst.DimensionsY * inst.ScaleY;
            float d = inst.DimensionsZ * inst.ScaleZ;
            int layers = 3;
            for (int l = 0; l < layers; l++)
            {
                float t = l / (float)layers;
                float shrink = 1f - l * 0.17f;
                Vector3 radii = new Vector3(w * 0.5f * shrink, h / layers * 0.58f, d * 0.5f * shrink);
                Mesh slab = CreateFacetedRockMesh("Slab_" + l, radii, unchecked((uint)(seedIndex * 53 + l * 19)), 0.22f, flattenTop: true);
                AddMesh(parent, "Layer_" + l, slab,
                    new Vector3((l - 1) * 0.16f, h * (t + 0.18f), (l % 2 == 0 ? 0.08f : -0.08f)),
                    new Vector3(0f, l * 9f - 8f, 0f), rock);
            }
        }

        private static void BuildRuinPillar(Transform parent, RegionPropInstance inst, Palette palette, int seedIndex)
        {
            Material stone = GetMaterial(palette, inst.MaterialVariant, inst, 0.12f, 0f);
            float w = inst.DimensionsX * inst.ScaleX;
            float h = inst.DimensionsY * inst.ScaleY;
            float d = inst.DimensionsZ * inst.ScaleZ;

            // 基壇
            AddBox(parent, "Plinth", new Vector3(0f, 0.16f, 0f), new Vector3(w * 1.24f, 0.32f, d * 1.24f), Vector3.zero, stone);

            if (inst.DamageState == "fallen_drum")
            {
                Mesh drum = CreateCylinderMesh("FallenDrum", w * 0.44f, h * 0.38f, 10);
                AddMesh(parent, "Drum", drum, new Vector3(0f, w * 0.44f, 0f), new Vector3(90f, 20f, 0f), stone);
                return;
            }

            float shaftH = inst.DamageState == "intact" ? h - 0.55f : (inst.DamageState == "stump" ? h * 0.28f : h * 0.58f);
            int sides = inst.ConstructionVariant == "plain_square" ? 4 : (inst.ConstructionVariant == "octagonal" ? 8 : 12);
            Mesh shaft = CreateCylinderMesh("PillarShaft", w * 0.44f, shaftH, sides);
            AddMesh(parent, "Shaft", shaft, new Vector3(0f, 0.32f + shaftH * 0.5f, 0f), Vector3.zero, stone);

            if (inst.DamageState == "intact")
            {
                AddBox(parent, "Capital", new Vector3(0f, 0.32f + shaftH + 0.14f, 0f), new Vector3(w * 1.22f, 0.28f, d * 1.22f), Vector3.zero, stone);
            }
            else if (inst.DamageState == "broken_mid")
            {
                Mesh topBreak = CreateFacetedRockMesh("BrokenCrown", new Vector3(w * 0.44f, 0.28f, d * 0.44f), (uint)(seedIndex + 401), 0.25f);
                AddMesh(parent, "BrokenTop", topBreak, new Vector3(0f, 0.32f + shaftH, 0f), new Vector3(14f, 25f, -12f), stone);
                // 折れた柱頭がすぐ横に転がる
                Mesh fallen = CreateCylinderMesh("FallenDrum", w * 0.42f, h * 0.24f, sides);
                AddMesh(parent, "FallenSegment", fallen, new Vector3(w * 1.35f, w * 0.40f, 0.45f), new Vector3(84f, 38f, 8f), stone);
            }
        }

        private static void BuildWoodBarrel(Transform parent, RegionPropInstance inst, Palette palette)
        {
            float w = inst.DimensionsX * inst.ScaleX;
            float h = inst.DimensionsY * inst.ScaleY;
            Material wood = GetMaterial(palette, inst.MaterialVariant, inst, 0.14f, 0f);
            Material iron = GetMaterial(palette, "iron_metal", inst, 0.34f, 0.70f);
            Mesh body = CreateCylinderMesh("BarrelBody", w * 0.5f, h, 10, bulge: 1.12f);

            if (inst.ConstructionVariant == "tipped_sideways")
            {
                var holder = new GameObject("Tipped");
                holder.transform.SetParent(parent, false);
                holder.transform.localPosition = new Vector3(0f, w * 0.52f, 0f);
                holder.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                AddMesh(holder.transform, "Body", body, Vector3.zero, Vector3.zero, wood);
                AddMesh(holder.transform, "HoopTop", CreateCylinderMesh("Hoop", w * 0.54f, 0.05f, 10), new Vector3(0f, h * 0.32f, 0f), Vector3.zero, iron);
                AddMesh(holder.transform, "HoopBot", CreateCylinderMesh("Hoop", w * 0.54f, 0.05f, 10), new Vector3(0f, -h * 0.32f, 0f), Vector3.zero, iron);
                // 転がり止め楔
                AddBox(parent, "ChockWedge", new Vector3(w * 0.42f, 0.06f, 0f), new Vector3(0.14f, 0.12f, 0.28f), new Vector3(0f, 0f, 18f), wood);
            }
            else
            {
                AddMesh(parent, "Body", body, new Vector3(0f, h * 0.5f, 0f), Vector3.zero, wood);
                AddMesh(parent, "HoopTop", CreateCylinderMesh("Hoop", w * 0.54f, 0.05f, 10), new Vector3(0f, h * 0.82f, 0f), Vector3.zero, iron);
                AddMesh(parent, "HoopMid", CreateCylinderMesh("Hoop", w * 0.57f, 0.05f, 10), new Vector3(0f, h * 0.50f, 0f), Vector3.zero, iron);
                AddMesh(parent, "HoopBot", CreateCylinderMesh("Hoop", w * 0.54f, 0.05f, 10), new Vector3(0f, h * 0.18f, 0f), Vector3.zero, iron);
            }
        }

        private static void BuildTrailSignpost(Transform parent, RegionPropInstance inst, Palette palette)
        {
            float h = inst.DimensionsY * inst.ScaleY;
            Material wood = GetMaterial(palette, inst.MaterialVariant, inst, 0.15f, 0f);
            Material accent = GetMaterial(palette, "stencil_stripe", inst, 0.14f, 0f);
            AddBox(parent, "Post", new Vector3(0f, h * 0.5f, 0f), new Vector3(0.14f, h, 0.14f), Vector3.zero, wood);
            AddBox(parent, "SignBoardA", new Vector3(0.18f, h * 0.82f, 0f), new Vector3(0.75f, 0.18f, 0.04f), new Vector3(0f, 14f, 0f), accent);
            AddBox(parent, "SignBoardB", new Vector3(-0.14f, h * 0.66f, 0.06f), new Vector3(0.68f, 0.16f, 0.04f), new Vector3(0f, -28f, 0f), wood);
            AddMesh(parent, "StoneCollar", CreateFacetedRockMesh("Collar", new Vector3(0.32f, 0.16f, 0.32f), 71u, 0.25f),
                new Vector3(0f, 0.08f, 0f), Vector3.zero, GetMaterial(palette, "sandstone_rubble", inst, 0.10f, 0f));
        }

        private static Mesh CreateFacetedRockMesh(string name, Vector3 radii, uint seed, float noiseAmp, bool flattenTop = false)
        {
            // 細分化した球を岩状に歪め、フラットシェーディング（頂点分離）で角張った岩面を作る
            Vector3[] baseVerts =
            {
                new Vector3( 1, 0, 0), new Vector3(-1, 0, 0),
                new Vector3( 0, 1, 0), new Vector3( 0,-1, 0),
                new Vector3( 0, 0, 1), new Vector3( 0, 0,-1)
            };
            int[] baseTris =
            {
                0,2,4, 4,2,1, 1,2,5, 5,2,0,
                4,3,0, 1,3,4, 5,3,1, 0,3,5
            };

            // 1回細分化して 32 面にする
            var verts = new List<Vector3>(baseVerts);
            var tris = new List<int>();
            var midCache = new Dictionary<long, int>();
            for (int t = 0; t < baseTris.Length; t += 3)
            {
                int a = baseTris[t], b = baseTris[t + 1], c = baseTris[t + 2];
                int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                tris.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }

            int Mid(int i, int j)
            {
                long key = i < j ? ((long)i << 32) | (uint)j : ((long)j << 32) | (uint)i;
                if (midCache.TryGetValue(key, out int m)) return m;
                Vector3 v = (verts[i] + verts[j]).normalized;
                m = verts.Count;
                verts.Add(v);
                midCache[key] = m;
                return m;
            }

            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 dir = verts[i].normalized;
                float n = RegionNoise.Fbm(dir.x * 1.9f + 3.1f, dir.z * 1.9f - 2.3f + dir.y * 1.3f, 2, seed);
                float r = 1f + n * noiseAmp;
                Vector3 p = new Vector3(dir.x * radii.x * r, dir.y * radii.y * r, dir.z * radii.z * r);
                if (flattenTop && p.y > radii.y * 0.45f) p.y = radii.y * 0.45f + (p.y - radii.y * 0.45f) * 0.25f;
                if (p.y < -radii.y * 0.65f) p.y = -radii.y * 0.65f;
                verts[i] = p;
            }

            // フラット法線のため三角形ごとに頂点を展開
            var flatVerts = new Vector3[tris.Count];
            var flatNorms = new Vector3[tris.Count];
            var flatIndices = new int[tris.Count];
            for (int t = 0; t < tris.Count; t += 3)
            {
                Vector3 a = verts[tris[t]];
                Vector3 b = verts[tris[t + 1]];
                Vector3 c = verts[tris[t + 2]];
                Vector3 norm = Vector3.Cross(b - a, c - a).normalized;
                if (norm.sqrMagnitude < 1e-6f) norm = Vector3.up;
                flatVerts[t] = a; flatVerts[t + 1] = b; flatVerts[t + 2] = c;
                flatNorms[t] = norm; flatNorms[t + 1] = norm; flatNorms[t + 2] = norm;
                flatIndices[t] = t; flatIndices[t + 1] = t + 1; flatIndices[t + 2] = t + 2;
            }

            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(flatVerts);
            mesh.SetNormals(flatNorms);
            mesh.SetTriangles(flatIndices, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateCylinderMesh(string name, float radius, float height, int sides, float bulge = 1f)
        {
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var tris = new List<int>();
            float hy = height * 0.5f;
            float midR = radius * bulge;

            for (int i = 0; i < sides; i++)
            {
                float a0 = (i / (float)sides) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)sides) * Mathf.PI * 2f;
                float c0 = Mathf.Cos(a0), s0 = Mathf.Sin(a0);
                float c1 = Mathf.Cos(a1), s1 = Mathf.Sin(a1);
                Vector3 bl = new Vector3(c0 * radius, -hy, s0 * radius);
                Vector3 br = new Vector3(c1 * radius, -hy, s1 * radius);
                Vector3 ml = new Vector3(c0 * midR, 0f, s0 * midR);
                Vector3 mr = new Vector3(c1 * midR, 0f, s1 * midR);
                Vector3 tl = new Vector3(c0 * radius, hy, s0 * radius);
                Vector3 tr = new Vector3(c1 * radius, hy, s1 * radius);

                AddQuad(bl, ml, mr, br);
                AddQuad(ml, tl, tr, mr);
                AddTri(new Vector3(0f, hy, 0f), tr, tl);
                AddTri(new Vector3(0f, -hy, 0f), bl, br);
            }

            void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                AddTri(a, b, c);
                AddTri(a, c, d);
            }

            void AddTri(Vector3 a, Vector3 b, Vector3 c)
            {
                int start = verts.Count;
                Vector3 n = Vector3.Cross(b - a, c - a).normalized;
                if (n.sqrMagnitude < 1e-6f) n = Vector3.up;
                verts.Add(a); verts.Add(b); verts.Add(c);
                norms.Add(n); norms.Add(n); norms.Add(n);
                tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
            }

            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddBox(Transform parent, string name, Vector3 localPos, Vector3 scale, Vector3 eulerDeg, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Collider col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(eulerDeg);
            go.transform.localScale = scale;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        private static void AddMesh(Transform parent, string name, Mesh mesh, Vector3 localPos, Vector3 eulerDeg, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(eulerDeg);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        private static Shader FindPropShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s != null) return s;
            s = Shader.Find("Standard");
            if (s != null) return s;
            return Resources.Load<Shader>(RegionTerrainView.ShaderResource);
        }

        private static Material GetMaterial(Palette palette, string variantKey, RegionPropInstance inst, float smoothness, float metallic, float valueAdjust = 0f)
        {
            // Tint を 4 段階に丸めてマテリアル数を抑えつつ個体差を出す
            int tintBucket = Mathf.RoundToInt(inst.TintShift * 25f);
            int dustBucket = Mathf.RoundToInt(inst.TopDust * 4f);
            string cacheKey = variantKey + "|" + tintBucket + "|" + dustBucket + "|" + valueAdjust.ToString("F2");
            if (palette.Materials.TryGetValue(cacheKey, out Material cached)) return cached;

            Color linearColor = ResolveLinearAlbedo(variantKey);
            float factor = Mathf.Clamp(1f + inst.TintShift + valueAdjust, 0.72f, 1.28f);
            linearColor.r *= factor;
            linearColor.g *= factor;
            linearColor.b *= factor;

            // V7 TopDust: 乾燥砂塵色との控えめななじませ
            Color dustLinear = new Color(0.56f, 0.41f, 0.27f, 1f).linear;
            float dustBlend = Mathf.Clamp01(inst.TopDust * 0.28f);
            linearColor = Color.Lerp(linearColor, dustLinear, dustBlend);

            // DS-011 最低明度 (sRGB >= 32 -> Linear >= 0.015) を保証
            linearColor.r = Mathf.Max(0.016f, linearColor.r);
            linearColor.g = Mathf.Max(0.016f, linearColor.g);
            linearColor.b = Mathf.Max(0.016f, linearColor.b);

            var mat = new Material(palette.Shader) { name = "DS011_" + variantKey, hideFlags = HideFlags.DontSave };
            Color displayColor = QualitySettings.activeColorSpace == ColorSpace.Linear ? linearColor.gamma : linearColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", displayColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", displayColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            palette.Materials[cacheKey] = mat;
            return mat;
        }

        private static Color ResolveLinearAlbedo(string key)
        {
            Color srgb;
            if (key.StartsWith("raw_pine", StringComparison.Ordinal)) srgb = new Color(0.73f, 0.56f, 0.35f, 1f);
            else if (key.StartsWith("weathered_cedar", StringComparison.Ordinal)) srgb = new Color(0.47f, 0.38f, 0.30f, 1f);
            else if (key.StartsWith("painted_stencil_crate", StringComparison.Ordinal)) srgb = new Color(0.28f, 0.37f, 0.27f, 1f);
            else if (key.StartsWith("iron_banded_dark_wood", StringComparison.Ordinal)) srgb = new Color(0.36f, 0.25f, 0.18f, 1f);
            else if (key.StartsWith("oak_iron_hoops", StringComparison.Ordinal)) srgb = new Color(0.46f, 0.32f, 0.20f, 1f);
            else if (key.StartsWith("dark_tarred_wood", StringComparison.Ordinal)) srgb = new Color(0.24f, 0.19f, 0.15f, 1f);
            else if (key.StartsWith("sun_bleached", StringComparison.Ordinal)) srgb = new Color(0.62f, 0.56f, 0.48f, 1f);
            else if (key.StartsWith("sandstone_rubble", StringComparison.Ordinal) || key.StartsWith("red_sandstone", StringComparison.Ordinal) || key.StartsWith("sandstone_carved", StringComparison.Ordinal))
                srgb = new Color(0.72f, 0.43f, 0.28f, 1f);
            else if (key.StartsWith("limestone_rubble", StringComparison.Ordinal) || key.StartsWith("pale_bleached", StringComparison.Ordinal) || key.StartsWith("limestone_weathered", StringComparison.Ordinal))
                srgb = new Color(0.74f, 0.67f, 0.56f, 1f);
            else if (key.StartsWith("basalt", StringComparison.Ordinal) || key.StartsWith("desert_varnish", StringComparison.Ordinal))
                srgb = new Color(0.32f, 0.29f, 0.28f, 1f);
            else if (key == "iron_metal") srgb = new Color(0.22f, 0.23f, 0.25f, 1f);
            else if (key == "stencil_stripe") srgb = new Color(0.84f, 0.78f, 0.52f, 1f);
            else if (key == "cargo_cloth") srgb = new Color(0.64f, 0.55f, 0.40f, 1f);
            else if (key == "packing_straw") srgb = new Color(0.76f, 0.64f, 0.34f, 1f);
            else if (key == "rope_hemp") srgb = new Color(0.58f, 0.47f, 0.31f, 1f);
            else srgb = new Color(0.55f, 0.45f, 0.35f, 1f);
            return srgb.linear;
        }
    }
}

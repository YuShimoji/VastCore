using System;
using System.Collections.Generic;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// DS-011 の 7 軸バリエーションを解決した配置インスタンス 1 件（Core 座標系: X 東・Y 上・Z 南、m）。
    /// </summary>
    public sealed class RegionPropInstance
    {
        public string PropId = "";
        public string Role = "Primary";
        public int ClusterIndex;
        public int MemberIndex;
        public int StackLayer;
        public int StackSupportMemberIndex = -1;
        public string ClusterModelVariant = "";

        public float X;
        public float Y;
        public float Z;

        public float YawDeg;
        public float PitchDeg;
        public float RollDeg;

        public float ScaleX = 1f;
        public float ScaleY = 1f;
        public float ScaleZ = 1f;

        public float DimensionsX = 1f;
        public float DimensionsY = 1f;
        public float DimensionsZ = 1f;

        public float EmbedDepthM;
        public RegionPropPivotRule PivotRule = RegionPropPivotRule.StackableUpright;
        public bool NeedsPalletBase;

        public string MeshVariant = "";
        public string MaterialVariant = "";
        public string DamageState = "";
        public string ConstructionVariant = "";
        public string[] ActiveAttachments = Array.Empty<string>();

        public float TintShift;
        public float EdgeWear;
        public float RustRatio;
        public bool StencilDecal;

        public float TopDust;
        public float GroundSkirtBlendM;
        public float MossMask;

        public float EffectiveHeightM => DimensionsY * ScaleY;
    }

    /// <summary>
    /// 3D 砂岩シーン上の DS-011 P0 Proof Slice 配置結果と、比較撮影用の視点情報。
    /// </summary>
    public sealed class RegionPropProofSlice
    {
        public readonly List<RegionPropInstance> Instances = new List<RegionPropInstance>();
        public RegionBiome Biome = RegionBiome.Desert;
        public RegionLookViewpoint CratesViewpoint;
        public RegionLookViewpoint RubbleViewpoint;
        public float CratesCenterX, CratesCenterZ;
        public float RubbleCenterX, RubbleCenterZ;
        public float RubbleFlowDirX, RubbleFlowDirZ;
        public Func<float, float, float> RenderedHeightAt;
        /// <summary>true で、RegionPropView がプレースホルダの組に加えて実アセットの組（DS011_Props_Real）も作る（RegionLookSpec.PropsRealAssets）。</summary>
        public bool UseRealAssets;
        /// <summary>true で、草原向けのモデル変種と地面なじませ色を使う。</summary>
        public bool UseGrasslandVariants;
        public bool HasGroundColor;
        public float GroundRed = 0.72f;
        public float GroundGreen = 0.56f;
        public float GroundBlue = 0.40f;
        public RegionLookViewpoint GrasslandPanoramaViewpoint;
        public RegionLookViewpoint GrasslandEyeViewpoint;
        public RegionLookViewpoint GrasslandOutcropViewpoint;
        public RegionLookViewpoint GrasslandTrailViewpoint;
    }

    /// <summary>
    /// DS-011 の V1〜V7 軸と SpawningRules に従い、決定的シードで Props の姿勢・損傷・組み方・材質・群生を解決する。
    /// </summary>
    public static class RegionPropPlacementSolver
    {
        public static RegionPropInstance ResolveSingle(
            RegionPropDefinition prop,
            uint seed,
            int index,
            float x,
            float y,
            float z,
            float slopeDeg,
            float slopeDirX = 0f,
            float slopeDirZ = 1f,
            string parentMaterial = null,
            string role = "Primary",
            float scaleOverride = -1f,
            float? yawOverride = null,
            string constructionOverride = null,
            string materialOverride = null,
            string damageOverride = null,
            RegionBiome biome = RegionBiome.Desert)
        {
            if (prop == null) throw new ArgumentNullException("prop");
            RegionPropVariationRules vr = prop.VariationRules;
            RegionPropTransformRules v1 = vr.V1_Transform;
            uint s = unchecked(seed * 2654435761u + (uint)index * 374761393u + HashString(prop.Id));

            // V2: 損傷状態
            string damage = !string.IsNullOrEmpty(damageOverride)
                ? damageOverride
                : PickWeighted(vr.V2_DamageState != null ? vr.V2_DamageState.States : null,
                               vr.V2_DamageState != null ? vr.V2_DamageState.Weights : null,
                               Hash01(index, 1, s));

            // V3: 組み方・姿勢
            string construction = !string.IsNullOrEmpty(constructionOverride)
                ? constructionOverride
                : PickConstruction(prop, damage, Hash01(index, 2, s));

            // V1: ヨー角
            float yaw = yawOverride.HasValue
                ? NormalizeAngle(yawOverride.Value)
                : ResolveYaw(v1, slopeDirX, slopeDirZ, Hash01(index, 3, s), Hash01(index, 4, s));

            // V1: 傾斜追従と姿勢制限
            float tiltLimit = v1.MaxTiltDeg;
            if ((damage == "leaning_weathered" || damage == "leaning_faded") && v1.DamagedTiltDeg > tiltLimit)
                tiltLimit = v1.DamagedTiltDeg;
            if (damage == "fallen_drum" && v1.FallenMaxTiltDeg > tiltLimit)
                tiltLimit = v1.FallenMaxTiltDeg;

            float pitch;
            float roll;
            ResolveTilt(prop, v1, slopeDeg, slopeDirX, slopeDirZ, yaw, tiltLimit,
                Hash01(index, 5, s), Hash01(index, 6, s), Hash01(index, 7, s), out pitch, out roll);

            // V1: スケール（均一 + 制限付き不均一）
            float uniformScale = scaleOverride > 0f
                ? scaleOverride
                : Lerp(v1.ScaleMin, v1.ScaleMax, Hash01(index, 8, s));
            float nu = v1.NonUniformMax;
            float sx = uniformScale * (1f + (Hash01(index, 9, s) * 2f - 1f) * nu);
            float sy = uniformScale * (1f + (Hash01(index, 10, s) * 2f - 1f) * nu);
            float sz = uniformScale * (1f + (Hash01(index, 11, s) * 2f - 1f) * nu);

            float dimX = prop.DimensionsM.X;
            float dimY = prop.DimensionsM.Y;
            float dimZ = prop.DimensionsM.Z;
            if (prop.Id == "cnt_wood_crate" && construction == "long_box")
            {
                dimX = 1.5f;
                dimY = 0.5f;
                dimZ = 0.5f;
            }
            if (prop.Id == "cnt_wood_crate" && damage == "collapsed_wreck")
            {
                sy *= 0.38f;
                sx *= 1.12f;
                sz *= 1.12f;
            }

            // 接地・埋没深度
            float embed = ResolveEmbedDepth(prop, dimY * sy, Hash01(index, 12, s));

            // V4: 材質・経年
            string material;
            if (!string.IsNullOrEmpty(materialOverride))
                material = materialOverride;
            else if (vr.V4_MaterialWeathering.InheritParentMaterial && !string.IsNullOrEmpty(parentMaterial))
                material = parentMaterial;
            else
                material = PickArray(prop.MaterialVariants, Hash01(index, 13, s), "default");

            string mesh = ResolveMeshVariant(prop, role, damage, construction, Hash01(index, 14, s));

            float tintRange = vr.V4_MaterialWeathering.PerInstanceTintShift > 0f
                ? vr.V4_MaterialWeathering.PerInstanceTintShift
                : vr.V4_MaterialWeathering.PerInstanceValueShift;
            float tintShift = (Hash01(index, 15, s) * 2f - 1f) * tintRange;
            float edgeWear = Lerp(vr.V4_MaterialWeathering.EdgeWearMin, vr.V4_MaterialWeathering.EdgeWearMax, Hash01(index, 16, s));
            float rustRatio = Lerp(vr.V4_MaterialWeathering.RustRatioMin, vr.V4_MaterialWeathering.RustRatioMax, Hash01(index, 17, s));
            bool stencilDecal = vr.V4_MaterialWeathering.StencilDecalToggle &&
                                (material.Contains("stencil") || Hash01(index, 18, s) < 0.35f);

            // V5: 付属物
            var attachments = new List<string>();
            if (vr.V5_ContentsAttachment != null)
            {
                for (int a = 0; a < vr.V5_ContentsAttachment.Length; a++)
                {
                    if (Hash01(index, 100 + a, s) < 0.45f)
                        attachments.Add(vr.V5_ContentsAttachment[a]);
                }
            }
            if (prop.Id == "cnt_wood_barrel" && construction == "tipped_sideways" && !attachments.Contains("wooden_chock_wedge"))
                attachments.Add("wooden_chock_wedge");

            // V7: 環境順応（バイオームに応じて砂塵と苔の重みを切り替える）
            RegionPropOvergrowthRules v7 = vr.V7_BiomeOvergrowth;
            RegionBiomeSurfaceProfile biomeProfile = RegionBiomeSurface.ProfileFor(biome);
            float topDust = Lerp(v7.TopDustMin, v7.TopDustMax, Hash01(index, 19, s)) * biomeProfile.TopDustScale;
            float baseMoss = v7.MossMaskMax > 0f
                ? Lerp(v7.MossMaskMin, v7.MossMaskMax, Hash01(index, 20, s))
                : (biomeProfile.MossMaskScale > 0f ? Lerp(0.12f, 0.38f, Hash01(index, 20, s)) : 0f);
            float mossMask = Clamp(baseMoss * biomeProfile.MossMaskScale, 0f, 1f);

            bool needsPallet = prop.SpawningRules.UsePalletUnderOnSlopeOverDeg >= 0f &&
                               slopeDeg > prop.SpawningRules.UsePalletUnderOnSlopeOverDeg;

            return new RegionPropInstance
            {
                PropId = prop.Id,
                Role = role,
                ClusterIndex = index,
                MemberIndex = 0,
                StackLayer = 0,
                X = x,
                Y = y - embed,
                Z = z,
                YawDeg = yaw,
                PitchDeg = pitch,
                RollDeg = roll,
                ScaleX = sx,
                ScaleY = sy,
                ScaleZ = sz,
                DimensionsX = dimX,
                DimensionsY = dimY,
                DimensionsZ = dimZ,
                EmbedDepthM = embed,
                PivotRule = prop.PivotRule,
                NeedsPalletBase = needsPallet,
                MeshVariant = mesh,
                MaterialVariant = material,
                DamageState = damage,
                ConstructionVariant = construction,
                ActiveAttachments = attachments.ToArray(),
                TintShift = tintShift,
                EdgeWear = edgeWear,
                RustRatio = rustRatio,
                StencilDecal = stencilDecal,
                TopDust = topDust,
                GroundSkirtBlendM = v7.GroundSkirtBlendM,
                MossMask = mossMask
            };
        }

        public static List<RegionPropInstance> PlaceCluster(
            RegionPropCatalog catalog,
            RegionPropDefinition prop,
            uint seed,
            int anchorIndex,
            float x,
            float y,
            float z,
            float slopeDeg,
            float slopeDirX,
            float slopeDirZ,
            Func<float, float, float> heightAt,
            float flowDirX = 0f,
            float flowDirZ = 0f,
            string parentMaterial = null,
            int minimumStackCrates = 0)
        {
            if (prop == null) throw new ArgumentNullException("prop");
            var result = new List<RegionPropInstance>();
            RegionPropClusterRules v6 = prop.VariationRules.V6_ClusterSpread;
            uint s = unchecked(seed * 16777619u + (uint)anchorIndex * 40503u + HashString(prop.Id));

            // 1. DirectionalCone (3層瓦礫広がり)
            if (v6.Pattern == RegionPropClusterPattern.DirectionalCone && v6.Layers.Count > 0)
            {
                float dirX = flowDirX * flowDirX + flowDirZ * flowDirZ > 1e-4f ? flowDirX : slopeDirX;
                float dirZ = flowDirX * flowDirX + flowDirZ * flowDirZ > 1e-4f ? flowDirZ : slopeDirZ;
                Normalize2(ref dirX, ref dirZ);
                float baseAngle = (float)Math.Atan2(dirZ, dirX);
                float halfCone = v6.ConeAngleDeg * 0.5f * (float)(Math.PI / 180.0);
                float radiusM = Math.Max(2.5f, prop.DimensionsM.X * 1.4f);

                // 先に親色を決める
                string coreMat = !string.IsNullOrEmpty(parentMaterial)
                    ? parentMaterial
                    : PickArray(prop.MaterialVariants, Hash01(anchorIndex, 50, s), "sandstone_rubble");

                int member = 0;
                for (int l = 0; l < v6.Layers.Count; l++)
                {
                    RegionPropSpreadLayer layer = v6.Layers[l];
                    int count = PickIntRange(layer.CountMin, layer.CountMax, Hash01(anchorIndex, 60 + l, s));
                    float layerScaleMul = layer.Role == "CorePile" ? 1.0f : (layer.Role == "MidFragments" ? 0.42f : 0.16f);
                    for (int c = 0; c < count; c++)
                    {
                        float hDist = Hash01(member, 200 + l, s);
                        float hAng = Hash01(member, 300 + l, s);
                        float distNorm = Lerp(layer.DistNormMin, layer.DistNormMax, hDist);
                        float dist = distNorm * radiusM;
                        float ang = baseAngle + (hAng * 2f - 1f) * halfCone;
                        float px = x + (float)Math.Cos(ang) * dist;
                        float pz = z + (float)Math.Sin(ang) * dist;
                        float py = heightAt != null ? heightAt(px, pz) : y;

                        float rawScale = Lerp(layer.ScaleMin, layer.ScaleMax, Hash01(member, 400 + l, s));
                        // 距離が遠いほど小さくする (sizeGradient = large_near_source_small_far)
                        float gradScale = rawScale * layerScaleMul * Lerp(1.1f, 0.72f, distNorm);

                        RegionPropInstance inst = ResolveSingle(
                            prop, seed, anchorIndex * 1000 + member, px, py, pz,
                            slopeDeg, dirX, dirZ, coreMat, layer.Role, gradScale);
                        inst.ClusterIndex = anchorIndex;
                        inst.MemberIndex = member;
                        inst.ClusterModelVariant = v6.ClusterModelVariant;
                        result.Add(inst);
                        member++;
                    }
                }
                return result;
            }

            // 2. StackedGrid (木箱・パレットの積み上げ & 崩壊木片随伴)
            if (v6.Pattern == RegionPropClusterPattern.StackedGrid)
            {
                int total = PickIntRange(v6.ClusterCountMin, v6.ClusterCountMax, Hash01(anchorIndex, 70, s));
                int maxLayers = Math.Max(1, v6.MaxStackLayers);
                total = Math.Max(total, Math.Min(minimumStackCrates, v6.ClusterCountMax));
                bool stack = maxLayers > 1 && (total >= 3 || (total == 2 && minimumStackCrates >= 2));
                int baseCount = stack ? (total + 1) / 2 : total;
                if (baseCount < 1) baseCount = 1;
                if (baseCount > 3) baseCount = 3;

                float baseYaw = ResolveYaw(prop.VariationRules.V1_Transform, slopeDirX, slopeDirZ, Hash01(anchorIndex, 71, s), Hash01(anchorIndex, 72, s));
                float step = Math.Max(prop.DimensionsM.X, prop.DimensionsM.Z) * 1.18f;
                float cos = (float)Math.Cos(baseYaw * Math.PI / 180.0);
                float sin = (float)Math.Sin(baseYaw * Math.PI / 180.0);

                var baseCrates = new List<RegionPropInstance>();
                bool needPallet = prop.SpawningRules.UsePalletUnderOnSlopeOverDeg >= 0f &&
                                  slopeDeg > prop.SpawningRules.UsePalletUnderOnSlopeOverDeg;
                if (needPallet && catalog != null && catalog.TryGetProp("cnt_supply_pallet", out RegionPropDefinition palletProp))
                {
                    float py = heightAt != null ? heightAt(x, z) : y;
                    RegionPropInstance pallet = ResolveSingle(palletProp, seed, anchorIndex * 1000 + 900, x, py, z,
                        Math.Min(slopeDeg, palletProp.VariationRules.V1_Transform.MaxTiltDeg), slopeDirX, slopeDirZ,
                        null, "PalletBase", 1f, baseYaw);
                    pallet.ClusterIndex = anchorIndex;
                    result.Add(pallet);
                }

                int member = 0;
                for (int b = 0; b < baseCount; b++)
                {
                    float ox = (b == 0 ? 0f : (b == 1 ? step : -0.15f * step));
                    float oz = (b == 0 ? 0f : (b == 1 ? 0.12f * step : step));
                    float px = x + ox * cos - oz * sin;
                    float pz = z + ox * sin + oz * cos;
                    float py = (heightAt != null ? heightAt(px, pz) : y) + (needPallet ? 0.15f : 0f);
                    float jy = baseYaw + (Hash01(b, 80, s) * 2f - 1f) * 6f;

                    RegionPropInstance inst = ResolveSingle(
                        prop, seed, anchorIndex * 1000 + member, px, py, pz,
                        slopeDeg, slopeDirX, slopeDirZ, parentMaterial, "BaseLayer", -1f, jy);
                    inst.ClusterIndex = anchorIndex;
                    inst.MemberIndex = member++;
                    inst.StackLayer = 0;
                    inst.NeedsPalletBase = needPallet;
                    result.Add(inst);
                    baseCrates.Add(inst);

                    if (inst.DamageState == "collapsed_wreck" && !string.IsNullOrEmpty(v6.CollapsedSpills) &&
                        catalog != null && catalog.TryGetProp(v6.CollapsedSpills, out RegionPropDefinition spillProp))
                    {
                        for (int sp = 0; sp < 3; sp++)
                        {
                            float sa = Hash01(sp, 90 + b, s) * 6.2831853f;
                            float sr = 0.55f + 0.65f * Hash01(sp, 95 + b, s);
                            float sx = px + (float)Math.Cos(sa) * sr;
                            float sz = pz + (float)Math.Sin(sa) * sr;
                            float sy = heightAt != null ? heightAt(sx, sz) : y;
                            RegionPropInstance spill = ResolveSingle(spillProp, seed, anchorIndex * 1000 + 800 + sp,
                                sx, sy, sz, slopeDeg, slopeDirX, slopeDirZ, inst.MaterialVariant, "CollapsedSpill");
                            spill.ClusterIndex = anchorIndex;
                            spill.MemberIndex = member++;
                            result.Add(spill);
                        }
                    }
                }

                // 2段目の積み上げ
                int remaining = total - baseCount;
                var occupied = new HashSet<int>();
                for (int top = 0; top < remaining && maxLayers >= 2; top++)
                {
                    RegionPropInstance support = null;
                    for (int k = 0; k < baseCrates.Count; k++)
                    {
                        RegionPropInstance cand = baseCrates[(top + k) % baseCrates.Count];
                        if (occupied.Contains(cand.MemberIndex) ||
                            (cand.DamageState != "pristine" && cand.DamageState != "worn_chipped") ||
                            (cand.ConstructionVariant != "plank_closed" && cand.ConstructionVariant != "framed_reinforced"))
                            continue;
                        support = cand;
                        break;
                    }
                    if (support == null) break;

                    float topYaw = support.YawDeg + (Hash01(top, 85, s) * 2f - 1f) * v6.StackYawJitterDeg;
                    float topScale = Math.Min(support.ScaleX, Lerp(prop.VariationRules.V1_Transform.ScaleMin, 1f, Hash01(top, 86, s)));
                    RegionPropInstance stacked = ResolveSingle(
                        prop, seed, anchorIndex * 1000 + member,
                        support.X, support.Y + support.EffectiveHeightM, support.Z,
                        0f, slopeDirX, slopeDirZ, support.MaterialVariant, "StackedLayer",
                        topScale, topYaw, support.ConstructionVariant, damageOverride: "worn_chipped");
                    stacked.PitchDeg = support.PitchDeg;
                    stacked.RollDeg = support.RollDeg;
                    stacked.StackSupportMemberIndex = support.MemberIndex;
                    occupied.Add(support.MemberIndex);
                    stacked.ClusterIndex = anchorIndex;
                    stacked.MemberIndex = member++;
                    stacked.StackLayer = 1;
                    stacked.EmbedDepthM = 0f;
                    stacked.Y = support.Y + support.EffectiveHeightM;
                    result.Add(stacked);
                }

                return result;
            }

            // 3. Single / Huddled / RadialScatter / WallSkirt
            int countStd = PickIntRange(v6.ClusterCountMin, v6.ClusterCountMax, Hash01(anchorIndex, 73, s));
            float radiusStd = v6.RadiusM > 0f ? v6.RadiusM : Math.Max(prop.DimensionsM.X, prop.DimensionsM.Z) * 1.6f;
            RegionPropInstance primary = ResolveSingle(prop, seed, anchorIndex * 1000, x, y, z, slopeDeg, slopeDirX, slopeDirZ, parentMaterial, "Primary");
            primary.ClusterIndex = anchorIndex;
            result.Add(primary);

            for (int m = 1; m < countStd; m++)
            {
                float ang = v6.Pattern == RegionPropClusterPattern.WallSkirt
                    ? (primary.YawDeg * (float)(Math.PI / 180.0)) + (m % 2 == 0 ? 0f : (float)Math.PI) + (Hash01(m, 51, s) - 0.5f) * 0.45f
                    : Hash01(m, 52, s) * 6.2831853f;
                float distMin = Math.Max(prop.DimensionsM.X, prop.DimensionsM.Z) * 0.85f;
                float dist = Lerp(distMin, radiusStd, Hash01(m, 53, s));
                float px = x + (float)Math.Cos(ang) * dist;
                float pz = z + (float)Math.Sin(ang) * dist;
                float py = heightAt != null ? heightAt(px, pz) : y;
                RegionPropInstance follower = ResolveSingle(
                    prop, seed, anchorIndex * 1000 + m, px, py, pz,
                    slopeDeg, slopeDirX, slopeDirZ, primary.MaterialVariant, v6.Pattern.ToString());
                follower.ClusterIndex = anchorIndex;
                follower.MemberIndex = m;
                result.Add(follower);
            }

            // 遺跡柱などの足元瓦礫自動随伴 (requireDebrisSkirt)
            if (!string.IsNullOrEmpty(v6.RequireDebrisSkirt) && catalog != null &&
                catalog.TryGetProp(v6.RequireDebrisSkirt, out RegionPropDefinition skirtProp))
            {
                List<RegionPropInstance> skirt = PlaceCluster(
                    catalog, skirtProp, unchecked(seed + 7919u), anchorIndex + 50,
                    x, y, z, slopeDeg, slopeDirX, slopeDirZ, heightAt,
                    slopeDirX, slopeDirZ, primary.MaterialVariant);
                for (int i = 0; i < skirt.Count; i++)
                {
                    skirt[i].Role = "DebrisSkirt_" + skirt[i].Role;
                    result.Add(skirt[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// 3D 砂岩シーン（seed 9 等）上で、P0 代表 Props（木箱 5型・材質差、瓦礫 3層広がり、層状岩、木樽、遺跡柱）を
        /// 近景の視認位置へ配置する Proof Slice を生成する。
        /// </summary>
        public static RegionPropProofSlice BuildProofSlice(RegionPropCatalog catalog, RegionLookScene scene, int seedOffset = 0)
        {
            if (catalog == null) throw new ArgumentNullException("catalog");
            if (scene == null) throw new ArgumentNullException("scene");

            uint seed = unchecked((uint)(scene.RegionSeed + seedOffset) * 2654435761u + 131u);
            RegionBiomeSurfaceProfile biomeProfile = scene.BiomeSurface ?? RegionBiomeSurface.ProfileFor(scene.PrimaryBiome);
            var slice = new RegionPropProofSlice
            {
                Biome = scene.PrimaryBiome,
                RenderedHeightAt = scene.RenderedHeightAt,
                HasGroundColor = scene.PrimaryBiome != RegionBiome.Desert,
                GroundRed = biomeProfile.GroundSkirtR,
                GroundGreen = biomeProfile.GroundSkirtG,
                GroundBlue = biomeProfile.GroundSkirtB,
            };

            // 1. 木箱・補給キャンプ区画: Near 視点（Eye: -121, -69）の前方手前、緩斜面を探す
            FindGentleSpot(scene, scene.CenterX - 120f, scene.CenterZ - 81f, 7f,
                out float crateX, out float crateZ, out float crateSlope, out float crateDirX, out float crateDirZ);
            float crateGroundY = scene.HeightAt(crateX, crateZ);
            slice.CratesCenterX = crateX;
            slice.CratesCenterZ = crateZ;

            RegionPropDefinition crateProp = catalog.GetProp("cnt_wood_crate");
            string[] crateConstructions = { "plank_closed", "slat_open", "framed_reinforced", "lid_ajar", "long_box" };
            string[] crateMaterials = { "raw_pine", "weathered_cedar", "painted_stencil_crate", "iron_banded_dark_wood", "weathered_cedar" };
            string[] crateDamages = { "pristine", "worn_chipped", "pristine", "broken_slat_open", "worn_chipped" };

            // ショーケース列: 5つの組み方と4つの材質差を1視野で明確に見比べられる配置
            for (int i = 0; i < crateConstructions.Length; i++)
            {
                float px = crateX - 2.8f + i * 1.45f;
                float pz = crateZ + (i % 2 == 0 ? 0f : 0.35f);
                float py = scene.HeightAt(px, pz);
                SampleSlope(scene, px, pz, out float sDeg, out float sDx, out float sDz);
                RegionPropInstance inst = ResolveSingle(
                    crateProp, seed, 10 + i, px, py, pz, sDeg, sDx, sDz,
                    null, "ShowcaseRow", 1f, (i * 12f - 22f),
                    crateConstructions[i], crateMaterials[i], crateDamages[i]);
                inst.StencilDecal = crateMaterials[i] == "painted_stencil_crate";
                inst.ClusterIndex = 1;
                inst.MemberIndex = i;
                if (inst.NeedsPalletBase && catalog.TryGetProp("cnt_supply_pallet", out RegionPropDefinition palletProp))
                {
                    RegionPropInstance pallet = ResolveSingle(palletProp, seed, 500 + i, px, py, pz,
                        Math.Min(sDeg, 6f), sDx, sDz, null, "PalletBase", 1f, inst.YawDeg);
                    slice.Instances.Add(pallet);
                    inst.Y += 0.15f;
                }
                slice.Instances.Add(inst);
            }

            // 木箱の StackedGrid クラスタ（2段積み + 崩壊片）を少し奥に置く
            float stackX = crateX + 0.6f;
            float stackZ = crateZ - 2.6f;
            float stackY = scene.HeightAt(stackX, stackZ);
            SampleSlope(scene, stackX, stackZ, out float stackSlope, out float stackDx, out float stackDz);
            slice.Instances.AddRange(PlaceCluster(
                catalog, crateProp, seed, 2, stackX, stackY, stackZ,
                stackSlope, stackDx, stackDz, scene.HeightAt, minimumStackCrates: 2));

            // 木樽（立置き＋横倒し楔）と道標（木箱列と重ならないよう左右に離す）
            if (catalog.TryGetProp("cnt_wood_barrel", out RegionPropDefinition barrelProp))
            {
                float bx = crateX - 4.8f;
                float bz = crateZ - 1.2f;
                SampleSlope(scene, bx, bz, out float bSlope, out float bDx, out float bDz);
                slice.Instances.AddRange(PlaceCluster(
                    catalog, barrelProp, seed, 3, bx, scene.HeightAt(bx, bz), bz,
                    bSlope, bDx, bDz, scene.HeightAt));
            }
            if (catalog.TryGetProp("set_trail_signpost", out RegionPropDefinition signProp))
            {
                float sx = crateX + 4.4f;
                float sz = crateZ - 1.0f;
                SampleSlope(scene, sx, sz, out float sSlope, out float sDx, out float sDz);
                RegionPropInstance sign = ResolveSingle(signProp, seed, 4, sx, scene.HeightAt(sx, sz), sz,
                    sSlope, sDx, sDz, null, "ShowcaseSign", 1f, 25f);
                slice.Instances.Add(sign);
            }

            // 2. 瓦礫の方向性広がり・層状岩・遺跡柱区画: 崖上ではなく、Near 前方の岩屑斜面 (talus) 上を探す
            FindTalusSpot(scene, scene.CenterX - 109f, scene.CenterZ - 95f, 10f, crateGroundY + 24f,
                out float rubX, out float rubZ, out float rubSlope, out float rubDx, out float rubDz);
            slice.RubbleCenterX = rubX;
            slice.RubbleCenterZ = rubZ;
            slice.RubbleFlowDirX = rubDx;
            slice.RubbleFlowDirZ = rubDz;

            RegionPropDefinition rubbleProp = catalog.GetProp("rub_stone_rubble_pile");
            slice.Instances.AddRange(PlaceCluster(
                catalog, rubbleProp, seed, 10, rubX, scene.HeightAt(rubX, rubZ), rubZ,
                rubSlope, rubDx, rubDz, scene.HeightAt, rubDx, rubDz, "sandstone_rubble"));

            if (catalog.TryGetProp("nat_stratified_slab", out RegionPropDefinition slabProp))
            {
                float nx = rubX - 5.0f;
                float nz = rubZ - 1.5f;
                SampleSlope(scene, nx, nz, out float nSlope, out float nDx, out float nDz);
                slice.Instances.AddRange(PlaceCluster(
                    catalog, slabProp, seed, 11, nx, scene.HeightAt(nx, nz), nz,
                    nSlope, nDx, nDz, scene.HeightAt));
            }

            if (catalog.TryGetProp("str_ruin_pillar", out RegionPropDefinition pillarProp))
            {
                float px = rubX + 5.2f;
                float pz = rubZ + 0.5f;
                SampleSlope(scene, px, pz, out float pSlope, out float pDx, out float pDz);
                slice.Instances.AddRange(PlaceCluster(
                    catalog, pillarProp, seed, 12, px, scene.HeightAt(px, pz), pz,
                    pSlope, pDx, pDz, scene.HeightAt, pDx, pDz, "sandstone_carved"));
            }

            // 比較撮影用の専用近接視点（木箱ショーケース / 瓦礫3層広がり）
            float crateRelE = crateX - scene.CenterX;
            float crateRelS = crateZ - scene.CenterZ;
            slice.CratesViewpoint = new RegionLookViewpoint(
                "props-crates",
                crateRelE - 0.8f, crateRelS + 6.4f, 3.3f,
                crateRelE + 0.1f, crateRelS - 0.8f, 0.7f,
                48f);

            float rubRelE = rubX - scene.CenterX;
            float rubRelS = rubZ - scene.CenterZ;
            // 瓦礫の方向性広がりを斜め前方から見下ろす（崖壁にめり込まないよう斜面下側へ置く）
            float perpX = -rubDz;
            float perpZ = rubDx;
            slice.RubbleViewpoint = new RegionLookViewpoint(
                "props-rubble",
                rubRelE + rubDx * 7.5f + perpX * 5.2f, rubRelS + rubDz * 7.5f + perpZ * 5.2f, 4.6f,
                rubRelE + rubDx * 1.4f, rubRelS + rubDz * 1.4f, 0.5f,
                52f);

            return slice;
        }

        private static void FindGentleSpot(RegionLookScene scene, float startX, float startZ, float radius,
            out float bestX, out float bestZ, out float bestSlope, out float bestDx, out float bestDz)
        {
            bestX = startX;
            bestZ = startZ;
            SampleSlope(scene, startX, startZ, out bestSlope, out bestDx, out bestDz);
            for (int dz = -2; dz <= 2; dz++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    float cx = startX + dx * (radius * 0.4f);
                    float cz = startZ + dz * (radius * 0.4f);
                    SampleSlope(scene, cx, cz, out float slope, out float sx, out float sz);
                    if (slope < bestSlope)
                    {
                        bestSlope = slope;
                        bestX = cx;
                        bestZ = cz;
                        bestDx = sx;
                        bestDz = sz;
                    }
                }
            }
        }

        private static void FindTalusSpot(RegionLookScene scene, float startX, float startZ, float radius, float maxHeight,
            out float bestX, out float bestZ, out float bestSlope, out float bestDx, out float bestDz)
        {
            bestX = startX;
            bestZ = startZ;
            SampleSlope(scene, startX, startZ, out bestSlope, out bestDx, out bestDz);
            float bestScore = ScoreTalus(scene, startX, startZ, bestSlope, maxHeight);
            for (int dz = -3; dz <= 3; dz++)
            {
                for (int dx = -3; dx <= 3; dx++)
                {
                    float cx = startX + dx * (radius / 3f);
                    float cz = startZ + dz * (radius / 3f);
                    SampleSlope(scene, cx, cz, out float slope, out float sx, out float sz);
                    float score = ScoreTalus(scene, cx, cz, slope, maxHeight);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestSlope = slope;
                        bestX = cx;
                        bestZ = cz;
                        bestDx = sx;
                        bestDz = sz;
                    }
                }
            }
        }

        private static float ScoreTalus(RegionLookScene scene, float x, float z, float slopeDeg, float maxHeight)
        {
            float h = scene.HeightAt(x, z);
            if (h > maxHeight || slopeDeg > 28f) return -1000f - h;
            return 30f - Math.Abs(slopeDeg - 16f);
        }

        public static void SampleSlope(RegionLookScene scene, float x, float z, out float slopeDeg, out float downX, out float downZ)
        {
            const float step = 1.5f;
            float hx0 = scene.HeightAt(x - step, z);
            float hx1 = scene.HeightAt(x + step, z);
            float hz0 = scene.HeightAt(x, z - step);
            float hz1 = scene.HeightAt(x, z + step);
            float gx = (hx1 - hx0) / (2f * step);
            float gz = (hz1 - hz0) / (2f * step);
            float grad = (float)Math.Sqrt(gx * gx + gz * gz);
            slopeDeg = (float)(Math.Atan(grad) * (180.0 / Math.PI));
            if (grad > 1e-4f)
            {
                downX = -gx / grad;
                downZ = -gz / grad;
            }
            else
            {
                downX = 0f;
                downZ = 1f;
            }
        }

        private static float ResolveYaw(RegionPropTransformRules v1, float dirX, float dirZ, float h0, float h1)
        {
            if (v1.AlignToPrevailingWind)
                return NormalizeAngle(Lerp(v1.YawMinDeg, v1.YawMaxDeg, h0) + 25f);
            if (v1.SouthFacingBias)
                return NormalizeAngle(180f + Lerp(v1.YawMinDeg, v1.YawMaxDeg, h0));
            if (v1.YawStepDeg >= 1f)
            {
                int steps = Math.Max(1, (int)Math.Round(360f / v1.YawStepDeg));
                int pick = Math.Min(steps - 1, (int)(h0 * steps));
                float jitter = (h1 * 2f - 1f) * v1.YawJitterDeg;
                return NormalizeAngle(pick * v1.YawStepDeg + jitter);
            }
            if ((v1.GridSnapBias > 0f && h1 < v1.GridSnapBias) || (v1.Step90Bias > 0f && h1 < v1.Step90Bias))
            {
                int q = Math.Min(3, (int)(h0 * 4f));
                float jitter = (Hash01(q, 99, (uint)(h1 * 100000f)) * 2f - 1f) * 4f;
                return NormalizeAngle(q * 90f + jitter);
            }
            return NormalizeAngle(Lerp(v1.YawMinDeg, v1.YawMaxDeg, h0));
        }

        /// <summary>
        /// 斜面に沿う傾き。pitch・roll は RegionPropView が Unity で Euler(pitch, yaw, -roll) として掛ける値で、
        /// 物の前（ローカル +Z）と右（+X）は Core の座標（X 東・Z 南）で前 = (sin yaw, -cos yaw)、右 = (cos yaw, sin yaw)。
        /// 下り（slopeDir）へ向いた側が下がる向きに傾ける（2026-11-27 に符号を直した。以前は Z の成分で前、X の成分で右が逆で、下り側が浮いた）。
        /// </summary>
        public static void ResolveTilt(
            RegionPropDefinition prop,
            RegionPropTransformRules v1,
            float slopeDeg,
            float slopeDirX,
            float slopeDirZ,
            float yawDeg,
            float tiltLimitDeg,
            float hPitch,
            float hRoll,
            float hFlat,
            out float pitchDeg,
            out float rollDeg)
        {
            float yawRad = yawDeg * (float)(Math.PI / 180.0);
            float fwdX = (float)Math.Sin(yawRad);
            float fwdZ = -(float)Math.Cos(yawRad);
            float rightX = (float)Math.Cos(yawRad);
            float rightZ = (float)Math.Sin(yawRad);

            float slopeAlign = Math.Min(slopeDeg, tiltLimitDeg);
            if (prop.PivotRule == RegionPropPivotRule.StackableUpright || prop.PivotRule == RegionPropPivotRule.ModularSnap)
            {
                // 人工物は直立優先: 斜面追従を弱め、ランダム傾きは極小に抑える
                float follow = slopeAlign * 0.55f;
                pitchDeg = (slopeDirX * fwdX + slopeDirZ * fwdZ) * follow + (hPitch * 2f - 1f) * Math.Min(1.5f, tiltLimitDeg * 0.3f);
                rollDeg = (slopeDirX * rightX + slopeDirZ * rightZ) * follow + (hRoll * 2f - 1f) * Math.Min(1.5f, tiltLimitDeg * 0.3f);
            }
            else
            {
                float flatFactor = (v1.FlatFaceDownBias > 0f && hFlat < v1.FlatFaceDownBias) ? 0.35f : 1f;
                float follow = slopeAlign * 0.7f;
                pitchDeg = (slopeDirX * fwdX + slopeDirZ * fwdZ) * follow + (hPitch * 2f - 1f) * (tiltLimitDeg * 0.55f * flatFactor);
                rollDeg = (slopeDirX * rightX + slopeDirZ * rightZ) * follow + (hRoll * 2f - 1f) * (tiltLimitDeg * 0.55f * flatFactor);
            }

            pitchDeg = Clamp(pitchDeg, -tiltLimitDeg, tiltLimitDeg);
            rollDeg = Clamp(rollDeg, -tiltLimitDeg, tiltLimitDeg);
        }

        private static float ResolveEmbedDepth(RegionPropDefinition prop, float scaledHeight, float h)
        {
            RegionPropSpawningRules sr = prop.SpawningRules;
            if (sr.EmbedDepthMaxRatio > 0f)
                return scaledHeight * Lerp(sr.EmbedDepthMinRatio, sr.EmbedDepthMaxRatio, h);
            if (prop.PivotRule == RegionPropPivotRule.NaturalEmbed)
                return scaledHeight * Lerp(0.15f, 0.30f, h);
            if (prop.PivotRule == RegionPropPivotRule.RubbleScatter)
                return Math.Min(0.16f, scaledHeight * 0.15f);
            if (prop.PivotRule == RegionPropPivotRule.ModularSnap)
                return sr.FoundationSkirtM > 0f ? sr.FoundationSkirtM * 0.4f : 0.03f;
            return Lerp(0f, 0.025f, h);
        }

        private static string PickConstruction(RegionPropDefinition prop, string damage, float h)
        {
            RegionPropConstructionRules v3 = prop.VariationRules.V3_Construction;
            if (v3 == null) return "";
            if (v3.AssemblyTypes.Length > 0) return PickArray(v3.AssemblyTypes, h, "");
            if (v3.Posture.Length > 0)
                return v3.PostureWeights.Length == v3.Posture.Length
                    ? PickWeighted(v3.Posture, v3.PostureWeights, h)
                    : PickArray(v3.Posture, h, "");
            if (v3.ShaftStyle.Length > 0) return PickArray(v3.ShaftStyle, h, "");
            if (v3.TopProfile.Length > 0) return PickArray(v3.TopProfile, h, "");
            if (v3.BoardCount.Length >= 2)
                return "boards_" + PickIntRange(v3.BoardCount[0], v3.BoardCount[v3.BoardCount.Length - 1], h);
            if (v3.DoorState.Length > 0) return PickArray(v3.DoorState, h, "");
            if (v3.Mount.Length > 0) return PickArray(v3.Mount, h, "");
            if (v3.CookingRig.Length > 0) return PickArray(v3.CookingRig, h, "");
            if (v3.CarvingPattern.Length > 0) return PickArray(v3.CarvingPattern, h, "");
            if (v3.SeamGrooves.Length > 0) return PickArray(v3.SeamGrooves, h, "");
            return "";
        }

        private static string ResolveMeshVariant(RegionPropDefinition prop, string role, string damage, string construction, float h)
        {
            if (prop.Id == "cnt_wood_crate")
            {
                switch (construction)
                {
                    case "slat_open": return "slat_open_1m";
                    case "framed_reinforced": return "framed_xbrace_1m";
                    case "lid_ajar": return "lid_open_1m";
                    case "long_box": return "long_crate_15x05m";
                    default: return h < 0.2f ? "half_crate_05m" : "plank_closed_1m";
                }
            }
            if (prop.Id == "rub_stone_rubble_pile")
            {
                if (role == "MidFragments") return "mid_chunk_3pack";
                if (role == "MicroScatter") return "micro_scatter_patch";
                return h < 0.5f ? "core_heap_low" : "core_heap_tall";
            }
            if (prop.Id == "str_ruin_pillar")
            {
                switch (damage)
                {
                    case "intact": return "intact_column";
                    case "broken_mid": return "broken_mid_diagonal";
                    case "stump": return "stump_base_only";
                    case "fallen_drum": return "fallen_drum_segment";
                }
            }
            if (prop.Id == "cnt_wood_barrel")
            {
                switch (construction)
                {
                    case "upright_open": return "upright_open_filled";
                    case "tipped_sideways": return "tipped_on_side_chocked";
                    default: return damage == "broken_stave" ? "stave_broken" : "upright_closed";
                }
            }
            return PickArray(prop.MeshVariants, h, prop.Id);
        }

        private static string PickWeighted(string[] items, float[] weights, float h)
        {
            if (items == null || items.Length == 0) return "";
            if (weights == null || weights.Length != items.Length)
                return PickArray(items, h, items[0]);
            float sum = 0f;
            for (int i = 0; i < weights.Length; i++) sum += Math.Max(0f, weights[i]);
            if (sum <= 0f) return items[0];
            float target = h * sum;
            float acc = 0f;
            for (int i = 0; i < items.Length; i++)
            {
                acc += Math.Max(0f, weights[i]);
                if (target <= acc) return items[i];
            }
            return items[items.Length - 1];
        }

        private static string PickArray(string[] items, float h, string fallback)
        {
            if (items == null || items.Length == 0) return fallback;
            int idx = Math.Min(items.Length - 1, Math.Max(0, (int)(h * items.Length)));
            return items[idx];
        }

        private static int PickIntRange(int min, int max, float h)
        {
            if (max <= min) return min;
            int val = min + (int)(h * (max - min + 1));
            return val > max ? max : val;
        }

        private static float Hash01(int x, int y, uint seed) => RegionNoise.Hash01(x, y, seed);

        private static uint HashString(string s)
        {
            unchecked
            {
                uint h = 2166136261u;
                if (s != null)
                {
                    for (int i = 0; i < s.Length; i++)
                    {
                        h ^= s[i];
                        h *= 16777619u;
                    }
                }
                return h;
            }
        }

        private static void Normalize2(ref float x, ref float z)
        {
            float len = (float)Math.Sqrt(x * x + z * z);
            if (len > 1e-5f)
            {
                x /= len;
                z /= len;
            }
            else
            {
                x = 0f;
                z = 1f;
            }
        }

        private static float NormalizeAngle(float deg)
        {
            deg %= 360f;
            if (deg < 0f) deg += 360f;
            return deg;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * Clamp(t, 0f, 1f);

        private static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
    }
}

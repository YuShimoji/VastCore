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
    /// 1 クラスタ分の配置計画と V6 境界情報（Gizmo 可視化・検証用）。
    /// </summary>
    public sealed class RegionPropClusterPlan
    {
        public string PropId = "";
        public RegionPropClusterPattern Pattern = RegionPropClusterPattern.Single;
        public float AnchorX;
        public float AnchorY;
        public float AnchorZ;
        public float BoundaryRadiusM;
        public float FlowDirX;
        public float FlowDirZ = 1f;
        public float ConeAngleDeg = 70f;
        public bool Accepted = true;
        public string SkipReason = "";
        public readonly List<RegionPropInstance> Instances = new List<RegionPropInstance>();
    }

    /// <summary>
    /// 3D 砂岩シーン上の DS-011 P0 Proof Slice 配置結果と、比較撮影用の視点情報。
    /// </summary>
    public sealed class RegionPropProofSlice
    {
        public readonly List<RegionPropInstance> Instances = new List<RegionPropInstance>();
        public readonly List<RegionPropClusterPlan> Clusters = new List<RegionPropClusterPlan>();
        public RegionLookViewpoint CratesViewpoint;
        public RegionLookViewpoint RubbleViewpoint;
        public RegionLookViewpoint P0ShowcaseViewpoint;
        public float CratesCenterX, CratesCenterZ;
        public float RubbleCenterX, RubbleCenterZ;
        public float RubbleFlowDirX, RubbleFlowDirZ;
    }

    /// <summary>
    /// RegionData / RegionSediment / RegionRocks を読み取り専用で参照し、
    /// 水域・傾斜・既存岩との非重複境界および土砂流向を評価するコンテキスト。
    /// </summary>
    public sealed class RegionPropFieldContext
    {
        private readonly RegionLookScene _scene;
        private readonly RegionData _data;
        private readonly RegionRockField _rocks;
        private readonly float _originX;
        private readonly float _originZ;
        private readonly float _cell;

        private RegionPropFieldContext(RegionLookScene scene, RegionData data, RegionRockField rocks, float originX, float originZ, float cell)
        {
            _scene = scene;
            _data = data;
            _rocks = rocks;
            _originX = originX;
            _originZ = originZ;
            _cell = cell > 0f ? cell : 1f;
        }

        public static RegionPropFieldContext FromScene(RegionLookScene scene)
        {
            if (scene == null) throw new ArgumentNullException("scene");
            RegionData innerData = scene.Inner != null ? scene.Inner.Data : null;
            return new RegionPropFieldContext(
                scene,
                innerData,
                scene.InnerRocks,
                scene.InnerMinX,
                scene.InnerMinZ,
                scene.InnerCell);
        }

        public static RegionPropFieldContext FromDetail(RegionDetail detail, RegionRockField rocks = null, float originX = 0f, float originZ = 0f)
        {
            if (detail == null || detail.Data == null) throw new ArgumentNullException("detail");
            return new RegionPropFieldContext(null, detail.Data, rocks, originX, originZ, detail.Data.CellMeters);
        }

        public static RegionPropFieldContext FromData(RegionData data, RegionRockField rocks = null, float originX = 0f, float originZ = 0f)
        {
            if (data == null) throw new ArgumentNullException("data");
            return new RegionPropFieldContext(null, data, rocks, originX, originZ, data.CellMeters);
        }

        public float HeightAt(float x, float z)
        {
            if (_scene != null) return _scene.HeightAt(x, z);
            float h = SampleArray(_data.Height, _data.Resolution, (x - _originX) / _cell, (z - _originZ) / _cell);
            return h > 0f ? h : 0f;
        }

        public void SampleSlope(float x, float z, out float slopeDeg, out float downX, out float downZ)
        {
            float step = Math.Max(1.0f, _cell);
            float hx0 = HeightAt(x - step, z);
            float hx1 = HeightAt(x + step, z);
            float hz0 = HeightAt(x, z - step);
            float hz1 = HeightAt(x, z + step);
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

        public void SampleSurfaceMetrics(
            float x,
            float z,
            out float deposit,
            out float scree,
            out float wear,
            out float sedimentFlow,
            out float coarse,
            out float talus,
            out float caprock,
            out float waterCoverage)
        {
            deposit = 0f;
            scree = 0f;
            wear = 0f;
            sedimentFlow = 0f;
            coarse = 0f;
            talus = 0f;
            caprock = 0f;
            waterCoverage = 0f;
            if (_data == null) return;

            int n = _data.Resolution;
            float fx = (x - _originX) / _cell;
            float fz = (z - _originZ) / _cell;
            if (fx < 0f || fz < 0f || fx > n - 1 || fz > n - 1) return;

            if (_data.Deposit != null) deposit = SampleArray(_data.Deposit, n, fx, fz);
            if (_data.Scree != null) scree = SampleArray(_data.Scree, n, fx, fz);
            if (_data.Wear != null) wear = SampleArray(_data.Wear, n, fx, fz);
            if (_data.SedimentFlow != null) sedimentFlow = SampleArray(_data.SedimentFlow, n, fx, fz);
            if (_data.Coarse != null) coarse = SampleArray(_data.Coarse, n, fx, fz);
            if (_data.Talus != null) talus = SampleArray(_data.Talus, n, fx, fz);
            if (_data.Caprock != null)
            {
                int ix = Math.Min(n - 1, Math.Max(0, (int)Math.Round(fx)));
                int iz = Math.Min(n - 1, Math.Max(0, (int)Math.Round(fz)));
                caprock = _data.Caprock[ix + iz * n];
            }
            if (_data.WaterCoverage != null) waterCoverage = SampleArray(_data.WaterCoverage, n, fx, fz);
            if (_data.LakeDepth != null)
            {
                float lake = SampleArray(_data.LakeDepth, n, fx, fz);
                if (lake > 0.01f) waterCoverage = Math.Max(waterCoverage, 1f);
            }
            if (HeightAt(x, z) <= 0.01f) waterCoverage = Math.Max(waterCoverage, 1f);
        }

        /// <summary>
        /// RegionSediment の Scree/Deposit/SedimentFlow 勾配から流向ベクトルを返す。土砂場が無い場合は地形勾配へフォールバックする。
        /// </summary>
        public bool TrySampleSedimentFlowDir(float x, float z, out float dirX, out float dirZ)
        {
            SampleSlope(x, z, out _, out float slopeDx, out float slopeDz);
            dirX = slopeDx;
            dirZ = slopeDz;
            if (_data == null || (_data.Scree == null && _data.Deposit == null && _data.SedimentFlow == null))
                return false;

            int n = _data.Resolution;
            float fx = (x - _originX) / _cell;
            float fz = (z - _originZ) / _cell;
            if (fx < 1f || fz < 1f || fx > n - 2 || fz > n - 2)
                return false;

            float[] field = _data.Scree ?? _data.Deposit ?? _data.SedimentFlow;
            float sx0 = SampleArray(field, n, fx - 1f, fz);
            float sx1 = SampleArray(field, n, fx + 1f, fz);
            float sz0 = SampleArray(field, n, fx, fz - 1f);
            float sz1 = SampleArray(field, n, fx, fz + 1f);
            // Scree 増加方向ではなく、地形の下り方向へ向けた堆積流出軸を優先し、直交バイアスとして土砂勾配を加味する
            float gx = (sx1 - sx0) * 0.5f;
            float gz = (sz1 - sz0) * 0.5f;
            float lenSq = gx * gx + gz * gz;
            if (lenSq > 1e-5f)
            {
                // 土砂が増えている下流（または地形と揃った向き）へ合わせる
                float dot = gx * slopeDx + gz * slopeDz;
                float sign = dot >= 0f ? 1f : -1f;
                dirX = slopeDx * 0.7f + sign * gx * 0.3f;
                dirZ = slopeDz * 0.7f + sign * gz * 0.3f;
                float norm = (float)Math.Sqrt(dirX * dirX + dirZ * dirZ);
                if (norm > 1e-5f)
                {
                    dirX /= norm;
                    dirZ /= norm;
                }
            }
            return true;
        }

        public bool OverlapsExistingRock(float x, float z, float propRadiusM, float clearanceM = 0.25f)
        {
            if (_rocks == null || _rocks.Rocks == null) return false;
            RegionRock[] rocks = _rocks.Rocks;
            for (int i = 0; i < rocks.Length; i++)
            {
                RegionRock rock = rocks[i];
                if (rock.Size < 0.45f) continue;
                float rockRadius = 0.48f * rock.Size;
                float minDist = rockRadius + propRadiusM + clearanceM;
                float dx = x - rock.X;
                float dz = z - rock.Z;
                if (dx * dx + dz * dz < minDist * minDist)
                    return true;
            }
            return false;
        }

        private static float SampleArray(float[] values, int n, float x, float y)
        {
            if (values == null || n < 2) return 0f;
            int x0 = (int)x, y0 = (int)y;
            if (x0 < 0) x0 = 0; else if (x0 > n - 2) x0 = n - 2;
            if (y0 < 0) y0 = 0; else if (y0 > n - 2) y0 = n - 2;
            float fx = x - x0, fy = y - y0;
            if (fx < 0f) fx = 0f; else if (fx > 1f) fx = 1f;
            if (fy < 0f) fy = 0f; else if (fy > 1f) fy = 1f;
            int i = x0 + y0 * n;
            float top = values[i] + (values[i + 1] - values[i]) * fx;
            float bottom = values[i + n] + (values[i + n + 1] - values[i + n]) * fx;
            return top + (bottom - top) * fy;
        }
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
            string damageOverride = null)
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

            // V7: 環境順応
            RegionPropOvergrowthRules v7 = vr.V7_BiomeOvergrowth;
            float topDust = Lerp(v7.TopDustMin, v7.TopDustMax, Hash01(index, 19, s));
            float mossMask = Lerp(v7.MossMaskMin, v7.MossMaskMax, Hash01(index, 20, s));

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
            string parentMaterial = null)
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
                int baseCount = maxLayers > 1 && total >= 3 ? (total + 1) / 2 : total;
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
                for (int top = 0; top < remaining && maxLayers >= 2; top++)
                {
                    RegionPropInstance support = null;
                    for (int k = 0; k < baseCrates.Count; k++)
                    {
                        RegionPropInstance cand = baseCrates[(top + k) % baseCrates.Count];
                        if (cand.DamageState == "collapsed_wreck" || cand.ConstructionVariant == "lid_ajar" || cand.ConstructionVariant == "long_box")
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
                        topScale, topYaw, support.ConstructionVariant == "long_box" ? "plank_closed" : null);
                    // 上段は傾かない・蓋開き/崩壊にしない
                    if (stacked.DamageState == "collapsed_wreck") stacked.DamageState = "worn_chipped";
                    if (stacked.ConstructionVariant == "lid_ajar") stacked.ConstructionVariant = "plank_closed";
                    stacked.PitchDeg = support.PitchDeg * 0.5f;
                    stacked.RollDeg = support.RollDeg * 0.5f;
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
            var slice = new RegionPropProofSlice();
            var field = RegionPropFieldContext.FromScene(scene);

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
                stackSlope, stackDx, stackDz, scene.HeightAt));

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
            field.TrySampleSedimentFlowDir(rubX, rubZ, out float rubFlowX, out float rubFlowZ);
            slice.RubbleFlowDirX = rubFlowX;
            slice.RubbleFlowDirZ = rubFlowZ;

            RegionPropDefinition rubbleProp = catalog.GetProp("rub_stone_rubble_pile");
            slice.Instances.AddRange(PlaceCluster(
                catalog, rubbleProp, seed, 10, rubX, scene.HeightAt(rubX, rubZ), rubZ,
                rubSlope, rubDx, rubDz, scene.HeightAt, rubFlowX, rubFlowZ, "sandstone_rubble"));

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

            if (scene.Spec != null && scene.Spec.PropsShowAllP0)
            {
                AppendRemainingP0Showcase(catalog, scene, field, seed, crateX, crateZ, rubX, rubZ, slice);
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
            float perpX = -rubFlowZ;
            float perpZ = rubFlowX;
            slice.RubbleViewpoint = new RegionLookViewpoint(
                "props-rubble",
                rubRelE + rubFlowX * 7.5f + perpX * 5.2f, rubRelS + rubFlowZ * 7.5f + perpZ * 5.2f, 4.6f,
                rubRelE + rubFlowX * 1.4f, rubRelS + rubFlowZ * 1.4f, 0.5f,
                52f);

            slice.P0ShowcaseViewpoint = new RegionLookViewpoint(
                "props-p0-all",
                crateRelE - 6.0f, crateRelS + 14.0f, 8.5f,
                (crateRelE + rubRelE) * 0.5f, (crateRelS + rubRelS) * 0.5f, 1.5f,
                55f);

            return slice;
        }

        private static void AppendRemainingP0Showcase(
            RegionPropCatalog catalog,
            RegionLookScene scene,
            RegionPropFieldContext field,
            uint seed,
            float crateX,
            float crateZ,
            float rubX,
            float rubZ,
            RegionPropProofSlice slice)
        {
            var placedIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < slice.Instances.Count; i++)
                placedIds.Add(slice.Instances[i].PropId);

            List<RegionPropDefinition> p0Props = catalog.Filter(RegionPropPriority.P0);
            int idx = 20;
            for (int i = 0; i < p0Props.Count; i++)
            {
                RegionPropDefinition prop = p0Props[i];
                if (placedIds.Contains(prop.Id)) continue;

                bool isNaturalOrRubble = prop.Domain == "NAT" || prop.Domain == "RUB" || prop.Id == "exp_monolith_fragment";
                float baseX = isNaturalOrRubble ? rubX : crateX;
                float baseZ = isNaturalOrRubble ? rubZ : crateZ;
                float angle = (idx - 20) * 0.85f;
                float ring = 5.5f + ((idx - 20) / 6) * 4.5f;
                float candX = baseX + (float)Math.Cos(angle) * ring;
                float candZ = baseZ + (float)Math.Sin(angle) * ring;

                if (RegionPropPlacementPlanner.TryPlanCluster(
                    catalog, prop, field, seed, idx, candX, candZ,
                    out RegionPropClusterPlan plan, slice.Instances, searchRadiusM: 22f))
                {
                    slice.Clusters.Add(plan);
                    slice.Instances.AddRange(plan.Instances);
                    placedIds.Add(prop.Id);
                }
                idx++;
            }
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

        private static void ResolveTilt(
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
            float fwdZ = (float)Math.Cos(yawRad);
            float rightX = (float)Math.Cos(yawRad);
            float rightZ = -(float)Math.Sin(yawRad);

            float slopeAlign = Math.Min(slopeDeg, tiltLimitDeg);
            if (prop.PivotRule == RegionPropPivotRule.StackableUpright || prop.PivotRule == RegionPropPivotRule.ModularSnap)
            {
                // 人工物は直立優先: 斜面追従を弱め、ランダム傾きは極小に抑える
                float follow = slopeAlign * 0.55f;
                pitchDeg = (slopeDirX * fwdX + slopeDirZ * fwdZ) * follow + (hPitch * 2f - 1f) * Math.Min(1.5f, tiltLimitDeg * 0.3f);
                rollDeg = -(slopeDirX * rightX + slopeDirZ * rightZ) * follow + (hRoll * 2f - 1f) * Math.Min(1.5f, tiltLimitDeg * 0.3f);
            }
            else
            {
                float flatFactor = (v1.FlatFaceDownBias > 0f && hFlat < v1.FlatFaceDownBias) ? 0.35f : 1f;
                float follow = slopeAlign * 0.7f;
                pitchDeg = (slopeDirX * fwdX + slopeDirZ * fwdZ) * follow + (hPitch * 2f - 1f) * (tiltLimitDeg * 0.55f * flatFactor);
                rollDeg = -(slopeDirX * rightX + slopeDirZ * rightZ) * follow + (hRoll * 2f - 1f) * (tiltLimitDeg * 0.55f * flatFactor);
            }

            pitchDeg = Clamp(pitchDeg, -tiltLimitDeg, tiltLimitDeg);
            rollDeg = Clamp(rollDeg, -tiltLimitDeg, tiltLimitDeg);
            float magSq = pitchDeg * pitchDeg + rollDeg * rollDeg;
            if (magSq > tiltLimitDeg * tiltLimitDeg && magSq > 1e-6f)
            {
                float scale = tiltLimitDeg / (float)Math.Sqrt(magSq);
                pitchDeg *= scale;
                rollDeg *= scale;
            }
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

    /// <summary>
    /// DS-011 の SpawningRules（最大傾斜）・水域除外・RegionRocks 非重複・RegionSediment 流向接続を適用して
    /// 決定論的な配置計画を構築する。
    /// </summary>
    public static class RegionPropPlacementPlanner
    {
        public static bool CanSpawnAtSlope(RegionPropDefinition prop, float slopeDeg)
        {
            if (prop == null) throw new ArgumentNullException("prop");
            return slopeDeg <= prop.SpawningRules.MaxSlopeDeg;
        }

        public static bool TryPlanCluster(
            RegionPropCatalog catalog,
            RegionPropDefinition prop,
            RegionPropFieldContext field,
            uint seed,
            int anchorIndex,
            float targetX,
            float targetZ,
            out RegionPropClusterPlan plan,
            IList<RegionPropInstance> existingInstances = null,
            bool enforceMaxSlope = true,
            float searchRadiusM = 0f,
            float? flowDirX = null,
            float? flowDirZ = null,
            string parentMaterial = null)
        {
            if (prop == null) throw new ArgumentNullException("prop");
            if (field == null) throw new ArgumentNullException("field");

            float anchorRadius = 0.45f * Math.Max(prop.DimensionsM.X, prop.DimensionsM.Z);
            plan = new RegionPropClusterPlan
            {
                PropId = prop.Id,
                Pattern = prop.VariationRules.V6_ClusterSpread.Pattern,
                ConeAngleDeg = prop.VariationRules.V6_ClusterSpread.ConeAngleDeg,
                Accepted = false
            };

            float bestX = targetX;
            float bestZ = targetZ;
            float bestSlope = 0f;
            float bestDx = 0f;
            float bestDz = 1f;
            bool found = false;
            string firstFailReason = "";

            int candidates = searchRadiusM > 0.1f ? 25 : 1;
            for (int c = 0; c < candidates; c++)
            {
                float cx = targetX;
                float cz = targetZ;
                if (c > 0)
                {
                    float ring = (float)c / (candidates - 1);
                    float r = searchRadiusM * (0.2f + 0.8f * ring);
                    float ang = c * 2.3999632f; // golden angle
                    cx = targetX + (float)Math.Cos(ang) * r;
                    cz = targetZ + (float)Math.Sin(ang) * r;
                }

                field.SampleSlope(cx, cz, out float slopeDeg, out float dx, out float dz);
                field.SampleSurfaceMetrics(cx, cz, out _, out _, out _, out _, out _, out _, out _, out float water);

                string reason = null;
                if (water > 0.01f) reason = "WaterBlocked";
                else if (enforceMaxSlope && !CanSpawnAtSlope(prop, slopeDeg)) reason = "SlopeExceedsMax";
                else if (field.OverlapsExistingRock(cx, cz, anchorRadius)) reason = "RockOverlap";
                else if (OverlapsInstances(cx, cz, anchorRadius, existingInstances)) reason = "PropOverlap";

                if (reason == null)
                {
                    bestX = cx;
                    bestZ = cz;
                    bestSlope = slopeDeg;
                    bestDx = dx;
                    bestDz = dz;
                    found = true;
                    break;
                }
                if (string.IsNullOrEmpty(firstFailReason))
                    firstFailReason = reason;
            }

            if (!found)
            {
                plan.AnchorX = targetX;
                plan.AnchorY = field.HeightAt(targetX, targetZ);
                plan.AnchorZ = targetZ;
                plan.SkipReason = firstFailReason;
                return false;
            }

            float fx = bestDx;
            float fz = bestDz;
            if (flowDirX.HasValue && flowDirZ.HasValue)
            {
                fx = flowDirX.Value;
                fz = flowDirZ.Value;
            }
            else if (prop.VariationRules.V6_ClusterSpread.Pattern == RegionPropClusterPattern.DirectionalCone)
            {
                field.TrySampleSedimentFlowDir(bestX, bestZ, out fx, out fz);
            }

            float bestY = field.HeightAt(bestX, bestZ);
            List<RegionPropInstance> raw = RegionPropPlacementSolver.PlaceCluster(
                catalog, prop, seed, anchorIndex, bestX, bestY, bestZ,
                bestSlope, bestDx, bestDz, field.HeightAt, fx, fz, parentMaterial);

            float maxDist = anchorRadius;
            for (int i = 0; i < raw.Count; i++)
            {
                RegionPropInstance inst = raw[i];
                bool isAnchor = i == 0 || inst.Role == "Primary" || inst.Role == "BaseLayer" || inst.Role == "PalletBase" || inst.Role == "CorePile";
                if (!isAnchor)
                {
                    field.SampleSurfaceMetrics(inst.X, inst.Z, out _, out _, out _, out _, out _, out _, out _, out float mw);
                    float mr = 0.4f * Math.Max(inst.DimensionsX * inst.ScaleX, inst.DimensionsZ * inst.ScaleZ);
                    if (mw > 0.01f || field.OverlapsExistingRock(inst.X, inst.Z, mr, 0.1f))
                        continue;
                }
                plan.Instances.Add(inst);
                float ddx = inst.X - bestX;
                float ddz = inst.Z - bestZ;
                float dist = (float)Math.Sqrt(ddx * ddx + ddz * ddz) + 0.5f * Math.Max(inst.DimensionsX * inst.ScaleX, inst.DimensionsZ * inst.ScaleZ);
                if (dist > maxDist) maxDist = dist;
            }

            plan.Accepted = plan.Instances.Count > 0;
            plan.AnchorX = bestX;
            plan.AnchorY = bestY;
            plan.AnchorZ = bestZ;
            plan.FlowDirX = fx;
            plan.FlowDirZ = fz;
            plan.BoundaryRadiusM = Math.Max(maxDist, prop.VariationRules.V6_ClusterSpread.RadiusM);
            return plan.Accepted;
        }

        public static RegionPropProofSlice PlanP0Showcase(RegionPropCatalog catalog, RegionLookScene scene, int seedOffset = 0)
        {
            if (catalog == null) throw new ArgumentNullException("catalog");
            if (scene == null) throw new ArgumentNullException("scene");
            bool prev = scene.Spec != null && scene.Spec.PropsShowAllP0;
            if (scene.Spec != null) scene.Spec.PropsShowAllP0 = true;
            try
            {
                return RegionPropPlacementSolver.BuildProofSlice(catalog, scene, seedOffset);
            }
            finally
            {
                if (scene.Spec != null) scene.Spec.PropsShowAllP0 = prev;
            }
        }

        private static bool OverlapsInstances(float x, float z, float radiusM, IList<RegionPropInstance> existing)
        {
            if (existing == null || existing.Count == 0) return false;
            for (int i = 0; i < existing.Count; i++)
            {
                RegionPropInstance e = existing[i];
                if (e.StackLayer > 0) continue;
                float er = 0.45f * Math.Max(e.DimensionsX * e.ScaleX, e.DimensionsZ * e.ScaleZ);
                float minDist = radiusM + er + 0.25f;
                float dx = x - e.X;
                float dz = z - e.Z;
                if (dx * dx + dz * dz < minDist * minDist)
                    return true;
            }
            return false;
        }
    }
}

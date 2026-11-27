using System;
using System.Collections.Generic;
using System.Linq;
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
            /// <summary>実アセットで置いた組（slice.UseRealAssets で読み出し口がある時だけ。モデルの無い Props はプレースホルダのまま入る）。</summary>
            public GameObject RealRoot;
            public int RealModelCount;
            public int RealTriangles;
            public string RealSummary = "";
            public readonly List<string> ContactMeasurements = new List<string>();
        }

        private sealed class Palette
        {
            public Shader Shader;
            public readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>(StringComparer.Ordinal);
        }

        public const string PropShaderName = "Vastcore/RegionProp";

        /// <summary>
        /// Props を置く。heightAt（Core 座標の地面の高さ）を渡すと、実アセットの地面なじませ（V7 Ground Skirt）が足元の地面の傾きに沿う。
        /// </summary>
        public static Result AddTo(Transform parent, RegionPropProofSlice slice, Func<float, float, float> heightAt = null,
            bool weatherGrassland = false)
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
                PlaceInstance(go.transform, root.transform, inst);
                result.PlacedCount++;
            }

            if (slice.UseRealAssets && RegionPropAssets.Source != null)
            {
                AddRealAssets(parent, slice, palette, heightAt, slice.RenderedHeightAt ?? heightAt, result, weatherGrassland);
                // 同じ場所に2組を重ねないよう、実アセットの組を出してプレースホルダの組を隠す（比較の撮影で入れ替える）
                root.SetActive(false);
            }
            return result;
        }

        private static void PlaceInstance(Transform go, Transform root, RegionPropInstance inst)
        {
            go.SetParent(root, false);
            go.localPosition = new Vector3(inst.X, inst.Y, -inst.Z);
            go.localRotation = Quaternion.Euler(inst.PitchDeg, inst.YawDeg, -inst.RollDeg);
        }

        // ---- 実アセットの差し替え口（DS-011 7節の造形方式の比較用。既定は切） ----

        private sealed class RealContext
        {
            public Shader Shader;
            public IRegionPropAssetSource Source;
            public RegionBiome Biome = RegionBiome.Desert;
            public bool UseGrasslandVariants;
            public bool WeatherGrassland;
            public bool HasGroundColor;
            public Vector4 GroundColor = new Vector4(0.72f, 0.56f, 0.40f, 1f);
            public Vector4 MossColor = new Vector4(0.22f, 0.34f, 0.14f, 1f);
            public readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>(StringComparer.Ordinal);
            public readonly Dictionary<string, IReadOnlyList<RegionPropModel>> Models = new Dictionary<string, IReadOnlyList<RegionPropModel>>(StringComparer.Ordinal);
            public readonly SortedDictionary<string, int> Used = new SortedDictionary<string, int>(StringComparer.Ordinal);
            public MaterialPropertyBlock Block;
        }

        private static void AddRealAssets(Transform parent, RegionPropProofSlice slice, Palette palette,
            Func<float, float, float> heightAt, Func<float, float, float> renderedHeightAt, Result result, bool weatherGrassland)
        {
            var realRoot = new GameObject("DS011_Props_Real");
            realRoot.transform.SetParent(parent, false);
            result.RealRoot = realRoot;
            Shader propShader = Shader.Find(PropShaderName);
            Color moss = RegionPropAssets.MossColorFor(slice.Biome);
            var ctx = new RealContext
            {
                Shader = propShader != null ? propShader : palette.Shader,
                Source = RegionPropAssets.Source,
                Biome = slice.Biome,
                UseGrasslandVariants = slice.UseGrasslandVariants || slice.Biome == RegionBiome.Grassland,
                WeatherGrassland = weatherGrassland && slice.Biome == RegionBiome.Grassland,
                HasGroundColor = slice.HasGroundColor || slice.Biome != RegionBiome.Desert,
                GroundColor = slice.HasGroundColor
                    ? new Vector4(slice.GroundRed, slice.GroundGreen, slice.GroundBlue, 1f)
                    : (Vector4)RegionPropAssets.GroundColorFor(slice.Biome),
                MossColor = new Vector4(moss.r, moss.g, moss.b, 1f),
                Block = new MaterialPropertyBlock()
            };
            float rootY = realRoot.transform.position.y;
            var completeClusters = new HashSet<(string, int)>();
            var crateSupports = new Dictionary<(int, int), Transform>();
            for (int i = 0; i < slice.Instances.Count; i++)
            {
                RegionPropInstance inst = slice.Instances[i];
                if (!ctx.Models.TryGetValue(inst.PropId, out IReadOnlyList<RegionPropModel> models))
                {
                    models = ctx.Source.Models(RegionPropAssets.DomainOf(inst.PropId), inst.PropId) ?? Array.Empty<RegionPropModel>();
                    ctx.Models[inst.PropId] = models;
                }
                RegionPropModel assembly = string.IsNullOrEmpty(inst.ClusterModelVariant) ? null :
                    models.FirstOrDefault(candidate => candidate.Variant == inst.ClusterModelVariant);
                bool covered = RegionPropAssets.CoveredByClusterModel(inst) ||
                    (assembly != null && !completeClusters.Add((inst.PropId, inst.ClusterIndex)));
                RegionPropModel model = assembly ?? SelectPlacementModel(inst, models, ctx.Biome, ctx.UseGrasslandVariants);
                GameObject go;
                if (model == null)
                {
                    go = BuildInstanceObject(inst, palette, i);
                    go.name += "_placeholder";
                }
                else if (covered)
                {
                    // 同じ群れの中片・小片や2つ目の芯は群れ全体のモデルに含まれる。
                    go = new GameObject(string.Format("{0:D2}_{1}_{2}_inModel", i, inst.PropId, inst.Role));
                }
                else
                {
                    go = BuildRealInstance(inst, model, ctx, i, heightAt, rootY, result);
                    result.RealModelCount++;
                }
                PlaceInstance(go.transform, realRoot.transform, inst);
                if (model != null && !covered && inst.PropId == "cnt_wood_crate")
                {
                    if (inst.Role == "BaseLayer") crateSupports[(inst.ClusterIndex, inst.MemberIndex)] = go.transform;
                    if (inst.StackSupportMemberIndex >= 0 &&
                        crateSupports.TryGetValue((inst.ClusterIndex, inst.StackSupportMemberIndex), out Transform support))
                        SeatCrate(go.transform, support, result);
                }
                if (heightAt != null && model != null && !covered && RegionPropAssets.IsClusterModel(inst.PropId))
                {
                    // 群れ全体のモデル（幅 2 m 前後）は、配置の傾き（斜面の 7 割に沿う、塊1つ用）では下り側の縁が浮くので、地面の法線へ合わせる
                    float gx = 0.5f * (heightAt(inst.X + 1f, inst.Z) - heightAt(inst.X - 1f, inst.Z));
                    float gz = 0.5f * (heightAt(inst.X, inst.Z + 1f) - heightAt(inst.X, inst.Z - 1f));
                    var normal = new Vector3(-gx, 1f, gz).normalized; // Unity の Z は Core の -Z
                    go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0f, inst.YawDeg, 0f);
                    go.transform.localPosition = new Vector3(inst.X, heightAt(inst.X, inst.Z) - 0.03f, -inst.Z);
                    if (assembly != null)
                        SettleRubble(go.transform, renderedHeightAt, result);
                }
            }
            var summary = new System.Text.StringBuilder();
            foreach (KeyValuePair<string, int> pair in ctx.Used)
                summary.Append(pair.Key).Append('×').Append(pair.Value).Append(' ');
            result.RealSummary = summary.ToString().TrimEnd();
        }

        private static RegionPropModel SelectPlacementModel(RegionPropInstance inst, IReadOnlyList<RegionPropModel> models, RegionBiome biome, bool useGrasslandVariants)
        {
            if (models.Count == 0) return null;
            if (useGrasslandVariants && !string.IsNullOrEmpty(inst.MeshVariant))
            {
                RegionPropModel exact = models.FirstOrDefault(model => string.Equals(model.Variant, inst.MeshVariant, StringComparison.Ordinal));
                if (exact != null) return exact;
            }
            models = RegionPropAssets.FilterModelsForBiome(inst.PropId, models, biome, useGrasslandVariants);
            if (models.Count == 0) return null;
            // 別の荷を載せる土台に、箱が焼き込まれた cargo 付きパレットを重ねない。
            if (inst.Role == "PalletBase" && inst.PropId == "cnt_supply_pallet")
                models = models.Where(model => !model.Variant.StartsWith("cargo-", StringComparison.Ordinal)).ToArray();
            if (models.Count == 0) return null;
            RegionPropModel chosen = models[RegionPropAssets.PickVariant(inst, models.Select(model => model.Variant).ToArray())];
            if (inst.PropId == "cnt_wood_barrel" && inst.ConstructionVariant == "tipped_sideways" &&
                !chosen.Variant.Contains("tipped"))
                return null;
            if (inst.PropId == "cnt_wood_crate" && inst.DamageState == "collapsed_wreck" &&
                !chosen.Variant.Contains("collapsed"))
                return null;
            return chosen;
        }

        private static void SeatCrate(Transform upper, Transform lower, Result result)
        {
            var lid = lower.Find("Model/LOD0").GetComponentsInChildren<MeshFilter>(true)
                .Where(part => PartNamed(part, "Lid board")).ToArray();
            var skids = upper.Find("Model/LOD0").GetComponentsInChildren<MeshFilter>(true)
                .Where(part => PartNamed(part, "Skid")).ToArray();
            if (lid.Length == 0 || skids.Length == 0) return;

            upper.rotation = Quaternion.FromToRotation(upper.up, lower.up) * upper.rotation;
            upper.position = lower.position;
            float deck = lid.Max(part => PartHeight(part, lower).y);
            float bottom = skids.Min(part => PartHeight(part, lower).x);
            upper.position += lower.up * (deck - bottom);
            float maxGap = skids.Max(part => Math.Abs(PartHeight(part, lower).x - deck));
            result.ContactMeasurements.Add(FormattableString.Invariant(
                $"crate stack: lower={lower.name} upper={upper.name} skids={skids.Length} maxGapCm={maxGap * 100f:F3}"));
        }

        private static bool PartNamed(MeshFilter part, string name) =>
            part.sharedMesh != null && part.name.Replace('_', ' ').StartsWith(name, StringComparison.OrdinalIgnoreCase);

        private static Vector2 PartHeight(MeshFilter part, Transform frame)
        {
            Matrix4x4 toFrame = frame.worldToLocalMatrix * part.transform.localToWorldMatrix;
            Bounds bounds = part.sharedMesh.bounds;
            Vector3 extent = bounds.extents;
            float center = toFrame.MultiplyPoint3x4(bounds.center).y;
            float half = Mathf.Abs(toFrame.m10) * extent.x + Mathf.Abs(toFrame.m11) * extent.y + Mathf.Abs(toFrame.m12) * extent.z;
            return new Vector2(center - half, center + half);
        }

        private static void SettleRubble(Transform heap, Func<float, float, float> heightAt, Result result)
        {
            var parts = heap.Find("Model/LOD0").GetComponentsInChildren<MeshFilter>(true);
            var core = parts.Where(part => PartNamed(part, "CorePile")).ToArray();
            var trail = parts.Where(part => PartNamed(part, "MicroScatter")).ToArray();
            if (core.Length < 3 || trail.Length == 0 || parts.Any(part => !part.sharedMesh || !part.sharedMesh.isReadable))
                return;

            Transform frame = heap.parent;
            Vector3 anchor = heap.localPosition;
            const float step = 0.15f;
            Vector3 normal = new Vector3(
                heightAt(anchor.x - step, -anchor.z) - heightAt(anchor.x + step, -anchor.z), 2f * step,
                heightAt(anchor.x, -anchor.z + step) - heightAt(anchor.x, -anchor.z - step)).normalized;
            heap.localRotation = Quaternion.FromToRotation(heap.localRotation * Vector3.up, normal) * heap.localRotation;
            Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, normal);
            if (downhill.sqrMagnitude > 0.0001f)
            {
                Vector3 from = Vector3.ProjectOnPlane(PartCenter(trail, frame) - PartCenter(core, frame), normal);
                heap.localRotation = Quaternion.FromToRotation(from, downhill) * heap.localRotation;
            }
            heap.localPosition = new Vector3(anchor.x, heightAt(anchor.x, -anchor.z), anchor.z);

            // 原本の芯は2片を上に積んでいる。下の石の移動に追従させる。
            var upper = core.OrderByDescending(part => PartHeight(part, heap).x).Take(2).ToArray();
            var groundCore = core.Except(upper).ToArray();
            var offsets = new Dictionary<MeshFilter, float>();
            float min = float.PositiveInfinity, max = float.NegativeInfinity;
            foreach (MeshFilter part in parts.Except(upper))
            {
                Vector3[] vertices = part.sharedMesh.vertices;
                float clearance = BottomClearance(part, frame, vertices, heightAt);
                float inset = PartNamed(part, "MicroScatter") ? 0.003f : 0.012f;
                float lift = -clearance - inset;
                part.transform.position += frame.TransformVector(Vector3.up * lift);
                offsets.Add(part, lift);
                float settled = BottomClearance(part, frame, vertices, heightAt);
                min = Mathf.Min(min, settled);
                max = Mathf.Max(max, settled);
            }
            foreach (MeshFilter part in upper)
            {
                Vector3 position = part.GetComponent<Renderer>().bounds.center;
                MeshFilter below = groundCore.OrderBy(candidate =>
                {
                    Vector3 delta = frame.InverseTransformVector(candidate.GetComponent<Renderer>().bounds.center - position);
                    return delta.x * delta.x + delta.z * delta.z;
                }).First();
                part.transform.position += frame.TransformVector(Vector3.up * offsets[below]);
            }
            result.ContactMeasurements.Add(FormattableString.Invariant(
                $"rubble contact: model={heap.name} slopeDeg={Vector3.Angle(Vector3.up, normal):F1} groundParts={offsets.Count} raisedParts={upper.Length} minCm={min * 100f:F3} maxCm={max * 100f:F3}"));
        }

        private static Vector3 PartCenter(MeshFilter[] parts, Transform frame)
        {
            Vector3 sum = Vector3.zero;
            foreach (MeshFilter part in parts) sum += part.GetComponent<Renderer>().bounds.center;
            return frame.InverseTransformPoint(sum / parts.Length);
        }

        private static float BottomClearance(MeshFilter part, Transform frame, Vector3[] vertices,
            Func<float, float, float> heightAt)
        {
            Matrix4x4 toFrame = frame.worldToLocalMatrix * part.transform.localToWorldMatrix;
            float bottom = float.PositiveInfinity;
            foreach (Vector3 vertex in vertices)
            {
                Vector3 p = toFrame.MultiplyPoint3x4(vertex);
                bottom = Mathf.Min(bottom, p.y - heightAt(p.x, -p.z));
            }
            return bottom;
        }

        private static GameObject BuildRealInstance(RegionPropInstance inst, RegionPropModel model, RealContext ctx, int index,
            Func<float, float, float> heightAt, float rootY, Result result)
        {
            string key = inst.PropId + "/" + model.Variant;
            ctx.Used[key] = ctx.Used.TryGetValue(key, out int used) ? used + 1 : 1;

            var go = new GameObject(string.Format("{0:D2}_{1}_{2}", index, inst.PropId, model.Variant));
            var body = new GameObject("Model");
            body.transform.SetParent(go.transform, false);
            GameObject lod0 = InstantiateModel(model.Lod0, body.transform, "LOD0");
            GameObject lod1 = model.Lod1 != null ? InstantiateModel(model.Lod1, body.transform, "LOD1") : null;

            // カタログの寸法へ合わせる（足元の原点はモデルのまま。GameProjects のモデルは接地点が原点で、下へ少し埋まる分を持つ）
            Bounds bounds = LocalBounds(lod0, body.transform);
            body.transform.localScale = RegionPropAssets.FitScale(inst, bounds.size);

            // V7: 足元の地面の高さと傾き（Unity 座標）。V4: 明るさと暖かさ
            float groundY = (heightAt != null ? heightAt(inst.X, inst.Z) : inst.Y + inst.EmbedDepthM) + rootY;
            float slopeX = 0f, slopeZ = 0f;
            if (heightAt != null)
            {
                slopeX = 0.5f * (heightAt(inst.X + 1f, inst.Z) - heightAt(inst.X - 1f, inst.Z));
                slopeZ = -0.5f * (heightAt(inst.X, inst.Z + 1f) - heightAt(inst.X, inst.Z - 1f));
            }
            float skirt = inst.GroundSkirtBlendM > 1e-3f ? inst.GroundSkirtBlendM : 0.15f;
            skirt = Math.Max(skirt, 0.08f + 0.06f * bounds.size.y * body.transform.localScale.y);
            uint h = RegionNoise.Hash(index, inst.ClusterIndex, 7717u);
            float warmth = ((h & 1023) / 1023f) * 2f - 1f;
            ctx.Block.Clear();
            ctx.Block.SetVector("_PropGround", new Vector4(groundY, slopeX, slopeZ, skirt));
            ctx.Block.SetVector("_PropParams", new Vector4(
                Mathf.Clamp01(inst.TopDust),
                Mathf.Clamp(1f + inst.TintShift, 0.78f, 1.22f),
                warmth * 0.5f,
                Mathf.Clamp01(inst.MossMask)));

            foreach (GameObject part in new[] { lod0, lod1 })
            {
                if (part == null) continue;
                foreach (Renderer renderer in part.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] source = renderer.sharedMaterials;
                    var replaced = new Material[source.Length];
                    for (int s = 0; s < source.Length; s++)
                        replaced[s] = RealMaterial(ctx, source[s] != null ? source[s].name : "");
                    renderer.sharedMaterials = replaced;
                    if (ctx.WeatherGrassland && renderer.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
                    {
                        Vector3 size = filter.sharedMesh.bounds.size;
                        Vector3 axis = size.y >= size.x && size.y >= size.z ? Vector3.up
                            : size.x >= size.z ? Vector3.right : Vector3.forward;
                        float strip = 0.36f + 0.004f * (h % 5);
                        ctx.Block.SetVector("_PropTimberAxis", new Vector4(axis.x, axis.y, axis.z, strip));
                    }
                    renderer.SetPropertyBlock(ctx.Block);
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                    if (part == lod0) result.RealTriangles += Triangles(renderer);
                }
            }

            if (lod1 != null)
            {
                var group = go.AddComponent<LODGroup>();
                group.SetLODs(new[]
                {
                    new LOD(0.10f, lod0.GetComponentsInChildren<Renderer>(true)),
                    new LOD(0.012f, lod1.GetComponentsInChildren<Renderer>(true))
                });
                group.RecalculateBounds();
            }
            return go;
        }

        private static GameObject InstantiateModel(GameObject template, Transform parent, string name)
        {
            GameObject copy = UnityEngine.Object.Instantiate(template, parent, false);
            copy.name = name;
            copy.hideFlags = HideFlags.DontSave;
            foreach (Collider collider in copy.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);
            return copy;
        }

        private static Bounds LocalBounds(GameObject model, Transform space)
        {
            bool any = false;
            var bounds = new Bounds();
            foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                Bounds mb = filter.sharedMesh.bounds;
                Matrix4x4 toSpace = space.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                for (int c = 0; c < 8; c++)
                {
                    var corner = new Vector3((c & 1) == 0 ? mb.min.x : mb.max.x, (c & 2) == 0 ? mb.min.y : mb.max.y, (c & 4) == 0 ? mb.min.z : mb.max.z);
                    Vector3 p = toSpace.MultiplyPoint3x4(corner);
                    if (!any) { bounds = new Bounds(p, Vector3.zero); any = true; }
                    else bounds.Encapsulate(p);
                }
            }
            return bounds;
        }

        private static int Triangles(Renderer renderer)
        {
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return 0;
            int count = 0;
            for (int s = 0; s < filter.sharedMesh.subMeshCount; s++) count += (int)filter.sharedMesh.GetIndexCount(s) / 3;
            return count;
        }

        private static Material RealMaterial(RealContext ctx, string sourceName)
        {
            RegionPropSurface surface = RegionPropAssets.SurfaceFor(sourceName, ctx.WeatherGrassland);
            if (ctx.Materials.TryGetValue(surface.Key, out Material cached)) return cached;
            var mat = new Material(ctx.Shader) { name = "DS011_Real_" + surface.Key, hideFlags = HideFlags.DontSave };
            Texture2D color = surface.ColorPath.Length > 0 ? ctx.Source.Texture(surface.ColorPath) : null;
            Texture2D normal = surface.NormalPath.Length > 0 ? ctx.Source.Texture(surface.NormalPath) : null;
            Texture2D rough = surface.RoughnessPath.Length > 0 ? ctx.Source.Texture(surface.RoughnessPath) : null;
            // 写真の色に掛ける値（線形）。彩度は灰色との混ぜで近似する（画素ごとの式はシェーダーを増やさない）
            Color tint = color != null ? surface.Value * surface.Match : surface.Linear;
            tint.a = 1f;
            if (color != null)
            {
                tint.r *= 1f + 0.05f * surface.Warmth;
                tint.b *= 1f - 0.07f * surface.Warmth;
            }
            if (color != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", color);
            if (normal != null && mat.HasProperty("_NormalMap")) mat.SetTexture("_NormalMap", normal);
            if (rough != null && mat.HasProperty("_RoughnessMap"))
            {
                mat.SetTexture("_RoughnessMap", rough);
                mat.SetFloat("_UseRoughnessMap", 1f);
            }
            // tint は線形の値。Linear の色空間では Unity がマテリアルの色を sRGB とみなして線形へ直して渡すので、先に gamma へ戻す
            Color baseColor = QualitySettings.activeColorSpace == ColorSpace.Linear ? tint.gamma : tint;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Roughness")) mat.SetFloat("_Roughness", surface.Roughness);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 1f - surface.Roughness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", surface.Metallic);
            if (mat.HasProperty("_PropPhoto"))
                mat.SetVector("_PropPhoto", new Vector4(surface.StoneTileMetres, surface.TimberMapping, 0f, 0f));
            if (ctx.HasGroundColor && mat.HasProperty("_PropGroundColor"))
                mat.SetVector("_PropGroundColor", ctx.GroundColor);
            if (mat.HasProperty("_PropMossColor"))
                mat.SetVector("_PropMossColor", ctx.MossColor);
            ctx.Materials[surface.Key] = mat;
            return mat;
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

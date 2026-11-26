using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Vastcore.RegionMap
{
    public enum RegionPropPriority
    {
        P0 = 0,
        P1 = 1,
        P2 = 2
    }

    public enum RegionPropTier
    {
        S = 0,
        M = 1,
        L = 2,
        XL = 3
    }

    public enum RegionPropPivotRule
    {
        StackableUpright = 0,
        ModularSnap = 1,
        RubbleScatter = 2,
        NaturalEmbed = 3
    }

    public enum RegionPropClusterPattern
    {
        Single = 0,
        StackedGrid = 1,
        Huddled = 2,
        RadialScatter = 3,
        DirectionalCone = 4,
        WallSkirt = 5
    }

    public sealed class RegionPropLodPolicyEntry
    {
        public float SizeMinM;
        public float SizeMaxM;
        public int Lod0TrisMin;
        public int Lod0TrisMax;
        public float[] LodRatios = Array.Empty<float>();
        public float CullDistMinM;
        public float CullDistMaxM;
        public string Collider = "";
    }

    public sealed class RegionPropTechnicalStandards
    {
        public string UnitScale = "1_unit_eq_1_meter";
        public float[] ModularGridMeters = Array.Empty<float>();
        public string ColorSpace = "Linear";
        public int AlbedoMinimumSrgb = 32;
        public int AlbedoMaximumSrgb = 235;
        public readonly Dictionary<string, int> TexelDensityPxPerMeter = new Dictionary<string, int>(StringComparer.Ordinal);
        public readonly Dictionary<string, string> PivotRules = new Dictionary<string, string>(StringComparer.Ordinal);
        public readonly Dictionary<string, RegionPropLodPolicyEntry> LodPolicy = new Dictionary<string, RegionPropLodPolicyEntry>(StringComparer.Ordinal);
    }

    public sealed class RegionPropDomain
    {
        public string Id = "";
        public string NameJa = "";
        public string Description = "";
    }

    public sealed class RegionPropDimensions
    {
        public float X = 1f;
        public float Y = 1f;
        public float Z = 1f;
    }

    public sealed class RegionPropTransformRules
    {
        public float YawMinDeg;
        public float YawMaxDeg = 360f;
        public float YawStepDeg;
        public float YawJitterDeg;
        public float GridSnapBias;
        public float Step90Bias;
        public float MaxTiltDeg = 6f;
        public float FallenMaxTiltDeg;
        public float DamagedTiltDeg;
        public float FlatFaceDownBias;
        public float ScaleMin = 1f;
        public float ScaleMax = 1f;
        public float NonUniformMax;
        public bool AlignToPrevailingWind;
        public bool SouthFacingBias;
    }

    public sealed class RegionPropDamageRules
    {
        public string[] States = Array.Empty<string>();
        public float[] Weights = Array.Empty<float>();
    }

    public sealed class RegionPropConstructionRules
    {
        public string[] AssemblyTypes = Array.Empty<string>();
        public bool InterlockingStackLip;
        public string[] Posture = Array.Empty<string>();
        public float[] PostureWeights = Array.Empty<float>();
        public string[] ShaftStyle = Array.Empty<string>();
        public string[] TopProfile = Array.Empty<string>();
        public string[] DoorState = Array.Empty<string>();
        public string[] Mount = Array.Empty<string>();
        public string[] CookingRig = Array.Empty<string>();
        public string[] CarvingPattern = Array.Empty<string>();
        public string[] RebarBendVariants = Array.Empty<string>();
        public string[] SeamGrooves = Array.Empty<string>();
        public string[] SideBorder = Array.Empty<string>();
        public string[] FlapState = Array.Empty<string>();
        public int[] BoardCount = Array.Empty<int>();
        public int[] StackHeightTiers = Array.Empty<int>();
        public int[] CrossArmCount = Array.Empty<int>();
        public int[] RailCount = Array.Empty<int>();
        public float LidAngleMinDeg;
        public float LidAngleMaxDeg;
        public float DishAimElevationMinDeg;
        public float DishAimElevationMaxDeg;
        public float BoardAngleSpreadDeg;
        public bool FlangeJoints;
        public readonly Dictionary<string, object> Raw = new Dictionary<string, object>(StringComparer.Ordinal);
    }

    public sealed class RegionPropWeatheringRules
    {
        public float PerInstanceTintShift;
        public float PerInstanceHueShift;
        public float PerInstanceValueShift;
        public float EdgeWearMin;
        public float EdgeWearMax;
        public float RustRatioMin;
        public float RustRatioMax;
        public bool StencilDecalToggle;
        public bool InheritParentMaterial;
        public readonly Dictionary<string, object> Raw = new Dictionary<string, object>(StringComparer.Ordinal);
    }

    public sealed class RegionPropSpreadLayer
    {
        public string Role = "";
        public int CountMin = 1;
        public int CountMax = 1;
        public float ScaleMin = 1f;
        public float ScaleMax = 1f;
        public float DistNormMin;
        public float DistNormMax = 1f;
    }

    public sealed class RegionPropClusterRules
    {
        public RegionPropClusterPattern Pattern = RegionPropClusterPattern.Single;
        public RegionPropClusterPattern? SecondaryPattern;
        public int ClusterCountMin = 1;
        public int ClusterCountMax = 1;
        public int MaxStackLayers = 1;
        public float StackYawJitterDeg;
        public float RadiusM = 2f;
        public float ConeAngleDeg = 70f;
        public float RowSpacingM;
        public float GapProbability;
        public string SizeGradient = "";
        public string CollapsedSpills = "";
        public string RequireDebrisSkirt = "";
        public string[] ActsAsBaseFor = Array.Empty<string>();
        public string[] PairWith = Array.Empty<string>();
        public string[] LeanAgainst = Array.Empty<string>();
        public string[] AnchorFor = Array.Empty<string>();
        public bool FollowBeddingOrientation;
        public bool LeeSideBias;
        public bool WashChannelBias;
        public bool HighGroundBias;
        public bool TrailJunctionBias;
        public readonly List<RegionPropSpreadLayer> Layers = new List<RegionPropSpreadLayer>();
    }

    public sealed class RegionPropOvergrowthRules
    {
        public float TopDustMin;
        public float TopDustMax;
        public float GroundSkirtBlendM;
        public float MossMaskMin;
        public float MossMaskMax;
    }

    public sealed class RegionPropVariationRules
    {
        public RegionPropTransformRules V1_Transform = new RegionPropTransformRules();
        public RegionPropDamageRules V2_DamageState;
        public RegionPropConstructionRules V3_Construction;
        public RegionPropWeatheringRules V4_MaterialWeathering = new RegionPropWeatheringRules();
        public string[] V5_ContentsAttachment = Array.Empty<string>();
        public RegionPropClusterRules V6_ClusterSpread = new RegionPropClusterRules();
        public RegionPropOvergrowthRules V7_BiomeOvergrowth = new RegionPropOvergrowthRules();
    }

    public sealed class RegionPropSpawningRules
    {
        public float MaxSlopeDeg = 20f;
        public float EmbedDepthMinRatio;
        public float EmbedDepthMaxRatio;
        public float UsePalletUnderOnSlopeOverDeg = -1f;
        public float FoundationSkirtM;
        public string Collider = "";
        public string[] PairWith = Array.Empty<string>();
    }

    /// <summary>
    /// DS-011 環境 Props 原型 1 件の定義。SP-018（V1..V5）と SP-019（StructureTags）へ接続可能な純粋データ。
    /// </summary>
    public sealed class RegionPropDefinition
    {
        public string Id = "";
        public string NameJa = "";
        public string Domain = "";
        public RegionPropPriority Priority = RegionPropPriority.P0;
        public RegionPropTier Tier = RegionPropTier.M;
        public string[] EraTags = Array.Empty<string>();
        public string[] BiomeTags = Array.Empty<string>();
        public readonly Dictionary<string, float> StructureTags = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        public RegionPropDimensions DimensionsM = new RegionPropDimensions();
        public RegionPropPivotRule PivotRule = RegionPropPivotRule.StackableUpright;
        public string LodTier = "tierM";
        public string[] MeshVariants = Array.Empty<string>();
        public string[] MaterialVariants = Array.Empty<string>();
        public RegionPropVariationRules VariationRules = new RegionPropVariationRules();
        public RegionPropSpawningRules SpawningRules = new RegionPropSpawningRules();

        /// <summary>SP-018 RotationMode 互換名（Fixed / Step90 / Free）を返す。</summary>
        public string Sp018RotationMode()
        {
            RegionPropTransformRules t = VariationRules.V1_Transform;
            if (t.YawStepDeg >= 90f) return "Step90";
            if (Math.Abs(t.YawMaxDeg - t.YawMinDeg) < 0.01f) return "Fixed";
            return "Free";
        }
    }

    /// <summary>
    /// DS-011 (`docs/02_design/environment-props-catalog.json`) の読込済みカタログ。
    /// </summary>
    public sealed class RegionPropCatalog
    {
        public const string DefaultRelativePath = "docs/02_design/environment-props-catalog.json";

        public string SchemaVersion = "";
        public string SpecId = "";
        public string LastUpdated = "";
        public string Project = "";
        public RegionPropTechnicalStandards TechnicalStandards = new RegionPropTechnicalStandards();
        public readonly Dictionary<string, string> VariationAxes = new Dictionary<string, string>(StringComparer.Ordinal);
        public readonly List<RegionPropDomain> Domains = new List<RegionPropDomain>();
        public readonly List<RegionPropDefinition> Props = new List<RegionPropDefinition>();

        private readonly Dictionary<string, RegionPropDefinition> _byId = new Dictionary<string, RegionPropDefinition>(StringComparer.Ordinal);

        public bool TryGetProp(string id, out RegionPropDefinition prop)
        {
            if (string.IsNullOrEmpty(id))
            {
                prop = null;
                return false;
            }
            return _byId.TryGetValue(id, out prop);
        }

        public RegionPropDefinition GetProp(string id)
        {
            if (!TryGetProp(id, out RegionPropDefinition prop))
                throw new KeyNotFoundException("DS-011 Prop ID が見つかりません: " + id);
            return prop;
        }

        public List<RegionPropDefinition> Filter(RegionPropPriority? priority = null, string domain = null, string eraTag = null, string biomeTag = null)
        {
            var list = new List<RegionPropDefinition>();
            foreach (RegionPropDefinition prop in Props)
            {
                if (priority.HasValue && prop.Priority != priority.Value) continue;
                if (!string.IsNullOrEmpty(domain) && !string.Equals(prop.Domain, domain, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.IsNullOrEmpty(eraTag) && !ContainsTag(prop.EraTags, eraTag)) continue;
                if (!string.IsNullOrEmpty(biomeTag) && !ContainsTag(prop.BiomeTags, "all") && !ContainsTag(prop.BiomeTags, biomeTag)) continue;
                list.Add(prop);
            }
            return list;
        }

        private static bool ContainsTag(string[] tags, string value)
        {
            if (tags == null) return false;
            for (int i = 0; i < tags.Length; i++)
                if (string.Equals(tags[i], value, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>現在ディレクトリまたはその親ディレクトリから `docs/02_design/environment-props-catalog.json` を探す。</summary>
        public static string FindDefaultCatalogPath(string startDirectory = null)
        {
            string dir = string.IsNullOrEmpty(startDirectory) ? Directory.GetCurrentDirectory() : startDirectory;
            for (int depth = 0; depth < 8 && !string.IsNullOrEmpty(dir); depth++)
            {
                string candidate = Path.Combine(dir, DefaultRelativePath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(candidate)) return candidate;
                DirectoryInfo parent = Directory.GetParent(dir);
                dir = parent != null ? parent.FullName : null;
            }
            return null;
        }

        public static RegionPropCatalog LoadDefault(string startDirectory = null)
        {
            string path = FindDefaultCatalogPath(startDirectory);
            if (path == null)
                throw new FileNotFoundException("DS-011 カタログ JSON が見つかりません: " + DefaultRelativePath);
            return LoadFromFile(path);
        }

        public static RegionPropCatalog LoadFromFile(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");
            return Parse(File.ReadAllText(path));
        }

        public static RegionPropCatalog Parse(string json)
        {
            Dictionary<string, object> root = RegionJson.ParseObject(json);
            var catalog = new RegionPropCatalog
            {
                SchemaVersion = GetString(root, "schemaVersion"),
                SpecId = GetString(root, "specId"),
                LastUpdated = GetString(root, "lastUpdated"),
                Project = GetString(root, "project")
            };

            if (TryGetDict(root, "technicalStandards", out Dictionary<string, object> ts))
                catalog.TechnicalStandards = ParseTechnicalStandards(ts);

            if (TryGetDict(root, "variationAxes", out Dictionary<string, object> axes))
            {
                foreach (var pair in axes)
                    catalog.VariationAxes[pair.Key] = pair.Value != null ? Convert.ToString(pair.Value, CultureInfo.InvariantCulture) : "";
            }

            if (TryGetList(root, "domains", out List<object> domains))
            {
                foreach (object item in domains)
                {
                    if (!(item is Dictionary<string, object> d)) continue;
                    catalog.Domains.Add(new RegionPropDomain
                    {
                        Id = GetString(d, "id"),
                        NameJa = GetString(d, "nameJa"),
                        Description = GetString(d, "description")
                    });
                }
            }

            if (TryGetList(root, "props", out List<object> props))
            {
                foreach (object item in props)
                {
                    if (!(item is Dictionary<string, object> p)) continue;
                    RegionPropDefinition def = ParseProp(p);
                    if (string.IsNullOrEmpty(def.Id))
                        throw new FormatException("DS-011 props エントリに id がありません。");
                    if (catalog._byId.ContainsKey(def.Id))
                        throw new FormatException("DS-011 Prop ID が重複しています: " + def.Id);
                    catalog.Props.Add(def);
                    catalog._byId[def.Id] = def;
                }
            }

            return catalog;
        }

        private static RegionPropTechnicalStandards ParseTechnicalStandards(Dictionary<string, object> ts)
        {
            var result = new RegionPropTechnicalStandards
            {
                UnitScale = GetString(ts, "unitScale", "1_unit_eq_1_meter"),
                ModularGridMeters = GetFloatArray(ts, "modularGridMeters"),
                ColorSpace = GetString(ts, "colorSpace", "Linear"),
                AlbedoMinimumSrgb = GetInt(ts, "albedoMinimumSrgb", 32),
                AlbedoMaximumSrgb = GetInt(ts, "albedoMaximumSrgb", 235)
            };
            if (TryGetDict(ts, "texelDensityPxPerMeter", out Dictionary<string, object> td))
            {
                foreach (var pair in td)
                    result.TexelDensityPxPerMeter[pair.Key] = ToInt(pair.Value);
            }
            if (TryGetDict(ts, "pivotRules", out Dictionary<string, object> pr))
            {
                foreach (var pair in pr)
                    result.PivotRules[pair.Key] = pair.Value != null ? Convert.ToString(pair.Value, CultureInfo.InvariantCulture) : "";
            }
            if (TryGetDict(ts, "lodPolicy", out Dictionary<string, object> lp))
            {
                foreach (var pair in lp)
                {
                    if (!(pair.Value is Dictionary<string, object> entry)) continue;
                    float[] sizeRange = GetFloatArray(entry, "sizeRangeM");
                    int[] tris = GetIntArray(entry, "lod0Tris");
                    float[] cull = GetFloatArray(entry, "cullDistM");
                    result.LodPolicy[pair.Key] = new RegionPropLodPolicyEntry
                    {
                        SizeMinM = sizeRange.Length > 0 ? sizeRange[0] : 0f,
                        SizeMaxM = sizeRange.Length > 1 ? sizeRange[1] : 0f,
                        Lod0TrisMin = tris.Length > 0 ? tris[0] : 0,
                        Lod0TrisMax = tris.Length > 1 ? tris[1] : 0,
                        LodRatios = GetFloatArray(entry, "lodRatios"),
                        CullDistMinM = cull.Length > 0 ? cull[0] : 0f,
                        CullDistMaxM = cull.Length > 1 ? cull[1] : 0f,
                        Collider = GetString(entry, "collider")
                    };
                }
            }
            return result;
        }

        private static RegionPropDefinition ParseProp(Dictionary<string, object> p)
        {
            var def = new RegionPropDefinition
            {
                Id = GetString(p, "id"),
                NameJa = GetString(p, "nameJa"),
                Domain = GetString(p, "domain"),
                Priority = ParsePriority(GetString(p, "priority", "P0")),
                Tier = ParseTier(GetString(p, "tier", "M")),
                EraTags = GetStringArray(p, "eraTags"),
                BiomeTags = GetStringArray(p, "biomeTags"),
                PivotRule = ParsePivotRule(GetString(p, "pivotRule", "stackable_upright")),
                LodTier = GetString(p, "lodTier", "tierM"),
                MeshVariants = GetStringArray(p, "meshVariants"),
                MaterialVariants = GetStringArray(p, "materialVariants")
            };

            if (TryGetDict(p, "structureTags", out Dictionary<string, object> st))
            {
                foreach (var pair in st)
                    def.StructureTags[pair.Key] = ToFloat(pair.Value);
            }

            if (TryGetDict(p, "dimensionsM", out Dictionary<string, object> dim))
            {
                def.DimensionsM = new RegionPropDimensions
                {
                    X = GetFloat(dim, "x", 1f),
                    Y = GetFloat(dim, "y", 1f),
                    Z = GetFloat(dim, "z", 1f)
                };
            }

            if (TryGetDict(p, "variationRules", out Dictionary<string, object> vr))
                def.VariationRules = ParseVariationRules(vr);

            if (TryGetDict(p, "spawningRules", out Dictionary<string, object> sr))
                def.SpawningRules = ParseSpawningRules(sr);

            return def;
        }

        private static RegionPropVariationRules ParseVariationRules(Dictionary<string, object> vr)
        {
            var rules = new RegionPropVariationRules();
            if (TryGetDict(vr, "V1_Transform", out Dictionary<string, object> v1))
            {
                var tr = new RegionPropTransformRules();
                float[] yaw = GetFloatArray(v1, "yawDeg");
                if (yaw.Length >= 2)
                {
                    tr.YawMinDeg = yaw[0];
                    tr.YawMaxDeg = yaw[1];
                }
                else
                {
                    tr.YawMinDeg = 0f;
                    tr.YawMaxDeg = 360f;
                }
                tr.YawStepDeg = GetFloat(v1, "yawStepDeg", 0f);
                tr.YawJitterDeg = GetFloat(v1, "yawJitterDeg", 0f);
                tr.GridSnapBias = GetFloat(v1, "gridSnapBias", 0f);
                tr.Step90Bias = GetFloat(v1, "step90Bias", 0f);
                tr.MaxTiltDeg = GetFloat(v1, "maxTiltDeg", 6f);
                tr.FallenMaxTiltDeg = GetFloat(v1, "fallenMaxTiltDeg", tr.MaxTiltDeg);
                tr.DamagedTiltDeg = GetFloat(v1, "damagedTiltDeg", tr.MaxTiltDeg);
                tr.FlatFaceDownBias = GetFloat(v1, "flatFaceDownBias", 0f);
                float[] scale = GetFloatArray(v1, "scaleRange");
                tr.ScaleMin = scale.Length > 0 ? scale[0] : 1f;
                tr.ScaleMax = scale.Length > 1 ? scale[1] : tr.ScaleMin;
                tr.NonUniformMax = GetFloat(v1, "nonUniformMax", 0f);
                tr.AlignToPrevailingWind = GetBool(v1, "alignToPrevailingWind", false);
                tr.SouthFacingBias = GetBool(v1, "southFacingBias", false);
                rules.V1_Transform = tr;
            }

            if (TryGetDict(vr, "V2_DamageState", out Dictionary<string, object> v2))
            {
                rules.V2_DamageState = new RegionPropDamageRules
                {
                    States = GetStringArray(v2, "states"),
                    Weights = GetFloatArray(v2, "weights")
                };
            }

            if (TryGetDict(vr, "V3_Construction", out Dictionary<string, object> v3))
            {
                var con = new RegionPropConstructionRules
                {
                    AssemblyTypes = GetStringArray(v3, "assemblyTypes"),
                    InterlockingStackLip = GetBool(v3, "interlockingStackLip", false),
                    Posture = GetStringArray(v3, "posture"),
                    PostureWeights = GetFloatArray(v3, "postureWeights"),
                    ShaftStyle = GetStringArray(v3, "shaftStyle"),
                    TopProfile = GetStringArray(v3, "topProfile"),
                    DoorState = GetStringArray(v3, "doorState"),
                    Mount = GetStringArray(v3, "mount"),
                    CookingRig = GetStringArray(v3, "cookingRig"),
                    CarvingPattern = GetStringArray(v3, "carvingPattern"),
                    RebarBendVariants = GetStringArray(v3, "rebarBendVariants"),
                    SeamGrooves = GetStringArray(v3, "seamGrooves"),
                    SideBorder = GetStringArray(v3, "sideBorder"),
                    FlapState = GetStringArray(v3, "flapState"),
                    BoardCount = GetIntArray(v3, "boardCount"),
                    StackHeightTiers = GetIntArray(v3, "stackHeightTiers"),
                    CrossArmCount = GetIntArray(v3, "crossArmCount"),
                    RailCount = GetIntArray(v3, "railCount"),
                    BoardAngleSpreadDeg = GetFloat(v3, "boardAngleSpreadDeg", 0f),
                    FlangeJoints = GetBool(v3, "flangeJoints", false)
                };
                ReadRange(v3, "lidAngleDeg", out con.LidAngleMinDeg, out con.LidAngleMaxDeg);
                ReadRange(v3, "dishAimElevationDeg", out con.DishAimElevationMinDeg, out con.DishAimElevationMaxDeg);
                foreach (var pair in v3) con.Raw[pair.Key] = pair.Value;
                rules.V3_Construction = con;
            }

            if (TryGetDict(vr, "V4_MaterialWeathering", out Dictionary<string, object> v4))
            {
                var w = new RegionPropWeatheringRules
                {
                    PerInstanceTintShift = GetFloat(v4, "perInstanceTintShift", 0f),
                    PerInstanceHueShift = GetFloat(v4, "perInstanceHueShift", 0f),
                    PerInstanceValueShift = GetFloat(v4, "perInstanceValueShift", 0f),
                    StencilDecalToggle = GetBool(v4, "stencilDecalToggle", false),
                    InheritParentMaterial = GetBool(v4, "inheritParentMaterial", false)
                };
                ReadRange(v4, "edgeWear", out w.EdgeWearMin, out w.EdgeWearMax);
                ReadRange(v4, "rustRatio", out w.RustRatioMin, out w.RustRatioMax);
                if (v4.ContainsKey("hoopRust"))
                    ReadRange(v4, "hoopRust", out w.RustRatioMin, out w.RustRatioMax);
                foreach (var pair in v4) w.Raw[pair.Key] = pair.Value;
                rules.V4_MaterialWeathering = w;
            }

            rules.V5_ContentsAttachment = GetStringArray(vr, "V5_ContentsAttachment");

            if (TryGetDict(vr, "V6_ClusterSpread", out Dictionary<string, object> v6))
            {
                var c = new RegionPropClusterRules
                {
                    Pattern = ParseClusterPattern(GetString(v6, "pattern", "Single")),
                    MaxStackLayers = GetInt(v6, "maxStackLayers", 1),
                    StackYawJitterDeg = GetFloat(v6, "stackYawJitterDeg", 0f),
                    RadiusM = GetFloat(v6, "radiusM", 3f),
                    ConeAngleDeg = GetFloat(v6, "coneAngleDeg", 70f),
                    RowSpacingM = GetFloat(v6, "rowSpacingM", 0f),
                    GapProbability = GetFloat(v6, "gapProbability", 0f),
                    SizeGradient = GetString(v6, "sizeGradient"),
                    CollapsedSpills = GetString(v6, "collapsedSpills"),
                    RequireDebrisSkirt = GetString(v6, "requireDebrisSkirt"),
                    ActsAsBaseFor = GetStringArray(v6, "actsAsBaseFor"),
                    PairWith = GetStringArray(v6, "pairWith"),
                    LeanAgainst = GetStringArray(v6, "leanAgainst"),
                    AnchorFor = GetStringArray(v6, "anchorFor"),
                    FollowBeddingOrientation = GetBool(v6, "followBeddingOrientation", false),
                    LeeSideBias = GetBool(v6, "leeSideBias", false),
                    WashChannelBias = GetBool(v6, "washChannelBias", false),
                    HighGroundBias = GetBool(v6, "highGroundBias", false),
                    TrailJunctionBias = GetBool(v6, "trailJunctionBias", false)
                };
                string secondary = GetString(v6, "secondaryPattern");
                if (!string.IsNullOrEmpty(secondary))
                    c.SecondaryPattern = ParseClusterPattern(secondary);
                int[] countRange = GetIntArray(v6, "clusterCount");
                c.ClusterCountMin = countRange.Length > 0 ? countRange[0] : 1;
                c.ClusterCountMax = countRange.Length > 1 ? countRange[1] : c.ClusterCountMin;
                if (TryGetList(v6, "layers", out List<object> layers))
                {
                    foreach (object item in layers)
                    {
                        if (!(item is Dictionary<string, object> ld)) continue;
                        int[] lc = GetIntArray(ld, "count");
                        var layer = new RegionPropSpreadLayer
                        {
                            Role = GetString(ld, "role"),
                            CountMin = lc.Length > 0 ? lc[0] : 1,
                            CountMax = lc.Length > 1 ? lc[1] : (lc.Length > 0 ? lc[0] : 1)
                        };
                        ReadRange(ld, "scale", out layer.ScaleMin, out layer.ScaleMax, 1f);
                        ReadRange(ld, "distNorm", out layer.DistNormMin, out layer.DistNormMax, 0f);
                        c.Layers.Add(layer);
                    }
                }
                rules.V6_ClusterSpread = c;
            }

            if (TryGetDict(vr, "V7_BiomeOvergrowth", out Dictionary<string, object> v7))
            {
                var o = new RegionPropOvergrowthRules
                {
                    GroundSkirtBlendM = GetFloat(v7, "groundSkirtBlendM", 0f)
                };
                ReadRange(v7, "topDust", out o.TopDustMin, out o.TopDustMax);
                ReadRange(v7, "mossMask", out o.MossMaskMin, out o.MossMaskMax);
                rules.V7_BiomeOvergrowth = o;
            }

            return rules;
        }

        private static RegionPropSpawningRules ParseSpawningRules(Dictionary<string, object> sr)
        {
            var s = new RegionPropSpawningRules
            {
                MaxSlopeDeg = GetFloat(sr, "maxSlopeDeg", 20f),
                UsePalletUnderOnSlopeOverDeg = GetFloat(sr, "usePalletUnderOnSlopeOverDeg", -1f),
                FoundationSkirtM = GetFloat(sr, "foundationSkirtM", 0f),
                Collider = GetString(sr, "collider"),
                PairWith = GetStringArray(sr, "pairWith")
            };
            ReadRange(sr, "embedDepthRatio", out s.EmbedDepthMinRatio, out s.EmbedDepthMaxRatio);
            return s;
        }

        private static RegionPropPriority ParsePriority(string s)
        {
            if (string.Equals(s, "P1", StringComparison.OrdinalIgnoreCase)) return RegionPropPriority.P1;
            if (string.Equals(s, "P2", StringComparison.OrdinalIgnoreCase)) return RegionPropPriority.P2;
            return RegionPropPriority.P0;
        }

        private static RegionPropTier ParseTier(string s)
        {
            if (string.Equals(s, "S", StringComparison.OrdinalIgnoreCase)) return RegionPropTier.S;
            if (string.Equals(s, "L", StringComparison.OrdinalIgnoreCase)) return RegionPropTier.L;
            if (string.Equals(s, "XL", StringComparison.OrdinalIgnoreCase)) return RegionPropTier.XL;
            return RegionPropTier.M;
        }

        private static RegionPropPivotRule ParsePivotRule(string s)
        {
            switch (s)
            {
                case "modular_snap": return RegionPropPivotRule.ModularSnap;
                case "rubble_scatter": return RegionPropPivotRule.RubbleScatter;
                case "natural_embed": return RegionPropPivotRule.NaturalEmbed;
                default: return RegionPropPivotRule.StackableUpright;
            }
        }

        private static RegionPropClusterPattern ParseClusterPattern(string s)
        {
            switch (s)
            {
                case "StackedGrid": return RegionPropClusterPattern.StackedGrid;
                case "Huddled": return RegionPropClusterPattern.Huddled;
                case "RadialScatter": return RegionPropClusterPattern.RadialScatter;
                case "DirectionalCone": return RegionPropClusterPattern.DirectionalCone;
                case "WallSkirt": return RegionPropClusterPattern.WallSkirt;
                default: return RegionPropClusterPattern.Single;
            }
        }

        private static bool TryGetDict(Dictionary<string, object> dict, string key, out Dictionary<string, object> value)
        {
            if (dict != null && dict.TryGetValue(key, out object obj) && obj is Dictionary<string, object> d)
            {
                value = d;
                return true;
            }
            value = null;
            return false;
        }

        private static bool TryGetList(Dictionary<string, object> dict, string key, out List<object> value)
        {
            if (dict != null && dict.TryGetValue(key, out object obj) && obj is List<object> list)
            {
                value = list;
                return true;
            }
            value = null;
            return false;
        }

        private static string GetString(Dictionary<string, object> dict, string key, string fallback = "")
        {
            if (dict != null && dict.TryGetValue(key, out object obj) && obj != null)
                return Convert.ToString(obj, CultureInfo.InvariantCulture);
            return fallback;
        }

        private static float GetFloat(Dictionary<string, object> dict, string key, float fallback = 0f)
        {
            if (dict != null && dict.TryGetValue(key, out object obj) && obj != null)
                return ToFloat(obj);
            return fallback;
        }

        private static int GetInt(Dictionary<string, object> dict, string key, int fallback = 0)
        {
            if (dict != null && dict.TryGetValue(key, out object obj) && obj != null)
                return ToInt(obj);
            return fallback;
        }

        private static bool GetBool(Dictionary<string, object> dict, string key, bool fallback = false)
        {
            if (dict != null && dict.TryGetValue(key, out object obj) && obj is bool b)
                return b;
            return fallback;
        }

        private static string[] GetStringArray(Dictionary<string, object> dict, string key)
        {
            if (!TryGetList(dict, key, out List<object> list) || list.Count == 0)
                return Array.Empty<string>();
            var arr = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
                arr[i] = list[i] != null ? Convert.ToString(list[i], CultureInfo.InvariantCulture) : "";
            return arr;
        }

        private static float[] GetFloatArray(Dictionary<string, object> dict, string key)
        {
            if (!TryGetList(dict, key, out List<object> list) || list.Count == 0)
                return Array.Empty<float>();
            var arr = new float[list.Count];
            for (int i = 0; i < list.Count; i++)
                arr[i] = ToFloat(list[i]);
            return arr;
        }

        private static int[] GetIntArray(Dictionary<string, object> dict, string key)
        {
            if (!TryGetList(dict, key, out List<object> list) || list.Count == 0)
                return Array.Empty<int>();
            var arr = new int[list.Count];
            for (int i = 0; i < list.Count; i++)
                arr[i] = ToInt(list[i]);
            return arr;
        }

        private static void ReadRange(Dictionary<string, object> dict, string key, out float min, out float max, float defaultVal = 0f)
        {
            min = defaultVal;
            max = defaultVal;
            if (dict == null || !dict.TryGetValue(key, out object obj) || obj == null) return;
            if (obj is List<object> list)
            {
                if (list.Count >= 1) min = ToFloat(list[0]);
                max = list.Count >= 2 ? ToFloat(list[1]) : min;
                return;
            }
            float single = ToFloat(obj);
            min = single;
            max = single;
        }

        private static float ToFloat(object obj)
        {
            if (obj == null) return 0f;
            if (obj is double d) return (float)d;
            if (obj is float f) return f;
            if (obj is int i) return i;
            if (obj is long l) return l;
            return float.Parse(Convert.ToString(obj, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }

        private static int ToInt(object obj)
        {
            if (obj == null) return 0;
            if (obj is double d) return (int)Math.Round(d);
            if (obj is int i) return i;
            if (obj is long l) return (int)l;
            return int.Parse(Convert.ToString(obj, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }
    }
}

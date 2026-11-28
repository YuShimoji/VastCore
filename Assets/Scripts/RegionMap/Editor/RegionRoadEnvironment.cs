using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using NVector2 = System.Numerics.Vector2;

namespace Vastcore.RegionMap.Editor
{
    public sealed partial class RegionRoadEnvironment : EditorWindow
    {
        private static readonly string[] Views = { "Far", "Mid", "Entrance", "Highway" };
        private const string TextureRoot = "Assets/Resources/MapGenerator/Textures/";
        private const int GroundResolution = 1025;
        private const int DetailedGroundResolution = 2049;
        private const int FieldResolution = 512;
        private const int DetailedFieldResolution = 1024;
        private const float HeightRange = 420f;
        private const float Span = 2f * RegionRoadPlan.Extent;

        private enum RoadAppearance { Original, Graded, Landscaped, Footing, VariedGravel, WornEdge }

        [SerializeField] private int seed = 8;
        [SerializeField] private RegionRoadPreset preset;
        [SerializeField] private bool showShape;
        private Stage preview;

        [MenuItem("Vastcore/Region Map/Highway Corridor Preview")]
        public static void OpenWindow() => GetWindow<RegionRoadEnvironment>("Highway Corridor");

        private void OnEnable() => EditorSceneManager.sceneClosing += ReleaseScene;

        private void OnDisable()
        {
            EditorSceneManager.sceneClosing -= ReleaseScene;
            ReleasePreview();
        }

        private void ReleaseScene(Scene scene, bool removingScene)
        {
            if (preview != null && preview.Root && preview.Root.scene == scene) ReleasePreview();
        }

        private void ReleasePreview()
        {
            preview?.Dispose();
            preview = null;
        }

        private void OnGUI()
        {
            seed = EditorGUILayout.IntField("Seed", seed);
            preset = (RegionRoadPreset)EditorGUILayout.EnumPopup("Road layout", preset);
            showShape = EditorGUILayout.Toggle("Shape only", showShape);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("Generate preview")) GeneratePreview();
            using (new EditorGUI.DisabledScope(preview == null || !preview.Root))
            using (new EditorGUILayout.HorizontalScope())
                for (int i = 0; i < Views.Length; i++)
                    if (GUILayout.Button(Views[i])) Focus(preview, i);
        }

        private void GeneratePreview()
        {
            bool sameScene = preview != null && preview.Root && preview.Root.scene == SceneManager.GetActiveScene();
            if (!sameScene)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                ReleasePreview();
            }
            try
            {
                EditorUtility.DisplayProgressBar("Highway Corridor", "Generating terrain and roads", 0.45f);
                Stage next = Build(seed, preset, showShape);
                if (sameScene) preview.Dispose();
                preview = next;
                Focus(preview, 0);
                Selection.activeGameObject = preview.Roads;
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private static void Focus(Stage stage, int view)
        {
            Place(stage, view);
            SceneView.lastActiveSceneView?.AlignViewToObject(stage.Camera.transform);
        }

        private sealed class Stage : IDisposable
        {
            public GameObject Root, Roads;
            public Camera Camera;
            public Light Sun;
            public Material Sky;
            public Terrain Terrain;
            public RegionRoadPlan Plan;
            public bool Detailed;
            public readonly List<UnityEngine.Object> Assets = new List<UnityEngine.Object>();

            public T Own<T>(T asset) where T : UnityEngine.Object
            {
                Assets.Add(asset);
                asset.hideFlags = HideFlags.DontSave;
                return asset;
            }

            public void Dispose()
            {
                if (Root)
                {
                    foreach (MeshFilter part in Root.GetComponentsInChildren<MeshFilter>(true))
                        if (part.sharedMesh && (part.sharedMesh.hideFlags & HideFlags.DontSave) != 0)
                            DestroyImmediate(part.sharedMesh);
                    foreach (MeshRenderer part in Root.GetComponentsInChildren<MeshRenderer>(true))
                        if (part.sharedMaterial && (part.sharedMaterial.hideFlags & HideFlags.DontSave) != 0 && !Assets.Contains(part.sharedMaterial))
                            Assets.Add(part.sharedMaterial);
                    DestroyImmediate(Root);
                }
                foreach (UnityEngine.Object asset in Assets)
                    if (asset) DestroyImmediate(asset);
                Assets.Clear();
            }
        }

        private static Stage Build(int seed, RegionRoadPreset preset, bool shapeOnly, RoadAppearance appearance = RoadAppearance.WornEdge)
        {
            bool detailed = appearance >= RoadAppearance.Graded;
            var stage = new Stage { Plan = new RegionRoadPlan(seed, preset), Detailed = detailed };
            stage.Root = new GameObject($"Highway Corridor – {preset} / {seed}") { hideFlags = HideFlags.DontSaveInEditor };
            try
            {
                AddGround(stage, shapeOnly);
                stage.Roads = new GameObject("Road kit");
                stage.Roads.transform.SetParent(stage.Root.transform, false);
                RegionRoadMaterials materials = CreateRoadMaterials(stage, shapeOnly);
                bool blendedVerge = detailed && !shapeOnly;
                RegionRoadView.Build(stage.Roads.transform, stage.Plan.Highway, materials, shapeOnly: shapeOnly, blendedVerge: blendedVerge);
                RegionRoadView.Build(stage.Roads.transform, stage.Plan.Arterial, materials, stage.Plan.Site, shapeOnly, blendedVerge);
                if (!shapeOnly)
                {
                    float Ground(float x, float z) => stage.Terrain.SampleHeight(new Vector3(x, 0f, z));
                    bool refined = appearance >= RoadAppearance.Landscaped;
                    RegionRoadLandscape.PlantFarmland(stage.Root.transform, stage.Plan, Ground, detailed, refined);
                    if (detailed)
                    {
                        var shader = Resources.Load<Shader>("RegionMap3D/RegionRoadMargin");
                        if (!shader) throw new InvalidOperationException("The roadside surface shader is missing.");
                        var verge = stage.Own(new Material(shader) { name = "Gravel, loam and turf" });
                        verge.SetTexture("_Gravel", Surface("Ground007"));
                        verge.SetTexture("_Soil", Surface("Ground009"));
                        verge.SetTexture("_Turf", Surface("Grass005"));
                        verge.SetFloat("_Refined", refined ? 1f : 0f);
                        bool footing = appearance >= RoadAppearance.Footing;
                        bool variedGravel = appearance >= RoadAppearance.VariedGravel;
                        bool wornEdge = appearance >= RoadAppearance.WornEdge;
                        if (footing)
                        {
                            verge.SetTexture("_GravelNormal", Surface("Ground007", "NormalGL"));
                            verge.SetFloat("_CloseDetail", 1f);
                            verge.SetFloat("_WornEdge", wornEdge ? 1f : 0f);
                            NVector2 offset = RegionRoadMargin.VergePocketOffset(seed);
                            verge.SetVector("_PocketOffset", new Vector4(offset.X, offset.Y));
                        }
                        RegionRoadGroundView.AddMargins(stage.Roads.transform, stage.Plan, Ground, verge, refined, footing,
                            variedGravel, wornEdge);
                        RegionRoadLandscape.PlantMargins(stage.Roads.transform, stage.Plan, Ground, refined);
                        RegionRoadGroundView.AddRidges(stage.Root.transform, stage.Plan, Ground,
                            Lit(stage, "Distant hills", new Color(0.30f, 0.40f, 0.35f)));
                    }
                    else RegionRoadLandscape.PlantVerges(stage.Roads.transform, stage.Plan, Ground);
                }

                var sun = new GameObject("Sun");
                sun.transform.SetParent(stage.Root.transform, false);
                stage.Sun = sun.AddComponent<Light>();
                stage.Sun.type = LightType.Directional;
                var camera = new GameObject("Main Camera") { tag = "MainCamera" };
                camera.transform.SetParent(stage.Root.transform, false);
                stage.Camera = camera.AddComponent<Camera>();
                stage.Camera.nearClipPlane = 0.18f;
                stage.Camera.farClipPlane = 5700f;
                RegionAtmosphere atmosphere = RegionAtmosphere.Create(RegionAtmosphere.Preset.ClearAfternoon);
                atmosphere.SunAltitudeDegrees = 38f;
                atmosphere.SunIntensity = 2.05f;
                atmosphere.Contrast = 4f;
                atmosphere.Saturation = shapeOnly ? -100f : -5f;
                atmosphere.VignetteIntensity = 0f;
                float previousSun = RegionVolumePatch.SunAltitudeDegrees;
                try
                {
                    atmosphere.PrepareSun();
                    GameObject volume = atmosphere.Apply(stage.Camera, stage.Sun, 23f, stage.Assets.Add);
                    volume.transform.SetParent(stage.Root.transform, false);
                }
                finally { RegionVolumePatch.SunAltitudeDegrees = previousSun; }
                stage.Sky = RenderSettings.skybox;
                // The road kit uses URP/Lit, so use Unity fog for roads as well as terrain.
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = 0.00036f;
                RenderSettings.fogColor = new Color(0.64f, 0.72f, 0.79f);
                return stage;
            }
            catch
            {
                stage.Dispose();
                throw;
            }
        }

        private static Texture2D Surface(string name, string channel = "Color") =>
            AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureRoot}{name}_1K-JPG/{name}_1K-JPG_{channel}.jpg");

        private static RegionRoadMaterials CreateRoadMaterials(Stage stage, bool shapeOnly)
        {
            Texture2D asphalt = shapeOnly ? null : Surface("Asphalt010");
            Texture2D gravel = shapeOnly ? null : Surface("Ground007");
            return new RegionRoadMaterials
            {
                Asphalt = Lit(stage, "Asphalt", shapeOnly ? new Color(0.31f, 0.31f, 0.31f) : new Color(0.74f, 0.76f, 0.77f), asphalt),
                Shoulder = Lit(stage, "Paved shoulder", new Color(0.70f, 0.72f, 0.72f), asphalt),
                Gravel = Lit(stage, "Compacted gravel", new Color(0.69f, 0.68f, 0.64f), gravel),
                WhitePaint = Lit(stage, "White road paint", new Color(0.84f, 0.85f, 0.81f)),
                CenterPaint = Lit(stage, "Warm centerline paint", new Color(0.88f, 0.54f, 0.18f)),
                Concrete = Lit(stage, "Median concrete", new Color(0.58f, 0.58f, 0.55f)),
                Metal = Lit(stage, "Galvanized steel", new Color(0.58f, 0.61f, 0.62f), null, 0.36f),
                Delineator = Lit(stage, "White marker", new Color(0.82f, 0.84f, 0.83f)),
            };
        }

        private static Material Lit(Stage stage, string name, Color color, Texture2D texture = null, float metal = 0f)
        {
            var material = stage.Own(new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name });
            material.SetColor("_BaseColor", color);
            if (texture) material.SetTexture("_BaseMap", texture);
            material.SetFloat("_Smoothness", metal > 0f ? 0.35f : 0.11f);
            material.SetFloat("_Metallic", metal);
            if (metal > 0f) material.SetFloat("_Cull", (float)CullMode.Off);
            return material;
        }

        private static float[,] HeightMap(Stage stage, bool graded)
        {
            int resolution = stage.Detailed ? DetailedGroundResolution : GroundResolution;
            RegionRoadPlan plan = stage.Plan;
            var heights = new float[resolution, resolution];
            float stride = Span / (resolution - 1);
            Func<float, float, float> height = plan.NaturalHeight;
            if (graded) height = stage.Detailed ? (x, z) => RegionRoadMargin.GradedTerrain(plan, x, z) : plan.GroundHeight;
            Parallel.For(0, resolution, z =>
            {
                float worldZ = z * stride - RegionRoadPlan.Extent;
                for (int x = 0; x < resolution; x++)
                    heights[z, x] = height(x * stride - RegionRoadPlan.Extent, worldZ) / HeightRange;
            });
            return heights;
        }

        private static void AddGround(Stage stage, bool shapeOnly)
        {
            var data = stage.Own(new TerrainData
            {
                heightmapResolution = stage.Detailed ? DetailedGroundResolution : GroundResolution,
                alphamapResolution = stage.Detailed ? DetailedFieldResolution : FieldResolution,
                baseMapResolution = 1024,
                size = new Vector3(Span, HeightRange, Span),
            });
            data.SetHeights(0, 0, HeightMap(stage, true));
            Texture2D grass = shapeOnly ? Texture2D.whiteTexture : Surface("Grass005");
            Texture2D soil = shapeOnly ? null : Surface("Ground009");
            Texture2D normal = shapeOnly ? null : Surface("Grass005", "NormalGL");
            var verge = Layer(stage, grass, normal, shapeOnly ? new Color(0.48f, 0.48f, 0.48f) : new Color(0.68f, 0.82f, 0.60f), new Vector2(3.3f, 3.3f));
            if (shapeOnly)
                data.terrainLayers = new[] { verge };
            else
            {
                data.terrainLayers = new[]
                {
                    verge,
                    Layer(stage, CropRows(stage), null, new Color(0.88f, 0.96f, 0.82f), new Vector2(5.6f, 13f)),
                    Layer(stage, grass, normal, new Color(0.91f, 0.95f, 0.65f), new Vector2(2.7f, 6f)),
                    Layer(stage, soil, null, new Color(0.98f, 0.88f, 0.76f), new Vector2(2.4f, 7f)),
                };
                data.SetAlphamaps(0, 0, FieldCoverage(stage, true));
            }

            GameObject ground = Terrain.CreateTerrainGameObject(data);
            ground.name = "Farmland terrain";
            ground.transform.SetParent(stage.Root.transform, false);
            ground.transform.localPosition = new Vector3(-RegionRoadPlan.Extent, 0f, -RegionRoadPlan.Extent);
            stage.Terrain = ground.GetComponent<Terrain>();
            stage.Terrain.heightmapPixelError = 2f;
            stage.Terrain.basemapDistance = 630f;
            stage.Terrain.drawInstanced = true;
            stage.Terrain.materialTemplate = stage.Own(new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")));
        }

        private static Texture2D CropRows(Stage stage)
        {
            const int width = 128, length = 64;
            // Terrain Lit reads diffuse alpha as smoothness. Crop rows need only base colour.
            var texture = stage.Own(new Texture2D(width, length, TextureFormat.RGB24, true)
            {
                name = "Crop rows", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4,
            });
            var pixels = new Color[width * length];
            for (int z = 0; z < length; z++)
            for (int x = 0; x < width; x++)
            {
                float phase = x / 32f + 0.045f * Mathf.Sin(z * 2f * Mathf.PI / length + x * 2f * Mathf.PI / width);
                float row = 1f - RegionNoise.Smooth(0.15f, 0.36f, Mathf.Abs(phase - Mathf.Round(phase)));
                float leaf = 0.88f + 0.24f * RegionNoise.Hash01(x, z, 3213u);
                pixels[x + z * width] = Color.Lerp(new Color(0.28f, 0.28f, 0.15f), new Color(0.37f, 0.47f, 0.19f), row) * leaf;
            }
            texture.SetPixels(pixels);
            texture.Apply(true, true);
            return texture;
        }

        private static TerrainLayer Layer(Stage stage, Texture2D color, Texture2D normal, Color tint, Vector2 size) =>
            stage.Own(new TerrainLayer
            {
                diffuseTexture = color, normalMapTexture = normal, normalScale = 0.42f,
                tileSize = size, diffuseRemapMax = tint, smoothness = 0.06f,
            });

        private static float[,,] FieldCoverage(Stage stage, bool roads)
        {
            RegionRoadPlan plan = stage.Plan;
            int resolution = stage.Detailed ? DetailedFieldResolution : FieldResolution;
            var weights = new float[resolution, resolution, 4];
            float stride = Span / (resolution - 1);
            void PaintRow(int z)
            {
                for (int x = 0; x < resolution; x++)
                {
                    var world = new NVector2(x * stride - RegionRoadPlan.Extent, z * stride - RegionRoadPlan.Extent);
                    float row = world.Y / RegionRoadPlan.FieldDepth + world.X * RegionRoadPlan.FieldSkew;
                    int parcelRow = Mathf.FloorToInt(row);
                    float u = RegionRoadMargin.FieldColumn(world.X, world.Y, parcelRow, (uint)plan.Seed, out _);
                    int parcelCol = Mathf.FloorToInt(u);
                    float boundary = Mathf.Min(1f - Mathf.Abs(2f * (u - parcelCol) - 1f),
                        1f - Mathf.Abs(2f * (row - parcelRow) - 1f));
                    float extent = Mathf.Abs(world.X + 65f * Mathf.Sin(world.Y / 680f));
                    float field = Mathf.SmoothStep(0f, 1f, boundary * 8f) * (1f - Mathf.SmoothStep(0f, 1f, (extent - 415f) / 730f));
                    var plotDelta = world - plan.Site.PlotCenter;
                    float soil = 0f;
                    if (roads)
                    {
                        RegionRoadCorridor verge = plan.Highway;
                        bool margin = verge.TryProject(world, out RegionRoadStation section, out float offset);
                        if (!margin)
                        {
                            verge = plan.Arterial;
                            margin = verge.TryProject(world, out section, out offset);
                        }
                        if (margin || plotDelta.LengthSquared() < 53f * 53f) field = 0f;
                        if (stage.Detailed && margin && !RegionRoadMargin.InSite(plan.Site, world, 0.7f))
                        {
                            float edge = Mathf.Abs(offset) - verge.Profile.HalfWidth;
                            float bare = 0.42f + 0.58f * RegionNoise.Smooth(-0.18f, 0.28f,
                                RegionNoise.Fbm(section.Distance / 14f, offset * 0.19f, 2, (uint)plan.Seed));
                            soil = 0.69f * bare * (1f - RegionNoise.Smooth(0.9f, 4f, Mathf.Abs(edge - RegionRoadMargin.DitchCenter)));
                        }
                    }
                    float choice = RegionNoise.Hash01(parcelCol, parcelRow, (uint)plan.Seed);
                    int layer = choice < 0.45f ? 1 : choice < 0.77f ? 2 : 3;
                    weights[z, x, 0] = 1f - field - soil;
                    weights[z, x, layer] = field;
                    weights[z, x, 3] += soil;
                }
            }
            if (stage.Detailed) Parallel.For(0, resolution, PaintRow);
            else for (int z = 0; z < resolution; z++) PaintRow(z);
            return weights;
        }

        private static Vector3 Position(NVector2 p, float y) => new Vector3(p.X, y, p.Y);

        private static void Place(Stage stage, int view)
        {
            RegionRoadPlan plan = stage.Plan;
            RegionRoadsideSite site = plan.Site;
            float z = site.Entrance.Y;
            Vector3 eye, focus;
            float fieldOfView;
            switch (view)
            {
                case 1:
                    eye = Position(site.PlotCenter + site.Outward * 42f - site.Station.Forward * 111f, site.PlotElevation + 44f);
                    focus = Position(site.Entrance - site.Outward * 58f + site.Station.Forward * 27f, site.EntranceElevation + 3f);
                    fieldOfView = 40f;
                    break;
                case 2:
                    NVector2 shoulder = site.Entrance - site.Outward * 0.6f - site.Station.Forward * 18f;
                    eye = Position(shoulder, site.EntranceElevation + 1.8f);
                    focus = Position(site.Entrance + site.Outward * 20f + site.Station.Forward * 6f, site.PlotElevation + 0.8f);
                    fieldOfView = 57f;
                    break;
                case 3:
                    float d = plan.Highway.Length * 0.44f;
                    RegionRoadStation start = plan.Highway.At(d), ahead = plan.Highway.At(d + 75f);
                    eye = Position(start.AtOffset(plan.Highway.Profile.OuterLaneEdge + 0.8f), start.Elevation + 1.65f);
                    focus = Position(ahead.AtOffset(-3f), ahead.Elevation + 1.9f);
                    fieldOfView = 55f;
                    break;
                default:
                    eye = new Vector3(site.Station.Center.X - 122f, site.EntranceElevation + 67f, z - 427f);
                    focus = new Vector3(10f, site.EntranceElevation + 4f, z + 485f);
                    fieldOfView = 42f;
                    break;
            }
            stage.Camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(focus - eye));
            stage.Camera.fieldOfView = fieldOfView;
        }

        public static void CaptureFromCommandLine()
        {
            int code = 0;
            try
            {
                string output = RegionMapMenu.Argument("-roadOut", "artifacts/region-map/highway-corridor");
                int chosenSeed = int.Parse(RegionMapMenu.Argument("-roadSeed", "8"), CultureInfo.InvariantCulture);
                string mode = RegionMapMenu.Argument("-roadReview", "full");
                int pc = Array.IndexOf(QualitySettings.names, "PC");
                if (pc >= 0) QualitySettings.SetQualityLevel(pc, true);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Directory.CreateDirectory(output);
                if (mode == "margin-shape")
                    CaptureMarginShape(output, chosenSeed);
                else if (mode == "margin-review")
                {
                    CaptureMarginShape(Path.Combine(output, "shape"), chosenSeed);
                    CaptureMargins(output, chosenSeed, RegionRoadPreset.Country, RoadAppearance.Original, RoadAppearance.Graded);
                    CaptureMargins(output, unchecked(chosenSeed + 11), RegionRoadPreset.Sweeping, RoadAppearance.Original, RoadAppearance.Graded);
                }
                else if (mode == "roadside-review")
                {
                    CaptureMargins(output, chosenSeed, RegionRoadPreset.Country, RoadAppearance.Graded, RoadAppearance.Landscaped);
                    CaptureMargins(output, unchecked(chosenSeed + 11), RegionRoadPreset.Sweeping, RoadAppearance.Graded, RoadAppearance.Landscaped);
                }
                else if (mode == "entrance-review")
                {
                    CaptureMargins(output, chosenSeed, RegionRoadPreset.Country, RoadAppearance.Landscaped, RoadAppearance.Footing, 2);
                    CaptureMargins(output, unchecked(chosenSeed + 11), RegionRoadPreset.Sweeping, RoadAppearance.Landscaped, RoadAppearance.Footing, 2);
                }
                else if (mode == "edge-review")
                {
                    CaptureMargins(output, chosenSeed, RegionRoadPreset.Country, RoadAppearance.Footing, RoadAppearance.WornEdge, 2);
                    CaptureMargins(output, unchecked(chosenSeed + 11), RegionRoadPreset.Sweeping, RoadAppearance.Footing, RoadAppearance.WornEdge, 2);
                    CaptureVariedGravel(output, chosenSeed);
                }
                else if (mode == "draft")
                {
                    using (Stage stage = Build(chosenSeed, RegionRoadPreset.Country, true))
                    {
                        VerifyRoadSurface(stage);
                        CaptureViews(stage, output, "shape", 2);
                    }
                    using (Stage stage = Build(chosenSeed, RegionRoadPreset.Country, false))
                        CaptureViews(stage, output, "draft", Views.Length);
                }
                else if (mode == "full")
                {
                    CaptureCase(output, chosenSeed, RegionRoadPreset.Country, true);
                    CaptureCase(output, unchecked(chosenSeed + 11), RegionRoadPreset.Sweeping, true);
                    CaptureCase(output, chosenSeed, RegionRoadPreset.Wide, false);
                }
                else throw new ArgumentException($"Unknown -roadReview mode: {mode}");
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        private static void CaptureCase(string output, int seed, RegionRoadPreset preset, bool comparison)
        {
            string folder = Path.Combine(output, $"{preset.ToString().ToLowerInvariant()}-{seed}");
            Directory.CreateDirectory(folder);
            var watch = Stopwatch.StartNew();
            using (Stage stage = Build(seed, preset, false))
            {
                double setup = watch.Elapsed.TotalMilliseconds;
                int checks = VerifyRoadSurface(stage);
                Place(stage, 0);
                RegionTerrainViewMenu.Capture(stage.Camera, null, null);
                CaptureViews(stage, folder, "road", Views.Length);
                if (comparison)
                {
                    foreach (float sun in new[] { 60f, 38f, 20f, 10f })
                    {
                        stage.Sun.transform.rotation = RegionTerrainView.SunRotation(sun);
                        CaptureViews(stage, folder, "sun-" + sun.ToString("00", CultureInfo.InvariantCulture), 2);
                    }
                    stage.Sun.transform.rotation = RegionTerrainView.SunRotation(38f);
                    stage.Roads.SetActive(false);
                    stage.Terrain.terrainData.SetHeights(0, 0, HeightMap(stage, false));
                    stage.Terrain.terrainData.SetAlphamaps(0, 0, FieldCoverage(stage, false));
                    CaptureViews(stage, folder, "natural", Views.Length);
                    stage.Terrain.terrainData.SetHeights(0, 0, HeightMap(stage, true));
                    stage.Terrain.terrainData.SetAlphamaps(0, 0, FieldCoverage(stage, true));
                    stage.Roads.SetActive(true);
                }
                string report = FormattableString.Invariant($"{preset} seed={seed} setup_ms={setup:F1} highway_m={stage.Plan.Highway.Length:F1} arterial_m={stage.Plan.Arterial.Length:F1} surface_checks={checks}\n");
                report += Measure(stage);
                File.WriteAllText(Path.Combine(folder, "performance.txt"), report);
                Debug.Log("[RegionMap] " + report);
            }
        }

        private static int VerifyRoadSurface(Stage stage)
        {
            Physics.SyncTransforms();
            int checks = 0;
            foreach (RegionRoadCorridor road in new[] { stage.Plan.Highway, stage.Plan.Arterial })
            {
                for (int i = 1; i < 20; i++)
                {
                    RegionRoadStation station = road.At(road.Length * i / 20f);
                    float lane = road.Profile.OuterLaneEdge - 0.4f;
                    float shoulder = road.Profile.HalfWidth - 0.28f;
                    foreach (float offset in new[] { -shoulder, -lane, lane, shoulder })
                        CheckSurface(station.AtOffset(offset), station.SurfaceAt(offset, road.Profile), road.Id);
                }
            }

            RegionRoadsideSite site = stage.Plan.Site;
            foreach (float reach in new[] { 0.8f, 4f, 8f, 12f, 20f, 25f, 28f, 45f, 67f })
            {
                float along = reach > RegionRoadsideSite.Setback ? RegionRoadsideSite.Frontage * 0.5f - 1f : 2.5f;
                foreach (float offset in new[] { -along, 0f, along })
                    CheckSurface(site.Entrance + site.Outward * reach + site.Station.Forward * offset,
                        site.ApproachElevation(reach), site.Id);
            }
            return checks;

            void CheckSurface(NVector2 point, float expectedY, string label)
            {
                if (!Physics.Raycast(Position(point, expectedY + 10f), Vector3.down, out RaycastHit hit, 15f) ||
                    !hit.collider.transform.IsChildOf(stage.Roads.transform) || Mathf.Abs(hit.point.y - expectedY) > 0.14f)
                    throw new InvalidOperationException(FormattableString.Invariant($"{label} surface at {point.X:F2},{point.Y:F2}: expected {expectedY:F2}, hit {hit.collider?.name ?? "nothing"} {hit.point.y:F2}"));
                checks++;
            }
        }

        private static void CaptureViews(Stage stage, string folder, string prefix, int endView, int firstView = 0)
        {
            for (int view = firstView; view < endView; view++)
            {
                Place(stage, view);
                RegionTerrainViewMenu.Capture(stage.Camera, folder, $"{prefix}-{Views[view].ToLowerInvariant()}.png");
            }
        }

        private static string SampleMidSection(Stage stage)
        {
            Physics.SyncTransforms();
            RegionRoadsideSite site = stage.Plan.Site;
            RegionRoadCorridor road = site.Road;
            RegionRoadStation station = road.At(site.Station.Distance + 32f);
            float pavement = station.SurfaceAt(road.Profile.HalfWidth, road.Profile);
            var terrain = stage.Terrain.GetComponent<TerrainCollider>();
            var result = new StringBuilder("offset_m,terrain_y_relative_m,top_y_relative_m,world_x,world_z\n");
            for (float offset = -2f; offset <= 24f; offset += 0.25f)
            {
                NVector2 point = station.AtOffset(-road.Profile.HalfWidth - offset);
                var ray = new Ray(Position(point, pavement + 12f), Vector3.down);
                if (!terrain.Raycast(ray, out RaycastHit ground, 24f) || !Physics.Raycast(ray, out RaycastHit top, 24f))
                    throw new InvalidOperationException("The mid-distance road section missed terrain.");
                result.AppendLine(FormattableString.Invariant($"{offset:F2},{ground.point.y - pavement:F3},{top.point.y - pavement:F3},{point.X:F3},{point.Y:F3}"));
            }
            return result.ToString();
        }

        private static string Measure(Stage stage)
        {
            var report = new StringBuilder("view\troad mesh off ms\troad mesh on ms\tdelta ms (CPU and GPU sync, 1600x900 MSAA4)\n");
            var target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var pixel = new Texture2D(1, 1, TextureFormat.RGB24, false);
            var samples = new[] { new double[12], new double[12] };
            RenderTexture previous = RenderTexture.active;
            try
            {
                stage.Camera.targetTexture = target;
                for (int view = 0; view < Views.Length; view++)
                {
                    Place(stage, view);
                    for (int repeat = 0; repeat < 4; repeat++)
                    for (int state = 0; state < 2; state++)
                    {
                        stage.Roads.SetActive(state == 1);
                        TimedDraw(stage.Camera, target, pixel);
                        for (int i = 0; i < 3; i++) samples[state][repeat * 3 + i] = TimedDraw(stage.Camera, target, pixel);
                    }
                    Array.Sort(samples[0]);
                    Array.Sort(samples[1]);
                    double off = (samples[0][5] + samples[0][6]) * 0.5, on = (samples[1][5] + samples[1][6]) * 0.5;
                    report.AppendLine(FormattableString.Invariant($"{Views[view]}\t{off:F2}\t{on:F2}\t{on - off:F2}"));
                }
            }
            finally
            {
                stage.Camera.targetTexture = null;
                stage.Roads.SetActive(true);
                RenderTexture.active = previous;
                target.Release();
                DestroyImmediate(target);
                DestroyImmediate(pixel);
            }
            return report.ToString();
        }

        private static double TimedDraw(Camera camera, RenderTexture target, Texture2D pixel)
        {
            var watch = Stopwatch.StartNew();
            camera.Render();
            RenderTexture.active = target;
            pixel.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
            RenderTexture.active = null;
            return watch.Elapsed.TotalMilliseconds;
        }
    }
}

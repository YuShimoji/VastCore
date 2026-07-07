using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vastcore.Editor.DesignerCockpit
{
    public class VastCoreDesignerCockpitWindow : EditorWindow
    {
        private const string DefaultSessionFolder = "Assets/Data/VastCore/DesignerSessions";
        private const string WindowTitle = "VastCore Designer Cockpit";

        private static readonly string[] ModeLabels =
        {
            "Overview",
            "Random Variation",
            "Terrain",
            "Composition",
            "Deform",
            "Diagnostics"
        };

        private VastCoreDesignerSession session;
        private Vector2 scrollPosition;
        private StatusSnapshot status = StatusSnapshot.CreateEmpty();
        private string lastOperationMessage = "Ready.";
        private DesignerCockpitMode selectedMode = DesignerCockpitMode.Overview;
        private bool showRandomAdvanced;
        private bool showDiagnosticsDrawer;

        [MenuItem("Tools/VastCore/Designer Cockpit")]
        public static void ShowWindow()
        {
            var window = GetWindow<VastCoreDesignerCockpitWindow>(WindowTitle);
            window.minSize = new Vector2(440f, 580f);
            window.RefreshStatus();
        }

        private void OnEnable()
        {
            if (session == null)
            {
                CreateNewSession();
            }

            RefreshStatus();
        }

        private void OnSelectionChange()
        {
            RefreshStatus();
            Repaint();
        }

        private void OnGUI()
        {
            if (session == null)
            {
                CreateNewSession();
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawTopSummary();
            DrawPrimaryActions();
            DrawModeSelector();
            DrawContextPanel();

            EditorGUILayout.EndScrollView();
        }

        private void DrawTopSummary()
        {
            EditorGUILayout.LabelField(WindowTitle, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawSummaryRow("Session", session.sessionName);
            DrawSummaryRow("Seed", session.seed.ToString());
            DrawSummaryRow("Selected", status.selectedObjectCount.ToString());
            DrawSummaryRow("Last Saved", session.lastSavedUtc);
            DrawSummaryRow("Loaded", GetSessionAccessLabel());

            EditorGUILayout.Space(2f);
            DrawStatusBadges();

            if (!string.IsNullOrWhiteSpace(lastOperationMessage))
            {
                EditorGUILayout.Space(2f);
                EditorGUILayout.LabelField(lastOperationMessage, EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSummaryRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel, GUILayout.Width(78f));
            EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(value) ? "-" : value, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatusBadges()
        {
            EditorGUILayout.BeginHorizontal();
            DrawBadge("Random", status.randomControl.state);
            DrawBadge("Terrain", status.terrain.state);
            DrawBadge("Compose", status.composition.state);
            DrawBadge("Deform", status.deform.state);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBadge(string label, string state)
        {
            EditorGUILayout.LabelField($"{label}: {state}", EditorStyles.miniBoldLabel, GUILayout.MinWidth(84f));
        }

        private void DrawPrimaryActions()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New", GUILayout.MinWidth(58f)))
            {
                CreateNewSession();
            }

            if (GUILayout.Button("Save", GUILayout.MinWidth(58f)))
            {
                SaveSession();
            }

            if (GUILayout.Button("Load", GUILayout.MinWidth(58f)))
            {
                LoadSessionFromProjectFile();
            }

            if (GUILayout.Button("Refresh", GUILayout.MinWidth(70f)))
            {
                RefreshStatus();
            }

            EditorGUI.BeginDisabledGroup(status.selectedObjectCount == 0);
            if (GUILayout.Button("Apply", GUILayout.MinWidth(68f)))
            {
                ApplyRandomTransformToSelected();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (status.selectedObjectCount == 0)
            {
                EditorGUILayout.LabelField("Select a scene object to apply Random Variation.", EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawModeSelector()
        {
            EditorGUILayout.Space(6f);
            selectedMode = (DesignerCockpitMode)GUILayout.Toolbar((int)selectedMode, ModeLabels);

            if (selectedMode == DesignerCockpitMode.Diagnostics)
            {
                showDiagnosticsDrawer = true;
            }
        }

        private void DrawContextPanel()
        {
            EditorGUILayout.Space(6f);

            switch (selectedMode)
            {
                case DesignerCockpitMode.Overview:
                    DrawOverviewPanel();
                    break;
                case DesignerCockpitMode.RandomVariation:
                    DrawRandomVariationPanel();
                    break;
                case DesignerCockpitMode.Terrain:
                    DrawFeaturePanel(
                        "Terrain",
                        status.terrain,
                        "Terrain generation is not connected to this cockpit yet.",
                        "Use Diagnostics to inspect parameter-type detection before a preview harness is wired.");
                    break;
                case DesignerCockpitMode.Composition:
                    DrawFeaturePanel(
                        "Composition",
                        status.composition,
                        "Composition is status-only from this cockpit.",
                        "CSG and Blend verification stay in the Structure Generator flow until evidence is captured.");
                    break;
                case DesignerCockpitMode.Deform:
                    DrawFeaturePanel(
                        "Deform",
                        status.deform,
                        "Deform is status-only from this cockpit.",
                        "Package symbols and runtime operation are not executed by this cockpit.");
                    break;
                case DesignerCockpitMode.Diagnostics:
                    DrawDiagnosticsContent();
                    break;
            }

            if (selectedMode != DesignerCockpitMode.Diagnostics)
            {
                DrawDiagnosticsDrawer();
            }
        }

        private void DrawOverviewPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Session", EditorStyles.boldLabel);
            DrawSessionAssetField();

            session.sessionName = EditorGUILayout.TextField("Name", session.sessionName);
            session.seed = EditorGUILayout.IntField("Seed", session.seed);
            session.notes = EditorGUILayout.TextArea(session.notes, GUILayout.MinHeight(42f));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Capability", EditorStyles.boldLabel);
            DrawCompactCapability("Random Variation", status.randomControl, "Apply available from selected scene objects.");
            DrawCompactCapability("Terrain", status.terrain, "Preview/generation not connected.");
            DrawCompactCapability("Composition", status.composition, "Status-only.");
            DrawCompactCapability("Deform", status.deform, "Status-only.");
            EditorGUILayout.EndVertical();
        }

        private void DrawSessionAssetField()
        {
            var loadedSession = (VastCoreDesignerSession)EditorGUILayout.ObjectField(
                "Session Asset",
                session,
                typeof(VastCoreDesignerSession),
                false);

            if (loadedSession != null && loadedSession != session)
            {
                session = loadedSession;
                lastOperationMessage = $"Loaded session asset: {session.name}";
                RefreshStatus();
            }
        }

        private void DrawCompactCapability(string label, FeatureStatus featureStatus, string copy)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel, GUILayout.Width(118f));
            EditorGUILayout.LabelField(featureStatus.state, EditorStyles.miniBoldLabel, GUILayout.Width(110f));
            EditorGUILayout.LabelField(copy, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRandomVariationPanel()
        {
            RandomTransformRecipe recipe = EnsureRandomRecipe();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Random Variation", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                status.selectedObjectCount == 0
                    ? "Select a scene object to apply this recipe."
                    : $"Ready for {status.selectedObjectCount} selected object(s).",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(4f);
            float positionSpread = GetHorizontalSpread(recipe);
            float newPositionSpread = EditorGUILayout.Slider("Position Spread", positionSpread, 0f, 50f);
            if (!Mathf.Approximately(positionSpread, newPositionSpread))
            {
                SetHorizontalSpread(recipe, newPositionSpread);
            }

            bool yLocked = IsYLocked(recipe);
            bool newYLocked = EditorGUILayout.Toggle("Y Lock", yLocked);
            if (newYLocked != yLocked)
            {
                SetYLock(recipe, newYLocked);
            }

            if (!newYLocked)
            {
                float heightVariation = GetHeightVariation(recipe);
                float newHeightVariation = EditorGUILayout.Slider("Height Variation", heightVariation, 0f, 20f);
                if (!Mathf.Approximately(heightVariation, newHeightVariation))
                {
                    SetHeightVariation(recipe, newHeightVariation);
                }
            }

            float yawVariation = GetYawVariation(recipe);
            float newYawVariation = EditorGUILayout.Slider("Yaw Variation", yawVariation, 0f, 360f);
            if (!Mathf.Approximately(yawVariation, newYawVariation))
            {
                SetYawVariation(recipe, newYawVariation);
            }

            float scaleVariation = GetScaleVariationPercent(recipe);
            float newScaleVariation = EditorGUILayout.Slider("Scale Variation (%)", scaleVariation, 0f, 100f);
            if (!Mathf.Approximately(scaleVariation, newScaleVariation))
            {
                SetScaleVariationPercent(recipe, newScaleVariation);
            }

            recipe.useRelativePosition = EditorGUILayout.Toggle("Relative Position", recipe.useRelativePosition);
            recipe.useUniformScale = EditorGUILayout.Toggle("Uniform Scale", recipe.useUniformScale);

            showRandomAdvanced = EditorGUILayout.Foldout(showRandomAdvanced, "Advanced ranges", true);
            if (showRandomAdvanced)
            {
                EditorGUI.indentLevel++;
                recipe.positionMin = EditorGUILayout.Vector3Field("Position Min", recipe.positionMin);
                recipe.positionMax = EditorGUILayout.Vector3Field("Position Max", recipe.positionMax);
                recipe.rotationMin = EditorGUILayout.Vector3Field("Rotation Min", recipe.rotationMin);
                recipe.rotationMax = EditorGUILayout.Vector3Field("Rotation Max", recipe.rotationMax);
                recipe.scaleMin = EditorGUILayout.Vector3Field("Scale Min", recipe.scaleMin);
                recipe.scaleMax = EditorGUILayout.Vector3Field("Scale Max", recipe.scaleMax);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFeaturePanel(string label, FeatureStatus featureStatus, string shortCopy, string nextCopy)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            DrawSummaryRow("State", featureStatus.state);
            EditorGUILayout.LabelField(featureStatus.summary, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(shortCopy, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField(nextCopy, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawDiagnosticsDrawer()
        {
            EditorGUILayout.Space(6f);
            showDiagnosticsDrawer = EditorGUILayout.Foldout(showDiagnosticsDrawer, "Diagnostics", true);
            if (showDiagnosticsDrawer)
            {
                DrawDiagnosticsContent();
            }
        }

        private void DrawDiagnosticsContent()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);
            DrawSummaryRow("Session Path", GetSessionAssetPathOrUnsaved());
            DrawSummaryRow("Selection", status.selectedObjectCount.ToString());
            DrawSummaryRow("Last Operation", lastOperationMessage);

            EditorGUILayout.Space(4f);
            DrawDiagnosticStatus("RandomControl", status.randomControl, session.randomControlStatusNote);
            DrawDiagnosticStatus("Composition", status.composition, session.compositionStatusNote);
            DrawDiagnosticStatus("Deform", status.deform, session.deformStatusNote);
            DrawDiagnosticStatus("Terrain", status.terrain, session.terrainStatusNote);
            DrawDiagnosticStatus("Topology / DualGrid", status.topology, session.topologyStatusNote);
            EditorGUILayout.EndVertical();
        }

        private void DrawDiagnosticStatus(string label, FeatureStatus featureStatus, string note)
        {
            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField($"{label}: {featureStatus.state}", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(featureStatus.summary, EditorStyles.wordWrappedMiniLabel);
            if (!string.IsNullOrWhiteSpace(featureStatus.detail))
            {
                EditorGUILayout.LabelField(featureStatus.detail, EditorStyles.wordWrappedMiniLabel);
            }

            if (!string.IsNullOrWhiteSpace(note))
            {
                EditorGUILayout.LabelField(note, EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void CreateNewSession()
        {
            session = CreateInstance<VastCoreDesignerSession>();
            session.ResetDefaults();
            selectedMode = DesignerCockpitMode.Overview;
            lastOperationMessage = "New unsaved session created.";
        }

        private void SaveSession()
        {
            if (session == null)
            {
                CreateNewSession();
            }

            EnsureSessionFolder();
            session.lastSavedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            string path = AssetDatabase.GetAssetPath(session);
            if (string.IsNullOrEmpty(path))
            {
                string safeName = MakeSafeFileName(session.sessionName);
                path = AssetDatabase.GenerateUniqueAssetPath($"{DefaultSessionFolder}/{safeName}.asset");
                AssetDatabase.CreateAsset(session, path);
            }

            EditorUtility.SetDirty(session);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = session;
            lastOperationMessage = $"Saved session: {path}";
            RefreshStatus();
        }

        private void LoadSessionFromProjectFile()
        {
            EnsureSessionFolder();
            string absolutePath = EditorUtility.OpenFilePanel("Load Designer Session", DefaultSessionFolder, "asset");
            if (string.IsNullOrEmpty(absolutePath))
            {
                return;
            }

            string projectPath = ToProjectPath(absolutePath);
            if (string.IsNullOrEmpty(projectPath))
            {
                EditorUtility.DisplayDialog("Load Session", "Choose a session asset inside this Unity project.", "OK");
                return;
            }

            VastCoreDesignerSession loadedSession = AssetDatabase.LoadAssetAtPath<VastCoreDesignerSession>(projectPath);
            if (loadedSession == null)
            {
                EditorUtility.DisplayDialog("Load Session", "The selected asset is not a VastCoreDesignerSession.", "OK");
                return;
            }

            session = loadedSession;
            Selection.activeObject = session;
            lastOperationMessage = $"Loaded session: {projectPath}";
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            bool randomControlTabFound = IsTypeAvailable("Vastcore.Editor.Generation.RandomControlTab");
            bool compositionTabFound = IsTypeAvailable("Vastcore.Editor.Generation.CompositionTab");
            bool deformerTabFound = IsTypeAvailable("Vastcore.Editor.StructureGenerator.Tabs.DeformerTab");
            bool terrainParamsFound = IsTypeAvailable("Vastcore.Generation.UnifiedTerrainParams") ||
                                      IsTypeAvailable("Vastcore.Generation.TerrainParamsConverter");
            bool legacyTerrainParamsFound = IsTypeAvailable("Vastcore.Terrain.UnifiedTerrainParams") ||
                                            IsTypeAvailable("Vastcore.Terrain.TerrainParamsConverter");
            bool topologyFound = IsTypeAvailable("Vastcore.Terrain.DualGrid.GridTopology");

            status = new StatusSnapshot
            {
                selectedObjectCount = Selection.gameObjects?.Length ?? 0,
                randomControl = FeatureStatus.Ready(
                    "Cockpit-owned deterministic random variation is available.",
                    randomControlTabFound
                        ? "RandomControlTab detected at Vastcore.Editor.Generation.RandomControlTab."
                        : "RandomControlTab was not detected; cockpit random variation remains standalone."),
                composition = compositionTabFound
                    ? FeatureStatus.Untested(
                        "CompositionTab detected; CSG/Blend execution is not run from this cockpit.",
                        "Detected at Vastcore.Editor.Generation.CompositionTab.")
                    : FeatureStatus.Missing(
                        "CompositionTab was not detected in loaded editor assemblies.",
                        "Expected Vastcore.Editor.Generation.CompositionTab."),
                deform = deformerTabFound
                    ? FeatureStatus.Untested(
                        "DeformerTab detected; package/runtime behavior is not run from this cockpit.",
                        "Detected at Vastcore.Editor.StructureGenerator.Tabs.DeformerTab.")
                    : FeatureStatus.Missing(
                        "DeformerTab was not detected in loaded editor assemblies.",
                        "Expected Vastcore.Editor.StructureGenerator.Tabs.DeformerTab."),
                terrain = CreateTerrainStatus(terrainParamsFound, legacyTerrainParamsFound),
                topology = topologyFound
                    ? FeatureStatus.Placeholder(
                        "DualGrid topology type detected; cockpit preview is future-only.",
                        "Detected at Vastcore.Terrain.DualGrid.GridTopology.")
                    : FeatureStatus.Placeholder(
                        "DualGrid topology slot is future-only.",
                        "GridTopology was not detected in loaded assemblies.")
            };

            Repaint();
        }

        private FeatureStatus CreateTerrainStatus(bool terrainParamsFound, bool legacyTerrainParamsFound)
        {
            if (terrainParamsFound)
            {
                string detail = legacyTerrainParamsFound
                    ? "Detected current Vastcore.Generation.* and legacy Vastcore.Terrain.* terrain parameter paths."
                    : "Detected current Vastcore.Generation.* terrain parameter path. The old Vastcore.Terrain.* lookup does not resolve in this branch.";

                return FeatureStatus.Partial(
                    "Terrain parameter types are available; generation is not connected to this cockpit.",
                    detail);
            }

            if (legacyTerrainParamsFound)
            {
                return FeatureStatus.DetectionMismatch(
                    "Only the legacy terrain parameter lookup resolved.",
                    "Check namespace expectations before wiring Terrain Preview.");
            }

            return FeatureStatus.Missing(
                "Terrain parameter integration types were not detected.",
                "Checked Vastcore.Generation.* and Vastcore.Terrain.* terrain parameter paths.");
        }

        private void ApplyRandomTransformToSelected()
        {
            GameObject[] selectedObjects = Selection.gameObjects?
                .Where(go => go != null)
                .OrderBy(go => go.name)
                .ThenBy(go => go.GetInstanceID())
                .ToArray() ?? Array.Empty<GameObject>();

            if (selectedObjects.Length == 0)
            {
                lastOperationMessage = "Select a scene object to apply Random Variation.";
                return;
            }

            RandomTransformRecipe recipe = EnsureRandomRecipe();
            var random = new System.Random(session.seed);
            Transform[] transforms = selectedObjects.Select(go => go.transform).ToArray();
            Undo.RecordObjects(transforms, "VastCore Designer Random Transform");

            foreach (GameObject go in selectedObjects)
            {
                ApplyTransformRecipe(go.transform, recipe, random);
                EditorUtility.SetDirty(go.transform);
                if (go.scene.IsValid())
                {
                    EditorSceneManager.MarkSceneDirty(go.scene);
                }
            }

            lastOperationMessage = $"Applied random variation to {selectedObjects.Length} selected object(s).";
            RefreshStatus();
            SceneView.RepaintAll();
        }

        private RandomTransformRecipe EnsureRandomRecipe()
        {
            RandomTransformRecipe recipe = session.randomTransform ?? new RandomTransformRecipe();
            session.randomTransform = recipe;
            return recipe;
        }

        private static void ApplyTransformRecipe(Transform transform, RandomTransformRecipe recipe, System.Random random)
        {
            Vector3 position = RandomVector(random, recipe.positionMin, recipe.positionMax);
            Vector3 rotation = RandomVector(random, recipe.rotationMin, recipe.rotationMax);
            Vector3 scale = recipe.useUniformScale
                ? Vector3.one * RandomRange(random, recipe.scaleMin.x, recipe.scaleMax.x)
                : RandomVector(random, recipe.scaleMin, recipe.scaleMax);

            if (recipe.useRelativePosition)
            {
                transform.localPosition += position;
            }
            else
            {
                transform.position = position;
            }

            transform.localRotation = Quaternion.Euler(rotation);
            transform.localScale = scale;
        }

        private static Vector3 RandomVector(System.Random random, Vector3 min, Vector3 max)
        {
            return new Vector3(
                RandomRange(random, min.x, max.x),
                RandomRange(random, min.y, max.y),
                RandomRange(random, min.z, max.z));
        }

        private static float RandomRange(System.Random random, float min, float max)
        {
            if (max < min)
            {
                float temp = min;
                min = max;
                max = temp;
            }

            return (float)(min + (max - min) * random.NextDouble());
        }

        private static float GetHorizontalSpread(RandomTransformRecipe recipe)
        {
            return Mathf.Max(
                Mathf.Abs(recipe.positionMin.x),
                Mathf.Abs(recipe.positionMax.x),
                Mathf.Abs(recipe.positionMin.z),
                Mathf.Abs(recipe.positionMax.z));
        }

        private static void SetHorizontalSpread(RandomTransformRecipe recipe, float spread)
        {
            recipe.positionMin.x = -spread;
            recipe.positionMin.z = -spread;
            recipe.positionMax.x = spread;
            recipe.positionMax.z = spread;
        }

        private static bool IsYLocked(RandomTransformRecipe recipe)
        {
            return Mathf.Approximately(recipe.positionMin.y, 0f) &&
                   Mathf.Approximately(recipe.positionMax.y, 0f);
        }

        private static void SetYLock(RandomTransformRecipe recipe, bool locked)
        {
            if (locked)
            {
                recipe.positionMin.y = 0f;
                recipe.positionMax.y = 0f;
            }
            else if (IsYLocked(recipe))
            {
                SetHeightVariation(recipe, 1f);
            }
        }

        private static float GetHeightVariation(RandomTransformRecipe recipe)
        {
            return Mathf.Max(Mathf.Abs(recipe.positionMin.y), Mathf.Abs(recipe.positionMax.y));
        }

        private static void SetHeightVariation(RandomTransformRecipe recipe, float heightVariation)
        {
            recipe.positionMin.y = -heightVariation;
            recipe.positionMax.y = heightVariation;
        }

        private static float GetYawVariation(RandomTransformRecipe recipe)
        {
            return Mathf.Max(Mathf.Abs(recipe.rotationMin.y), Mathf.Abs(recipe.rotationMax.y));
        }

        private static void SetYawVariation(RandomTransformRecipe recipe, float yawVariation)
        {
            recipe.rotationMin = Vector3.zero;
            recipe.rotationMax = new Vector3(0f, yawVariation, 0f);
        }

        private static float GetScaleVariationPercent(RandomTransformRecipe recipe)
        {
            float lower = Mathf.Abs(1f - recipe.scaleMin.x);
            float upper = Mathf.Abs(recipe.scaleMax.x - 1f);
            return Mathf.Max(lower, upper) * 100f;
        }

        private static void SetScaleVariationPercent(RandomTransformRecipe recipe, float percent)
        {
            float delta = percent / 100f;
            float min = Mathf.Max(0.01f, 1f - delta);
            float max = 1f + delta;
            recipe.scaleMin = new Vector3(min, min, min);
            recipe.scaleMax = new Vector3(max, max, max);
        }

        private static bool IsTypeAvailable(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetType(fullName) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private string GetSessionAccessLabel()
        {
            string path = GetSessionAssetPath();
            return string.IsNullOrEmpty(path) ? "Unsaved local session" : path;
        }

        private string GetSessionAssetPathOrUnsaved()
        {
            string path = GetSessionAssetPath();
            return string.IsNullOrEmpty(path) ? "Unsaved" : path;
        }

        private string GetSessionAssetPath()
        {
            return session == null ? string.Empty : AssetDatabase.GetAssetPath(session);
        }

        private static void EnsureSessionFolder()
        {
            string[] parts = DefaultSessionFolder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static string MakeSafeFileName(string value)
        {
            string safe = string.IsNullOrWhiteSpace(value) ? "DesignerSession" : value.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                safe = safe.Replace(invalid, '_');
            }

            return safe.Replace(' ', '_');
        }

        private static string ToProjectPath(string absolutePath)
        {
            string normalizedPath = absolutePath.Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');

            if (!normalizedPath.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return "Assets" + normalizedPath.Substring(dataPath.Length);
        }

        private enum DesignerCockpitMode
        {
            Overview,
            RandomVariation,
            Terrain,
            Composition,
            Deform,
            Diagnostics
        }

        private struct StatusSnapshot
        {
            public int selectedObjectCount;
            public FeatureStatus randomControl;
            public FeatureStatus composition;
            public FeatureStatus deform;
            public FeatureStatus terrain;
            public FeatureStatus topology;

            public static StatusSnapshot CreateEmpty()
            {
                return new StatusSnapshot
                {
                    selectedObjectCount = 0,
                    randomControl = FeatureStatus.Unknown("Not refreshed.", string.Empty),
                    composition = FeatureStatus.Unknown("Not refreshed.", string.Empty),
                    deform = FeatureStatus.Unknown("Not refreshed.", string.Empty),
                    terrain = FeatureStatus.Unknown("Not refreshed.", string.Empty),
                    topology = FeatureStatus.Unknown("Not refreshed.", string.Empty)
                };
            }
        }

        private struct FeatureStatus
        {
            public string state;
            public string summary;
            public string detail;

            public static FeatureStatus Ready(string summary, string detail) => Create("Ready", summary, detail);
            public static FeatureStatus Partial(string summary, string detail) => Create("Partial", summary, detail);
            public static FeatureStatus Untested(string summary, string detail) => Create("Untested", summary, detail);
            public static FeatureStatus Placeholder(string summary, string detail) => Create("Placeholder", summary, detail);
            public static FeatureStatus Missing(string summary, string detail) => Create("Missing", summary, detail);
            public static FeatureStatus DetectionMismatch(string summary, string detail) => Create("Detection mismatch", summary, detail);
            public static FeatureStatus Unknown(string summary, string detail) => Create("Unknown", summary, detail);

            private static FeatureStatus Create(string state, string summary, string detail)
            {
                return new FeatureStatus
                {
                    state = state,
                    summary = summary,
                    detail = detail
                };
            }
        }
    }
}

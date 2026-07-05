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

        private VastCoreDesignerSession session;
        private Vector2 scrollPosition;
        private StatusSnapshot status = StatusSnapshot.CreateEmpty();
        private string lastOperationMessage = "Ready.";

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

            DrawTopStrip();
            DrawSessionControls();
            DrawRecipeControls();
            DrawStatusTiles();
            DrawActionControls();

            EditorGUILayout.EndScrollView();
        }

        private void DrawTopStrip()
        {
            EditorGUILayout.LabelField(WindowTitle, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"Selected objects: {status.selectedObjectCount}\n" +
                $"Session: {session.sessionName}\n" +
                $"Seed: {session.seed}\n" +
                $"Last saved UTC: {session.lastSavedUtc}\n" +
                $"Status: {lastOperationMessage}",
                MessageType.Info);
        }

        private void DrawSessionControls()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Session", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

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

            session.sessionName = EditorGUILayout.TextField("Name", session.sessionName);
            session.seed = EditorGUILayout.IntField("Seed", session.seed);
            session.notes = EditorGUILayout.TextArea(session.notes, GUILayout.MinHeight(48f));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New Session"))
            {
                CreateNewSession();
            }

            if (GUILayout.Button("Save Session"))
            {
                SaveSession();
            }

            if (GUILayout.Button("Load Session"))
            {
                LoadSessionFromProjectFile();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawRecipeControls()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Random Transform Recipe", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            RandomTransformRecipe recipe = session.randomTransform ?? new RandomTransformRecipe();
            session.randomTransform = recipe;

            recipe.positionMin = EditorGUILayout.Vector3Field("Position Min", recipe.positionMin);
            recipe.positionMax = EditorGUILayout.Vector3Field("Position Max", recipe.positionMax);
            recipe.rotationMin = EditorGUILayout.Vector3Field("Rotation Min", recipe.rotationMin);
            recipe.rotationMax = EditorGUILayout.Vector3Field("Rotation Max", recipe.rotationMax);
            recipe.scaleMin = EditorGUILayout.Vector3Field("Scale Min", recipe.scaleMin);
            recipe.scaleMax = EditorGUILayout.Vector3Field("Scale Max", recipe.scaleMax);
            recipe.useRelativePosition = EditorGUILayout.Toggle("Relative Position", recipe.useRelativePosition);
            recipe.useUniformScale = EditorGUILayout.Toggle("Uniform Scale", recipe.useUniformScale);

            EditorGUILayout.HelpBox(
                "Apply uses a deterministic System.Random sequence from the session seed and records Undo on selected transforms.",
                MessageType.None);

            EditorGUILayout.EndVertical();
        }

        private void DrawStatusTiles()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Review Status", EditorStyles.boldLabel);

            DrawStatusTile("RandomControl", status.randomControl, session.randomControlStatusNote);
            DrawStatusTile("Composition", status.composition, session.compositionStatusNote);
            DrawStatusTile("Deform", status.deform, session.deformStatusNote);
            DrawStatusTile("Terrain", status.terrain, session.terrainStatusNote);
            DrawStatusTile("Topology / DualGrid", status.topology, session.topologyStatusNote);
        }

        private void DrawStatusTile(string label, FeatureStatus featureStatus, string note)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"{label}: {featureStatus.state}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(featureStatus.summary, EditorStyles.wordWrappedLabel);
            if (!string.IsNullOrWhiteSpace(note))
            {
                EditorGUILayout.LabelField(note, EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawActionControls()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Status"))
            {
                RefreshStatus();
            }

            EditorGUI.BeginDisabledGroup(status.selectedObjectCount == 0);
            if (GUILayout.Button("Apply Random Transform to Selected"))
            {
                ApplyRandomTransformToSelected();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (status.selectedObjectCount == 0)
            {
                EditorGUILayout.HelpBox("Select one or more scene objects to apply the random transform recipe.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void CreateNewSession()
        {
            session = CreateInstance<VastCoreDesignerSession>();
            session.ResetDefaults();
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
            status = new StatusSnapshot
            {
                selectedObjectCount = Selection.gameObjects?.Length ?? 0,
                randomControl = FeatureStatus.Available(
                    IsTypeAvailable("Vastcore.Editor.Generation.RandomControlTab")
                        ? "RandomControlTab found; cockpit random transform operation is locally available."
                        : "RandomControlTab not found; cockpit random transform operation is standalone."),
                composition = FeatureStatus.Partial(
                    IsTypeAvailable("Vastcore.Editor.Generation.CompositionTab")
                        ? "CompositionTab found; CSG/Blend execution remains untested from this cockpit."
                        : "CompositionTab not found in loaded editor assemblies."),
                deform = FeatureStatus.Partial(
                    IsTypeAvailable("Vastcore.Editor.StructureGenerator.Tabs.DeformerTab")
                        ? "DeformerTab found; package symbol/runtime availability not executed here."
                        : "DeformerTab not found in loaded editor assemblies."),
                terrain = FeatureStatus.Partial(
                    IsTypeAvailable("Vastcore.Terrain.UnifiedTerrainParams") ||
                    IsTypeAvailable("Vastcore.Terrain.TerrainParamsConverter")
                        ? "Terrain parameter types found; terrain generation is status-only in this cockpit."
                        : "Terrain parameter integration types not found."),
                topology = FeatureStatus.Placeholder(
                    IsTypeAvailable("Vastcore.Terrain.DualGrid.GridTopology")
                        ? "DualGrid topology type found; dashboard slot is a future preview surface only."
                        : "DualGrid topology type not found; slot remains future-only.")
            };

            Repaint();
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
                lastOperationMessage = "No selected objects to transform.";
                return;
            }

            RandomTransformRecipe recipe = session.randomTransform ?? new RandomTransformRecipe();
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

            lastOperationMessage = $"Applied random transform to {selectedObjects.Length} selected object(s).";
            RefreshStatus();
            SceneView.RepaintAll();
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
                    randomControl = FeatureStatus.Unknown("Not refreshed."),
                    composition = FeatureStatus.Unknown("Not refreshed."),
                    deform = FeatureStatus.Unknown("Not refreshed."),
                    terrain = FeatureStatus.Unknown("Not refreshed."),
                    topology = FeatureStatus.Unknown("Not refreshed.")
                };
            }
        }

        private struct FeatureStatus
        {
            public string state;
            public string summary;

            public static FeatureStatus Available(string summary) => Create("Available", summary);
            public static FeatureStatus Partial(string summary) => Create("Partial / Untested", summary);
            public static FeatureStatus Placeholder(string summary) => Create("Placeholder", summary);
            public static FeatureStatus Unknown(string summary) => Create("Unknown", summary);

            private static FeatureStatus Create(string state, string summary)
            {
                return new FeatureStatus
                {
                    state = state,
                    summary = summary
                };
            }
        }
    }
}

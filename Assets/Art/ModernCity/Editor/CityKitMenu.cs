using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vastcore.ModernCity.Editor
{
    internal sealed class CityKitMenu : EditorWindow
    {
        private const string Menu = "Vastcore/Modern City/";
        [SerializeField] private int seed = CityBlock.DefaultSeed;
        [SerializeField] private CityStreetPlan street = CityStreetPlan.CornerShops;
        [SerializeField] private CityDetail detail = CityDetail.Furnished;
        [SerializeField] private int panel;
        [SerializeField] private Vector3 origin = new Vector3(0, CityStreetKit.Pavement, 0);
        [SerializeField] private int across = 4;
        [SerializeField] private int deep = 3;
        [SerializeField] private int floors = 3;
        [SerializeField] private float facing;
        [SerializeField] private CityFacade facade;
        [SerializeField] private CityRoof roof;
        [SerializeField] private CityRoom ground = CityRoom.Cafe;
        [SerializeField] private CityRoom upper = CityRoom.Office;

        [MenuItem(Menu + "Open Sample", false, 10)]
        private static void OpenSample()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(CityKitAssets.SampleScene);
        }

        [MenuItem(Menu + "Variants...", false, 11)]
        private static void OpenWindow()
        {
            var window = GetWindow<CityKitMenu>("Modern City Variants");
            window.minSize = new Vector2(290, 325);
        }

        [MenuItem(Menu + "Export Asset Package...", false, 12)]
        private static void ExportPackage()
        {
            string path = EditorUtility.SaveFilePanel("Export Modern City", Application.dataPath, "ModernCity", "unitypackage");
            if (!string.IsNullOrEmpty(path))
                Export(path);
        }

        private void OnGUI()
        {
            panel = GUILayout.Toolbar(panel, new[] { "Street block", "Building" });
            seed = EditorGUILayout.IntField("Seed", seed);
            detail = (CityDetail)EditorGUILayout.EnumPopup("Prop density", detail);
            if (panel == 0)
            {
                street = (CityStreetPlan)EditorGUILayout.EnumPopup("Street plan", street);
                if (GUILayout.Button("Generate as new scene") && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    CreateVariant();
            }
            else
            {
                origin = EditorGUILayout.Vector3Field("Entry corner", origin);
                facing = EditorGUILayout.FloatField("Facing (degrees)", facing);
                across = EditorGUILayout.IntSlider("Frontage bays", across, 3, 8);
                deep = EditorGUILayout.IntSlider("Depth bays", deep, 3, 8);
                floors = EditorGUILayout.IntSlider("Floors", floors, 1, 10);
                facade = (CityFacade)EditorGUILayout.EnumPopup("Facade", facade);
                roof = (CityRoof)EditorGUILayout.EnumPopup("Roof profile", roof);
                ground = (CityRoom)EditorGUILayout.EnumPopup("Ground floor", ground);
                upper = (CityRoom)EditorGUILayout.EnumPopup("Upper floors", upper);
                if (GUILayout.Button("Add building to active scene"))
                    AddBuilding();
            }
        }

        private void CreateVariant() => Selection.activeGameObject = GenerateStreet(seed, street, detail).Root.gameObject;

        internal static CityBlock GenerateStreet(int seed, CityStreetPlan street, CityDetail detail)
        {
            var kit = new CityKitAssets();
            var block = CityBlock.Assemble(kit, seed, street);
            CityFurnishing.Dress(kit, block, detail, seed);
            CityLighting.PrepareIndirect(block);
            CityLighting.StreetReflections(bake: false);
            EditorSceneManager.MarkSceneDirty(block.Scene);
            return block;
        }

        private void AddBuilding()
        {
            var plot = new CityPlot($"{facade} building", origin.x, origin.z, across, deep, floors, facing,
                facade, ground, upper, origin.y, roof: roof);
            var building = PlaceBuilding(plot, detail, seed);
            Undo.RegisterCreatedObjectUndo(building.gameObject, "Add modern city building");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = building.gameObject;
        }

        internal static Transform PlaceBuilding(CityPlot plot, CityDetail detail, int seed)
        {
            var kit = new CityKitAssets();
            var building = CityBlock.AssembleBuilding(kit, plot, null, seed);
            CityFurnishing.DressBuilding(kit, plot, building, detail, seed);
            CityLighting.PrepareBuilding(plot, building);
            return building;
        }

        internal static void Export(string path)
        {
            string[] library = AssetDatabase.GetAllAssetPaths()
                .Where(p => p.StartsWith(CityKitAssets.Root + "/", StringComparison.Ordinal) && File.Exists(p))
                .ToArray();
            if (!File.Exists(CityKitAssets.SampleScene))
                throw new InvalidOperationException("The Modern City sample has not been saved.");
            // URP and Input System are Unity package prerequisites. Every project asset must live inside the kit.
            var outside = AssetDatabase.GetDependencies(library)
                .Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) &&
                    !p.StartsWith(CityKitAssets.Root + "/", StringComparison.Ordinal)).ToArray();
            if (outside.Length != 0)
                throw new InvalidOperationException("Assets outside the Modern City kit: " + string.Join(", ", outside));
            AssetDatabase.ExportPackage(library.OrderBy(p => p, StringComparer.Ordinal).ToArray(), path,
                ExportPackageOptions.Default);
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
                throw new IOException("Unity did not create the asset package at " + path);
        }
    }
}

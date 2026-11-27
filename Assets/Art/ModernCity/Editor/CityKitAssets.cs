using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Vastcore.ModernCity.Editor
{
    internal sealed class CityKitAssets
    {
        internal const string Root = "Assets/Art/ModernCity";
        internal const string SampleScene = Root + "/ModernCity.unity";
        internal const string Buildings = "Buildings";
        internal const string Street = "Street";
        internal const string Props = "Props";
        private readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

        internal void Bake(string category, string name, Action<CityModel> author)
        {
            Folder(Root + "/Meshes/" + category);
            Folder(Root + "/Prefabs/" + category);
            string meshPath = Root + "/Meshes/" + category + "/" + name + ".asset";
            var model = new CityModel();
            author(model);
            Mesh next = model.CreateMesh(name);
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null)
            {
                AssetDatabase.CreateAsset(next, meshPath);
                mesh = next;
            }
            else
            {
                EditorUtility.CopySerialized(next, mesh);
                EditorUtility.SetDirty(mesh);
                Object.DestroyImmediate(next);
            }

            var instance = new GameObject(name);
            try
            {
                model.Attach(instance, mesh);
                string prefabPath = PrefabPath(category, name);
                prefabs[prefabPath] = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath, out bool saved);
                if (!saved)
                    throw new InvalidOperationException("Could not save " + prefabPath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        internal GameObject Place(string category, string name, Transform parent, Vector3 position, float yaw = 0f)
        {
            string path = PrefabPath(category, name);
            if (!prefabs.TryGetValue(path, out GameObject prefab))
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    throw new InvalidOperationException("Missing city prefab at " + path);
                prefabs[path] = prefab;
            }

            var placed = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            placed.transform.SetLocalPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            return placed;
        }

        internal static GameObject Group(string name, Transform parent, Vector3 position, float yaw = 0f)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            group.transform.SetLocalPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            return group;
        }

        internal static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            int divider = path.LastIndexOf('/');
            string parent = path.Substring(0, divider);
            Folder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(divider + 1));
        }

        private static string PrefabPath(string category, string name) =>
            Root + "/Prefabs/" + category + "/" + name + ".prefab";
    }

    internal sealed class CityPalette
    {
        private const string TimberMaps = "Assets/Art/Textures/CNT/weathered_pine_timber/raw_plank_wall";
        internal readonly Material Asphalt = Textured("Asphalt", RoadMap("Asphalt010"), "CFD4DB", 4, 0.05f);
        internal readonly Material Paving = Textured("Paving", RoadMap("PavingStones073"), "CFCCC6", 3.5f, 0.16f);
        internal readonly Material Stone = Make("Warm limestone", "A9A396", 0.23f);
        internal readonly Material Brick = Textured("Brick", RoadMap("Bricks102"), "C8B9AE", 2.5f, 0.15f);
        internal readonly Material Timber = Textured("Timber", TimberMaps + "_diff_2k.jpg", "BCA78E", 2, 0.34f,
            TimberMaps + "_nor_gl_2k.jpg");
        internal readonly Material Concrete = Make("Concrete", "90999B", 0.18f);
        internal readonly Material Paint = Make("Porcelain", "D5D8D5", 0.28f);
        internal readonly Material Dark = Make("Charcoal", "3E4850", 0.31f);
        internal readonly Material Rubber = Make("Rubber", "30363C", 0.12f);
        internal readonly Material Metal = Make("Galvanized metal", "9FA9AD", 0.45f, 0.36f);
        internal readonly Material Brass = Make("Brushed brass", "AC8756", 0.44f, 0.37f);
        internal readonly Material White = Make("Road paint", "E8E8D6", 0.1f);
        internal readonly Material Yellow = Make("Safety amber", "E2AF52", 0.22f);
        internal readonly Material Red = Make("Oxide red", "A64C42", 0.25f);
        internal readonly Material Blue = Make("Slate blue", "536E80", 0.3f);
        internal readonly Material Textile = Make("Textile warm grey", "837367", 0.08f);
        internal readonly Material Olive = Make("Textile olive", "798361", 0.12f);
        internal readonly Material Card = Make("Kraft paper", "AA8763", 0.05f);
        internal readonly Material Bark = Make("Bark", "645648", 0.14f);
        internal readonly Material Leaf = Make("Foliage shaded", "4E663D", 0.16f, doubleSided: true);
        internal readonly Material LeafLight = Make("Foliage sunlit", "70824F", 0.2f, doubleSided: true);
        internal readonly Material Soil = Make("Planting soil", "514A42", 0.12f);
        internal readonly Material Glass = Make("Glazing", "91B0BF", 0.83f, glass: true);
        internal readonly Material Light = Make("Warm diffuser", "FFF0CC", 0.3f, emission: 0.73f);
        internal readonly Material Screen = Make("Display", "586E7B", 0.62f, emission: 0.17f);
        internal readonly Material Awning = Make("Ochre canvas", "B79869", 0.13f);

        private static string RoadMap(string name)
        {
            string stem = name + "_1K-JPG";
            return "Assets/Resources/MapGenerator/Textures/" + stem + "/" + stem + "_Color.jpg";
        }

        private static Material Textured(string name, string colorPath, string color, float tileMetres, float gloss,
            string normalPath = null)
        {
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath);
            if (albedo == null)
                throw new InvalidOperationException("Missing color map at " + colorPath);
            Material material = Make(name, color, gloss);
            material.SetTexture("_BaseMap", albedo);
            material.SetTextureScale("_BaseMap", Vector2.one / tileMetres);
            if (normalPath != null)
            {
                var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                if (normal == null)
                    throw new InvalidOperationException("Missing normal map at " + normalPath);
                material.SetTexture("_BumpMap", normal);
                material.SetFloat("_BumpScale", 0.65f);
                material.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material Make(string name, string color, float gloss, float metal = 0,
            bool glass = false, float emission = 0, bool doubleSided = false)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("Modern City requires the Universal Render Pipeline.");
            var authored = new Material(shader) { name = name, enableInstancing = true };
            ColorUtility.TryParseHtmlString("#" + color, out var tint);
            if (doubleSided)
                authored.SetFloat("_Cull", (float)CullMode.Off);
            if (glass)
            {
                tint.a = 0.28f;
                authored.SetFloat("_Surface", 1);
                authored.SetFloat("_ZWrite", 0);
                authored.SetFloat("_Cull", (float)CullMode.Off);
                authored.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                authored.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                authored.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                authored.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                authored.SetOverrideTag("RenderType", "Transparent");
                authored.SetShaderPassEnabled("ShadowCaster", false);
                authored.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                authored.renderQueue = (int)RenderQueue.Transparent;
            }

            if (emission > 0)
            {
                authored.SetColor("_EmissionColor", tint.linear * emission);
                authored.EnableKeyword("_EMISSION");
            }

            authored.SetColor("_BaseColor", tint);
            authored.SetFloat("_Metallic", metal);
            authored.SetFloat("_Smoothness", gloss);
            string folder = CityKitAssets.Root + "/Materials";
            CityKitAssets.Folder(folder);
            string path = folder + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(authored, path);
                return authored;
            }

            EditorUtility.CopySerialized(authored, existing);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(authored);
            return existing;
        }
    }
}

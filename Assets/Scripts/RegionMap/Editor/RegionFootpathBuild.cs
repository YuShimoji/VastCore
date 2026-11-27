using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Vastcore.RegionMap.Editor
{
    internal static class RegionFootpathBuild
    {
        private const string Asset = "Assets/Resources/RegionMap3D/Footpath/FootpathParts.asset";

        internal static void BuildFromCommandLine()
        {
            try
            {
                RegionTerrainViewMenu.UseBuildQuality();
                var material = AssetDatabase.LoadAllAssetsAtPath(Asset).OfType<Material>()
                    .Single(item => item.name == "Shelf PBR");
                var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(RegionPropAssets.StoneSet + "worn_rock_natural_01_diff_2k.jpg");
                var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(RegionPropAssets.StoneSet + "worn_rock_natural_01_nor_gl_2k.jpg");
                if (!albedo || !normal) throw new InvalidDataException("The shared DS-012 sandstone maps are missing");
                material.SetTexture("_BaseMap", albedo);
                material.SetTexture("_BumpMap", normal);
                material.SetFloat("_BumpScale", .85f);
                material.SetColor("_BaseColor", RegionPropAssets.StoneMatch);
                material.EnableKeyword("_NORMALMAP");
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssets();

                var prefab = Resources.Load<GameObject>(RegionFootpath.Resource);
                if (!prefab || prefab.GetComponentsInChildren<LODGroup>(true).Length != 10 ||
                    prefab.GetComponentsInChildren<Renderer>(true).Any(renderer => renderer.sharedMaterials.Any(
                        item => !item || !item.shader || !item.shader.isSupported || item.shader.name == "Hidden/InternalErrorShader")))
                    throw new InvalidDataException("The playable footpath has missing geometry or materials");

                BuildWindowsPlayer("artifacts/region-map/footpath", BuildOptions.Development);
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        internal static void BuildExplorerFromCommandLine()
        {
            try
            {
                BuildWindowsPlayer("artifacts/region-map/windows", BuildOptions.None);
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        private static void BuildWindowsPlayer(string defaultOutput, BuildOptions options)
        {
            string output = Path.GetFullPath(RegionMapMenu.Argument("-regionOut", defaultOutput));
            Directory.CreateDirectory(output);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { RegionMapMenu.ExplorerScenePath },
                locationPathName = Path.Combine(output, "VastCoreRegionExplorer.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = options
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"VastCore Player build: {report.summary.result}");
            Debug.Log($"[RegionMap] Windows Player: {report.summary.totalSize} bytes, {report.summary.totalTime}");
        }
    }
}

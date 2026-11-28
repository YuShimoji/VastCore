using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.ModernCity.Editor
{
    internal static class CityLighting
    {
        private const string SettingsPath = CityKitAssets.Root + "/Interior Lighting.lighting";
        private const string ReflectionFolder = CityKitAssets.Root + "/Textures/Storefront/";

        internal static void PrepareIndirect(CityBlock block)
        {
            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath);
            if (!settings)
            {
                settings = new LightingSettings
                {
                    lightmapper = LightingSettings.Lightmapper.ProgressiveCPU,
                    bakedGI = true,
                    realtimeGI = false,
                    mixedBakeMode = MixedLightingMode.IndirectOnly,
                    directSampleCount = 24,
                    indirectSampleCount = 64,
                    environmentSampleCount = 48,
                    maxBounces = 3,
                    lightmapResolution = 4,
                    lightmapMaxSize = 256,
                    indirectScale = 1.12f
                };
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            Lightmapping.lightingSettings = settings;
            block.Sun.lightmapBakeType = LightmapBakeType.Mixed;
            foreach (var (plot, building) in block.Buildings)
                PrepareBuilding(plot, building);

            // Pavement and the ground contribute exterior bounce without receiving an atlas.
            var streets = block.Root.Find("Street and sidewalks");
            foreach (var renderer in streets.GetComponentsInChildren<MeshRenderer>())
            {
                ContributeIndirect(renderer);
                renderer.lightProbeUsage = LightProbeUsage.Off;
            }
        }

        internal static void PrepareBuilding(CityPlot plot, Transform building)
        {
            foreach (var renderer in building.GetComponentsInChildren<MeshRenderer>())
                ContributeIndirect(renderer);
            foreach (var light in building.GetComponentsInChildren<Light>())
            {
                light.lightmapBakeType = LightmapBakeType.Mixed;
                light.bounceIntensity = 1.32f;
            }

            var positions = new List<Vector3>();
            for (int floor = 0; floor < plot.Floors; floor++)
            {
                float storey = floor * CityBuildingKit.Storey;
                float front = plot.FrontBay(floor) * CityBuildingKit.Bay;
                foreach (float x in new[] { 2.3f, plot.Width * 0.5f, plot.Width - 2.3f })
                foreach (float y in new[] { 0.72f, 2.16f })
                foreach (float z in new[] { front + 1.45f, front + 3.3f, plot.Length - 1.1f })
                    positions.Add(new Vector3(x, storey + y, z));

                float mid = plot.Width * 0.5f;
                positions.Add(new Vector3(mid, storey + 1.45f, front - 1.0f));
                positions.Add(new Vector3(mid, storey + 1.45f, plot.Length + 0.9f));
            }
            var probes = CityKitAssets.Group("Room light probes", building, Vector3.zero).AddComponent<LightProbeGroup>();
            probes.probePositions = positions.ToArray();
            for (int floor = 0; floor < plot.Floors; floor++)
            {
                var level = building.Find($"Floor {floor + 1}");
                int roomBelow = Mathf.Max(0, floor - 1);
                float frontBelow = plot.FrontBay(roomBelow) * CityBuildingKit.Bay;
                AnchorSlabTiles(level, plot.Width, frontBelow, floor == 0 ? 0.72f : -0.84f);
            }
            AnchorSlabTiles(building.Find("Roof"), plot.Width, plot.FrontBay(plot.Floors - 1) * CityBuildingKit.Bay, -0.84f);
        }

        private static void AnchorSlabTiles(Transform level, float width, float front, float height)
        {
            // A slab mesh includes its ceiling underside. Share the sample below adjacent ceiling panels.
            var anchor = CityKitAssets.Group("Slab light probe anchor", level,
                new Vector3(width * 0.5f, height, front + 3.3f)).transform;
            foreach (Transform piece in level)
                if (piece.name.EndsWith(" slab", StringComparison.Ordinal))
                    piece.GetComponent<MeshRenderer>().probeAnchor = anchor;
        }

        internal static void StreetReflections(bool bake)
        {
            var root = CityKitAssets.Group("Street reflections", null, Vector3.zero).transform;
            var zones = new[]
            {
                ("West street", "Street_reflection", new Vector3(-25f, 3.15f, 0.8f), new Vector3(69f, 20f, 30f)),
                ("Main junction", "Junction_reflection", new Vector3(29f, 3.25f, -0.55f), new Vector3(64f, 25f, 34f)),
                ("East crossing", "East_reflection", new Vector3(88f, 4.2f, 0.3f), new Vector3(100f, 25f, 32f)),
                ("South lane", "South_lane_reflection", new Vector3(11f, 2.8f, -34f), new Vector3(65f, 12f, 31f))
            };
            var captures = new List<(ReflectionProbe Probe, Cubemap Map)>();
            foreach (var (name, file, origin, size) in zones)
            {
                var probe = CityKitAssets.Group(name, root, origin).AddComponent<ReflectionProbe>();
                probe.mode = bake ? ReflectionProbeMode.Custom : ReflectionProbeMode.Realtime;
                probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
                probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
                probe.renderDynamicObjects = bake;
                probe.resolution = bake ? 128 : 64;
                probe.hdr = true;
                probe.size = size;
                probe.blendDistance = 4;
                probe.boxProjection = true;
                probe.intensity = 0.88f;
                probe.nearClipPlane = 0.28f;
                probe.farClipPlane = 136;
                if (!bake)
                    continue;

                string path = ReflectionFolder + file + ".exr";
                if (!Lightmapping.BakeReflectionProbe(probe, path))
                    throw new InvalidOperationException("Could not capture the reflection for " + name);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var texture = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
                if (!texture)
                    throw new InvalidOperationException("Unity did not import a cubemap at " + path);
                captures.Add((probe, texture));
            }
            foreach (var (probe, texture) in captures)
                probe.customBakedTexture = texture;
        }

        internal static string BakeIndirect()
        {
            var timer = Stopwatch.StartNew();
            if (!Lightmapping.Bake() || !LightmapSettings.lightProbes || LightmapSettings.lightProbes.count == 0)
                throw new InvalidOperationException("Unity did not bake the Modern City light probes.");
            return FormattableString.Invariant($"Indirect lighting: {LightmapSettings.lightProbes.count} baked probes, {LightmapSettings.lightmaps.Length} lightmaps, {timer.Elapsed.TotalSeconds:F1} seconds.");
        }

        private static void ContributeIndirect(MeshRenderer renderer)
        {
            var target = renderer.gameObject;
            GameObjectUtility.SetStaticEditorFlags(target, GameObjectUtility.GetStaticEditorFlags(target) |
                StaticEditorFlags.ContributeGI);
            renderer.receiveGI = ReceiveGI.LightProbes;
        }
    }
}

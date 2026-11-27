using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Vastcore.RegionMap.Editor
{
    /// <summary>
    /// 表面の写真素材の候補を撮り比べる（設計 docs/02_design/REGION_SURFACE_MATERIALS_SPEC.md 4 節の段 4、6.3 節の T5）。
    /// 候補ごとに、素材を役のフォルダへ一時的に置いて取り込み時の処理（RegionSurfaceImport）で 3 枚を作り、GPU の上へ写して持つ。
    /// 太陽の高さごとに場面を 1 回作り（頂点の日当たりは場面を作る時に焼く）、その中で「素材なし」と各候補を 1 役ずつ差し替えて、
    /// 固定の遠景・中景・近景とアルコーブを撮る。他の役は素材なし（今の手続きの色）のままにする。
    /// 指定: -surfaceCandidates 候補の一覧（JSON）-regionOut 出力先 -sunAltitudes 60,38,20,10 -regionSeed 9 [-lookCenter X,Y]。
    /// 出力: &lt;出力&gt;/sun&lt;高さ&gt;/&lt;候補の id&gt;/look-far.png など（候補の id "none" は素材なし）。
    /// 置いたフォルダは終わると消す（リポジトリへ素材を残さない）。
    /// </summary>
    public static class RegionSurfaceCandidates
    {
        [Serializable]
        private sealed class Manifest
        {
            public Candidate[] candidates = new Candidate[0];
        }

        [Serializable]
        private sealed class Candidate
        {
            /// <summary>出力のフォルダ名。</summary>
            public string id;
            /// <summary>役のフォルダ名（RegionSurfaceMaps.Folders）。</summary>
            public string role;
            /// <summary>素材の画像のあるフォルダ（絶対パス）。</summary>
            public string source;
            /// <summary>画像の横 1 枚の実寸（m）。0 は設定の値。</summary>
            public float tileMetres;
        }

        private const string AssetRoot = "Assets/Resources/" + RegionSurfaceMaps.Root;

        public static void CaptureFromCommandLine()
        {
            int exitCode = 0;
            var cache = new Dictionary<string, Texture2D[]>();
            try
            {
                string output = Path.GetFullPath(RegionMapMenu.Argument("-regionOut", "artifacts/region-map/surface-candidates"));
                Directory.CreateDirectory(output);
                string manifestPath = RegionMapMenu.Argument("-surfaceCandidates", "");
                if (manifestPath.Length == 0) throw new ArgumentException("-surfaceCandidates <候補の一覧 JSON>");
                Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
                ShaderUtil.allowAsyncCompilation = false;
                RegionTerrainViewMenu.UseBuildQuality();

                var watch = System.Diagnostics.Stopwatch.StartNew();
                foreach (Candidate candidate in manifest.candidates)
                    cache[candidate.id] = Prepare(candidate);
                Debug.Log("[RegionMap] 素材の候補 " + cache.Count + " 個を用意 " + watch.ElapsedMilliseconds + "ms");

                var regionSpec = new RegionSpec { Seed = int.Parse(RegionMapMenu.Argument("-regionSeed", "9")) };
                var spec = new RegionLookSpec();
                string center = RegionMapMenu.Argument("-lookCenter", "");
                if (center.Length > 0)
                {
                    string[] xy = center.Split(',');
                    spec.CenterMapX = float.Parse(xy[0], CultureInfo.InvariantCulture);
                    spec.CenterMapY = float.Parse(xy[1], CultureInfo.InvariantCulture);
                }
                foreach (string value in RegionMapMenu.Argument("-sunAltitudes", "60,38,20,10").Split(','))
                {
                    float altitude = float.Parse(value, CultureInfo.InvariantCulture);
                    RegionAtmosphere atmosphere = RegionAtmosphere.Create(RegionAtmosphere.Preset.ClearAfternoon);
                    atmosphere.SunAltitudeDegrees = altitude;
                    watch.Restart();
                    RegionSurfaceMaterials.SourceOverride = null;
                    RegionSurfaceMaterials.TileOverride = null;
                    RegionTerrainView.LookResult look = RegionTerrainViewMenu.CreateLookScene(regionSpec, spec, atmosphere, out Camera camera);
                    RegionLookSky.UseOwnClouds(camera, look, RegionLookSky.MixedCloudTime(look.Scene), highOnly: RegionLookSky.HighOnly);
                    Debug.Log("[RegionMap] 素材の候補: 太陽 " + value + " 度の場面 " + watch.ElapsedMilliseconds + "ms");
                    string sunFolder = Path.Combine(output, "sun" + altitude.ToString("00", CultureInfo.InvariantCulture));

                    Shoot(camera, look, Path.Combine(sunFolder, "none"), null);
                    foreach (Candidate candidate in manifest.candidates)
                        Shoot(camera, look, Path.Combine(sunFolder, candidate.id), candidate, cache[candidate.id]);
                }
                RegionVolumePatch.SunAltitudeDegrees = RegionVolumePatch.DefaultSunAltitudeDegrees;
            }
            catch (Exception exception)
            {
                Debug.LogError("[RegionMap] 素材の候補の撮影に失敗: " + exception);
                exitCode = 1;
            }
            finally
            {
                RegionSurfaceMaterials.SourceOverride = null;
                RegionSurfaceMaterials.TileOverride = null;
                foreach (Texture2D[] maps in cache.Values)
                foreach (Texture2D map in maps)
                    if (map != null) UnityEngine.Object.DestroyImmediate(map);
            }
            EditorApplication.Exit(exitCode);
        }

        private static void Shoot(Camera camera, RegionTerrainView.LookResult look, string folder, Candidate candidate, Texture2D[] maps = null)
        {
            int role = candidate == null ? -1 : Array.IndexOf(RegionSurfaceMaps.Folders, candidate.role);
            RegionSurfaceMaterials.SourceOverride = r => r == role ? maps : null;
            RegionSurfaceMaterials.TileOverride = r => r == role ? candidate.tileMetres : 0f;
            RegionSurfaceMaterials.Bind();
            Debug.Log("[RegionMap] 素材の候補 " + (candidate == null ? "none" : candidate.id) + ": " + RegionSurfaceMaterials.Summary);
            Directory.CreateDirectory(folder);
            // 配列を替えた直後の 1 枚を捨てる
            RegionTerrainView.PlaceLook(camera.transform, look.Scene, look.Scene.Spec.Far);
            RegionTerrainViewMenu.Capture(camera, null, null);
            foreach (RegionLookViewpoint viewpoint in look.Scene.Spec.Viewpoints())
            {
                RegionTerrainView.PlaceLook(camera.transform, look.Scene, viewpoint);
                camera.fieldOfView = viewpoint.FovDegrees;
                RegionTerrainViewMenu.Capture(camera, folder, "look-" + viewpoint.Name + ".png");
            }
            if (look.Alcove == null) return;
            foreach (bool mid in new[] { true, false })
            {
                look.Alcove.Place(camera.transform, mid, 0.57f, true);
                camera.fieldOfView = mid ? 48f : 67f;
                RegionTerrainViewMenu.Capture(camera, folder, mid ? "alcove-mid.png" : "alcove-near.png");
            }
        }

        // 素材を役のフォルダへ置いて 3 枚を作り、GPU の上へ写して返す。置いたフォルダは消す
        private static Texture2D[] Prepare(Candidate candidate)
        {
            int role = Array.IndexOf(RegionSurfaceMaps.Folders, candidate.role);
            if (role < 0) throw new ArgumentException("役の名前が違う: " + candidate.role);
            if (!AssetDatabase.IsValidFolder(AssetRoot)) AssetDatabase.CreateFolder("Assets/Resources/RegionMap3D", "Surfaces");
            string folder = AssetRoot + "/" + candidate.role;
            if (AssetDatabase.IsValidFolder(folder)) throw new InvalidOperationException(folder + " が既にある。素材を置いた写しでは撮らない");
            AssetDatabase.CreateFolder(AssetRoot, candidate.role);
            try
            {
                int copied = 0;
                foreach (string file in Directory.GetFiles(candidate.source))
                {
                    RegionSurfaceMaps.MapKind kind = RegionSurfaceMaps.Classify(file);
                    if (kind == RegionSurfaceMaps.MapKind.None || kind == RegionSurfaceMaps.MapKind.Generated) continue;
                    File.Copy(file, Path.Combine(folder, Path.GetFileName(file)));
                    copied++;
                }
                if (copied == 0) throw new InvalidDataException("素材の画像が無い: " + candidate.source);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                RegionSurfaceImport.Rebuild(folder);
                var maps = new Texture2D[3];
                string[] names = { RegionSurfaceMaps.DetailName, RegionSurfaceMaps.NormalName, RegionSurfaceMaps.HraName };
                for (int i = 0; i < 3; i++)
                {
                    var source = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/" + names[i] + ".png");
                    if (source == null) throw new InvalidDataException(candidate.id + ": " + names[i] + " ができていない");
                    var copy = new Texture2D(source.width, source.height, source.graphicsFormat, source.mipmapCount, TextureCreationFlags.MipChain)
                    {
                        name = candidate.id + " " + names[i], hideFlags = HideFlags.HideAndDontSave,
                    };
                    Graphics.CopyTexture(source, copy);
                    maps[i] = copy;
                }
                Debug.Log("[RegionMap] 素材の候補 " + candidate.id + "（" + candidate.role + "、横 " + candidate.tileMetres.ToString("0.##", CultureInfo.InvariantCulture) + " m）を用意");
                return maps;
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}

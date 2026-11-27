using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Vastcore.RegionMap.Editor
{
    /// <summary>
    /// 表面の写真素材の候補を撮り比べる（設計 docs/02_design/REGION_SURFACE_MATERIALS_SPEC.md 4 節の段 4、6.3 節の T5）。
    /// 候補ごとに、素材を役のフォルダへ一時的に置いて取り込み時の処理（RegionSurfaceImport）で 3 枚を作り、GPU の上へ写して持つ。
    /// 太陽の高さごとに場面を 1 回作り（頂点の日当たりは場面を作る時に焼く）、その中で「素材なし」と各候補を 1 役ずつ差し替えて撮る。
    /// 他の役は素材なし（今の手続きの色）のままにする。
    /// 視点は 2 通り（-candidateViews）:
    ///   fixed  … 固定の遠景・中景・近景とアルコーブ。出力は &lt;出力&gt;/sun&lt;高さ&gt;/&lt;候補&gt;/look-far.png など（候補 "none" は素材なし）。
    ///   search … 役ごとに、候補の違いが大きく写る視点を、見る距離（25 m・60 m）ごとに 1 つ探して撮る。探し方は、内側の窓の地面の上 1.7 m から
    ///            6 つの向きの地面を見る視点を並べ（間を地形が遮る物は除く）、小さな画像で役の最初の 2 つの候補を描き比べ、違う画素の割合が大きい物を選ぶ。
    ///            「素材なし」と比べないのは、どれか 1 役に素材を置くだけで全体の描き方（鏡面・高さの混合）が変わり、役に関係なく点が付くため。
    ///            出力は &lt;出力&gt;/sun&lt;高さ&gt;/&lt;役&gt;/v&lt;番号&gt;/&lt;候補&gt;[@&lt;強さ&gt;].png と、選んだ視点の views.json。
    /// -strengths "1:0,2:0.5" は写真の使い方（明暗の強さ:写真の色を混ぜる割合）の並び。最初の物を既定とし、他は名前に @強さ を付ける
    /// （既定の -strengths は 1:0。置かれた素材の既定の強さは RegionSurfaceMaterials.DefaultContrast・DefaultPhotoColor）。
    /// 指定: -surfaceCandidates 候補の一覧（JSON）-regionOut 出力先 -sunAltitudes 60,38,20,10 -regionSeed 9 [-lookCenter X,Y]。
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
            /// <summary>出力の名前。</summary>
            public string id;
            /// <summary>役のフォルダ名（RegionSurfaceMaps.Folders）。</summary>
            public string role;
            /// <summary>素材の画像のあるフォルダ（絶対パス）。</summary>
            public string source;
            /// <summary>画像の横 1 枚の実寸（m）。0 は設定の値。</summary>
            public float tileMetres;
        }

        private struct Prepared
        {
            public Texture2D[] Maps;
            public Color Tint;
        }

        private struct View
        {
            public Vector3 Position;
            public float Yaw, Pitch, Score, Distance;
        }

        private struct Strength
        {
            public float Contrast, Color;
            public string Suffix;
        }

        private const string AssetRoot = "Assets/Resources/" + RegionSurfaceMaps.Root;
        private const float ViewFov = 55f, EyeHeight = 1.7f;
        private const int SearchWidth = 320, SearchHeight = 180;
        private static readonly float[] Distances = { 25f, 60f };

        public static void CaptureFromCommandLine()
        {
            int exitCode = 0;
            var cache = new Dictionary<string, Prepared>();
            try
            {
                string output = Path.GetFullPath(RegionMapMenu.Argument("-regionOut", "artifacts/region-map/surface-candidates"));
                Directory.CreateDirectory(output);
                string manifestPath = RegionMapMenu.Argument("-surfaceCandidates", "");
                if (manifestPath.Length == 0) throw new ArgumentException("-surfaceCandidates <候補の一覧 JSON>");
                Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
                bool search = RegionMapMenu.Argument("-candidateViews", "fixed") == "search";
                Strength[] strengths = ParseStrengths(RegionMapMenu.Argument("-strengths", "1:0"));
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
                Dictionary<string, View[]> roleViews = null;
                foreach (string value in RegionMapMenu.Argument("-sunAltitudes", "60,38,20,10").Split(','))
                {
                    float altitude = float.Parse(value, CultureInfo.InvariantCulture);
                    RegionAtmosphere atmosphere = RegionAtmosphere.Create(RegionAtmosphere.Preset.ClearAfternoon);
                    atmosphere.SunAltitudeDegrees = altitude;
                    watch.Restart();
                    Reset();
                    RegionTerrainView.LookResult look = RegionTerrainViewMenu.CreateLookScene(regionSpec, spec, atmosphere, out Camera camera);
                    RegionLookSky.UseOwnClouds(camera, look, RegionLookSky.MixedCloudTime(look.Scene), highOnly: RegionLookSky.HighOnly);
                    Debug.Log("[RegionMap] 素材の候補: 太陽 " + value + " 度の場面 " + watch.ElapsedMilliseconds + "ms");
                    string sunFolder = Path.Combine(output, "sun" + altitude.ToString("00", CultureInfo.InvariantCulture));
                    if (!search)
                    {
                        ShootFixed(camera, look, Path.Combine(sunFolder, "none"), null, default);
                        foreach (Candidate candidate in manifest.candidates)
                            ShootFixed(camera, look, Path.Combine(sunFolder, candidate.id), candidate, cache[candidate.id]);
                        continue;
                    }
                    if (roleViews == null)
                    {
                        watch.Restart();
                        roleViews = SearchViews(camera, look.Scene, manifest, cache, strengths[strengths.Length - 1], regionSpec.Seed);
                        WriteViews(Path.Combine(output, "views.json"), roleViews);
                        Debug.Log("[RegionMap] 素材の候補: 視点を探した " + watch.ElapsedMilliseconds + "ms");
                    }
                    ShootRoles(camera, Path.Combine(sunFolder), manifest, cache, roleViews, strengths);
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
                Reset();
                foreach (Prepared prepared in cache.Values)
                foreach (Texture2D map in prepared.Maps)
                    if (map != null) UnityEngine.Object.DestroyImmediate(map);
            }
            EditorApplication.Exit(exitCode);
        }

        /// <summary>
        /// 置かれた素材（Resources の Surfaces、既定の強さ）のまま撮る（T5 の採否の後の撮り直し。素材の無い版で撮れば前の絵になる）。
        /// 太陽の高さごとに場面を作り、固定の遠景・中景・近景とアルコーブを &lt;出力&gt;/sun&lt;高さ&gt;/fixed/&lt;名前&gt;/ へ、
        /// -viewsFrom の views.json（候補の撮影が選んだ近い視点）の視点を &lt;出力&gt;/sun&lt;高さ&gt;/&lt;役&gt;/v&lt;番号&gt;/&lt;名前&gt;.png へ撮る。
        /// 指定: -regionOut 出力先 -shotName 名前（既定 current）-viewsFrom views.json -sunAltitudes 60,38,20,10 -regionSeed 9 [-lookCenter X,Y]
        /// [-alsoOff true（同じ場面で素材を切った絵も名前 off で撮る）]。
        /// </summary>
        public static void CaptureCurrentFromCommandLine()
        {
            int exitCode = 0;
            try
            {
                string output = Path.GetFullPath(RegionMapMenu.Argument("-regionOut", "artifacts/region-map/surface-current"));
                string name = RegionMapMenu.Argument("-shotName", "current");
                bool alsoOff = RegionMapMenu.Argument("-alsoOff", "false") == "true";
                string viewsPath = RegionMapMenu.Argument("-viewsFrom", "");
                Dictionary<string, View[]> roleViews = viewsPath.Length > 0 ? ReadViews(File.ReadAllText(viewsPath)) : new Dictionary<string, View[]>();
                ShaderUtil.allowAsyncCompilation = false;
                RegionTerrainViewMenu.UseBuildQuality();
                var regionSpec = new RegionSpec { Seed = int.Parse(RegionMapMenu.Argument("-regionSeed", "9")) };
                var spec = new RegionLookSpec();
                string center = RegionMapMenu.Argument("-lookCenter", "");
                if (center.Length > 0)
                {
                    string[] xy = center.Split(',');
                    spec.CenterMapX = float.Parse(xy[0], CultureInfo.InvariantCulture);
                    spec.CenterMapY = float.Parse(xy[1], CultureInfo.InvariantCulture);
                }
                var watch = System.Diagnostics.Stopwatch.StartNew();
                foreach (string value in RegionMapMenu.Argument("-sunAltitudes", "60,38,20,10").Split(','))
                {
                    float altitude = float.Parse(value, CultureInfo.InvariantCulture);
                    RegionAtmosphere atmosphere = RegionAtmosphere.Create(RegionAtmosphere.Preset.ClearAfternoon);
                    atmosphere.SunAltitudeDegrees = altitude;
                    watch.Restart();
                    Reset();
                    RegionTerrainView.LookResult look = RegionTerrainViewMenu.CreateLookScene(regionSpec, spec, atmosphere, out Camera camera);
                    RegionLookSky.UseOwnClouds(camera, look, RegionLookSky.MixedCloudTime(look.Scene), highOnly: RegionLookSky.HighOnly);
                    RegionSurfaceMaterials.Bind();
                    Debug.Log("[RegionMap] 置かれた素材で撮る（" + name + "）: 太陽 " + value + " 度の場面 " + watch.ElapsedMilliseconds + "ms、素材 "
                              + RegionSurfaceMaterials.Summary);
                    string sunFolder = Path.Combine(output, "sun" + altitude.ToString("00", CultureInfo.InvariantCulture));
                    ShootCurrent(camera, look, sunFolder, name, roleViews);
                    // -alsoOff true: 同じ場面で素材を切った絵（素材を置く前と同じ描き方）も「off」の名前で撮る
                    if (alsoOff && RegionSurfaceMaterials.Count > 0)
                    {
                        RegionSurfaceMaterials.SetActive(false);
                        ShootCurrent(camera, look, sunFolder, "off", roleViews);
                        RegionSurfaceMaterials.SetActive(true);
                    }
                }
                RegionVolumePatch.SunAltitudeDegrees = RegionVolumePatch.DefaultSunAltitudeDegrees;
            }
            catch (Exception exception)
            {
                Debug.LogError("[RegionMap] 置かれた素材での撮影に失敗: " + exception);
                exitCode = 1;
            }
            finally
            {
                Reset();
            }
            EditorApplication.Exit(exitCode);
        }

        // 固定の視点を sun/fixed/name/ へ、役ごとの近い視点を sun/役/v番号/name.png へ撮る
        private static void ShootCurrent(Camera camera, RegionTerrainView.LookResult look, string sunFolder, string name, Dictionary<string, View[]> roleViews)
        {
            ShootFixedViews(camera, look, Path.Combine(sunFolder, "fixed", name));
            foreach (KeyValuePair<string, View[]> pair in roleViews)
            {
                if (pair.Value.Length == 0) continue;
                Place(camera, pair.Value[0]);
                RegionTerrainViewMenu.Capture(camera, null, null);
                for (int i = 0; i < pair.Value.Length; i++)
                {
                    string folder = Path.Combine(sunFolder, pair.Key, "v" + (i + 1));
                    Directory.CreateDirectory(folder);
                    Place(camera, pair.Value[i]);
                    RegionTerrainViewMenu.Capture(camera, folder, name + ".png");
                }
            }
        }

        // WriteViews の書式（{"役": [{"x": …, "y": …, "z": …, "yaw": …, "pitch": …, "fov": …, "distance": …, "score": …}, …], …}）を読む
        private static Dictionary<string, View[]> ReadViews(string text)
        {
            var result = new Dictionary<string, View[]>();
            foreach (System.Text.RegularExpressions.Match role in System.Text.RegularExpressions.Regex.Matches(text, "\"(\\w+)\"\\s*:\\s*\\[([^\\]]*)\\]"))
            {
                var views = new List<View>();
                foreach (System.Text.RegularExpressions.Match item in System.Text.RegularExpressions.Regex.Matches(role.Groups[2].Value, "\\{([^}]*)\\}"))
                {
                    var values = new Dictionary<string, float>();
                    foreach (System.Text.RegularExpressions.Match field in System.Text.RegularExpressions.Regex.Matches(item.Groups[1].Value, "\"(\\w+)\"\\s*:\\s*(-?[0-9.]+)"))
                        values[field.Groups[1].Value] = float.Parse(field.Groups[2].Value, CultureInfo.InvariantCulture);
                    views.Add(new View
                    {
                        Position = new Vector3(values["x"], values["y"], values["z"]), Yaw = values["yaw"], Pitch = values["pitch"],
                        Distance = values["distance"], Score = values["score"],
                    });
                }
                result[role.Groups[1].Value] = views.ToArray();
            }
            return result;
        }

        private static void Reset()
        {
            RegionSurfaceMaterials.SourceOverride = null;
            RegionSurfaceMaterials.TileOverride = null;
            RegionSurfaceMaterials.TintOverride = null;
            RegionSurfaceMaterials.Contrast = RegionSurfaceMaterials.DefaultContrast;
            RegionSurfaceMaterials.PhotoColor = RegionSurfaceMaterials.DefaultPhotoColor;
            RegionSurfaceMaterials.TileBreak = true;
        }

        private static Strength[] ParseStrengths(string text)
        {
            var list = new List<Strength>();
            foreach (string item in text.Split(','))
            {
                string[] pair = item.Split(':');
                var strength = new Strength
                {
                    Contrast = float.Parse(pair[0], CultureInfo.InvariantCulture),
                    Color = pair.Length > 1 ? float.Parse(pair[1], CultureInfo.InvariantCulture) : 0f,
                };
                strength.Suffix = list.Count == 0 ? "" : "@" + item.Replace(':', '-');
                list.Add(strength);
            }
            return list.ToArray();
        }

        // 1 つの役だけを候補にして配列を束ね直す（candidate が null なら全部の役が素材なし）
        private static void Use(Candidate candidate, Prepared prepared, Strength strength)
        {
            int role = candidate == null ? -1 : Array.IndexOf(RegionSurfaceMaps.Folders, candidate.role);
            RegionSurfaceMaterials.SourceOverride = r => r == role ? prepared.Maps : null;
            RegionSurfaceMaterials.TileOverride = r => r == role ? candidate.tileMetres : 0f;
            RegionSurfaceMaterials.TintOverride = r => r == role ? prepared.Tint : Color.gray;
            RegionSurfaceMaterials.Contrast = strength.Contrast <= 0f ? 1f : strength.Contrast;
            RegionSurfaceMaterials.PhotoColor = strength.Color;
            RegionSurfaceMaterials.Bind();
        }

        private static void ShootFixed(Camera camera, RegionTerrainView.LookResult look, string folder, Candidate candidate, Prepared prepared)
        {
            Use(candidate, prepared, new Strength { Contrast = 1f });
            Debug.Log("[RegionMap] 素材の候補 " + (candidate == null ? "none" : candidate.id) + ": " + RegionSurfaceMaterials.Summary);
            ShootFixedViews(camera, look, folder);
        }

        // 固定の遠景・中景・近景とアルコーブ（中・近）を folder へ撮る
        private static void ShootFixedViews(Camera camera, RegionTerrainView.LookResult look, string folder)
        {
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

        private static void Place(Camera camera, View view)
        {
            camera.transform.position = view.Position;
            camera.transform.rotation = Quaternion.Euler(view.Pitch, view.Yaw, 0f);
            camera.fieldOfView = ViewFov;
        }

        // 内側の窓の地面の上 1.7 m から、25 m 先・60 m 先の地面（+1 m）を見る視点を並べる（間を地形が遮る視点は除く）。
        // 役ごとに、最初の 2 つの候補で描き比べて違う画素の割合が大きい視点を、距離ごとに 1 つ選ぶ
        private static Dictionary<string, View[]> SearchViews(Camera camera, RegionLookScene scene, Manifest manifest,
            Dictionary<string, Prepared> cache, Strength strength, int seed)
        {
            var random = new System.Random(seed * 7919 + 17);
            int points = int.Parse(RegionMapMenu.Argument("-searchPoints", "160"));
            float half = Mathf.Min(450f, 0.5f * (scene.InnerMaxX - scene.InnerMinX) - 80f);
            var views = new List<View>();
            for (int i = 0; i < points; i++)
            {
                float x = scene.CenterX + (float)(random.NextDouble() * 2.0 - 1.0) * half;
                float z = scene.CenterZ + (float)(random.NextDouble() * 2.0 - 1.0) * half;
                float eye = scene.HeightAt(x, z) + EyeHeight;
                float baseAngle = (float)(random.NextDouble() * Math.PI * 2.0);
                for (int k = 0; k < 6; k++)
                foreach (float distance in Distances)
                {
                    float angle = baseAngle + k * Mathf.PI / 3f;
                    float tx = x + Mathf.Cos(angle) * distance, tz = z + Mathf.Sin(angle) * distance;
                    float th = scene.HeightAt(tx, tz) + 1f;
                    if (!Clear(scene, x, z, eye, tx, tz, th)) continue;
                    var position = new Vector3(x, eye, -z);
                    Vector3 euler = Quaternion.LookRotation(new Vector3(tx, th, -tz) - position, Vector3.up).eulerAngles;
                    float pitch = euler.x > 180f ? euler.x - 360f : euler.x;
                    if (pitch < -40f || pitch > 45f) continue;
                    views.Add(new View { Position = position, Yaw = euler.y, Pitch = pitch, Distance = distance });
                }
            }
            var target = new RenderTexture(SearchWidth, SearchHeight, 24, RenderTextureFormat.ARGB32);
            var read = new Texture2D(SearchWidth, SearchHeight, TextureFormat.RGBA32, false);
            var result = new Dictionary<string, View[]>();
            try
            {
                camera.targetTexture = target;
                foreach (string role in RegionSurfaceMaps.Folders)
                {
                    Candidate[] pair = Array.FindAll(manifest.candidates, c => c.role == role);
                    if (pair.Length < 2) continue;
                    Use(pair[0], cache[pair[0].id], strength);
                    var first = new Color32[views.Count][];
                    for (int v = 0; v < views.Count; v++) first[v] = Render(camera, views[v], target, read);
                    Use(pair[1], cache[pair[1].id], strength);
                    var best = new View[Distances.Length];
                    for (int v = 0; v < views.Count; v++)
                    {
                        Color32[] pixels = Render(camera, views[v], target, read);
                        int changed = 0;
                        for (int i = 0; i < pixels.Length; i++)
                        {
                            Color32 a = pixels[i], b = first[v][i];
                            if (Math.Abs(a.r - b.r) > 6 || Math.Abs(a.g - b.g) > 6 || Math.Abs(a.b - b.b) > 6) changed++;
                        }
                        View view = views[v];
                        view.Score = (float)changed / pixels.Length;
                        int d = Array.IndexOf(Distances, view.Distance);
                        if (view.Score > best[d].Score) best[d] = view;
                    }
                    var chosen = new List<View>();
                    foreach (View view in best)
                        if (view.Score > 0f) chosen.Add(view);
                    result[role] = chosen.ToArray();
                    Debug.Log("[RegionMap] 素材の候補: " + role + " の視点 " + string.Join("、", chosen.ConvertAll(c =>
                        c.Distance.ToString("0", CultureInfo.InvariantCulture) + " m 先 " + (100f * c.Score).ToString("0.0", CultureInfo.InvariantCulture) + "%")));
                }
            }
            finally
            {
                camera.targetTexture = null;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(read);
            }
            return result;
        }

        // 目から見る先までの線より地形が高い所が無いか（線の 8 割まで調べる。見る先の手前の斜面は数えない）
        private static bool Clear(RegionLookScene scene, float x, float z, float eye, float tx, float tz, float th)
        {
            for (int i = 1; i <= 16; i++)
            {
                float t = 0.8f * i / 16f;
                float h = scene.HeightAt(Mathf.Lerp(x, tx, t), Mathf.Lerp(z, tz, t));
                if (h > Mathf.Lerp(eye, th, t) - 0.3f) return false;
            }
            return true;
        }

        private static Color32[] Render(Camera camera, View view, RenderTexture target, Texture2D read)
        {
            Place(camera, view);
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            read.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            RenderTexture.active = previous;
            return read.GetPixels32();
        }

        private static void WriteViews(string path, Dictionary<string, View[]> roleViews)
        {
            var text = new StringBuilder("{\n");
            int r = 0;
            foreach (KeyValuePair<string, View[]> pair in roleViews)
            {
                text.Append("  \"").Append(pair.Key).Append("\": [");
                for (int i = 0; i < pair.Value.Length; i++)
                {
                    View v = pair.Value[i];
                    // 書式の末尾に "}}" を置くと直前の書式指定に食われるので、括弧は別に足す
                    text.Append(i == 0 ? "{" : ", {").Append(string.Format(CultureInfo.InvariantCulture,
                        "\"x\": {0:F2}, \"y\": {1:F2}, \"z\": {2:F2}, \"yaw\": {3:F1}, \"pitch\": {4:F1}, \"fov\": {5:F0}, \"distance\": {6:F0}, \"score\": {7:F4}",
                        v.Position.x, v.Position.y, v.Position.z, v.Yaw, v.Pitch, ViewFov, v.Distance, v.Score)).Append('}');
                }
                text.Append(++r < roleViews.Count ? "],\n" : "]\n");
            }
            File.WriteAllText(path, text.Append("}\n").ToString());
        }

        // 役ごとに、選んだ視点で「素材なし」と各候補（強さごと）を撮る
        private static void ShootRoles(Camera camera, string sunFolder, Manifest manifest, Dictionary<string, Prepared> cache,
            Dictionary<string, View[]> roleViews, Strength[] strengths)
        {
            foreach (KeyValuePair<string, View[]> pair in roleViews)
            {
                string role = pair.Key;
                if (pair.Value.Length == 0) continue;
                var shots = new List<(Candidate candidate, Strength strength, string name)> { (null, strengths[0], "none") };
                foreach (Candidate candidate in manifest.candidates)
                {
                    if (candidate.role != role) continue;
                    foreach (Strength strength in strengths) shots.Add((candidate, strength, candidate.id + strength.Suffix));
                }
                foreach ((Candidate candidate, Strength strength, string name) in shots)
                {
                    Use(candidate, candidate == null ? default : cache[candidate.id], strength);
                    // 配列を替えた直後の 1 枚を捨てる
                    Place(camera, pair.Value[0]);
                    RegionTerrainViewMenu.Capture(camera, null, null);
                    for (int i = 0; i < pair.Value.Length; i++)
                    {
                        string folder = Path.Combine(sunFolder, role, "v" + (i + 1));
                        Directory.CreateDirectory(folder);
                        Place(camera, pair.Value[i]);
                        RegionTerrainViewMenu.Capture(camera, folder, name + ".png");
                    }
                }
            }
        }

        // 素材を役のフォルダへ置いて 3 枚を作り、GPU の上へ写して返す。写真の平均の色（Linear）も返す。置いたフォルダは消す
        private static Prepared Prepare(Candidate candidate)
        {
            int role = Array.IndexOf(RegionSurfaceMaps.Folders, candidate.role);
            if (role < 0) throw new ArgumentException("役の名前が違う: " + candidate.role);
            if (!AssetDatabase.IsValidFolder(AssetRoot)) AssetDatabase.CreateFolder("Assets/Resources/RegionMap3D", "Surfaces");
            string folder = AssetRoot + "/" + candidate.role;
            if (AssetDatabase.IsValidFolder(folder)) throw new InvalidOperationException(folder + " が既にある。素材を置いた写しでは撮らない");
            AssetDatabase.CreateFolder(AssetRoot, candidate.role);
            // 一時的に置く素材の平均の色を設定へ書かない（候補の色は TintOverride で渡す）
            RegionSurfaceImport.StoreTints = false;
            try
            {
                int copied = 0;
                Color tint = Color.gray;
                foreach (string file in Directory.GetFiles(candidate.source))
                {
                    RegionSurfaceMaps.MapKind kind = RegionSurfaceMaps.Classify(file);
                    if (kind == RegionSurfaceMaps.MapKind.None || kind == RegionSurfaceMaps.MapKind.Generated) continue;
                    File.Copy(file, Path.Combine(folder, Path.GetFileName(file)));
                    if (kind == RegionSurfaceMaps.MapKind.Color) tint = MeanLinear(file);
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
                Debug.Log("[RegionMap] 素材の候補 " + candidate.id + "（" + candidate.role + "、横 " + candidate.tileMetres.ToString("0.##", CultureInfo.InvariantCulture)
                          + " m、平均の色 " + tint.r.ToString("0.000", CultureInfo.InvariantCulture) + "/" + tint.g.ToString("0.000", CultureInfo.InvariantCulture)
                          + "/" + tint.b.ToString("0.000", CultureInfo.InvariantCulture) + "）を用意");
                return new Prepared { Maps = maps, Tint = tint };
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
                RegionSurfaceImport.StoreTints = true;
            }
        }

        // 色の画像（sRGB）の画素を Linear に直して平均する
        private static Color MeanLinear(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                return texture.LoadImage(File.ReadAllBytes(path)) ? RegionSurfaceMaps.MeanLinear(texture.GetPixels()) : Color.gray;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}

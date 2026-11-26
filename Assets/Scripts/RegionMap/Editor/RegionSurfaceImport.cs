using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Vastcore.RegionMap.Editor
{
    /// <summary>
    /// 表面の写真素材の取り込み。<c>Assets/Resources/RegionMap3D/Surfaces/&lt;役&gt;/</c> に素材の画像（色・法線・高さ・粗さ・AO）を置くと、
    /// 同じフォルダへ配列に入れる 3 枚（surface-detail・surface-normal・surface-hra、1024²）を作る。
    /// 法線は、法線の画像 → 高さの画像 → 色の画像の順にある物から作る。元の画像は材料なので、画像としては 32² で取り込む（実行ファイルを重くしない）。
    /// 素材を外すと 3 枚も消す。設計は docs/02_design/REGION_SURFACE_MATERIALS_SPEC.md 3.1・3.2 節。
    /// </summary>
    public sealed class RegionSurfaceImport : AssetPostprocessor
    {
        private const string AssetRoot = "Assets/Resources/" + RegionSurfaceMaps.Root + "/";
        private const string SettingsPath = AssetRoot + "SurfaceSettings.asset";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(AssetRoot, StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter;
            string name = Path.GetFileNameWithoutExtension(assetPath);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.npotScale = TextureImporterNPOTScale.None;
            if (RegionSurfaceMaps.Classify(assetPath) != RegionSurfaceMaps.MapKind.Generated)
            {
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 32;
                importer.textureCompression = TextureImporterCompression.Compressed;
                return;
            }
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.maxTextureSize = RegionSurfaceMaps.Texels;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            // 配列は 1 本の中で同じ形式が要るので、形式を決めて揃える
            TextureImporterFormat format = name == RegionSurfaceMaps.NormalName ? TextureImporterFormat.BC5 : TextureImporterFormat.BC7;
            TextureImporterPlatformSettings standalone = importer.GetPlatformTextureSettings("Standalone");
            standalone.overridden = true;
            standalone.maxTextureSize = RegionSurfaceMaps.Texels;
            standalone.format = format;
            importer.SetPlatformTextureSettings(standalone);
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            var folders = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string[] list in new[] { imported, deleted, moved, movedFrom })
            foreach (string path in list)
            {
                if (!path.StartsWith(AssetRoot, StringComparison.Ordinal)) continue;
                if (path == SettingsPath)
                {
                    for (int role = 0; role < RegionSurfaceMaps.Folders.Length; role++) folders.Add(AssetRoot + RegionSurfaceMaps.Folders[role]);
                    continue;
                }
                RegionSurfaceMaps.MapKind kind = RegionSurfaceMaps.Classify(path);
                if (kind == RegionSurfaceMaps.MapKind.Generated || kind == RegionSurfaceMaps.MapKind.None) continue;
                string folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
                if (folder != null && RoleOf(folder) >= 0) folders.Add(folder);
            }
            foreach (string folder in folders) Rebuild(folder);
        }

        /// <summary>全部の役の 3 枚を作り直す。</summary>
        [MenuItem("Vastcore/Region Map/Rebuild Surface Materials")]
        public static void RebuildAll()
        {
            for (int role = 0; role < RegionSurfaceMaps.Folders.Length; role++) Rebuild(AssetRoot + RegionSurfaceMaps.Folders[role]);
        }

        private static int RoleOf(string folder)
        {
            if (!folder.StartsWith(AssetRoot, StringComparison.Ordinal)) return -1;
            return Array.IndexOf(RegionSurfaceMaps.Folders, folder.Substring(AssetRoot.Length));
        }

        /// <summary>1 つの役のフォルダの 3 枚を、置かれた素材から作る（無ければ消す）。</summary>
        public static void Rebuild(string folder)
        {
            int role = RoleOf(folder);
            if (role < 0) return;
            string roleName = RegionSurfaceMaps.Folders[role];
            var sources = new Dictionary<RegionSurfaceMaps.MapKind, string>();
            if (Directory.Exists(folder))
            {
                string[] files = Directory.GetFiles(folder);
                Array.Sort(files, StringComparer.Ordinal);
                foreach (string file in files)
                {
                    RegionSurfaceMaps.MapKind kind = RegionSurfaceMaps.Classify(file);
                    if (kind == RegionSurfaceMaps.MapKind.None || kind == RegionSurfaceMaps.MapKind.Generated) continue;
                    if (sources.ContainsKey(kind))
                        Debug.LogWarning("[RegionMap] 表面の素材 " + roleName + ": " + kind + " が 2 つある。先の " + Path.GetFileName(sources[kind]) + " を使う");
                    else sources[kind] = file.Replace('\\', '/');
                }
            }
            if (!sources.ContainsKey(RegionSurfaceMaps.MapKind.Color))
            {
                if (sources.Count > 0) Debug.LogWarning("[RegionMap] 表面の素材 " + roleName + ": 色の画像が無いので使わない");
                Remove(folder);
                return;
            }

            RegionSurfaceSettings.Entry entry = LoadSettings()[role];
            int size = RegionSurfaceMaps.Texels;
            // 縦横の比が整数倍の画像は、正方形へ並べる時に縦か横へ繰り返すので、1 枚の実寸は横の長さで決まる
            float metresPerPixel = entry.TileMetres / size;
            Color[] color = Load(sources[RegionSurfaceMaps.MapKind.Color], size);
            Color[] detail = RegionSurfaceMaps.Detail(color);

            float[] height;
            string heightFrom;
            if (sources.TryGetValue(RegionSurfaceMaps.MapKind.Height, out string heightFile))
            {
                height = RegionSurfaceMaps.HeightFromDisplacement(Load(heightFile, size), entry.HeightMetres);
                heightFrom = "高さ";
            }
            else
            {
                height = RegionSurfaceMaps.HeightFromColor(color, size, metresPerPixel, entry.HeightMetres);
                heightFrom = "色";
            }

            Vector2[] slopes;
            string normalFrom;
            if (sources.TryGetValue(RegionSurfaceMaps.MapKind.NormalGL, out string normalFile))
            {
                slopes = RegionSurfaceMaps.SlopesFromNormals(Load(normalFile, size), false);
                normalFrom = "法線（GL）";
            }
            else if (sources.TryGetValue(RegionSurfaceMaps.MapKind.NormalDX, out normalFile))
            {
                slopes = RegionSurfaceMaps.SlopesFromNormals(Load(normalFile, size), true);
                normalFrom = "法線（DX、緑を反転）";
            }
            else
            {
                slopes = RegionSurfaceMaps.Slopes(height, size, metresPerPixel);
                normalFrom = heightFrom + "から作成";
            }

            Color[] roughness = null, occlusion = null;
            if (sources.TryGetValue(RegionSurfaceMaps.MapKind.Roughness, out string roughnessFile)) roughness = Load(roughnessFile, size);
            if (sources.TryGetValue(RegionSurfaceMaps.MapKind.Occlusion, out string occlusionFile)) occlusion = Load(occlusionFile, size);
            if (sources.TryGetValue(RegionSurfaceMaps.MapKind.Arm, out string armFile))
            {
                Color[] arm = Load(armFile, size);
                if (occlusion == null) occlusion = Channel(arm, 0);
                if (roughness == null) roughness = Channel(arm, 1);
            }
            Color[] hra = RegionSurfaceMaps.PackHeightRoughnessOcclusion(height, entry.HeightMetres, roughness, occlusion, entry.Roughness);

            int written = Write(folder, RegionSurfaceMaps.DetailName, detail, size)
                        + Write(folder, RegionSurfaceMaps.NormalName, RegionSurfaceMaps.EncodeNormals(slopes), size)
                        + Write(folder, RegionSurfaceMaps.HraName, hra, size);
            Debug.Log("[RegionMap] 表面の素材 " + roleName + ": 色 " + Path.GetFileName(sources[RegionSurfaceMaps.MapKind.Color])
                      + "、法線は" + normalFrom + "、高さは" + heightFrom + (roughness != null ? "、粗さあり" : "") + (occlusion != null ? "、AO あり" : "")
                      + "、横 " + entry.TileMetres.ToString("0.##") + " m（書き換え " + written + " 枚）");
        }

        private static RegionSurfaceSettings.Entry[] LoadSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<RegionSurfaceSettings>(SettingsPath);
            RegionSurfaceSettings.Entry[] defaults = RegionSurfaceSettings.Defaults();
            if (settings == null || settings.Roles == null) return defaults;
            for (int i = 0; i < Math.Min(defaults.Length, settings.Roles.Length); i++) defaults[i] = settings.Roles[i];
            return defaults;
        }

        // 元のファイルを読み直して、size×size の正方形にする（取り込んだ画像は 32² に縮めてあるため使わない）
        private static Color[] Load(string path, int size)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(path))) throw new InvalidDataException("画像として読めない: " + path);
                return RegionSurfaceMaps.ToSquare(texture.GetPixels(), texture.width, texture.height, size);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static Color[] Channel(Color[] pixels, int channel)
        {
            var result = new Color[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                float v = pixels[i][channel];
                result[i] = new Color(v, v, v, 1f);
            }
            return result;
        }

        // 中身が変わった時だけ書いて取り込み直す（取り込みの繰り返しを起こさない）。書いた枚数を返す
        private static int Write(string folder, string name, Color[] pixels, int size)
        {
            string path = folder + "/" + name + ".png";
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            byte[] bytes;
            try
            {
                texture.SetPixels(pixels);
                bytes = texture.EncodeToPNG();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
            if (File.Exists(path) && SameBytes(File.ReadAllBytes(path), bytes)) return 0;
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return 1;
        }

        private static bool SameBytes(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        private static void Remove(string folder)
        {
            foreach (string name in new[] { RegionSurfaceMaps.DetailName, RegionSurfaceMaps.NormalName, RegionSurfaceMaps.HraName })
            {
                string path = folder + "/" + name + ".png";
                if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
            }
        }
    }
}

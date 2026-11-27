using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Vastcore.TexturePipeline.Editor
{
    /// <summary>
    /// 規則の読み込み（JsonUtility）と、取り込みの設定・画素の検査の Unity 側の部分。
    /// 規則は docs/02_design/texture-rules.json（tools/TextureAudit と同じ物）。
    /// </summary>
    public static class TexturePipelineUnity
    {
        private static TextureRules cached;
        private static DateTime cachedStamp;

        public static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        public static string RulesFullPath => Path.Combine(ProjectRoot, TextureRules.DefaultRelativePath);

        /// <summary>規則を読む（ファイルの更新時刻が変わった時だけ読み直す）。無い・壊れている時は null と警告。</summary>
        public static TextureRules Rules
        {
            get
            {
                string path = RulesFullPath;
                if (!File.Exists(path)) return null;
                DateTime stamp = File.GetLastWriteTimeUtc(path);
                if (cached != null && stamp == cachedStamp) return cached;
                try
                {
                    TextureRules rules = JsonUtility.FromJson<TextureRules>(File.ReadAllText(path));
                    List<string> problems = rules.Validate();
                    if (problems.Count > 0)
                    {
                        Debug.LogWarning("[Texture] 規則の表に問題があるので使わない: " + string.Join("; ", problems));
                        return null;
                    }
                    cached = rules;
                    cachedStamp = stamp;
                    return rules;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Texture] 規則を読めない: " + path + " (" + e.Message + ")");
                    return null;
                }
            }
        }

        public static TextureManifest ParseManifest(string text)
        {
            return JsonUtility.FromJson<TextureManifest>(text) ?? throw new InvalidDataException("空の記録");
        }

        /// <summary>
        /// 種類と場所に合う取り込みの設定を入れる。変えた項目の説明を返す（変えなければ空）。
        /// sRGB・Normal map の型・緑の反転・mipmap・最大の辺・圧縮・Read/Write を決める。
        /// </summary>
        public static List<string> ApplyRules(TextureImporter importer, TextureRules rules, TextureZone zone, TextureKindRule kind)
        {
            var changes = new List<string>();
            bool ui = importer.textureType == TextureImporterType.Sprite || importer.textureType == TextureImporterType.GUI;
            if (kind != null)
            {
                if (kind.normalMap && importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    changes.Add("型 → Normal map");
                }
                if (!kind.normalMap && importer.textureType == TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.Default;
                    changes.Add("型 → Default");
                }
                bool flip = kind.name == TextureKindRule.NormalDX;
                if (kind.normalMap && importer.flipGreenChannel != flip)
                {
                    importer.flipGreenChannel = flip;
                    changes.Add("緑の反転 → " + (flip ? "入（DX 版）" : "切"));
                }
                if (!kind.normalMap && importer.sRGBTexture != kind.srgb)
                {
                    importer.sRGBTexture = kind.srgb;
                    changes.Add("sRGB → " + (kind.srgb ? "入" : "切"));
                }
                TextureImporterCompression compression = kind.compression == "hq" ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Compressed;
                if (importer.textureCompression != compression && (importer.textureCompression == TextureImporterCompression.Uncompressed || zone.requireCompression))
                {
                    importer.textureCompression = compression;
                    changes.Add("圧縮 → " + compression);
                }
            }
            if (zone.requireMips && !ui && !importer.mipmapEnabled)
            {
                importer.mipmapEnabled = true;
                changes.Add("mipmap → 入");
            }
            if (zone.requireMips && !ui && importer.anisoLevel < 4)
            {
                importer.anisoLevel = 4;
                changes.Add("aniso → 4");
            }
            if (zone.maxImport > 0 && importer.maxTextureSize > zone.maxImport)
            {
                importer.maxTextureSize = zone.maxImport;
                changes.Add("最大の辺 → " + zone.maxImport);
            }
            TextureImporterPlatformSettings standalone = importer.GetPlatformTextureSettings("Standalone");
            if (zone.maxImport > 0 && standalone.overridden && standalone.maxTextureSize > zone.maxImport)
            {
                standalone.maxTextureSize = zone.maxImport;
                importer.SetPlatformTextureSettings(standalone);
                changes.Add("Standalone の最大の辺 → " + zone.maxImport);
            }
            if (importer.isReadable)
            {
                importer.isReadable = false;
                changes.Add("Read/Write → 切");
            }
            return changes;
        }

        /// <summary>
        /// 画素の検査（TextureQuality）。元のファイル（PNG・JPG）を読み直し、長い辺が 1024 を超える時は区画の平均で縮めてから測る。
        /// 読めない形式は空を返す。
        /// </summary>
        public static List<TextureFinding> CheckPixels(string relativePath, TextureKindRule kind, bool tiling)
        {
            string extension = Path.GetExtension(relativePath).ToLowerInvariant();
            if (extension != ".png" && extension != ".jpg" && extension != ".jpeg") return new List<TextureFinding>();
            string full = Path.Combine(ProjectRoot, relativePath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(full))) return new List<TextureFinding>();
                Color[] pixels = texture.GetPixels();
                int width = texture.width, height = texture.height;
                int factor = 1;
                while (Math.Max(width, height) / factor > 1024) factor *= 2;
                int w = width / factor, h = height / factor;
                var r = new float[w * h];
                var g = new float[w * h];
                var b = new float[w * h];
                float inv = 1f / (factor * factor);
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float sr = 0, sg = 0, sb = 0;
                    for (int dy = 0; dy < factor; dy++)
                    for (int dx = 0; dx < factor; dx++)
                    {
                        Color c = pixels[(y * factor + dy) * width + x * factor + dx];
                        sr += c.r;
                        sg += c.g;
                        sb += c.b;
                    }
                    int i = y * w + x;
                    r[i] = sr * inv;
                    g[i] = sg * inv;
                    b[i] = sb * inv;
                }
                return TextureQuality.Check(relativePath, kind?.name, tiling, r, g, b, w, h);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        /// <summary>静的な検査に、画素の検査を足した結果（Editor のメニューとバッチの入口が使う）。</summary>
        public static TextureAuditResult Audit(bool pixels)
        {
            TextureRules rules = Rules;
            if (rules == null) throw new InvalidOperationException("規則が無い・読めない: " + RulesFullPath);
            TextureAuditResult result = TextureAuditor.Run(ProjectRoot, rules, ParseManifest);
            if (!pixels) return result;
            foreach (TextureRecord record in result.records)
            {
                TextureZone zone = rules.ZoneFor(record.path);
                if (zone == null || zone.status == TextureZone.Ignored || record.kind == "generated") continue;
                TextureKindRule kind = rules.Classify(record.path);
                if (kind == null) continue;
                string metaPath = Path.Combine(ProjectRoot, record.path + ".meta");
                TextureImportFacts facts = File.Exists(metaPath) ? TextureImportFacts.Parse(File.ReadAllText(metaPath)) : new TextureImportFacts();
                bool tiling = zone.IsOwned || (facts.WrapU != 1 && facts.EffectiveType != TextureImportFacts.TypeSprite && facts.EffectiveType != TextureImportFacts.TypeGui);
                foreach (TextureFinding finding in CheckPixels(record.path, kind, tiling))
                {
                    if (zone.status == TextureZone.Legacy)
                    {
                        finding.severity = TextureSeverity.Info;
                        finding.message = "[legacy] " + finding.message;
                    }
                    result.findings.Add(finding);
                }
            }
            result.Count();
            return result;
        }

        /// <summary>結果を artifacts/texture-audit/ へ書く（git 管理外）。書いた Markdown のパスを返す。</summary>
        public static string Write(TextureAuditResult result, string directory = null)
        {
            directory = directory ?? Path.Combine(ProjectRoot, "artifacts", "texture-audit");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "texture-audit-unity.json"), JsonUtility.ToJson(result, true));
            string markdown = Path.Combine(directory, "texture-audit-unity.md");
            File.WriteAllText(markdown, result.ToMarkdown());
            return markdown;
        }
    }

    /// <summary>
    /// 初めて取り込む画像（.meta がまだ無い物）に、場所と種類に合う既定の設定を入れ、画素の検査の結果を Console に出す。
    /// 既にある画像の設定は変えない（直すのはメニューの Apply Import Rules To Selection で明示する）。
    /// owner のある場所（RegionSurfaceImport の Surfaces など）と除外の場所は触らない。
    /// </summary>
    public sealed class TextureImportDefaults : AssetPostprocessor
    {
        private static readonly HashSet<string> FirstImports = new HashSet<string>(StringComparer.Ordinal);

        public override int GetPostprocessOrder() => 100;

        private void OnPreprocessTexture()
        {
            if (!assetImporter.importSettingsMissing) return;
            TextureRules rules = TexturePipelineUnity.Rules;
            if (rules == null || rules.IsExcluded(assetPath) || !rules.IsTextureFile(assetPath)) return;
            TextureZone zone = rules.ZoneFor(assetPath);
            if (zone == null || zone.status != TextureZone.Active) return;
            FirstImports.Add(assetPath);
            if (zone.IsOwned || !zone.applyImportDefaults) return;
            var importer = (TextureImporter)assetImporter;
            TextureKindRule kind = rules.Classify(assetPath);
            List<string> changes = TexturePipelineUnity.ApplyRules(importer, rules, zone, kind);
            if (changes.Count > 0)
                Debug.Log("[Texture] " + assetPath + "（" + (kind?.name ?? "種類不明") + "、" + zone.name + "）に既定の設定: " + string.Join("、", changes));
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (FirstImports.Count == 0) return;
            var paths = new List<string>(FirstImports);
            FirstImports.Clear();
            TextureRules rules = TexturePipelineUnity.Rules;
            if (rules == null) return;
            foreach (string path in paths)
            {
                TextureZone zone = rules.ZoneFor(path);
                if (zone == null || zone.IsGenerated(Path.GetFileName(path))) continue;
                TextureKindRule kind = rules.Classify(path);
                if (kind == null) continue;
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                bool tiling = zone.IsOwned || (importer != null && importer.wrapMode == TextureWrapMode.Repeat
                                               && importer.textureType != TextureImporterType.Sprite && importer.textureType != TextureImporterType.GUI);
                foreach (TextureFinding finding in TexturePipelineUnity.CheckPixels(path, kind, tiling))
                    Debug.LogWarning("[Texture] " + finding.code + " " + finding.path + ": " + finding.message);
            }
        }
    }

    /// <summary>Editor のメニューとバッチの入口。</summary>
    public static class TextureAuditMenu
    {
        [MenuItem("Vastcore/Textures/Audit Project (static + pixels)")]
        public static void AuditProject()
        {
            TextureAuditResult result = TexturePipelineUnity.Audit(true);
            string report = TexturePipelineUnity.Write(result);
            foreach (TextureFinding f in result.findings)
            {
                if (f.severity == TextureSeverity.Error) Debug.LogError("[Texture] " + f.code + " " + f.path + ": " + f.message);
                else if (f.severity == TextureSeverity.Warning) Debug.LogWarning("[Texture] " + f.code + " " + f.path + ": " + f.message);
            }
            Debug.Log("[Texture] " + result.Summary() + "。報告: " + report);
        }

        [MenuItem("Vastcore/Textures/Apply Import Rules To Selection")]
        public static void ApplyToSelection()
        {
            TextureRules rules = TexturePipelineUnity.Rules;
            if (rules == null)
            {
                Debug.LogWarning("[Texture] 規則が無い・読めない: " + TexturePipelineUnity.RulesFullPath);
                return;
            }
            int changed = 0;
            foreach (string guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer) || rules.IsExcluded(path)) continue;
                TextureZone zone = rules.ZoneFor(path);
                if (zone == null || zone.IsOwned || zone.status == TextureZone.Ignored)
                {
                    Debug.Log("[Texture] " + path + " は " + (zone?.owner ?? "規則の外") + " が設定を持つので変えない");
                    continue;
                }
                List<string> changes = TexturePipelineUnity.ApplyRules(importer, rules, zone, rules.Classify(path));
                if (changes.Count == 0) continue;
                importer.SaveAndReimport();
                changed++;
                Debug.Log("[Texture] " + path + ": " + string.Join("、", changes));
            }
            Debug.Log("[Texture] 選んだ画像のうち " + changed + " 枚の設定を規則に合わせた");
        }

        /// <summary>
        /// バッチの入口: Unity.exe -batchmode -projectPath . -executeMethod Vastcore.TexturePipeline.Editor.TextureAuditMenu.RunBatch -quit
        /// error があれば終了コード 1（-strict を付けると warning でも 1）。
        /// </summary>
        public static void RunBatch()
        {
            int code;
            try
            {
                TextureAuditResult result = TexturePipelineUnity.Audit(true);
                string report = TexturePipelineUnity.Write(result);
                foreach (TextureFinding f in result.findings)
                    if (f.severity != TextureSeverity.Info) Console.WriteLine(f.severity.ToUpperInvariant() + " " + f.code + " " + f.path + ": " + f.message);
                Console.WriteLine(result.Summary() + "。報告: " + report);
                bool strict = Array.IndexOf(Environment.GetCommandLineArgs(), "-strict") >= 0;
                code = result.errors > 0 || (strict && result.warnings > 0) ? 1 : 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("texture audit を実行できない: " + e);
                code = 2;
            }
            EditorApplication.Exit(code);
        }
    }
}

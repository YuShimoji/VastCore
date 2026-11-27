using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Vastcore.RegionMap;
using Vastcore.TexturePipeline;
using Vastcore.TexturePipeline.Editor;

namespace Vastcore.Tests.EditMode
{
    /// <summary>テクスチャの規則（docs/02_design/texture-rules.json）と、Unity 側の取り込み・検査（TEXTURE_PIPELINE_SPEC）。</summary>
    public class TexturePipelineTests
    {
        private const string TempFolder = "Assets/Art/__TexturePipelineTest";

        [Test]
        public void Rules_LoadWithJsonUtility_AndAreConsistent()
        {
            TextureRules rules = TexturePipelineUnity.Rules;
            Assert.IsNotNull(rules, "規則を読めない: " + TexturePipelineUnity.RulesFullPath);
            CollectionAssert.IsEmpty(rules.Validate());
            Assert.AreEqual("other", rules.ZoneFor("Assets/UI/x.png").name);
            Assert.IsTrue(rules.IsExcluded("Assets/WeatherMaker/Prefab/Textures/x.png"));
        }

        [Test]
        public void SurfaceZone_MatchesRegionSurfaceImport()
        {
            TextureRules rules = TexturePipelineUnity.Rules;
            TextureZone zone = rules.zones.Single(z => z.owner == "RegionSurfaceImport");
            Assert.AreEqual("Assets/Resources/" + RegionSurfaceMaps.Root + "/", zone.path);
            CollectionAssert.AreEqual(RegionSurfaceMaps.Folders, zone.roles);
            CollectionAssert.AreEquivalent(new[] { RegionSurfaceMaps.DetailName, RegionSurfaceMaps.NormalName, RegionSurfaceMaps.HraName }, zone.generatedNames);
            Assert.AreEqual(RegionSurfaceMaps.Texels, zone.generatedMaxImport);
            Assert.AreEqual(32, zone.sourceMaxImport);
        }

        [Test]
        public void Classify_AgreesWithRegionSurfaceMaps()
        {
            TextureRules rules = TexturePipelineUnity.Rules;
            TextureZone zone = rules.zones.Single(z => z.owner == "RegionSurfaceImport");
            var names = new[]
            {
                "Ground054_1K-JPG_Color.jpg", "Ground054_1K-JPG_NormalGL.jpg", "Ground054_1K-JPG_NormalDX.jpg", "Ground054_1K-JPG_Displacement.jpg",
                "Ground054_1K-JPG_Roughness.jpg", "Ground054_1K-JPG_AmbientOcclusion.jpg", "red_sandstone_wall_diff_2k.jpg", "red_sandstone_wall_nor_gl_2k.jpg",
                "red_sandstone_wall_nor_dx_2k.jpg", "red_sandstone_wall_arm_2k.jpg", "rock_disp_2k.png", "rock_ao.png", "rock_albedo.png", "Ground054.png",
                "Asphalt010_1K-JPG_Opacity.jpg", "rock_metallic.png", "notes.png",
            };
            var map = new Dictionary<RegionSurfaceMaps.MapKind, string>
            {
                { RegionSurfaceMaps.MapKind.Color, "color" }, { RegionSurfaceMaps.MapKind.NormalGL, "normal" }, { RegionSurfaceMaps.MapKind.NormalDX, "normaldx" },
                { RegionSurfaceMaps.MapKind.Height, "height" }, { RegionSurfaceMaps.MapKind.Roughness, "roughness" },
                { RegionSurfaceMaps.MapKind.Occlusion, "occlusion" }, { RegionSurfaceMaps.MapKind.Arm, "arm" },
            };
            foreach (string name in names)
            {
                RegionSurfaceMaps.MapKind surface = RegionSurfaceMaps.Classify(name);
                TextureKindRule kind = rules.Classify(name);
                string used = kind != null && zone.useKinds.Contains(kind.name) ? kind.name : null;
                string expected = map.TryGetValue(surface, out string s) ? s : null;
                Assert.AreEqual(expected, used, name + " の種類が取り込み（RegionSurfaceMaps.Classify）と検査で違う");
            }
        }

        [Test]
        public void NormalConvention_DetectsRealAmbientCgMaps()
        {
            TextureRules rules = TexturePipelineUnity.Rules;
            TextureKindRule normal = rules.kinds.Single(k => k.name == TextureKindRule.Normal);
            TextureKindRule normalDx = rules.kinds.Single(k => k.name == TextureKindRule.NormalDX);
            int checkedSets = 0;
            foreach (string id in new[] { "Ground054", "Ground081" })
            {
                string folder = "Assets/Resources/MapGenerator/Textures/" + id + "_1K-JPG/";
                string gl = folder + id + "_1K-JPG_NormalGL.jpg", dx = folder + id + "_1K-JPG_NormalDX.jpg";
                if (!File.Exists(gl) || !File.Exists(dx)) continue;
                checkedSets++;
                Assert.IsFalse(TexturePipelineUnity.CheckPixels(gl, normal, true).Any(f => f.code == "TQ105"), gl + " は GL と判定されるはず");
                Assert.IsFalse(TexturePipelineUnity.CheckPixels(dx, normalDx, true).Any(f => f.code == "TQ105"), dx + " は DX と判定されるはず");
                Assert.IsTrue(TexturePipelineUnity.CheckPixels(gl, normalDx, true).Any(f => f.code == "TQ105"), gl + " を DX と名乗らせたら食い違いを指摘するはず");
            }
            if (checkedSets == 0) Assert.Ignore("ambientCG の法線の組が無い");
        }

        [Test]
        public void Seam_PassesTileableAndFlagsCroppedPhoto()
        {
            const string path = "Assets/Resources/MapGenerator/Textures/Bricks102_1K-JPG/Bricks102_1K-JPG_Color.jpg";
            if (!File.Exists(path)) Assert.Ignore("Bricks102 が無い");
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)));
                int width = texture.width, height = texture.height;
                Color[] pixels = texture.GetPixels();
                var full = new float[width * height];
                for (int i = 0; i < full.Length; i++) full[i] = pixels[i].grayscale;
                // 継ぎ目の無い原本を、端を外して 60% に切り出すと継ぎ目ができる
                int cw = width * 6 / 10, ch = height * 6 / 10, ox = width / 5, oy = height / 7;
                var crop = new float[cw * ch];
                for (int y = 0; y < ch; y++)
                for (int x = 0; x < cw; x++)
                    crop[y * cw + x] = full[(y + oy) * width + x + ox];
                double tile = TextureQuality.SeamRatio(full, width, height), cropped = TextureQuality.SeamRatio(crop, cw, ch);
                Assert.Less(tile, TextureQuality.SeamLimit, "原本 " + tile);
                Assert.Greater(cropped, TextureQuality.SeamLimit, "切り出し " + cropped);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void StaticAudit_ProjectHasNoErrors()
        {
            TextureAuditResult result = TexturePipelineUnity.Audit(false);
            string errors = string.Join("\n", result.findings.Where(f => f.severity == TextureSeverity.Error).Select(f => f.code + " " + f.path + ": " + f.message));
            Assert.AreEqual(0, result.errors, errors);
            Assert.Greater(result.scanned, 0);
        }

        [Test]
        public void FirstImport_AppliesKindDefaults_InArtZone()
        {
            try
            {
                Directory.CreateDirectory(TempFolder);
                WritePng(TempFolder + "/probe_NormalGL.png", new Color(0.5f, 0.5f, 1f));
                WritePng(TempFolder + "/probe_Roughness.png", new Color(0.7f, 0.7f, 0.7f));
                WritePng(TempFolder + "/probe_Color.png", new Color(0.6f, 0.4f, 0.3f));
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                var normal = (TextureImporter)AssetImporter.GetAtPath(TempFolder + "/probe_NormalGL.png");
                var rough = (TextureImporter)AssetImporter.GetAtPath(TempFolder + "/probe_Roughness.png");
                var color = (TextureImporter)AssetImporter.GetAtPath(TempFolder + "/probe_Color.png");
                Assert.AreEqual(TextureImporterType.NormalMap, normal.textureType);
                Assert.IsFalse(normal.flipGreenChannel);
                Assert.IsFalse(rough.sRGBTexture);
                Assert.IsTrue(color.sRGBTexture);
                Assert.IsTrue(color.mipmapEnabled);
                Assert.AreEqual(TextureImporterCompression.CompressedHQ, color.textureCompression);
                Assert.LessOrEqual(color.maxTextureSize, 2048);

                // 既にある画像は変えない: 手で sRGB を入れた粗さは、取り込み直しても入のまま
                rough.sRGBTexture = true;
                rough.SaveAndReimport();
                Assert.IsTrue(((TextureImporter)AssetImporter.GetAtPath(TempFolder + "/probe_Roughness.png")).sRGBTexture);
            }
            finally
            {
                AssetDatabase.DeleteAsset(TempFolder);
                if (AssetDatabase.IsValidFolder("Assets/Art") && AssetDatabase.FindAssets(string.Empty, new[] { "Assets/Art" }).Length == 0)
                    AssetDatabase.DeleteAsset("Assets/Art");
            }
        }

        private static void WritePng(string path, Color color)
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);
            try
            {
                var pixels = new Color[64 * 64];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
                texture.SetPixels(pixels);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}

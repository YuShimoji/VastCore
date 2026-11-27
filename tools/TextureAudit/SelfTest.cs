using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vastcore.TexturePipeline;

namespace Vastcore.TextureAudit
{
    /// <summary>
    /// 検査そのものの試験（--self-test）。一時フォルダに小さなプロジェクトを作り、各 code が出る・出ないを確かめる。
    /// 規則は本物の docs/02_design/texture-rules.json を読む（場所の決め方・種類の名前の読みも試す）。
    /// </summary>
    public static class SelfTest
    {
        private static int failures;

        public static int Run()
        {
            failures = 0;
            string rulesPath = FindRules();
            TextureRules rules = Program.LoadRules(rulesPath);
            Check(rules.Validate().Count == 0, "規則の表が成り立つ: " + string.Join("; ", rules.Validate()));

            Classify(rules);
            Meta();
            Headers();
            Glob();
            Quality();
            EndToEnd(rules);

            Console.WriteLine(failures == 0 ? "self-test: すべて通過" : "self-test: 失敗 " + failures + " 件");
            return failures == 0 ? 0 : 1;
        }

        private static void Classify(TextureRules rules)
        {
            var cases = new Dictionary<string, string>
            {
                { "Ground054_1K-JPG_Color.jpg", "color" },
                { "Ground054_1K-JPG_NormalGL.jpg", "normal" },
                { "Ground054_1K-JPG_NormalDX.jpg", "normaldx" },
                { "Ground054_1K-JPG_Displacement.jpg", "height" },
                { "Ground054_1K-JPG_Roughness.jpg", "roughness" },
                { "Ground054_1K-JPG_AmbientOcclusion.jpg", "occlusion" },
                { "Asphalt010_1K-JPG_Opacity.jpg", "mask" },
                { "red_sandstone_wall_diff_2k.jpg", "color" },
                { "red_sandstone_wall_nor_gl_2k.jpg", "normal" },
                { "red_sandstone_wall_nor_dx_2k.jpg", "normaldx" },
                { "red_sandstone_wall_arm_2k.jpg", "arm" },
                { "red_sandstone_wall_disp_2k.png", "height" },
                { "DesertPlants.png", null },
                { "Ground054.png", null },
            };
            foreach (KeyValuePair<string, string> c in cases)
            {
                string got = rules.Classify(c.Key)?.name;
                Check(got == c.Value, "種類 " + c.Key + " → " + (c.Value ?? "null") + "（結果 " + (got ?? "null") + "）");
            }
            Check(rules.ZoneFor("Assets/Resources/RegionMap3D/Surfaces/Sand/a_Color.png").name == "terrain-surface", "場所: Surfaces");
            Check(rules.ZoneFor("Assets/Resources/RegionMap3D/DesertPlants.png").name == "region-3d", "場所: RegionMap3D");
            Check(rules.ZoneFor("Assets/UI/icon.png").name == "other", "場所: その他");
            Check(rules.IsExcluded("Assets/TextMesh Pro/Sprites/x.png"), "除外: TextMesh Pro");
        }

        private static void Meta()
        {
            const string meta = "fileFormatVersion: 2\nguid: 0\nTextureImporter:\n  mipmaps:\n    enableMipMap: 1\n    sRGBTexture: 0\n    flipGreenChannel: 1\n"
                              + "  isReadable: 1\n  maxTextureSize: 2048\n  textureSettings:\n    wrapU: 0\n  textureType: 1\n  platformSettings:\n"
                              + "  - serializedVersion: 4\n    buildTarget: DefaultTexturePlatform\n    maxTextureSize: 2048\n    textureFormat: -1\n    textureCompression: 1\n    overridden: 0\n"
                              + "  - serializedVersion: 4\n    buildTarget: Standalone\n    maxTextureSize: 1024\n    textureFormat: 4\n    textureCompression: 1\n    overridden: 1\n"
                              + "  spriteSheet:\n    serializedVersion: 2\n";
            TextureImportFacts f = TextureImportFacts.Parse(meta.Replace("\n", "\r\n"));
            Check(f.IsTextureImporter, "meta: importer");
            Check(f.SRgb == 0 && f.Mipmaps == 1 && f.FlipGreen == 1 && f.IsReadable == 1 && f.WrapU == 0 && f.TextureType == 1, "meta: 上の段の値");
            Check(f.MaxSize == 2048 && f.EffectiveMaxSize == 1024, "meta: Standalone の上書きの最大");
            Check(f.IsUncompressed, "meta: Standalone の RGBA32 は非圧縮");
            Check(TextureImportFacts.Parse("IHVImageFormatImporter:\n  isReadable: 0\n").Importer == "IHVImageFormatImporter", "meta: 別の importer");
        }

        private static void Headers()
        {
            byte[] png = Png(640, 320, 1);
            Check(ImageHeader.TryRead(png, out int w, out int h) && w == 640 && h == 320, "頭: PNG");
            byte[] jpeg = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x04, 0x00, 0x00, 0xFF, 0xC2, 0x00, 0x11, 0x08, 0x02, 0x00, 0x04, 0x00, 0x03, 0, 0, 0, 0, 0, 0 };
            Check(ImageHeader.TryRead(jpeg, out w, out h) && w == 1024 && h == 512, "頭: JPEG（progressive の SOF2）");
            var tga = new byte[18];
            tga[12] = 0x00; tga[13] = 0x01; tga[14] = 0x80; tga[15] = 0x00;
            Check(ImageHeader.TryReadTga(tga, out w, out h) && w == 256 && h == 128, "頭: TGA");
        }

        private static void Glob()
        {
            Check(TextureManifest.Glob("Ground054*", "Ground054_1K-JPG_Color.jpg"), "glob: 接頭辞");
            Check(TextureManifest.Glob("*.png", "A.PNG"), "glob: 大文字小文字");
            Check(!TextureManifest.Glob("Rock?.jpg", "Rock10.jpg"), "glob: ? は 1 字");
        }

        private static void Quality()
        {
            const int size = 64;
            // 継ぎ目の無い細かい模様: 乱数を周期の境をまたいでぼかす
            var random = new Random(7);
            var noise = new float[size * size];
            for (int i = 0; i < noise.Length; i++) noise[i] = (float)random.NextDouble();
            var tile = new float[size * size];
            var ramp = new float[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float sum = 0;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    sum += noise[((y + dy + size) % size) * size + (x + dx + size) % size];
                tile[y * size + x] = 0.3f + 0.4f * sum / 9f;
                ramp[y * size + x] = 0.1f + 0.8f * x / (size - 1f);
            }
            double seamTile = TextureQuality.SeamRatio(tile, size, size), seamRamp = TextureQuality.SeamRatio(ramp, size, size);
            Check(seamTile < 1.2, "継ぎ目: 周期の模様は小さい（" + seamTile.ToString("0.00") + "）");
            Check(seamRamp > TextureQuality.SeamLimit, "継ぎ目: 左右で明るさが違うと大きい（" + seamRamp.ToString("0.0") + "）");
            Check(TextureQuality.LightingVariation(ramp, size, size) > TextureQuality.LightingLimit, "むら: 明るさの傾きは検出");
            Check(TextureQuality.LightingVariation(tile, size, size) < TextureQuality.LightingLimit, "むら: 細かい模様は検出しない");

            // 高さの場から GL の法線を作る（行は下から上、緑 = +Y の傾きの逆）
            var height = new double[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                height[y * size + x] = Math.Sin(2 * Math.PI * x / size) * Math.Sin(2 * Math.PI * 2 * y / size)
                                       + 0.5 * Math.Cos(2 * Math.PI * 3 * x / size + 1) * Math.Sin(2 * Math.PI * y / size);
            var r = new float[size * size];
            var g = new float[size * size];
            var b = new float[size * size];
            var gDx = new float[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                double hx = (height[y * size + (x + 1) % size] - height[y * size + (x - 1 + size) % size]) * 0.5 * 4;
                double hy = (height[((y + 1) % size) * size + x] - height[((y - 1 + size) % size) * size + x]) * 0.5 * 4;
                double len = Math.Sqrt(hx * hx + hy * hy + 1);
                int i = y * size + x;
                r[i] = (float)((-hx / len + 1) * 0.5);
                g[i] = (float)((-hy / len + 1) * 0.5);
                b[i] = (float)((1 / len + 1) * 0.5);
                gDx[i] = 1 - g[i];
            }
            Check(TextureQuality.NormalConvention(r, g, b, size, size, out double ratioGl) == "gl", "法線の向き: GL（比 " + ratioGl.ToString("0.00") + "）");
            Check(TextureQuality.NormalConvention(r, gDx, b, size, size, out double ratioDx) == "dx", "法線の向き: DX（比 " + ratioDx.ToString("0.00") + "）");
            List<TextureFinding> wrong = TextureQuality.Check("x_NormalGL.png", TextureKindRule.Normal, true, r, gDx, b, size, size);
            Check(wrong.Any(f => f.code == "TQ105"), "法線の向き: 名前と中身の食い違いを指摘");
        }

        private static void EndToEnd(TextureRules rules)
        {
            string root = Path.Combine(Path.GetTempPath(), "vastcore-texture-selftest-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            int seed = 10;
            try
            {
                string art = "Assets/Art/Textures/Rock/";
                Texture(root, art + "rock_Color.png", 1024, 1024, Meta(srgb: 1, type: 0), ref seed);
                Texture(root, art + "rock_NormalGL.png", 1024, 1024, Meta(srgb: 0, type: 1), ref seed);
                Texture(root, art + "rock_Roughness.png", 1024, 1024, Meta(srgb: 1, type: 0), ref seed);            // TX004
                Texture(root, art + "rock_Displacement.png", 1024, 1024, Meta(srgb: 0, type: 0), ref seed);
                Texture(root, art + "rock_AO.png", 1024, 1024, Meta(srgb: 0, type: 1), ref seed);                  // TX005（法線でない Normal map）
                Texture(root, art + "rock_NormalDX.png", 1024, 1024, Meta(srgb: 0, type: 1), ref seed);            // TX006
                Texture(root, art + "rock_mask.png", 1000, 600, Meta(srgb: 0, type: 0), ref seed);                 // TX007
                Texture(root, art + "rock_metal.png", 1024, 1024, Meta(srgb: 0, type: 0, max: 4096), ref seed);    // TX008
                Texture(root, art + "rock_emission.png", 8192, 8192, Meta(srgb: 1, type: 0, readable: 1, mips: 0, compression: 0), ref seed); // TX009 TX010 TX011 TX016
                Texture(root, art + "rock_col_nometa.png", 1024, 1024, null, ref seed);                            // TX001
                File.Copy(Path.Combine(root, art + "rock_Color.png"), Path.Combine(root, art + "rock_copy_Color.png"));
                File.WriteAllText(Path.Combine(root, art + "rock_copy_Color.png.meta"), Meta(srgb: 1, type: 0));    // TX012
                Write(root, art + "texture-source.json",
                      "{ \"sets\": [ { \"files\": [\"rock_*\"], \"origin\": \"downloaded\", \"source\": \"https://example.invalid/rock\", \"license\": \"CC0-1.0\", \"author\": \"x\", \"tileMetres\": 8 } ] }");
                // TX017: 1024 px ÷ 8 m = 128 px/m は art の下限 128 と同じなので出ない。下は 2048 ÷ 32 m
                string coarse = "Assets/Art/Textures/Cliff/";
                Texture(root, coarse + "cliff_Color.png", 2048, 2048, Meta(srgb: 1, type: 0), ref seed);
                Write(root, coarse + "texture-source.json",
                      "{ \"sets\": [ { \"origin\": \"downloaded\", \"source\": \"https://example.invalid/cliff\", \"license\": \"CC-BY-4.0\", \"tileMetres\": 32 } ] }"); // TX003（author 無し）
                Texture(root, "Assets/Art/Textures/Loose/loose_Color.png", 512, 512, Meta(srgb: 1, type: 0), ref seed); // TX002

                string surfaces = "Assets/Resources/RegionMap3D/Surfaces/";
                Texture(root, surfaces + "Sand/sand_Roughness.jpg", 1024, 1024, Meta(srgb: 0, type: 0, max: 32), ref seed); // TX014（色なし）
                Texture(root, surfaces + "Sand/sand.png", 1024, 1024, Meta(srgb: 0, type: 0, max: 32), ref seed);            // TX013
                Texture(root, surfaces + "Sand/surface-normal.png", 1024, 1024, Meta(srgb: 0, type: 0, max: 1024), ref seed);
                Texture(root, surfaces + "Marble/marble_Color.jpg", 1024, 1024, Meta(srgb: 0, type: 0, max: 32), ref seed);  // TX014（役でない）
                Texture(root, surfaces + "Talus/talus_Color.jpg", 1024, 640, Meta(srgb: 0, type: 0, max: 32), ref seed);     // TX019
                Texture(root, surfaces + "Talus/talus_Color.tga", 1024, 1024, Meta(srgb: 0, type: 0, max: 32), ref seed);    // TX015・TX018
                Write(root, surfaces + "texture-source.json",
                      "{ \"sets\": [ { \"origin\": \"downloaded\", \"source\": \"https://example.invalid/s\", \"license\": \"CC0-1.0\", \"tileMetres\": 2 } ] }");

                Texture(root, "Assets/_Textures/old.png", 300, 300, Meta(srgb: 1, type: 0, readable: 1), ref seed);           // legacy → info
                Texture(root, "Assets/TextMesh Pro/x.png", 64, 64, null, ref seed);                                           // 除外

                TextureAuditResult result = TextureAuditor.Run(root, rules, Program.ParseManifest);
                Func<string, string, bool> has = (code, file) => result.findings.Any(f => f.code == code && f.path.EndsWith(file, StringComparison.Ordinal));
                Check(!result.findings.Any(f => f.path.EndsWith("rock_Color.png", StringComparison.Ordinal)), "正しい色の画像は指摘なし");
                Check(!result.findings.Any(f => f.path.EndsWith("rock_NormalGL.png", StringComparison.Ordinal)), "正しい法線は指摘なし");
                Check(!result.findings.Any(f => f.path.EndsWith("rock_Displacement.png", StringComparison.Ordinal)), "正しい高さは指摘なし");
                Check(has("TX001", "rock_col_nometa.png"), "TX001 .meta なし");
                Check(has("TX002", "loose_Color.png"), "TX002 出所の記録なし");
                Check(has("TX003", "cliff_Color.png"), "TX003 CC-BY の author なし");
                Check(has("TX004", "rock_Roughness.png"), "TX004 データの sRGB");
                Check(has("TX005", "rock_AO.png"), "TX005 法線でない Normal map");
                Check(has("TX006", "rock_NormalDX.png"), "TX006 DX の法線");
                Check(has("TX007", "rock_mask.png"), "TX007 2 の冪でない");
                Check(has("TX008", "rock_metal.png"), "TX008 取り込みの最大");
                Check(has("TX009", "rock_emission.png") && has("TX010", "rock_emission.png") && has("TX011", "rock_emission.png") && has("TX016", "rock_emission.png"),
                      "TX009・TX010・TX011・TX016");
                Check(has("TX012", "rock_copy_Color.png"), "TX012 同じ中身");
                Check(has("TX013", "Sand/sand.png"), "TX013 使われない名前");
                Check(result.findings.Any(f => f.code == "TX014" && f.path.EndsWith("Surfaces/Sand/", StringComparison.Ordinal)), "TX014 色の無い役");
                Check(has("TX014", "marble_Color.jpg"), "TX014 役でないフォルダ");
                Check(has("TX015", "talus_Color.tga") && has("TX018", "talus_Color.tga"), "TX015・TX018");
                Check(has("TX017", "cliff_Color.png"), "TX017 画素密度");
                Check(has("TX019", "talus_Color.jpg"), "TX019 縦横の比");
                Check(!has("TX002", "surface-normal.png"), "作った画像は出所の記録が要らない");
                Check(result.findings.Where(f => f.path.EndsWith("old.png", StringComparison.Ordinal)).All(f => f.severity == TextureSeverity.Info)
                      && has("TX002", "old.png"), "legacy は info");
                Check(!result.findings.Any(f => f.path.Contains("TextMesh Pro")), "除外の場所は見ない");
                Check(result.scanned == 20, "数えた枚数 20（結果 " + result.scanned + "）");
                if (failures > 0) Console.WriteLine(result.ToMarkdown());
            }
            finally
            {
                try { Directory.Delete(root, true); } catch (IOException) { }
            }
        }

        private static string Meta(int srgb, int type, int max = 2048, int readable = 0, int mips = 1, int compression = 1)
        {
            return "fileFormatVersion: 2\nguid: 0\nTextureImporter:\n  mipmaps:\n    enableMipMap: " + mips + "\n    sRGBTexture: " + srgb
                   + "\n    flipGreenChannel: 0\n  isReadable: " + readable + "\n  maxTextureSize: " + max + "\n  textureType: " + type
                   + "\n  platformSettings:\n  - serializedVersion: 4\n    buildTarget: DefaultTexturePlatform\n    maxTextureSize: " + max
                   + "\n    textureFormat: -1\n    textureCompression: " + compression + "\n    overridden: 0\n";
        }

        private static void Texture(string root, string rel, int width, int height, string meta, ref int seed)
        {
            string path = Path.Combine(root, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            byte[] bytes;
            if (rel.EndsWith(".tga", StringComparison.Ordinal))
            {
                bytes = new byte[32];
                bytes[12] = (byte)width; bytes[13] = (byte)(width >> 8); bytes[14] = (byte)height; bytes[15] = (byte)(height >> 8);
                bytes[20] = (byte)seed;
            }
            else if (rel.EndsWith(".jpg", StringComparison.Ordinal))
                bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x11, 0x08, (byte)(height >> 8), (byte)height, (byte)(width >> 8), (byte)width, 0x03, (byte)seed, 0, 0, 0, 0, 0, 0, 0 };
            else bytes = Png(width, height, seed);
            seed++;
            File.WriteAllBytes(path, bytes);
            if (meta != null) File.WriteAllText(path + ".meta", meta);
        }

        private static byte[] Png(int width, int height, int seed)
        {
            var b = new byte[40];
            byte[] sig = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, (byte)'I', (byte)'H', (byte)'D', (byte)'R' };
            Array.Copy(sig, b, sig.Length);
            b[16] = (byte)(width >> 24); b[17] = (byte)(width >> 16); b[18] = (byte)(width >> 8); b[19] = (byte)width;
            b[20] = (byte)(height >> 24); b[21] = (byte)(height >> 16); b[22] = (byte)(height >> 8); b[23] = (byte)height;
            b[30] = (byte)seed;
            b[31] = (byte)(seed >> 8);
            return b;
        }

        private static void Write(string root, string rel, string text)
        {
            string path = Path.Combine(root, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text, new UTF8Encoding(false));
        }

        private static string FindRules()
        {
            string dir = AppContext.BaseDirectory;
            for (int i = 0; i < 10 && dir != null; i++)
            {
                string candidate = Path.Combine(dir, TextureRules.DefaultRelativePath);
                if (File.Exists(candidate)) return candidate;
                dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar));
            }
            string cwd = Path.Combine(Directory.GetCurrentDirectory(), TextureRules.DefaultRelativePath);
            if (File.Exists(cwd)) return cwd;
            throw new FileNotFoundException("規則が見つからない: " + TextureRules.DefaultRelativePath);
        }

        private static void Check(bool ok, string name)
        {
            if (ok) return;
            failures++;
            Console.WriteLine("FAIL " + name);
        }
    }
}

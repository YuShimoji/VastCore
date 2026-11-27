using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Vastcore.TexturePipeline
{
    /// <summary>
    /// Unity の .meta（TextureImporter）の、検査に要る値。Unity を起動せずに .meta の文を読む。
    /// 値が書かれていない項目は -1。
    /// </summary>
    public sealed class TextureImportFacts
    {
        // Unity の TextureImporterType の値
        public const int TypeDefault = 0, TypeNormalMap = 1, TypeGui = 2, TypeSprite = 8, TypeLightmap = 6, TypeSingleChannel = 10;
        // TextureImporterCompression の値
        public const int Uncompressed = 0;

        public string Importer = string.Empty;
        public int TextureType = -1;
        public int SRgb = -1;
        public int Mipmaps = -1;
        public int MaxSize = -1;
        public int IsReadable = -1;
        public int WrapU = -1;
        public int FlipGreen = -1;
        public int AlphaIsTransparency = -1;
        public readonly List<PlatformFacts> Platforms = new List<PlatformFacts>();

        public sealed class PlatformFacts
        {
            public string Target = string.Empty;
            public int MaxSize = -1;
            public int Format = -1;
            public int Compression = -1;
            public bool Overridden;
        }

        public bool IsTextureImporter => Importer == "TextureImporter";

        /// <summary>-1（古い .meta の未設定）は Default として扱う。</summary>
        public int EffectiveType => TextureType < 0 ? TypeDefault : TextureType;

        public PlatformFacts Platform(string target)
        {
            foreach (PlatformFacts platform in Platforms)
                if (platform.Target == target) return platform;
            return null;
        }

        /// <summary>Standalone で実際に使う最大の辺（上書きがあればそれ、無ければ既定）。</summary>
        public int EffectiveMaxSize
        {
            get
            {
                PlatformFacts standalone = Platform("Standalone");
                if (standalone != null && standalone.Overridden && standalone.MaxSize > 0) return standalone.MaxSize;
                PlatformFacts fallback = Platform("DefaultTexturePlatform");
                if (fallback != null && fallback.MaxSize > 0) return fallback.MaxSize;
                return MaxSize;
            }
        }

        // TextureFormat のうち圧縮しない形式（Alpha8・RGB24・RGBA32・ARGB32・RGB565・R16・RGBA4444・BGRA32・RHalf・RGHalf・RGBAHalf・RFloat・RGFloat・RGBAFloat・R8・RG16・RG32・RGB48・RGBA64）
        private static readonly int[] UncompressedFormats = { 1, 3, 4, 5, 7, 9, 13, 14, 15, 16, 17, 18, 19, 20, 62, 63, 72, 73, 74 };

        /// <summary>Standalone で圧縮しない設定か。</summary>
        public bool IsUncompressed
        {
            get
            {
                PlatformFacts standalone = Platform("Standalone");
                if (standalone != null && standalone.Overridden && standalone.Format >= 0)
                    return Array.IndexOf(UncompressedFormats, standalone.Format) >= 0;
                if (standalone != null && standalone.Overridden && standalone.Compression >= 0)
                    return standalone.Compression == Uncompressed;
                PlatformFacts fallback = Platform("DefaultTexturePlatform");
                return fallback != null && fallback.Compression == Uncompressed;
            }
        }

        /// <summary>.meta の文を読む。TextureImporter 以外の importer でも Importer の名前だけは入る。</summary>
        public static TextureImportFacts Parse(string metaText)
        {
            var facts = new TextureImportFacts();
            bool inPlatforms = false;
            PlatformFacts current = null;
            string[] lines = metaText.Replace("\r\n", "\n").Split('\n');
            foreach (string raw in lines)
            {
                if (raw.Length == 0) continue;
                int indent = 0;
                while (indent < raw.Length && raw[indent] == ' ') indent++;
                string line = raw.Substring(indent);
                if (indent == 0 && line.EndsWith("Importer:", StringComparison.Ordinal))
                {
                    facts.Importer = line.Substring(0, line.Length - 1);
                    continue;
                }
                if (indent == 2 && line == "platformSettings:")
                {
                    inPlatforms = true;
                    continue;
                }
                if (inPlatforms)
                {
                    if (indent == 2 && line.StartsWith("- ", StringComparison.Ordinal))
                    {
                        current = new PlatformFacts();
                        facts.Platforms.Add(current);
                        line = line.Substring(2);
                        indent = 4;
                    }
                    else if (indent <= 2)
                    {
                        inPlatforms = false;
                        current = null;
                    }
                }
                int colon = line.IndexOf(':');
                if (colon <= 0) continue;
                string key = line.Substring(0, colon);
                string value = line.Substring(colon + 1).Trim();
                if (inPlatforms && current != null)
                {
                    switch (key)
                    {
                        case "buildTarget": current.Target = value; break;
                        case "maxTextureSize": current.MaxSize = Int(value); break;
                        case "textureFormat": current.Format = Int(value); break;
                        case "textureCompression": current.Compression = Int(value); break;
                        case "overridden": current.Overridden = Int(value) == 1; break;
                    }
                    continue;
                }
                switch (key)
                {
                    case "textureType": facts.TextureType = Int(value); break;
                    case "sRGBTexture": facts.SRgb = Int(value); break;
                    case "enableMipMap": facts.Mipmaps = Int(value); break;
                    case "maxTextureSize": if (facts.MaxSize < 0) facts.MaxSize = Int(value); break;
                    case "isReadable": facts.IsReadable = Int(value); break;
                    case "wrapU": facts.WrapU = Int(value); break;
                    case "flipGreenChannel": facts.FlipGreen = Int(value); break;
                    case "alphaIsTransparency": facts.AlphaIsTransparency = Int(value); break;
                }
            }
            return facts;
        }

        private static int Int(string value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : -1;
        }
    }

    /// <summary>画像ファイルの先頭から縦横の画素数を読む（PNG・JPEG・TGA・PSD・BMP）。読めない形式は 0。</summary>
    public static class ImageHeader
    {
        public static bool TryRead(string path, out int width, out int height)
        {
            width = height = 0;
            byte[] head;
            using (FileStream stream = File.OpenRead(path))
            {
                // JPEG の SOF は先頭から離れることがある（EXIF のサムネイル）ので、最大 1 MB まで読む
                int length = (int)Math.Min(stream.Length, 1 << 20);
                head = new byte[length];
                int read = 0;
                while (read < length)
                {
                    int n = stream.Read(head, read, length - read);
                    if (n <= 0) break;
                    read += n;
                }
            }
            return TryRead(head, out width, out height);
        }

        public static bool TryRead(byte[] b, out int width, out int height)
        {
            width = height = 0;
            if (b.Length >= 24 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47)
            {
                width = BigEndian(b, 16);
                height = BigEndian(b, 20);
                return width > 0 && height > 0;
            }
            if (b.Length >= 4 && b[0] == 0xFF && b[1] == 0xD8) return Jpeg(b, out width, out height);
            if (b.Length >= 26 && b[0] == (byte)'8' && b[1] == (byte)'B' && b[2] == (byte)'P' && b[3] == (byte)'S')
            {
                height = BigEndian(b, 14);
                width = BigEndian(b, 18);
                return width > 0 && height > 0;
            }
            if (b.Length >= 26 && b[0] == (byte)'B' && b[1] == (byte)'M')
            {
                width = Math.Abs(BitConverter.ToInt32(b, 18));
                height = Math.Abs(BitConverter.ToInt32(b, 22));
                return width > 0 && height > 0;
            }
            return false;
        }

        /// <summary>TGA は先頭に印が無いので、拡張子で決めてから読む。</summary>
        public static bool TryReadTga(byte[] b, out int width, out int height)
        {
            width = height = 0;
            if (b.Length < 18) return false;
            width = b[12] | (b[13] << 8);
            height = b[14] | (b[15] << 8);
            return width > 0 && height > 0;
        }

        private static bool Jpeg(byte[] b, out int width, out int height)
        {
            width = height = 0;
            int i = 2;
            while (i + 9 < b.Length)
            {
                if (b[i] != 0xFF) { i++; continue; }
                byte marker = b[i + 1];
                if (marker == 0xFF) { i++; continue; }
                if (marker == 0xD8 || marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7)) { i += 2; continue; }
                int length = (b[i + 2] << 8) | b[i + 3];
                bool sof = marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC;
                if (sof)
                {
                    height = (b[i + 5] << 8) | b[i + 6];
                    width = (b[i + 7] << 8) | b[i + 8];
                    return width > 0 && height > 0;
                }
                if (marker == 0xD9 || length < 2) return false;
                i += 2 + length;
            }
            return false;
        }

        private static int BigEndian(byte[] b, int offset)
        {
            return (b[offset] << 24) | (b[offset + 1] << 16) | (b[offset + 2] << 8) | b[offset + 3];
        }
    }
}

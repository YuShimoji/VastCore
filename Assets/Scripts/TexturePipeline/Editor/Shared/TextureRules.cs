using System;
using System.Collections.Generic;
using System.IO;

namespace Vastcore.TexturePipeline
{
    // このフォルダ（Shared）のファイルは UnityEngine を使わない。Unity の Editor と tools/TextureAudit（.NET）の両方でコンパイルする。
    // 規則の正本は docs/02_design/texture-rules.json、説明は docs/02_design/TEXTURE_PIPELINE_SPEC.md。
    // フィールド名は JSON のキーと同じ（Unity の JsonUtility は名前の完全一致で読むため camelCase にしている）。

    /// <summary>texture-rules.json の全体。</summary>
    [Serializable]
    public sealed class TextureRules
    {
        public const string DefaultRelativePath = "docs/02_design/texture-rules.json";

        public int version;
        public string spec;
        public string manifestName;
        public string[] extensions;
        public string[] exclude;
        public TextureLicense[] licenses;
        public TextureKindRule[] kinds;
        public TextureZone[] zones;

        /// <summary>画像として扱う拡張子か（点を含まない小文字で比べる）。</summary>
        public bool IsTextureFile(string path)
        {
            string extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
            return extensions != null && Array.IndexOf(extensions, extension) >= 0;
        }

        /// <summary>第三者の資産など、検査も既定の取り込みもしない場所か。</summary>
        public bool IsExcluded(string relativePath)
        {
            string path = Normalize(relativePath);
            if (exclude == null) return false;
            foreach (string prefix in exclude)
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>最も長く一致する場所の規則。無ければ null。</summary>
        public TextureZone ZoneFor(string relativePath)
        {
            string path = Normalize(relativePath);
            TextureZone best = null;
            if (zones == null) return null;
            foreach (TextureZone zone in zones)
            {
                if (string.IsNullOrEmpty(zone.path) || !path.StartsWith(zone.path, StringComparison.OrdinalIgnoreCase)) continue;
                if (best == null || zone.path.Length > best.path.Length) best = zone;
            }
            return best;
        }

        /// <summary>ファイル名から画像の種類を決める（kinds の並びの順に最初に当たった物）。当たらなければ null。</summary>
        public TextureKindRule Classify(string fileName)
        {
            string name = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
            string[] tokens = name.Split('_', '-', ' ', '.');
            if (kinds == null) return null;
            foreach (TextureKindRule kind in kinds)
            {
                if (kind.contains != null)
                    foreach (string part in kind.contains)
                        if (name.Contains(part)) return kind;
                if (kind.tokens != null)
                    foreach (string token in tokens)
                        if (Array.IndexOf(kind.tokens, token) >= 0) return kind;
            }
            return null;
        }

        public TextureLicense License(string id)
        {
            if (licenses == null || id == null) return null;
            foreach (TextureLicense license in licenses)
                if (string.Equals(license.id, id, StringComparison.Ordinal)) return license;
            return null;
        }

        public static string Normalize(string path)
        {
            string p = (path ?? string.Empty).Replace('\\', '/');
            while (p.StartsWith("./", StringComparison.Ordinal)) p = p.Substring(2);
            return p;
        }

        /// <summary>
        /// 規則の表として成り立っているかを確かめ、問題を返す（空なら良い）。読み込んだ直後に呼ぶ。
        /// </summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            if (version != 1) problems.Add("version は 1 だけを読める: " + version);
            if (string.IsNullOrEmpty(manifestName)) problems.Add("manifestName が空");
            if (extensions == null || extensions.Length == 0) problems.Add("extensions が空");
            if (kinds == null || kinds.Length == 0) problems.Add("kinds が空");
            if (zones == null || zones.Length == 0) problems.Add("zones が空");
            if (zones != null)
            {
                bool hasRoot = false;
                foreach (TextureZone zone in zones)
                {
                    if (zone.path == "Assets/") hasRoot = true;
                    if (string.IsNullOrEmpty(zone.path) || !zone.path.EndsWith("/", StringComparison.Ordinal))
                        problems.Add("zone " + zone.name + " の path は / で終える: " + zone.path);
                    if (zone.status != TextureZone.Active && zone.status != TextureZone.Legacy && zone.status != TextureZone.Ignored)
                        problems.Add("zone " + zone.name + " の status が不明: " + zone.status);
                }
                if (!hasRoot) problems.Add("Assets/ の zone（残りの全部）が無い");
            }
            return problems;
        }
    }

    [Serializable]
    public sealed class TextureLicense
    {
        public string id;
        /// <summary>帰属表示が要る（manifest に author が要る）。</summary>
        public bool needsAuthor;
    }

    /// <summary>画像の種類（色・法線・高さ…）と、その種類に正しい取り込み設定。</summary>
    [Serializable]
    public sealed class TextureKindRule
    {
        public const string NormalDX = "normaldx", Normal = "normal", Color = "color";

        public string name;
        /// <summary>ファイル名を _ - 空白 . で区切った語のどれかが一致すれば当たり。</summary>
        public string[] tokens;
        /// <summary>ファイル名（小文字）がこの文字列を含めば当たり（区切りをまたぐ名前用）。</summary>
        public string[] contains;
        /// <summary>色の値として扱う（sRGB）。false はデータ（Linear）。</summary>
        public bool srgb;
        /// <summary>Unity の Normal map 型で取り込む。</summary>
        public bool normalMap;
        /// <summary>"hq"（BC7・BC5）か "normal"。</summary>
        public string compression;
    }

    /// <summary>場所（Assets からの接頭辞）ごとの規則。</summary>
    [Serializable]
    public sealed class TextureZone
    {
        public const string Active = "active", Legacy = "legacy", Ignored = "ignored";

        public string name;
        public string path;
        /// <summary>active: 検査する。legacy: 保留中の経路。指摘は info に落とす。ignored: Unity の焼き込みなど。.meta だけ見る。</summary>
        public string status;
        /// <summary>取り込み設定を決めるコード（例 RegionSurfaceImport）。空でなければ取り込み設定の検査と既定の適用をしない。</summary>
        public string owner;
        public bool requireManifest;
        /// <summary>初めて取り込む時に既定の設定を入れる（Unity の TextureImportDefaults）。</summary>
        public bool applyImportDefaults;
        public bool requireMips;
        public bool requireCompression;
        /// <summary>2 の冪でない寸法を許す。</summary>
        public bool allowNpot;
        /// <summary>取り込み後の最大の辺（Standalone）。0 で検査しない。</summary>
        public int maxImport;
        /// <summary>git に置く元の画像の最大の辺。これより大きい元は git の外に置き、使う解像度へ縮めた物を置く。0 で検査しない。</summary>
        public int maxSourcePixels;
        /// <summary>manifest に実寸（tileMetres）がある時の、取り込み後の最低の画素密度（px/m）。0 で検査しない。</summary>
        public float minPixelsPerMetre;
        /// <summary>置ける拡張子。空なら extensions の全部。</summary>
        public string[] formats;
        /// <summary>直下のフォルダ名として許す役（空なら検査しない）。</summary>
        public string[] roles;
        /// <summary>owner が読む画像の種類（kinds の name）。空なら全部。これ以外の名前の画像は取り込みで使われない。</summary>
        public string[] useKinds;
        /// <summary>owner が作る画像の名前（拡張子なし）。</summary>
        public string[] generatedNames;
        /// <summary>owner が元の画像を取り込む時の最大の辺。</summary>
        public int sourceMaxImport;
        /// <summary>owner が作る画像の最大の辺。</summary>
        public int generatedMaxImport;

        public bool IsOwned => !string.IsNullOrEmpty(owner);

        public bool IsGenerated(string fileName)
        {
            if (generatedNames == null) return false;
            string name = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
            return Array.IndexOf(generatedNames, name) >= 0;
        }

        public bool AllowsFormat(string fileName)
        {
            if (formats == null || formats.Length == 0) return true;
            string extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
            return Array.IndexOf(formats, extension) >= 0;
        }
    }

    /// <summary>素材の出所の記録（フォルダごとの texture-source.json）。</summary>
    [Serializable]
    public sealed class TextureManifest
    {
        public TextureSourceSet[] sets;

        /// <summary>このファイル名を覆う最初の組。無ければ null。</summary>
        public TextureSourceSet SetFor(string fileName)
        {
            if (sets == null) return null;
            foreach (TextureSourceSet set in sets)
            {
                string[] patterns = set.files == null || set.files.Length == 0 ? new[] { "*" } : set.files;
                foreach (string pattern in patterns)
                    if (Glob(pattern, fileName)) return set;
            }
            return null;
        }

        /// <summary>* と ? だけの名前の照合（大文字小文字を区別しない）。</summary>
        public static bool Glob(string pattern, string name)
        {
            return GlobAt(pattern.ToLowerInvariant(), 0, name.ToLowerInvariant(), 0);
        }

        private static bool GlobAt(string p, int pi, string s, int si)
        {
            while (pi < p.Length)
            {
                char c = p[pi];
                if (c == '*')
                {
                    for (int k = si; k <= s.Length; k++)
                        if (GlobAt(p, pi + 1, s, k)) return true;
                    return false;
                }
                if (si >= s.Length || (c != '?' && c != s[si])) return false;
                pi++;
                si++;
            }
            return si == s.Length;
        }
    }

    [Serializable]
    public sealed class TextureSourceSet
    {
        public const string Downloaded = "downloaded", Generated = "generated", Original = "original";

        /// <summary>覆うファイル名（* と ?）。空なら同じフォルダの全部。</summary>
        public string[] files;
        /// <summary>downloaded（外部の素材）・generated（このリポジトリの道具で作った）・original（手で描いた・撮った）。</summary>
        public string origin;
        /// <summary>downloaded の時の取得元 URL。</summary>
        public string source;
        /// <summary>generated の時の、作った道具のリポジトリ内のパス。</summary>
        public string generator;
        public string license;
        public string author;
        /// <summary>取得日（YYYY-MM-DD）。</summary>
        public string retrieved;
        /// <summary>画像の横 1 枚が覆う実寸（m）。繰り返す素材だけ。0 で不明。</summary>
        public float tileMetres;
        public string notes;
    }
}

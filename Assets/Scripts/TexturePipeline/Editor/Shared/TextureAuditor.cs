using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Vastcore.TexturePipeline
{
    public static class TextureSeverity
    {
        public const string Error = "error", Warning = "warning", Info = "info";
    }

    /// <summary>1 件の指摘。code の意味は TEXTURE_PIPELINE_SPEC.md 4節。</summary>
    [Serializable]
    public sealed class TextureFinding
    {
        public string code;
        public string severity;
        public string path;
        public string message;
    }

    /// <summary>1 枚の画像の読み取った値（報告の表）。</summary>
    [Serializable]
    public sealed class TextureRecord
    {
        public string path;
        public string zone;
        public string kind;
        public int width;
        public int height;
        public int importMax;
        public int srgb;
        public int textureType;
        public long estimatedBytes;
        public bool inResources;
    }

    [Serializable]
    public sealed class TextureAuditResult
    {
        public string rulesPath;
        public int scanned;
        public int errors;
        public int warnings;
        public int infos;
        public long estimatedBytes;
        public long estimatedResourcesBytes;
        public List<TextureFinding> findings = new List<TextureFinding>();
        public List<TextureRecord> records = new List<TextureRecord>();

        public void Add(string code, string severity, string path, string message)
        {
            findings.Add(new TextureFinding { code = code, severity = severity, path = path, message = message });
        }

        /// <summary>件数を数え直す（指摘を足した後に呼ぶ）。</summary>
        public void Count()
        {
            errors = warnings = infos = 0;
            foreach (TextureFinding finding in findings)
            {
                if (finding.severity == TextureSeverity.Error) errors++;
                else if (finding.severity == TextureSeverity.Warning) warnings++;
                else infos++;
            }
        }

        public string Summary()
        {
            Count();
            return "texture audit: " + scanned + " 枚、error " + errors + "・warning " + warnings + "・info " + infos
                   + "、GPU メモリの見積り " + Mb(estimatedBytes) + "（うち Resources " + Mb(estimatedResourcesBytes) + "）";
        }

        /// <summary>人が読む報告（Markdown）。指摘は重さ・code・パスの順。</summary>
        public string ToMarkdown()
        {
            Count();
            var text = new StringBuilder();
            text.AppendLine("# Texture audit");
            text.AppendLine();
            text.AppendLine(Summary());
            text.AppendLine();
            text.AppendLine("規則: `" + rulesPath + "`。code の意味は `docs/02_design/TEXTURE_PIPELINE_SPEC.md` 4節。");
            text.AppendLine();
            var sorted = new List<TextureFinding>(findings);
            sorted.Sort((a, b) =>
            {
                int c = Rank(a.severity).CompareTo(Rank(b.severity));
                if (c != 0) return c;
                c = string.CompareOrdinal(a.code, b.code);
                return c != 0 ? c : string.CompareOrdinal(a.path, b.path);
            });
            text.AppendLine("| 重さ | code | ファイル | 内容 |");
            text.AppendLine("|---|---|---|---|");
            foreach (TextureFinding f in sorted)
                text.AppendLine("| " + f.severity + " | " + f.code + " | `" + f.path + "` | " + f.message.Replace("|", "\\|") + " |");
            text.AppendLine();
            text.AppendLine("| ファイル | 場所 | 種類 | 寸法 | 取り込みの最大 | sRGB | 型 | 見積り |");
            text.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (TextureRecord r in records)
                text.AppendLine("| `" + r.path + "` | " + r.zone + " | " + (r.kind ?? "-") + " | " + r.width + "×" + r.height + " | " + r.importMax
                                + " | " + r.srgb + " | " + r.textureType + " | " + Mb(r.estimatedBytes) + " |");
            return text.ToString();
        }

        private static int Rank(string severity)
        {
            return severity == TextureSeverity.Error ? 0 : severity == TextureSeverity.Warning ? 1 : 2;
        }

        public static string Mb(long bytes)
        {
            return (bytes / (1024.0 * 1024.0)).ToString("0.0", CultureInfo.InvariantCulture) + " MB";
        }
    }

    /// <summary>
    /// 画像の静的な検査（.meta・画像の頭・出所の記録・規則の表）。Unity を起動しない。
    /// 画素を読む検査は TextureQuality（Unity の Editor から）。
    /// </summary>
    public static class TextureAuditor
    {
        /// <param name="root">プロジェクトの根（Assets の親）。</param>
        /// <param name="parseManifest">texture-source.json の文を読む関数（Unity は JsonUtility、.NET は System.Text.Json）。失敗は例外。</param>
        public static TextureAuditResult Run(string root, TextureRules rules, Func<string, TextureManifest> parseManifest, string rulesPath = TextureRules.DefaultRelativePath)
        {
            var result = new TextureAuditResult { rulesPath = rulesPath };
            foreach (string problem in rules.Validate()) result.Add("TX000", TextureSeverity.Error, rulesPath, problem);
            string assets = Path.Combine(root, "Assets");
            if (!Directory.Exists(assets))
            {
                result.Add("TX000", TextureSeverity.Error, "Assets/", "Assets フォルダが無い: " + root);
                result.Count();
                return result;
            }

            var files = new List<string>();
            foreach (string file in Directory.EnumerateFiles(assets, "*", SearchOption.AllDirectories))
            {
                string rel = Relative(root, file);
                if (rules.IsExcluded(rel) || !rules.IsTextureFile(rel)) continue;
                files.Add(rel);
            }
            files.Sort(StringComparer.Ordinal);

            var manifests = new Dictionary<string, ManifestEntry>(StringComparer.Ordinal);
            var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
            var roleKinds = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

            foreach (string rel in files)
            {
                TextureZone zone = rules.ZoneFor(rel);
                if (zone == null) continue;
                result.scanned++;
                string full = Path.Combine(root, rel);
                string fileName = Path.GetFileName(rel);
                bool legacy = zone.status == TextureZone.Legacy;
                Action<string, string, string> add = (code, severity, message) =>
                    result.Add(code, legacy && severity != TextureSeverity.Info ? TextureSeverity.Info : severity, rel, (legacy ? "[legacy] " : "") + message);

                string metaPath = full + ".meta";
                if (!File.Exists(metaPath))
                {
                    add("TX001", TextureSeverity.Error, ".meta が無い。Unity で取り込んでから commit する（移動・削除は .meta と一緒に）");
                    continue;
                }
                if (zone.status == TextureZone.Ignored) continue;

                TextureImportFacts facts = TextureImportFacts.Parse(File.ReadAllText(metaPath));
                int width, height;
                bool sized = Path.GetExtension(rel).ToLowerInvariant() == ".tga"
                    ? ImageHeader.TryReadTga(ReadHead(full, 18), out width, out height)
                    : ImageHeader.TryRead(full, out width, out height);

                bool generated = zone.IsOwned && zone.IsGenerated(fileName);
                TextureKindRule kind = rules.Classify(fileName);
                string hash = Hash(full);
                if (hashes.TryGetValue(hash, out string first))
                    add("TX012", TextureSeverity.Warning, "中身が " + first + " と同じ。片方を消すか、参照を 1 つにまとめる");
                else hashes[hash] = rel;

                // 出所の記録
                TextureSourceSet set = null;
                if (!generated)
                {
                    ManifestEntry manifest = FindManifest(root, rel, rules.manifestName, manifests, parseManifest, result);
                    set = manifest?.Manifest?.SetFor(fileName);
                    if (set == null)
                    {
                        if (zone.requireManifest)
                            add("TX002", TextureSeverity.Error, "出所の記録が無い。同じフォルダか上のフォルダの " + rules.manifestName + " に、このファイルを覆う組を書く");
                    }
                    else CheckSet(root, rules, set, manifest.Path, add);
                }

                if (!zone.AllowsFormat(fileName))
                    add("TX015", TextureSeverity.Error, "この場所で使えない形式。使える形式: " + string.Join(", ", zone.formats));

                int longest = Math.Max(width, height);
                if (sized && zone.maxSourcePixels > 0 && longest > zone.maxSourcePixels && !generated)
                    add("TX009", TextureSeverity.Warning, "元の画像 " + width + "×" + height + " が git に置く上限 " + zone.maxSourcePixels
                        + " を超える。使う解像度へ縮めて置き、大きい元は git の外に残す");

                int importMax;
                if (zone.IsOwned)
                {
                    importMax = generated ? zone.generatedMaxImport : zone.sourceMaxImport;
                    if (!generated) CheckOwnedSource(rules, zone, rel, fileName, kind, width, height, sized, roleKinds, add);
                    if (generated && zone.generatedMaxImport > 0 && facts.EffectiveMaxSize > zone.generatedMaxImport)
                        add("TX008", TextureSeverity.Error, "作った画像の取り込みの最大 " + facts.EffectiveMaxSize + " が " + zone.generatedMaxImport + " を超える");
                }
                else
                {
                    importMax = facts.EffectiveMaxSize;
                    CheckImport(zone, kind, facts, width, height, sized, add);
                }

                if (set != null && set.tileMetres > 0 && zone.minPixelsPerMetre > 0 && sized)
                {
                    int used = zone.IsOwned ? Math.Min(width, zone.generatedMaxImport > 0 ? zone.generatedMaxImport : width) : Math.Min(width, importMax > 0 ? importMax : width);
                    double density = used / set.tileMetres;
                    if (density < zone.minPixelsPerMetre)
                        add("TX017", TextureSeverity.Warning, "画素密度 " + density.ToString("0", CultureInfo.InvariantCulture) + " px/m が下限 "
                            + zone.minPixelsPerMetre.ToString("0", CultureInfo.InvariantCulture) + " px/m より粗い（横 " + used + " px ÷ "
                            + set.tileMetres.ToString("0.##", CultureInfo.InvariantCulture) + " m）");
                }

                bool inResources = rel.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) >= 0;
                long bytes = sized ? Estimate(width, height, importMax, facts) : 0;
                result.estimatedBytes += bytes;
                if (inResources) result.estimatedResourcesBytes += bytes;
                result.records.Add(new TextureRecord
                {
                    path = rel, zone = zone.name, kind = generated ? "generated" : kind?.name, width = width, height = height,
                    importMax = importMax, srgb = facts.SRgb, textureType = facts.EffectiveType, estimatedBytes = bytes, inResources = inResources,
                });
            }

            foreach (KeyValuePair<string, Dictionary<string, string>> role in roleKinds)
                if (role.Value.Count > 0 && !role.Value.ContainsKey(TextureKindRule.Color))
                    result.Add("TX014", TextureSeverity.Error, role.Key + "/", "色の画像が無いので、この役の素材は使われない（取り込みは色の画像を必須にしている）");

            result.Count();
            return result;
        }

        private static void CheckImport(TextureZone zone, TextureKindRule kind, TextureImportFacts facts, int width, int height, bool sized,
                                        Action<string, string, string> add)
        {
            if (!facts.IsTextureImporter)
            {
                add("TX020", TextureSeverity.Warning, "TextureImporter 以外で取り込まれている（" + facts.Importer + "）。設定を検査できない");
                return;
            }
            int type = facts.EffectiveType;
            bool ui = type == TextureImportFacts.TypeSprite || type == TextureImportFacts.TypeGui;
            if (kind != null && facts.SRgb >= 0 && (facts.SRgb == 1) != kind.srgb)
                add("TX004", TextureSeverity.Error, kind.name + " の画像は sRGB を " + (kind.srgb ? "入" : "切") + "にする（今は " + (facts.SRgb == 1 ? "入" : "切")
                    + "）。" + (kind.srgb ? "色の値" : "データ（法線・粗さ・高さなど）") + "として読まれる");
            if (kind != null && kind.normalMap && type != TextureImportFacts.TypeNormalMap)
                add("TX005", TextureSeverity.Error, "法線の画像は Texture Type を Normal map にする（今は " + type + "）");
            if ((kind == null || !kind.normalMap) && type == TextureImportFacts.TypeNormalMap)
                add("TX005", TextureSeverity.Error, "名前が法線でない画像が Normal map 型。名前を直すか型を戻す");
            if (kind != null && kind.name == TextureKindRule.NormalDX && facts.FlipGreen != 1)
                add("TX006", TextureSeverity.Error, "DirectX 向きの法線（緑が下）。Unity は OpenGL 向きなので、GL 版を置くか Flip Green Channel を入にする");
            if (sized && !zone.allowNpot && !ui && (!PowerOfTwo(width) || !PowerOfTwo(height)))
                add("TX007", TextureSeverity.Warning, "寸法 " + width + "×" + height + " が 2 の冪でない。mip と圧縮が崩れるので 2 の冪に直す");
            if (zone.maxImport > 0 && facts.EffectiveMaxSize > zone.maxImport)
                add("TX008", TextureSeverity.Error, "取り込みの最大 " + facts.EffectiveMaxSize + " がこの場所の上限 " + zone.maxImport + " を超える");
            if (zone.requireCompression && facts.IsUncompressed)
                add("TX010", TextureSeverity.Warning, "圧縮しない設定。理由が無ければ Compressed にする");
            if (zone.requireMips && !ui && facts.Mipmaps == 0)
                add("TX011", TextureSeverity.Warning, "mipmap が切。3D の面に貼る画像は遠くでちらつく");
            if (facts.IsReadable == 1)
                add("TX016", TextureSeverity.Warning, "Read/Write が入。CPU 側にも複製が残りメモリが倍になる。スクリプトで画素を読む時だけ入にする");
        }

        private static void CheckOwnedSource(TextureRules rules, TextureZone zone, string rel, string fileName, TextureKindRule kind,
                                             int width, int height, bool sized, Dictionary<string, Dictionary<string, string>> roleKinds,
                                             Action<string, string, string> add)
        {
            string inside = rel.Substring(zone.path.Length);
            int slash = inside.IndexOf('/');
            string role = slash < 0 ? null : inside.Substring(0, slash);
            if (role == null || (zone.roles != null && zone.roles.Length > 0 && Array.IndexOf(zone.roles, role) < 0))
            {
                add("TX014", TextureSeverity.Error, "役のフォルダ（" + string.Join("・", zone.roles ?? new string[0]) + "）の直下に置く。ここでは使われない");
                return;
            }
            bool used = kind != null && (zone.useKinds == null || zone.useKinds.Length == 0 || Array.IndexOf(zone.useKinds, kind.name) >= 0);
            if (!used)
            {
                add("TX013", TextureSeverity.Warning, "名前から種類が分からない（または " + zone.owner + " が読まない種類）ので、取り込みで使われない。"
                    + "名前の末尾を _Color・_NormalGL・_Displacement・_Roughness・_AmbientOcclusion などにする");
                return;
            }
            string roleKey = zone.path + role;
            if (!roleKinds.TryGetValue(roleKey, out Dictionary<string, string> kinds))
                roleKinds[roleKey] = kinds = new Dictionary<string, string>(StringComparer.Ordinal);
            if (kinds.TryGetValue(kind.name, out string other))
                add("TX018", TextureSeverity.Warning, kind.name + " が 2 つある。取り込みは名前の順で先の " + other + " を使う");
            else kinds[kind.name] = fileName;
            if (sized && width != height)
            {
                int big = Math.Max(width, height), small = Math.Min(width, height);
                if (big % small != 0)
                    add("TX019", TextureSeverity.Warning, "縦横の比 " + width + "×" + height + " が整数倍でないので、正方形へ引き伸ばされる");
            }
        }

        private static void CheckSet(string root, TextureRules rules, TextureSourceSet set, string manifestPath, Action<string, string, string> add)
        {
            string at = "（" + manifestPath + "）";
            TextureLicense license = rules.License(set.license);
            if (license == null)
                add("TX003", TextureSeverity.Error, "ライセンス " + (set.license ?? "（空）") + " は使えない。使える物: " + LicenseList(rules) + at);
            else if (license.needsAuthor && string.IsNullOrEmpty(set.author))
                add("TX003", TextureSeverity.Error, set.license + " は帰属表示が要るので author を書く" + at);
            switch (set.origin)
            {
                case TextureSourceSet.Downloaded:
                    if (string.IsNullOrEmpty(set.source)) add("TX003", TextureSeverity.Error, "downloaded は取得元の URL（source）を書く" + at);
                    break;
                case TextureSourceSet.Generated:
                    if (string.IsNullOrEmpty(set.generator)) add("TX003", TextureSeverity.Error, "generated は作った道具のパス（generator）を書く" + at);
                    else if (!File.Exists(Path.Combine(root, set.generator)))
                        add("TX003", TextureSeverity.Warning, "generator のファイルが無い: " + set.generator + at);
                    break;
                case TextureSourceSet.Original:
                    break;
                default:
                    add("TX003", TextureSeverity.Error, "origin は downloaded・generated・original のどれか（今は " + (set.origin ?? "空") + "）" + at);
                    break;
            }
        }

        private static string LicenseList(TextureRules rules)
        {
            var ids = new List<string>();
            if (rules.licenses != null) foreach (TextureLicense l in rules.licenses) ids.Add(l.id);
            return string.Join(", ", ids);
        }

        private sealed class ManifestEntry
        {
            public string Path;
            public TextureManifest Manifest;
        }

        // ファイルのフォルダから Assets まで上へ、最初に見つかった記録を使う
        private static ManifestEntry FindManifest(string root, string rel, string manifestName, Dictionary<string, ManifestEntry> cache,
                                                  Func<string, TextureManifest> parse, TextureAuditResult result)
        {
            string dir = rel.Substring(0, rel.LastIndexOf('/'));
            var visited = new List<string>();
            ManifestEntry found = null;
            while (true)
            {
                if (cache.TryGetValue(dir, out ManifestEntry cached)) { found = cached; break; }
                visited.Add(dir);
                string candidate = dir + "/" + manifestName;
                string full = Path.Combine(root, candidate);
                if (File.Exists(full))
                {
                    found = new ManifestEntry { Path = candidate };
                    try
                    {
                        found.Manifest = parse(File.ReadAllText(full));
                    }
                    catch (Exception e)
                    {
                        result.Add("TX003", TextureSeverity.Error, candidate, "出所の記録を JSON として読めない: " + e.Message);
                    }
                    break;
                }
                int slash = dir.LastIndexOf('/');
                if (dir == "Assets" || slash < 0) break;
                dir = dir.Substring(0, slash);
            }
            foreach (string v in visited) cache[v] = found;
            return found;
        }

        // 取り込んだ後の GPU 上の大きさの見積り（圧縮 8 bit/画素、非圧縮 32 bit/画素、mip で 4/3 倍）
        private static long Estimate(int width, int height, int importMax, TextureImportFacts facts)
        {
            double w = width, h = height;
            if (importMax > 0 && Math.Max(w, h) > importMax)
            {
                double scale = importMax / Math.Max(w, h);
                w *= scale;
                h *= scale;
            }
            double bitsPerPixel = facts.IsUncompressed ? 32 : 8;
            double bytes = w * h * bitsPerPixel / 8;
            if (facts.Mipmaps != 0) bytes *= 4.0 / 3.0;
            return (long)bytes;
        }

        public static bool PowerOfTwo(int v)
        {
            return v > 0 && (v & (v - 1)) == 0;
        }

        private static byte[] ReadHead(string path, int count)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                var buffer = new byte[Math.Min(count, (int)stream.Length)];
                int read = 0;
                while (read < buffer.Length)
                {
                    int n = stream.Read(buffer, read, buffer.Length - read);
                    if (n <= 0) break;
                    read += n;
                }
                return buffer;
            }
        }

        private static string Hash(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        public static string Relative(string root, string full)
        {
            string r = Path.GetFullPath(root).TrimEnd('\\', '/');
            string f = Path.GetFullPath(full);
            return TextureRules.Normalize(f.Substring(r.Length + 1));
        }
    }
}

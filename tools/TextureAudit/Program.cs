using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Vastcore.TexturePipeline;

namespace Vastcore.TextureAudit
{
    /// <summary>
    /// 使い方: dotnet run --project tools/TextureAudit -- [--root .] [--rules docs/02_design/texture-rules.json] [--out artifacts/texture-audit] [--strict] [--self-test]
    /// 終了コード: 0 = error なし（--strict では warning もなし）、1 = 指摘あり、2 = 実行できない。
    /// </summary>
    public static class Program
    {
        public static readonly JsonSerializerOptions Json = new JsonSerializerOptions
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = false,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public static int Main(string[] args)
        {
            Console.OutputEncoding = new UTF8Encoding(false);
            string root = ".", rules = null, output = null;
            bool strict = false, selfTest = false;
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--root": root = args[++i]; break;
                    case "--rules": rules = args[++i]; break;
                    case "--out": output = args[++i]; break;
                    case "--strict": strict = true; break;
                    case "--self-test": selfTest = true; break;
                    default:
                        Console.Error.WriteLine("不明な引数: " + args[i]);
                        return 2;
                }
            }
            try
            {
                if (selfTest) return SelfTest.Run();
                root = Path.GetFullPath(root);
                string rulesPath = rules ?? Path.Combine(root, TextureRules.DefaultRelativePath);
                TextureRules table = LoadRules(rulesPath);
                TextureAuditResult result = TextureAuditor.Run(root, table, ParseManifest, TextureRules.Normalize(Path.GetRelativePath(root, rulesPath)));
                if (output != null) Write(result, Path.GetFullPath(Path.Combine(root, output)));
                Print(result);
                return result.errors > 0 || (strict && result.warnings > 0) ? 1 : 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("texture audit を実行できない: " + e.Message);
                return 2;
            }
        }

        public static TextureRules LoadRules(string path)
        {
            return JsonSerializer.Deserialize<TextureRules>(File.ReadAllText(path), Json) ?? throw new InvalidDataException("規則が空: " + path);
        }

        public static TextureManifest ParseManifest(string text)
        {
            return JsonSerializer.Deserialize<TextureManifest>(text, Json) ?? throw new InvalidDataException("空の記録");
        }

        private static void Write(TextureAuditResult result, string directory)
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "texture-audit.json"), JsonSerializer.Serialize(result, Json), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(directory, "texture-audit.md"), result.ToMarkdown(), new UTF8Encoding(false));
            Console.WriteLine("報告: " + Path.Combine(directory, "texture-audit.md"));
        }

        private static void Print(TextureAuditResult result)
        {
            foreach (TextureFinding f in result.findings)
                if (f.severity != TextureSeverity.Info)
                    Console.WriteLine(f.severity.ToUpperInvariant() + " " + f.code + " " + f.path + ": " + f.message);
            Console.WriteLine(result.Summary());
        }
    }
}

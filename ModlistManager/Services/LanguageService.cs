using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ETS2ATS.ModlistManager.Services
{
    public class LanguageService
    {
        private Dictionary<string, string> _map = new();
        public string CurrentCode { get; private set; } = "de";

        public string this[string key]
            => string.IsNullOrWhiteSpace(key) ? "" :
               (_map.TryGetValue(key, out var v) ? v : key);

        public void Load(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) code = "de";
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            var enFile = FindLangFile(baseDir, "en");
            var targetFile = FindLangFile(baseDir, code);

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (enFile != null)
                {
                    var jsonEn = File.ReadAllText(enFile);
                    var baseMap = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonEn);
                    if (baseMap != null)
                    {
                        foreach (var kv in baseMap) map[kv.Key] = kv.Value;
                    }
                }

                if (targetFile != null && !code.Equals("en", StringComparison.OrdinalIgnoreCase))
                {
                    var json = File.ReadAllText(targetFile);
                    var overlay = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (overlay != null)
                    {
                        foreach (var kv in overlay) map[kv.Key] = kv.Value;
                    }
                }

                // Wenn weder en noch Ziel geladen werden konnten, Map leer lassen
                _map = map;
                CurrentCode = code;
            }
            catch
            {
                _map = map; // ggf. nur EN-Inhalt oder leer
                CurrentCode = code;
            }
        }

        // Discover available languages (code, friendly name, path)
        public sealed class LanguageInfo
        {
            public string Code { get; init; } = "";
            public string Name { get; init; } = "";
            public string Path { get; init; } = "";
            public override string ToString() => string.IsNullOrWhiteSpace(Name) ? Code : ($"{Name} ({Code})");
        }

        public IEnumerable<LanguageInfo> EnumerateAvailableLanguages()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var dirs = CandidateLanguageDirs(baseDir).Where(Directory.Exists).ToArray();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var results = new List<LanguageInfo>();
            foreach (var dir in dirs)
            {
                IEnumerable<string> files;
                try { files = Directory.EnumerateFiles(dir, "lang.*.json", SearchOption.TopDirectoryOnly); }
                catch { continue; }

                foreach (var file in files)
                {
                    var name = Path.GetFileName(file);
                    if (!name.StartsWith("lang.", StringComparison.OrdinalIgnoreCase) || !name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var code = name.Substring(5, name.Length - 5 - 5); // between 'lang.' and '.json'
                    if (!seen.Add(code)) continue; // keep first occurrence by priority
                    var display = TryReadLanguageDisplay(file) ?? GuessLanguageName(code) ?? code;
                    results.Add(new LanguageInfo { Code = code, Name = display, Path = file });
                }
            }
            foreach (var li in results) yield return li;
        }

        private static string? TryReadLanguageDisplay(string file)
        {
            try
            {
                using var fs = File.OpenRead(file);
                using var doc = JsonDocument.Parse(fs);
                if (doc.RootElement.TryGetProperty("Language.Name", out var val) && val.ValueKind == JsonValueKind.String)
                    return val.GetString();
            }
            catch { }
            return null;
        }

        private static string? GuessLanguageName(string code)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["de"] = "Deutsch",
                ["en"] = "English",
                ["fr"] = "Français",
                ["es"] = "Español",
                ["it"] = "Italiano",
                ["pl"] = "Polski",
                ["pt"] = "Português",
                ["pt-br"] = "Português (Brasil)",
                ["ru"] = "Русский",
                ["tr"] = "Türkçe",
                ["cs"] = "Čeština",
                ["sk"] = "Slovenčina",
                ["nl"] = "Nederlands",
                ["sv"] = "Svenska",
                ["fi"] = "Suomi",
                ["da"] = "Dansk",
                ["no"] = "Norsk",
                ["hu"] = "Magyar",
                ["ro"] = "Română",
                ["uk"] = "Українська",
                ["zh-cn"] = "简体中文",
                ["zh-tw"] = "繁體中文",
                ["ja"] = "日本語",
                ["ko"] = "한국어"
            };
            return map.TryGetValue(code, out var name) ? name : null;
        }

        private static string? FindLangFile(string baseDir, string code)
        {
            foreach (var dir in CandidateLanguageDirs(baseDir))
            {
                try
                {
                    var path = Path.Combine(dir, $"lang.{code}.json");
                    if (File.Exists(path)) return path;
                }
                catch { }
            }
            return null;
        }

        private static IEnumerable<string> CandidateLanguageDirs(string baseDir)
        {
            // Priority: app Resources\Languages (your main complete translations), then top-level Languages, then packaged
            yield return Path.Combine(baseDir, "Resources", "Languages");
            yield return Path.Combine(baseDir, "Languages");
            yield return Path.Combine(baseDir, "ModlistManager", "Resources", "Languages");
        }
    }
}


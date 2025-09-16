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

            string? file = FindLangFile(baseDir, code);
            if (file == null && !code.Equals("en", StringComparison.OrdinalIgnoreCase))
            {
                code = "en";
                file = FindLangFile(baseDir, code);
            }

            if (file == null)
            {
                _map = new();
                CurrentCode = code;
                return;
            }

            try
            {
                var json = File.ReadAllText(file);
                _map = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                CurrentCode = code;
            }
            catch
            {
                _map = new();
                CurrentCode = code;
            }
        }

        private static string? FindLangFile(string baseDir, string code)
        {
            // Single source of truth: ModlistManager/Resources/Languages
            var path = Path.Combine(baseDir, "ModlistManager", "Resources", "Languages", $"lang.{code}.json");
            return File.Exists(path) ? path : null;
        }
    }
}


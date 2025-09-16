using System;
using System.IO;
using System.Text.Json;
using ETS2ATS.ModlistManager.Models;

namespace ETS2ATS.ModlistManager.Services
{
    public class SettingsService
    {
        private readonly string _settingsPath;
        public AppSettings Current { get; private set; }

        public SettingsService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "ETS2ATS.ModlistManager");
            Directory.CreateDirectory(dir);
            _settingsPath = Path.Combine(dir, "settings.json");
            Current = Load();
        }

        private AppSettings Load()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var s = JsonSerializer.Deserialize<AppSettings>(json);
                    if (s != null) return s;
                }
            }
            catch
            {
                // ignore; fallback to defaults
            }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
            }
            catch
            {
                // ignore
            }
        }
    }
}


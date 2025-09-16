using System;
using System.IO;
using System.Windows.Forms;

namespace ETS2ATS.ModlistManager
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            var lang = new Services.LanguageService();
            lang.Load("de"); // Sprache nachträglich laden

            var theme = new Services.ThemeService();
            var settings = new Services.SettingsService();

            Application.Run(new ModlistManager.Forms.Main.MainForm(lang, theme, settings));
        }
    }
}


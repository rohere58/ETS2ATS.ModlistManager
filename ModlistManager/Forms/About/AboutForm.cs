using System.Windows.Forms;

namespace ETS2ATS.ModlistManager.Forms.About
{
    public partial class AboutForm : Form
    {
        private readonly ETS2ATS.ModlistManager.Services.LanguageService _lang;
        private readonly ETS2ATS.ModlistManager.Services.SettingsService _settings;

        public AboutForm(ETS2ATS.ModlistManager.Services.SettingsService settings)
        {
            _settings = settings;
            _lang = new ETS2ATS.ModlistManager.Services.LanguageService();
            InitializeComponent();

            try { _lang.Load(_settings.Current.Language ?? "de"); } catch { }
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            void L(Control c)
            {
                if (c is null) return;
                if (c.Tag is string key && !string.IsNullOrWhiteSpace(key))
                {
                    try { c.Text = _lang[key]; } catch { }
                }
                foreach (Control child in c.Controls)
                    L(child);
            }

            L(this);

            // Versions-Label nachträglich setzen (nicht aus Sprachdatei, sondern Assembly-Version)
            try
            {
                var asm = typeof(AboutForm).Assembly;
                string versionString = "?";
                // Versuch 1: AssemblyInformationalVersion (kann SemVer + Suffix beinhalten)
                try
                {
                    var infoAttr = asm.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
                        .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
                        .FirstOrDefault();
                    if (infoAttr != null && !string.IsNullOrWhiteSpace(infoAttr.InformationalVersion))
                    {
                        versionString = infoAttr.InformationalVersion.Split('+')[0]; // Build-Metadaten abschneiden
                    }
                }
                catch { }
                // Versuch 2: ProductVersion aus FileVersionInfo (kann sehr lang sein) – nur nehmen, wenn noch '?'
                if (versionString == "?")
                {
                    try
                    {
                        // In Single-File Deployments ist asm.Location leer -> Fallback auf AppContext.BaseDirectory
                        string loc = asm.Location;
                        bool singleFile = string.IsNullOrEmpty(loc);
                        if (singleFile)
                        {
                            // künstliche Datei im Basisverzeichnis für FileVersionInfo ermitteln
                            loc = System.IO.Path.Combine(AppContext.BaseDirectory, "ETS2ATS.ModlistManager.exe");
                        }
                        if (System.IO.File.Exists(loc))
                        {
                            var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(loc);
                            var pv = fvi.ProductVersion ?? fvi.FileVersion;
                            if (!string.IsNullOrWhiteSpace(pv)) versionString = pv.Split('+')[0];
                        }
                    }
                    catch { }
                }
                // Versuch 3: AssemblyName.Version -> Major.Minor.Build
                if (versionString == "?")
                {
                    var ver = asm.GetName().Version;
                    if (ver != null) versionString = $"{ver.Major}.{ver.Minor}.{ver.Build}";
                }
                // Falls jetzt immer noch sehr lang (z.B. 0.1.15.0 oder mit PräRelease), auf die ersten drei Komponenten reduzieren
                if (Version.TryParse(versionString, out var parsed))
                {
                    versionString = $"{parsed.Major}.{parsed.Minor}.{parsed.Build}";
                }
                var lbl = FindControlByTag(this, "About.Version.Value");
                if (lbl != null) lbl.Text = versionString;
            }
            catch { }
        }

        private static Control? FindControlByTag(Control root, string tag)
        {
            if (root.Tag is string t && t == tag) return root;
            foreach (Control c in root.Controls)
            {
                var f = FindControlByTag(c, tag);
                if (f != null) return f;
            }
            return null;
        }
    }
}

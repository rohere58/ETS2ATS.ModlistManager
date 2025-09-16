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
        }
    }
}

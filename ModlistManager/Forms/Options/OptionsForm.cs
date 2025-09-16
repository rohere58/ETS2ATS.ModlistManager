using System;
using System.IO;
using System.Windows.Forms;
using ETS2ATS.ModlistManager.Services;

namespace ETS2ATS.ModlistManager.Forms.Options
{
    public partial class OptionsForm : Form
    {
        private readonly SettingsService _settings;
        private readonly LanguageService _langService = new LanguageService();
        private bool _suppressCascade;

        // Live-Preview-Events
        public event Action<string>? LanguageChangedLive;
        public event Action<string>? ThemeChangedLive;
        public event Action<string>? PreferredGameChangedLive;
        public event Action<string, string>? PathsChangedLive;

        public string SelectedLanguageCode { get; private set; } = "de";
        public string SelectedTheme { get; private set; } = "Light";
        public string SelectedPreferredGame { get; private set; } = "ETS2";
        public string? Ets2Path { get; private set; }
        public string? AtsPath { get; private set; }
    public bool ConfirmBeforeAdopt { get; private set; }

        public OptionsForm(SettingsService settings)
        {
            InitializeComponent();
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            InitCombos();

            _suppressCascade = true;
            try
            {
                LoadFromSettings();
                // Sprache initial laden
                _langService.Load(SelectedLanguageCode);
            }
            finally { _suppressCascade = false; }

            ApplyLanguageToControls();
            WireEvents();
        }

        private void InitCombos()
        {
            // Sprache
            cbLanguage.DisplayMember = "Text";
            cbLanguage.ValueMember = "Value";
            cbLanguage.Items.Clear();
            cbLanguage.Items.Add(new { Text = "Deutsch (de)", Value = "de" });
            cbLanguage.Items.Add(new { Text = "English (en)", Value = "en" });

            // Theme
            cbTheme.Items.Clear();
            cbTheme.Items.Add("Light");
            cbTheme.Items.Add("Dark");

            // Preferred Game
            cbPreferredGame.Items.Clear();
            cbPreferredGame.Items.Add("ETS2");
            cbPreferredGame.Items.Add("ATS");
        }

        private void LoadFromSettings()
        {
            // Sprache
            var langCode = _settings.Current.Language ?? "de";
            for (int i = 0; i < cbLanguage.Items.Count; i++)
            {
                dynamic it = cbLanguage.Items[i]!;
                if ((string)it.Value == langCode) { cbLanguage.SelectedIndex = i; break; }
            }
            if (cbLanguage.SelectedIndex < 0) cbLanguage.SelectedIndex = 0;
            SelectedLanguageCode = ExtractSelectedLang();

            // Theme
            var theme = string.IsNullOrWhiteSpace(_settings.Current.Theme) ? "Light" : _settings.Current.Theme;
            cbTheme.SelectedItem = theme == "Dark" ? "Dark" : "Light";
            SelectedTheme = (cbTheme.SelectedItem as string) ?? "Light";

            // Preferred Game
            var pg = string.IsNullOrWhiteSpace(_settings.Current.PreferredGame) ? "ETS2" : _settings.Current.PreferredGame;
            cbPreferredGame.SelectedItem = pg.Equals("ATS", StringComparison.OrdinalIgnoreCase) ? "ATS" : "ETS2";
            SelectedPreferredGame = (cbPreferredGame.SelectedItem as string) ?? "ETS2";

            // Pfade
            txtEts2Path.Text = _settings.Current.Ets2ProfilesPath ?? "";
            txtAtsPath.Text = _settings.Current.AtsProfilesPath ?? "";
            Ets2Path = string.IsNullOrWhiteSpace(txtEts2Path.Text) ? null : txtEts2Path.Text;
            AtsPath = string.IsNullOrWhiteSpace(txtAtsPath.Text) ? null : txtAtsPath.Text;

            // Checkbox
            chkConfirmBeforeAdopt.Checked = _settings.Current.ConfirmBeforeAdopt;
            ConfirmBeforeAdopt = chkConfirmBeforeAdopt.Checked;
        }

        private void WireEvents()
        {
            btnBrowseEts2.Click += (s, e) => BrowseInto(txtEts2Path);
            btnBrowseAts.Click += (s, e) => BrowseInto(txtAtsPath);
            btnOK.Click += btnOK_Click;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            cbLanguage.SelectedIndexChanged += (s, e) =>
            {
                if (_suppressCascade) return;
                SelectedLanguageCode = ExtractSelectedLang();
                _langService.Load(SelectedLanguageCode);
                LanguageChangedLive?.Invoke(SelectedLanguageCode);
                ApplyLanguageToControls();
            };
            cbTheme.SelectedIndexChanged += (s, e) =>
            {
                if (_suppressCascade) return;
                SelectedTheme = (cbTheme.SelectedItem as string) ?? "Light";
                ThemeChangedLive?.Invoke(SelectedTheme);
            };
            cbPreferredGame.SelectedIndexChanged += (s, e) =>
            {
                if (_suppressCascade) return;
                SelectedPreferredGame = (cbPreferredGame.SelectedItem as string) ?? "ETS2";
                PreferredGameChangedLive?.Invoke(SelectedPreferredGame);
            };
            chkConfirmBeforeAdopt.CheckedChanged += (s, e) =>
            {
                if (_suppressCascade) return;
                ConfirmBeforeAdopt = chkConfirmBeforeAdopt.Checked;
            };
            txtEts2Path.TextChanged += (s, e) =>
            {
                if (_suppressCascade) return;
                Ets2Path = string.IsNullOrWhiteSpace(txtEts2Path.Text) ? null : txtEts2Path.Text;
                PathsChangedLive?.Invoke(Ets2Path ?? "", AtsPath ?? "");
            };
            txtAtsPath.TextChanged += (s, e) =>
            {
                if (_suppressCascade) return;
                AtsPath = string.IsNullOrWhiteSpace(txtAtsPath.Text) ? null : txtAtsPath.Text;
                PathsChangedLive?.Invoke(Ets2Path ?? "", AtsPath ?? "");
            };
        }

        private void ApplyLanguageToControls()
        {
            lblLanguage.Text = _langService["Options.Language"];
            lblTheme.Text = _langService["Options.Theme"];
            lblPreferredGame.Text = _langService["Options.PreferredGame"];
            lblEts2Path.Text = _langService["Options.Ets2Path"];
            lblAtsPath.Text = _langService["Options.AtsPath"];
            btnBrowseEts2.Text = _langService["Options.Browse"];
            btnBrowseAts.Text = _langService["Options.Browse"];
            btnOK.Text = _langService["Common.OK"];
            btnCancel.Text = _langService["Common.Cancel"];
            chkConfirmBeforeAdopt.Text = _langService["Options.ConfirmBeforeAdopt"];
        }

        private void BrowseInto(TextBox target)
        {
            using var dlg = new FolderBrowserDialog();
            dlg.Description = "Profil-Ordner wählen";
            dlg.ShowNewFolderButton = false;
            dlg.UseDescriptionForTitle = true;
            if (Directory.Exists(target.Text)) dlg.SelectedPath = target.Text;
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                target.Text = dlg.SelectedPath;
            }
        }

        private void btnOK_Click(object? sender, EventArgs e)
        {
            // Auslesen
            SelectedLanguageCode = ExtractSelectedLang();
            SelectedTheme = (cbTheme.SelectedItem as string) ?? "Light";
            SelectedPreferredGame = (cbPreferredGame.SelectedItem as string) ?? "ETS2";
            Ets2Path = string.IsNullOrWhiteSpace(txtEts2Path.Text) ? null : txtEts2Path.Text;
            AtsPath = string.IsNullOrWhiteSpace(txtAtsPath.Text) ? null : txtAtsPath.Text;
            ConfirmBeforeAdopt = chkConfirmBeforeAdopt.Checked;

            // In Settings schreiben
            _settings.Current.ConfirmBeforeAdopt = ConfirmBeforeAdopt;
            DialogResult = DialogResult.OK;
            Close();
        }

        private string ExtractSelectedLang()
        {
            if (cbLanguage.SelectedIndex >= 0)
            {
                dynamic it = cbLanguage.Items[cbLanguage.SelectedIndex]!;
                return (string)it.Value;
            }
            return "de";
        }
    }
}
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO.Compression;
using Microsoft.Win32;
using ETS2ATS.ModlistManager.Forms.Options; // hinzugefügt

namespace ETS2ATS.ModlistManager.Forms.Main
{
    public partial class MainForm : Form
    {
        private static bool IsDesignTime => LicenseManager.UsageMode == LicenseUsageMode.Designtime;

        private readonly ETS2ATS.ModlistManager.Services.SettingsService _settings;
        private readonly ETS2ATS.ModlistManager.Services.LanguageService _lang;
        private readonly ETS2ATS.ModlistManager.Services.ThemeService _theme;

        private bool _suppressCascade;
    // Pfad der aktuell geladenen Modliste (für korrekte Persistenz der Info-JSON)
    private string? _currentModlistPath;
    // Modlisten-Notiz (.note) Handling
    private bool _loadingModInfo;
    private string _lastNoteText = string.Empty;
    private System.Windows.Forms.Timer? _noteSaveTimer;
    // Undo-Link Animation
    private System.Windows.Forms.Timer? _undoAnimTimer;
    private int _undoAnimStep = 0;           // aktueller Schritt
    private int _undoAnimDir = 0;            // +1 = einblenden, -1 = ausblenden, 0 = kein Lauf
    private const int UNDO_ANIM_STEPS = 12;  // Anzahl Steps für die Animation
    private Color _undoStartColor = Color.Gray;
    private Color _undoEndColor = Color.RoyalBlue; // wird je nach Theme gesetzt

        private sealed class GameItem
        {
            public string Code { get; }
            public string Display { get; }
            public GameItem(string code, string display) { Code = code; Display = display; }
            public override string ToString() => Display;
        }

        private sealed record class UiSnapshot(
            string Language,
            string Theme,
            string PreferredGame,
            string? Ets2Path,
            string? AtsPath
        );

        private sealed class ProfileItem
        {
            public string Display { get; }
            public string Directory { get; }
            public bool IsSteam { get; }
            public ProfileItem(string display, string directory, bool isSteam)
            {
                Display = display; Directory = directory; IsSteam = isSteam;
            }
            public override string ToString() => Display;
        }

        // Parameterloser Konstruktor nur für den Designer
        public MainForm() : this(
            IsDesignTime ? new ETS2ATS.ModlistManager.Services.LanguageService() : throw new InvalidOperationException("Design-time ctor only"),
            IsDesignTime ? new ETS2ATS.ModlistManager.Services.ThemeService()    : throw new InvalidOperationException("Design-time ctor only"),
            IsDesignTime ? new ETS2ATS.ModlistManager.Services.SettingsService() : throw new InvalidOperationException("Design-time ctor only"))
        {
        }

        public MainForm(ETS2ATS.ModlistManager.Services.LanguageService lang,
                        ETS2ATS.ModlistManager.Services.ThemeService theme,
                        ETS2ATS.ModlistManager.Services.SettingsService settings)
        {
            _lang = lang; _theme = theme; _settings = settings;
            InitializeComponent();

            this.menuMain.Renderer = new SimpleRenderer();

            if (!IsDesignTime)
            {
                EnsureLanguageTags();

                // Sprache laden + anwenden
                try { _lang.Load(_settings.Current.Language ?? "de"); } catch { }
                ApplyLanguage();

                // Theme anwenden (und Grid-Buttons passend stylen)
                try { _theme.Apply(this, _settings.Current.Theme ?? "Light"); } catch { }
                try { _theme.ApplyToDataGridView(this.gridMods, string.Equals(_settings.Current.Theme, "Dark", StringComparison.OrdinalIgnoreCase)); } catch { }
                try { this.headerBanner.ApplyTheme(string.Equals(_settings.Current.Theme, "Dark", StringComparison.OrdinalIgnoreCase)); } catch { }

                // Undo-Animation vorbereiten (Endfarbe je nach Theme)
                var isDarkTheme = string.Equals(_settings.Current.Theme, "Dark", StringComparison.OrdinalIgnoreCase);
                _undoEndColor = isDarkTheme ? Color.SkyBlue : Color.RoyalBlue;
                _undoAnimTimer = new System.Windows.Forms.Timer { Interval = 25 };
                _undoAnimTimer.Tick += UndoAnimTimer_Tick;

                // Game-UI initialisieren
                InitGameCombo();
                this.cbGame.SelectedIndexChanged += CbGame_SelectedIndexChanged;
                // Modlisten-UI initialisieren
                this.cbModlist.SelectedIndexChanged += CbModlist_SelectedIndexChanged;

                // Spielauswahl normalisieren + anwenden, Logo/Profiles updaten
                var initCode = NormalizeGameCode(_settings.Current.PreferredGame);
                SelectGameByCode(initCode);
                UpdateGameLogo(initCode);
                try { this.headerBanner.GameCode = initCode; } catch { }
                try { this.headerBanner.Title = _lang["MainForm.Title"]; } catch { this.headerBanner.Title = "ETS2/ATS Modlist Manager"; }
                try { this.headerBanner.Subtitle = _lang["MainForm.Header.Subtitle"]; } catch { this.headerBanner.Subtitle = "Manage and share your modlists"; }
                try { LoadProfilesForSelectedGame(); } catch { }
                try { AutoDecryptProfilesForSelectedGame(); } catch { }
                try { LoadModlistNamesForSelectedGame(); } catch { }

                // Sicherstellen, dass Basisordner für Modlisten existieren
                try { EnsureModlistsDirectories(); } catch { }

                // Einmaliger Hinweis: SII_Decrypt.exe nicht mehr gebündelt – bitte in ModlistManager\\Tools ablegen
                try { ShowSiiDecryptHintIfMissing(); } catch { }

                // Grid-Events
                this.gridMods.RowPostPaint += GridMods_RowPostPaint;
                this.gridMods.CellContentClick += GridMods_CellContentClick;
                this.gridMods.CellEndEdit += GridMods_CellEndEdit;
                this.gridMods.CellMouseDown += GridMods_CellMouseDown;

                WireUiEvents();

                // Footer-Notiz (Modlistenbeschreibung) speichern mit Debounce
                try { this.txtModInfo.TextChanged += TxtModInfo_TextChanged; } catch { }
            }
        }

        private void ShowSiiDecryptHintIfMissing()
        {
            try
            {
                // Nur unterdrücken, wenn der Nutzer explizit "nicht mehr anzeigen" gewählt hat
                if (_settings.Current.SiiDecryptDontShowAgain) return;
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var toolsDir = Path.Combine(baseDir, "ModlistManager", "Tools");
                var exePath = Path.Combine(toolsDir, "SII_Decrypt.exe");
                if (!File.Exists(exePath))
                {
                    using var dlg = new ETS2ATS.ModlistManager.Forms.Common.SiiDecryptHintForm(exePath);
                    try { dlg.Text = T("SiiDecrypt.Hint.Title", "SII_Decrypt.exe erforderlich"); } catch { }
                    try { dlg.lblMessage.Text = T("SiiDecrypt.Hint.Message", "Hinweis: Ab dieser Version wird SII_Decrypt.exe nicht mehr mitgeliefert. Bitte lege die Datei manuell in den Ordner 'ModlistManager\\\\Tools' neben der Anwendung ab."); } catch { }
                    try { dlg.lblPathLabel.Text = T("SiiDecrypt.Hint.ExpectedPath", "Erwarteter Pfad:"); } catch { }
                    try { dlg.chkDontShow.Text = T("SiiDecrypt.Hint.DontShow", "Diesen Hinweis nicht mehr anzeigen"); } catch { }
                    dlg.ShowDialog(this);
                    // Merke, dass wir den Hinweis mindestens einmal gezeigt haben (informativ)
                    _settings.Current.HasShownSiiDecryptHint = true;
                    if (dlg.DontShowAgain)
                    {
                        _settings.Current.SiiDecryptDontShowAgain = true;
                        _settings.Save();
                    }
                }
                else
                {
                    // Datei vorhanden → kein Hinweis nötig; keine Unterdrückungs-Flags setzen
                }
            }
            catch { }
        }

        private void EnsureModlistsDirectories()
        {
            // Neue Strategie: Modlisten gehören in den Spielordner (Installationsverzeichnis)/modlists bzw. Dokumente/<Game>/modlists.
            // Diese Methode sorgt je Spiel für das Anlegen des Zielordners und kopiert Demo-Dateien nur beim ersten Start (keine Überschreibungen).
            try { EnsureModlistsDirectoryForGame("ets2"); } catch { }
            try { EnsureModlistsDirectoryForGame("ats"); } catch { }
        }

        private void EnsureModlistsDirectoryForGame(string norm)
        {
            try
            {
                var root = GetModlistsRootForGame(norm);
                if (string.IsNullOrWhiteSpace(root)) return;
                try { Directory.CreateDirectory(root); } catch { }

                // Nur initial befüllen, wenn noch keine Modlisten (*.txt) existieren
                bool hasTxt = false;
                try { hasTxt = Directory.EnumerateFiles(root, "*.txt", SearchOption.TopDirectoryOnly).Any(); } catch { }
                if (hasTxt) return;

                foreach (var demo in GetDemoSeedCandidates(norm))
                {
                    try
                    {
                        if (!Directory.Exists(demo)) continue;
                        if (!Directory.EnumerateFiles(demo, "*.txt", SearchOption.TopDirectoryOnly).Any()) continue;

                        foreach (var file in Directory.EnumerateFiles(demo, "*", SearchOption.TopDirectoryOnly))
                        {
                            var name = Path.GetFileName(file);
                            if (string.IsNullOrWhiteSpace(name)) continue;
                            if (name.Equals("links.json", StringComparison.OrdinalIgnoreCase))
                            {
                                var target = Path.Combine(root, name);
                                if (!File.Exists(target))
                                {
                                    try { File.Copy(file, target, overwrite: false); } catch { }
                                }
                                continue;
                            }

                            var ext = Path.GetExtension(name);
                            if (ext.Equals(".txt", StringComparison.OrdinalIgnoreCase) ||
                                ext.Equals(".note", StringComparison.OrdinalIgnoreCase) ||
                                ext.Equals(".json", StringComparison.OrdinalIgnoreCase))
                            {
                                var target = Path.Combine(root, name);
                                if (!File.Exists(target))
                                {
                                    try { File.Copy(file, target, overwrite: false); } catch { }
                                }
                            }
                        }
                        break; // erster geeigneter Seed genügt
                    }
                    catch { }
                }
            }
            catch { }
        }

        // Einfache, designerfreundliche Renderer
        private sealed class SimpleColorTable : ProfessionalColorTable
        {
            public override Color MenuStripGradientBegin => Color.FromArgb(245, 245, 247);
            public override Color MenuStripGradientEnd => Color.FromArgb(232, 232, 236);
            public override Color MenuItemSelected => Color.FromArgb(210, 230, 255);
            public override Color MenuItemBorder => Color.FromArgb(140, 170, 200);
            public override Color ToolStripDropDownBackground => Color.White;
            public override Color ImageMarginGradientBegin => Color.FromArgb(245, 245, 247);
            public override Color ImageMarginGradientMiddle => Color.FromArgb(240, 240, 244);
            public override Color ImageMarginGradientEnd => Color.FromArgb(235, 235, 240);
        }

        private sealed class SimpleRenderer : ToolStripProfessionalRenderer
        {
            public SimpleRenderer() : base(new SimpleColorTable()) { }
        }

        // Spielauswahl füllen (Display zeigt Klartext, intern Code)
        private void InitGameCombo()
        {
            cbGame.BeginUpdate();
            cbGame.Items.Clear();
            cbGame.DisplayMember = "Display";
            cbGame.ValueMember = "Code";
            cbGame.Items.Add(new GameItem("ETS2", "Euro Truck Simulator 2"));
            cbGame.Items.Add(new GameItem("ATS",  "American Truck Simulator"));
            cbGame.EndUpdate();
        }

        // Hilfsfunktion: versucht mehrere Pfade, lädt schonend (ohne File-Lock)
        private Image? TryLoadImage(params string[] candidates)
        {
            foreach (var p in candidates)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(p) && File.Exists(p))
                    {
                        using var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        return Image.FromStream(fs);
                    }
                }
                catch { }
            }
            return null;
        }

        // SCS speichert den Profilnamen als Hex (UTF-8 Bytes). Diese Funktion decodiert den Ordnernamen.
        private static string? DecodeProfileFolderName(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) return null;

            // Quick check: gerade Länge, nur Hex-Zeichen
            if (folder.Length % 2 != 0) return null;
            for (int i = 0; i < folder.Length; i++)
            {
                char c = folder[i];
                bool isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!isHex) return null;
            }

            try
            {
                var bytes = new byte[folder.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    string hex = folder.Substring(i * 2, 2);
                    bytes[i] = Convert.ToByte(hex, 16);
                }
                return Encoding.UTF8.GetString(bytes);
            }
            catch { return null; }
        }

        // --- Sprach-Helfer: rekursive Anwendung über Control-Baum ---
        private void ApplyLanguageToControlTree(Control root)
        {
            if (root == null) return;

            // Controls mit Tag
            // WICHTIG: Die Notiz-Textbox (txtModInfo) enthält benutzerdefinierten Inhalt
            // und darf nicht durch die Lokalisierung überschrieben werden.
            if (!object.ReferenceEquals(root, txtModInfo))
            {
                if (root.Tag is string key && !string.IsNullOrWhiteSpace(key))
                {
                    try { root.Text = _lang[key]; } catch { }
                }
            }

            // DataGridView-Spalten
            if (root is DataGridView grid)
            {
                foreach (DataGridViewColumn col in grid.Columns)
                {
                    if (col.Tag is string ckey && !string.IsNullOrWhiteSpace(ckey))
                    {
                        try { col.HeaderText = _lang[ckey]; } catch { }
                    }
                    else if (col.HeaderCell?.Tag is string hKey && !string.IsNullOrWhiteSpace(hKey))
                    {
                        try { col.HeaderText = _lang[hKey]; } catch { }
                    }
                }
            }

            // Menüs (ToolStripItems mit Tag) – Top-Level
            if (root is MenuStrip ms)
            {
                foreach (ToolStripItem item in ms.Items)
                    ApplyLanguageToMenuItem(item);
            }

            foreach (Control c in root.Controls)
                ApplyLanguageToControlTree(c);
        }

        // NEU: Alle MenuStrips und ContextMenuStrips rekursiv anwenden
        private void ApplyLanguageToAllMenuStrips()
        {
            foreach (var ms in this.Controls.OfType<MenuStrip>())
                foreach (ToolStripItem item in ms.Items)
                    ApplyLanguageToMenuItem(item);

            foreach (var cms in this.Controls.OfType<ContextMenuStrip>())
                foreach (ToolStripItem item in cms.Items)
                    ApplyLanguageToMenuItem(item);

            // Explizit auch Grid-Kontextmenü berücksichtigen (falls nicht im Control-Tree gefunden)
            if (cmsGrid != null)
            {
                foreach (ToolStripItem item in cmsGrid.Items)
                    ApplyLanguageToMenuItem(item);
            }
        }

        private void ApplyLanguageToMenuItem(ToolStripItem item)
        {
            if (item == null) return;

            if (item.Tag is string key && !string.IsNullOrWhiteSpace(key))
            {
                try { item.Text = _lang[key]; } catch { }
            }
            if (item is ToolStripMenuItem mi)
            {
                foreach (ToolStripItem sub in mi.DropDownItems)
                    ApplyLanguageToMenuItem(sub);

                // Dynamisch befüllte Menüs beim Öffnen (re)lokalisieren
                mi.DropDownOpening -= MenuItem_DropDownOpening_Relocalize;
                mi.DropDownOpening += MenuItem_DropDownOpening_Relocalize;
            }
        }

        private void MenuItem_DropDownOpening_Relocalize(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem mi)
            {
                foreach (ToolStripItem sub in mi.DropDownItems)
                    ApplyLanguageToMenuItem(sub);
            }
        }

        private void ApplyLanguage()
        {
            try { this.Text = _lang["MainForm.Title"]; } catch { }
            ApplyLanguageToControlTree(this);   // übrige Controls + Grid
            ApplyLanguageToAllMenuStrips();     // alle Menüs inkl. Untermenüs und ContextMenus
            // Banner-Texte aktualisieren
            try { this.headerBanner.Title = _lang["MainForm.Title"]; } catch { }
            try { this.headerBanner.Subtitle = _lang["MainForm.Header.Subtitle"]; } catch { }
            try { this.headerBanner.Invalidate(); } catch { }

            // Platzhalter für Notiz-Textbox setzen (aber Inhalt niemals überschreiben)
            try { if (txtModInfo != null) txtModInfo.PlaceholderText = T("MainForm.ModInfo.Input", "Notiz zur Modliste …"); } catch { }
        }

        // Aktualisiert das Spiel-Logo im Footer entsprechend des gewählten Spiels
        private void UpdateGameLogo(string? gameCode)
        {
            try
            {
                var norm = NormalizeGameCode(gameCode);
                string[] candidates = norm == "ets2"
                    ? new[] {
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Logos", "ets2.png"),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "ets2.png")
                    }
                    : new[] {
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Logos", "ats.png"),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "ats.png")
                    };
                var img = TryLoadImage(candidates);
                if (pbGameLogo != null)
                {
                    var old = pbGameLogo.Image;
                    pbGameLogo.Image = img;
                    try { old?.Dispose(); } catch { }
                }
            }
            catch { }
        }

        // Reagiert auf Änderung der Spiel-Auswahl
        private void CbGame_SelectedIndexChanged(object? sender, EventArgs e)
        {
            try
            {
                var code = (cbGame.SelectedItem as GameItem)?.Code ?? "ETS2";
                var norm = NormalizeGameCode(code);
                _settings.Current.PreferredGame = norm;
                try { headerBanner.GameCode = norm; } catch { }
                UpdateGameLogo(norm);
                LoadProfilesForSelectedGame();
                LoadModlistNamesForSelectedGame();
            }
            catch { }
        }

        // Profile für aktuelles Spiel ermitteln und in die Combo laden
        private void LoadProfilesForSelectedGame()
        {
            try
            {
                var code = (cbGame.SelectedItem as GameItem)?.Code ?? "ETS2";
                var norm = NormalizeGameCode(code);
                var list = EnumerateProfiles(norm);
                cbProfile.BeginUpdate();
                try
                {
                    cbProfile.Items.Clear();
                    foreach (var p in list)
                        cbProfile.Items.Add(p);
                    if (cbProfile.Items.Count > 0)
                        cbProfile.SelectedIndex = 0;
                }
                finally { cbProfile.EndUpdate(); }
            }
            catch { }
        }

        // Findet Profile (Dokumente/<Game>/profiles oder benutzerdefinierte Pfade)
        private IEnumerable<ProfileItem> EnumerateProfiles(string normCode)
        {
            var items = new List<ProfileItem>();
            try
            {
                string? customProfiles = GetProfilesRootForGame(normCode);
                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var gameFolderName = normCode == "ets2" ? "Euro Truck Simulator 2" : "American Truck Simulator";
                var gameRoot = Path.Combine(docs, gameFolderName);

                var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(customProfiles) && Directory.Exists(customProfiles))
                    roots.Add(customProfiles);
                var defaultProfiles = Path.Combine(gameRoot, "profiles");
                if (Directory.Exists(defaultProfiles)) roots.Add(defaultProfiles);

                foreach (var root in roots)
                {
                    bool isSteam = false; // Steam-Profile werden aktuell nicht gesondert markiert
                    try
                    {
                        foreach (var dir in Directory.EnumerateDirectories(root))
                        {
                            var folder = Path.GetFileName(dir);
                            var display = DecodeProfileFolderName(folder) ?? folder;
                            items.Add(new ProfileItem(display, dir, isSteam));
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return items.OrderBy(p => p.Display, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        // --- FAQ ---
        private void ShowFaq()
        {
            try
            {
                using var dlg = new FaqForm(_lang);
                dlg.ShowDialog(this);
            }
            catch (Exception ex)
            {
                try { MessageBox.Show(this, ex.Message, "FAQ", MessageBoxButtons.OK, MessageBoxIcon.Error); } catch { }
            }
        }

        // Menü: Modlistenordner öffnen
        private void OpenModlistsFolder_Click(object? sender, EventArgs e)
        {
            try
            {
                var code = (cbGame.SelectedItem as GameItem)?.Code ?? "ETS2";
                var norm = NormalizeGameCode(code);
                var root = GetModlistsRootForGame(norm);
                if (!string.IsNullOrWhiteSpace(root))
                {
                    try { Directory.CreateDirectory(root); } catch { }
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(root) { UseShellExecute = true });
                }
            }
            catch { }
        }

        // Optionen-Dialog öffnen, Werte übernehmen, speichern und anwenden
        private void OpenOptionsDialog()
        {
            using var dlg = new OptionsForm(_settings);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _settings.Current.Language = dlg.SelectedLanguageCode;
                _settings.Current.Theme = dlg.SelectedTheme;
                _settings.Current.PreferredGame = dlg.SelectedPreferredGame;
                _settings.Current.Ets2ProfilesPath = dlg.Ets2Path;
                _settings.Current.AtsProfilesPath = dlg.AtsPath;
                _settings.Save();

                // Sprache laden + anwenden
                try { _lang.Load(dlg.SelectedLanguageCode); } catch { }
                ApplyLanguage();

                // Theme anwenden (und Grid-Buttons passend stylen)
                try { _theme.Apply(this, dlg.SelectedTheme); } catch { }
                try { _theme.ApplyToDataGridView(this.gridMods, string.Equals(dlg.SelectedTheme, "Dark", StringComparison.OrdinalIgnoreCase)); } catch { }
                try { this.headerBanner.ApplyTheme(string.Equals(dlg.SelectedTheme, "Dark", StringComparison.OrdinalIgnoreCase)); } catch { }

                // Game-Combo/Logo/Profiles aktualisieren
                try
                {
                    SelectGameByCode(dlg.SelectedPreferredGame);
                    UpdateGameLogo(dlg.SelectedPreferredGame);
                    try { this.headerBanner.GameCode = dlg.SelectedPreferredGame; } catch { }
                    LoadProfilesForSelectedGame();
                    // Neueinstellung der Modlistenpfade übernehmen
                    try { EnsureModlistsDirectoryForGame("ets2"); } catch { }
                    try { EnsureModlistsDirectoryForGame("ats"); } catch { }
                    try { LoadModlistNamesForSelectedGame(); } catch { }
                }
                catch { }
            }
        }

        // Hilfs-Methode, um in cbGame per Code zu wählen
        private void SelectGameByCode(string code)
        {
            for (int i = 0; i < cbGame.Items.Count; i++)
            {
                if (cbGame.Items[i] is GameItem gi && gi.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
                {
                    cbGame.SelectedIndex = i;
                    return;
                }
            }
            if (cbGame.Items.Count > 0) cbGame.SelectedIndex = 0;
        }

        // Row-Nummerierung: setzt # = RowIndex+1 automatisch
        private void GridMods_RowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
        {
            if (gridMods.Columns.Count == 0) return;
            var row = gridMods.Rows[e.RowIndex];
            var idxCol = gridMods.Columns["colIndex"];
            if (idxCol != null)
                row.Cells[idxCol.Index].Value = (e.RowIndex + 1).ToString();
        }

        // Link-Klicks: Download/Suche
        private void GridMods_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var col = gridMods.Columns[e.ColumnIndex];
            if (col == null) return;

            if (col.Name == "colDownload")
            {
                var url = gridMods.Rows[e.RowIndex].Cells["colUrl"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(url) && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
                    catch { /* TODO: Meldung */ }
                }
            }

            if (col.Name == "colSearch")
            {
                var modname = gridMods.Rows[e.RowIndex].Cells["colModName"].Value?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(modname))
                {
                    var q = Uri.EscapeDataString($"ETS2 {modname} download");
                    var url = $"https://www.google.com/search?q={q}";
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
                    catch { /* TODO: Meldung */ }
                }
            }
        }

        // Bearbeitung der Kurzinfo/Modname persistieren
        private void GridMods_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0) return;
                var col = gridMods.Columns[e.ColumnIndex];
                if (col == null) return;
                try { gridMods.CommitEdit(DataGridViewDataErrorContexts.Commit); } catch { }
                try { gridMods.EndEdit(); } catch { }

                // Modname-Spalte editierbar und speichert Änderungen in <Modlistname>.json
                if (col.Name == "colModName")
                {
                    var modlistPath = _currentModlistPath;
                    if (string.IsNullOrWhiteSpace(modlistPath))
                    {
                        if (cbModlist?.SelectedItem is not ModlistItem it || string.IsNullOrWhiteSpace(it.Path)) return;
                        modlistPath = it.Path;
                    }
                    var pkg = gridMods.Rows[e.RowIndex].Cells["colPackage"].Value?.ToString()?.Trim();
                    var cellName = gridMods.Rows[e.RowIndex].Cells["colModName"];
                    var modName = (cellName?.EditedFormattedValue ?? cellName?.Value)?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(pkg)) return;

                    // Laden, aktualisieren, speichern (strukturierte Map: Package -> {modname, info})
                    var map = LoadInfoMapFor(modlistPath) ?? new Dictionary<string, ModMeta>(StringComparer.CurrentCultureIgnoreCase);
                    if (!map.TryGetValue(pkg, out var meta))
                    {
                        meta = new ModMeta();
                        map[pkg] = meta;
                    }
                    meta.ModName = string.IsNullOrWhiteSpace(modName) ? null : modName;
                    // Key entfernen, wenn beide Werte leer sind
                    if (string.IsNullOrWhiteSpace(meta.ModName) && string.IsNullOrWhiteSpace(meta.Info))
                    {
                        map.Remove(pkg);
                    }
                    SaveInfoMapFor(modlistPath, map);
                    try { ShowStatus($"Modname gespeichert: {modName} für Package: {pkg}"); } catch { }
                    return;
                }

                // Info-Spalte wie bisher
                if (col.Name != "colInfo") return; // nur Info-Spalte
                var modlistPathInfo = _currentModlistPath;
                if (string.IsNullOrWhiteSpace(modlistPathInfo))
                {
                    if (cbModlist?.SelectedItem is not ModlistItem it || string.IsNullOrWhiteSpace(it.Path)) return;
                    modlistPathInfo = it.Path;
                }
                var pkgInfo = gridMods.Rows[e.RowIndex].Cells["colPackage"].Value?.ToString()?.Trim();
                var cellInfo = gridMods.Rows[e.RowIndex].Cells[e.ColumnIndex];
                var text = (cellInfo?.EditedFormattedValue ?? cellInfo?.Value)?.ToString() ?? string.Empty;

                // Laden, aktualisieren, speichern
                var mapInfo = LoadInfoMapFor(modlistPathInfo) ?? new Dictionary<string, ModMeta>(StringComparer.CurrentCultureIgnoreCase);
                string key = pkgInfo ?? string.Empty;
                if (string.IsNullOrWhiteSpace(key)) return;

                if (!mapInfo.TryGetValue(key, out var metaInfo))
                {
                    metaInfo = new ModMeta();
                    mapInfo[key] = metaInfo;
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    // leere Eingabe entfernt die Info (Key nur löschen, wenn keine ModName-Override vorhanden)
                    metaInfo.Info = null;
                    if (string.IsNullOrWhiteSpace(metaInfo.ModName))
                    {
                        mapInfo.Remove(key);
                    }
                    try { ShowStatus(T("MainForm.Info.Removed", "Kurzinfo entfernt") + ": " + key); } catch { }
                }
                else
                {
                    metaInfo.Info = text;
                    try { ShowStatus(T("MainForm.Info.Saved", "Kurzinfo gespeichert") + ": " + key); } catch { }
                }

                SaveInfoMapFor(modlistPathInfo, mapInfo);
            }
            catch { }
        }

        // --- Modlisten laden ---
        private string GetModlistsRootForGame(string norm)
        {
            // 0) Benutzerdefinierte Pfade aus den Einstellungen
            try
            {
                var custom = norm.Equals("ets2", StringComparison.OrdinalIgnoreCase) ? _settings.Current.Ets2ModlistsPath : _settings.Current.AtsModlistsPath;
                if (!string.IsNullOrWhiteSpace(custom)) return custom!;
            }
            catch { }

            var candidates = new List<string>();
            // 1) Dokumente/<Game>/modlists (Standard)
            try
            {
                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var gameFolder = norm.Equals("ets2", StringComparison.OrdinalIgnoreCase) ? "Euro Truck Simulator 2" : "American Truck Simulator";
                candidates.Add(Path.Combine(docs, gameFolder, "modlists"));
            }
            catch { }
            // 2) Spiel-Installationsverzeichnis/modlists
            try
            {
                var install = FindGameInstallDir(norm);
                if (!string.IsNullOrWhiteSpace(install)) candidates.Add(Path.Combine(install!, "modlists"));
            }
            catch { }
            // 3) Fallback: App-Verzeichnis (Kompatibilität / erste Nutzung)
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var sub = norm.Equals("ets2", StringComparison.OrdinalIgnoreCase) ? "ETS2" : "ATS";
                candidates.Add(Path.Combine(baseDir, "ModlistManager", "modlists", sub));
                candidates.Add(Path.Combine(baseDir, "modlists", sub));
            }
            catch { }

            return candidates.First();
        }

        private IEnumerable<string> GetDemoSeedCandidates(string norm)
        {
            var list = new List<string>();
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var sub = norm.Equals("ets2", StringComparison.OrdinalIgnoreCase) ? "ETS2" : "ATS";
                list.Add(Path.Combine(baseDir, "ModlistManager", "modlists", sub));
                list.Add(Path.Combine(baseDir, "modlists", sub));
                // Elternordner hochlaufen (Dev-Szenario)
                string? dir = baseDir;
                for (int i = 0; i < 6 && !string.IsNullOrEmpty(dir); i++)
                {
                    var parent = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    if (string.IsNullOrEmpty(parent)) break;
                    list.Add(Path.Combine(parent, "ModlistManager", "modlists", sub));
                    list.Add(Path.Combine(parent, "modlists", sub));
                    dir = parent;
                }
            }
            catch { }
            return list.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        // Struktur für persistierte Metadaten: pro Package optional ModName-Override und Kurzinfo
        private sealed class ModMeta
        {
            public string? ModName { get; set; }
            public string? Info { get; set; }
        }

        private sealed class ModlistItem
        {
            public string Display { get; }
            public string Path { get; }
            public ModlistItem(string display, string path) { Display = display; Path = path; }
            public override string ToString() => Display;
        }

        private void LoadModlistNamesForSelectedGame()
        {
            var code = (cbGame.SelectedItem as GameItem)?.Code ?? "ETS2";
            var norm = NormalizeGameCode(code);
            var root = GetModlistsRootForGame(norm);
            try { Directory.CreateDirectory(root); } catch { }

            cbModlist.BeginUpdate();
            try
            {
                cbModlist.Items.Clear();
                int fileCount = 0;
                if (Directory.Exists(root))
                {
                    var files = Directory.EnumerateFiles(root, "*.txt", SearchOption.TopDirectoryOnly)
                                                 .OrderBy(f => f, StringComparer.CurrentCultureIgnoreCase);
                    foreach (var file in files)
                    {
                        var name = Path.GetFileNameWithoutExtension(file);
                        cbModlist.Items.Add(new ModlistItem(name, file));
                        fileCount++;
                    }
                }

                if (cbModlist.Items.Count > 0)
                    cbModlist.SelectedIndex = 0;
                else
                    ClearModsGrid();

                // Statusmeldung
                if (fileCount == 0)
                    ShowStatus(T("MainForm.Modlists.FoundNone", "Keine Modlisten gefunden: ") + root);
                else
                    ShowStatus(T("MainForm.Modlists.Found", "Modlisten gefunden: ") + fileCount + " (" + root + ")");
            }
            finally
            {
                cbModlist.EndUpdate();
            }
        }

        private void CbModlist_SelectedIndexChanged(object? sender, EventArgs e)
        {
            try
            {
                // Vor dem Wechsel die aktuell bearbeitete Notiz (falls geändert) wegschreiben
                try { SaveCurrentModlistNoteImmediate(); } catch { }
                // Offene Bearbeitungen abschließen, um inkonsistente Saves zu vermeiden
                try { gridMods.EndEdit(); } catch { }
                try { gridMods.CommitEdit(DataGridViewDataErrorContexts.Commit); } catch { }
                try { gridMods.CurrentCell = null; } catch { }

                LoadSelectedModlistIntoGrid();
            }
            catch { }
        }

        private void LoadSelectedModlistIntoGrid()
        {
            if (cbModlist?.SelectedItem is not ModlistItem it || string.IsNullOrWhiteSpace(it.Path) || !File.Exists(it.Path))
            {
                ClearModsGrid();
                _currentModlistPath = null;
                // Notiz leeren
                try
                {
                    _loadingModInfo = true;
                    txtModInfo.Text = string.Empty;
                    _lastNoteText = string.Empty;
                }
                finally { _loadingModInfo = false; }
                return;
            }
            // Merke aktive Modliste
            _currentModlistPath = it.Path;

            string[] lines;
            try { lines = File.ReadAllLines(it.Path); }
            catch { ClearModsGrid(); ShowStatus(T("MainForm.Modlists.ReadError", "Modliste konnte nicht gelesen werden: ") + it.Display); return; }
            if (lines.Length == 0) { ClearModsGrid(); return; }

            int start = 0;
            // Erste Zeile kann die Anzahl enthalten; wenn Zahl, überspringen
            if (int.TryParse(lines[0].Trim(), out _)) start = 1;

            var entries = new List<(string Package, string ModName)>();
            for (int i = start; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;
                var pair = TryParseModlistLine(line);
                entries.Add(pair);
            }

            // Umgekehrte Reihenfolge ins Grid
            entries.Reverse();

            // Die letzte Grid-Zeile (entspricht der ersten Zeile der Modliste) nicht anzeigen
            if (entries.Count > 0)
            {
                entries.RemoveAt(entries.Count - 1);
            }

            // Links-Mapping laden (global + modlist-spezifisch)
            var linkMap = LoadLinkMapFor(it.Path);
            // Kurzinfos + Modname-Overrides (per Modliste) laden
            var infoMap = LoadInfoMapFor(it.Path) ?? new Dictionary<string, ModMeta>(StringComparer.CurrentCultureIgnoreCase);
            // Zusätzliche normalisierte Map (nur wenn Links vorhanden)
            Dictionary<string, string>? linkMapNorm = null;
            static string NormalizeKey(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return string.Empty;
                var sb = new System.Text.StringBuilder(s.Length);
                foreach (var ch in s)
                {
                    if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
                }
                return sb.ToString();
            }
            if (linkMap != null)
            {
                linkMapNorm = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in linkMap)
                {
                    var nk = NormalizeKey(kv.Key);
                    if (!string.IsNullOrEmpty(nk)) linkMapNorm[nk] = kv.Value;
                }
            }

            // Lokale Helfer: URL normalisieren (http/https erzwingen, falls plausibel)
            static string? NormalizeUrl(string? raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return null;
                var u = raw.Trim();
                if (u.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    return u;
                // Wenn es wie eine Domain aussieht oder mit www. beginnt → https:// voranstellen
                bool looksDomain = u.StartsWith("www.", StringComparison.OrdinalIgnoreCase) || (u.Contains('.') && !u.Contains(' '));
                if (looksDomain)
                    return "https://" + u;
                return u; // sonst roh zurück
            }

            try { gridMods.EndEdit(); } catch { }
            try { gridMods.CancelEdit(); } catch { }
            try { gridMods.CurrentCell = null; } catch { }

            // Verfügbare Mods einmal ermitteln (lokal + Workshop), um je Zeile schnellen Status zu setzen
            var currentGameCode = (cbGame.SelectedItem as GameItem)?.Code ?? "ETS2";
            var normGame = NormalizeGameCode(currentGameCode);
            var availableLocal = new HashSet<string>(StringComparer.OrdinalIgnoreCase);             // Dateiname inkl. Ext
            var availableLocalNoExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase);        // Dateiname ohne Ext
            var availableWorkshop = new HashSet<string>(StringComparer.OrdinalIgnoreCase);         // Dateiname inkl. Ext
            var availableWorkshopNoExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase);    // Dateiname ohne Ext
            var availableWorkshopDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);     // reine Verzeichnisnamen (IDs)
            var availableLocalNormNoExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase);   // normalisiert, ohne Ext
            var availableWorkshopNormNoExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase);// normalisiert, ohne Ext
            var workshopTitlesNorm = new HashSet<string>(StringComparer.OrdinalIgnoreCase);        // normalisierte Workshop-Titel
            var workshopTitlesByNorm = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // norm -> Originaltitel
            var workshopTitlesById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);   // Dezimal-ID -> Titel
            var workshopSubscribedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);           // alle IDs aus ACF (abonniert)
            var workshopTitleTokensById = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase); // ID -> Tokensatz
            var workshopFileTokensById = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);  // ID -> Tokensatz aus scs-Dateinamen
            try
            {
                var modsFolder = GetModsFolderForGame(normGame);
                if (!string.IsNullOrWhiteSpace(modsFolder) && Directory.Exists(modsFolder))
                {
                    foreach (var f in Directory.EnumerateFiles(modsFolder, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(p => p.EndsWith(".scs", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)))
                    {
                        var fn = Path.GetFileName(f);
                        availableLocal.Add(fn);
                        var noExt = Path.GetFileNameWithoutExtension(fn);
                        if (!string.IsNullOrWhiteSpace(noExt)) availableLocalNoExt.Add(noExt);
                        if (!string.IsNullOrWhiteSpace(noExt)) availableLocalNormNoExt.Add(NormalizeKey(noExt));
                    }
                }

                // Zusätzlich: Mod-Ordner direkt im Spiel-Installationsverzeichnis scannen (<Install>/mod)
                try
                {
                    var installDir = FindGameInstallDir(normGame);
                    if (!string.IsNullOrWhiteSpace(installDir))
                    {
                        var installMod = Path.Combine(installDir, "mod");
                        if (Directory.Exists(installMod))
                        {
                            foreach (var f in Directory.EnumerateFiles(installMod, "*.*", SearchOption.TopDirectoryOnly)
                                .Where(p => p.EndsWith(".scs", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)))
                            {
                                var fn = Path.GetFileName(f);
                                availableLocal.Add(fn);
                                var noExt = Path.GetFileNameWithoutExtension(fn);
                                if (!string.IsNullOrWhiteSpace(noExt)) availableLocalNoExt.Add(noExt);
                            }
                        }
                    }
                }
                catch { }

                // Steam Workshop durchsuchen (optional, kann je nach Anzahl ein wenig dauern)
                try
                {
                    var appId = normGame == "ets2" ? "227300" : "270880";
                    var steamPath = TryGetSteamPathFromRegistry();
                    var libs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var l in EnumerateSteamLibraries(steamPath)) libs.Add(l);
                    // Fallback: Library-Root aus Installationspfad ableiten
                    try
                    {
                        var install = FindGameInstallDir(normGame);
                        if (!string.IsNullOrWhiteSpace(install))
                        {
                            var p = new DirectoryInfo(install);
                            for (int i = 0; i < 3 && p?.Parent != null; i++) p = p.Parent; // .. -> steamapps -> <lib>
                            var root = p?.FullName;
                            if (!string.IsNullOrWhiteSpace(root) && Directory.Exists(root)) libs.Add(root);
                        }
                    }
                    catch { }
                    bool anyFound = false;
                    foreach (var lib in libs)
                    {
                        var overrideContent = (normGame == "ets2" ? _settings.Current.Ets2WorkshopContentOverride : _settings.Current.AtsWorkshopContentOverride);
                        var content = !string.IsNullOrWhiteSpace(overrideContent)
                            ? overrideContent!
                            : Path.Combine(lib, "steamapps", "workshop", "content", appId);
                        if (!Directory.Exists(content)) continue;
                        AppendLog($"[Workshop] content: {content}");
                        anyFound = true;
                        // Versuche Titel aus appworkshop_<appid>.acf zu lesen (sowohl Library als auch Haupt-Steam-Pfad)
                        try
                        {
                            var acfCandidates = new List<string>();
                            acfCandidates.Add(Path.Combine(lib, "steamapps", "workshop", $"appworkshop_{appId}.acf"));
                            if (!string.IsNullOrWhiteSpace(steamPath))
                                acfCandidates.Add(Path.Combine(steamPath!, "steamapps", "workshop", $"appworkshop_{appId}.acf"));
                            foreach (var acf in acfCandidates.Distinct(StringComparer.OrdinalIgnoreCase))
                            {
                                if (!File.Exists(acf)) continue;
                                var titlesMap = TryReadWorkshopTitles(acf);
                                foreach (var kv in titlesMap)
                                {
                                    workshopSubscribedIds.Add(kv.Key);
                                    workshopTitlesById[kv.Key] = kv.Value;
                                    var n = NormalizeKey(kv.Value);
                                    if (!string.IsNullOrWhiteSpace(n))
                                    {
                                        workshopTitlesNorm.Add(n);
                                        if (!workshopTitlesByNorm.ContainsKey(n)) workshopTitlesByNorm[n] = kv.Value;
                                    }
                                }
                            }
                        }
                        catch { }
                        // Verzeichnisnamen (IDs) vormerken und manifest.sii für Titel lesen
                        try
                        {
                            foreach (var dir in Directory.EnumerateDirectories(content))
                            {
                                var dn = Path.GetFileName(dir);
                                if (!string.IsNullOrWhiteSpace(dn)) availableWorkshopDirs.Add(dn);
                                // Debug: erstes paar Einträge loggen
                                if (availableWorkshopDirs.Count <= 3) AppendLog($"[Workshop] dir id: {dn}");
                                // manifest.sii -> Name/Titel extrahieren
                                try
                                {
                                    var manifest = Path.Combine(dir, "manifest.sii");
                                    if (File.Exists(manifest))
                                    {
                                        foreach (var raw in File.ReadLines(manifest))
                                        {
                                            var line = raw.Trim();
                                            // Flexibler: Keys wie display_name/name/title/… zulassen
                                            var colon = line.IndexOf(':');
                                            if (colon > 0)
                                            {
                                                var key = line.Substring(0, colon).Trim();
                                                if (key.IndexOf("name", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("title", StringComparison.OrdinalIgnoreCase) >= 0)
                                                {
                                                    var q1 = line.IndexOf('"', colon + 1);
                                                    if (q1 >= 0)
                                                    {
                                                        var q2 = line.IndexOf('"', q1 + 1);
                                                        if (q2 > q1)
                                                        {
                                                            var title = line.Substring(q1 + 1, q2 - q1 - 1);
                                                            var n = NormalizeKey(title);
                                                            if (!string.IsNullOrWhiteSpace(n))
                                                            {
                                                                workshopTitlesNorm.Add(n);
                                                                if (!workshopTitlesByNorm.ContainsKey(n)) workshopTitlesByNorm[n] = title;
                                                                if (!string.IsNullOrWhiteSpace(dn))
                                                                {
                                                                    workshopTitlesById[dn] = title;
                                                                    workshopTitleTokensById[dn] = Tokenize(title);
                                                                }
                                                                if (workshopTitlesById.Count <= 3) AppendLog($"[Workshop] manifest title: {dn} -> {title}");
                                                            }
                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    // Fallback: manifest.sii aus .scs-Archiv im Verzeichnis lesen
                                    if (!workshopTitlesById.ContainsKey(dn ?? string.Empty))
                                    {
                                        foreach (var scs in Directory.EnumerateFiles(dir, "*.scs", SearchOption.TopDirectoryOnly))
                                        {
                                            try
                                            {
                                                using var z = ZipFile.OpenRead(scs);
                                                var entry = z.GetEntry("manifest.sii") ?? z.Entries.FirstOrDefault(e => e.FullName.EndsWith("/manifest.sii", StringComparison.OrdinalIgnoreCase));
                                                if (entry != null)
                                                {
                                                    using var er = new StreamReader(entry.Open(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                                                    string? line;
                                                    while ((line = er.ReadLine()) != null)
                                                    {
                                                        var tline = line.Trim();
                                                        var colon = tline.IndexOf(':');
                                                        if (colon > 0)
                                                        {
                                                            var key = tline.Substring(0, colon).Trim();
                                                            if (key.IndexOf("name", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("title", StringComparison.OrdinalIgnoreCase) >= 0)
                                                            {
                                                                var q1 = tline.IndexOf('"', colon + 1);
                                                                if (q1 >= 0)
                                                                {
                                                                    var q2 = tline.IndexOf('"', q1 + 1);
                                                                    if (q2 > q1)
                                                                    {
                                                                        var title = tline.Substring(q1 + 1, q2 - q1 - 1);
                                                                        var n = NormalizeKey(title);
                                                                        if (!string.IsNullOrWhiteSpace(n))
                                                                        {
                                                                            workshopTitlesNorm.Add(n);
                                                                            if (!workshopTitlesByNorm.ContainsKey(n)) workshopTitlesByNorm[n] = title;
                                                                            if (!string.IsNullOrWhiteSpace(dn))
                                                                            {
                                                                                workshopTitlesById[dn] = title;
                                                                                workshopTitleTokensById[dn] = Tokenize(title);
                                                                            }
                                                                            if (workshopTitlesById.Count <= 3) AppendLog($"[Workshop] scs manifest title: {dn} -> {title}");
                                                                        }
                                                                        break;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            catch { }
                                        }
                                        // Auch Tokens aus SCS-Dateinamen sammeln
                                        try
                                        {
                                            foreach (var scs in Directory.EnumerateFiles(dir, "*.scs", SearchOption.TopDirectoryOnly))
                                            {
                                                var name = Path.GetFileNameWithoutExtension(scs) ?? string.Empty;
                                                if (string.IsNullOrWhiteSpace(name)) continue;
                                                var toks = Tokenize(name);
                                                if (!string.IsNullOrWhiteSpace(dn))
                                                {
                                                    if (!workshopFileTokensById.TryGetValue(dn, out var set))
                                                    {
                                                        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                                        workshopFileTokensById[dn] = set;
                                                    }
                                                    foreach (var t in toks) set.Add(t);
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                }
                                catch { }
                            }
                        }
                        catch { }
                        foreach (var f in Directory.EnumerateFiles(content, "*.*", SearchOption.AllDirectories)
                            .Where(p => p.EndsWith(".scs", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)))
                        {
                            var fn = Path.GetFileName(f);
                            availableWorkshop.Add(fn);
                            var noExt = Path.GetFileNameWithoutExtension(fn);
                            if (!string.IsNullOrWhiteSpace(noExt)) availableWorkshopNoExt.Add(noExt);
                            if (!string.IsNullOrWhiteSpace(noExt)) availableWorkshopNormNoExt.Add(NormalizeKey(noExt));
                        }
                    }
                    // Zusammenfassung loggen
                    AppendLog($"[Workshop] IDs: {availableWorkshopDirs.Count}, ACF IDs: {workshopSubscribedIds.Count}, Titles: {workshopTitlesByNorm.Count}");
                    if (!anyFound)
                    {
                        AppendLog("[Workshop] Kein Workshop-Content-Pfad gefunden. Du kannst ihn manuell in den Optionen setzen.");
                    }
                }
                catch { }
            }
            catch { }

            gridMods.Rows.Clear();
            foreach (var e in entries)
            {
                int idx = gridMods.Rows.Add();
                var row = gridMods.Rows[idx];
                try
                {
                    if (gridMods.Columns.Contains("colPackage")) row.Cells["colPackage"].Value = e.Package;
                    if (gridMods.Columns.Contains("colModName")) row.Cells["colModName"].Value = e.ModName;

                    string? url = null;
                    if (linkMap != null)
                    {
                        var keyMod = (e.ModName ?? string.Empty).Trim();
                        var keyPkg = (e.Package ?? string.Empty).Trim();
                        // 1) Direkte Treffer (case-insensitiv)
                        if (!string.IsNullOrEmpty(keyMod) && !linkMap.TryGetValue(keyMod, out url))
                        {
                            if (!string.IsNullOrEmpty(keyPkg)) linkMap.TryGetValue(keyPkg, out url);
                        }
                        // 2) Normalisierte Treffer (nur Buchstaben+Ziffern)
                        if (string.IsNullOrEmpty(url) && linkMapNorm != null)
                        {
                            var nMod = NormalizeKey(keyMod);
                            var nPkg = NormalizeKey(keyPkg);
                            if (!string.IsNullOrEmpty(nMod) && !linkMapNorm.TryGetValue(nMod, out url))
                            {
                                if (!string.IsNullOrEmpty(nPkg)) linkMapNorm.TryGetValue(nPkg, out url);
                            }
                        }
                        // 3) Schwacher Fallback: Contains-Match auf normalisierte Keys (z. B. wenn Versionen angehängt sind)
                        if (string.IsNullOrEmpty(url) && linkMapNorm != null)
                        {
                            var nQuery = NormalizeKey(!string.IsNullOrEmpty(keyMod) ? keyMod : keyPkg);
                            if (!string.IsNullOrEmpty(nQuery) && nQuery.Length >= 4)
                            {
                                // suche zuerst Keys, die den Query komplett enthalten
                                var hit = linkMapNorm.FirstOrDefault(kv => kv.Key.Contains(nQuery, StringComparison.OrdinalIgnoreCase));
                                if (!string.IsNullOrEmpty(hit.Key)) url = hit.Value;
                                // falls nichts: versuche umgekehrt (Query enthält Key)
                                if (string.IsNullOrEmpty(url))
                                {
                                    var hit2 = linkMapNorm.FirstOrDefault(kv => nQuery.Contains(kv.Key, StringComparison.OrdinalIgnoreCase));
                                    if (!string.IsNullOrEmpty(hit2.Key)) url = hit2.Value;
                                }
                            }
                        }
                    }

                    // URL ggf. auf absoluten Link normalisieren
                    url = NormalizeUrl(url);

                    // Hidden colUrl = echte URL für Download-Click
                    if (gridMods.Columns.Contains("colUrl"))
                    {
                        row.Cells["colUrl"].Value = url ?? string.Empty;
                    }

                    // Modname-Override aus JSON (nur per Package-Key)
                    if (gridMods.Columns.Contains("colModName"))
                    {
                        if (!string.IsNullOrWhiteSpace(e.Package) && infoMap.TryGetValue(e.Package, out var metaName) && !string.IsNullOrWhiteSpace(metaName.ModName))
                        {
                            row.Cells["colModName"].Value = metaName.ModName;
                        }
                    }

                    // Info-Spalte (Kurzinfo) aus per‑Liste JSON
                    if (gridMods.Columns.Contains("colInfo"))
                    {
                        string? infoText = null;
                        // Vorrang: Package; Fallback: Modname (Legacy-Keys)
                        if (!string.IsNullOrWhiteSpace(e.Package))
                        {
                            if (infoMap.TryGetValue(e.Package, out var metaPkg) && !string.IsNullOrWhiteSpace(metaPkg.Info))
                            {
                                infoText = metaPkg.Info;
                            }
                            else if (!string.IsNullOrWhiteSpace(e.ModName) && infoMap.TryGetValue(e.ModName, out var metaByName) && !string.IsNullOrWhiteSpace(metaByName.Info))
                            {
                                infoText = metaByName.Info;
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(e.ModName))
                        {
                            if (infoMap.TryGetValue(e.ModName, out var metaByName2) && !string.IsNullOrWhiteSpace(metaByName2.Info))
                            {
                                infoText = metaByName2.Info;
                            }
                        }
                        row.Cells["colInfo"].Value = infoText ?? string.Empty;
                    }

                    // Status-Spalte: Mod vorhanden/fehlend anhand Mods-Ordner (+ Workshop) anzeigen
                    if (gridMods.Columns.Contains("colStatus"))
                    {
                        var statusCell = row.Cells[gridMods.Columns["colStatus"].Index];

                        // Kandidaten bilden (genaue Dateinamen mit/ohne Erweiterung)
                        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        void AddCandidate(string? name)
                        {
                            if (string.IsNullOrWhiteSpace(name)) return;
                            var n = name.Trim();
                            candidates.Add(n);
                            if (!n.EndsWith(".scs", StringComparison.OrdinalIgnoreCase) && !n.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                            {
                                candidates.Add(n + ".scs");
                                candidates.Add(n + ".zip");
                            }
                        }

                        AddCandidate(e.Package);
                        AddCandidate(e.ModName);

                        string? foundSource = null;
                        string? foundName = null;
                        bool MatchAny(Func<string, bool> pred)
                        {
                            foreach (var c in candidates) if (pred(c)) return true;
                            return false;
                        }

                        // normalisierte Kandidaten ohne Endung vorbereiten
                        var candNoExt = candidates.Select(c => Path.GetFileNameWithoutExtension(c) ?? string.Empty).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                        var candNorm = candNoExt.Select(NormalizeKey).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

                        bool found = false;
                        // 1) Exakte Dateinamen (mit Ext)
                        foreach (var c in candidates)
                        {
                            if (availableLocal.Contains(c)) { found = true; foundSource = "mod"; foundName = c; break; }
                            if (availableWorkshop.Contains(c)) { found = true; foundSource = "workshop"; foundName = c; break; }
                        }
                        // 2) Dateinamen ohne Ext (exakt)
                        if (!found)
                        {
                            foreach (var c in candNoExt)
                            {
                                if (availableLocalNoExt.Contains(c)) { found = true; foundSource = "mod"; foundName = c; break; }
                                if (availableWorkshopNoExt.Contains(c)) { found = true; foundSource = "workshop"; foundName = c; break; }
                            }
                        }
                        // 3) Normalisierte Contains-Matches (beide Richtungen) ohne Ext
                        if (!found)
                        {
                            foreach (var c in candNorm)
                            {
                                if (availableLocalNormNoExt.Any(a => a.Contains(c, StringComparison.OrdinalIgnoreCase))
                                    || candNorm.Any(x => availableLocalNormNoExt.Any(a => c.Contains(a, StringComparison.OrdinalIgnoreCase))))
                                { found = true; foundSource = "mod"; break; }
                                if (availableWorkshopNormNoExt.Any(a => a.Contains(c, StringComparison.OrdinalIgnoreCase))
                                    || candNorm.Any(x => availableWorkshopNormNoExt.Any(a => c.Contains(a, StringComparison.OrdinalIgnoreCase))))
                                { found = true; foundSource = "workshop"; break; }
                            }
                        }
                        // 4) Workshop-ID aus mod_workshop_package.<hex> ableiten (normal + little-endian-Reverse) und gegen Dezimal-ID prüfen
                        if (!found)
                        {
                            foreach (var decId in ExtractWorkshopIdCandidates(e.Package))
                            {
                                if (availableWorkshopDirs.Contains(decId))
                                { found = true; foundSource = "workshop"; foundName = workshopTitlesById.TryGetValue(decId, out var t) ? t : decId; break; }
                                if (workshopSubscribedIds.Contains(decId))
                                { found = true; foundSource = "workshop"; foundName = workshopTitlesById.TryGetValue(decId, out var t2) ? t2 : decId; break; }
                            }
                            // Debug für Workshop-Pakete
                            if (!found && !string.IsNullOrWhiteSpace(e.Package) && e.Package.StartsWith("mod_workshop_package.", StringComparison.OrdinalIgnoreCase))
                            {
                                var ids = string.Join(",", ExtractWorkshopIdCandidates(e.Package));
                                AppendLog($"[Workshop] No match for {e.Package} | candidates: {ids}");
                            }
                        }
                        // 5) Workshop-Verzeichnisnamen (IDs) matchen
                        if (!found)
                        {
                            foreach (var c in candNoExt)
                            {
                                if (availableWorkshopDirs.Contains(c)) { found = true; foundSource = "workshop"; foundName = c; break; }
                            }
                        }
                        // 6) Workshop-Titel (normalisiert) matchen: contains in beide Richtungen
                        if (!found && workshopTitlesNorm.Count > 0)
                        {
                            foreach (var c in candNorm)
                            {
                                var key = workshopTitlesByNorm.Keys.FirstOrDefault(t => t.Contains(c, StringComparison.OrdinalIgnoreCase) || c.Contains(t, StringComparison.OrdinalIgnoreCase));
                                if (!string.IsNullOrEmpty(key)) { found = true; foundSource = "workshop"; foundName = workshopTitlesByNorm[key]; break; }
                            }
                        }
                        // 7) Fuzzy: Token-Overlap mit Workshop-Titeln/Dateinamen
                        if (!found)
                        {
                            var candTokenSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            foreach (var c in candNoExt) foreach (var t in Tokenize(c)) candTokenSet.Add(t);
                            // Versuche gegen alle IDs
                            foreach (var id in availableWorkshopDirs)
                            {
                                int best = 0;
                                string? bestName = null;
                                if (workshopTitleTokensById.TryGetValue(id, out var tset) && tset.Count > 0)
                                {
                                    int overlap = candTokenSet.Count == 0 ? 0 : candTokenSet.Intersect(tset).Count();
                                    int minBase = Math.Min(Math.Max(1, candTokenSet.Count), tset.Count);
                                    if (overlap >= 2 && overlap * 2 >= minBase) { best = overlap; bestName = workshopTitlesById.TryGetValue(id, out var t) ? t : id; }
                                }
                                if (best == 0 && workshopFileTokensById.TryGetValue(id, out var fset) && fset.Count > 0)
                                {
                                    int overlap = candTokenSet.Count == 0 ? 0 : candTokenSet.Intersect(fset).Count();
                                    int minBase = Math.Min(Math.Max(1, candTokenSet.Count), fset.Count);
                                    if (overlap >= 2 && overlap * 2 >= minBase) { best = overlap; bestName = workshopTitlesById.TryGetValue(id, out var t) ? t : id; }
                                }
                                if (best > 0)
                                {
                                    found = true; foundSource = "workshop"; foundName = bestName ?? id; break;
                                }
                            }
                            if (!found && !string.IsNullOrWhiteSpace(e.Package) && e.Package.StartsWith("mod_workshop_package.", StringComparison.OrdinalIgnoreCase))
                            {
                                AppendLog($"[Workshop] Fuzzy no match. candTokens: {string.Join(' ', candTokenSet)}");
                            }
                        }

                        if (found)
                        {
                            if (MatchAny(c => availableLocal.Contains(c) || availableLocalNoExt.Contains(Path.GetFileNameWithoutExtension(c) ?? string.Empty)))
                                foundSource = "mod";
                            else if (MatchAny(c => availableWorkshop.Contains(c) || availableWorkshopNoExt.Contains(Path.GetFileNameWithoutExtension(c) ?? string.Empty) || availableWorkshopDirs.Contains(Path.GetFileNameWithoutExtension(c) ?? string.Empty)))
                                foundSource = "workshop";
                        }

                        string statusText = found
                            ? T("MainForm.Mods.Status.Available", "Vorhanden")
                            : T("MainForm.Mods.Status.Missing", "Fehlt");

                        statusCell.Value = statusText;
                        statusCell.ToolTipText = found
                            ? (foundSource == "mod"
                                ? T("MainForm.Mods.Status.AvailableTip", "Mod-Datei wurde gefunden (mod/ oder Workshop)") + " — mod/" + (foundName != null ? (" (" + foundName + ")") : string.Empty)
                                : T("MainForm.Mods.Status.AvailableTip", "Mod-Datei wurde gefunden (mod/ oder Workshop)") + " — Workshop" + (foundName != null ? (" (" + foundName + ")") : string.Empty))
                            : T("MainForm.Mods.Status.MissingTip", "Kein passendes Mod-Archiv gefunden (.scs/.zip)");

                        var style = statusCell.Style ?? new DataGridViewCellStyle();
                        style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        style.ForeColor = found ? System.Drawing.Color.SeaGreen : System.Drawing.Color.IndianRed;
                        statusCell.Style = style;
                    }

                    // Download-Button-Text pro Zeile setzen
                    if (gridMods.Columns.Contains("colDownload"))
                    {
                        var idxCol = gridMods.Columns["colDownload"].Index;
                        var btnCell = row.Cells[idxCol];
                        bool valid = !string.IsNullOrWhiteSpace(url) && Uri.IsWellFormedUriString(url, UriKind.Absolute);
                        btnCell.Value = valid ? "Download" : "-";
                    }

                    // Tooltips für Link-Spalten
                    if (gridMods.Columns.Contains("colDownload"))
                    {
                        var tip = string.IsNullOrWhiteSpace(url)
                            ? T("MainForm.Modlists.Status.NoLinkTip", "Kein Download-Link hinterlegt")
                            : url;
                        row.Cells[gridMods.Columns["colDownload"].Index].ToolTipText = tip;
                    }
                    if (gridMods.Columns.Contains("colSearch"))
                    {
                        var tip = T("MainForm.Modlists.SearchTip", "Nach Mod suchen: ") + (e.ModName ?? "");
                        row.Cells[gridMods.Columns["colSearch"].Index].ToolTipText = tip;
                    }
                }
                catch { }
            }

            if (entries.Count == 0)
                ShowStatus(T("MainForm.Modlists.Empty", "Modliste leer: ") + it.Display);
            else
                ShowStatus(T("MainForm.Modlists.Loaded", "Modliste geladen: ") + it.Display + " (" + entries.Count + ")");

            // Notiz aus <Modlistname>.note laden
            try { LoadCurrentModlistNote(); } catch { }
        }

        // Mods-Ordner ermitteln (Dokumente/<Game>/mod), bevorzugt relativ zum gewählten Profiles-Pfad
        private string? GetModsFolderForGame(string norm)
        {
            try
            {
                var profilesRoot = GetProfilesRootForGame(norm);
                if (!string.IsNullOrWhiteSpace(profilesRoot) && Directory.Exists(profilesRoot))
                {
                    var gameRoot = Directory.GetParent(profilesRoot)?.FullName;
                    if (!string.IsNullOrWhiteSpace(gameRoot))
                    {
                        var mod = Path.Combine(gameRoot, "mod");
                        if (Directory.Exists(mod)) return mod;
                    }
                }

                // Fallback: Standard-Dokumente-Struktur
                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var gameFolder = norm == "ets2" ? "Euro Truck Simulator 2" : "American Truck Simulator";
                var modFolder = Path.Combine(docs, gameFolder, "mod");
                return Directory.Exists(modFolder) ? modFolder : null;
            }
            catch { return null; }
        }

        // mod_workshop_package.<HEXID> -> publishedFileId (dezimal)
        private static string? ExtractWorkshopDecimalId(string? packageName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(packageName)) return null;
                var s = packageName.Trim();
                const string prefix = "mod_workshop_package.";
                if (!s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
                var hex = s.Substring(prefix.Length);
                int dot = hex.IndexOf('|');
                if (dot >= 0) hex = hex.Substring(0, dot);
                // Einige Listen enthalten evtl. zusätzliche Teile – nur Hex-Zeichen behalten
                hex = new string(hex.Where(c => Uri.IsHexDigit(c)).ToArray());
                if (hex.Length == 0) return null;
                if (ulong.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id))
                {
                    return id.ToString(CultureInfo.InvariantCulture);
                }
            }
            catch { }
            return null;
        }

        private static IEnumerable<string> ExtractWorkshopIdCandidates(string? packageName)
        {
            var list = new List<string>();
            try
            {
                var dec = ExtractWorkshopDecimalId(packageName);
                if (!string.IsNullOrEmpty(dec)) list.Add(dec);
                // Versuche Byte-Reverse der Hex-ID (Little-Endian vs Big-Endian)
                const string prefix = "mod_workshop_package.";
                if (!string.IsNullOrWhiteSpace(packageName) && packageName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var hex = packageName.Substring(prefix.Length);
                    int p = hex.IndexOf('|'); if (p >= 0) hex = hex.Substring(0, p);
                    hex = new string(hex.Where(c => Uri.IsHexDigit(c)).ToArray());
                    if (hex.Length >= 2 && hex.Length % 2 == 0)
                    {
                        var bytes = Enumerable.Range(0, hex.Length / 2).Select(i => hex.Substring(i * 2, 2)).ToArray();
                        Array.Reverse(bytes);
                        var revHex = string.Concat(bytes);
                        if (ulong.TryParse(revHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id2))
                            list.Add(id2.ToString(CultureInfo.InvariantCulture));
                    }
                }
            }
            catch { }
            return list;
        }

        private static HashSet<string> Tokenize(string s)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(s)) return set;
            var sb = new StringBuilder();
            void Flush()
            {
                if (sb.Length >= 3)
                {
                    set.Add(sb.ToString().ToLowerInvariant());
                }
                sb.Clear();
            }
            foreach (var ch in s)
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(ch);
                else Flush();
            }
            Flush();
            return set;
        }

        // --- Links Export/Import ---
        private void ExportModlist_Click(object? sender, EventArgs e)
        {
            try
            {
                // Aktuelle Modliste bestimmen
                string? modlistPath = _currentModlistPath;
                string? displayName = null;
                if (string.IsNullOrWhiteSpace(modlistPath) && cbModlist?.SelectedItem is ModlistItem it)
                {
                    modlistPath = it.Path; displayName = it.Display;
                }
                else if (cbModlist?.SelectedItem is ModlistItem it2)
                {
                    displayName = it2.Display;
                }
                if (string.IsNullOrWhiteSpace(modlistPath) || !File.Exists(modlistPath))
                {
                    ShowStatus(T("MainForm.Export.NoList", "Keine Modliste ausgewählt"));
                    return;
                }

                var dir = Path.GetDirectoryName(modlistPath!) ?? string.Empty;
                var baseName = Path.GetFileNameWithoutExtension(modlistPath!) ?? "modlist";

                using var sfd = new SaveFileDialog();
                sfd.Title = T("MainForm.ExportZip.Title", "Modliste als ZIP exportieren");
                sfd.Filter = "ZIP-Archiv (*.zip)|*.zip|Alle Dateien (*.*)|*.*";
                sfd.InitialDirectory = dir;
                sfd.FileName = baseName + ".zip";
                if (sfd.ShowDialog(this) != DialogResult.OK) return;

                var zipPath = sfd.FileName;
                CreateZipForModlist(modlistPath!, zipPath);
                ShowStatus(T("MainForm.ExportZip.Done", "Paket exportiert: ") + Path.GetFileName(zipPath));
            }
            catch (Exception ex)
            {
                ShowStatus(T("MainForm.ExportZip.Failed", "Export fehlgeschlagen: ") + ex.Message);
            }
        }

        private void ImportModlist_Click(object? sender, EventArgs e)
        {
            try
            {
                var code = (cbGame.SelectedItem as GameItem)?.Code ?? "ETS2";
                var norm = NormalizeGameCode(code);
                var root = GetModlistsRootForGame(norm);
                try { Directory.CreateDirectory(root); } catch { }

                using var ofd = new OpenFileDialog();
                ofd.Title = T("MainForm.ImportZip.Title", "Modlisten-Paket importieren");
                ofd.Filter = "Modlisten-Paket (*.zip)|*.zip|Modlisten (*.txt)|*.txt|Alle Dateien (*.*)|*.*";
                ofd.Multiselect = false;
                if (ofd.ShowDialog(this) != DialogResult.OK) return;

                string chosen = ofd.FileName;
                int addedLinks = 0;
                string importedBaseName;

                if (string.Equals(Path.GetExtension(chosen), ".zip", StringComparison.OrdinalIgnoreCase))
                {
                    // ZIP-Import
                    using var temp = new TempFolder();
                    System.IO.Compression.ZipFile.ExtractToDirectory(chosen, temp.Path);

                    // Erwarte mindestens eine .txt – nehme deren Basename als Referenz
                    var txtFiles = Directory.GetFiles(temp.Path, "*.txt", SearchOption.TopDirectoryOnly);
                    if (txtFiles.Length == 0)
                        throw new InvalidOperationException("ZIP enthält keine Modlisten-Textdatei (*.txt)");

                    var srcTxt = txtFiles[0];
                    var baseName = Path.GetFileNameWithoutExtension(srcTxt);
                    importedBaseName = EnsureUniqueBasename(root, baseName);

                    // Pfade im Temp
                    var srcNote = Path.Combine(temp.Path, baseName + ".note");
                    var srcInfo = Path.Combine(temp.Path, baseName + ".json");
                    var srcLink = Path.Combine(temp.Path, baseName + ".link.json");

                    // Zielpfade mit evtl. neuem Basename
                    var destTxt = Path.Combine(root, importedBaseName + ".txt");
                    var destNote = Path.Combine(root, importedBaseName + ".note");
                    var destInfo = Path.Combine(root, importedBaseName + ".json");
                    var destLink = Path.Combine(root, importedBaseName + ".link.json");

                    File.Copy(srcTxt, destTxt, overwrite: false);
                    if (File.Exists(srcNote)) File.Copy(srcNote, destNote, overwrite: false);
                    if (File.Exists(srcInfo)) File.Copy(srcInfo, destInfo, overwrite: false);
                    if (File.Exists(srcLink)) File.Copy(srcLink, destLink, overwrite: false);

                    // Merge importierte per‑Liste Links in globale links.json (ohne Duplikate)
                    if (File.Exists(srcLink))
                    {
                        var importMap = ReadFlatLinksFromJsonFile(srcLink);
                        var globalLinksPath = Path.Combine(root, "links.json");
                        var globalMap = ReadFlatLinksFromJsonFile(globalLinksPath);
                        foreach (var kv in importMap)
                        {
                            if (!globalMap.ContainsKey(kv.Key))
                            {
                                globalMap[kv.Key] = kv.Value; addedLinks++;
                            }
                        }
                        if (addedLinks > 0) WriteFlatLinksToJsonFile(globalLinksPath, globalMap);
                    }

                    // Merge nach globalem links.json
                    if (File.Exists(srcLink))
                    {
                        var importMap = ReadFlatLinksFromJsonFile(srcLink);
                        var globalLinksPath = Path.Combine(root, "links.json");
                        var globalMap = ReadFlatLinksFromJsonFile(globalLinksPath);
                        foreach (var kv in importMap)
                        {
                            if (!globalMap.ContainsKey(kv.Key))
                            {
                                globalMap[kv.Key] = kv.Value; addedLinks++;
                            }
                        }
                        if (addedLinks > 0)
                        {
                            WriteFlatLinksToJsonFile(globalLinksPath, globalMap);
                        }
                    }
                }
                else
                {
                    // Fallback: einzelnes .txt importieren (bestehendes Verhalten)
                    var srcTxt = chosen;
                    var name = Path.GetFileName(srcTxt);
                    var baseName = Path.GetFileNameWithoutExtension(srcTxt);
                    importedBaseName = Path.GetFileNameWithoutExtension(EnsureUniqueFilename(Path.Combine(root, name)));
                    var destTxt = Path.Combine(root, importedBaseName + ".txt");
                    File.Copy(srcTxt, destTxt, overwrite: false);

                    var srcLink = Path.Combine(Path.GetDirectoryName(srcTxt) ?? string.Empty, baseName + ".link.json");
                    var globalLinksPath = Path.Combine(root, "links.json");
                    if (File.Exists(srcLink))
                    {
                        var importMap = ReadFlatLinksFromJsonFile(srcLink);
                        var globalMap = ReadFlatLinksFromJsonFile(globalLinksPath);
                        foreach (var kv in importMap)
                        {
                            if (!globalMap.ContainsKey(kv.Key))
                            {
                                globalMap[kv.Key] = kv.Value; addedLinks++;
                            }
                        }
                        if (addedLinks > 0)
                        {
                            WriteFlatLinksToJsonFile(globalLinksPath, globalMap);
                        }
                        var destLink = Path.Combine(root, importedBaseName + ".link.json");
                        try { if (!File.Exists(destLink)) File.Copy(srcLink, destLink, overwrite: false); } catch { }
                    }
                }

                // Modlisten neu laden und importierte selektieren
                try
                {
                    LoadModlistNamesForSelectedGame();
                    SelectModlistByDisplay(importedBaseName);
                }
                catch { }

                if (addedLinks > 0)
                    ShowStatus(T("MainForm.ImportZip.DoneLinks", "Import abgeschlossen – Links hinzugefügt: ") + addedLinks);
                else
                    ShowStatus(T("MainForm.ImportZip.Done", "Import abgeschlossen"));
            }
            catch (Exception ex)
            {
                ShowStatus(T("MainForm.ImportZip.Failed", "Import fehlgeschlagen: ") + ex.Message);
            }
        }

        private static Dictionary<string, string> ReadFlatLinksFromJsonFile(string path)
        {
            var map = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            try
            {
                if (!File.Exists(path)) return map;
                var json = File.ReadAllText(path);
                var jdOpts = new System.Text.Json.JsonDocumentOptions
                {
                    CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };
                using var doc = System.Text.Json.JsonDocument.Parse(json, jdOpts);
                if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            var val = prop.Value.GetString() ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(val)) map[prop.Name] = val;
                        }
                        else if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object && prop.Value.TryGetProperty("url", out var urlEl) && urlEl.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            var val = urlEl.GetString() ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(val)) map[prop.Name] = val;
                        }
                    }
                }
            }
            catch { }
            return map;
        }

        private static void WriteFlatLinksToJsonFile(string path, Dictionary<string, string> map)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(map, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
            catch { }
        }

        private static string EnsureUniqueFilename(string path)
        {
            try
            {
                if (!File.Exists(path)) return path;
                var dir = Path.GetDirectoryName(path) ?? string.Empty;
                var name = Path.GetFileNameWithoutExtension(path);
                var ext = Path.GetExtension(path);
                for (int i = 1; i < 9999; i++)
                {
                    var candidate = Path.Combine(dir, $"{name} ({i}){ext}");
                    if (!File.Exists(candidate)) return candidate;
                }
                return Path.Combine(dir, $"{name} ({DateTime.Now:yyyyMMdd_HHmmss}){ext}");
            }
            catch { return path; }
        }

        private void SelectModlistByDisplay(string display)
        {
            try
            {
                for (int i = 0; i < cbModlist.Items.Count; i++)
                {
                    if (cbModlist.Items[i] is ModlistItem mi && string.Equals(mi.Display, display, StringComparison.CurrentCultureIgnoreCase))
                    {
                        cbModlist.SelectedIndex = i;
                        return;
                    }
                }
            }
            catch { }
        }

        private static string EnsureUniqueBasename(string root, string baseName)
        {
            try
            {
                string candidate = baseName;
                int i = 1;
                while (File.Exists(Path.Combine(root, candidate + ".txt")))
                {
                    candidate = $"{baseName} ({i++})";
                }
                return candidate;
            }
            catch { return baseName; }
        }

        private sealed class TempFolder : IDisposable
        {
            public string Path { get; }
            public TempFolder()
            {
                var tmpRoot = System.IO.Path.GetTempPath();
                Path = System.IO.Path.Combine(tmpRoot, "ETS2ATS.ModlistManager", System.Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path);
            }
            public void Dispose()
            {
                try { Directory.Delete(Path, recursive: true); } catch { }
            }
        }

        private void CreateZipForModlist(string modlistTxtPath, string zipTarget)
        {
            var dir = System.IO.Path.GetDirectoryName(modlistTxtPath) ?? string.Empty;
            var baseName = System.IO.Path.GetFileNameWithoutExtension(modlistTxtPath) ?? "modlist";

            using var temp = new TempFolder();

            // Zu packende Dateien: .txt, .note, .json, .link.json
            var srcTxt = System.IO.Path.Combine(dir, baseName + ".txt");
            var srcNote = System.IO.Path.Combine(dir, baseName + ".note");
            var srcInfo = System.IO.Path.Combine(dir, baseName + ".json");
            var srcLink = System.IO.Path.Combine(dir, baseName + ".link.json");

            // Immer .txt kopieren (muss existieren)
            File.Copy(srcTxt, System.IO.Path.Combine(temp.Path, System.IO.Path.GetFileName(srcTxt)), overwrite: true);

            // Optional: leere Platzhalter erzeugen, wenn nicht vorhanden
            var tempNote = System.IO.Path.Combine(temp.Path, baseName + ".note");
            var tempInfo = System.IO.Path.Combine(temp.Path, baseName + ".json");
            var tempLink = System.IO.Path.Combine(temp.Path, baseName + ".link.json");

            if (File.Exists(srcNote)) File.Copy(srcNote, tempNote, overwrite: true); else File.WriteAllText(tempNote, string.Empty, new UTF8Encoding(false));
            if (File.Exists(srcInfo)) File.Copy(srcInfo, tempInfo, overwrite: true); else File.WriteAllText(tempInfo, "{}", new UTF8Encoding(false));

            // .link.json für die Modliste erzeugen: aus globalen Links gefiltert nach Modliste und mit per‑Liste Overrides gemerged
            try
            {
                var globalLinksPath = Path.Combine(dir, "links.json");
                var keys = CollectModKeysFromTxt(srcTxt);
                var perListMap = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

                // 1) aus globalen Links relevante Einträge übernehmen
                if (File.Exists(globalLinksPath))
                {
                    var globalMap = ReadFlatLinksFromJsonFile(globalLinksPath);
                    foreach (var k in keys)
                    {
                        if (globalMap.TryGetValue(k, out var url) && !string.IsNullOrWhiteSpace(url))
                        {
                            perListMap[k] = url;
                        }
                    }
                }

                // 2) per‑Liste Overrides (falls vorhanden) anwenden/erweitern
                if (File.Exists(srcLink))
                {
                    var overrides = ReadFlatLinksFromJsonFile(srcLink);
                    foreach (var kv in overrides)
                    {
                        if (keys.Contains(kv.Key))
                            perListMap[kv.Key] = kv.Value;
                    }
                }

                // 3) Schreiben in das temporäre Ziel
                WriteFlatLinksToJsonFile(tempLink, perListMap);
                try { ShowStatus(T("MainForm.ExportZip.LinksCreated", "Links für Modliste erstellt: ") + perListMap.Count); } catch { }
            }
            catch
            {
                // Fallback: falls etwas schiefgeht, leere Datei schreiben
                File.WriteAllText(tempLink, "{}", new UTF8Encoding(false));
            }

            // ZIP schreiben
            try { if (File.Exists(zipTarget)) File.Delete(zipTarget); } catch { }
            System.IO.Compression.ZipFile.CreateFromDirectory(temp.Path, zipTarget, System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: false);
        }

        private static Dictionary<string, ModMeta>? LoadInfoMapFor(string modlistFile)
        {
            try
            {
                var dir = Path.GetDirectoryName(modlistFile);
                if (string.IsNullOrEmpty(dir)) return null;
                var baseName = Path.GetFileNameWithoutExtension(modlistFile);
                var infoPath = Path.Combine(dir, baseName + ".json");
                if (!File.Exists(infoPath)) return new Dictionary<string, ModMeta>(StringComparer.CurrentCultureIgnoreCase);

                var json = File.ReadAllText(infoPath);
                var opts = new System.Text.Json.JsonDocumentOptions
                {
                    CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };
                using var doc = System.Text.Json.JsonDocument.Parse(json, opts);
                var map = new Dictionary<string, ModMeta>(StringComparer.CurrentCultureIgnoreCase);
                if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        // Abwärtskompatibel: String-Wert entspricht Info-Text
                        if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            map[prop.Name] = new ModMeta { Info = prop.Value.GetString() ?? string.Empty };
                        }
                        else if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            string? modName = null;
                            string? info = null;
                            foreach (var inner in prop.Value.EnumerateObject())
                            {
                                var inName = inner.Name;
                                if (inName.Equals("modname", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (inner.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                                        modName = inner.Value.GetString();
                                }
                                else if (inName.Equals("info", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (inner.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                                        info = inner.Value.GetString();
                                }
                            }
                            map[prop.Name] = new ModMeta { ModName = string.IsNullOrWhiteSpace(modName) ? null : modName, Info = string.IsNullOrWhiteSpace(info) ? null : info };
                        }
                    }
                }
                return map;
            }
            catch { return new Dictionary<string, ModMeta>(StringComparer.CurrentCultureIgnoreCase); }
        }

        private static void SaveInfoMapFor(string modlistFile, Dictionary<string, ModMeta> map)
        {
            try
            {
                var dir = Path.GetDirectoryName(modlistFile);
                if (string.IsNullOrEmpty(dir)) return;
                var baseName = Path.GetFileNameWithoutExtension(modlistFile);
                var infoPath = Path.Combine(dir, baseName + ".json");

                // Leere Einträge (kein ModName, keine Info) herausfiltern
                var filtered = map
                    .Where(kv => !string.IsNullOrWhiteSpace(kv.Value?.ModName) || !string.IsNullOrWhiteSpace(kv.Value?.Info))
                    .ToDictionary(kv => kv.Key, kv => kv.Value!, StringComparer.CurrentCultureIgnoreCase);

                var json = System.Text.Json.JsonSerializer.Serialize(filtered, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                File.WriteAllText(infoPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
            catch { }
        }

        // Sammelt die relevanten Keys aus einer Modlisten-.txt: sowohl Package als auch Modname
        private static HashSet<string> CollectModKeysFromTxt(string txtPath)
        {
            var set = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            try
            {
                if (!File.Exists(txtPath)) return set;
                var lines = File.ReadAllLines(txtPath);
                int start = 0;
                if (lines.Length > 0 && int.TryParse(lines[0].Trim(), out _)) start = 1;
                for (int i = start; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var pair = TryParseModlistLine(line);
                    if (!string.IsNullOrWhiteSpace(pair.Package)) set.Add(pair.Package.Trim());
                    if (!string.IsNullOrWhiteSpace(pair.ModName)) set.Add(pair.ModName.Trim());
                }
            }
            catch { }
            return set;
        }

        private static Dictionary<string, string>? LoadLinkMapFor(string modlistFile)
        {
            try
            {
                var dir = Path.GetDirectoryName(modlistFile);
                if (string.IsNullOrEmpty(dir)) return null;

                // 1) Globales links.json im Spiel-Ordner
                var globalLinks = Path.Combine(dir, "links.json");
                // 2) Modlistspezifisch: <Name>.link.json
                var baseName = Path.GetFileNameWithoutExtension(modlistFile);
                var perList = Path.Combine(dir, baseName + ".link.json");

                var map = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

                void Merge(string path)
                {
                    try
                    {
                        if (!File.Exists(path)) return;
                        var json = File.ReadAllText(path);

                        // Toleranter JSON-Parser mit Kommentar-/Komma-Handhabung
                        var jdOpts = new System.Text.Json.JsonDocumentOptions
                        {
                            CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                            AllowTrailingCommas = true
                        };
                        using var doc = System.Text.Json.JsonDocument.Parse(json, jdOpts);
                        if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            foreach (var prop in doc.RootElement.EnumerateObject())
                            {
                                if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                                {
                                    var key = prop.Name;
                                    var val = prop.Value.GetString() ?? string.Empty;
                                    map[key] = val; // perList überschreibt global
                                }
                                else if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object && prop.Value.TryGetProperty("url", out var urlEl) && urlEl.ValueKind == System.Text.Json.JsonValueKind.String)
                                {
                                    var key = prop.Name;
                                    var val = urlEl.GetString() ?? string.Empty;
                                    map[key] = val;
                                }
                            }
                        }
                    }
                    catch { /* ignorieren */ }
                }

                Merge(globalLinks);
                Merge(perList);

                return map.Count > 0 ? map : null;
            }
            catch { return null; }
        }

        // --- Modlisten-Notiz (.note) laden/speichern ---
        private static string GetNotePath(string modlistFile)
        {
            var dir = Path.GetDirectoryName(modlistFile) ?? string.Empty;
            var baseName = Path.GetFileNameWithoutExtension(modlistFile) ?? string.Empty;
            return Path.Combine(dir, baseName + ".note");
        }

        private void LoadCurrentModlistNote()
        {
            var path = _currentModlistPath;
            if (string.IsNullOrWhiteSpace(path)) return;
            var notePath = GetNotePath(path);
            string content = string.Empty;
            try
            {
                if (File.Exists(notePath))
                {
                    content = File.ReadAllText(notePath);
                }
            }
            catch { }

            try
            {
                _loadingModInfo = true;
                txtModInfo.Text = content;
                _lastNoteText = content;
            }
            finally { _loadingModInfo = false; }
        }

        private void TxtModInfo_TextChanged(object? sender, EventArgs e)
        {
            if (_loadingModInfo) return;
            // Debounced Autosave
            _noteSaveTimer ??= new System.Windows.Forms.Timer();
            _noteSaveTimer.Stop();
            _noteSaveTimer.Interval = 800; // 0.8s nach der letzten Eingabe speichern
            _noteSaveTimer.Tick -= NoteSaveTimer_Tick;
            _noteSaveTimer.Tick += NoteSaveTimer_Tick;
            _noteSaveTimer.Start();
        }

        private void NoteSaveTimer_Tick(object? sender, EventArgs e)
        {
            try { _noteSaveTimer?.Stop(); SaveCurrentModlistNoteImmediate(); } catch { }
        }

        private void SaveCurrentModlistNoteImmediate()
        {
            var path = _currentModlistPath;
            if (string.IsNullOrWhiteSpace(path)) return;
            var notePath = GetNotePath(path);
            var text = txtModInfo.Text ?? string.Empty;
            // nur schreiben, wenn sich etwas geändert hat
            if (string.Equals(text, _lastNoteText, StringComparison.Ordinal)) return;
            try
            {
                File.WriteAllText(notePath, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                _lastNoteText = text;
                ShowStatus(T("MainForm.Note.Saved", "Notiz gespeichert"));
            }
            catch { }
        }

        private static (string Package, string ModName) TryParseModlistLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return (string.Empty, string.Empty);

            // 1) Extrahiere ggf. den Inhalt in Anführungszeichen: active_mods[0]: "pkg|mod"
            var s = line.Trim();
            int q1 = s.IndexOf('"');
            int q2 = s.LastIndexOf('"');
            if (q1 >= 0 && q2 > q1)
            {
                s = s.Substring(q1 + 1, q2 - q1 - 1);
            }

            s = s.Trim();

            // 2) Bevorzugt '|' als Trennzeichen (pkg|modname)
            int pipe = s.IndexOf('|');
            if (pipe > 0)
            {
                var left = s.Substring(0, pipe).Trim().Trim('"');
                var right = s.Substring(pipe + 1).Trim().Trim('"');
                return (left, right);
            }

            // 3) Legacy-Fallbacks: ';' / Tab / " - "
            static (string, string)? SplitBy(string str, string sep)
            {
                int p = str.IndexOf(sep, StringComparison.Ordinal);
                if (p <= 0) return null;
                var left = str.Substring(0, p).Trim();
                var right = str.Substring(p + sep.Length).Trim();
                if (left.Length == 0 && right.Length == 0) return null;
                return (left, right);
            }

            (string, string)? r = null;
            r ??= SplitBy(s, ";");
            r ??= SplitBy(s, "\t");
            r ??= SplitBy(s, " - ");

            if (r is not null)
            {
                var left = r.Value.Item1.Trim('"');
                var right = r.Value.Item2.Trim('"');
                return (left, right);
            }

            // 4) Kein erkennbarer Trenner: verwende die gesamte Zeile für beide Spalten
            return (s.Trim('"'), s.Trim('"'));
        }

        private void ClearModsGrid()
        {
            try { gridMods.Rows.Clear(); } catch { }
        }

        // Robust: Menü- und Button-Wiring für den Optionsdialog
        private void WireUiEvents()
        {
            if (IsDesignTime) return;

            // Alias für erwarteten Parameter-Namen
            var menuStrip1 = this.menuMain;

            // Nur den Menüeintrag „Optionen…“ verdrahten
            var optionsItem = TryFindMenuItemByName(menuStrip1, "miOptsOpen");
            if (optionsItem is not null)
            {
                optionsItem.Click -= OpenOptionsDialog_Click;
                optionsItem.Click += OpenOptionsDialog_Click;
            }

            // Quick-Button: Text-Check auf Menü-Logik routen, falls vorhanden
            if (btnTextCheck != null)
            {
                btnTextCheck.Click -= TextCheck_Click;
                btnTextCheck.Click += TextCheck_Click;
            }

            // Quick-Button: Modliste erstellen aus profile.sii
            if (btnCreate != null)
            {
                btnCreate.Click -= CreateModlistFromProfile_Click;
                btnCreate.Click += CreateModlistFromProfile_Click;
            }

            // Quick-Button: Modliste übernehmen → active_mods in profile.sii ersetzen
            if (btnAdopt != null)
            {
                btnAdopt.Click -= AdoptModlistToProfile_Click;
                btnAdopt.Click += AdoptModlistToProfile_Click;
            }

            // Menüpunkt Text-Check ebenfalls verdrahten, wenn vorhanden
            var textCheckItem = TryFindMenuItemByName(menuStrip1, "miModText") as ToolStripMenuItem;
            if (textCheckItem is not null)
            {
                textCheckItem.Click -= TextCheck_Click;
                textCheckItem.Click += TextCheck_Click;
            }
            // Hilfe -> Über…
            var aboutItem = TryFindMenuItemByName(menuStrip1, "miAbout");
            if (aboutItem is not null)
            {
                aboutItem.Click -= OpenAboutDialog_Click;
                aboutItem.Click += OpenAboutDialog_Click;
            }

            // Hilfe -> Donate (Ko-fi)
            var donateItem = TryFindMenuItemByName(menuStrip1, "miDonate");
            if (donateItem is not null)
            {
                donateItem.Click -= OpenDonateLink_Click;
                donateItem.Click += OpenDonateLink_Click;
            }

            // Profile -> Profilordner öffnen
            var openProfileFolderItem = TryFindMenuItemByName(menuStrip1, "miProfOpen");
            if (openProfileFolderItem is not null)
            {
                openProfileFolderItem.Click -= OpenSelectedProfileFolder_Click;
                openProfileFolderItem.Click += OpenSelectedProfileFolder_Click;
            }

            // Profile -> Klonen/Umbenennen/Löschen
            var profCloneItem = TryFindMenuItemByName(menuStrip1, "miProfClone");
            if (profCloneItem is not null)
            {
                profCloneItem.Click -= CloneSelectedProfile_Click;
                profCloneItem.Click += CloneSelectedProfile_Click;
            }

            var profRenameItem = TryFindMenuItemByName(menuStrip1, "miProfRename");
            if (profRenameItem is not null)
            {
                profRenameItem.Click -= RenameSelectedProfile_Click;
                profRenameItem.Click += RenameSelectedProfile_Click;
            }

            var profDeleteItem = TryFindMenuItemByName(menuStrip1, "miProfDelete");
            if (profDeleteItem is not null)
            {
                profDeleteItem.Click -= DeleteSelectedProfile_Click;
                profDeleteItem.Click += DeleteSelectedProfile_Click;
            }

            // Backup -> profile.sii wiederherstellen…
            var restoreSiiItem = TryFindMenuItemByName(menuStrip1, "miBkSii");
            if (restoreSiiItem is not null)
            {
                restoreSiiItem.Click -= RestoreProfileSiiFromBackup_Click;
                restoreSiiItem.Click += RestoreProfileSiiFromBackup_Click;
            }

            // Backup -> Alle Profile sichern…
            var backupAllItem = TryFindMenuItemByName(menuStrip1, "miBkAll");
            if (backupAllItem is not null)
            {
                backupAllItem.Click -= BackupAllProfiles_Click;
                backupAllItem.Click += BackupAllProfiles_Click;
            }

            // Backup -> Profile wiederherstellen…
            var restoreProfilesItem = TryFindMenuItemByName(menuStrip1, "miBkRestore");
            if (restoreProfilesItem is not null)
            {
                restoreProfilesItem.Click -= RestoreProfilesFromBackup_Click;
                restoreProfilesItem.Click += RestoreProfilesFromBackup_Click;
            }

            // Modlisten -> Ordner öffnen
            var openModlistsFolderItem = TryFindMenuItemByName(menuStrip1, "miModOpen");
            if (openModlistsFolderItem is not null)
            {
                openModlistsFolderItem.Click -= OpenModlistsFolder_Click;
                openModlistsFolderItem.Click += OpenModlistsFolder_Click;
            }

            // Modlisten -> Löschen…
            var deleteModlistItem = TryFindMenuItemByName(menuStrip1, "miModDelete");
            if (deleteModlistItem is not null)
            {
                deleteModlistItem.Click -= DeleteModlist_Click;
                deleteModlistItem.Click += DeleteModlist_Click;
            }

            // Modlisten -> Weitergeben… (Export)
            var shareModlistItem = TryFindMenuItemByName(menuStrip1, "miModShare");
            if (shareModlistItem is not null)
            {
                shareModlistItem.Click -= ExportModlist_Click;
                shareModlistItem.Click += ExportModlist_Click;
            }

            // Modlisten -> Importieren…
            var importModlistItem = TryFindMenuItemByName(menuStrip1, "miModImport");
            if (importModlistItem is not null)
            {
                importModlistItem.Click -= ImportModlist_Click;
                importModlistItem.Click += ImportModlist_Click;
            }

            // Optional: Dynamischer Menüpunkt zum Setzen des Modlisten-Ordners pro Spiel
            try
            {
                var miModlists = TryFindMenuItemByName(menuStrip1, "miModlists") as ToolStripMenuItem;
                if (miModlists != null)
                {
                    // bereits vorhanden? Dann neu anhängen, falls nicht da
                    var existing = miModlists.DropDownItems.Cast<ToolStripItem>().FirstOrDefault(i => string.Equals(i.Name, "miModSetFolder", StringComparison.OrdinalIgnoreCase));
                    if (existing == null)
                    {
                        var miSet = new ToolStripMenuItem { Name = "miModSetFolder", Tag = "MainForm.Menu.Modlists.SetFolder", Text = T("MainForm.Menu.Modlists.SetFolder", "Modlistenordner wählen…") };
                        miSet.Click += (_, __) => ChooseAndSaveModlistsFolderForCurrentGame();
                        miModlists.DropDownItems.Insert(0, miSet);
                    }
                    else
                    {
                        // Stelle sicher, dass Tag und Text korrekt sind (für Lokalisierung und konsistente Anzeige)
                        existing.Tag = "MainForm.Menu.Modlists.SetFolder";
                        existing.Text = T("MainForm.Menu.Modlists.SetFolder", "Modlistenordner wählen…");
                    }
                }
            }
            catch { }

            // Footer: Undo-Link verdrahten
            if (linkUndo != null)
            {
                linkUndo.LinkClicked -= LinkUndo_LinkClicked;
                linkUndo.LinkClicked += LinkUndo_LinkClicked;
            }

            // Grid-Kontextmenü: Events
            if (miAddLink != null) { miAddLink.Click -= MiAddLink_Click; miAddLink.Click += MiAddLink_Click; }
            if (miRemoveLink != null) { miRemoveLink.Click -= MiRemoveLink_Click; miRemoveLink.Click += MiRemoveLink_Click; }
            if (miAddLinkPerList != null) { miAddLinkPerList.Click -= MiAddLinkPerList_Click; miAddLinkPerList.Click += MiAddLinkPerList_Click; }
            if (miRemoveLinkPerList != null) { miRemoveLinkPerList.Click -= MiRemoveLinkPerList_Click; miRemoveLinkPerList.Click += MiRemoveLinkPerList_Click; }
        }

        private void ChooseAndSaveModlistsFolderForCurrentGame()
        {
            try
            {
                var code = (cbGame.SelectedItem as GameItem)?.Code ?? "ETS2";
                var norm = NormalizeGameCode(code);
                using var fbd = new FolderBrowserDialog();
                fbd.Description = norm == "ets2" ? T("MainForm.Choose.Modlists.Ets2", "Zielordner für ETS2-Modlisten wählen") : T("MainForm.Choose.Modlists.Ats", "Zielordner für ATS-Modlisten wählen");
                var current = GetModlistsRootForGame(norm);
                if (!string.IsNullOrWhiteSpace(current) && Directory.Exists(current)) fbd.SelectedPath = current;
                if (fbd.ShowDialog(this) != DialogResult.OK) return;

                var chosen = fbd.SelectedPath;
                if (norm == "ets2") _settings.Current.Ets2ModlistsPath = chosen; else _settings.Current.AtsModlistsPath = chosen;
                _settings.Save();

                // Ordner anlegen und initial befüllen (nur wenn leer)
                try { Directory.CreateDirectory(chosen); } catch { }
                EnsureModlistsDirectoryForGame(norm);

                // UI aktualisieren
                LoadModlistNamesForSelectedGame();
            }
            catch { }
        }

        private void GridMods_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                try
                {
                    gridMods.ClearSelection();
                    gridMods.Rows[e.RowIndex].Selected = true;
                    gridMods.CurrentCell = gridMods.Rows[e.RowIndex].Cells[Math.Max(0, e.ColumnIndex)];
                }
                catch { }
            }
        }

        private void MiAddLink_Click(object? sender, EventArgs e)
        {
            try
            {
                if (gridMods.SelectedRows.Count == 0) return;
                var row = gridMods.SelectedRows[0];
                var modName = row.Cells[gridMods.Columns["colModName"].Index].Value?.ToString() ?? string.Empty;
                var pkg = row.Cells[gridMods.Columns["colPackage"].Index].Value?.ToString() ?? string.Empty;
                var key = !string.IsNullOrWhiteSpace(modName) ? modName : pkg;
                if (string.IsNullOrWhiteSpace(key)) return;

                // Vorschlag: vorhandene URL aus der hidden colUrl
                var currentUrl = row.Cells[gridMods.Columns["colUrl"].Index].Value?.ToString() ?? string.Empty;
                var title = T("MainForm.Grid.AddLink", "Download-Link hinzufügen");
                var label = T("MainForm.Prompt.Url", "URL:");
                var url = PromptForText(this, title, label, currentUrl, limit20: false);
                if (string.IsNullOrWhiteSpace(url)) return;

                url = NormalizeUrl(url) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    ShowStatus(T("MainForm.Grid.InvalidUrl", "Ungültige URL"));
                    return;
                }

                // In globales links.json der aktuellen Spiel-Modlists schreiben
                var code = (cbGame.SelectedItem as GameItem)?.Code ?? "ETS2";
                var norm = NormalizeGameCode(code);
                var root = GetModlistsRootForGame(norm);
                var globalLinksPath = Path.Combine(root, "links.json");
                var map = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
                try
                {
                    if (File.Exists(globalLinksPath))
                    {
                        var json = File.ReadAllText(globalLinksPath);
                        var jdOpts = new System.Text.Json.JsonDocumentOptions { CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true };
                        using var doc = System.Text.Json.JsonDocument.Parse(json, jdOpts);
                        if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            foreach (var prop in doc.RootElement.EnumerateObject())
                            {
                                if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                                    map[prop.Name] = prop.Value.GetString() ?? string.Empty;
                                else if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object && prop.Value.TryGetProperty("url", out var urlEl) && urlEl.ValueKind == System.Text.Json.JsonValueKind.String)
                                    map[prop.Name] = urlEl.GetString() ?? string.Empty;
                            }
                        }
                    }
                }
                catch { }

                map[key] = url;

                try
                {
                    var jsonOut = System.Text.Json.JsonSerializer.Serialize(map, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });
                    File.WriteAllText(globalLinksPath, jsonOut, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                    ShowStatus(T("MainForm.Grid.LinkSaved", "Link gespeichert") + ": " + key);
                }
                catch (Exception ex)
                {
                    ShowStatus(T("MainForm.Grid.LinkSaveFailed", "Link speichern fehlgeschlagen: ") + ex.Message);
                }

                // Grid aktualisieren, Auswahl beibehalten
                var selectedIndex = row.Index;
                LoadSelectedModlistIntoGrid();
                try { if (selectedIndex >= 0 && selectedIndex < gridMods.Rows.Count) gridMods.Rows[selectedIndex].Selected = true; } catch { }
            }
            catch { }
        }

        private void MiRemoveLink_Click(object? sender, EventArgs e)
        {
            try
            {
                if (gridMods.SelectedRows.Count == 0) return;
                var row = gridMods.SelectedRows[0];
                var modName = row.Cells[gridMods.Columns["colModName"].Index].Value?.ToString() ?? string.Empty;
                var pkg = row.Cells[gridMods.Columns["colPackage"].Index].Value?.ToString() ?? string.Empty;
                var key = !string.IsNullOrWhiteSpace(modName) ? modName : pkg;
                if (string.IsNullOrWhiteSpace(key)) return;

                var code = (cbGame.SelectedItem as GameItem)?.Code ?? "ETS2";
                var norm = NormalizeGameCode(code);
                var root = GetModlistsRootForGame(norm);
                var globalLinksPath = Path.Combine(root, "links.json");
                if (!File.Exists(globalLinksPath)) { ShowStatus(T("MainForm.Grid.NoLinks", "Kein links.json gefunden")); return; }

                Dictionary<string, string> map;
                try
                {
                    var json = File.ReadAllText(globalLinksPath);
                    var jdOpts = new System.Text.Json.JsonDocumentOptions { CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true };
                    using var doc = System.Text.Json.JsonDocument.Parse(json, jdOpts);
                    map = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
                    if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                                map[prop.Name] = prop.Value.GetString() ?? string.Empty;
                            else if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object && prop.Value.TryGetProperty("url", out var urlEl) && urlEl.ValueKind == System.Text.Json.JsonValueKind.String)
                                map[prop.Name] = urlEl.GetString() ?? string.Empty;
                        }
                    }
                }
                catch { ShowStatus(T("MainForm.Grid.NoLinks", "Kein links.json gefunden")); return; }

                if (map.Remove(key))
                {
                    try
                    {
                        var jsonOut = System.Text.Json.JsonSerializer.Serialize(map, new System.Text.Json.JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                        File.WriteAllText(globalLinksPath, jsonOut, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                        ShowStatus(T("MainForm.Grid.LinkRemoved", "Link entfernt") + ": " + key);
                    }
                    catch (Exception ex)
                    {
                        ShowStatus(T("MainForm.Grid.LinkRemoveFailed", "Link entfernen fehlgeschlagen: ") + ex.Message);
                    }
                }
                else
                {
                    ShowStatus(T("MainForm.Grid.LinkNotFound", "Kein Link für diesen Eintrag gefunden"));
                }

                var selectedIndex = row.Index;
                LoadSelectedModlistIntoGrid();
                try { if (selectedIndex >= 0 && selectedIndex < gridMods.Rows.Count) gridMods.Rows[selectedIndex].Selected = true; } catch { }
            }
            catch { }
        }

        private void MiAddLinkPerList_Click(object? sender, EventArgs e)
        {
            try
            {
                if (gridMods.SelectedRows.Count == 0) return;
                if (_currentModlistPath == null || !File.Exists(_currentModlistPath)) return;
                var row = gridMods.SelectedRows[0];
                var modName = row.Cells[gridMods.Columns["colModName"].Index].Value?.ToString() ?? string.Empty;
                var pkg = row.Cells[gridMods.Columns["colPackage"].Index].Value?.ToString() ?? string.Empty;
                var key = !string.IsNullOrWhiteSpace(modName) ? modName : pkg;
                if (string.IsNullOrWhiteSpace(key)) return;

                var dir = Path.GetDirectoryName(_currentModlistPath)!;
                var baseName = Path.GetFileNameWithoutExtension(_currentModlistPath)!;
                var perListPath = Path.Combine(dir, baseName + ".link.json");

                // vorhandene URL vorschlagen
                var currentUrl = row.Cells[gridMods.Columns["colUrl"].Index].Value?.ToString() ?? string.Empty;
                var title = T("MainForm.Grid.AddLinkPerList", "In Modlisten-Links speichern");
                var label = T("MainForm.Prompt.Url", "URL:");
                var url = PromptForText(this, title, label, currentUrl, limit20: false);
                if (string.IsNullOrWhiteSpace(url)) return;
                url = NormalizeUrl(url) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    ShowStatus(T("MainForm.Grid.InvalidUrl", "Ungültige URL"));
                    return;
                }

                var map = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
                try
                {
                    if (File.Exists(perListPath))
                    {
                        var json = File.ReadAllText(perListPath);
                        var opts = new System.Text.Json.JsonDocumentOptions { CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true };
                        using var doc = System.Text.Json.JsonDocument.Parse(json, opts);
                        if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            foreach (var prop in doc.RootElement.EnumerateObject())
                            {
                                if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                                    map[prop.Name] = prop.Value.GetString() ?? string.Empty;
                                else if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object && prop.Value.TryGetProperty("url", out var urlEl) && urlEl.ValueKind == System.Text.Json.JsonValueKind.String)
                                    map[prop.Name] = urlEl.GetString() ?? string.Empty;
                            }
                        }
                    }
                }
                catch { }

                map[key] = url;

                try
                {
                    var jsonOut = System.Text.Json.JsonSerializer.Serialize(map, new System.Text.Json.JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                    File.WriteAllText(perListPath, jsonOut, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                    ShowStatus(T("MainForm.Grid.LinkSaved", "Link gespeichert") + ": " + key);
                }
                catch (Exception ex)
                {
                    ShowStatus(T("MainForm.Grid.LinkSaveFailed", "Link speichern fehlgeschlagen: ") + ex.Message);
                }

                var selectedIndex = row.Index;
                LoadSelectedModlistIntoGrid();
                try { if (selectedIndex >= 0 && selectedIndex < gridMods.Rows.Count) gridMods.Rows[selectedIndex].Selected = true; } catch { }
            }
            catch { }
        }

        private void MiRemoveLinkPerList_Click(object? sender, EventArgs e)
        {
            try
            {
                if (gridMods.SelectedRows.Count == 0) return;
                if (_currentModlistPath == null || !File.Exists(_currentModlistPath)) return;
                var row = gridMods.SelectedRows[0];
                var modName = row.Cells[gridMods.Columns["colModName"].Index].Value?.ToString() ?? string.Empty;
                var pkg = row.Cells[gridMods.Columns["colPackage"].Index].Value?.ToString() ?? string.Empty;
                var key = !string.IsNullOrWhiteSpace(modName) ? modName : pkg;
                if (string.IsNullOrWhiteSpace(key)) return;

                var dir = Path.GetDirectoryName(_currentModlistPath)!;
                var baseName = Path.GetFileNameWithoutExtension(_currentModlistPath)!;
                var perListPath = Path.Combine(dir, baseName + ".link.json");
                if (!File.Exists(perListPath)) { ShowStatus(T("MainForm.Grid.NoLinks", "Kein links.json gefunden")); return; }

                Dictionary<string, string> map;
                try
                {
                    var json = File.ReadAllText(perListPath);
                    var opts = new System.Text.Json.JsonDocumentOptions { CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true };
                    using var doc = System.Text.Json.JsonDocument.Parse(json, opts);
                    map = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
                    if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                                map[prop.Name] = prop.Value.GetString() ?? string.Empty;
                            else if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object && prop.Value.TryGetProperty("url", out var urlEl) && urlEl.ValueKind == System.Text.Json.JsonValueKind.String)
                                map[prop.Name] = urlEl.GetString() ?? string.Empty;
                        }
                    }
                }
                catch { ShowStatus(T("MainForm.Grid.NoLinks", "Kein links.json gefunden")); return; }

                if (map.Remove(key))
                {
                    try
                    {
                        var jsonOut = System.Text.Json.JsonSerializer.Serialize(map, new System.Text.Json.JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                        File.WriteAllText(perListPath, jsonOut, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                        ShowStatus(T("MainForm.Grid.LinkRemoved", "Link entfernt") + ": " + key);
                    }
                    catch (Exception ex)
                    {
                        ShowStatus(T("MainForm.Grid.LinkRemoveFailed", "Link entfernen fehlgeschlagen: ") + ex.Message);
                    }
                }
                else
                {
                    ShowStatus(T("MainForm.Grid.LinkNotFound", "Kein Link für diesen Eintrag gefunden"));
                }

                var selectedIndex = row.Index;
                LoadSelectedModlistIntoGrid();
                try { if (selectedIndex >= 0 && selectedIndex < gridMods.Rows.Count) gridMods.Rows[selectedIndex].Selected = true; } catch { }
            }
            catch { }
        }

        private void TextCheck_Click(object? sender, EventArgs e)
        {
            ExecuteTextCheck();
        }

        private void ExecuteTextCheck()
        {
            // Öffnet profile.sii als Text, wenn möglich; sonst versucht zu entschlüsseln
            if (cbProfile?.SelectedItem is not ProfileItem p)
                return;

            var sii = FindProfileSiiPath(p.Directory);
            if (sii == null || !File.Exists(sii)) { ShowStatus("profile.sii nicht gefunden"); return; }

            if (!IsProbablyTextFile(sii))
            {
                // Versuch der Entschlüsselung on-demand
                TryDecryptProfileFile(sii);
            }

            if (IsProbablyTextFile(sii))
            {
                // In Notepad anzeigen
                var arg = $"\"{sii}\"";
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("notepad.exe", arg) { UseShellExecute = true }); } catch { }
                ShowStatus("profile.sii geöffnet");
            }
            else
            {
                ShowStatus("Binärdatei – Entschlüsselung fehlgeschlagen");
            }
        }

        // --- Undo-Link Animation & Helpers ---
        private void StartUndoFadeIn()
        {
            if (linkUndo == null) return;
            try
            {
                linkUndo.Visible = true;
                linkUndo.LinkColor = _undoStartColor;
                linkUndo.ActiveLinkColor = _undoStartColor;
                linkUndo.VisitedLinkColor = _undoStartColor;
                _undoAnimStep = 0;
                _undoAnimDir = +1;
                _undoAnimTimer?.Start();
            }
            catch { linkUndo.Visible = true; }
        }

        private void StartUndoFadeOut(bool hideAtEnd)
        {
            if (linkUndo == null) return;
            try
            {
                linkUndo.Tag = hideAtEnd ? linkUndo.Tag : linkUndo.Tag; // keep tag
                _undoAnimStep = UNDO_ANIM_STEPS;
                _undoAnimDir = -1;
                _undoAnimTimer?.Start();
            }
            catch { if (hideAtEnd) linkUndo.Visible = false; }
        }

        private void UndoAnimTimer_Tick(object? sender, EventArgs e)
        {
            if (linkUndo == null || _undoAnimDir == 0)
            {
                _undoAnimTimer?.Stop();
                return;
            }

            _undoAnimStep += _undoAnimDir;
            if (_undoAnimStep <= 0)
            {
                _undoAnimStep = 0;
                _undoAnimDir = 0;
                _undoAnimTimer?.Stop();
                linkUndo.LinkColor = _undoStartColor;
                linkUndo.ActiveLinkColor = _undoStartColor;
                linkUndo.VisitedLinkColor = _undoStartColor;
                // Beim Ausblenden endgültig verstecken
                linkUndo.Visible = false;
                return;
            }
            if (_undoAnimStep >= UNDO_ANIM_STEPS)
            {
                _undoAnimStep = UNDO_ANIM_STEPS;
                _undoAnimDir = 0;
                _undoAnimTimer?.Stop();
                linkUndo.LinkColor = _undoEndColor;
                linkUndo.ActiveLinkColor = _undoEndColor;
                linkUndo.VisitedLinkColor = _undoEndColor;
                return;
            }

            // Interpolation
            float t = (float)_undoAnimStep / UNDO_ANIM_STEPS;
            var c = LerpColor(_undoStartColor, _undoEndColor, t);
            linkUndo.LinkColor = c;
            linkUndo.ActiveLinkColor = c;
            linkUndo.VisitedLinkColor = c;
        }

        private static Color LerpColor(Color a, Color b, float t)
        {
            if (t <= 0) return a;
            if (t >= 1) return b;
            int r = a.R + (int)Math.Round((b.R - a.R) * t);
            int g = a.G + (int)Math.Round((b.G - a.G) * t);
            int bl = a.B + (int)Math.Round((b.B - a.B) * t);
            return Color.FromArgb(r, g, bl);
        }

        private void OpenAboutDialog_Click(object? sender, EventArgs e)
        {
            using var dlg = new ETS2ATS.ModlistManager.Forms.About.AboutForm(_settings);
            dlg.ShowDialog(this);
        }

        private void OpenDonateLink_Click(object? sender, EventArgs e)
        {
            var url = "https://ko-fi.com/rore58";
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url)
                {
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void DeleteModlist_Click(object? sender, EventArgs e)
        {
            try
            {
                if (cbModlist?.SelectedItem is not ModlistItem it || string.IsNullOrWhiteSpace(it.Path) || !File.Exists(it.Path))
                {
                    ShowStatus(T("MainForm.Delete.NoList", "Keine Modliste ausgewählt"));
                    return;
                }

                var dir = Path.GetDirectoryName(it.Path) ?? string.Empty;
                var baseName = Path.GetFileNameWithoutExtension(it.Path) ?? string.Empty;
                var files = new[]
                {
                    Path.Combine(dir, baseName + ".txt"),
                    Path.Combine(dir, baseName + ".note"),
                    Path.Combine(dir, baseName + ".json"),
                    Path.Combine(dir, baseName + ".link.json"),
                };

                var title = T("MainForm.Delete.Title", "Modliste löschen");
                var question = T("MainForm.Delete.Question", "Diese Modliste und alle zugehörigen Dateien löschen?");
                var res = MessageBox.Show(this, question, title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (res != DialogResult.Yes) return;

                int deleted = 0;
                foreach (var f in files)
                {
                    try { if (File.Exists(f)) { File.Delete(f); deleted++; } } catch { }
                }

                // UI aktualisieren
                try { LoadModlistNamesForSelectedGame(); } catch { }
                ShowStatus(T("MainForm.Delete.Done", "Modliste gelöscht") + (deleted > 0 ? $" ({deleted})" : string.Empty));
            }
            catch (Exception ex)
            {
                ShowStatus(T("MainForm.Delete.Failed", "Löschen fehlgeschlagen: ") + ex.Message);
            }
        }

        private void CreateModlistFromProfile_Click(object? sender, EventArgs e)
        {
            try
            {
                // Profil prüfen
                if (cbProfile?.SelectedItem is not ProfileItem p || string.IsNullOrWhiteSpace(p.Directory) || !Directory.Exists(p.Directory))
                {
                    ShowStatus(T("MainForm.Create.NoProfile", "Kein Profil ausgewählt"));
                    return;
                }

                var sii = FindProfileSiiPath(p.Directory);
                if (sii == null || !File.Exists(sii))
                {
                    ShowStatus(T("MainForm.Create.SiiNotFound", "profile.sii nicht gefunden"));
                    return;
                }

                // Sicherstellen, dass Text vorliegt – ggf. entschlüsseln
                if (!IsProbablyTextFile(sii))
                {
                    if (!TryDecryptProfileFile(sii) || !IsProbablyTextFile(sii))
                    {
                        ShowStatus(T("MainForm.Create.DecryptFailed", "profile.sii konnte nicht entschlüsselt werden"));
                        return;
                    }
                }

                // active_mods Block 1:1 sammeln
                var lines = File.ReadAllLines(sii);
                var block = new List<string>();
                foreach (var raw in lines)
                {
                    var l = raw ?? string.Empty;
                    var t = l.TrimStart();
                    if (t.StartsWith("active_mods", StringComparison.Ordinal))
                    {
                        block.Add(l); // unverändert übernehmen
                    }
                }
                if (block.Count == 0)
                {
                    ShowStatus(T("MainForm.Create.NoActiveMods", "Kein active_mods-Block gefunden"));
                    return;
                }

                // Zielordner = Modlistenordner für aktuelles Spiel
                var code = (cbGame.SelectedItem as GameItem)?.Code ?? "ETS2";
                var norm = NormalizeGameCode(code);
                var root = GetModlistsRootForGame(norm);
                try { Directory.CreateDirectory(root); } catch { }

                // Name vorschlagen = Profilanzeige
                var suggested = SanitizeAsFileName(p.Display);
                if (string.IsNullOrWhiteSpace(suggested)) suggested = $"modlist_{DateTime.Now:yyyyMMdd-HHmm}";

                var name = PromptForText(this, T("MainForm.Toolbar.Create", "Modliste erstellen"), T("MainForm.Prompt.Name", "Name:"), suggested, limit20: false);
                if (string.IsNullOrWhiteSpace(name)) return;
                var requestedBase = SanitizeAsFileName(name!);
                var baseName = requestedBase;

                // Überschreib-Abfrage, falls Dateien existieren
                if (File.Exists(Path.Combine(root, requestedBase + ".txt")) ||
                    File.Exists(Path.Combine(root, requestedBase + ".note")) ||
                    File.Exists(Path.Combine(root, requestedBase + ".json")) ||
                    File.Exists(Path.Combine(root, requestedBase + ".link.json")))
                {
                    var cap = T("MainForm.Create.OverwriteTitle", "Modliste erstellen");
                    var msg = T("MainForm.Create.OverwriteQuestion", "Dateien mit diesem Namen existieren bereits. Überschreiben?");
                    var choice = MessageBox.Show(this, msg, cap, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button3);
                    if (choice == DialogResult.Cancel) return;
                    if (choice == DialogResult.No)
                    {
                        baseName = EnsureUniqueBasename(root, requestedBase);
                    }
                    // Yes → baseName bleibt requestedBase und überschreibt
                }

                // Dateien schreiben: .txt (Block), .note (leer), .json ({}), .link.json ({})
                var pathTxt = Path.Combine(root, baseName + ".txt");
                var pathNote = Path.Combine(root, baseName + ".note");
                var pathInfo = Path.Combine(root, baseName + ".json");
                var pathLink = Path.Combine(root, baseName + ".link.json");

                // Schreiben (bei Überschreiben explizit true)
                File.WriteAllLines(pathTxt, block, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                File.WriteAllText(pathNote, string.Empty, new UTF8Encoding(false));
                File.WriteAllText(pathInfo, "{}", new UTF8Encoding(false));
                File.WriteAllText(pathLink, "{}", new UTF8Encoding(false));

                // Modlisten neu laden und selektieren
                try { LoadModlistNamesForSelectedGame(); SelectModlistByDisplay(baseName); } catch { }

                ShowStatus(T("MainForm.Create.Done", "Modliste erstellt: ") + baseName);
            }
            catch (Exception ex)
            {
                ShowStatus(T("MainForm.Create.Failed", "Erstellung fehlgeschlagen: ") + ex.Message);
            }
        }

        private void AdoptModlistToProfile_Click(object? sender, EventArgs e)
        {
            try
            {
                // Validierung: Profil & Modliste
                if (cbProfile?.SelectedItem is not ProfileItem p || string.IsNullOrWhiteSpace(p.Directory) || !Directory.Exists(p.Directory))
                {
                    ShowStatus(T("MainForm.Adopt.NoProfile", "Kein Profil ausgewählt"));
                    return;
                }
                if (cbModlist?.SelectedItem is not ModlistItem it || string.IsNullOrWhiteSpace(it.Path) || !File.Exists(it.Path))
                {
                    ShowStatus(T("MainForm.Adopt.NoList", "Keine Modliste ausgewählt"));
                    return;
                }

                var sii = FindProfileSiiPath(p.Directory);
                if (sii == null || !File.Exists(sii))
                {
                    ShowStatus(T("MainForm.Adopt.SiiNotFound", "profile.sii nicht gefunden"));
                    return;
                }

                // Bestätigung (optional je nach Einstellung)
                if (_settings.Current.ConfirmBeforeAdopt)
                {
                    var cap = T("MainForm.Adopt.ConfirmTitle", "Modliste übernehmen");
                    var msg = T("MainForm.Adopt.ConfirmQuestion", "active_mods in profile.sii ersetzen?");
                    var confirm = MessageBox.Show(this, msg, cap, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                    if (confirm != DialogResult.Yes) return;
                }

                // Sicherstellen, dass profile.sii als Text vorliegt – ggf. entschlüsseln
                if (!IsProbablyTextFile(sii))
                {
                    if (!TryDecryptProfileFile(sii) || !IsProbablyTextFile(sii))
                    {
                        ShowStatus(T("MainForm.Adopt.DecryptFailed", "profile.sii konnte nicht entschlüsselt werden"));
                        return;
                    }
                }

                // Neue active_mods-Zeilen aus Modliste lesen (1:1 übernehmen)
                string[] srcLines;
                try { srcLines = File.ReadAllLines(it.Path); }
                catch (Exception ex)
                {
                    ShowStatus(T("MainForm.Adopt.Failed", "Übernahme fehlgeschlagen: ") + ex.Message);
                    return;
                }

                var newBlock = new List<string>();
                foreach (var raw in srcLines)
                {
                    var l = raw ?? string.Empty;
                    if (l.TrimStart().StartsWith("active_mods", StringComparison.Ordinal))
                        newBlock.Add(l);
                }
                if (newBlock.Count == 0)
                {
                    ShowStatus(T("MainForm.Adopt.NoActiveModsInList", "In der Modliste wurden keine active_mods-Zeilen gefunden"));
                    return;
                }

                // Bestehende profile.sii laden und Block ersetzen
                List<string> target;
                try { target = File.ReadAllLines(sii).ToList(); }
                catch (Exception ex)
                {
                    ShowStatus(T("MainForm.Adopt.Failed", "Übernahme fehlgeschlagen: ") + ex.Message);
                    return;
                }

                int first = -1, last = -1;
                string? originalIndent = null;
                for (int i = 0; i < target.Count; i++)
                {
                    if (target[i].TrimStart().StartsWith("active_mods", StringComparison.Ordinal))
                    {
                        if (first < 0) first = i;
                        last = i;
                        // Einrückung der ersten Fundstelle merken (vor RemoveRange!)
                        if (originalIndent == null)
                        {
                            var line = target[i];
                            int wsLen = line.Length - line.TrimStart().Length;
                            originalIndent = wsLen > 0 ? line.Substring(0, wsLen) : string.Empty;
                        }
                    }
                }

                // Vor Änderung: Backup erzeugen und merken
                string? backupPath = null;
                try
                {
                    var dir = Path.GetDirectoryName(sii) ?? string.Empty;
                    var name = Path.GetFileName(sii);
                    var ts = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                    backupPath = Path.Combine(dir, $"{name}.{ts}.bak");
                    File.Copy(sii, backupPath, overwrite: false);
                }
                catch { backupPath = null; }

                if (first >= 0 && last >= first)
                {
                    // Bereich [first..last] durch neuen Block ersetzen
                    int count = last - first + 1;
                    try { target.RemoveRange(first, count); } catch { }
                    // Einrückung aus originalIndent verwenden
                    string indent = originalIndent ?? string.Empty;
                    var adjusted = new List<string>(newBlock.Count);
                    foreach (var nl in newBlock)
                    {
                        var t = nl.TrimStart();
                        adjusted.Add((indent ?? string.Empty) + t);
                    }
                    target.InsertRange(first, adjusted);
                }
                else
                {
                    // Kein vorhandener Block gefunden → am Ende anhängen (mit Leerzeile)
                    if (target.Count > 0 && !string.IsNullOrWhiteSpace(target[^1])) target.Add(string.Empty);
                    // Einrückung neutral halten
                    target.AddRange(newBlock.Select(l => l.TrimStart()));
                }

                // Zurückschreiben (UTF-8 ohne BOM)
                try
                {
                    File.WriteAllLines(sii, target, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                    ShowStatus(T("MainForm.Adopt.Done", "Modliste übernommen") + ": " + it.Display);
                    // Undo-Link anbieten, falls Backup existiert
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(backupPath) && File.Exists(backupPath))
                        {
                            if (linkUndo != null)
                            {
                                linkUndo.Tag = backupPath;
                                linkUndo.Text = T("MainForm.Adopt.Undo", "Backup wiederherstellen…");
                                StartUndoFadeIn();
                            }
                        }
                    }
                    catch { }
                }
                catch (Exception ex)
                {
                    ShowStatus(T("MainForm.Adopt.Failed", "Übernahme fehlgeschlagen: ") + ex.Message);
                }
            }
            catch (Exception ex)
            {
                ShowStatus(T("MainForm.Adopt.Failed", "Übernahme fehlgeschlagen: ") + ex.Message);
            }
        }

        private void LinkUndo_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                if (linkUndo?.Tag is not string bak || string.IsNullOrWhiteSpace(bak) || !File.Exists(bak))
                {
                    if (linkUndo != null) { StartUndoFadeOut(true); linkUndo.Tag = null; }
                    ShowStatus(T("MainForm.RestoreSii.NoBackups", "Keine Backups gefunden."));
                    return;
                }

                var dir = Path.GetDirectoryName(bak) ?? string.Empty;
                var target = Path.Combine(dir, "profile.sii");
                try { if (File.Exists(target)) CreateTimestampBackup(target); } catch { }
                File.Copy(bak, target, overwrite: true);
                ShowStatus(T("MainForm.RestoreSii.Success", "profile.sii wiederhergestellt"));
            }
            catch (Exception ex)
            {
                ShowStatus(T("MainForm.RestoreSii.Failed", "Wiederherstellung fehlgeschlagen: ") + ex.Message);
            }
            finally
            {
                if (linkUndo != null) { StartUndoFadeOut(true); linkUndo.Tag = null; }
            }
        }

        // Klassenweiter Helfer: URL normalisieren (http/https erzwingen, falls plausibel)
        private static string? NormalizeUrl(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var u = raw.Trim();
            if (u.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return u;
            bool looksDomain = u.StartsWith("www.", StringComparison.OrdinalIgnoreCase) || (u.Contains('.') && !u.Contains(' '));
            if (looksDomain)
                return "https://" + u;
            return u;
        }

        private static string SanitizeAsFileName(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(input.Length);
            foreach (var ch in input)
            {
                if (invalid.Contains(ch)) sb.Append('_'); else sb.Append(ch);
            }
            var trimmed = sb.ToString().Trim();
            if (string.IsNullOrEmpty(trimmed)) return "modlist";
            return trimmed;
        }

        private void OpenSelectedProfileFolder_Click(object? sender, EventArgs e)
        {
            if (cbProfile?.SelectedItem is ProfileItem p && Directory.Exists(p.Directory))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", p.Directory)
                    {
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        }

        // --- Profile: Klonen, Umbenennen, Löschen ---
        private void CloneSelectedProfile_Click(object? sender, EventArgs e)
        {
            if (cbProfile?.SelectedItem is not ProfileItem p || !Directory.Exists(p.Directory)) return;

            var currentName = p.Display;
            var suggested = string.IsNullOrWhiteSpace(currentName) ? "Profil-Kopie" : $"{currentName} - Kopie";
            var newName = PromptForText(this, _lang["MainForm.Menu.Profiles.Clone"], _lang["MainForm.Prompt.Name"], suggested);
            if (string.IsNullOrWhiteSpace(newName)) return; // Abbruch

            // Max 20 Zeichen
            if (newName.Length > 20)
            {
                var msg = _lang["MainForm.Warning.ProfileNameMax20"]; // "SCS erlaubt maximal 20 Zeichen für Profilnamen. Bitte kürzen."
                var cap = T("MainForm.Warning.Caption", "Warnung");
                MessageBox.Show(this, msg, cap, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var destDir = GetSiblingProfileDirectoryForName(p.Directory, newName);
            if (destDir == null) { ShowStatus("Zielpfad ungültig"); return; }

            if (Directory.Exists(destDir)) { ShowStatus("Profil existiert bereits"); return; }

            try
            {
                CopyDirectory(p.Directory, destDir);
                TryUpdateProfileNameInSii(destDir, newName);
                ShowStatus("Profil geklont");
                ReloadProfilesAndSelect(destDir);
            }
            catch (Exception ex)
            {
                ShowStatus($"Fehler beim Klonen: {ex.Message}");
            }
        }

        private void RenameSelectedProfile_Click(object? sender, EventArgs e)
        {
            if (cbProfile?.SelectedItem is not ProfileItem p || !Directory.Exists(p.Directory)) return;

            var currentName = p.Display;
            var newName = PromptForText(this, _lang["MainForm.Menu.Profiles.Rename"], _lang["MainForm.Prompt.Name"], currentName);
            if (string.IsNullOrWhiteSpace(newName)) return; // Abbruch
            if (string.Equals(newName, currentName, StringComparison.CurrentCulture)) return; // keine Änderung

            // Max 20 Zeichen
            if (newName.Length > 20)
            {
                var msg = _lang["MainForm.Warning.ProfileNameMax20"]; // "SCS erlaubt maximal 20 Zeichen für Profilnamen. Bitte kürzen."
                var cap = T("MainForm.Warning.Caption", "Warnung");
                MessageBox.Show(this, msg, cap, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var destDir = GetSiblingProfileDirectoryForName(p.Directory, newName);
            if (destDir == null) { ShowStatus("Zielpfad ungültig"); return; }
            if (Directory.Exists(destDir)) { ShowStatus("Ziel existiert bereits"); return; }

            try
            {
                Directory.Move(p.Directory, destDir);
                TryUpdateProfileNameInSii(destDir, newName);
                ShowStatus("Profil umbenannt");
                ReloadProfilesAndSelect(destDir);
            }
            catch (Exception ex)
            {
                ShowStatus($"Fehler beim Umbenennen: {ex.Message}");
            }
        }

        private void DeleteSelectedProfile_Click(object? sender, EventArgs e)
        {
            if (cbProfile?.SelectedItem is not ProfileItem p || !Directory.Exists(p.Directory)) return;

            var msg = _lang["MainForm.Confirm.DeleteProfile"];
            var caption = _lang["MainForm.Menu.Profiles.Delete"];
            var res = MessageBox.Show(this, msg, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (res != DialogResult.Yes) return;

            try
            {
                Directory.Delete(p.Directory, recursive: true);
                ShowStatus("Profil gelöscht");
                ReloadProfilesAndSelect(null);
            }
            catch (Exception ex)
            {
                ShowStatus($"Fehler beim Löschen: {ex.Message}");
            }
        }

        // Hilfen
        private void ReloadProfilesAndSelect(string? targetDir)
        {
            var code = (cbGame.SelectedItem as GameItem)?.Code ?? "ETS2";
            var norm = NormalizeGameCode(code);
            try { LoadProfilesForSelectedGame(); } catch { }

            if (string.IsNullOrWhiteSpace(targetDir)) return;
            try
            {
                for (int i = 0; i < cbProfile.Items.Count; i++)
                {
                    if (cbProfile.Items[i] is ProfileItem pi && string.Equals(pi.Directory, targetDir, StringComparison.OrdinalIgnoreCase))
                    {
                        cbProfile.SelectedIndex = i;
                        break;
                    }
                }
            }
            catch { }
        }

        private static string? GetSiblingProfileDirectoryForName(string existingProfileDir, string newDisplayName)
        {
            try
            {
                var parent = Path.GetDirectoryName(existingProfileDir);
                if (string.IsNullOrWhiteSpace(parent)) return null;
                var encoded = EncodeProfileFolderName(newDisplayName);
                if (string.IsNullOrWhiteSpace(encoded)) return null;
                return Path.Combine(parent, encoded);
            }
            catch { return null; }
        }

        private static string EncodeProfileFolderName(string display)
        {
            // SCS: Ordnername = UTF-8 Bytes in Hex (klein)
            var bytes = Encoding.UTF8.GetBytes(display);
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private void TryUpdateProfileNameInSii(string profileDir, string newDisplayName)
        {
            try
            {
                var sii = FindProfileSiiPath(profileDir);
                if (sii == null || !File.Exists(sii)) return;

                // Sicherstellen, dass Text vorliegt
                if (!IsProbablyTextFile(sii))
                {
                    TryDecryptProfileFile(sii);
                }
                // Vor Änderung: Zeitstempel-Backup
                CreateTimestampBackup(sii);
                UpdateProfileSiiName(sii, newDisplayName);
            }
            catch { }
        }

        private static bool UpdateProfileSiiName(string siiPath, string newDisplayName)
        {
            try
            {
                var text = File.ReadAllText(siiPath, Encoding.UTF8);
                var escaped = EscapeSiiString(newDisplayName);
                var pattern = "^\\s*profile_name:\\s*\".*?\"";
                var replaced = Regex.Replace(text, pattern, $"profile_name: \"{escaped}\"", RegexOptions.Multiline);
                if (!ReferenceEquals(replaced, text) && !string.Equals(replaced, text, StringComparison.Ordinal))
                {
                    File.WriteAllText(siiPath, replaced, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                    return true;
                }
                return false; // nichts gefunden/ersetzt
            }
            catch { return false; }
        }

        private static string EscapeSiiString(string value)
        {
            if (string.IsNullOrEmpty(value)) return value ?? string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static void CreateTimestampBackup(string path)
        {
            try
            {
                if (!File.Exists(path)) return;
                var dir = Path.GetDirectoryName(path) ?? string.Empty;
                var name = Path.GetFileName(path);
                var ts = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                var bak = Path.Combine(dir, $"{name}.{ts}.bak");
                File.Copy(path, bak, overwrite: false);
            }
            catch { }
        }

        private void RestoreProfileSiiFromBackup_Click(object? sender, EventArgs e)
        {
            if (cbProfile?.SelectedItem is not ProfileItem p) return;
            var dir = p.Directory;
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return;

            try
            {
                var pattern = Path.Combine(dir, "profile.sii.*.bak");
                var backups = Directory.GetFiles(dir, "profile.sii.*.bak", SearchOption.TopDirectoryOnly)
                                        .OrderByDescending(f => f)
                                        .ToArray();
                if (backups.Length == 0)
                {
                    var msgNone = T("MainForm.RestoreSii.NoBackups", "Keine Backups gefunden.");
                    var cap = T("MainForm.Menu.Backup.RestoreSii", "profile.sii wiederherstellen");
                    MessageBox.Show(this, msgNone, cap, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var ofd = new OpenFileDialog();
                ofd.Title = T("MainForm.RestoreSii.ChooseBackup", "Backup-Datei auswählen");
                ofd.InitialDirectory = dir;
                ofd.Filter = "Backup-Dateien|profile.sii.*.bak|Alle Dateien|*.*";
                ofd.FileName = Path.GetFileName(backups[0]);

                if (ofd.ShowDialog(this) != DialogResult.OK) return;

                var target = Path.Combine(dir, "profile.sii");
                // Aktuelle Datei vorher sichern
                if (File.Exists(target)) CreateTimestampBackup(target);

                File.Copy(ofd.FileName, target, overwrite: true);
                ShowStatus(T("MainForm.RestoreSii.Success", "profile.sii wiederhergestellt"));
            }
            catch (Exception ex)
            {
                var msg = T("MainForm.RestoreSii.Failed", "Wiederherstellung fehlgeschlagen: ") + ex.Message;
                ShowStatus(msg);
            }
        }

        private void BackupAllProfiles_Click(object? sender, EventArgs e)
        {
            var code = (cbGame.SelectedItem as GameItem)?.Code ?? "ETS2";
            var norm = NormalizeGameCode(code);
            var profilesRoot = GetProfilesRootForGame(norm);
            if (string.IsNullOrWhiteSpace(profilesRoot) || !Directory.Exists(profilesRoot))
            {
                ShowStatus(T("MainForm.Backup.SourceMissing", "Profiles-Ordner wurde nicht gefunden."));
                return;
            }

            var ts = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var suggested = $"{norm}_profiles_{ts}";
            var res = PromptForBackupOptions(norm, suggested);
            if (res is null) return; // Abbruch
            string name = res.Value.Name;
            string backupsRoot = res.Value.BaseDir;
            try { Directory.CreateDirectory(backupsRoot); } catch { }

            var destDirBase = Path.Combine(backupsRoot, name);
            var destDir = GetUniqueDirectoryName(destDirBase);

            try
            {
                Directory.CreateDirectory(destDir);
                // Inhalte von profiles nach <dest>/profiles
                var targetProfiles = Path.Combine(destDir, "profiles");
                CopyDirectory(profilesRoot, targetProfiles);
                ShowStatus(T("MainForm.Backup.Done", "Backup erstellt: ") + name);
            }
            catch (Exception ex)
            {
                ShowStatus(T("MainForm.Backup.Failed", "Backup fehlgeschlagen: ") + ex.Message);
            }
        }

        private void RestoreProfilesFromBackup_Click(object? sender, EventArgs e)
        {
            var code = (cbGame.SelectedItem as GameItem)?.Code ?? "ETS2";
            var norm = NormalizeGameCode(code);
            var profilesRoot = GetProfilesRootForGame(norm);
            if (string.IsNullOrWhiteSpace(profilesRoot))
            {
                ShowStatus(T("MainForm.Restore.SourceTargetMissing", "Ziel-Profiles-Ordner nicht gefunden."));
                return;
            }

            var backupsRoot = GetBackupsRootForGame(norm);
            try { Directory.CreateDirectory(backupsRoot); } catch { }

            using var fbd = new FolderBrowserDialog();
            fbd.Description = T("MainForm.Restore.ChooseFolder", "Backup-Ordner mit 'profiles' wählen");
            fbd.SelectedPath = backupsRoot;

            if (fbd.ShowDialog(this) != DialogResult.OK) return;

            var chosen = fbd.SelectedPath;
            string sourceProfiles = Path.Combine(chosen, "profiles");
            if (!Directory.Exists(sourceProfiles))
            {
                // Falls direkt auf 'profiles' gezeigt
                if (string.Equals(Path.GetFileName(chosen), "profiles", StringComparison.OrdinalIgnoreCase))
                    sourceProfiles = chosen;
            }

            if (!Directory.Exists(sourceProfiles))
            {
                ShowStatus(T("MainForm.Restore.NoProfilesInBackup", "Im gewählten Ordner wurde kein 'profiles' gefunden."));
                return;
            }

            var confirm = MessageBox.Show(this,
                T("MainForm.Restore.Confirm", "Profile aus dem Backup in den Zielordner kopieren und Dateien überschreiben?"),
                T("MainForm.Menu.Backup.Restore", "Profile wiederherstellen…"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes) return;

            try
            {
                CopyDirectoryMergeOverwrite(sourceProfiles, profilesRoot);
                ShowStatus(T("MainForm.Restore.Done", "Profile wiederhergestellt."));
                // Nach Restore ggf. Liste aktualisieren
                try { LoadProfilesForSelectedGame(); } catch { }
            }
            catch (Exception ex)
            {
                ShowStatus(T("MainForm.Restore.Failed", "Wiederherstellung fehlgeschlagen: ") + ex.Message);
            }
        }

        private string? GetProfilesRootForGameInternal(string normCode)
        {
            try
            {
                string? custom = normCode == "ets2" ? _settings.Current.Ets2ProfilesPath : _settings.Current.AtsProfilesPath;
                if (!string.IsNullOrWhiteSpace(custom) && Directory.Exists(custom))
                    return custom;

                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var gameFolder = normCode == "ets2" ? "Euro Truck Simulator 2" : "American Truck Simulator";
                var gameRoot = Path.Combine(docs, gameFolder);
                var defaultProfiles = Path.Combine(gameRoot, "profiles");
                if (Directory.Exists(defaultProfiles)) return defaultProfiles;
            }
            catch { }
            return null;
        }

        // Korrigierte Verwendung im Aufrufer: wrapper, um alte Signatur zu bedienen
    private string? GetProfilesRootForGame(string norm) => GetProfilesRootForGameInternal(norm);

        private static string GetBackupsRoot()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, "Backups");
        }

        // Neuer, spielabhängiger Backups-Ordner unter dem Steam-Installationsverzeichnis des Spiels
        private string GetBackupsRootForGame(string norm)
        {
            var install = GetOrChooseGameInstallDir(norm);
            return Path.Combine(install, "profiles_backups");
        }

        private string GetOrChooseGameInstallDir(string norm)
        {
            var dir = FindGameInstallDir(norm);
            if (!string.IsNullOrWhiteSpace(dir)) return dir!;

            using var fbd = new FolderBrowserDialog();
            fbd.Description = T("MainForm.Backup.ChooseInstallDir", norm == "ets2" ? "ETS2-Installationsordner wählen" : "ATS-Installationsordner wählen");
            if (fbd.ShowDialog(this) == DialogResult.OK) return fbd.SelectedPath;

            // Fallback: Dokumente-Spielordner
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var gameFolder = norm == "ets2" ? "Euro Truck Simulator 2" : "American Truck Simulator";
            var gameRoot = Path.Combine(docs, gameFolder);
            return Directory.Exists(gameRoot) ? gameRoot : AppDomain.CurrentDomain.BaseDirectory;
        }

        private string? FindGameInstallDir(string norm)
        {
            var steamPath = TryGetSteamPathFromRegistry();
            foreach (var lib in EnumerateSteamLibraries(steamPath))
            {
                var gameName = norm == "ets2" ? "Euro Truck Simulator 2" : "American Truck Simulator";
                var candidate = Path.Combine(lib, "steamapps", "common", gameName);
                if (Directory.Exists(candidate)) return candidate;
            }
            return null;
        }

        private static string? TryGetSteamPathFromRegistry()
        {
            try
            {
                using var hkcu = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\\Valve\\Steam");
                var path = hkcu?.GetValue("SteamPath") as string;
                if (!string.IsNullOrWhiteSpace(path)) return path;

                using var hklm = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\\WOW6432Node\\Valve\\Steam");
                path = hklm?.GetValue("InstallPath") as string;
                if (!string.IsNullOrWhiteSpace(path)) return path;
            }
            catch { }
            return null;
        }

        private static IEnumerable<string> EnumerateSteamLibraries(string? steamPath)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!string.IsNullOrWhiteSpace(steamPath) && Directory.Exists(steamPath))
                {
                    set.Add(steamPath);
                    var vdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                    foreach (var lib in ParseLibraryFoldersVdf(vdf)) set.Add(lib);
                }
            }
            catch { }

            // Standardpfad ergänzen
            var defaultX86 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
            if (Directory.Exists(defaultX86)) set.Add(defaultX86);
            var defaultPF = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam");
            if (Directory.Exists(defaultPF)) set.Add(defaultPF);
            var defaultUser = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Steam");
            if (Directory.Exists(defaultUser)) set.Add(defaultUser);
            // Optional: Public Steam (selten)
            var publicSteam = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Steam");
            if (Directory.Exists(publicSteam)) set.Add(publicSteam);
            return set;
        }

        private static IEnumerable<string> ParseLibraryFoldersVdf(string vdfPath)
        {
            var result = new List<string>();
            try
            {
                if (!File.Exists(vdfPath)) return result;
                foreach (var raw in File.ReadAllLines(vdfPath))
                {
                    var line = raw.Trim();
                    if (line.Length == 0) continue;
                    // Sehr einfache Extraktion: "path"  "C:\\SteamLibrary"
                    if (line.StartsWith("\"path\"", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split('"');
                        if (parts.Length >= 4)
                        {
                            var val = parts[3].Replace("\\\\", "\\");
                            if (Directory.Exists(val)) result.Add(val);
                        }
                    }
                }
            }
            catch { }
            return result;
        }

        // Kleiner Parser für appworkshop_<appid>.acf, liefert Mapping (PublishedFileId -> Title)
        private static Dictionary<string, string> TryReadWorkshopTitles(string acfPath)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(acfPath)) return map;
                string? currentId = null;
                foreach (var raw in File.ReadLines(acfPath))
                {
                    var line = raw.Trim();
                    if (line.Length == 0) continue;
                    // ID-Start: "<digits>" {  -> lese die digits zwischen den ersten Anführungszeichen
                    if (currentId == null && line.Length > 2 && line[0] == '"' && char.IsDigit(line.Length > 1 ? line[1] : '\0'))
                    {
                        int endQuote = line.IndexOf('"', 1);
                        if (endQuote > 1)
                        {
                            var id = line.Substring(1, endQuote - 1);
                            // nur akzeptieren, wenn die Zeile eine öffnende Klammer enthält
                            if (line.IndexOf('{', endQuote) >= 0)
                            {
                                currentId = id;
                                continue;
                            }
                        }
                    }
                    // Innerhalb eines ID-Blocks nach Titel suchen
                    if (currentId != null)
                    {
                        if (line.StartsWith("\"title\""))
                        {
                            // Find first quote after the key
                            int first = line.IndexOf('"', 7); // nach "title"
                            if (first >= 0)
                            {
                                int second = line.IndexOf('"', first + 1);
                                if (second > first)
                                {
                                    var title = line.Substring(first + 1, second - first - 1);
                                    map[currentId] = title;
                                    continue;
                                }
                            }
                        }
                        else if (line == "}")
                        {
                            currentId = null;
                        }
                    }
                }
            }
            catch { }
            return map;
        }

        private static string GetUniqueDirectoryName(string basePath)
        {
            if (!Directory.Exists(basePath)) return basePath;
            int i = 1;
            while (true)
            {
                var candidate = basePath + "_" + i.ToString();
                if (!Directory.Exists(candidate)) return candidate;
                i++;
            }
        }

        private static void CopyDirectoryMergeOverwrite(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var name = Path.GetFileName(file);
                var targetFile = Path.Combine(destDir, name);
                File.Copy(file, targetFile, overwrite: true);
            }

            foreach (var sub in Directory.GetDirectories(sourceDir))
            {
                var name = Path.GetFileName(sub);
                var targetSub = Path.Combine(destDir, name);
                CopyDirectoryMergeOverwrite(sub, targetSub);
            }
        }

        private string T(string key, string fallback)
        {
            try { return _lang[key]; } catch { return fallback; }
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            var src = new DirectoryInfo(sourceDir);
            if (!src.Exists) throw new DirectoryNotFoundException(sourceDir);

            Directory.CreateDirectory(destDir);

            foreach (var file in src.GetFiles())
            {
                var targetFilePath = Path.Combine(destDir, file.Name);
                file.CopyTo(targetFilePath, overwrite: false);
            }

            foreach (var sub in src.GetDirectories())
            {
                var targetSubPath = Path.Combine(destDir, sub.Name);
                CopyDirectory(sub.FullName, targetSubPath);
            }
        }

        // Dialog: Backup-Name + Zielbasis wählen (Default = profiles_backups des Spiels)
        private (string Name, string BaseDir)? PromptForBackupOptions(string norm, string suggestedName)
        {
            using var f = new Form();
            f.StartPosition = FormStartPosition.CenterParent;
            f.Text = T("MainForm.Backup.DialogTitle", "Profile sichern");
            f.FormBorderStyle = FormBorderStyle.FixedDialog;
            f.MinimizeBox = false;
            f.MaximizeBox = false;
            f.ShowInTaskbar = false;
            f.ClientSize = new Size(540, 160);

            var lblName = new Label { Left = 12, Top = 16, Width = 120, Text = T("MainForm.Backup.NameLabel", "Name:") };
            var tbName = new TextBox { Left = 130, Top = 12, Width = 390, Text = suggestedName };

            var lblPath = new Label { Left = 12, Top = 52, Width = 120, Text = T("MainForm.Backup.TargetLabel", "Zielordner:") };
            var tbPath = new TextBox { Left = 130, Top = 48, Width = 308, ReadOnly = false };
            var btnBrowse = new Button { Left = 446, Top = 46, Width = 74, Text = T("MainForm.Common.Browse", "Durchsuchen…") };

            string defaultRoot = GetBackupsRootForGame(norm);
            try { Directory.CreateDirectory(defaultRoot); } catch { }
            tbPath.Text = defaultRoot;

            btnBrowse.Click += (_, __) =>
            {
                using var fbd = new FolderBrowserDialog();
                fbd.Description = T("MainForm.Backup.ChooseTarget", "Backup-Zielordner wählen");
                fbd.SelectedPath = Directory.Exists(tbPath.Text) ? tbPath.Text : defaultRoot;
                if (fbd.ShowDialog(f) == DialogResult.OK)
                {
                    tbPath.Text = fbd.SelectedPath;
                }
            };

            var btnOk = new Button { Text = T("Common.OK", "OK"), DialogResult = DialogResult.OK, Left = 360, Width = 75, Top = 110 };
            var btnCancel = new Button { Text = T("Common.Cancel", "Abbrechen"), DialogResult = DialogResult.Cancel, Left = 445, Width = 75, Top = 110 };

            f.Controls.AddRange(new Control[] { lblName, tbName, lblPath, tbPath, btnBrowse, btnOk, btnCancel });
            f.AcceptButton = btnOk; f.CancelButton = btnCancel;

            var res = f.ShowDialog(this);
            if (res != DialogResult.OK) return null;

            var name = (tbName.Text ?? string.Empty).Trim();
            var baseDir = (tbPath.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(baseDir)) return null;
            return (name, baseDir);
        }

        private string? PromptForText(IWin32Window owner, string title, string label, string initial, bool limit20 = true)
        {
            using var f = new Form();
            f.StartPosition = FormStartPosition.CenterParent;
            f.Text = title;
            f.FormBorderStyle = FormBorderStyle.FixedDialog;
            f.MinimizeBox = false;
            f.MaximizeBox = false;
            f.ShowInTaskbar = false;
            f.ClientSize = new Size(420, 140);

            var lbl = new Label { Left = 12, Top = 15, Width = 390, Text = label };
            var tb = new TextBox { Left = 12, Top = 40, Width = 390, Text = initial };
            Label? lblHint = null;
            if (limit20)
            {
                lblHint = new Label { Left = 12, Top = 66, Width = 390, AutoSize = false, Height = 18, Text = T("MainForm.Warning.ProfileNameHint", "Max. 20 Zeichen"), ForeColor = SystemColors.GrayText };
            }
            var btnOk = new Button { Text = _GetOkText(), DialogResult = DialogResult.OK, Left = 236, Width = 80, Top = 90 };
            var btnCancel = new Button { Text = _GetCancelText(), DialogResult = DialogResult.Cancel, Left = 322, Width = 80, Top = 90 };

            // Live-Warnung bei >20 Zeichen: Feld rot einfärben
            if (limit20)
            {
                void UpdateWarn()
                {
                    bool over = (tb.Text?.Length ?? 0) > 20;
                    tb.BackColor = over ? Color.MistyRose : SystemColors.Window;
                    tb.ForeColor = over ? Color.Maroon : SystemColors.WindowText;
                    if (lblHint != null) lblHint.ForeColor = over ? Color.Maroon : SystemColors.GrayText;
                }
                tb.TextChanged += (_, __) => UpdateWarn();
                UpdateWarn();
            }

            if (lblHint != null)
                f.Controls.AddRange(new Control[] { lbl, tb, lblHint, btnOk, btnCancel });
            else
                f.Controls.AddRange(new Control[] { lbl, tb, btnOk, btnCancel });
            f.AcceptButton = btnOk; f.CancelButton = btnCancel;

            return f.ShowDialog(owner) == DialogResult.OK ? tb.Text?.Trim() : null;

            string _GetOkText() => SafeLang("Common.OK", "OK");
            string _GetCancelText() => SafeLang("Common.Cancel", "Abbrechen");
            string SafeLang(string key, string fallback)
            {
                try { return _lang[key]; } catch { return fallback; }
            }
        }
        private ToolStripItem? TryFindMenuItemByName(MenuStrip? ms, string name)
        {
            if (ms == null) return null;
            foreach (ToolStripItem item in ms.Items)
            {
                if (string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                    return item;
                if (item is ToolStripMenuItem mi)
                {
                    var found = FindInDropDown(mi, name);
                    if (found != null) return found;
                }
            }
            return null;
        }

        private ToolStripItem? FindInDropDown(ToolStripMenuItem parent, string name)
        {
            foreach (ToolStripItem sub in parent.DropDownItems)
            {
                if (string.Equals(sub.Name, name, StringComparison.OrdinalIgnoreCase))
                    return sub;
                if (sub is ToolStripMenuItem mi)
                {
                    var found = FindInDropDown(mi, name);
                    if (found != null) return found;
                }
            }
            return null;
        }

        // Click-Handler für Optionen – robuste Live-Preview-Version
        private void OpenOptionsDialog_Click(object? sender, EventArgs e)
        {
            var snapshot = TakeSnapshot();

            using var dlg = new OptionsForm(_settings);

            // Live-Preview: Events abonnieren
            dlg.LanguageChangedLive += OnLanguageLive;
            dlg.ThemeChangedLive += OnThemeLive;
            dlg.PreferredGameChangedLive += OnPreferredGameLive;
            dlg.PathsChangedLive += OnPathsLive;

            try
            {
                var result = dlg.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    // Persistieren
                    _settings.Save();
                    // Sicherheit: Theme final anwenden (inkl. Header-Banner)
                    try { _theme.Apply(this, _settings.Current.Theme ?? "Light"); } catch { }
                    try { this.headerBanner.ApplyTheme(string.Equals(_settings.Current.Theme, "Dark", StringComparison.OrdinalIgnoreCase)); } catch { }
                    return;
                }

                // Abbrechen -> Snapshot wiederherstellen
                ApplySnapshot(snapshot);
            }
            finally
            {
                // Sauber wieder abmelden
                dlg.LanguageChangedLive -= OnLanguageLive;
                dlg.ThemeChangedLive -= OnThemeLive;
                dlg.PreferredGameChangedLive -= OnPreferredGameLive;
                dlg.PathsChangedLive -= OnPathsLive;
            }

            // Live-Handler
            void OnLanguageLive(string langCode)
            {
                if (_suppressCascade) return;
                this.SuspendLayout();
                try
                {
                    _suppressCascade = true;
                    _settings.Current.Language = string.IsNullOrWhiteSpace(langCode) ? "de" : langCode;
                    try { _lang.Load(_settings.Current.Language); } catch { }
                    ApplyLanguage();
                }
                finally
                {
                    _suppressCascade = false;
                    this.ResumeLayout(true);
                    this.Refresh();
                }
            }

            void OnThemeLive(string themeName)
            {
                if (_suppressCascade) return;
                this.SuspendLayout();
                try
                {
                    _suppressCascade = true;
                    _settings.Current.Theme = string.IsNullOrWhiteSpace(themeName) ? "Light" : themeName;
                    try { _theme.Apply(this, _settings.Current.Theme); } catch { }
                    // Header-Banner sofort an Theme anpassen
                    try { this.headerBanner.ApplyTheme(string.Equals(_settings.Current.Theme, "Dark", StringComparison.OrdinalIgnoreCase)); } catch { }
                    // Undo-Link Ziel-Farbe dem Theme anpassen
                    var isDark = string.Equals(_settings.Current.Theme, "Dark", StringComparison.OrdinalIgnoreCase);
                    _undoEndColor = isDark ? Color.SkyBlue : Color.RoyalBlue;
                    // Falls der Link sichtbar ist und keine Animation läuft, sofort die Linkfarben angleichen
                    if (linkUndo != null && linkUndo.Visible && _undoAnimDir == 0)
                    {
                        linkUndo.LinkColor = _undoEndColor;
                        linkUndo.ActiveLinkColor = _undoEndColor;
                        linkUndo.VisitedLinkColor = _undoEndColor;
                    }
                }
                finally
                {
                    _suppressCascade = false;
                    this.ResumeLayout(true);
                    this.Refresh();
                }
            }

            void OnPreferredGameLive(string game)
            {
                if (_suppressCascade) return;
                this.SuspendLayout();
                try
                {
                    _suppressCascade = true;
                    var code = NormalizeGameCode(game);
                    _settings.Current.PreferredGame = code;
                    try { SelectGameByCode(code); } catch { }
                    try { UpdateGameLogo(code); } catch { }
                    try { LoadProfilesForSelectedGame(); } catch { }
                }
                finally
                {
                    _suppressCascade = false;
                    this.ResumeLayout(true);
                    this.Refresh();
                }
            }

            void OnPathsLive(string ets2, string ats)
            {
                // Nur übernehmen (kein unmittelbarer visueller Effekt)
                _settings.Current.Ets2ProfilesPath = string.IsNullOrWhiteSpace(ets2) ? null : ets2;
                _settings.Current.AtsProfilesPath = string.IsNullOrWhiteSpace(ats) ? null : ats;
            }
        }

        private UiSnapshot TakeSnapshot()
            => new UiSnapshot(
                _settings.Current.Language ?? "de",
                _settings.Current.Theme ?? "Light",
                NormalizeGameCode(_settings.Current.PreferredGame),
                _settings.Current.Ets2ProfilesPath,
                _settings.Current.AtsProfilesPath
            );

        private void ApplySnapshot(UiSnapshot s)
        {
            this.SuspendLayout();
            try
            {
                _suppressCascade = true;

                _settings.Current.Language = s.Language;
                _settings.Current.Theme = s.Theme;
                _settings.Current.PreferredGame = s.PreferredGame;
                _settings.Current.Ets2ProfilesPath = s.Ets2Path;
                _settings.Current.AtsProfilesPath = s.AtsPath;

                try { _lang.Load(_settings.Current.Language); } catch { }
                ApplyLanguage();

                try { _theme.Apply(this, _settings.Current.Theme); } catch { }
                try { this.headerBanner.ApplyTheme(string.Equals(_settings.Current.Theme, "Dark", StringComparison.OrdinalIgnoreCase)); } catch { }
                try { this.headerBanner.ApplyTheme(string.Equals(_settings.Current.Theme, "Dark", StringComparison.OrdinalIgnoreCase)); } catch { }

                try { SelectGameByCode(_settings.Current.PreferredGame); } catch { }
                try { UpdateGameLogo(_settings.Current.PreferredGame); } catch { }
                try { LoadProfilesForSelectedGame(); } catch { }
            }
            finally
            {
                _suppressCascade = false;
                this.ResumeLayout(true);
                this.Refresh();
            }
        }

        private static string NormalizeGameCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "ets2";
            var c = code.Trim();

            if (c.Equals("ets2", StringComparison.OrdinalIgnoreCase) ||
                c.Equals("ETS2", StringComparison.OrdinalIgnoreCase) ||
                c.Equals("Euro Truck Simulator 2", StringComparison.OrdinalIgnoreCase) ||
                c.Equals("EuroTruckSimulator2", StringComparison.OrdinalIgnoreCase))
                return "ets2";

            if (c.Equals("ats", StringComparison.OrdinalIgnoreCase) ||
                c.Equals("ATS", StringComparison.OrdinalIgnoreCase) ||
                c.Equals("American Truck Simulator", StringComparison.OrdinalIgnoreCase) ||
                c.Equals("AmericanTruckSimulator", StringComparison.OrdinalIgnoreCase))
                return "ats";

            return "ets2";
        }

        private void AutoDecryptProfilesForSelectedGame()
        {
            var code = (cbGame.SelectedItem as GameItem)?.Code ?? "ETS2";
            var norm = NormalizeGameCode(code);

            int total = 0, decrypted = 0;
            foreach (var prof in EnumerateProfiles(norm))
            {
                var sii = FindProfileSiiPath(prof.Directory);
                if (sii == null || !File.Exists(sii)) continue;
                total++;
                if (IsProbablyTextFile(sii)) continue; // bereits Klartext
                if (TryDecryptProfileFile(sii)) decrypted++;
            }
            if (total > 0) ShowStatus($"{decrypted}/{total} profile.sii entschlüsselt");
        }

    private System.Windows.Forms.Timer? _statusTimer;
    private void ShowStatus(string message, int milliseconds = 4000)
        {
            try
            {
        // Zeige Meldungen ausschließlich im Logfenster, um Doppelanzeigen und abgeschnittenen Text zu vermeiden
        if (lblStatus != null) lblStatus.Text = string.Empty;
                AppendLog(message);
                _statusTimer ??= new System.Windows.Forms.Timer();
                _statusTimer.Stop();
                _statusTimer.Interval = milliseconds;
                _statusTimer.Tick -= StatusTimer_Tick;
                _statusTimer.Tick += StatusTimer_Tick;
                _statusTimer.Start();
            }
            catch { }
        }

        private void StatusTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                _statusTimer?.Stop();
                if (lblStatus != null) lblStatus.Text = string.Empty; // bleibt leer
            }
            catch { }
        }

        private void AppendLog(string? message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message)) return;
                if (txtLog == null) return;
                if (txtLog.IsDisposed) return;

                if (txtLog.InvokeRequired)
                {
                    txtLog.Invoke(new Action<string?>(AppendLog), message);
                    return;
                }

                var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
                if (txtLog.TextLength > 0) txtLog.AppendText(Environment.NewLine);
                txtLog.AppendText(line);

                // Optional: Scroll to end
                txtLog.SelectionStart = txtLog.TextLength;
                txtLog.ScrollToCaret();
            }
            catch { }
        }

        private static string? FindProfileSiiPath(string profileDir)
        {
            try
            {
                var p = Path.Combine(profileDir, "profile.sii");
                return File.Exists(p) ? p : null;
            }
            catch { return null; }
        }

        private static bool IsProbablyTextFile(string path)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var len = (int)Math.Min(2048, fs.Length);
                var buf = new byte[len];
                fs.Read(buf, 0, len);

                int control = 0;
                for (int i = 0; i < len; i++)
                {
                    byte b = buf[i];
                    if (b == 0) return false; // NUL = sehr wahrscheinlich binär
                    // Erlaube gängige Textbereiche (Tab, LF, CR, etc.)
                    bool printable = b == 9 || b == 10 || b == 13 || (b >= 32 && b <= 126) || (b >= 128);
                    if (!printable) control++;
                }
                // Wenn weniger als 5% nicht-druckbare Steuerzeichen, als Text behandeln
                return control * 20 <= len;
            }
            catch { return false; }
        }

        private string? GetSiiDecryptPath()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var p1 = Path.Combine(baseDir, "ModlistManager", "Tools", "SII_Decrypt.exe");
                var p2 = Path.Combine(baseDir, "Tools", "SII_Decrypt.exe");
                if (File.Exists(p1)) return p1;
                if (File.Exists(p2)) return p2;
            }
            catch { }
            return null;
        }

        private bool TryDecryptProfileFile(string siiPath)
        {
            var tool = GetSiiDecryptPath();
            if (string.IsNullOrWhiteSpace(tool) || !File.Exists(tool)) return false;

            // Versuche verschiedene Aufrufvarianten
            bool ok = false;
            string? backup = null;
            try
            {
                backup = siiPath + ".bak";
                if (!File.Exists(backup))
                {
                    try { File.Copy(siiPath, backup, overwrite: false); } catch { }
                }

                // 1) Einfach: nur Input-Argument
                ok = RunTool(tool, $"\"{siiPath}\"");
                if (!ok || !IsProbablyTextFile(siiPath))
                {
                    // 2) -i/-o Variante
                    ok = RunTool(tool, $"-i \"{siiPath}\" -o \"{siiPath}\"");
                }

                if (!ok || !IsProbablyTextFile(siiPath))
                {
                    // 3) Eingabe + Ausgabe-Datei
                    var outPath = siiPath + ".dec";
                    ok = RunTool(tool, $"\"{siiPath}\" \"{outPath}\"");
                    if (ok && File.Exists(outPath) && IsProbablyTextFile(outPath))
                    {
                        try
                        {
                            File.Copy(outPath, siiPath, overwrite: true);
                            try { File.Delete(outPath); } catch { }
                        }
                        catch { ok = false; }
                    }
                }
            }
            catch { ok = false; }

            return ok && IsProbablyTextFile(siiPath);

            bool RunTool(string exe, string args)
            {
                try
                {
                    using var p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = exe;
                    p.StartInfo.Arguments = args;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.RedirectStandardError = false;
                    p.StartInfo.RedirectStandardOutput = false;
                    p.Start();
                    p.WaitForExit(5000);
                    return p.ExitCode == 0 || p.ExitCode == 1; // einige Tools liefern 1 trotz Erfolg
                }
                catch { return false; }
            }
        }
    }
}


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ETS2ATS.ModlistManager.Services;

namespace ETS2ATS.ModlistManager.Forms.Tools
{
    public sealed class ModFolderCleanupForm : Form
    {
        private readonly LanguageService _lang;
        private readonly string _modsFolder;
        private readonly string _modlistsFolder;
        private readonly string? _currentModlistPath;

        private readonly Label _lblSummary;
        private readonly CheckBox _chkAllModlists;
        private readonly CheckBox _chkIncludeSubfolders;
        private readonly CheckBox _chkShowOtherTypes;
        private readonly TextBox _txtArchiveFolder;
        private readonly Button _btnChooseArchive;
        private readonly CheckedListBox _list;
        private readonly Button _btnRefresh;
        private readonly Button _btnMove;
        private readonly Button _btnOpenMods;
        private readonly Button _btnOpenArchive;
        private readonly Button _btnClose;

        private sealed record FileEntry(string FullPath, string Display, bool CanMove);

        private IReadOnlyList<FileEntry> _unusedFiles = Array.Empty<FileEntry>();

        public ModFolderCleanupForm(LanguageService lang, string modsFolder, string modlistsFolder, string? currentModlistPath)
        {
            _lang = lang;
            _modsFolder = modsFolder;
            _modlistsFolder = modlistsFolder;
            _currentModlistPath = currentModlistPath;

            Text = T("ModCleanup.Title", "Modordner bereinigen");
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = true;
            Width = 920;
            Height = 650;

            var top = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(12),
            };
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            top.RowStyles.Add(new RowStyle());
            top.RowStyles.Add(new RowStyle());

            _lblSummary = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                Text = "",
            };

            _chkAllModlists = new CheckBox
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                Checked = true,
                Text = T("ModCleanup.ScopeAll", "Alle Modlisten berücksichtigen"),
            };
            _chkAllModlists.CheckedChanged += (_, __) => RefreshList();

            var optionsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0, 6, 0, 0)
            };

            _chkIncludeSubfolders = new CheckBox
            {
                AutoSize = true,
                Checked = false,
                Text = T("ModCleanup.IncludeSubfolders", "Unterordner scannen"),
            };
            _chkIncludeSubfolders.CheckedChanged += (_, __) => RefreshList();

            _chkShowOtherTypes = new CheckBox
            {
                AutoSize = true,
                Checked = false,
                Text = T("ModCleanup.ShowOtherTypes", "Auch .7z/.rar anzeigen (nur Anzeige)"),
            };
            _chkShowOtherTypes.CheckedChanged += (_, __) => RefreshList();

            optionsFlow.Controls.Add(_chkIncludeSubfolders);
            optionsFlow.Controls.Add(_chkShowOtherTypes);

            _btnRefresh = new Button
            {
                AutoSize = true,
                Text = T("ModCleanup.Refresh", "Aktualisieren"),
            };
            _btnRefresh.Click += (_, __) => RefreshList();

            top.Controls.Add(_lblSummary, 0, 0);
            top.Controls.Add(_chkAllModlists, 1, 0);
            top.Controls.Add(_btnRefresh, 2, 0);

            top.Controls.Add(optionsFlow, 0, 1);
            top.SetColumnSpan(optionsFlow, 3);

            var archiveRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                Padding = new Padding(12, 0, 12, 12)
            };
            archiveRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            archiveRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            archiveRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var lblArchive = new Label
            {
                AutoSize = true,
                Text = T("ModCleanup.ArchiveFolder", "Archivordner:"),
                Anchor = AnchorStyles.Left,
            };

            _txtArchiveFolder = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = false,
            };

            _btnChooseArchive = new Button
            {
                AutoSize = true,
                Text = T("ModCleanup.Choose", "Wählen…"),
            };
            _btnChooseArchive.Click += (_, __) => ChooseArchiveFolder();

            archiveRow.Controls.Add(lblArchive, 0, 0);
            archiveRow.Controls.Add(_txtArchiveFolder, 1, 0);
            archiveRow.Controls.Add(_btnChooseArchive, 2, 0);

            _list = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                HorizontalScrollbar = true,
                IntegralHeight = false,
            };

            var bottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(12),
                AutoSize = true,
                WrapContents = false
            };

            _btnClose = new Button { AutoSize = true, Text = T("Common.Close", "Schließen") };
            _btnClose.Click += (_, __) => Close();

            _btnMove = new Button { AutoSize = true, Text = T("ModCleanup.Move", "Ausgewählte verschieben") };
            _btnMove.Click += (_, __) => MoveSelected();

            _btnOpenArchive = new Button { AutoSize = true, Text = T("ModCleanup.OpenArchive", "Archiv öffnen") };
            _btnOpenArchive.Click += (_, __) => TryOpenFolder(_txtArchiveFolder.Text);

            _btnOpenMods = new Button { AutoSize = true, Text = T("ModCleanup.OpenMods", "Modordner öffnen") };
            _btnOpenMods.Click += (_, __) => TryOpenFolder(_modsFolder);

            bottom.Controls.Add(_btnClose);
            bottom.Controls.Add(_btnMove);
            bottom.Controls.Add(_btnOpenArchive);
            bottom.Controls.Add(_btnOpenMods);

            Controls.Add(_list);
            Controls.Add(bottom);
            Controls.Add(archiveRow);
            Controls.Add(top);

            _txtArchiveFolder.Text = GetDefaultArchiveFolder(_modsFolder);

            RefreshList();
        }

        private string GetDefaultArchiveFolder(string modsFolder)
        {
            var folderName = T("ModCleanup.DefaultArchiveName", "Mods Alt");

            try
            {
                // Prefer a sibling folder next to "mod" (e.g. ...\Euro Truck Simulator 2\Mods Alt)
                var parent = Directory.GetParent(modsFolder)?.FullName;
                if (!string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent))
                    return Path.Combine(parent, folderName);
            }
            catch { }

            // Fallback (should be rare): keep inside mods folder
            return Path.Combine(modsFolder, folderName);
        }

        private void RefreshList()
        {
            try
            {
                var usedPackages = CollectUsedPackages();
                var archiveFolder = _txtArchiveFolder.Text?.Trim();
                var modFiles = EnumerateCandidateFiles(_modsFolder, _chkIncludeSubfolders.Checked, archiveFolder, _chkShowOtherTypes.Checked).ToList();

                var unused = new List<FileEntry>();
                foreach (var entry in modFiles)
                {
                    if (!entry.CanMove)
                    {
                        unused.Add(entry);
                        continue;
                    }

                    var baseName = Path.GetFileNameWithoutExtension(entry.FullPath);
                    if (string.IsNullOrWhiteSpace(baseName)) continue;

                    if (!usedPackages.Contains(baseName))
                        unused.Add(entry);
                }

                unused.Sort((a, b) => StringComparer.CurrentCultureIgnoreCase.Compare(a.Display, b.Display));
                _unusedFiles = unused;

                _list.BeginUpdate();
                try
                {
                    _list.Items.Clear();
                    foreach (var entry in _unusedFiles)
                    {
                        var display = entry.Display;
                        if (!entry.CanMove)
                            display += " " + T("ModCleanup.DisplayOnlySuffix", "(nur Anzeige)");
                        _list.Items.Add(display, true);
                    }
                }
                finally { _list.EndUpdate(); }

                _lblSummary.Text = string.Format(
                    T("ModCleanup.Summary", "Mods im Ordner: {0} • In Modlisten referenziert: {1} • Ungenutzt: {2}"),
                    modFiles.Count,
                    usedPackages.Count,
                    _unusedFiles.Count);

                _btnMove.Enabled = _unusedFiles.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    T("ModCleanup.ErrorRefresh", "Fehler beim Abgleich:") + "\n" + ex.Message,
                    T("ModCleanup.Title", "Modordner bereinigen"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private HashSet<string> CollectUsedPackages()
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var files = new List<string>();
            if (_chkAllModlists.Checked)
            {
                try
                {
                    if (Directory.Exists(_modlistsFolder))
                    {
                        files.AddRange(Directory.EnumerateFiles(_modlistsFolder, "*.txt", SearchOption.TopDirectoryOnly));
                    }
                }
                catch { }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(_currentModlistPath) && File.Exists(_currentModlistPath))
                    files.Add(_currentModlistPath);
            }

            foreach (var file in files.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    foreach (var pkg in ReadPackagesFromModlist(file))
                        used.Add(pkg);
                }
                catch { }
            }

            return used;
        }

        private static IEnumerable<string> ReadPackagesFromModlist(string modlistFile)
        {
            foreach (var lineRaw in File.ReadLines(modlistFile))
            {
                var line = lineRaw?.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Fast-path: typical format contains quotes and a '|' separator.
                var pair = TryParsePair(line);
                if (pair == null) continue;

                var pkg = pair.Value.Package;
                if (string.IsNullOrWhiteSpace(pkg)) continue;

                // Workshop entries are not in the local mod folder (Documents/<Game>/mod)
                if (pkg.StartsWith("mod_workshop_package.", StringComparison.OrdinalIgnoreCase))
                    continue;

                yield return pkg;
            }
        }

        private static (string Package, string Name)? TryParsePair(string line)
        {
            var s = line.Trim();

            // Prefer quoted content (active_mods[0]: "pkg|name")
            int q1 = s.IndexOf('"');
            int q2 = s.LastIndexOf('"');
            if (q1 >= 0 && q2 > q1)
            {
                s = s.Substring(q1 + 1, q2 - q1 - 1);
            }

            s = DecodeScsEscapes(s);

            int pipe = s.IndexOf('|');
            if (pipe > 0)
            {
                var left = DecodeScsEscapes(s.Substring(0, pipe).Trim().Trim('"'));
                var right = DecodeScsEscapes(s.Substring(pipe + 1).Trim().Trim('"'));
                return (left, right);
            }

            // Legacy fallbacks
            foreach (var sep in new[] { ";", "\t", " - " })
            {
                int p = s.IndexOf(sep, StringComparison.Ordinal);
                if (p > 0)
                {
                    var left = DecodeScsEscapes(s.Substring(0, p).Trim().Trim('"'));
                    var right = DecodeScsEscapes(s.Substring(p + sep.Length).Trim().Trim('"'));
                    if (!string.IsNullOrWhiteSpace(left) || !string.IsNullOrWhiteSpace(right))
                        return (left, right);
                }
            }

            return null;
        }

        private static string DecodeScsEscapes(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            if (input.IndexOf("\\x", StringComparison.OrdinalIgnoreCase) < 0) return input;

            try
            {
                var bytes = new List<byte>(input.Length);
                bool changed = false;

                for (int i = 0; i < input.Length; i++)
                {
                    char c = input[i];
                    if (c == '\\' && i + 3 < input.Length && (input[i + 1] == 'x' || input[i + 1] == 'X'))
                    {
                        int hi = HexVal(input[i + 2]);
                        int lo = HexVal(input[i + 3]);
                        if (hi >= 0 && lo >= 0)
                        {
                            bytes.Add((byte)((hi << 4) | lo));
                            i += 3;
                            changed = true;
                            continue;
                        }
                    }

                    if (c <= 0x7F)
                    {
                        bytes.Add((byte)c);
                    }
                    else
                    {
                        bytes.AddRange(Encoding.UTF8.GetBytes(new[] { c }));
                    }
                }

                if (!changed) return input;
                return Encoding.UTF8.GetString(bytes.ToArray());
            }
            catch
            {
                return input;
            }

            static int HexVal(char ch)
            {
                if (ch >= '0' && ch <= '9') return ch - '0';
                if (ch >= 'a' && ch <= 'f') return 10 + (ch - 'a');
                if (ch >= 'A' && ch <= 'F') return 10 + (ch - 'A');
                return -1;
            }
        }

        private IEnumerable<FileEntry> EnumerateCandidateFiles(string modsFolder, bool includeSubfolders, string? archiveFolder, bool showOtherTypes)
        {
            if (string.IsNullOrWhiteSpace(modsFolder) || !Directory.Exists(modsFolder))
                yield break;

            IEnumerable<string> files;
            var searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            try { files = Directory.EnumerateFiles(modsFolder, "*.*", searchOption); }
            catch { yield break; }

            string? archiveFull = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(archiveFolder))
                    archiveFull = Path.GetFullPath(archiveFolder);
            }
            catch { archiveFull = null; }

            foreach (var file in files)
            {
                if (archiveFull != null)
                {
                    try
                    {
                        var fileFull = Path.GetFullPath(file);
                        if (IsUnderDirectory(fileFull, archiveFull))
                            continue;
                    }
                    catch { }
                }

                var ext = Path.GetExtension(file);

                bool canMove = string.Equals(ext, ".scs", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(ext, ".zip", StringComparison.OrdinalIgnoreCase);

                bool include = canMove;
                if (!include && showOtherTypes)
                {
                    include = string.Equals(ext, ".7z", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(ext, ".rar", StringComparison.OrdinalIgnoreCase);
                }
                if (!include) continue;

                string display;
                try { display = Path.GetRelativePath(modsFolder, file); }
                catch { display = Path.GetFileName(file); }

                yield return new FileEntry(file, display, canMove);
            }
        }

        private static bool IsUnderDirectory(string candidatePath, string parentDir)
        {
            try
            {
                var parent = Path.GetFullPath(parentDir);
                if (!parent.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                    parent += Path.DirectorySeparatorChar;
                var cand = Path.GetFullPath(candidatePath);
                return cand.StartsWith(parent, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private void ChooseArchiveFolder()
        {
            using var fbd = new FolderBrowserDialog();
            fbd.Description = T("ModCleanup.ChooseArchive", "Zielordner für ungenutzte Mods wählen");
            var current = _txtArchiveFolder.Text;
            if (!string.IsNullOrWhiteSpace(current))
            {
                try
                {
                    var dir = Directory.Exists(current) ? current : Path.GetDirectoryName(current);
                    if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                        fbd.SelectedPath = dir;
                }
                catch { }
            }

            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                _txtArchiveFolder.Text = fbd.SelectedPath;
            }
        }

        private void MoveSelected()
        {
            if (_unusedFiles.Count == 0) return;

            var archiveFolder = _txtArchiveFolder.Text?.Trim();
            if (string.IsNullOrWhiteSpace(archiveFolder))
            {
                MessageBox.Show(this,
                    T("ModCleanup.NoArchive", "Bitte einen Archivordner auswählen."),
                    T("ModCleanup.Title", "Modordner bereinigen"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var selected = new List<FileEntry>();
            for (int i = 0; i < _list.Items.Count && i < _unusedFiles.Count; i++)
            {
                if (_list.GetItemChecked(i))
                    selected.Add(_unusedFiles[i]);
            }

            if (selected.Count == 0)
            {
                MessageBox.Show(this,
                    T("ModCleanup.NoneSelected", "Keine Dateien ausgewählt."),
                    T("ModCleanup.Title", "Modordner bereinigen"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var movable = selected.Where(s => s.CanMove).ToList();
            var displayOnly = selected.Where(s => !s.CanMove).ToList();

            if (movable.Count == 0)
            {
                MessageBox.Show(this,
                    T("ModCleanup.NothingMovable", "Die Auswahl enthält keine verschiebbaren Dateien (nur .scs/.zip werden verschoben)."),
                    T("ModCleanup.Title", "Modordner bereinigen"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var msg = string.Format(
                T("ModCleanup.ConfirmMove", "{0} Datei(en) nach '{1}' verschieben?"),
                movable.Count,
                archiveFolder);

            if (MessageBox.Show(this, msg, T("ModCleanup.Title", "Modordner bereinigen"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                Directory.CreateDirectory(archiveFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    T("ModCleanup.CreateArchiveFailed", "Archivordner konnte nicht erstellt werden:") + "\n" + ex.Message,
                    T("ModCleanup.Title", "Modordner bereinigen"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            int moved = 0;
            var errors = new List<string>();

            foreach (var src in movable)
            {
                try
                {
                    if (!File.Exists(src.FullPath)) continue;
                    var dest = Path.Combine(archiveFolder, Path.GetFileName(src.FullPath));
                    dest = GetUniquePath(dest);
                    File.Move(src.FullPath, dest);
                    moved++;
                }
                catch (Exception ex)
                {
                    errors.Add(Path.GetFileName(src.FullPath) + ": " + ex.Message);
                }
            }

            RefreshList();

            if (errors.Count == 0)
            {
                MessageBox.Show(this,
                    string.Format(T("ModCleanup.MovedOk", "Verschoben: {0}"), moved)
                        + (displayOnly.Count > 0 ? "\n" + string.Format(T("ModCleanup.DisplayOnlySkipped", "Nicht verschoben (nur Anzeige): {0}"), displayOnly.Count) : ""),
                    T("ModCleanup.Title", "Modordner bereinigen"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this,
                    string.Format(T("ModCleanup.MovedPartial", "Verschoben: {0}\nFehler: {1}"), moved, errors.Count)
                        + (displayOnly.Count > 0 ? "\n" + string.Format(T("ModCleanup.DisplayOnlySkipped", "Nicht verschoben (nur Anzeige): {0}"), displayOnly.Count) : "")
                        + "\n\n" + string.Join("\n", errors.Take(12)) + (errors.Count > 12 ? "\n…" : ""),
                    T("ModCleanup.Title", "Modordner bereinigen"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private static string GetUniquePath(string path)
        {
            if (!File.Exists(path)) return path;

            var dir = Path.GetDirectoryName(path) ?? string.Empty;
            var name = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);

            for (int i = 1; i < 10_000; i++)
            {
                var candidate = Path.Combine(dir, $"{name} ({i}){ext}");
                if (!File.Exists(candidate)) return candidate;
            }

            // worst-case fallback
            return Path.Combine(dir, $"{name} ({DateTime.Now:yyyyMMdd_HHmmss}){ext}");
        }

        private static void TryOpenFolder(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch { }
        }

        private string T(string key, string fallback)
        {
            try
            {
                var v = _lang[key];
                if (!string.IsNullOrWhiteSpace(v) && !string.Equals(v, key, StringComparison.OrdinalIgnoreCase))
                    return v;
            }
            catch { }
            return fallback;
        }
    }
}

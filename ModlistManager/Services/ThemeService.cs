using System.Drawing;
using System.Windows.Forms;

namespace ETS2ATS.ModlistManager.Services
{
    public class ThemeService
    {
        public enum ThemeMode { Light, Dark }
        public ThemeMode Current { get; private set; } = ThemeMode.Light;

        public ThemeService() { }

        public void Set(ThemeMode mode) => Current = mode;

        // Einfaches Theme-Apply (rekursiv), ohne externe Abhängigkeiten
        public void Apply(Form form, string themeName)
        {
            var isDark = string.Equals(themeName, "Dark", System.StringComparison.OrdinalIgnoreCase);
            Current = isDark ? ThemeMode.Dark : ThemeMode.Light;

            var back = isDark ? Color.FromArgb(32, 32, 36) : SystemColors.Window;
            var fore = isDark ? Color.Gainsboro : SystemColors.ControlText;
            var ctlBack = isDark ? Color.FromArgb(40, 40, 44) : SystemColors.Control;

            form.BackColor = back;
            form.ForeColor = fore;

            ApplyToControlTree(form, back, fore, ctlBack, isDark);
        }

        private void ApplyToControlTree(Control root, Color back, Color fore, Color ctlBack, bool isDark)
        {
            if (root is MenuStrip ms)
            {
                ms.BackColor = ctlBack;
                ms.ForeColor = fore;
                foreach (ToolStripItem it in ms.Items)
                {
                    it.ForeColor = fore;
                }
            }
            else if (root is StatusStrip ss)
            {
                ss.BackColor = ctlBack;
                ss.ForeColor = fore;
                foreach (ToolStripItem it in ss.Items)
                    it.ForeColor = fore;
            }
            else if (root is ToolStrip ts)
            {
                ts.BackColor = ctlBack;
                ts.ForeColor = fore;
            }
            else if (root is Button btn)
            {
                btn.BackColor = ctlBack;
                btn.ForeColor = fore;
                if (isDark)
                {
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 0;
                }
                else
                {
                    btn.FlatStyle = FlatStyle.Standard;
                }
            }
            else if (root is DataGridView grid)
            {
                ApplyToDataGridView(grid, isDark);
            }
            else if (root is LinkLabel link)
            {
                // LinkLabel separat behandeln: Linkfarben für Dark/Light setzen
                link.BackColor = ctlBack;
                link.ForeColor = fore;

                if (isDark)
                {
                    // Helle, gut lesbare Linkfarben auf dunklem Grund
                    link.LinkColor = Color.SkyBlue;
                    link.ActiveLinkColor = Color.DeepSkyBlue;
                    link.VisitedLinkColor = Color.LightSkyBlue;
                    link.DisabledLinkColor = Color.Gray;
                }
                else
                {
                    // Systemnahe Standardfarben für helle Themes
                    link.LinkColor = Color.RoyalBlue;
                    link.ActiveLinkColor = Color.DodgerBlue;
                    link.VisitedLinkColor = Color.MediumPurple;
                    link.DisabledLinkColor = Color.Gray;
                }
            }
            else
            {
                // generisch
                root.BackColor = root is TextBox or ComboBox ? back : ctlBack;
                root.ForeColor = fore;
            }

            foreach (Control c in root.Controls)
                ApplyToControlTree(c, back, fore, ctlBack, isDark);
        }

        public void ApplyToDataGridView(DataGridView grid, bool isDark)
        {
            if (isDark)
            {
                grid.EnableHeadersVisualStyles = false;
                grid.BackgroundColor = Color.FromArgb(32, 32, 36);
                grid.BorderStyle = BorderStyle.FixedSingle;
                grid.GridColor = Color.FromArgb(64, 64, 70);

                grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 50);
                grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.Gainsboro;
                grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(64, 64, 70);

                grid.DefaultCellStyle.BackColor = Color.FromArgb(36, 36, 40);
                grid.DefaultCellStyle.ForeColor = Color.Gainsboro;
                grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(64, 64, 70);

                grid.RowHeadersDefaultCellStyle.BackColor = Color.FromArgb(36, 36, 40);
                grid.RowHeadersDefaultCellStyle.ForeColor = Color.Gainsboro;

                // Button-Spalten dunkler und flach darstellen
                var btnBack = Color.FromArgb(52, 52, 58);
                var btnSelBack = Color.FromArgb(72, 72, 80);
                foreach (DataGridViewColumn col in grid.Columns)
                {
                    if (col is DataGridViewButtonColumn bcol)
                    {
                        bcol.FlatStyle = FlatStyle.Flat;
                        bcol.DefaultCellStyle.BackColor = btnBack;
                        bcol.DefaultCellStyle.ForeColor = Color.Gainsboro;
                        bcol.DefaultCellStyle.SelectionBackColor = btnSelBack;
                        bcol.DefaultCellStyle.SelectionForeColor = Color.Gainsboro;
                    }
                }
            }
            else
            {
                grid.EnableHeadersVisualStyles = true;
                grid.BackgroundColor = SystemColors.Window;
                grid.BorderStyle = BorderStyle.FixedSingle;
                grid.GridColor = SystemColors.InactiveBorder;

                grid.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = SystemColors.ControlText;

                grid.DefaultCellStyle.BackColor = SystemColors.Window;
                grid.DefaultCellStyle.ForeColor = SystemColors.ControlText;

                grid.RowHeadersDefaultCellStyle.BackColor = SystemColors.Control;
                grid.RowHeadersDefaultCellStyle.ForeColor = SystemColors.ControlText;

                // Button-Spalten systemnah darstellen
                foreach (DataGridViewColumn col in grid.Columns)
                {
                    if (col is DataGridViewButtonColumn bcol)
                    {
                        // Use Flat to ensure per-cell coloring (e.g. missing-link highlight) is visible.
                        // With Standard style, Windows theming can ignore ForeColor/BackColor for buttons.
                        bcol.FlatStyle = FlatStyle.Flat;
                        bcol.DefaultCellStyle.BackColor = SystemColors.Control;
                        bcol.DefaultCellStyle.ForeColor = SystemColors.ControlText;
                        bcol.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
                        bcol.DefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;
                    }
                }
            }
        }
    }
}


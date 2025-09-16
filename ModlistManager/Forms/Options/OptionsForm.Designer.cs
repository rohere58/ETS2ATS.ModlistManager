using System.Drawing;
using System.Windows.Forms;

namespace ETS2ATS.ModlistManager.Forms.Options
{
    partial class OptionsForm
    {
        private TableLayoutPanel layout;
        private Label lblLanguage;
        private Label lblTheme;
        private Label lblPreferredGame;
        private Label lblEts2Path;
        private Label lblAtsPath;

        private ComboBox cbLanguage;
        private ComboBox cbTheme;
        private ComboBox cbPreferredGame;

        private TextBox txtEts2Path;
        private Button btnBrowseEts2;

        private TextBox txtAtsPath;
        private Button btnBrowseAts;

        private FlowLayoutPanel panelButtons;
        private Button btnOK;
        private Button btnCancel;
    private CheckBox chkConfirmBeforeAdopt;

        private void InitializeComponent()
        {
            layout = new TableLayoutPanel();
            lblLanguage = new Label();
            lblTheme = new Label();
            lblPreferredGame = new Label();
            lblEts2Path = new Label();
            lblAtsPath = new Label();

            cbLanguage = new ComboBox();
            cbTheme = new ComboBox();
            cbPreferredGame = new ComboBox();

            txtEts2Path = new TextBox();
            btnBrowseEts2 = new Button();

            txtAtsPath = new TextBox();
            btnBrowseAts = new Button();

            panelButtons = new FlowLayoutPanel();
            btnOK = new Button();
            btnCancel = new Button();

            SuspendLayout();

            // layout
            layout.ColumnCount = 3;
            layout.RowCount = 7;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));           // Label
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));      // Input
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));           // Browse/Spacer
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(10);
            layout.AutoSize = true;
            // --- Layout-Feinschliff (optional, verhindert seltsames Zusammenquetschen) ---
            layout.GrowStyle = TableLayoutPanelGrowStyle.AddRows;

            // Labels - mit Tags f�r sp�tere Lokalisierung
            lblLanguage.Text = "Sprache";
            lblLanguage.Tag = "Options.Language";
            lblLanguage.TextAlign = ContentAlignment.MiddleLeft;
            lblLanguage.Anchor = AnchorStyles.Left;

            lblTheme.Text = "Theme";
            lblTheme.Tag = "Options.Theme";
            lblTheme.TextAlign = ContentAlignment.MiddleLeft;
            lblTheme.Anchor = AnchorStyles.Left;

            lblPreferredGame.Text = "Bevorzugtes Spiel";
            lblPreferredGame.Tag = "Options.PreferredGame";
            lblPreferredGame.TextAlign = ContentAlignment.MiddleLeft;
            lblPreferredGame.Anchor = AnchorStyles.Left;

            lblEts2Path.Text = "ETS2-Profile Ordner";
            lblEts2Path.Tag = "Options.Ets2Path";
            lblEts2Path.TextAlign = ContentAlignment.MiddleLeft;
            lblEts2Path.Anchor = AnchorStyles.Left;

            lblAtsPath.Text = "ATS-Profile Ordner";
            lblAtsPath.Tag = "Options.AtsPath";
            lblAtsPath.TextAlign = ContentAlignment.MiddleLeft;
            lblAtsPath.Anchor = AnchorStyles.Left;

            // Checkbox: Bestätigung vor Übernehmen
            chkConfirmBeforeAdopt = new CheckBox();
            chkConfirmBeforeAdopt.Text = "Bestätigung vor Übernehmen";
            chkConfirmBeforeAdopt.Tag = "Options.ConfirmBeforeAdopt";
            chkConfirmBeforeAdopt.Anchor = AnchorStyles.Left;

            // Combos
            cbLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cbLanguage.Width = 240;

            cbTheme.DropDownStyle = ComboBoxStyle.DropDownList;
            cbTheme.Width = 240;

            cbPreferredGame.DropDownStyle = ComboBoxStyle.DropDownList;
            cbPreferredGame.Width = 240;

            // Paths
            txtEts2Path.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtAtsPath.Anchor  = AnchorStyles.Left | AnchorStyles.Right;

            // Browse-Buttons: nie abgeschnitten
            btnBrowseEts2.Text = "Durchsuchen�";
            btnBrowseEts2.Tag = "Options.Browse";
            btnBrowseEts2.AutoSize = true;
            btnBrowseEts2.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnBrowseEts2.MinimumSize = new Size(110, 0);  // Sicherheitsnetz
            btnBrowseEts2.Anchor = AnchorStyles.Left;

            btnBrowseAts.Text = "Durchsuchen�";
            btnBrowseAts.Tag = "Options.Browse";
            btnBrowseAts.AutoSize = true;
            btnBrowseAts.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnBrowseAts.MinimumSize = new Size(110, 0);   // Sicherheitsnetz
            btnBrowseAts.Anchor = AnchorStyles.Left;

            // Buttons
            panelButtons.FlowDirection = FlowDirection.RightToLeft;
            panelButtons.Dock = DockStyle.Bottom;
            panelButtons.Padding = new Padding(10, 0, 10, 10);
            panelButtons.Height = 46;

            btnOK.Text = "OK";
            btnOK.Tag = "Common.OK";
            btnOK.AutoSize = true;
            btnOK.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            btnCancel.Text = "Abbrechen";
            btnCancel.Tag = "Common.Cancel";
            btnCancel.AutoSize = true;
            btnCancel.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            panelButtons.Controls.AddRange(new Control[] { btnCancel, btnOK });

            // Layout-Zuordnung
            int r = 0;
            layout.Controls.Add(lblLanguage, 0, r);
            layout.Controls.Add(cbLanguage, 1, r++);
            layout.SetColumnSpan(cbLanguage, 2);

            layout.Controls.Add(lblTheme, 0, r);
            layout.Controls.Add(cbTheme, 1, r++);
            layout.SetColumnSpan(cbTheme, 2);

            layout.Controls.Add(lblPreferredGame, 0, r);
            layout.Controls.Add(cbPreferredGame, 1, r++);
            layout.SetColumnSpan(cbPreferredGame, 2);

            layout.Controls.Add(lblEts2Path, 0, r);
            layout.Controls.Add(txtEts2Path, 1, r);
            layout.Controls.Add(btnBrowseEts2, 2, r++);
            layout.SetColumnSpan(txtEts2Path, 1);

            layout.Controls.Add(lblAtsPath, 0, r);
            layout.Controls.Add(txtAtsPath, 1, r);
            layout.Controls.Add(btnBrowseAts, 2, r++);
            layout.SetColumnSpan(txtAtsPath, 1);

            // Checkbox über gesamte Breite
            layout.Controls.Add(chkConfirmBeforeAdopt, 0, r);
            layout.SetColumnSpan(chkConfirmBeforeAdopt, 3);
            r++;

            // Dock-Reihenfolge: erst Bottom, dann Fill (so quetscht der Footer nichts)
            Controls.Add(panelButtons);
            Controls.Add(layout);

            // Form
            Text = "Optionen";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ClientSize = new Size(630, 280);   // vorher: 590
            MinimumSize = new Size(630, 280);  // MinSize, damit nichts einklappt
            AutoScaleMode = AutoScaleMode.Dpi;

            ResumeLayout(false);
            PerformLayout();
        }
    }
}
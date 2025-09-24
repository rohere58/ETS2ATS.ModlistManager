using System.Drawing;
using System.Windows.Forms;

namespace ETS2ATS.ModlistManager.Forms.About
{
    partial class AboutForm
    {
        private Label lblTitle;
        private TableLayoutPanel layout;
        private Label lblAppName, lblVersion, lblAuthor, lblTech, lblAppNameValue, lblVersionValue, lblAuthorValue, lblTechValue;
        private TextBox txtDescription;
        private Button btnOK;

        private void InitializeComponent()
        {
            lblTitle = new Label();
            layout = new TableLayoutPanel();
            lblAppName = new Label();
            lblVersion = new Label();
            lblAuthor = new Label();
            lblTech = new Label();
            lblAppNameValue = new Label();
            lblVersionValue = new Label();
            lblAuthorValue = new Label();
            lblTechValue = new Label();
            txtDescription = new TextBox();
            btnOK = new Button();
            SuspendLayout();

            // lblTitle
            lblTitle.Dock = DockStyle.Top;
            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTitle.Location = new Point(0, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(520, 36);
            lblTitle.TabIndex = 0;
            lblTitle.Tag = "About.Title";
            lblTitle.Text = "Über dieses Programm";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;

            // layout
            layout.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Location = new Point(12, 48);
            layout.Name = "layout";
            layout.RowCount = 4;
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.Size = new Size(496, 108);
            layout.TabIndex = 1;

            // Labels links
            lblAppName.AutoSize = true; lblAppName.Tag = "About.AppName"; lblAppName.Text = "Programm:";
            lblVersion.AutoSize = true; lblVersion.Tag = "About.Version"; lblVersion.Text = "Version:";
            lblAuthor.AutoSize = true; lblAuthor.Tag = "About.Author"; lblAuthor.Text = "Autor:";
            lblTech.AutoSize = true; lblTech.Tag = "About.Tech"; lblTech.Text = "Technik:";

            // Values rechts
            lblAppNameValue.AutoSize = true; lblAppNameValue.Text = "ETS2/ATS Modlist Manager"; lblAppNameValue.Tag = "About.AppName.Value";
            lblVersionValue.AutoSize = true; lblVersionValue.Text = ""; lblVersionValue.Tag = "About.Version.Value"; // dynamisch befüllt
            lblAuthorValue.AutoSize = true; lblAuthorValue.Text = "Winnie (rohere58)"; lblAuthorValue.Tag = "About.Author.Value";
            lblTechValue.AutoSize = true; lblTechValue.Text = "C# (.NET 8, WinForms)"; lblTechValue.Tag = "About.Tech.Value";

            layout.Controls.Add(lblAppName, 0, 0); layout.Controls.Add(lblAppNameValue, 1, 0);
            layout.Controls.Add(lblVersion, 0, 1);  layout.Controls.Add(lblVersionValue, 1, 1);
            layout.Controls.Add(lblAuthor, 0, 2);   layout.Controls.Add(lblAuthorValue, 1, 2);
            layout.Controls.Add(lblTech, 0, 3);     layout.Controls.Add(lblTechValue, 1, 3);

            // txtDescription
            txtDescription.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            txtDescription.Location = new Point(12, 168);
            txtDescription.Multiline = true;
            txtDescription.ReadOnly = true;
            txtDescription.ScrollBars = ScrollBars.Vertical;
            txtDescription.Size = new Size(496, 120);
            txtDescription.Tag = "About.Description";
            txtDescription.Text = "Einfacher Manager für ETS2/ATS Modlisten.";

            // btnOK
            btnOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Location = new Point(433, 300);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(75, 28);
            btnOK.TabIndex = 3;
            btnOK.Tag = "Common.OK";
            btnOK.Text = "OK";

            // AboutForm
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(520, 340);
            Controls.Add(btnOK);
            Controls.Add(txtDescription);
            Controls.Add(layout);
            Controls.Add(lblTitle);
            Font = new Font("Segoe UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AboutForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Über";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}

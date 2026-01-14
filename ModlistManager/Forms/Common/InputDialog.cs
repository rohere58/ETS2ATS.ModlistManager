using System;
using System.Drawing;
using System.Windows.Forms;

namespace ETS2ATS.ModlistManager.Forms.Common
{
    internal sealed class InputDialog : Form
    {
        private readonly Label lblPrompt;
        private readonly TextBox txtValue;
        private readonly Button btnOk;
        private readonly Button btnCancel;
        private readonly TableLayoutPanel layout;

        public string Value => txtValue.Text;

        private InputDialog(string title, string prompt, string defaultValue)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;

            layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(12),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            lblPrompt = new Label
            {
                Text = prompt,
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 6),
            };

            txtValue = new TextBox
            {
                Text = defaultValue ?? string.Empty,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 12),
            };

            btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                AutoSize = true,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 0, 8, 0),
            };

            btnCancel = new Button
            {
                Text = "Abbrechen",
                DialogResult = DialogResult.Cancel,
                AutoSize = true,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0),
            };

            // Layout: Prompt
            layout.Controls.Add(lblPrompt, 0, 0);
            layout.SetColumnSpan(lblPrompt, 2);

            // Layout: TextBox
            layout.Controls.Add(txtValue, 0, 1);
            layout.SetColumnSpan(txtValue, 2);

            // Layout: Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0),
            };
            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnOk);

            layout.Controls.Add(buttonPanel, 0, 2);
            layout.SetColumnSpan(buttonPanel, 2);

            Controls.Add(layout);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            Shown += (_, __) =>
            {
                try
                {
                    txtValue.SelectionStart = 0;
                    txtValue.SelectionLength = txtValue.TextLength;
                    txtValue.Focus();
                }
                catch { }
            };
        }

        public static DialogResult ShowDialog(IWin32Window owner, string title, string prompt, string defaultValue,
            string okText, string cancelText, out string value)
        {
            using var dlg = new InputDialog(title, prompt, defaultValue);
            try { dlg.btnOk.Text = okText; } catch { }
            try { dlg.btnCancel.Text = cancelText; } catch { }

            var res = dlg.ShowDialog(owner);
            value = dlg.Value ?? string.Empty;
            return res;
        }
    }
}

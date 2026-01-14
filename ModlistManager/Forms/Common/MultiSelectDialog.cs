using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ETS2ATS.ModlistManager.Forms.Common
{
    internal sealed class MultiSelectDialog : Form
    {
        private readonly Label lblPrompt;
        private readonly CheckedListBox clb;
        private readonly Button btnSelectAll;
        private readonly Button btnSelectNone;
        private readonly Button btnOk;
        private readonly Button btnCancel;

        public IReadOnlyList<object> SelectedItems
            => clb.CheckedItems.Cast<object>().ToList();

        private MultiSelectDialog(string title, string prompt)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;

            ClientSize = new Size(520, 420);

            lblPrompt = new Label
            {
                Text = prompt,
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 44,
                Padding = new Padding(12, 10, 12, 0),
            };

            clb = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                IntegralHeight = false,
                Margin = new Padding(12),
            };

            var topButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(12, 0, 12, 0),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
            };

            btnSelectAll = new Button { Text = "Alle", AutoSize = true };
            btnSelectNone = new Button { Text = "Keine", AutoSize = true };
            btnSelectAll.Click += (_, __) => SetAllChecked(true);
            btnSelectNone.Click += (_, __) => SetAllChecked(false);
            topButtons.Controls.Add(btnSelectAll);
            topButtons.Controls.Add(btnSelectNone);

            var bottomButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                Padding = new Padding(12, 6, 12, 6),
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
            };

            btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
            btnCancel = new Button { Text = "Abbrechen", DialogResult = DialogResult.Cancel, AutoSize = true };
            bottomButtons.Controls.Add(btnCancel);
            bottomButtons.Controls.Add(btnOk);

            Controls.Add(clb);
            Controls.Add(bottomButtons);
            Controls.Add(topButtons);
            Controls.Add(lblPrompt);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            Shown += (_, __) => { try { clb.Focus(); } catch { } };
        }

        private void SetAllChecked(bool value)
        {
            try
            {
                clb.BeginUpdate();
                for (int i = 0; i < clb.Items.Count; i++)
                {
                    clb.SetItemChecked(i, value);
                }
            }
            finally
            {
                try { clb.EndUpdate(); } catch { }
            }
        }

        public static DialogResult ShowDialog(IWin32Window owner,
            string title,
            string prompt,
            IEnumerable<object> items,
            Func<object, string> display,
            IEnumerable<object>? prechecked,
            string okText,
            string cancelText,
            string selectAllText,
            string selectNoneText,
            out IReadOnlyList<object> selected)
        {
            using var dlg = new MultiSelectDialog(title, prompt);

            try { dlg.btnOk.Text = okText; } catch { }
            try { dlg.btnCancel.Text = cancelText; } catch { }
            try { dlg.btnSelectAll.Text = selectAllText; } catch { }
            try { dlg.btnSelectNone.Text = selectNoneText; } catch { }

            var pre = new HashSet<object>(prechecked ?? Array.Empty<object>());

            try
            {
                dlg.clb.BeginUpdate();
                foreach (var it in items)
                {
                    var idx = dlg.clb.Items.Add(it);
                    if (pre.Contains(it)) dlg.clb.SetItemChecked(idx, true);
                }
            }
            finally
            {
                try { dlg.clb.EndUpdate(); } catch { }
            }

            dlg.clb.Format += (_, e) =>
            {
                try
                {
                    if (e.ListItem != null) e.Value = display(e.ListItem);
                }
                catch { }
            };

            var res = dlg.ShowDialog(owner);
            selected = dlg.SelectedItems;
            return res;
        }
    }
}

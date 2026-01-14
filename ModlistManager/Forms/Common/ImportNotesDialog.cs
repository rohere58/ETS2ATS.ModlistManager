using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ETS2ATS.ModlistManager.Forms.Common
{
    internal sealed class ImportNotesDialog : Form
    {
        private readonly Label lblIntro;
        private readonly ListView lv;
        private readonly TextBox txt;
        private readonly Button btnOk;

        private readonly List<Entry> entries;

        internal sealed class Entry
        {
            public string Title { get; }
            public string Note { get; }

            public Entry(string title, string note)
            {
                Title = title ?? string.Empty;
                Note = note ?? string.Empty;
            }
        }

        private ImportNotesDialog(string title, string intro, string closeText, IReadOnlyList<Entry> entries)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MaximizeBox = true;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;

            this.entries = entries.ToList();

            ClientSize = new Size(760, 480);

            lblIntro = new Label
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(12, 10, 12, 0),
                Text = intro,
                AutoEllipsis = true,
            };

            lv = new ListView
            {
                Dock = DockStyle.Left,
                Width = 260,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = false,
            };
            lv.Columns.Add("", -2);

            txt = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = (SystemFonts.MessageBoxFont != null)
                    ? new Font(SystemFonts.MessageBoxFont.FontFamily, SystemFonts.MessageBoxFont.Size)
                    : new Font(FontFamily.GenericSansSerif, 9f),
            };

            Panel bottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                Padding = new Padding(12, 8, 12, 8),
            };

            btnOk = new Button
            {
                Text = closeText,
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                AutoSize = true,
            };
            bottom.Controls.Add(btnOk);
            bottom.Resize += (_, __) =>
            {
                try
                {
                    var b = bottom;
                    if (b == null) return;
                    btnOk.Left = b.ClientSize.Width - btnOk.Width;
                    btnOk.Top = (b.ClientSize.Height - btnOk.Height) / 2;
                }
                catch { }
            };

            Controls.Add(txt);
            Controls.Add(lv);
            Controls.Add(bottom);
            Controls.Add(lblIntro);

            AcceptButton = btnOk;

            lv.SelectedIndexChanged += (_, __) => ShowSelected();
            Shown += (_, __) =>
            {
                try
                {
                    Populate();
                    if (lv.Items.Count > 0) lv.Items[0].Selected = true;
                    lv.Focus();
                }
                catch { }
            };
        }

        private void Populate()
        {
            lv.BeginUpdate();
            try
            {
                lv.Items.Clear();
                foreach (var e in entries)
                {
                    var it = new ListViewItem(e.Title) { Tag = e };
                    lv.Items.Add(it);
                }

                if (lv.Columns.Count > 0) lv.Columns[0].Width = -2;
            }
            finally
            {
                lv.EndUpdate();
            }
        }

        private void ShowSelected()
        {
            try
            {
                var entry = lv.SelectedItems.Cast<ListViewItem>().FirstOrDefault()?.Tag as Entry;
                if (entry == null)
                {
                    txt.Text = string.Empty;
                    return;
                }

                txt.Text = entry.Note;
            }
            catch
            {
                txt.Text = string.Empty;
            }
        }

        public static void Show(IWin32Window owner,
            string title,
            string intro,
            string closeText,
            IReadOnlyList<Entry> entries,
            string emptyNoteText)
        {
            if (entries == null || entries.Count == 0) return;

            // Normalize notes (avoid null + provide placeholder)
            var norm = entries
                .Select(e => new Entry(e.Title, string.IsNullOrWhiteSpace(e.Note) ? emptyNoteText : e.Note))
                .ToList();

            using var dlg = new ImportNotesDialog(title, intro, closeText, norm);
            dlg.ShowDialog(owner);
        }
    }
}

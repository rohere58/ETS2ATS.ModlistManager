using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ETS2ATS.ModlistManager.Services;

namespace ETS2ATS.ModlistManager.Forms.Main
{
    public class FaqForm : Form
    {
        private readonly LanguageService _lang;
        private readonly string _langCode;
        private readonly RichTextBox _rtb;
        private readonly Button _btnClose;
        private readonly WebBrowser _browser; // einfacher Renderer für HTML

        public FaqForm(LanguageService lang)
        {
            _lang = lang;
            _langCode = lang.CurrentCode;
            Text = _lang["Faq.Title"];
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9F);
            MinimumSize = new Size(600, 400);
            Size = new Size(760, 520);

            _rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = SystemColors.Window,
                DetectUrls = true,
                Font = new Font("Consolas", 10f),
                HideSelection = false,
                Visible = false // Standard: wir versuchen erst Markdown
            };

            _browser = new WebBrowser
            {
                Dock = DockStyle.Fill,
                AllowWebBrowserDrop = false,
                IsWebBrowserContextMenuEnabled = false,
                ScriptErrorsSuppressed = true,
                WebBrowserShortcutsEnabled = true
            };

            _btnClose = new Button
            {
                Text = _lang["Common.Close"],
                Dock = DockStyle.Right,
                Width = 100,
                Height = 30,
                Margin = new Padding(8)
            };
            _btnClose.Click += (s, e) => Close();

            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 42,
                Padding = new Padding(8)
            };
            bottomPanel.Controls.Add(_btnClose);

            Controls.Add(_browser);
            Controls.Add(_rtb); // rtb unten für Fallback
            Controls.Add(bottomPanel);

            LoadContent();
        }

        private void LoadContent()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                // Kandidatenlisten aufbauen (Markdown priorisiert)
                var mdList = new System.Collections.Generic.List<string>();
                void AddMd(string relative)
                {
                    mdList.Add(Path.Combine(baseDir, relative));
                }
                // Häufige Layouts: nach Publish können Ressourcen direkt unter Resources oder weiter unter ModlistManager/Resources liegen
                // Wunsch: FAQ bevorzugt immer auf Englisch anzeigen, wenn verfügbar.
                AddMd($"ModlistManager{Path.DirectorySeparatorChar}Resources{Path.DirectorySeparatorChar}faq.en.md");
                AddMd($"ModlistManager{Path.DirectorySeparatorChar}Resources{Path.DirectorySeparatorChar}faq.{_langCode}.md");
                AddMd($"Resources{Path.DirectorySeparatorChar}faq.en.md");
                AddMd($"Resources{Path.DirectorySeparatorChar}faq.{_langCode}.md");
                AddMd("faq.en.md");
                AddMd($"faq.{_langCode}.md");

                foreach (var candidate in mdList)
                {
                    if (File.Exists(candidate))
                    {
                        var mdText = File.ReadAllText(candidate);
                        var html = RenderMarkdownToHtml(mdText, IsDarkTheme());
                        _browser.DocumentText = html;
                        return;
                    }
                }

                // TXT Fallback analog
                var txtList = new System.Collections.Generic.List<string>();
                void AddTxt(string relative) => txtList.Add(Path.Combine(baseDir, relative));
                AddTxt($"ModlistManager{Path.DirectorySeparatorChar}Resources{Path.DirectorySeparatorChar}faq.en.txt");
                AddTxt($"ModlistManager{Path.DirectorySeparatorChar}Resources{Path.DirectorySeparatorChar}faq.{_langCode}.txt");
                AddTxt($"Resources{Path.DirectorySeparatorChar}faq.en.txt");
                AddTxt($"Resources{Path.DirectorySeparatorChar}faq.{_langCode}.txt");
                AddTxt("faq.en.txt");
                AddTxt($"faq.{_langCode}.txt");

                foreach (var candidate in txtList)
                {
                    if (File.Exists(candidate))
                    {
                        _rtb.Text = File.ReadAllText(candidate);
                        _rtb.SelectionStart = 0;
                        _rtb.SelectionLength = 0;
                        _rtb.Visible = true;
                        _browser.Visible = false;
                        return;
                    }
                }

                // Diagnose: welche Pfade wurden versucht? (nur in Debug)
                #if DEBUG
                _rtb.Text = "(FAQ file not found.)\nTried (md):\n" + string.Join("\n", mdList) + "\n---\nTried (txt):\n" + string.Join("\n", txtList);
                #else
                _rtb.Text = "(FAQ file not found.)";
                #endif
                _rtb.Visible = true;
                _browser.Visible = false;
            }
            catch (Exception ex)
            {
                _rtb.Text = "Error loading FAQ: " + ex.Message;
                _rtb.Visible = true;
                _browser.Visible = false;
            }
        }

        private bool IsDarkTheme()
        {
            // Heuristik: Fenster-Hintergrund dunkel?
            var c = this.BackColor;
            int l = (c.R + c.G + c.B) / 3;
            return l < 100; // sehr einfache Einschätzung
        }

        /// <summary>
        /// Minimaler Markdown Parser (bewusst simpel, keine vollständige Spezifikation)
        /// Unterstützt: #, ##, ###, Listen (-, *), **bold**, *italic*, `code`.
        /// </summary>
        private string RenderMarkdownToHtml(string md, bool dark)
        {
            // Zuerst alle Zeilen einlesen um Headings für ToC sammeln zu können
            var lines = md.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            var headingList = new System.Collections.Generic.List<(int level,string text,string slug)>();
            bool manualTocPresent = false;
            foreach (var raw in lines)
            {
                var t = raw.Trim();
                if (t.StartsWith("# ") || t.StartsWith("## ") || t.StartsWith("### "))
                {
                    int lvl = t.StartsWith("### ") ? 3 : t.StartsWith("## ") ? 2 : 1;
                    string txt = lvl==3? t.Substring(4): lvl==2? t.Substring(3): t.Substring(2);

                    // Manuelles Inhaltsverzeichnis erkennen (z.B. "## Inhalt" oder "## Table of Contents")
                    if (txt.Equals("Inhalt", StringComparison.OrdinalIgnoreCase)
                        || txt.Equals("Inhaltsverzeichnis", StringComparison.OrdinalIgnoreCase)
                        || txt.Equals("Table of Contents", StringComparison.OrdinalIgnoreCase)
                        || txt.Equals("Contents", StringComparison.OrdinalIgnoreCase))
                    {
                        manualTocPresent = true;
                    }

                    string slug = MakeSlug(txt);
                    // Slug-Kollision vermeiden
                    string orig = slug; int counter=2;
                    while (headingList.Exists(h => h.slug==slug)) { slug = orig + "-" + counter++; }
                    headingList.Add((lvl, txt, slug));
                }
                if (t.Contains("[Inhalt]")
                    || t.Contains("[Inhaltsverzeichnis]")
                    || t.Contains("Table of Contents", StringComparison.OrdinalIgnoreCase)
                    || t.Contains("Contents", StringComparison.OrdinalIgnoreCase))
                    manualTocPresent = true;
            }

            var sb = new System.Text.StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'/><title>FAQ</title><style>");
            sb.Append(dark ?
                "body{font-family:Segoe UI,Arial,sans-serif;background:#1e1f22;color:#e6e6e6;margin:16px;line-height:1.35;}" :
                "body{font-family:Segoe UI,Arial,sans-serif;background:#ffffff;color:#222;margin:16px;line-height:1.35;}");
            sb.Append("h1{font-size:1.6em;margin-top:0.2em;}h2{font-size:1.3em;margin-top:1.2em;}h3{font-size:1.15em;margin-top:1.1em;}p{margin:0.6em 0;}ul{margin:0.6em 0 0.6em 1.4em;}code{background:" + (dark?"#2d2f33":"#f1f3f5") + ";padding:2px 4px;border-radius:4px;font-family:Consolas,monospace;font-size:0.95em;}hr{border:0;border-top:1px solid " + (dark?"#444":"#ccc") + ";margin:1.2em 0;} a{color:" + (dark?"#58a6ff":"#0a63c7") + ";text-decoration:none;} a:hover{text-decoration:underline;}");
            // TOC CSS
            sb.Append("nav.toc{border:1px solid "+(dark?"#444":"#ccc")+";padding:12px 14px;border-radius:8px;background:"+(dark?"#262a2e":"#f8f9fa")+";margin:0 0 18px 0;}nav.toc h2{margin:0 0 8px 0;font-size:1.05em;}nav.toc ul{margin:0;padding-left:18px;}nav.toc li{margin:2px 0;font-size:0.95em;}nav.toc a{text-decoration:none;}nav.toc a:hover{text-decoration:underline;}");
            sb.Append("</style></head><body>");

            // Automatisches TOC (falls mehrere Überschriften und kein manueller ToC)
            if (!manualTocPresent && headingList.Count > 1)
            {
                var tocTitle = _lang["Faq.Contents"];
                if (string.IsNullOrWhiteSpace(tocTitle) || tocTitle == "Faq.Contents")
                    tocTitle = "Contents";
                sb.Append("<nav class='toc'><h2>").Append(EscapeHtml(tocTitle)).Append("</h2><ul>");
                int lastLevel = 0;
                foreach (var h in headingList)
                {
                    // simple flache Liste (Optional: Einrückung nach Level)
                    sb.Append("<li>").Append("<a href='#").Append(h.slug).Append("'>").Append(EscapeHtml(h.text)).Append("</a></li>");
                    lastLevel = h.level;
                }
                sb.Append("</ul></nav>");
            }

            // Zweiter Durchlauf: Inhalt rendern (indexbasiert, damit Listenzeilen sauber übersprungen werden)
            for (int i = 0; i < lines.Length; i++)
            {
                var raw = lines[i];
                if (string.IsNullOrWhiteSpace(raw)) { sb.Append("<p></p>"); continue; }

                var trimmed = raw.Trim();
                // Headings
                if (trimmed.StartsWith("### ")) { var txt = trimmed.Substring(4); var slug = GetSlugFor(headingList, 3, txt); sb.Append("<h3 id='" + slug + "'>").Append(ParseInline(txt)).Append("</h3>"); continue; }
                if (trimmed.StartsWith("## ")) { var txt = trimmed.Substring(3); var slug = GetSlugFor(headingList, 2, txt); sb.Append("<h2 id='" + slug + "'>").Append(ParseInline(txt)).Append("</h2>"); continue; }
                if (trimmed.StartsWith("# ")) { var txt = trimmed.Substring(2); var slug = GetSlugFor(headingList, 1, txt); sb.Append("<h1 id='" + slug + "'>").Append(ParseInline(txt)).Append("</h1>"); continue; }
                if (trimmed == "---" || trimmed == "***" || trimmed == "___") { sb.Append("<hr/>"); continue; }

                if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
                {
                    // Liste sammeln (alle unmittelbar folgenden Listenelemente aufnehmen)
                    var items = new System.Collections.Generic.List<string>();
                    int j = i;
                    while (j < lines.Length)
                    {
                        var nraw = lines[j];
                        if (string.IsNullOrWhiteSpace(nraw)) break;
                        var ntrim = nraw.TrimStart();
                        if (ntrim.StartsWith("- ") || ntrim.StartsWith("* "))
                        {
                            items.Add(ntrim.Substring(2));
                            j++;
                            continue;
                        }
                        break;
                    }

                    sb.Append("<ul>");
                    foreach (var it in items)
                        sb.Append("<li>").Append(ParseInline(it)).Append("</li>");
                    sb.Append("</ul>");

                    // bereits verarbeitete Zeilen überspringen
                    i = j - 1;
                    continue;
                }

                sb.Append("<p>").Append(ParseInline(trimmed)).Append("</p>");
            }

            sb.Append("</body></html>");
            return sb.ToString();
        }

        private static string MakeSlug(string text)
        {
            text = text.Trim().ToLowerInvariant();
            var sb = new System.Text.StringBuilder();
            foreach (var ch in text)
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(ch);
                else if (char.IsWhiteSpace(ch) || ch=='-' || ch=='_') sb.Append('-');
            }
            var slug = sb.ToString();
            while (slug.Contains("--")) slug = slug.Replace("--", "-");
            if (slug.StartsWith("-")) slug = slug.TrimStart('-');
            if (slug.EndsWith("-")) slug = slug.TrimEnd('-');
            if (string.IsNullOrEmpty(slug)) slug = "section";
            return slug;
        }

        private static string GetSlugFor(System.Collections.Generic.List<(int level,string text,string slug)> list, int level, string text)
        {
            foreach (var h in list)
                if (h.level == level && string.Equals(h.text, text, StringComparison.Ordinal))
                    return h.slug;
            return MakeSlug(text); // Fallback
        }

        private string ParseInline(string text)
        {
            // Code spans `code`
            text = System.Text.RegularExpressions.Regex.Replace(text, "`([^`]+)`", m => "<code>" + EscapeHtml(m.Groups[1].Value) + "</code>");
            // Bold **text**
            text = System.Text.RegularExpressions.Regex.Replace(text, "\\*\\*([^*]+)\\*\\*", "<strong>$1</strong>");
            // Italic *text*
            text = System.Text.RegularExpressions.Regex.Replace(text, "(?<!\\*)\\*([^*]+)\\*(?!\\*)", "<em>$1</em>");
            // Links [text](url)
            text = System.Text.RegularExpressions.Regex.Replace(text, "\\[([^\\]]+)\\]\\(([^)]+)\\)", "<a href='$2'>$1</a>");
            return text;
        }

        private static string EscapeHtml(string s)
            => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }

    internal static class TextReaderExtensions
    {
        public static string? PeekLine(this TextReader reader)
        {
            if (reader is StringReader sr)
            {
                var posField = typeof(StringReader).GetField("_pos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var lenField = typeof(StringReader).GetField("_length", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var strField = typeof(StringReader).GetField("_s", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (posField != null && lenField != null && strField != null)
                {
                    int pos = (int)posField.GetValue(sr)!;
                    string s = (string)strField.GetValue(sr)!;
                    if (pos >= s.Length) return null;
                    int nl = s.IndexOf('\n', pos);
                    if (nl < 0) nl = s.Length;
                    return s.Substring(pos, nl - pos).TrimEnd('\r');
                }
            }
            return null; // Fallback nicht unterstützt
        }
    }
}

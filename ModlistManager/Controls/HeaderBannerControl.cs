using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace ETS2ATS.ModlistManager.Controls
{
    public class HeaderBannerControl : Control
    {
        private bool _darkTheme;
        private string? _gameCode;
        private Image? _ets2Logo;
        private Image? _atsLogo;
    private readonly System.Windows.Forms.Timer _animTimer = new System.Windows.Forms.Timer();
    private float _glossPhase = 0f; // 0..1
    private bool _glossEnabled = true;
        private float _backgroundOpacity = 1f; // 0..1

        // Optional: Text im Banner
        public string? Title { get; set; }
        public string? Subtitle { get; set; }

        public string? GameCode
        {
            get => _gameCode;
            set
            {
                if (_gameCode == value) return;
                _gameCode = value;
                EnsureLogosLoaded();
                Invalidate();
            }
        }

        /// <summary>
        /// Opazität des Banner-Hintergrundes (0 = komplett transparent, 1 = voll deckend).
        /// Beeinflusst den Verlauf sowie die Straße inkl. Markierungen.
        /// Logos und Text bleiben unverändert halbtransparent/normal, um Lesbarkeit zu erhalten.
        /// </summary>
        public float BackgroundOpacity
        {
            get => _backgroundOpacity;
            set
            {
                var v = Math.Max(0f, Math.Min(1f, value));
                if (Math.Abs(v - _backgroundOpacity) < 0.0001f) return;
                _backgroundOpacity = v;
                Invalidate();
            }
        }

        public HeaderBannerControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            Height = 90;
            Cursor = Cursors.Default;

            _animTimer.Interval = 60; // ~16 FPS (schonender)
            _animTimer.Tick += (s, e) => { _glossPhase += 0.01f; if (_glossPhase > 1f) _glossPhase -= 1f; Invalidate(); };
            _animTimer.Start();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            try
            {
                if (Visible) _animTimer.Start();
                else _animTimer.Stop();
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { _animTimer.Stop(); } catch { }
                try { _animTimer.Dispose(); } catch { }
                _ets2Logo?.Dispose();
                _atsLogo?.Dispose();
            }
            base.Dispose(disposing);
        }

        public void ApplyTheme(bool dark)
        {
            _darkTheme = dark;
            Invalidate();
        }

        private void EnsureLogosLoaded()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                if (_ets2Logo == null)
                {
                    // Reihenfolge geändert: zuerst benutzerdefiniertes override 'logo.png', dann Standard 'ets2.png'
                    _ets2Logo = TryLoadImage(
                        Path.Combine(baseDir, "Resources", "Logos", "logo.png"),
                        Path.Combine(baseDir, "Resources", "logo.png"),
                        Path.Combine(baseDir, "Resources", "Logos", "ets2.png"),
                        Path.Combine(baseDir, "Resources", "ets2.png")
                    );
                }
                if (_atsLogo == null)
                {
                    // Neu: zuerst logo_ats.png, dann ats.png
                    _atsLogo = TryLoadImage(
                        Path.Combine(baseDir, "Resources", "Logos", "logo_ats.png"),
                        Path.Combine(baseDir, "Resources", "Logos", "ats.png"),
                        Path.Combine(baseDir, "Resources", "ats.png")
                    );
                }
            }
            catch { /* ignore */ }
        }

        private static Image? TryLoadImage(params string[] candidates)
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = ClientRectangle;
            if (rect.Width <= 2 || rect.Height <= 2)
                return;

            // Hintergrundverlauf (mit globaler Opazität)
            Color c1, c2;
            if (_darkTheme)
            {
                c1 = Color.FromArgb(36, 36, 42);
                c2 = Color.FromArgb(22, 22, 26);
            }
            else
            {
                c1 = Color.FromArgb(245, 248, 252);
                c2 = Color.FromArgb(230, 235, 240);
            }
            if (_backgroundOpacity < 1f)
            {
                int a = (int)(255 * _backgroundOpacity);
                c1 = Color.FromArgb(a, c1);
                c2 = Color.FromArgb(a, c2);
            }
            using (var lg = new LinearGradientBrush(rect, c1, c2, LinearGradientMode.Horizontal))
            {
                if (_backgroundOpacity > 0f)
                    g.FillRectangle(lg, rect);
            }

            // Dekor: Diagonale Straße
            float h = rect.Height;
            float w = rect.Width;
            float roadWidth = Math.Max(48f, h * 0.55f);
            var roadColor = _darkTheme ? Color.FromArgb(64, 64, 68) : Color.FromArgb(190, 195, 200);
            if (_backgroundOpacity < 1f)
            {
                int a = (int)(255 * _backgroundOpacity);
                roadColor = Color.FromArgb(a, roadColor);
            }
            using (var roadPath = new GraphicsPath())
            {
                // Diagonale von links unten nach rechts oben
                var p1 = new PointF(-w * 0.1f, h);
                var p2 = new PointF(w * 0.65f, 0);
                // parallele Kante versetzt um roadWidth
                var dir = new PointF(p2.X - p1.X, p2.Y - p1.Y);
                var len = (float)Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y);
                if (len < 1f) len = 1f;
                var nx = -dir.Y / len; // Normalenvektor
                var ny = dir.X / len;

                var p3 = new PointF(p2.X + nx * roadWidth, p2.Y + ny * roadWidth);
                var p4 = new PointF(p1.X + nx * roadWidth, p1.Y + ny * roadWidth);

                roadPath.AddPolygon(new[] { p1, p2, p3, p4 });
                using var roadBrush = new SolidBrush(roadColor);
                g.FillPath(roadBrush, roadPath);

                // Glanz-Effekt (subtiler Wander-Highlight innerhalb der Straße)
                if (_glossEnabled)
                {
                    DrawGloss(g, roadPath, p1, p2, len);
                }
            }

            // Fahrbahnmarkierungen (mittig, gestrichelt)
            using (var pen = new Pen(_darkTheme ? Color.FromArgb(230, 210, 40) : Color.FromArgb(240, 200, 20), Math.Max(2f, h * 0.04f)))
            {
                if (_backgroundOpacity < 1f)
                {
                    int a = (int)(255 * _backgroundOpacity);
                    var baseCol = pen.Color;
                    pen.Color = Color.FromArgb(a, baseCol);
                }
                pen.DashStyle = DashStyle.Custom;
                pen.DashPattern = new[] { Math.Max(6f, h * 0.2f), Math.Max(6f, h * 0.15f) };

                // Linie entlang der Diagonalen (leicht innerhalb der Straße)
                var start = new PointF(w * 0.05f, h * 0.95f);
                var end = new PointF(w * 0.7f, h * 0.05f);
                if (_backgroundOpacity > 0f)
                    g.DrawLine(pen, start, end);
            }

            // Logos rechts zeichnen (transparent, je nach Game-Code)
            EnsureLogosLoaded();
            var logoTargetHeight = Math.Min(60f, h * 0.7f);
            var margin = Math.Max(10f, h * 0.1f);
            var alpha = 160; // halbtransparent

            void DrawLogo(Image? img, float rightOffset)
            {
                if (img == null) return;
                float scale = logoTargetHeight / img.Height;
                var ww = img.Width * scale;
                var hh = img.Height * scale;
                var x = w - rightOffset - ww;
                var y = (h - hh) / 2f;
                using var ia = new ImageAttributes();
                var cm = new ColorMatrix { Matrix33 = alpha / 255f };
                ia.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                var dest = new Rectangle((int)Math.Round(x), (int)Math.Round(y), (int)Math.Round(ww), (int)Math.Round(hh));
                g.DrawImage(
                    img,
                    dest,
                    0, 0, img.Width, img.Height,
                    GraphicsUnit.Pixel,
                    ia
                );
            }

            if (string.Equals(_gameCode, "ETS2", StringComparison.OrdinalIgnoreCase))
            {
                DrawLogo(_ets2Logo, margin);
            }
            else if (string.Equals(_gameCode, "ATS", StringComparison.OrdinalIgnoreCase))
            {
                DrawLogo(_atsLogo, margin);
            }
            else
            {
                // Fallback: beide kleiner zeichnen
                DrawLogo(_ets2Logo, margin + 70);
                DrawLogo(_atsLogo, margin);
            }

            // Optionaler Text links oben
            if (!string.IsNullOrWhiteSpace(Title))
            {
                using var titleFont = new Font("Segoe UI", Math.Max(10f, h * 0.22f), FontStyle.Bold);
                var titleColor = _darkTheme ? Color.WhiteSmoke : Color.FromArgb(30, 30, 35);
                var titlePoint = new PointF(margin, Math.Max(6f, h * 0.12f));
                TextRenderer.DrawText(g, Title, titleFont, Point.Round(titlePoint), titleColor);
            }
            if (!string.IsNullOrWhiteSpace(Subtitle))
            {
                using var subFont = new Font("Segoe UI", Math.Max(8f, h * 0.16f), FontStyle.Regular);
                var subColor = _darkTheme ? Color.Gainsboro : Color.FromArgb(70, 70, 75);
                var y = Math.Max(6f, h * 0.12f) + Math.Max(10f, h * 0.28f);
                var subPoint = new PointF(margin, y);
                TextRenderer.DrawText(g, Subtitle, subFont, Point.Round(subPoint), subColor);
            }
        }

        private void DrawGloss(Graphics g, GraphicsPath roadPath, PointF p1, PointF p2, float length)
        {
            var bounds = roadPath.GetBounds();
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            // Winkel entlang der Straße
            float angle = (float)(Math.Atan2(p2.Y - p1.Y, p2.X - p1.X) * 180.0 / Math.PI);

            // Heller Streifen mit Verlauf: transparent -> hell -> transparent
            var intensity = _darkTheme ? 80 : 110; // Alpha-Spitze
            using var brush = new LinearGradientBrush(bounds, Color.Transparent, Color.Transparent, angle);
            var cb = new ColorBlend
            {
                Colors = new[]
                {
                    Color.FromArgb(0, Color.White),           // 0.0
                    Color.FromArgb(intensity/2, Color.White),  // 0.48
                    Color.FromArgb(intensity, Color.White),    // 0.50
                    Color.FromArgb(intensity/2, Color.White),  // 0.52
                    Color.FromArgb(0, Color.White)             // 1.0
                },
                Positions = new float[] { 0.0f, 0.48f, 0.50f, 0.52f, 1.0f }
            };
            brush.InterpolationColors = cb;

            // Animation: den Verlauf entlang der Straße verschieben
            float shift = _glossPhase * length * 0.25f; // langsamer als volle Länge
            using (var m = new Matrix())
            {
                m.RotateAt(angle, new PointF(bounds.Left, bounds.Top));
                m.Translate(shift, 0, MatrixOrder.Append);
                m.RotateAt(-angle, new PointF(bounds.Left, bounds.Top), MatrixOrder.Append);
                brush.MultiplyTransform(m);
            }

            // Nur innerhalb der Straße zeichnen
            var state = g.Save();
            try
            {
                g.SetClip(roadPath, CombineMode.Intersect);
                g.FillRectangle(brush, bounds);
            }
            finally
            {
                g.Restore(state);
            }
        }
    }
}

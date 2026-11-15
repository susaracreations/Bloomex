using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BlueStacks4FPSTweaker
{
    public static class StudioGraphics
    {
        public static GraphicsPath CreateRoundedPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = radius * 2f;
            if (diameter > rect.Width) diameter = rect.Width;
            if (diameter > rect.Height) diameter = rect.Height;

            RectangleF arc = new RectangleF(rect.X, rect.Y, diameter, diameter);

            // Top-left
            path.AddArc(arc, 180, 90);

            // Top-right
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);

            // Bottom-right
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // Bottom-left
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }

    /// <summary>
    /// Studio-grade Card with subtle 1px border and deep dark titanium background
    /// </summary>
    public class StudioCard : Panel
    {
        public int CornerRadius { get; set; }
        public Color BorderColor { get; set; }
        public Color FillColor { get; set; }
        public bool DrawTopGlow { get; set; }

        public StudioCard()
        {
            CornerRadius = 14;
            BorderColor = Color.FromArgb(32, 42, 60);
            FillColor = Color.FromArgb(17, 23, 34);
            DrawTopGlow = false;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);

            using (GraphicsPath path = StudioGraphics.CreateRoundedPath(rect, CornerRadius))
            {
                using (SolidBrush brush = new SolidBrush(FillColor))
                {
                    e.Graphics.FillPath(brush, path);
                }

                if (DrawTopGlow)
                {
                    RectangleF topRect = new RectangleF(0.5f, 0.5f, Width - 1f, 24f);
                    using (LinearGradientBrush lgb = new LinearGradientBrush(topRect, Color.FromArgb(20, 0, 229, 255), Color.Transparent, LinearGradientMode.Vertical))
                    {
                        e.Graphics.FillPath(lgb, path);
                    }
                }

                using (Pen pen = new Pen(BorderColor, 1.2f))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }
    }

    /// <summary>
    /// Circular Speedometer / Arc Gauge for Target FPS Hero display
    /// </summary>
    public class ArcGaugeControl : Control
    {
        private int _value = 90;
        private int _minimum = 30;
        private int _maximum = 240;

        public int Value
        {
            get { return _value; }
            set
            {
                int clamped = Math.Max(_minimum, Math.Min(_maximum, value));
                if (_value != clamped)
                {
                    _value = clamped;
                    Invalidate();
                }
            }
        }

        public ArcGaugeControl()
        {
            Size = new Size(180, 180);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            float cx = Width / 2f;
            float cy = Height / 2f;
            float radius = Math.Min(cx, cy) - 16f;

            RectangleF arcRect = new RectangleF(cx - radius, cy - radius, radius * 2f, radius * 2f);

            float startAngle = 135f;
            float sweepAngle = 270f;

            // Background arc track
            using (Pen bgPen = new Pen(Color.FromArgb(28, 38, 54), 10f))
            {
                bgPen.StartCap = LineCap.Round;
                bgPen.EndCap = LineCap.Round;
                e.Graphics.DrawArc(bgPen, arcRect, startAngle, sweepAngle);
            }

            // Active glowing gradient arc
            float progress = (float)(_value - _minimum) / (float)(_maximum - _minimum);
            float activeSweep = sweepAngle * progress;

            if (activeSweep > 3f)
            {
                // Draw neon cyan glow underneath
                using (Pen glowPen = new Pen(Color.FromArgb(40, 0, 229, 255), 18f))
                {
                    glowPen.StartCap = LineCap.Round;
                    glowPen.EndCap = LineCap.Round;
                    e.Graphics.DrawArc(glowPen, arcRect, startAngle, activeSweep);
                }

                // Draw sharp active arc
                using (LinearGradientBrush lgb = new LinearGradientBrush(arcRect, Color.FromArgb(0, 229, 255), Color.FromArgb(121, 40, 202), LinearGradientMode.ForwardDiagonal))
                {
                    using (Pen activePen = new Pen(lgb, 10f))
                    {
                        activePen.StartCap = LineCap.Round;
                        activePen.EndCap = LineCap.Round;
                        e.Graphics.DrawArc(activePen, arcRect, startAngle, activeSweep);
                    }
                }
            }

            // Center Text: Numeric Value
            string valStr = _value.ToString();
            using (Font numFont = new Font("Segoe UI", 32f, FontStyle.Bold))
            {
                SizeF sz = e.Graphics.MeasureString(valStr, numFont);
                e.Graphics.DrawString(valStr, numFont, Brushes.White, cx - (sz.Width / 2f), cy - sz.Height + 10f);
            }

            // "FPS" Label with cyan neon color
            using (Font fpsFont = new Font("Segoe UI", 11f, FontStyle.Bold))
            {
                using (SolidBrush cyanBrush = new SolidBrush(Color.FromArgb(0, 229, 255)))
                {
                    SizeF szFps = e.Graphics.MeasureString("FPS", fpsFont);
                    e.Graphics.DrawString("FPS", fpsFont, cyanBrush, cx - (szFps.Width / 2f), cy + 12f);
                }
            }

            // Subtitle
            string sub = _value >= 144 ? "ESPORTS" : (_value >= 90 ? "OPTIMAL" : "STANDARD");
            using (Font subFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            {
                using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    SizeF szSub = e.Graphics.MeasureString(sub, subFont);
                    e.Graphics.DrawString(sub, subFont, subBrush, cx - (szSub.Width / 2f), cy + 32f);
                }
            }
        }
    }

    /// <summary>
    /// Studio-grade Preset Pill Button with active glow and selection borders
    /// </summary>
    public class PresetPillButton : Control
    {
        public int FpsValue { get; set; }
        public string SubLabel { get; set; }
        public bool IsSelected { get; set; }

        private bool _isHovered;

        public PresetPillButton()
        {
            Size = new Size(116, 54);
            Cursor = Cursors.Hand;
            SubLabel = "Hz";
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            RectangleF rect = new RectangleF(1f, 1f, Width - 2f, Height - 2f);

            using (GraphicsPath path = StudioGraphics.CreateRoundedPath(rect, 12f))
            {
                Color bg;
                Color border;

                if (IsSelected)
                {
                    bg = Color.FromArgb(20, 36, 56);
                    border = Color.FromArgb(0, 229, 255);
                }
                else if (_isHovered)
                {
                    bg = Color.FromArgb(26, 35, 50);
                    border = Color.FromArgb(64, 82, 110);
                }
                else
                {
                    bg = Color.FromArgb(17, 23, 34);
                    border = Color.FromArgb(32, 42, 60);
                }

                // Background
                using (SolidBrush brush = new SolidBrush(bg))
                {
                    e.Graphics.FillPath(brush, path);
                }

                // Border
                using (Pen pen = new Pen(border, IsSelected ? 1.8f : 1.2f))
                {
                    e.Graphics.DrawPath(pen, path);
                }

                // If selected, draw tiny cyan glow dot in top right
                if (IsSelected)
                {
                    using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(0, 229, 255)))
                    {
                        e.Graphics.FillEllipse(dotBrush, Width - 14, 8, 6, 6);
                    }
                }
            }

            // Draw Text: "90 FPS"
            string mainText = Text;
            using (Font mainFont = new Font("Segoe UI", 11f, FontStyle.Bold))
            {
                Color textCol = IsSelected ? Color.White : (_isHovered ? Color.FromArgb(240, 246, 252) : Color.FromArgb(203, 213, 225));
                using (SolidBrush textBrush = new SolidBrush(textCol))
                {
                    e.Graphics.DrawString(mainText, mainFont, textBrush, 12, 9);
                }
            }

            // Draw Subtext: e.g. "Free Fire Pro"
            using (Font subFont = new Font("Segoe UI", 7.5f))
            {
                Color subCol = IsSelected ? Color.FromArgb(0, 229, 255) : Color.FromArgb(100, 116, 139);
                using (SolidBrush subBrush = new SolidBrush(subCol))
                {
                    e.Graphics.DrawString(SubLabel, subFont, subBrush, 12, 31);
                }
            }
        }
    }

    /// <summary>
    /// Smooth Precision Slider with Tick Marks and Glow Track
    /// </summary>
    public class StudioSlider : Control
    {
        public event EventHandler ValueChanged;

        private int _minimum = 30;
        private int _maximum = 240;
        private int _value = 90;

        public int Minimum
        {
            get { return _minimum; }
            set { _minimum = value; Invalidate(); }
        }

        public int Maximum
        {
            get { return _maximum; }
            set { _maximum = value; Invalidate(); }
        }

        public int Value
        {
            get { return _value; }
            set
            {
                int clamped = Math.Max(_minimum, Math.Min(_maximum, value));
                if (_value != clamped)
                {
                    _value = clamped;
                    Invalidate();
                    if (ValueChanged != null)
                    {
                        ValueChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        private bool _isDragging = false;

        public StudioSlider()
        {
            Height = 44;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                UpdateValueFromMouse(e.X);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_isDragging)
            {
                UpdateValueFromMouse(e.X);
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _isDragging = false;
            base.OnMouseUp(e);
        }

        private void UpdateValueFromMouse(int mouseX)
        {
            int padding = 16;
            int trackWidth = Width - (padding * 2);
            if (trackWidth <= 0) return;

            float ratio = (float)(mouseX - padding) / (float)trackWidth;
            if (ratio < 0f) ratio = 0f;
            if (ratio > 1f) ratio = 1f;

            int computed = _minimum + (int)Math.Round(ratio * (_maximum - _minimum));
            Value = computed;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int padding = 16;
            int trackHeight = 6;
            int trackY = 16;
            int trackWidth = Width - (padding * 2);

            Rectangle fullTrack = new Rectangle(padding, trackY, trackWidth, trackHeight);

            // Inactive track
            using (GraphicsPath trackPath = StudioGraphics.CreateRoundedPath(fullTrack, 3f))
            {
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(28, 38, 54)))
                {
                    e.Graphics.FillPath(brush, trackPath);
                }
            }

            // Active gradient track
            float ratio = (float)(_value - _minimum) / (float)(_maximum - _minimum);
            int activeWidth = (int)(trackWidth * ratio);
            if (activeWidth > 0)
            {
                Rectangle activeTrack = new Rectangle(padding, trackY, activeWidth, trackHeight);
                using (GraphicsPath activePath = StudioGraphics.CreateRoundedPath(activeTrack, 3f))
                {
                    using (LinearGradientBrush brush = new LinearGradientBrush(fullTrack, Color.FromArgb(0, 229, 255), Color.FromArgb(121, 40, 202), LinearGradientMode.Horizontal))
                    {
                        e.Graphics.FillPath(brush, activePath);
                    }
                }
            }

            // Ticks at 60, 90, 120, 144, 240
            int[] ticks = new int[] { 60, 90, 120, 144, 240 };
            using (Font tickFont = new Font("Segoe UI", 7f, FontStyle.Bold))
            {
                using (SolidBrush tickBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    foreach (int t in ticks)
                    {
                        float tRatio = (float)(t - _minimum) / (float)(_maximum - _minimum);
                        int tx = padding + (int)(trackWidth * tRatio);

                        // Tick pip
                        e.Graphics.FillRectangle(tickBrush, tx - 1, trackY + trackHeight + 3, 2, 4);

                        // Tick label
                        string lbl = t.ToString();
                        SizeF sz = e.Graphics.MeasureString(lbl, tickFont);
                        e.Graphics.DrawString(lbl, tickFont, tickBrush, tx - (sz.Width / 2f), trackY + trackHeight + 8);
                    }
                }
            }

            // Thumb handle with glowing ring
            int thumbX = padding + activeWidth - 11;
            int thumbY = trackY - 8;
            Rectangle thumbRect = new Rectangle(thumbX, thumbY, 22, 22);

            // Glow
            using (SolidBrush glow = new SolidBrush(Color.FromArgb(50, 0, 229, 255)))
            {
                e.Graphics.FillEllipse(glow, thumbX - 3, thumbY - 3, 28, 28);
            }

            // Knob
            using (SolidBrush knobBrush = new SolidBrush(Color.White))
            {
                e.Graphics.FillEllipse(knobBrush, thumbRect);
            }
            using (Pen ringPen = new Pen(Color.FromArgb(0, 229, 255), 2.5f))
            {
                e.Graphics.DrawEllipse(ringPen, thumbRect);
            }
        }
    }

    /// <summary>
    /// Modern Interactive Card Toggle with description and smooth switch
    /// </summary>
    public class StudioSettingCard : Control
    {
        public event EventHandler CheckedChanged;

        private bool _checked = false;
        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked != value)
                {
                    _checked = value;
                    Invalidate();
                    if (CheckedChanged != null)
                    {
                        CheckedChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        public string Title { get; set; }
        public string Subtitle { get; set; }

        private bool _isHovered;

        public StudioSettingCard()
        {
            Size = new Size(218, 64);
            Cursor = Cursors.Hand;
            Title = "Setting Name";
            Subtitle = "Feature description";
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnClick(EventArgs e)
        {
            Checked = !Checked;
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            RectangleF rect = new RectangleF(1f, 1f, Width - 2f, Height - 2f);

            using (GraphicsPath path = StudioGraphics.CreateRoundedPath(rect, 10f))
            {
                Color bg = _checked ? Color.FromArgb(19, 29, 44) : (_isHovered ? Color.FromArgb(22, 30, 42) : Color.FromArgb(15, 20, 30));
                Color border = _checked ? Color.FromArgb(0, 180, 216) : (_isHovered ? Color.FromArgb(50, 65, 88) : Color.FromArgb(28, 38, 54));

                using (SolidBrush brush = new SolidBrush(bg))
                {
                    e.Graphics.FillPath(brush, path);
                }

                using (Pen pen = new Pen(border, 1.2f))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }

            // Draw Texts
            using (Font titleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            {
                using (SolidBrush brush = new SolidBrush(_checked ? Color.White : Color.FromArgb(226, 232, 240)))
                {
                    e.Graphics.DrawString(Title, titleFont, brush, 12, 12);
                }
            }

            using (Font subFont = new Font("Segoe UI", 7.5f))
            {
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    e.Graphics.DrawString(Subtitle, subFont, brush, 12, 34);
                }
            }

            // Draw Toggle Switch on Right
            int swWidth = 36;
            int swHeight = 20;
            int swX = Width - swWidth - 12;
            int swY = (Height - swHeight) / 2;

            Rectangle swRect = new Rectangle(swX, swY, swWidth, swHeight);
            using (GraphicsPath swPath = StudioGraphics.CreateRoundedPath(swRect, swHeight / 2f))
            {
                Color swBg = _checked ? Color.FromArgb(0, 230, 118) : Color.FromArgb(40, 52, 70);
                using (SolidBrush swBrush = new SolidBrush(swBg))
                {
                    e.Graphics.FillPath(swBrush, swPath);
                }

                int knobSize = swHeight - 4;
                int knobX = _checked ? (swX + swWidth - knobSize - 2) : (swX + 2);
                int knobY = swY + 2;

                using (SolidBrush knobBrush = new SolidBrush(Color.White))
                {
                    e.Graphics.FillEllipse(knobBrush, knobX, knobY, knobSize, knobSize);
                }
            }
        }
    }

    /// <summary>
    /// Premium Studio Action Button with high-contrast gradient, smooth hover and click animations
    /// </summary>
    public class StudioActionButton : Button
    {
        public int CornerRadius { get; set; }
        public Color GradientStart { get; set; }
        public Color GradientEnd { get; set; }
        public Color BorderColor { get; set; }
        public Color ShadowGlowColor { get; set; }

        private bool _isHovered;
        private bool _isPressed;

        public StudioActionButton()
        {
            CornerRadius = 14;
            GradientStart = Color.FromArgb(0, 180, 255);
            GradientEnd = Color.FromArgb(0, 119, 237);
            BorderColor = Color.FromArgb(0, 229, 255);
            ShadowGlowColor = Color.FromArgb(40, 0, 229, 255);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            _isPressed = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _isPressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            RectangleF rect = new RectangleF(1f, 1f, Width - 2f, Height - 2f);

            Color start = GradientStart;
            Color end = GradientEnd;

            if (_isPressed)
            {
                start = ControlPaint.Dark(start, 0.2f);
                end = ControlPaint.Dark(end, 0.2f);
            }
            else if (_isHovered)
            {
                start = ControlPaint.Light(start, 0.15f);
                end = ControlPaint.Light(end, 0.15f);
            }

            using (GraphicsPath path = StudioGraphics.CreateRoundedPath(rect, CornerRadius))
            {
                // Glow on hover
                if (_isHovered && !_isPressed && ShadowGlowColor != Color.Transparent)
                {
                    using (Pen glowPen = new Pen(ShadowGlowColor, 4f))
                    {
                        e.Graphics.DrawPath(glowPen, path);
                    }
                }

                // Fill gradient
                using (LinearGradientBrush lgb = new LinearGradientBrush(rect, start, end, LinearGradientMode.Horizontal))
                {
                    e.Graphics.FillPath(lgb, path);
                }

                // Border
                if (BorderColor != Color.Transparent)
                {
                    using (Pen pen = new Pen(_isHovered ? Color.White : BorderColor, 1.2f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }

            // Text
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(0, 0, Width, Height), ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MinecraftBuildingGuide3D
{
    // Color scheme for dark theme
    public static class DarkTheme
    {
        public static Color Background = Color.FromArgb(40, 40, 55);
        public static Color BackgroundDark = Color.FromArgb(30, 30, 42);
        public static Color BackgroundLight = Color.FromArgb(50, 55, 70);
        public static Color Border = Color.FromArgb(60, 65, 80);
        public static Color BorderFocus = Color.FromArgb(80, 130, 180);
        public static Color Text = Color.FromArgb(220, 220, 230);
        public static Color TextDim = Color.FromArgb(150, 150, 165);
        public static Color Accent = Color.FromArgb(70, 130, 180);
        public static Color AccentHover = Color.FromArgb(90, 150, 200);
        public static Color CheckMark = Color.FromArgb(100, 180, 255);
        public static Color SliderTrack = Color.FromArgb(50, 55, 70);
        public static Color SliderThumb = Color.FromArgb(90, 140, 190);
        public static Color SliderThumbHover = Color.FromArgb(110, 160, 210);
    }

    // Custom dark-themed CheckBox
    public class DarkCheckBox : CheckBox
    {
        private bool _isHovering = false;

        public DarkCheckBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            Font = new Font("Segoe UI", 9.5f);
            ForeColor = DarkTheme.Text;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovering = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovering = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor == Color.Transparent ? Parent?.BackColor ?? DarkTheme.BackgroundDark : BackColor);

            int boxSize = 16;
            int boxY = (Height - boxSize) / 2;
            var boxRect = new Rectangle(0, boxY, boxSize, boxSize);

            // Draw checkbox background
            using (var brush = new SolidBrush(_isHovering ? DarkTheme.BackgroundLight : DarkTheme.Background))
                g.FillRectangle(brush, boxRect);

            // Draw border
            using (var pen = new Pen(Focused || _isHovering ? DarkTheme.BorderFocus : DarkTheme.Border, 1.5f))
                g.DrawRectangle(pen, boxRect);

            // Draw checkmark
            if (Checked)
            {
                using (var pen = new Pen(DarkTheme.CheckMark, 2f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawLine(pen, 3, boxY + 8, 6, boxY + 12);
                    g.DrawLine(pen, 6, boxY + 12, 13, boxY + 4);
                }
            }

            // Draw text
            var textRect = new Rectangle(boxSize + 6, 0, Width - boxSize - 6, Height);
            TextRenderer.DrawText(g, Text, Font, textRect, ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }
    }

    // Custom dark-themed RadioButton
    public class DarkRadioButton : RadioButton
    {
        private bool _isHovering = false;

        public DarkRadioButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            Font = new Font("Segoe UI", 10f);
            ForeColor = DarkTheme.Text;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovering = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovering = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor == Color.Transparent ? Parent?.BackColor ?? DarkTheme.BackgroundDark : BackColor);

            int circleSize = 16;
            int circleY = (Height - circleSize) / 2;
            var circleRect = new Rectangle(0, circleY, circleSize, circleSize);

            // Draw radio background
            using (var brush = new SolidBrush(_isHovering ? DarkTheme.BackgroundLight : DarkTheme.Background))
                g.FillEllipse(brush, circleRect);

            // Draw border
            using (var pen = new Pen(Focused || _isHovering ? DarkTheme.BorderFocus : DarkTheme.Border, 1.5f))
                g.DrawEllipse(pen, circleRect);

            // Draw inner circle when checked
            if (Checked)
            {
                using (var brush = new SolidBrush(DarkTheme.CheckMark))
                    g.FillEllipse(brush, circleRect.X + 4, circleRect.Y + 4, 8, 8);
            }

            // Draw text
            var textRect = new Rectangle(circleSize + 6, 0, Width - circleSize - 6, Height);
            TextRenderer.DrawText(g, Text, Font, textRect, ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }
    }

    // Custom dark-themed ComboBox
    public class DarkComboBox : ComboBox
    {
        private bool _isHovering = false;

        public DarkComboBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
            Font = new Font("Segoe UI", 9.5f);
            BackColor = DarkTheme.Background;
            ForeColor = DarkTheme.Text;
            FlatStyle = FlatStyle.Flat;
            ItemHeight = 24;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovering = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovering = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            var g = e.Graphics;
            var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            using (var brush = new SolidBrush(isSelected ? DarkTheme.Accent : DarkTheme.Background))
                g.FillRectangle(brush, e.Bounds);

            var textColor = isSelected ? Color.White : DarkTheme.Text;
            TextRenderer.DrawText(g, Items[e.Index].ToString(), Font, e.Bounds, textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Background
            using (var brush = new SolidBrush(_isHovering ? DarkTheme.BackgroundLight : DarkTheme.Background))
                g.FillRectangle(brush, ClientRectangle);

            // Border
            using (var pen = new Pen(Focused || _isHovering || DroppedDown ? DarkTheme.BorderFocus : DarkTheme.Border, 1f))
                g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

            // Draw selected text
            var textRect = new Rectangle(4, 0, Width - 24, Height);
            if (SelectedItem != null)
            {
                TextRenderer.DrawText(g, SelectedItem.ToString(), Font, textRect, DarkTheme.Text,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }

            // Draw dropdown arrow
            int arrowX = Width - 18;
            int arrowY = Height / 2 - 2;
            using (var brush = new SolidBrush(DarkTheme.Text))
            {
                var arrowPoints = new Point[] {
                    new Point(arrowX, arrowY),
                    new Point(arrowX + 8, arrowY),
                    new Point(arrowX + 4, arrowY + 5)
                };
                g.FillPolygon(brush, arrowPoints);
            }
        }
    }

    // Custom dark-themed TrackBar (Slider)
    public class DarkTrackBar : Control
    {
        private int _minimum = 0;
        private int _maximum = 100;
        private int _value = 50;
        private bool _isDragging = false;
        private bool _isHovering = false;

        public event EventHandler ValueChanged;

        public int Minimum
        {
            get => _minimum;
            set
            {
                _minimum = value;
                if (_value < _minimum) Value = _minimum;
                Invalidate();
            }
        }

        public int Maximum
        {
            get => _maximum;
            set
            {
                _maximum = value;
                if (_value > _maximum) Value = _maximum;
                Invalidate();
            }
        }

        public int Value
        {
            get => _value;
            set
            {
                int newVal = Math.Max(_minimum, Math.Min(_maximum, value));
                if (newVal != _value)
                {
                    _value = newVal;
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        public DarkTrackBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
            Size = new Size(130, 26);
            Cursor = Cursors.Hand;
        }

        private Rectangle GetThumbRect()
        {
            int trackWidth = Width - 16;
            float ratio = (_maximum > _minimum) ? (float)(_value - _minimum) / (_maximum - _minimum) : 0;
            int thumbX = (int)(ratio * trackWidth);
            return new Rectangle(thumbX, 4, 16, Height - 8);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovering = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovering = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                UpdateValueFromMouse(e.X);
                Capture = true;
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_isDragging)
                UpdateValueFromMouse(e.X);
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _isDragging = false;
            Capture = false;
            base.OnMouseUp(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            int delta = e.Delta > 0 ? 1 : -1;
            Value += delta * Math.Max(1, (_maximum - _minimum) / 20);
            base.OnMouseWheel(e);
        }

        private void UpdateValueFromMouse(int mouseX)
        {
            int trackWidth = Width - 16;
            float ratio = Math.Max(0, Math.Min(1, (mouseX - 8f) / trackWidth));
            Value = (int)(_minimum + ratio * (_maximum - _minimum));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? DarkTheme.BackgroundDark);

            int trackHeight = 6;
            int trackY = (Height - trackHeight) / 2;
            var trackRect = new Rectangle(8, trackY, Width - 16, trackHeight);

            // Draw track background
            using (var brush = new SolidBrush(DarkTheme.SliderTrack))
            {
                using (var path = GetRoundedRect(trackRect, 3))
                    g.FillPath(brush, path);
            }

            // Draw filled portion
            float ratio = (_maximum > _minimum) ? (float)(_value - _minimum) / (_maximum - _minimum) : 0;
            int filledWidth = (int)(ratio * (Width - 16));
            if (filledWidth > 0)
            {
                var filledRect = new Rectangle(8, trackY, filledWidth, trackHeight);
                using (var brush = new SolidBrush(DarkTheme.Accent))
                {
                    using (var path = GetRoundedRect(filledRect, 3))
                        g.FillPath(brush, path);
                }
            }

            // Draw thumb
            var thumbRect = GetThumbRect();
            var thumbColor = _isDragging || _isHovering ? DarkTheme.SliderThumbHover : DarkTheme.SliderThumb;
            using (var brush = new SolidBrush(thumbColor))
            {
                g.FillEllipse(brush, thumbRect.X, thumbRect.Y + 2, 14, 14);
            }

            // Thumb border
            using (var pen = new Pen(DarkTheme.Border, 1f))
            {
                g.DrawEllipse(pen, thumbRect.X, thumbRect.Y + 2, 14, 14);
            }
        }

        private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            var arc = new Rectangle(rect.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // Custom dark-themed NumericUpDown
    public class DarkNumericUpDown : Control
    {
        private int _minimum = 0;
        private int _maximum = 100;
        private int _value = 0;
        private bool _isHoveringUp = false;
        private bool _isHoveringDown = false;
        private bool _isHoveringText = false;
        private Timer _repeatTimer;
        private int _repeatDirection = 0;
        private TextBox _textBox;

        public event EventHandler ValueChanged;

        public int Minimum
        {
            get => _minimum;
            set { _minimum = value; if (_value < _minimum) Value = _minimum; }
        }

        public int Maximum
        {
            get => _maximum;
            set { _maximum = value; if (_value > _maximum) Value = _maximum; }
        }

        public decimal Value
        {
            get => _value;
            set
            {
                int newVal = Math.Max(_minimum, Math.Min(_maximum, (int)value));
                if (newVal != _value)
                {
                    _value = newVal;
                    if (_textBox != null) _textBox.Text = _value.ToString();
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        public DarkNumericUpDown()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
            Size = new Size(65, 26);

            _textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = DarkTheme.Background,
                ForeColor = DarkTheme.Text,
                Font = new Font("Segoe UI", 9.5f),
                TextAlign = HorizontalAlignment.Center,
                Text = _value.ToString()
            };
            _textBox.TextChanged += TextBox_TextChanged;
            _textBox.KeyDown += TextBox_KeyDown;
            _textBox.GotFocus += (s, e) => Invalidate();
            _textBox.LostFocus += (s, e) => { ValidateText(); Invalidate(); };
            Controls.Add(_textBox);

            _repeatTimer = new Timer { Interval = 100 };
            _repeatTimer.Tick += RepeatTimer_Tick;

            UpdateTextBoxBounds();
        }

        private void UpdateTextBoxBounds()
        {
            if (_textBox == null) return;  // Not yet initialized
            int buttonWidth = 20;
            _textBox.Location = new Point(4, (Height - _textBox.Height) / 2);
            _textBox.Width = Width - buttonWidth - 8;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateTextBoxBounds();
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            // Allow typing, validation happens on focus lost
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ValidateText();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                Value++;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                Value--;
                e.SuppressKeyPress = true;
            }
        }

        private void ValidateText()
        {
            if (int.TryParse(_textBox.Text, out int result))
                Value = result;
            else
                _textBox.Text = _value.ToString();
        }

        private Rectangle GetUpButtonRect() => new Rectangle(Width - 20, 0, 20, Height / 2);
        private Rectangle GetDownButtonRect() => new Rectangle(Width - 20, Height / 2, 20, Height / 2);

        protected override void OnMouseMove(MouseEventArgs e)
        {
            bool wasHoveringUp = _isHoveringUp;
            bool wasHoveringDown = _isHoveringDown;

            _isHoveringUp = GetUpButtonRect().Contains(e.Location);
            _isHoveringDown = GetDownButtonRect().Contains(e.Location);
            _isHoveringText = !_isHoveringUp && !_isHoveringDown;

            if (wasHoveringUp != _isHoveringUp || wasHoveringDown != _isHoveringDown)
                Invalidate();

            Cursor = (_isHoveringUp || _isHoveringDown) ? Cursors.Hand : Cursors.IBeam;
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHoveringUp = _isHoveringDown = _isHoveringText = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_isHoveringUp)
                {
                    Value++;
                    _repeatDirection = 1;
                    _repeatTimer.Start();
                }
                else if (_isHoveringDown)
                {
                    Value--;
                    _repeatDirection = -1;
                    _repeatTimer.Start();
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _repeatTimer.Stop();
            _repeatDirection = 0;
            base.OnMouseUp(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            Value += e.Delta > 0 ? 1 : -1;
            base.OnMouseWheel(e);
        }

        private void RepeatTimer_Tick(object sender, EventArgs e)
        {
            Value += _repeatDirection;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Background
            using (var brush = new SolidBrush(DarkTheme.Background))
                g.FillRectangle(brush, ClientRectangle);

            // Border
            bool hasFocus = _textBox.Focused || _isHoveringUp || _isHoveringDown;
            using (var pen = new Pen(hasFocus ? DarkTheme.BorderFocus : DarkTheme.Border, 1f))
                g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

            // Button separator
            using (var pen = new Pen(DarkTheme.Border, 1f))
                g.DrawLine(pen, Width - 21, 0, Width - 21, Height);

            // Up button
            var upRect = GetUpButtonRect();
            if (_isHoveringUp)
            {
                using (var brush = new SolidBrush(DarkTheme.BackgroundLight))
                    g.FillRectangle(brush, upRect);
            }
            DrawArrow(g, upRect, true);

            // Down button
            var downRect = GetDownButtonRect();
            if (_isHoveringDown)
            {
                using (var brush = new SolidBrush(DarkTheme.BackgroundLight))
                    g.FillRectangle(brush, downRect);
            }
            DrawArrow(g, downRect, false);

            // Middle separator
            using (var pen = new Pen(DarkTheme.Border, 1f))
                g.DrawLine(pen, Width - 20, Height / 2, Width - 1, Height / 2);
        }

        private void DrawArrow(Graphics g, Rectangle rect, bool up)
        {
            int cx = rect.X + rect.Width / 2;
            int cy = rect.Y + rect.Height / 2;

            using (var brush = new SolidBrush(DarkTheme.Text))
            {
                Point[] arrow;
                if (up)
                {
                    arrow = new Point[] {
                        new Point(cx - 4, cy + 2),
                        new Point(cx + 4, cy + 2),
                        new Point(cx, cy - 2)
                    };
                }
                else
                {
                    arrow = new Point[] {
                        new Point(cx - 4, cy - 2),
                        new Point(cx + 4, cy - 2),
                        new Point(cx, cy + 2)
                    };
                }
                g.FillPolygon(brush, arrow);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _repeatTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
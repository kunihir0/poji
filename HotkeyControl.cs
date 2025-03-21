using System;
using System.Drawing;
using System.Windows.Forms;

namespace poji
{
    public class HotkeyControl : Control
    {
        private Keys _key = Keys.None;
        private bool _alt;
        private bool _shift;
        private bool _ctrl;
        private bool _isEditing;
        private BorderStyle _borderStyle = BorderStyle.FixedSingle;

        public event EventHandler HotkeyFocusEntered;
        public event EventHandler HotkeyFocusLeft;

        public BorderStyle BorderStyle
        {
            get => _borderStyle;
            set
            {
                _borderStyle = value;
                Invalidate();
            }
        }
        public HotkeyControl()
        {
            SetStyle(ControlStyles.Selectable, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);

            BackColor = SystemColors.Window;
            ForeColor = SystemColors.WindowText;
            MinimumSize = new Size(50, 24);
            Cursor = Cursors.Hand;

            this.Click += (s, e) => StartEditing();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            // Draw the background
            using (SolidBrush bgBrush = new SolidBrush(BackColor))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            // Draw the border if needed
            if (_borderStyle == BorderStyle.FixedSingle)
            {
                using (Pen borderPen = new Pen(_isEditing ? Color.FromArgb(0, 120, 215) : Color.FromArgb(100, 100, 100)))
                {
                    g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
                }
            }

            // Calculate the center position for the text
            string text = _isEditing ? "Press a key..." : GetDisplayText();
            SizeF textSize = g.MeasureString(text, Font);
            float x = (Width - textSize.Width) / 2;
            float y = (Height - textSize.Height) / 2;

            // Draw the text
            using (SolidBrush textBrush = new SolidBrush(ForeColor))
            {
                g.DrawString(text, Font, textBrush, x, y);
            }

            // Draw focus rectangle if focused
            if (Focused && !_isEditing)
            {
                ControlPaint.DrawFocusRectangle(g, new Rectangle(2, 2, Width - 4, Height - 4));
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            StartEditing();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            StopEditing();
        }

        private void StartEditing()
        {
            _isEditing = true;
            HotkeyFocusEntered?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        private void StopEditing()
        {
            _isEditing = false;
            HotkeyFocusLeft?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        public void SetKey(Keys keyData)
        {
            // Extract modifiers
            _ctrl = (keyData & Keys.Control) == Keys.Control;
            _alt = (keyData & Keys.Alt) == Keys.Alt;
            _shift = (keyData & Keys.Shift) == Keys.Shift;

            // Extract the actual key
            _key = keyData & Keys.KeyCode;

            // Handle special cases
            if (_key == Keys.Escape) // Clear the binding if Escape is pressed
            {
                _key = Keys.None;
                _ctrl = _alt = _shift = false;
                StopEditing();
                Invalidate();
                return;
            }

            // Ignore modifier keys when pressed alone
            if (_key == Keys.ControlKey || _key == Keys.ShiftKey || _key == Keys.Menu)
            {
                // Don't set the key, just keep editing
                return;
            }

            // Stop editing
            StopEditing();
            Invalidate();
        }

        public string GetDisplayText()
        {
            if (_key == Keys.None)
            {
                return "None";
            }

            string text = "";

            if (_ctrl) text += "Ctrl+";
            if (_alt) text += "Alt+";
            if (_shift) text += "Shift+";

            text += _key.ToString();

            return text;
        }

        public HotkeyBinding GetBinding()
        {
            if (_key == Keys.None) return null;

            return new HotkeyBinding
            {
                Key = _key,
                Alt = _alt,
                Shift = _shift,
                Ctrl = _ctrl
            };
        }

        public void SetBinding(HotkeyBinding binding)
        {
            if (binding == null)
            {
                _key = Keys.None;
                _alt = false;
                _shift = false;
                _ctrl = false;
            }
            else
            {
                _key = binding.Key;
                _alt = binding.Alt;
                _shift = binding.Shift;
                _ctrl = binding.Ctrl;
            }

            Invalidate();
        }
    }
}

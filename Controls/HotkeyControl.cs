using System;
using System.Drawing;
using System.Windows.Forms;
using poji.Models;

namespace poji.Controls
{
    /// <summary>
    /// A custom control for capturing and displaying keyboard hotkey combinations.
    /// </summary>
    public class HotkeyControl : Control
    {
        private Keys _key = Keys.None;
        private bool _alt;
        private bool _shift;
        private bool _ctrl;
        private bool _isEditing;
        private BorderStyle _borderStyle = BorderStyle.FixedSingle;

        /// <summary>
        /// Occurs when the control enters hotkey capture mode.
        /// </summary>
        public event EventHandler HotkeyFocusEntered;

        /// <summary>
        /// Occurs when the control exits hotkey capture mode.
        /// </summary>
        public event EventHandler HotkeyFocusLeft;

        /// <summary>
        /// Gets or sets the border style for the control.
        /// </summary>
        public BorderStyle BorderStyle
        {
            get => _borderStyle;
            set
            {
                _borderStyle = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Initializes a new instance of the HotkeyControl class.
        /// </summary>
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

            Click += (s, e) => StartEditing();
        }

        /// <summary>
        /// Paints the control.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            // Draw background
            using (SolidBrush backgroundBrush = new SolidBrush(BackColor))
            {
                g.FillRectangle(backgroundBrush, ClientRectangle);
            }

            // Draw border
            if (_borderStyle == BorderStyle.FixedSingle)
            {
                System.Drawing.Color borderColor = _isEditing ?
                    System.Drawing.Color.FromArgb(0, 120, 215) :
                    System.Drawing.Color.FromArgb(100, 100, 100);

                using (Pen borderPen = new Pen(borderColor))
                {
                    g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
                }
            }

            // Draw text
            string displayText = _isEditing ? "Press a key..." : GetDisplayText();
            SizeF textSize = g.MeasureString(displayText, Font);
            float x = (Width - textSize.Width) / 2;
            float y = (Height - textSize.Height) / 2;

            using (SolidBrush textBrush = new SolidBrush(ForeColor))
            {
                g.DrawString(displayText, Font, textBrush, x, y);
            }

            // Draw focus rectangle
            if (Focused && !_isEditing)
            {
                ControlPaint.DrawFocusRectangle(g, new Rectangle(2, 2, Width - 4, Height - 4));
            }
        }

        /// <summary>
        /// Handles the control gaining focus.
        /// </summary>
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            StartEditing();
        }

        /// <summary>
        /// Handles the control losing focus.
        /// </summary>
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

        /// <summary>
        /// Sets the hotkey from keyboard input.
        /// </summary>
        public void SetKey(Keys keyData)
        {
            // Extract modifiers
            _ctrl = (keyData & Keys.Control) == Keys.Control;
            _alt = (keyData & Keys.Alt) == Keys.Alt;
            _shift = (keyData & Keys.Shift) == Keys.Shift;

            // Extract actual key
            _key = keyData & Keys.KeyCode;

            // Handle special case: Escape clears the binding
            if (_key == Keys.Escape)
            {
                ClearHotkey();
                StopEditing();
                return;
            }

            // Ignore modifier keys when pressed alone
            if (IsModifierKey(_key))
            {
                return; // Keep editing
            }

            StopEditing();
            Invalidate();
        }

        private bool IsModifierKey(Keys key)
        {
            return key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.Menu;
        }

        private void ClearHotkey()
        {
            _key = Keys.None;
            _ctrl = _alt = _shift = false;
            Invalidate();
        }

        /// <summary>
        /// Gets a text representation of the current hotkey.
        /// </summary>
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

        /// <summary>
        /// Gets the current hotkey binding.
        /// </summary>
        /// <returns>A HotkeyBinding object, or null if no key is set.</returns>
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

        /// <summary>
        /// Sets the hotkey binding from a HotkeyBinding object.
        /// </summary>
        public void SetBinding(HotkeyBinding binding)
        {
            if (binding == null)
            {
                ClearHotkey();
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
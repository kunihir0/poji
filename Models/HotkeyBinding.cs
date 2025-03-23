using System.Windows.Forms;

namespace poji.Models
{
    /// <summary>
    /// Represents a keyboard hotkey combination including modifiers.
    /// </summary>
    public class HotkeyBinding
    {
        /// <summary>
        /// The primary key of the hotkey binding.
        /// </summary>
        public Keys Key { get; set; } = Keys.None;

        /// <summary>
        /// Indicates whether the Alt modifier is used.
        /// </summary>
        public bool Alt { get; set; }

        /// <summary>
        /// Indicates whether the Shift modifier is used.
        /// </summary>
        public bool Shift { get; set; }

        /// <summary>
        /// Indicates whether the Ctrl modifier is used.
        /// </summary>
        public bool Ctrl { get; set; }

        /// <summary>
        /// Gets a Keys value representing all active modifiers.
        /// </summary>
        public Keys ModifierKeys
        {
            get
            {
                Keys modifiers = Keys.None;
                if (Alt) modifiers |= Keys.Alt;
                if (Shift) modifiers |= Keys.Shift;
                if (Ctrl) modifiers |= Keys.Control;
                return modifiers;
            }
        }

        /// <summary>
        /// Gets the combined key value including modifiers.
        /// </summary>
        public Keys KeyWithModifiers => Key | ModifierKeys;

        /// <summary>
        /// Returns true if this hotkey binding has any valid key defined.
        /// </summary>
        public bool IsValid => Key != Keys.None;

        /// <summary>
        /// Creates a deep copy of this hotkey binding.
        /// </summary>
        public HotkeyBinding Clone()
        {
            return new HotkeyBinding
            {
                Key = this.Key,
                Alt = this.Alt,
                Shift = this.Shift,
                Ctrl = this.Ctrl
            };
        }
    }
}
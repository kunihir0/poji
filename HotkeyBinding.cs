using System.Windows.Forms;

namespace poji
{
    public class HotkeyBinding
    {
        public Keys Key { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public bool Ctrl { get; set; }

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
    }
}

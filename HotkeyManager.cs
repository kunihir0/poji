using System;
using System.Windows.Forms;

namespace poji
{
    public enum HotkeyAction
    {
        Exit,
        ToggleVisibility,
        ReloadCrosshair,
        SwitchMonitor
    }
    
    public class HotkeyEventArgs : EventArgs
    {
        public HotkeyAction Action { get; }
        public int Data { get; }
        
        public HotkeyEventArgs(HotkeyAction action, int data = 0)
        {
            Action = action;
            Data = data;
        }
    }
    
    public class HotkeyManager : IDisposable
    {
        private readonly GlobalKeyboardHook _keyboardHook;
        public event EventHandler<HotkeyEventArgs> HotkeyTriggered;
        
        public HotkeyManager()
        {
            _keyboardHook = new GlobalKeyboardHook();
            _keyboardHook.KeyDown += KeyboardHook_KeyDown;
            _keyboardHook.Start();
        }

        private void KeyboardHook_KeyDown(object sender, KeyEventArgs e)
        {
            // Check for hotkeys
            bool altPressed = (Control.ModifierKeys & Keys.Alt) != 0;
            bool shiftPressed = (Control.ModifierKeys & Keys.Shift) != 0;
            
            // Only handle our specific hotkey combinations
            if (altPressed && shiftPressed)
            {
                // Alt+Shift+W to exit
                if (e.KeyCode == Keys.W)
                {
                    HotkeyTriggered?.Invoke(this, new HotkeyEventArgs(HotkeyAction.Exit));
                    e.Handled = true;
                }
                
                // Alt+Shift+H to toggle visibility
                else if (e.KeyCode == Keys.H)
                {
                    HotkeyTriggered?.Invoke(this, new HotkeyEventArgs(HotkeyAction.ToggleVisibility));
                    e.Handled = true;
                }
                
                // Alt+Shift+R to reload crosshair
                else if (e.KeyCode == Keys.R)
                {
                    HotkeyTriggered?.Invoke(this, new HotkeyEventArgs(HotkeyAction.ReloadCrosshair));
                    e.Handled = true;
                }
                
                // Alt+Shift+[1-9] to switch monitors
                else if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9)
                {
                    int screenIndex = e.KeyCode - Keys.D1;
                    HotkeyTriggered?.Invoke(this, new HotkeyEventArgs(HotkeyAction.SwitchMonitor, screenIndex));
                    e.Handled = true;
                }
            }
        }
        
        public void Dispose()
        {
            _keyboardHook?.Stop();
            _keyboardHook?.Dispose();
        }
    }
}
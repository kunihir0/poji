using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace poji
{
    public enum HotkeyAction
    {
        Exit,
        ToggleVisibility,
        ReloadCrosshair,
        SwitchMonitor,
        ToggleRecoilPattern,
        StartRecoilGuidance,
        StopRecoilGuidance
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
        private Dictionary<string, HotkeyBinding> _hotkeyBindings;
        
        public event EventHandler<HotkeyEventArgs> HotkeyTriggered;
        
        public HotkeyManager()
        {
            _keyboardHook = new GlobalKeyboardHook();
            _keyboardHook.KeyDown += KeyboardHook_KeyDown;
            
            // Default hotkey bindings
            _hotkeyBindings = new Dictionary<string, HotkeyBinding>
            {
                ["ToggleVisibility"] = new HotkeyBinding { Key = Keys.H, Alt = true, Shift = true },
                ["Exit"] = new HotkeyBinding { Key = Keys.W, Alt = true, Shift = true },
                ["ReloadCrosshair"] = new HotkeyBinding { Key = Keys.R, Alt = true, Shift = true },
                ["SwitchMonitor"] = new HotkeyBinding { Key = Keys.D1, Alt = true, Shift = true }
            };
            
            _keyboardHook.Start();
        }
        
        private void KeyboardHook_KeyDown(object sender, KeyEventArgs e)
        {
            // Get current modifiers
            bool altPressed = (Control.ModifierKeys & Keys.Alt) != 0;
            bool shiftPressed = (Control.ModifierKeys & Keys.Shift) != 0;
            bool ctrlPressed = (Control.ModifierKeys & Keys.Control) != 0;
            
            // Check each binding
            foreach (var binding in _hotkeyBindings)
            {
                HotkeyBinding hotkey = binding.Value;
                if (hotkey == null) continue;
                
                // Check if key and modifiers match
                if (e.KeyCode == hotkey.Key &&
                    altPressed == hotkey.Alt &&
                    shiftPressed == hotkey.Shift &&
                    ctrlPressed == hotkey.Ctrl)
                {
                    // Handle special case for monitor switching
                    if (binding.Key == "SwitchMonitor" && e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9)
                    {
                        int screenIndex = e.KeyCode - Keys.D1;
                        HotkeyTriggered?.Invoke(this, new HotkeyEventArgs(HotkeyAction.SwitchMonitor, screenIndex));
                    }
                    else
                    {
                        // Trigger appropriate action
                        switch (binding.Key)
                        {
                            case "Exit":
                                HotkeyTriggered?.Invoke(this, new HotkeyEventArgs(HotkeyAction.Exit));
                                break;
                            case "ToggleVisibility":
                                HotkeyTriggered?.Invoke(this, new HotkeyEventArgs(HotkeyAction.ToggleVisibility));
                                break;
                            case "ReloadCrosshair":
                                HotkeyTriggered?.Invoke(this, new HotkeyEventArgs(HotkeyAction.ReloadCrosshair));
                                break;
                        }
                    }
                    
                    e.Handled = true;
                    break;
                }
            }
            
            // Legacy support for monitor switching
            if (altPressed && shiftPressed && e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9)
            {
                int screenIndex = e.KeyCode - Keys.D1;
                HotkeyTriggered?.Invoke(this, new HotkeyEventArgs(HotkeyAction.SwitchMonitor, screenIndex));
                e.Handled = true;
            }
        }
        
        public void UpdateBindings(Dictionary<string, HotkeyBinding> bindings)
        {
            if (bindings != null)
            {
                _hotkeyBindings = new Dictionary<string, HotkeyBinding>(bindings);
            }
        }
        
        public Dictionary<string, HotkeyBinding> GetBindings()
        {
            return new Dictionary<string, HotkeyBinding>(_hotkeyBindings);
        }
        
        public void Dispose()
        {
            _keyboardHook?.Stop();
            _keyboardHook?.Dispose();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using poji.Enums;
using poji.Hooks;
using poji.Models;

namespace poji
{
    /// <summary>
    /// Manages hotkey detection and triggering of associated actions.
    /// </summary>
    public class HotkeyManager : IDisposable
    {
        private readonly GlobalKeyboardHook _keyboardHook;
        private readonly Dictionary<string, HotkeyBinding> _hotkeyBindings;
        private bool _isDisposed;

        /// <summary>
        /// Occurs when a registered hotkey is triggered.
        /// </summary>
        public event EventHandler<HotkeyEventArgs> HotkeyTriggered;

        /// <summary>
        /// Initializes a new instance of the HotkeyManager class with default bindings.
        /// </summary>
        public HotkeyManager()
        {
            _keyboardHook = new GlobalKeyboardHook();
            _keyboardHook.KeyDown += OnKeyDown;

            _hotkeyBindings = CreateDefaultBindings();

            _keyboardHook.Start();
        }

        private Dictionary<string, HotkeyBinding> CreateDefaultBindings()
        {
            return new Dictionary<string, HotkeyBinding>
            {
                ["ToggleVisibility"] = new HotkeyBinding { Key = Keys.H, Alt = true, Shift = true },
                ["Exit"] = new HotkeyBinding { Key = Keys.W, Alt = true, Shift = true },
                ["ReloadCrosshair"] = new HotkeyBinding { Key = Keys.R, Alt = true, Shift = true },
                ["SwitchMonitor"] = new HotkeyBinding { Key = Keys.D1, Alt = true, Shift = true }
            };
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            bool altPressed = (Control.ModifierKeys & Keys.Alt) != 0;
            bool shiftPressed = (Control.ModifierKeys & Keys.Shift) != 0;
            bool ctrlPressed = (Control.ModifierKeys & Keys.Control) != 0;

            // First check for monitor switching hotkeys
            if (altPressed && shiftPressed && IsDigitKey(e.KeyCode))
            {
                int screenIndex = e.KeyCode - Keys.D1;
                TriggerHotkeyAction(HotkeyAction.SwitchMonitor, screenIndex);
                e.Handled = true;
                return;
            }

            // Check registered hotkey bindings
            foreach (var binding in _hotkeyBindings)
            {
                string actionName = binding.Key;
                HotkeyBinding hotkey = binding.Value;

                if (hotkey == null) continue;

                if (KeyMatchesBinding(e.KeyCode, altPressed, shiftPressed, ctrlPressed, hotkey))
                {
                    TriggerActionByName(actionName, e);
                    break;
                }
            }
        }

        private bool IsDigitKey(Keys key)
        {
            return key >= Keys.D1 && key <= Keys.D9;
        }

        private bool KeyMatchesBinding(Keys keyCode, bool alt, bool shift, bool ctrl, HotkeyBinding binding)
        {
            return keyCode == binding.Key &&
                   alt == binding.Alt &&
                   shift == binding.Shift &&
                   ctrl == binding.Ctrl;
        }

        private void TriggerActionByName(string actionName, KeyEventArgs e)
        {
            switch (actionName)
            {
                case "Exit":
                    TriggerHotkeyAction(HotkeyAction.Exit);
                    break;
                case "ToggleVisibility":
                    TriggerHotkeyAction(HotkeyAction.ToggleVisibility);
                    break;
                case "ReloadCrosshair":
                    TriggerHotkeyAction(HotkeyAction.ReloadCrosshair);
                    break;
                case "SwitchMonitor":
                    if (IsDigitKey(e.KeyCode))
                    {
                        int screenIndex = e.KeyCode - Keys.D1;
                        TriggerHotkeyAction(HotkeyAction.SwitchMonitor, screenIndex);
                    }
                    break;
            }

            e.Handled = true;
        }

        private void TriggerHotkeyAction(HotkeyAction action, int data = 0)
        {
            HotkeyTriggered?.Invoke(this, new HotkeyEventArgs(action, data));
        }

        /// <summary>
        /// Updates the hotkey bindings.
        /// </summary>
        /// <param name="bindings">The new bindings to use.</param>
        public void UpdateBindings(Dictionary<string, HotkeyBinding> bindings)
        {
            if (bindings == null) return;

            _hotkeyBindings.Clear();
            foreach (var binding in bindings)
            {
                _hotkeyBindings[binding.Key] = binding.Value?.Clone();
            }
        }

        /// <summary>
        /// Gets a copy of the current hotkey bindings.
        /// </summary>
        /// <returns>A dictionary of hotkey bindings.</returns>
        public Dictionary<string, HotkeyBinding> GetBindings()
        {
            var result = new Dictionary<string, HotkeyBinding>();

            foreach (var binding in _hotkeyBindings)
            {
                result[binding.Key] = binding.Value?.Clone();
            }

            return result;
        }

        /// <summary>
        /// Releases all resources used by the HotkeyManager.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases resources used by the HotkeyManager.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _keyboardHook?.Dispose();
                }

                _isDisposed = true;
            }
        }
    }
}
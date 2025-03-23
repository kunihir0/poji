using System;
using poji.Enums;

namespace poji.Models
{
    /// <summary>
    /// Provides data for the HotkeyTriggered event.
    /// </summary>
    public class HotkeyEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the action associated with the triggered hotkey.
        /// </summary>
        public HotkeyAction Action { get; }

        /// <summary>
        /// Gets additional data for the action (such as screen index for SwitchMonitor).
        /// </summary>
        public int Data { get; }

        /// <summary>
        /// Initializes a new instance of the HotkeyEventArgs class.
        /// </summary>
        /// <param name="action">The action to trigger.</param>
        /// <param name="data">Additional data for the action.</param>
        public HotkeyEventArgs(HotkeyAction action, int data = 0)
        {
            Action = action;
            Data = data;
        }
    }
}
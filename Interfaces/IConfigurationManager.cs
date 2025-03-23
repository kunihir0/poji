using System.Collections.Generic;
using poji.Models;
using poji.Enums;

namespace poji.Interfaces
{
    /// <summary>
    /// Interface for configuration management
    /// </summary>
    public interface IConfigurationManager
    {
        /// <summary>
        /// Current crosshair share code
        /// </summary>
        string ShareCode { get; }

        /// <summary>
        /// Current crosshair scale factor
        /// </summary>
        float Scale { get; }

        /// <summary>
        /// Current monitor index for overlay display
        /// </summary>
        int MonitorIndex { get; }

        /// <summary>
        /// Loads settings from storage
        /// </summary>
        void LoadSettings();

        /// <summary>
        /// Saves share code to configuration
        /// </summary>
        void SaveShareCode(string shareCode);

        /// <summary>
        /// Saves scale setting to configuration
        /// </summary>
        void SaveScale(float scale);

        /// <summary>
        /// Saves monitor index to configuration
        /// </summary>
        void SaveMonitorIndex(int monitorIndex);

        /// <summary>
        /// Saves all current settings to storage
        /// </summary>
        void SaveAllSettings();

        /// <summary>
        /// Gets all configured hotkey bindings
        /// </summary>
        Dictionary<HotkeyAction, HotkeyBinding> GetHotkeyBindings();

        /// <summary>
        /// Saves hotkey binding configuration
        /// </summary>
        void SaveHotkeyBindings(Dictionary<HotkeyAction, HotkeyBinding> bindings);

        /// <summary>
        /// Restores all settings to default values
        /// </summary>
        void RestoreDefaultSettings();
    }
}
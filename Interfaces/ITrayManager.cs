using System;
using poji.Models.Events;

namespace poji.Interfaces
{
    /// <summary>
    /// Interface for system tray operations
    /// </summary>
    public interface ITrayManager : IDisposable
    {
        // Events
        event EventHandler ExitRequested;
        event EventHandler ToggleVisibilityRequested;
        event EventHandler LoadCrosshairRequested;
        event EventHandler<MonitorChangeEventArgs> MonitorChangeRequested;
        event EventHandler<ScaleChangeEventArgs> ScaleChangeRequested;
        event EventHandler SettingsRequested;

        // Methods
        void Initialize();
        void SetIcon(System.Drawing.Icon icon);
        void UpdateMonitorList();
    }
}
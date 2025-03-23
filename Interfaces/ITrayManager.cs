using System;
using poji.Models.Events;

namespace poji.Interfaces
{
    /// <summary>
    /// Interface for system tray operations
    /// </summary>
    public interface ITrayManager : IDisposable
    {
        // Existing events
        event EventHandler ExitRequested;
        event EventHandler ToggleVisibilityRequested;
        event EventHandler LoadCrosshairRequested;
        event EventHandler<MonitorChangeEventArgs> MonitorChangeRequested;
        event EventHandler<ScaleChangeEventArgs> ScaleChangeRequested;
        event EventHandler SettingsRequested;

        // New events for MainForm functionality
        event EventHandler<RenderModeChangeEventArgs> RenderModeChangeRequested;
        event EventHandler ToggleDebugModeRequested;
        event EventHandler ShowColorCustomizationRequested;
        event EventHandler<OpacityChangeEventArgs> OpacityChangeRequested;
        event EventHandler ToggleRecoilSimulationRequested;

        // Methods
        void Initialize();
        void SetIcon(System.Drawing.Icon icon);
        void UpdateMonitorList();
    }
}
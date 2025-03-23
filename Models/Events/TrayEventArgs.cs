using System;
using poji.Rendering;

namespace poji.Models.Events
{
    /// <summary>
    /// Event arguments for monitor selection change events
    /// </summary>
    public class MonitorChangeEventArgs : EventArgs
    {
        public int MonitorIndex { get; }

        public MonitorChangeEventArgs(int monitorIndex)
        {
            MonitorIndex = monitorIndex;
        }
    }

    /// <summary>
    /// Event arguments for scale change events
    /// </summary>
    public class ScaleChangeEventArgs : EventArgs
    {
        public float ScaleFactor { get; }

        public ScaleChangeEventArgs(float scaleFactor)
        {
            ScaleFactor = scaleFactor;
        }
    }

    /// <summary>
    /// Event arguments for render mode change events
    /// </summary>
    public class RenderModeChangeEventArgs : EventArgs
    {
        public CrosshairRenderer.RenderMode Mode { get; }

        public RenderModeChangeEventArgs(CrosshairRenderer.RenderMode mode)
        {
            Mode = mode;
        }
    }

    /// <summary>
    /// Event arguments for opacity change events
    /// </summary>
    public class OpacityChangeEventArgs : EventArgs
    {
        public float Opacity { get; }

        public OpacityChangeEventArgs(float opacity)
        {
            Opacity = opacity;
        }
    }
}
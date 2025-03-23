using System;

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
}

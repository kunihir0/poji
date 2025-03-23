using System;
using GameOverlayInjection.Core;
using GameOverlayInjection.Overlay;
using SharpDX;

namespace GameOverlayInjection.Interfaces
{
    /// <summary>
    /// Implementation of the overlay interface for remote communication
    /// </summary>
    public class OverlayInterface : MarshalByRefObject, IOverlayInterface
    {
        public void SetCrosshairSettings(int size, int gap, int thickness, bool dot, Color4 color)
        {
            // Forward settings to the active overlay instance
            InjectedOverlay.Instance?.UpdateCrosshairSettings(size, gap, thickness, dot, color);
        }

        public void SetVisible(bool visible)
        {
            InjectedOverlay.Instance?.SetVisible(visible);
        }

        /// <summary>
        /// Override to control the lifetime of the remote object
        /// </summary>
        public override object InitializeLifetimeService()
        {
            // Return null to indicate an infinite lifetime
            return null;
        }
    }
}
using SharpDX;

namespace GameOverlayInjection.Core
{
    /// <summary>
    /// Remote interface for communicating with the injected library
    /// </summary>
    public interface IOverlayInterface
    {
        /// <summary>
        /// Update crosshair appearance settings
        /// </summary>
        void SetCrosshairSettings(int size, int gap, int thickness, bool dot, Color4 color);

        /// <summary>
        /// Show or hide the crosshair overlay
        /// </summary>
        void SetVisible(bool visible);
    }
}
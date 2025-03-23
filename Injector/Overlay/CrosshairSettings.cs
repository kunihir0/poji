using SharpDX;

namespace GameOverlayInjection.Overlay
{
    /// <summary>
    /// Stores all configurable parameters for the crosshair appearance
    /// </summary>
    public class CrosshairSettings
    {
        /// <summary>
        /// Size of each crosshair line in pixels
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Gap between center and crosshair lines in pixels
        /// </summary>
        public int Gap { get; set; }

        /// <summary>
        /// Thickness of lines in pixels
        /// </summary>
        public int Thickness { get; set; }

        /// <summary>
        /// Whether to draw a center dot
        /// </summary>
        public bool Dot { get; set; }

        /// <summary>
        /// Color of the crosshair (RGBA)
        /// </summary>
        public Color4 Color { get; set; }

        /// <summary>
        /// Create crosshair settings with default values
        /// </summary>
        public static CrosshairSettings CreateDefault()
        {
            return new CrosshairSettings
            {
                Size = 5,
                Gap = 2,
                Thickness = 1,
                Dot = true,
                Color = new Color4(0.0f, 1.0f, 0.0f, 1.0f) // Green
            };
        }
    }
}
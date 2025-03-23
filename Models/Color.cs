namespace poji.Models
{
    /// <summary>
    /// Represents an RGBA color for a crosshair.
    /// </summary>
    public class Color
    {
        public byte Red { get; set; }
        public byte Green { get; set; }
        public byte Blue { get; set; }
        public byte Alpha { get; set; }

        public Color(byte red, byte green, byte blue, byte alpha = 255)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        /// <summary>
        /// Converts the color to a hex string.
        /// </summary>
        /// <returns>The color in hex format (#RRGGBB).</returns>
        public string ToHex()
        {
            return $"#{Red:X2}{Green:X2}{Blue:X2}";
        }

        /// <summary>
        /// Converts the color to an RGBA string.
        /// </summary>
        /// <returns>The color in rgba format (rgba(R, G, B, A)).</returns>
        public string ToRgba()
        {
            return $"rgba({Red}, {Green}, {Blue}, {Alpha / 255.0:F2})";
        }

        /// <summary>
        /// Applies default colors based on the color index.
        /// </summary>
        /// <param name="colorIndex">The index of the color preset (1-5).</param>
        public void ApplyDefaultColors(int colorIndex)
        {
            switch (colorIndex)
            {
                case 1: // Green
                    Red = 0;
                    Green = 255;
                    Blue = 0;
                    break;
                case 2: // Yellow
                    Red = 255;
                    Green = 255;
                    Blue = 0;
                    break;
                case 3: // Blue
                    Red = 0;
                    Green = 0;
                    Blue = 255;
                    break;
                case 4: // Cyan
                    Red = 0;
                    Green = 255;
                    Blue = 255;
                    break;
                    // Default (colorIndex = 5) uses custom RGB values
            }
        }

        /// <summary>
        /// Converts to a System.Drawing.Color.
        /// </summary>
        /// <returns>A System.Drawing.Color representation of this color.</returns>
        public System.Drawing.Color ToDrawingColor()
        {
            return System.Drawing.Color.FromArgb(Alpha, Red, Green, Blue);
        }

        /// <summary>
        /// Creates a black color with full opacity for outlines.
        /// </summary>
        /// <returns>A System.Drawing.Color representing black.</returns>
        public static System.Drawing.Color GetOutlineColor()
        {
            return System.Drawing.Color.FromArgb(255, 0, 0, 0);
        }
    }
}
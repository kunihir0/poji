using System;

namespace poji.Utils
{
    /// <summary>
    /// Utility methods for crosshair calculations.
    /// </summary>
    public static class CrosshairUtils
    {
        /// <summary>
        /// Converts an unsigned byte to a signed byte.
        /// </summary>
        /// <param name="number">The unsigned byte to convert.</param>
        /// <returns>The signed byte equivalent.</returns>
        public static sbyte Uint8ToInt8(byte number)
        {
            return (sbyte)(number < 128 ? number : number - 256);
        }

        /// <summary>
        /// Rounds up a value to the nearest odd number.
        /// </summary>
        /// <param name="value">The value to round.</param>
        /// <returns>The nearest odd number that's greater than or equal to the input.</returns>
        public static float RoundUpToOdd(float value)
        {
            int intValue = (int)Math.Ceiling(value);
            return intValue % 2 == 0 ? intValue + 1 : intValue;
        }

        /// <summary>
        /// Maps gap values according to the Python implementation's logic.
        /// </summary>
        /// <param name="x">The gap value to map.</param>
        /// <returns>The mapped gap value.</returns>
        public static float MapGapValue(float x)
        {
            if (x > -5)
                return x - (-5);
            else if (x < -5)
                return ((x + 5) * -1) - 5;
            else
                return 0;
        }
    }
}
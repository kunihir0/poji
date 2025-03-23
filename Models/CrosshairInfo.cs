using System;
using poji.Enums;
using poji.Utils;

namespace poji.Models
{
    /// <summary>
    /// Contains all information about a CS:GO crosshair configuration.
    /// </summary>
    public class CrosshairInfo
    {
        // Core properties
        public CrosshairStyle Style { get; set; }
        public bool HasCenterDot { get; set; }
        public float Length { get; set; }
        public float Thickness { get; set; }
        public float Gap { get; set; }
        public bool HasOutline { get; set; }
        public float Outline { get; set; }
        public Color Color { get; private set; }
        public bool HasAlpha { get; set; }
        public int SplitDistance { get; set; }
        public float InnerSplitAlpha { get; set; }
        public float OuterSplitAlpha { get; set; }
        public float SplitSizeRatio { get; set; }
        public bool IsTStyle { get; set; }
        public float FixedCrosshairGap { get; set; }
        public int ColorIndex { get; set; }
        public bool DeployedWeaponGapEnabled { get; set; }
        public bool FollowRecoil { get; set; }
        public float ScaleFactor { get; set; } = 1.0f;

        // Raw data for debugging
        public byte[] RawData { get; private set; }
        public string ShareCode { get; private set; }

        // Simple properties
        public float Size { get; set; }
        public bool Dot { get; set; }
        public bool DotOnly { get; set; }
        public bool T { get; set; }
        public bool ShowDebugText { get; set; }

        // Color component accessors
        public byte ColorR
        {
            get => Color.Red;
            set => Color.Red = value;
        }

        public byte ColorG
        {
            get => Color.Green;
            set => Color.Green = value;
        }

        public byte ColorB
        {
            get => Color.Blue;
            set => Color.Blue = value;
        }

        public byte Alpha
        {
            get => Color.Alpha;
            set => Color.Alpha = value;
        }

        /// <summary>
        /// Default constructor creating a simple green crosshair.
        /// </summary>
        public CrosshairInfo()
        {
            // Default to a simple green crosshair
            Size = 5;
            Gap = 2;
            Thickness = 1;
            Outline = 0;

            Color = new Color(0, 255, 0, 255);

            Dot = true;
            DotOnly = false;
            T = true;
            ShowDebugText = false;

            Length = Size;
        }

        /// <summary>
        /// Constructor used when decoding a share code.
        /// </summary>
        /// <param name="bytes">The decoded bytes from the share code.</param>
        /// <param name="shareCode">The original share code.</param>
        public CrosshairInfo(byte[] bytes, string shareCode)
        {
            ValidateBytes(bytes);

            // Store raw data
            RawData = bytes;
            ShareCode = shareCode;

            DecodeFromBytes(bytes);
        }

        private void ValidateBytes(byte[] bytes)
        {
            if (bytes.Length < 15)
            {
                throw new ArgumentException("Not enough bytes to decode CrosshairInfo (need at least 15)");
            }
        }

        private void DecodeFromBytes(byte[] bytes)
        {
            // Decode properties to match Python implementation
            Gap = CrosshairUtils.Uint8ToInt8(bytes[3]) / 10.0f;
            Outline = bytes[4] / 2.0f;
            Color = new Color(bytes[5], bytes[6], bytes[7], bytes[8]);

            SplitDistance = (int)bytes[9];
            FollowRecoil = ((bytes[9] >> 7) & 1) == 1;

            FixedCrosshairGap = CrosshairUtils.Uint8ToInt8(bytes[10]) / 10.0f;

            ColorIndex = bytes[11] & 7;
            HasOutline = ((bytes[11] >> 3) & 1) == 1;
            InnerSplitAlpha = ((bytes[11] >> 4) & 0xF) / 10.0f;

            OuterSplitAlpha = (bytes[12] & 0xF) / 10.0f;
            SplitSizeRatio = ((bytes[12] >> 4) & 0xF) / 10.0f;

            Thickness = bytes[13] / 10.0f;

            // Decode flags from bytes[14]
            HasCenterDot = ((bytes[14] >> 4) & 1) == 1;
            DeployedWeaponGapEnabled = ((bytes[14] >> 5) & 1) == 1;
            HasAlpha = ((bytes[14] >> 6) & 1) == 1;
            IsTStyle = ((bytes[14] >> 7) & 1) == 1;

            // Style from lower nibble of bytes[14]
            Style = (CrosshairStyle)((bytes[14] & 0xF) >> 1);

            // Length (and Size)
            Length = bytes[15] / 10.0f;
            Size = Length;

            // Apply default colors if a preset color is selected
            if (ColorIndex != 5)
            {
                Color.ApplyDefaultColors(ColorIndex);
            }

            // Map additional simple properties
            Dot = HasCenterDot;
            DotOnly = false;
            T = IsTStyle;
            ShowDebugText = false;
        }

        /// <summary>
        /// Gets the length scaled by the scale factor.
        /// </summary>
        public float GetScaledLength() => Length * ScaleFactor;

        /// <summary>
        /// Gets the thickness scaled by the scale factor.
        /// </summary>
        public float GetScaledThickness() => Thickness * ScaleFactor;

        /// <summary>
        /// Gets the gap scaled by the scale factor.
        /// </summary>
        public float GetScaledGap() => Gap * ScaleFactor;

        /// <summary>
        /// Gets the outline scaled by the scale factor.
        /// </summary>
        public float GetScaledOutline() => Outline * ScaleFactor;

        /// <summary>
        /// Gets the size for rendering.
        /// </summary>
        public float GetRenderSize() => CrosshairUtils.RoundUpToOdd(2 * Length);

        /// <summary>
        /// Gets the thickness for rendering.
        /// </summary>
        public float GetRenderThickness() => (float)Math.Floor(CrosshairUtils.RoundUpToOdd(2 * Thickness) / 2);

        /// <summary>
        /// Gets the gap for rendering.
        /// </summary>
        public float GetRenderGap() => CrosshairUtils.RoundUpToOdd(2 * CrosshairUtils.MapGapValue(Gap));

        /// <summary>
        /// Gets the System.Drawing.Color representation.
        /// </summary>
        public System.Drawing.Color GetColor()
        {
            return Color.ToDrawingColor();
        }

        /// <summary>
        /// Gets the outline color.
        /// </summary>
        public System.Drawing.Color GetOutlineColor()
        {
            return Models.Color.GetOutlineColor();
        }

        /// <summary>
        /// Creates a clone of this CrosshairInfo.
        /// </summary>
        public CrosshairInfo Clone()
        {
            return (CrosshairInfo)MemberwiseClone();
        }
    }
}
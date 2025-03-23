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
            Style = CrosshairStyle.Classic;
            FollowRecoil = true;
            HasCenterDot = false;
            Length = 3.9f;
            Thickness = 0.6f;
            Gap = -2.2f;
            HasOutline = true;
            Outline = 1.0f;
            Color = new Color(0, 255, 0, 200);
            HasAlpha = true;
            SplitDistance = 3;
            InnerSplitAlpha = 0.0f;
            OuterSplitAlpha = 1.0f;
            SplitSizeRatio = 1.0f;
            IsTStyle = false;
            DeployedWeaponGapEnabled = true;
            FixedCrosshairGap = 3.0f;

            // Map simple properties
            Size = Length;
            Dot = HasCenterDot;
            DotOnly = false;
            T = IsTStyle;
            ShowDebugText = false;
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
            if (bytes.Length < 16)
            {
                throw new ArgumentException("Not enough bytes to decode CrosshairInfo (need at least 16)");
            }
        }

        private void DecodeFromBytes(byte[] bytes)
        {
            // Decode gap from byte 2
            Gap = CrosshairUtils.Uint8ToInt8(bytes[2]) / 10.0f;
            
            // Decode outline from byte 3
            Outline = bytes[3] / 2.0f;
            
            // Decode color from bytes 4-7
            Color = new Color(bytes[4], bytes[5], bytes[6], bytes[7]);
            
            // Decode split distance and follow recoil from byte 8
            SplitDistance = bytes[8] & 127;
            FollowRecoil = ((bytes[8] >> 7) & 1) == 1;
            
            // Decode fixed crosshair gap from byte 9
            FixedCrosshairGap = CrosshairUtils.Uint8ToInt8(bytes[9]) / 10.0f;
            
            // Decode color index, outline enabled, and inner split alpha from byte 10
            ColorIndex = bytes[10] & 7;
            HasOutline = ((bytes[10] >> 3) & 1) == 1;
            InnerSplitAlpha = Math.Min(1.0f, ((bytes[10] >> 4) & 0xF) / 10.0f);
            
            // Decode outer split alpha and split size ratio from byte 11
            OuterSplitAlpha = Math.Min(1.0f, (bytes[11] & 0xF) / 10.0f);
            SplitSizeRatio = Math.Min(1.0f, ((bytes[11] >> 4) & 0xF) / 10.0f);
            
            // Decode thickness from byte 12
            Thickness = (bytes[12] & 63) / 10.0f;
            
            // Decode flags from byte 13
            Style = (CrosshairStyle)((bytes[13] & 0xF) >> 1);
            HasCenterDot = ((bytes[13] >> 4) & 1) == 1;
            DeployedWeaponGapEnabled = ((bytes[13] >> 5) & 1) == 1;
            HasAlpha = ((bytes[13] >> 6) & 1) == 1;
            IsTStyle = ((bytes[13] >> 7) & 1) == 1;
            
            // Decode length from bytes 14-15
            int temp = bytes[15] & 31;
            Length = ((temp << 8) | bytes[14]) / 10.0f;
            
            // Apply default colors if a preset color is selected
            if (ColorIndex != 5)
            {
                if (ColorIndex < 0 || ColorIndex >= 5)
                {
                    ColorIndex = 1; // Default to green if out of range
                }
                Color.ApplyDefaultColors(ColorIndex);
            }

            // Map additional simple properties
            Size = Length;
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
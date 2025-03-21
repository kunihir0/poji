using System;
using System.Text.RegularExpressions;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;

namespace poji
{
    // Main decoder class
    public class CsgoCrosshairDecoder
    {
        private const string DICTIONARY = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefhijkmnopqrstuvwxyz23456789";
        private static readonly Regex SHARECODE_PATTERN = new Regex(@"^CSGO(-?[\w]{5}){5}$");

        public CrosshairInfo DecodeShareCodeToCrosshairInfo(string shareCode)
        {
            if (!SHARECODE_PATTERN.IsMatch(shareCode))
            {
                throw new InvalidSharecodeException("Invalid share code format. Must be like: CSGO-XXXXX-XXXXX-XXXXX-XXXXX-XXXXX");
            }

            // Remove prefix and dashes
            string code = shareCode.Substring(5).Replace("-", "");
            
            // Decode bytes
            byte[] bytes = Base58Decode(code);
            
            // Verify crosshair checksum
            // ValidateChecksum(bytes);
            
            // Create crosshair object
            return new CrosshairInfo(bytes, shareCode);
        }

        private byte[] Base58Decode(string code)
        {
            // Convert to big integer
            BigInteger big = 0;
            BigInteger baseNum = DICTIONARY.Length;

            foreach (char c in code.Reverse())
            {
                int pos = DICTIONARY.IndexOf(c);
                if (pos == -1)
                {
                    throw new InvalidSharecodeException($"Invalid character '{c}' in share code");
                }
                big = big * baseNum + pos;
            }

            // Convert to byte array
            var bytes = new List<byte>();
            while (big > 0)
            {
                bytes.Add((byte)(big % 256));
                big /= 256;
            }

            // Add padding to ensure 19 bytes total
            while (bytes.Count < 19)
            {
                bytes.Add(0);
            }

            // The bytes should be in reverse order for correct interpretation
            bytes.Reverse();
            return bytes.ToArray();
        }

        private void ValidateChecksum(byte[] bytes)
        {
            int sum = 0;
            for (int i = 1; i < bytes.Length; i++)
            {
                sum += bytes[i];
            }
            if (bytes[0] != (sum % 256))
            {
                throw new InvalidSharecodeException("Invalid crosshair share code: checksum mismatch");
            }
        }

        // Helper method to convert uint8 to int8 (signed byte)
        public static sbyte Uint8ToInt8(byte number)
        {
            return (sbyte)(number < 128 ? number : number - 256);
        }

        // Helper method to round up to odd number
        public static float RoundUpToOdd(float value)
        {
            int intValue = (int)Math.Ceiling(value);
            return intValue % 2 == 0 ? intValue + 1 : intValue;
        }

        // Helper method to map gap value similar to Python implementation
        public static float MapGapValue(float x)
        {
            if (x > -5)
                return x - (-5);
            else if (x < -5)
                return ((x + 5) * -1) - 5;
            else
                return 0;
        }

        // Exception class
        public class InvalidSharecodeException : Exception
        {
            public InvalidSharecodeException(string message = "Invalid share code") : base(message) { }
        }

        // Crosshair style enum (adjusted to match Python)
        public enum CrosshairStyle
        {
            Default,
            DefaultStatic,
            Classic,
            ClassicDynamic,
            ClassicStatic
        }

        // RGB color structure
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

            public string ToHex()
            {
                return $"#{Red:X2}{Green:X2}{Blue:X2}";
            }

            public string ToRgba()
            {
                return $"rgba({Red}, {Green}, {Blue}, {Alpha / 255.0:F2})";
            }

            // Apply default colors based on color index
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
        }

        // Crosshair information class
        public class CrosshairInfo
        {
            // Core properties
            public CrosshairStyle Style { get; private set; }
            public bool HasCenterDot { get; private set; }
            public float Length { get; private set; }
            public float Thickness { get; private set; }
            public float Gap { get; private set; }
            public bool HasOutline { get; private set; }
            public float Outline { get; private set; }
            public Color Color { get; private set; }
            public bool HasAlpha { get; private set; }
            public int SplitDistance { get; private set; }
            public float InnerSplitAlpha { get; private set; }
            public float OuterSplitAlpha { get; private set; }
            public float SplitSizeRatio { get; private set; }
            public bool IsTStyle { get; private set; }
            public float FixedCrosshairGap { get; private set; }
            public int ColorIndex { get; private set; }
            public bool DeployedWeaponGapEnabled { get; private set; }
            public bool FollowRecoil { get; private set; }
            public float ScaleFactor { get; set; } = 1.0f;
            
            // Raw values for debugging
            public byte[] RawData { get; private set; }
            public string ShareCode { get; private set; }

            public CrosshairInfo(byte[] bytes, string shareCode)
            {
                if (bytes.Length < 15)
                {
                    throw new ArgumentException("Not enough bytes to decode CrosshairInfo (need at least 15)");
                }

                // Store raw data
                RawData = bytes;
                ShareCode = shareCode;
                
                // Decode properties to match Python implementation
                Gap = Uint8ToInt8(bytes[3]) / 10.0f;
                Outline = bytes[4] / 2.0f;
                Color = new Color(bytes[5], bytes[6], bytes[7], bytes[8]);
                SplitDistance = (int)bytes[9];
                FollowRecoil = ((bytes[9] >> 7) & 1) == 1;
                FixedCrosshairGap = Uint8ToInt8(bytes[10]) / 10.0f;
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
                
                // Length
                Length = bytes[15] / 10.0f;
                
                // Apply default colors if a preset color is selected
                if (ColorIndex != 5)
                {
                    Color.ApplyDefaultColors(ColorIndex);
                }
            }

            public float GetScaledLength() => Length * ScaleFactor;
            public float GetScaledThickness() => Thickness * ScaleFactor;
            public float GetScaledGap() => Gap * ScaleFactor;
            public float GetScaledOutline() => Outline * ScaleFactor;
            
            // Methods for rendering calculations to match Python implementation
            public float GetRenderSize() => RoundUpToOdd(2 * Length);
            public float GetRenderThickness() => (float)Math.Floor(RoundUpToOdd(2 * Thickness) / 2);
            public float GetRenderGap() => RoundUpToOdd(2 * MapGapValue(Gap));
            
            public System.Drawing.Color GetColor() 
            {
                return System.Drawing.Color.FromArgb(Color.Alpha, Color.Red, Color.Green, Color.Blue);
            }
            
            public System.Drawing.Color GetOutlineColor() 
            {
                return System.Drawing.Color.FromArgb(255, 0, 0, 0); // Black outline
            }

            public string ToConsoleCommands()
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("// CS:GO Console Commands for this crosshair");
                sb.AppendLine($"cl_crosshairstyle {(int)Style + 1}");
                sb.AppendLine($"cl_crosshairsize {Length}");
                sb.AppendLine($"cl_crosshairthickness {Thickness}");
                sb.AppendLine($"cl_crosshairgap {Gap}");
                sb.AppendLine($"cl_crosshair_drawoutline {(HasOutline ? "1" : "0")}");
                sb.AppendLine($"cl_crosshair_outlinethickness {Outline}");
                sb.AppendLine($"cl_crosshaircolor {ColorIndex}");
                sb.AppendLine($"cl_crosshaircolor_r {Color.Red}");
                sb.AppendLine($"cl_crosshaircolor_g {Color.Green}");
                sb.AppendLine($"cl_crosshaircolor_b {Color.Blue}");
                sb.AppendLine($"cl_crosshairusealpha {(HasAlpha ? "1" : "0")}");
                sb.AppendLine($"cl_crosshairalpha {Color.Alpha}");
                sb.AppendLine($"cl_crosshairdot {(HasCenterDot ? "1" : "0")}");
                sb.AppendLine($"cl_crosshair_t {(IsTStyle ? "1" : "0")}");
                sb.AppendLine($"cl_crosshairgap_useweaponvalue {(DeployedWeaponGapEnabled ? "1" : "0")}");
                sb.AppendLine($"cl_crosshair_dynamic_splitdist {SplitDistance}");
                sb.AppendLine($"cl_fixedcrosshairgap {FixedCrosshairGap}");
                sb.AppendLine($"cl_crosshair_dynamic_splitalpha_innermod {InnerSplitAlpha}");
                sb.AppendLine($"cl_crosshair_dynamic_splitalpha_outermod {OuterSplitAlpha}");
                sb.AppendLine($"cl_crosshair_dynamic_maxdist_splitratio {SplitSizeRatio}");
                return sb.ToString();
            }

            public string ToJson()
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine($"  \"shareCode\": \"{ShareCode}\",");
                sb.AppendLine($"  \"style\": \"{StyleToString(Style)}\",");
                sb.AppendLine($"  \"hasCenterDot\": {HasCenterDot.ToString().ToLower()},");
                sb.AppendLine($"  \"length\": {Length},");
                sb.AppendLine($"  \"thickness\": {Thickness},");
                sb.AppendLine($"  \"gap\": {Gap},");
                sb.AppendLine($"  \"hasOutline\": {HasOutline.ToString().ToLower()},");
                sb.AppendLine($"  \"outline\": {Outline},");
                sb.AppendLine($"  \"color\": {ColorIndex},");
                sb.AppendLine("  \"rgb\": {");
                sb.AppendLine($"    \"red\": {Color.Red},");
                sb.AppendLine($"    \"green\": {Color.Green},");
                sb.AppendLine($"    \"blue\": {Color.Blue},");
                sb.AppendLine($"    \"hex\": \"{Color.ToHex()}\"");
                sb.AppendLine("  },");
                sb.AppendLine($"  \"alpha\": {Color.Alpha},");
                sb.AppendLine($"  \"hasAlpha\": {HasAlpha.ToString().ToLower()},");
                sb.AppendLine($"  \"splitDistance\": {SplitDistance},");
                sb.AppendLine($"  \"innerSplitAlpha\": {InnerSplitAlpha},");
                sb.AppendLine($"  \"outerSplitAlpha\": {OuterSplitAlpha},");
                sb.AppendLine($"  \"splitSizeRatio\": {SplitSizeRatio},");
                sb.AppendLine($"  \"isTStyle\": {IsTStyle.ToString().ToLower()},");
                sb.AppendLine($"  \"followRecoil\": {FollowRecoil.ToString().ToLower()},");
                sb.AppendLine($"  \"fixedCrosshairGap\": {FixedCrosshairGap},");
                sb.AppendLine($"  \"deployedWeaponGapEnabled\": {DeployedWeaponGapEnabled.ToString().ToLower()}");
                sb.AppendLine("}");
                return sb.ToString();
            }
            
            private string StyleToString(CrosshairStyle style)
            {
                switch (style)
                {
                    case CrosshairStyle.Default: return "Default";
                    case CrosshairStyle.DefaultStatic: return "Default Static";
                    case CrosshairStyle.Classic: return "Classic";
                    case CrosshairStyle.ClassicDynamic: return "Classic Dynamic";
                    case CrosshairStyle.ClassicStatic: return "Classic Static";
                    default: return "Unknown";
                }
            }
            
            public CrosshairInfo Clone()
            {
                return (CrosshairInfo)MemberwiseClone();
            }
        }
    }
}
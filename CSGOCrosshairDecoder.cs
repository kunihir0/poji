using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace poji
{
    public class CSGOCrosshairDecoder
    {
        // Dictionary and regex pattern
        private const string DICTIONARY = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefhijkmnopqrstuvwxyz23456789";
        private static readonly Regex SHARECODE_PATTERN = new Regex(@"^CSGO(-?[\w]{5}){5}$");

        // Crosshair style enum
        public enum CrosshairStyle
        {
            Default,
            DefaultStatic,
            Classic,
            ClassicDynamic,
            ClassicStatic
        }

        // Crosshair info structure
        public class CrosshairInfo
        {
            public CrosshairStyle Style { get; set; }
            public bool HasCenterDot { get; set; }
            public float Length { get; set; }
            public float Thickness { get; set; }
            public float Gap { get; set; }
            public bool HasOutline { get; set; }
            public float Outline { get; set; }
            public int Red { get; set; }
            public int Green { get; set; }
            public int Blue { get; set; }
            public bool HasAlpha { get; set; }
            public int Alpha { get; set; }
            public int SplitDistance { get; set; }
            public float InnerSplitAlpha { get; set; }
            public float OuterSplitAlpha { get; set; }
            public float SplitSizeRatio { get; set; }
            public bool IsTStyle { get; set; }

            // Method to generate console commands for CS:GO
            public string ToConsoleCommands()
            {
                var sb = new StringBuilder();
                sb.AppendLine("// CS:GO Console Commands for this crosshair");
                sb.AppendLine($"cl_crosshairstyle {(int)Style + 1}");
                sb.AppendLine($"cl_crosshairdot {(HasCenterDot ? "1" : "0")}");
                sb.AppendLine($"cl_crosshairsize {Length}");
                sb.AppendLine($"cl_crosshairthickness {Thickness}");
                sb.AppendLine($"cl_crosshairgap {Gap}");
                sb.AppendLine($"cl_crosshair_drawoutline {(HasOutline ? "1" : "0")}");
                sb.AppendLine($"cl_crosshair_outlinethickness {Outline}");
                sb.AppendLine($"cl_crosshaircolor_r {Red}");
                sb.AppendLine($"cl_crosshaircolor_g {Green}");
                sb.AppendLine($"cl_crosshaircolor_b {Blue}");
                sb.AppendLine($"cl_crosshairusealpha {(HasAlpha ? "1" : "0")}");
                sb.AppendLine($"cl_crosshairalpha {Alpha}");
                sb.AppendLine($"cl_crosshair_t {(IsTStyle ? "1" : "0")}");
                sb.AppendLine("cl_crosshairgap_useweaponvalue 0");
                return sb.ToString();
            }

            // Get the color as a System.Drawing.Color
            public Color GetColor()
            {
                return Color.FromArgb(Alpha, Red, Green, Blue);
            }
        }

        // Convert CrosshairStyle to string
        public static string StyleToString(CrosshairStyle style)
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

        // Convert RGB to hex color string
        public static string RgbToHex(int r, int g, int b)
        {
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        // Decode share code to byte array
        public byte[] DecodeShareCode(string shareCode)
        {
            // Validate share code format
            if (!SHARECODE_PATTERN.IsMatch(shareCode))
            {
                throw new ArgumentException("Invalid share code format. Must be like: CSGO-XXXXX-XXXXX-XXXXX-XXXXX-XXXXX");
            }

            // Remove prefix and dashes
            string code = shareCode.Substring(5);
            code = code.Replace("-", "");

            // Convert to big integer (reversed)
            BigInteger big = 0;
            BigInteger baseNum = DICTIONARY.Length;

            foreach (char c in code.Reverse())
            {
                int pos = DICTIONARY.IndexOf(c);
                if (pos == -1)
                {
                    throw new ArgumentException("Invalid character in share code");
                }
                big = big * baseNum + pos;
            }

            // Convert to byte array (little-endian)
            List<byte> bytes = new List<byte>();
            while (big > 0)
            {
                bytes.Add((byte)(big % 256));
                big /= 256;
            }

            // Add padding if needed
            if (bytes.Count == 18)
            {
                bytes.Add(0);
            }

            // Convert to big-endian
            bytes.Reverse();
            return bytes.ToArray();
        }

        // Decode crosshair info from byte array
        public CrosshairInfo DecodeCrosshairInfo(byte[] bytes)
        {
            if (bytes.Length < 16)
            {
                throw new ArgumentException("Not enough bytes to decode CrosshairInfo (need at least 16)");
            }

            var info = new CrosshairInfo
            {
                // Decode values
                Outline = bytes[4] / 2.0f,
                Red = bytes[5],
                Green = bytes[6],
                Blue = bytes[7],
                Alpha = bytes[8],
                SplitDistance = bytes[9],
                InnerSplitAlpha = (bytes[11] >> 4) / 10.0f,
                HasOutline = (bytes[11] & 8) != 0,
                OuterSplitAlpha = (bytes[12] & 0xF) / 10.0f,
                SplitSizeRatio = (bytes[12] >> 4) / 10.0f,
                Thickness = bytes[13] / 10.0f,
                Length = bytes[15] / 10.0f,
                Gap = (sbyte)bytes[3] / 10.0f
            };

            // Handle byte[14] flags
            byte upperNibble = (byte)(bytes[14] >> 4);
            info.HasCenterDot = (upperNibble & 1) != 0;
            info.HasAlpha = (upperNibble & 4) != 0;
            info.IsTStyle = (upperNibble & 8) != 0;

            // Get style
            int styleValue = (bytes[14] & 0xF) >> 1;
            info.Style = (CrosshairStyle)styleValue;

            return info;
        }

        // Format crosshair info as JSON string
        public string CrosshairInfoToJson(CrosshairInfo info)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"style\": \"{StyleToString(info.Style)}\",");
            sb.AppendLine($"  \"hasCenterDot\": {info.HasCenterDot.ToString().ToLower()},");
            sb.AppendLine($"  \"length\": {info.Length},");
            sb.AppendLine($"  \"thickness\": {info.Thickness},");
            sb.AppendLine($"  \"gap\": {info.Gap},");
            sb.AppendLine($"  \"hasOutline\": {info.HasOutline.ToString().ToLower()},");
            sb.AppendLine($"  \"outline\": {info.Outline},");
            sb.AppendLine("  \"color\": {");
            sb.AppendLine($"    \"red\": {info.Red},");
            sb.AppendLine($"    \"green\": {info.Green},");
            sb.AppendLine($"    \"blue\": {info.Blue},");
            sb.AppendLine($"    \"hex\": \"{RgbToHex(info.Red, info.Green, info.Blue)}\"");
            sb.AppendLine("  },");
            sb.AppendLine($"  \"alpha\": {info.Alpha},");
            sb.AppendLine($"  \"hasAlpha\": {info.HasAlpha.ToString().ToLower()},");
            sb.AppendLine($"  \"splitDistance\": {info.SplitDistance},");
            sb.AppendLine($"  \"innerSplitAlpha\": {info.InnerSplitAlpha},");
            sb.AppendLine($"  \"outerSplitAlpha\": {info.OuterSplitAlpha},");
            sb.AppendLine($"  \"splitSizeRatio\": {info.SplitSizeRatio},");
            sb.AppendLine($"  \"isTStyle\": {info.IsTStyle.ToString().ToLower()}");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
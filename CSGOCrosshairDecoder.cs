using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using poji.Exceptions;
using poji.Models;

namespace poji
{
    /// <summary>
    /// Main decoder class for CS:GO crosshair share codes.
    /// </summary>
    public class CsgoCrosshairDecoder
    {
        private const string DICTIONARY = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefhijkmnopqrstuvwxyz23456789";
        private static readonly Regex SHARECODE_PATTERN = new Regex(@"^CSGO(-?[\w]{5}){5}$");

        /// <summary>
        /// Decodes a CS:GO share code into crosshair information.
        /// </summary>
        /// <param name="shareCode">The share code to decode (format: CSGO-XXXXX-XXXXX-XXXXX-XXXXX-XXXXX).</param>
        /// <returns>A CrosshairInfo object containing the decoded crosshair settings.</returns>
        /// <exception cref="InvalidSharecodeException">Thrown when the share code format is invalid.</exception>
        public CrosshairInfo DecodeShareCodeToCrosshairInfo(string shareCode)
        {
            ValidateShareCode(shareCode);

            // Remove prefix and dashes
            string code = shareCode.Substring(5).Replace("-", "");

            // Decode bytes
            byte[] bytes = Base58Decode(code);

            // ValidateChecksum(bytes); // Uncommented for now as in original code

            // Create crosshair object from decoded bytes
            return new CrosshairInfo(bytes, shareCode);
        }

        private void ValidateShareCode(string shareCode)
        {
            if (!SHARECODE_PATTERN.IsMatch(shareCode))
            {
                throw new InvalidSharecodeException("Invalid share code format. Must be like: CSGO-XXXXX-XXXXX-XXXXX-XXXXX-XXXXX");
            }
        }

        private byte[] Base58Decode(string code)
        {
            // Convert to big integer
            BigInteger bigInt = ConvertToBigInteger(code);

            // Convert to byte array
            List<byte> bytes = ConvertToByteList(bigInt);

            // Add padding to ensure 19 bytes total
            EnsureByteArraySize(bytes, 19);

            // The bytes should be in reverse order for correct interpretation
            bytes.Reverse();
            return bytes.ToArray();
        }

        private BigInteger ConvertToBigInteger(string code)
        {
            BigInteger bigInt = 0;
            BigInteger baseNum = DICTIONARY.Length;

            foreach (char c in code.Reverse())
            {
                int pos = DICTIONARY.IndexOf(c);
                if (pos == -1)
                {
                    throw new InvalidSharecodeException($"Invalid character '{c}' in share code");
                }
                bigInt = bigInt * baseNum + pos;
            }

            return bigInt;
        }

        private List<byte> ConvertToByteList(BigInteger bigInt)
        {
            var bytes = new List<byte>();
            while (bigInt > 0)
            {
                bytes.Add((byte)(bigInt % 256));
                bigInt /= 256;
            }
            return bytes;
        }

        private void EnsureByteArraySize(List<byte> bytes, int size)
        {
            while (bytes.Count < size)
            {
                bytes.Add(0);
            }
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
    }
}
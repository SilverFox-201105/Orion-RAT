using System;
using System.Diagnostics;

namespace Orion.Common.Cryptography
{
    /// <summary>
    /// Provides byte rotation obfuscation methods for simple data protection.
    /// </summary>
    public static class ByteRotationObfuscator
    {
        /// <summary>
        /// The rotation amount used for obfuscation.
        /// </summary>
        private const int ROTATION_AMOUNT = 16;

        /// <summary>
        /// Obfuscates data by rotating each byte by a fixed amount with overflow wrapping.
        /// </summary>
        /// <param name="data">The data to obfuscate.</param>
        /// <returns>The obfuscated data.</returns>
        public static byte[] Obfuscate(byte[] data)
        {
            if (data == null)
            {
                Debug.WriteLine("Failed to Obfuscate. Data is null.");
                return data;
            }


            byte[] result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = RotateByte(data[i], ROTATION_AMOUNT);
            }
            return result;
        }

        /// <summary>
        /// Deobfuscates data by rotating each byte back by the fixed amount with overflow wrapping.
        /// </summary>
        /// <param name="data">The obfuscated data to deobfuscate.</param>
        /// <returns>The original data.</returns>
        public static byte[] Deobfuscate(byte[] data)
        {
            if (data == null)
            {
                Debug.WriteLine("Failed to Deobfuscate. Data is null.");
                return data;
            }

            byte[] result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = RotateByte(data[i], -ROTATION_AMOUNT);
            }
            return result;
        }

        /// <summary>
        /// Rotates a byte by the specified amount with overflow wrapping.
        /// </summary>
        /// <param name="value">The byte to rotate.</param>
        /// <param name="amount">The rotation amount (can be positive or negative).</param>
        /// <returns>The rotated byte.</returns>
        private static byte RotateByte(byte value, int amount)
        {
            amount = ((amount % 256) + 256) % 256;
            
            int result = (value + amount) % 256;
            return (byte)result;
        }

    }
}

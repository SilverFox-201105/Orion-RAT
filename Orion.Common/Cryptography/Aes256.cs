using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Orion.Common.Cryptography
{
    public class Aes256
    {
        private const int KeyLength = 32;
        private const int NonceLength = 12;
        private const int TagLength = 16;
        private const int Pbkdf2Iterations = 100_000;
        private readonly byte[] _key;

        private static readonly object ModeLogLock = new object();
        private static bool _modeLogged;
        private static readonly ConcurrentDictionary<string, byte[]> KeyCache = new ConcurrentDictionary<string, byte[]>(StringComparer.Ordinal);

        private static readonly byte[] Salt =
        {
            // SHA256("Orion_AES_256_KEY_DERIVE_SALT")
            0x5A, 0x23, 0xF8, 0x39, 0x46, 0x40, 0xCB, 0x9E, 0x40, 0x65, 0x84, 0x46, 0xA0, 0x4C, 0x0B, 0xBA,
            0xE8, 0x2D, 0xC9, 0x3D, 0x04, 0x70, 0xE1, 0xB2, 0xA4, 0x06, 0xA9, 0x0F, 0xD2, 0x52, 0x03, 0x82
        };

        public Aes256(string masterKey)
        {
            if (string.IsNullOrEmpty(masterKey))
                throw new ArgumentException($"{nameof(masterKey)} can not be null or empty.");

            _key = (byte[])KeyCache.GetOrAdd(masterKey, DeriveKey).Clone();
            EnsureModeLogged();
        }

        public string Encrypt(string input)
        {
            return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(input)));
        }

        public byte[] Encrypt(byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException($"{nameof(input)} can not be null.");

            var nonce = CreateRandomBytes(NonceLength);
            var ciphertext = new byte[input.Length];
            var tag = new byte[TagLength];

            EncryptInternal(nonce, input, ciphertext, tag);

            var output = new byte[NonceLength + TagLength + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, output, 0, NonceLength);
            Buffer.BlockCopy(tag, 0, output, NonceLength, TagLength);
            Buffer.BlockCopy(ciphertext, 0, output, NonceLength + TagLength, ciphertext.Length);

            return output;
        }

        public string Decrypt(string input)
        {
            return Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(input)));
        }

        public byte[] Decrypt(byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException($"{nameof(input)} can not be null.");

            if (input.Length < NonceLength + TagLength)
                throw new CryptographicException("Ciphertext is too short to contain nonce and tag.");

            var nonce = new byte[NonceLength];
            var tag = new byte[TagLength];
            var ciphertextLength = input.Length - NonceLength - TagLength;
            var ciphertext = new byte[ciphertextLength];

            Buffer.BlockCopy(input, 0, nonce, 0, NonceLength);
            Buffer.BlockCopy(input, NonceLength, tag, 0, TagLength);
            Buffer.BlockCopy(input, NonceLength + TagLength, ciphertext, 0, ciphertextLength);

            var plaintext = new byte[ciphertextLength];
            DecryptInternal(nonce, ciphertext, tag, plaintext);
            return plaintext;
        }

        private static byte[] CreateRandomBytes(int length)
        {
            var buffer = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }

            return buffer;
        }

    private void EncryptInternal(byte[] nonce, byte[] plaintext, byte[] ciphertext, byte[] tag)
    {
#if NET5_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            using (var aesGcm = new AesGcm(_key))
            {
                aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
            }
#elif NETFRAMEWORK
            AesGcmCng.Encrypt(_key, nonce, plaintext, ciphertext, tag);
#else
#error AES-GCM fallback requires either .NET 5+ or .NET Framework with CNG support.
#endif
        }

        private void DecryptInternal(byte[] nonce, byte[] ciphertext, byte[] tag, byte[] plaintext)
        {
#if NET5_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            using (var aesGcm = new AesGcm(_key))
            {
                aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
            }
#elif NETFRAMEWORK
            AesGcmCng.Decrypt(_key, nonce, ciphertext, tag, plaintext);
#else
#error AES-GCM fallback requires either .NET 5+ or .NET Framework with CNG support.
#endif
        }

        private static byte[] DeriveKey(string masterKey)
        {
            byte[] password = Encoding.UTF8.GetBytes(masterKey);
            return Pbkdf2Sha256.DeriveKey(password, Salt, Pbkdf2Iterations, KeyLength);
        }

        private static class Pbkdf2Sha256
        {
            public static byte[] DeriveKey(byte[] password, byte[] salt, int iterations, int keyLength)
            {
                if (password == null) throw new ArgumentNullException(nameof(password));
                if (salt == null) throw new ArgumentNullException(nameof(salt));
                if (iterations <= 0) throw new ArgumentOutOfRangeException(nameof(iterations));
                if (keyLength <= 0) throw new ArgumentOutOfRangeException(nameof(keyLength));

                using (var hmac = new HMACSHA256(password))
                {
                    int hashSize = hmac.HashSize / 8;
                    int blocks = (keyLength + hashSize - 1) / hashSize;
                    byte[] result = new byte[keyLength];
                    byte[] block = new byte[hashSize];
                    byte[] saltWithIndex = new byte[salt.Length + 4];
                    Buffer.BlockCopy(salt, 0, saltWithIndex, 0, salt.Length);

                    for (int i = 1; i <= blocks; i++)
                    {
                        saltWithIndex[salt.Length] = (byte)((i >> 24) & 0xFF);
                        saltWithIndex[salt.Length + 1] = (byte)((i >> 16) & 0xFF);
                        saltWithIndex[salt.Length + 2] = (byte)((i >> 8) & 0xFF);
                        saltWithIndex[salt.Length + 3] = (byte)(i & 0xFF);

                        byte[] u = hmac.ComputeHash(saltWithIndex);
                        Buffer.BlockCopy(u, 0, block, 0, hashSize);

                        for (int j = 1; j < iterations; j++)
                        {
                            u = hmac.ComputeHash(u);
                            for (int k = 0; k < hashSize; k++)
                                block[k] ^= u[k];
                        }

                        int copyLen = Math.Min(hashSize, keyLength - ((i - 1) * hashSize));
                        Buffer.BlockCopy(block, 0, result, (i - 1) * hashSize, copyLen);
                    }

                    return result;
                }
            }
        }

        private static void EnsureModeLogged()
        {
            if (_modeLogged)
                return;

            lock (ModeLogLock)
            {
                if (_modeLogged)
                    return;

                Debug.WriteLine($"[Orion] Transport E2EE mode: {GetModeDescription()}");
                _modeLogged = true;
            }
        }

        private static string GetModeDescription()
        {
#if NET5_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            return "AES-GCM via System.Security.Cryptography.AesGcm";
#elif NETFRAMEWORK
            return "AES-GCM via Windows CNG (AuthenticatedAes)";
#else
            return "AES-GCM (unknown runtime configuration)";
#endif
        }
    }
}

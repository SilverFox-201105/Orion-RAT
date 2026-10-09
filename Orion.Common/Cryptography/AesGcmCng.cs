#if NETFRAMEWORK
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Orion.Common.Cryptography
{
    internal static class AesGcmCng
    {
        private const uint ERROR_SUCCESS = 0x00000000;
        private const uint STATUS_AUTH_TAG_MISMATCH = 0xC000A002;

        private const string BCRYPT_AES_ALGORITHM = "AES";
        private const string MS_PRIMITIVE_PROVIDER = "Microsoft Primitive Provider";
        private const string BCRYPT_CHAINING_MODE = "ChainingMode";
        private const string BCRYPT_CHAIN_MODE_GCM = "ChainingModeGCM";
        private const string BCRYPT_OBJECT_LENGTH = "ObjectLength";
        private const string BCRYPT_KEY_DATA_BLOB = "KeyDataBlob";

        private static readonly byte[] KeyBlobMagic = BitConverter.GetBytes(0x4d42444b); // "KDBM"

        internal static void Encrypt(byte[] key, byte[] nonce, byte[] plaintext, byte[] ciphertext, byte[] tag)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (nonce == null) throw new ArgumentNullException(nameof(nonce));
            if (plaintext == null) throw new ArgumentNullException(nameof(plaintext));
            if (ciphertext == null) throw new ArgumentNullException(nameof(ciphertext));
            if (tag == null) throw new ArgumentNullException(nameof(tag));

            if (ciphertext.Length != plaintext.Length)
            {
                throw new CryptographicException("Ciphertext buffer length must match plaintext length.");
            }

            using (SafeAlgorithmHandle algorithm = OpenAlgorithm())
            using (SafeKeyHandle keyHandle = ImportKey(algorithm, key))
            {
                byte[] output = new byte[ciphertext.Length];
                byte[] tagBuffer = new byte[tag.Length];
                AuthInfo authInfo = new AuthInfo(nonce, null, tagBuffer);

                try
                {
                    byte[] ivBuffer = (byte[])nonce.Clone();
                    int result = 0;
                    uint status = BCryptEncrypt(keyHandle.DangerousGetHandle(), plaintext, plaintext.Length, ref authInfo.Info, ivBuffer, ivBuffer.Length, output, output.Length, ref result, 0);

                    if (status != ERROR_SUCCESS)
                    {
                        throw new CryptographicException(string.Format("BCryptEncrypt failed with status code 0x{0:X8}.", status));
                    }

                    if (result != ciphertext.Length)
                    {
                        throw new CryptographicException("Ciphertext length mismatch during encryption.");
                    }

                    Buffer.BlockCopy(output, 0, ciphertext, 0, result);
                    authInfo.CopyTag(tag);
                }
                finally
                {
                    authInfo.Dispose();
                }
            }
        }

        internal static void Decrypt(byte[] key, byte[] nonce, byte[] ciphertext, byte[] tag, byte[] plaintext)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (nonce == null) throw new ArgumentNullException(nameof(nonce));
            if (ciphertext == null) throw new ArgumentNullException(nameof(ciphertext));
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            if (plaintext == null) throw new ArgumentNullException(nameof(plaintext));

            if (ciphertext.Length != plaintext.Length)
            {
                throw new CryptographicException("Plaintext buffer length must match ciphertext length.");
            }

            using (SafeAlgorithmHandle algorithm = OpenAlgorithm())
            using (SafeKeyHandle keyHandle = ImportKey(algorithm, key))
            {
                byte[] output = new byte[plaintext.Length];
                AuthInfo authInfo = new AuthInfo(nonce, null, tag);

                try
                {
                    byte[] ivBuffer = (byte[])nonce.Clone();
                    int result = 0;
                    uint status = BCryptDecrypt(keyHandle.DangerousGetHandle(), ciphertext, ciphertext.Length, ref authInfo.Info, ivBuffer, ivBuffer.Length, output, output.Length, ref result, 0);

                    if (status == STATUS_AUTH_TAG_MISMATCH)
                    {
                        throw new CryptographicException("Authentication tag mismatch during decryption.");
                    }

                    if (status != ERROR_SUCCESS)
                    {
                        throw new CryptographicException(string.Format("BCryptDecrypt failed with status code 0x{0:X8}.", status));
                    }

                    if (result != plaintext.Length)
                    {
                        throw new CryptographicException("Plaintext length mismatch during decryption.");
                    }

                    Buffer.BlockCopy(output, 0, plaintext, 0, result);
                }
                finally
                {
                    authInfo.Dispose();
                }
            }
        }

        private static SafeAlgorithmHandle OpenAlgorithm()
        {
            IntPtr rawHandle;
            uint status = BCryptOpenAlgorithmProvider(out rawHandle, BCRYPT_AES_ALGORITHM, MS_PRIMITIVE_PROVIDER, 0);
            if (status != ERROR_SUCCESS)
            {
                throw new CryptographicException(string.Format("BCryptOpenAlgorithmProvider failed with status code 0x{0:X8}.", status));
            }

            SafeAlgorithmHandle handle = new SafeAlgorithmHandle(rawHandle);
            try
            {
                byte[] chainMode = Encoding.Unicode.GetBytes(BCRYPT_CHAIN_MODE_GCM);
                status = BCryptSetAlgorithmProperty(handle.DangerousGetHandle(), BCRYPT_CHAINING_MODE, chainMode, chainMode.Length, 0);
                if (status != ERROR_SUCCESS)
                {
                    throw new CryptographicException(string.Format("BCryptSetAlgorithmProperty failed with status code 0x{0:X8}.", status));
                }

                return handle;
            }
            catch
            {
                handle.Dispose();
                throw;
            }
        }

        private static SafeKeyHandle ImportKey(SafeAlgorithmHandle algorithm, byte[] key)
        {
            byte[] objectLength = GetAlgorithmProperty(algorithm.DangerousGetHandle(), BCRYPT_OBJECT_LENGTH);
            int keyObjectSize = BitConverter.ToInt32(objectLength, 0);
            IntPtr keyObject = Marshal.AllocHGlobal(keyObjectSize);

            try
            {
                byte[] blob = BuildKeyBlob(key);
                IntPtr rawKey;
                uint status = BCryptImportKey(algorithm.DangerousGetHandle(), IntPtr.Zero, BCRYPT_KEY_DATA_BLOB, out rawKey, keyObject, keyObjectSize, blob, blob.Length, 0);
                if (status != ERROR_SUCCESS)
                {
                    throw new CryptographicException(string.Format("BCryptImportKey failed with status code 0x{0:X8}.", status));
                }

                return new SafeKeyHandle(rawKey, keyObject);
            }
            catch
            {
                Marshal.FreeHGlobal(keyObject);
                throw;
            }
        }

        private static byte[] GetAlgorithmProperty(IntPtr handle, string property)
        {
            int size = 0;
            uint status = BCryptGetProperty(handle, property, null, 0, ref size, 0);
            if (status != ERROR_SUCCESS)
            {
                throw new CryptographicException(string.Format("BCryptGetProperty (query size) failed with status code 0x{0:X8}.", status));
            }

            byte[] buffer = new byte[size];
            status = BCryptGetProperty(handle, property, buffer, buffer.Length, ref size, 0);
            if (status != ERROR_SUCCESS)
            {
                throw new CryptographicException(string.Format("BCryptGetProperty failed with status code 0x{0:X8}.", status));
            }

            return buffer;
        }

        private static byte[] BuildKeyBlob(byte[] key)
        {
            byte[] blob = new byte[KeyBlobMagic.Length + sizeof(int) + sizeof(int) + key.Length];
            Buffer.BlockCopy(KeyBlobMagic, 0, blob, 0, KeyBlobMagic.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(1), 0, blob, KeyBlobMagic.Length, sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(key.Length), 0, blob, KeyBlobMagic.Length + sizeof(int), sizeof(int));
            Buffer.BlockCopy(key, 0, blob, KeyBlobMagic.Length + (sizeof(int) * 2), key.Length);
            return blob;
        }

        private sealed class AuthInfo : IDisposable
        {
            internal BCryptAuthenticatedCipherModeInfo Info;
            private GCHandle _nonceHandle;
            private GCHandle _aadHandle;
            private GCHandle _tagHandle;
            private GCHandle _macHandle;
            private byte[] _tagBuffer;

            internal AuthInfo(byte[] nonce, byte[] aad, byte[] tag)
            {
                Info = new BCryptAuthenticatedCipherModeInfo();
                Info.cbSize = Marshal.SizeOf(typeof(BCryptAuthenticatedCipherModeInfo));
                Info.dwInfoVersion = 1;

                if (nonce != null && nonce.Length > 0)
                {
                    byte[] nonceCopy = (byte[])nonce.Clone();
                    _nonceHandle = GCHandle.Alloc(nonceCopy, GCHandleType.Pinned);
                    Info.pbNonce = _nonceHandle.AddrOfPinnedObject();
                    Info.cbNonce = nonceCopy.Length;
                }

                if (aad != null && aad.Length > 0)
                {
                    byte[] aadCopy = (byte[])aad.Clone();
                    _aadHandle = GCHandle.Alloc(aadCopy, GCHandleType.Pinned);
                    Info.pbAuthData = _aadHandle.AddrOfPinnedObject();
                    Info.cbAuthData = aadCopy.Length;
                    Info.cbAAD = aadCopy.Length;
                }

                if (tag != null && tag.Length > 0)
                {
                    _tagBuffer = (byte[])tag.Clone();
                    _tagHandle = GCHandle.Alloc(_tagBuffer, GCHandleType.Pinned);
                    Info.pbTag = _tagHandle.AddrOfPinnedObject();
                    Info.cbTag = _tagBuffer.Length;

                    byte[] mac = new byte[_tagBuffer.Length];
                    _macHandle = GCHandle.Alloc(mac, GCHandleType.Pinned);
                    Info.pbMacContext = _macHandle.AddrOfPinnedObject();
                    Info.cbMacContext = mac.Length;
                }
            }

            internal void CopyTag(byte[] destination)
            {
                if (_tagBuffer != null && destination != null)
                {
                    Buffer.BlockCopy(_tagBuffer, 0, destination, 0, Math.Min(_tagBuffer.Length, destination.Length));
                }
            }

            public void Dispose()
            {
                if (_macHandle.IsAllocated) _macHandle.Free();
                if (_tagHandle.IsAllocated) _tagHandle.Free();
                if (_aadHandle.IsAllocated) _aadHandle.Free();
                if (_nonceHandle.IsAllocated) _nonceHandle.Free();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BCryptAuthenticatedCipherModeInfo
        {
            internal int cbSize;
            internal int dwInfoVersion;
            internal IntPtr pbNonce;
            internal int cbNonce;
            internal IntPtr pbAuthData;
            internal int cbAuthData;
            internal IntPtr pbTag;
            internal int cbTag;
            internal IntPtr pbMacContext;
            internal int cbMacContext;
            internal int cbAAD;
            internal long cbData;
            internal int dwFlags;
        }

        private sealed class SafeAlgorithmHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            internal SafeAlgorithmHandle(IntPtr handle) : base(true)
            {
                SetHandle(handle);
            }

            protected override bool ReleaseHandle()
            {
                return BCryptCloseAlgorithmProvider(handle, 0) == ERROR_SUCCESS;
            }
        }

        private sealed class SafeKeyHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private readonly IntPtr _keyObject;

            internal SafeKeyHandle(IntPtr handle, IntPtr keyObject) : base(true)
            {
                SetHandle(handle);
                _keyObject = keyObject;
            }

            protected override bool ReleaseHandle()
            {
                if (_keyObject != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_keyObject);
                }

                return BCryptDestroyKey(handle) == ERROR_SUCCESS;
            }
        }

        [DllImport("bcrypt.dll")]
        private static extern uint BCryptOpenAlgorithmProvider(out IntPtr phAlgorithm, [MarshalAs(UnmanagedType.LPWStr)] string pszAlgId, [MarshalAs(UnmanagedType.LPWStr)] string pszImplementation, uint dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern uint BCryptCloseAlgorithmProvider(IntPtr hAlgorithm, uint flags);

        [DllImport("bcrypt.dll", EntryPoint = "BCryptGetProperty")]
        private static extern uint BCryptGetProperty(IntPtr hObject, [MarshalAs(UnmanagedType.LPWStr)] string pszProperty, byte[] pbOutput, int cbOutput, ref int pcbResult, uint flags);

        [DllImport("bcrypt.dll", EntryPoint = "BCryptSetProperty")]
        private static extern uint BCryptSetAlgorithmProperty(IntPtr hObject, [MarshalAs(UnmanagedType.LPWStr)] string pszProperty, byte[] pbInput, int cbInput, int dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern uint BCryptImportKey(IntPtr hAlgorithm, IntPtr hImportKey, [MarshalAs(UnmanagedType.LPWStr)] string pszBlobType, out IntPtr phKey, IntPtr pbKeyObject, int cbKeyObject, byte[] pbInput, int cbInput, uint dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern uint BCryptDestroyKey(IntPtr hKey);

        [DllImport("bcrypt.dll")]
        private static extern uint BCryptEncrypt(IntPtr hKey, byte[] pbInput, int cbInput, ref BCryptAuthenticatedCipherModeInfo pPaddingInfo, byte[] pbIV, int cbIV, byte[] pbOutput, int cbOutput, ref int pcbResult, uint dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern uint BCryptDecrypt(IntPtr hKey, byte[] pbInput, int cbInput, ref BCryptAuthenticatedCipherModeInfo pPaddingInfo, byte[] pbIV, int cbIV, byte[] pbOutput, int cbOutput, ref int pcbResult, uint dwFlags);
    }
}
#endif

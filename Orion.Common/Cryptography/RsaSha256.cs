using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Orion.Common.Cryptography
{
    public static class RsaSha256
    {
        private const int PROV_RSA_AES = 24;

        private static RSACryptoServiceProvider GetRsaCsp(RSA rsa)
        {
            if (rsa == null)
                throw new ArgumentNullException(nameof(rsa));

            RSAParameters rsaParams;
            bool hasPrivateKey;

            try
            {
                rsaParams = rsa.ExportParameters(true);
                hasPrivateKey = true;
            }
            catch (CryptographicException)
            {
                rsaParams = rsa.ExportParameters(false);
                hasPrivateKey = false;
            }

            CspParameters cspParams = new CspParameters
            {
                ProviderType = PROV_RSA_AES,
                KeyNumber = hasPrivateKey ? (int)KeyNumber.Exchange : (int)KeyNumber.Signature
            };

            RSACryptoServiceProvider csp = new RSACryptoServiceProvider(cspParams);
            try
            {
                csp.ImportParameters(rsaParams);
                csp.PersistKeyInCsp = false;
                return csp;
            }
            catch
            {
                csp.Clear();
                throw;
            }
        }

        public static byte[] SignHash(RSA rsaPrivateKey, byte[] hash)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));

            using (var csp = GetRsaCsp(rsaPrivateKey))
            {
                return csp.SignHash(hash, CryptoConfig.MapNameToOID("SHA256"));
            }
        }

        public static bool VerifyHash(RSA rsaPublicKey, byte[] hash, byte[] signature)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));

            using (var csp = GetRsaCsp(rsaPublicKey))
            {
                return csp.VerifyHash(hash, CryptoConfig.MapNameToOID("SHA256"), signature);
            }
        }

        public static byte[] SignHash(X509Certificate2 certificate, byte[] hash)
        {
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));
            using (var rsa = certificate.GetRSAPrivateKey())
            {
                return SignHash(rsa, hash);
            }
        }

        public static bool VerifyHash(X509Certificate2 certificate, byte[] hash, byte[] signature)
        {
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));
            using (var rsa = certificate.GetRSAPublicKey())
            {
                return VerifyHash(rsa, hash, signature);
            }
        }
    }
}

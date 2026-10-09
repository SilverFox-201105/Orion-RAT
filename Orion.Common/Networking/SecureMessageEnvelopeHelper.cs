using Orion.Common.Cryptography;
using Orion.Common.Messages.Other;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Orion.Common.Networking
{
    public static class SecureMessageEnvelopeHelper
    {
        public static bool CanUse(X509Certificate2 certificate)
        {
            return certificate != null && certificate.RawData != null && certificate.RawData.Length > 0;
        }

        public static SecureMessageEnvelope Wrap(IMessage message, X509Certificate2 certificate)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message is SecureMessageEnvelope)
                throw new InvalidOperationException("Message is already wrapped in a secure envelope.");

            if (!CanUse(certificate))
                throw new ArgumentException("A valid certificate is required to wrap the message.", nameof(certificate));

            var serialized = OrionMessagePackSerializer.Serialize(message);
            var keyMaterial = DeriveKeyMaterial(certificate);

            var cipher = new Aes256(keyMaterial);
            var payload = cipher.Encrypt(serialized);

            return new SecureMessageEnvelope
            {
                Payload = payload
            };
        }

        public static IMessage Unwrap(SecureMessageEnvelope envelope, X509Certificate2 certificate)
        {
            if (envelope == null)
                throw new ArgumentNullException(nameof(envelope));

            if (envelope.Payload == null || envelope.Payload.Length == 0)
                throw new ArgumentException("Envelope payload is empty.", nameof(envelope));

            if (!CanUse(certificate))
                throw new ArgumentException("A valid certificate is required to unwrap the message.", nameof(certificate));

            var keyMaterial = DeriveKeyMaterial(certificate);

            var cipher = new Aes256(keyMaterial);
            var decrypted = cipher.Decrypt(envelope.Payload);
            try
            {
                return OrionMessagePackSerializer.Deserialize(decrypted);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to deserialize secure payload (length={decrypted?.Length ?? 0}).", ex);
            }
        }

        private static string DeriveKeyMaterial(X509Certificate2 certificate)
        {
            var thumbprint = certificate.Thumbprint?.Replace(" ", string.Empty)?.ToUpperInvariant();
            if (string.IsNullOrEmpty(thumbprint))
                throw new InvalidOperationException("Certificate thumbprint is unavailable for secure envelope key derivation.");

            var hash = Sha256.ComputeHash(Encoding.UTF8.GetBytes(thumbprint));
            return Convert.ToBase64String(hash);
        }

    }
}

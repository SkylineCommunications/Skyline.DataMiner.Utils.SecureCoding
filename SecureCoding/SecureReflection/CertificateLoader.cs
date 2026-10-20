namespace Skyline.DataMiner.Utils.SecureCoding.SecureReflection
{
    using System.Security.Cryptography.X509Certificates;

    // Keep the .NET Standard implementation available to full Framework consumers.
    // Modern loaders accept single X.509 certificates, not PKCS12/PKCS7 containers.
    internal static class CertificateLoader
    {
        internal static X509Certificate2 LoadCertificateFromFile(string path)
        {
#if NET9_0_OR_GREATER
            return X509CertificateLoader.LoadCertificateFromFile(path);
#else
            return new X509Certificate2(path);
#endif
        }

        internal static X509Certificate2 LoadCertificate(byte[] data)
        {
#if NET9_0_OR_GREATER
            return X509CertificateLoader.LoadCertificate(data);
#else
            return new X509Certificate2(data);
#endif
        }
    }
}

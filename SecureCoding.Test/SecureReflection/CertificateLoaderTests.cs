namespace SecureCoding.Test.SecureReflection
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Skyline.DataMiner.Utils.SecureCoding.SecureReflection;

    [TestClass]
    public class CertificateLoaderTests
    {
        private static readonly string Items = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "SecureReflection", "TestItems"));

        [TestMethod]
        [DataRow("Signed.cer")]
        [DataRow("SelfSigned.cer")]
        public void FileAndBytesPreserveCertificate(string fileName)
        {
            var path = Path.Combine(Items, fileName);
            var data = File.ReadAllBytes(path);
            var text = System.Text.Encoding.ASCII.GetString(data);
            var expectedDer = text.StartsWith("-----BEGIN CERTIFICATE-----", StringComparison.Ordinal)
                ? Convert.FromBase64String(text.Replace("-----BEGIN CERTIFICATE-----", string.Empty).Replace("-----END CERTIFICATE-----", string.Empty))
                : data;
            using (var fromFile = CertificateLoader.LoadCertificateFromFile(path))
            using (var fromBytes = CertificateLoader.LoadCertificate(data))
            {
                CollectionAssert.AreEqual(expectedDer, fromFile.RawData);
                CollectionAssert.AreEqual(expectedDer, fromBytes.RawData);
                Assert.IsFalse(fromFile.HasPrivateKey);
                Assert.IsFalse(fromBytes.HasPrivateKey);
            }
        }

        [TestMethod]
        public void InvalidCertificateDataIsRejected()
        {
            Assert.Throws<CryptographicException>(() => CertificateLoader.LoadCertificate(new byte[] { 1, 2, 3 }));
        }

#if NET9_0_OR_GREATER
        [TestMethod]
        public void PemCertificateMatchesDerCertificate()
        {
            var data = File.ReadAllBytes(Path.Combine(Items, "SelfSigned.cer"));
            var pem = "-----BEGIN CERTIFICATE-----\n" + Convert.ToBase64String(data) + "\n-----END CERTIFICATE-----";
            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(path, pem);
                using (var fromFile = CertificateLoader.LoadCertificateFromFile(path))
                using (var fromBytes = CertificateLoader.LoadCertificate(System.Text.Encoding.ASCII.GetBytes(pem)))
                {
                    CollectionAssert.AreEqual(data, fromFile.RawData);
                    CollectionAssert.AreEqual(data, fromBytes.RawData);
                }
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void Pkcs12ContainerRequiresExplicitLoading()
        {
            using (var certificate = X509CertificateLoader.LoadPkcs12FromFile(Path.Combine(Items, "SelfSigned.pfx"), "SecureCodingNugetTest", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet))
            {
                // An unencrypted container must not be silently imported as a public certificate.
                var container = certificate.Export(X509ContentType.Pkcs12, string.Empty);
                var path = Path.GetTempFileName();
                try
                {
                    File.WriteAllBytes(path, container);
                    Assert.Throws<CryptographicException>(() => CertificateLoader.LoadCertificate(container));
                    Assert.Throws<CryptographicException>(() => CertificateLoader.LoadCertificateFromFile(path));
                    Assert.IsNotNull(SecureAssembly.LoadFrom(Path.Combine(Items, "SelfSigned.dll"), certificate, false));
                }
                finally { File.Delete(path); }
            }
        }
#endif
    }
}

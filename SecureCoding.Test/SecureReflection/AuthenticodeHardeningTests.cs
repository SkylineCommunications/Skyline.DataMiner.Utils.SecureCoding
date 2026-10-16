namespace SecureCoding.Test.SecureReflection
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Cryptography;
    using System.Security.Cryptography.Pkcs;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Skyline.DataMiner.Utils.SecureCoding.SecureReflection;

    [TestClass]
    public class AuthenticodeHardeningTests
    {
        private static readonly string Items = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "SecureReflection", "TestItems"));

        [TestMethod]
        [DataRow("optional-header")]
        [DataRow("directory-count")]
        [DataRow("overlapping-section")]
        [DataRow("certificate-in-section")]
        [DataRow("certificate-not-at-end")]
        [DataRow("unsigned-padding")]
        [DataRow("tampered-section")]
        public void MalformedImageIsRejected(string mutation)
        {
            var image = File.ReadAllBytes(Path.Combine(Items, "Signed.dll"));
            var pe = BitConverter.ToInt32(image, 60);
            var optional = pe + 24;
            var directory = optional + 128;
            var table = BitConverter.ToInt32(image, directory);
            var sections = optional + BitConverter.ToUInt16(image, pe + 20);
            switch (mutation)
            {
                case "optional-header": Write16(image, pe + 20, 2); break;
                case "directory-count": Write32(image, optional + 92, int.MaxValue); break;
                case "overlapping-section": Write32(image, sections + 40 + 20, BitConverter.ToInt32(image, sections + 20)); break;
                case "certificate-in-section": Write32(image, directory, 512); break;
                case "certificate-not-at-end": Array.Resize(ref image, image.Length + 8); break;
                case "unsigned-padding":
                    Array.Resize(ref image, image.Length + 8);
                    Write32(image, directory + 4, image.Length - table);
                    Write32(image, table, image.Length - table);
                    image[image.Length - 1] = 1;
                    break;
                case "tampered-section": image[1024] ^= 1; break;
            }

            using (var certificate = CertificateLoader.LoadCertificateFromFile(Path.Combine(Items, "Signed.cer")))
            {
                Assert.Throws<SecurityException>(() => AuthenticodeVerifier.ValidateImage(image, false, new[] { certificate }));
            }
        }

        [TestMethod]
        public void BoundedReaderRejectsOversizedFiles()
        {
            var path = Path.GetTempFileName();
            try
            {
                using (var stream = File.OpenWrite(path)) stream.SetLength(256L * 1024 * 1024 + 1);
                Assert.Throws<SecurityException>(() => AuthenticodeVerifier.ReadImage(path));
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void SnapshotDoesNotFollowFileReplacement()
        {
            var path = Path.GetTempFileName();
            try
            {
                File.Copy(Path.Combine(Items, "Signed.dll"), path, true);
                var snapshot = AuthenticodeVerifier.ReadImage(path);
                File.Copy(Path.Combine(Items, "Tampered.dll"), path, true);
                using (var certificate = CertificateLoader.LoadCertificateFromFile(Path.Combine(Items, "Signed.cer")))
                {
                    AuthenticodeVerifier.ValidateImage(snapshot, false, new[] { certificate });
                    Assert.Throws<SecurityException>(() => AuthenticodeVerifier.ValidateImage(AuthenticodeVerifier.ReadImage(path), false, new[] { certificate }));
                }
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void TruncatedAndRandomInputsFailClosed()
        {
            var random = new Random(12345);
            using (var certificate = CertificateLoader.LoadCertificateFromFile(Path.Combine(Items, "Signed.cer")))
            {
                for (var length = 0; length < 512; length++)
                {
                    var image = new byte[length];
                    random.NextBytes(image);
                    Assert.Throws<SecurityException>(() => AuthenticodeVerifier.ValidateImage(image, false, new[] { certificate }));
                }
            }
        }

        [TestMethod]
        [OSCondition(OperatingSystems.Linux)]
        public void LinuxLoadsVerifiedBytesWithoutPathLocation()
        {
            using (var certificate = CertificateLoader.LoadCertificateFromFile(Path.Combine(Items, "SelfSigned.cer")))
            {
                var assembly = SecureAssembly.LoadFrom(Path.Combine(Items, "SelfSigned.dll"), certificate, false);
                Assert.AreEqual(string.Empty, assembly.Location);
                Assert.AreEqual("SLThemeNewtecIntelsat", assembly.GetName().Name);
                Assert.AreEqual(string.Empty, SecureAssembly.LoadFile(Path.Combine(Items, "SelfSigned.dll"), certificate, false).Location);
            }
        }

#if NET8_0_OR_GREATER
        [TestMethod]
        [DataRow("valid", true)]
        [DataRow("truncated", false)]
        [DataRow("trailing", false)]
        [DataRow("wrong-signer", false)]
        public void TimestampMustBeBoundToTheVerifiedSignature(string mutation, bool expected)
        {
            var cms = ReadCms("Signed.dll");
            byte[] encoded = null;
            foreach (CryptographicAttributeObject attribute in cms.SignerInfos[0].UnsignedAttributes)
            {
                if (attribute.Oid.Value == "1.3.6.1.4.1.311.3.3.1") encoded = attribute.Values[0].RawData;
            }

            Assert.IsNotNull(encoded);
            var signer = cms.SignerInfos[0];
            if (mutation == "truncated") Array.Resize(ref encoded, encoded.Length - 1);
            if (mutation == "trailing") Array.Resize(ref encoded, encoded.Length + 1);
            if (mutation == "wrong-signer") signer = ReadCms("SelfSigned.dll").SignerInfos[0];
            var actual = AuthenticodeVerifier.TryValidateTimestampToken(encoded, signer, out _, out var tsa);
            using (tsa) Assert.AreEqual(expected, actual);
        }

        private static SignedCms ReadCms(string file)
        {
            var image = File.ReadAllBytes(Path.Combine(Items, file));
            var directory = BitConverter.ToInt32(image, 60) + 24 + 128;
            var table = BitConverter.ToInt32(image, directory);
            var encoded = new byte[BitConverter.ToInt32(image, table) - 8];
            Buffer.BlockCopy(image, table + 8, encoded, 0, encoded.Length);
            var cms = new SignedCms();
            cms.Decode(encoded);
            return cms;
        }

        [TestMethod]
        [DataRow("SHA1", 2048, true, false)]
        [DataRow("SHA256", 1024, true, false)]
        [DataRow("SHA256", 2048, false, false)]
        [DataRow("SHA256", 2048, true, true)]
        public void SignaturePolicyIsEnforced(string digest, int keySize, bool codeSigning, bool accepted)
        {
            using (var key = RSA.Create(keySize))
            {
                var request = new CertificateRequest("CN=Ephemeral Authenticode Test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
                request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid(codeSigning ? "1.3.6.1.5.5.7.3.3" : "1.3.6.1.5.5.7.3.1") }, true));
                using (var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1)))
                {
                    var image = ResignFixture(certificate, digest);
                    if (accepted)
                    {
                        AuthenticodeVerifier.ValidateImage(image, false, new[] { certificate });
                        Assert.Throws<SecurityException>(() => AuthenticodeVerifier.ValidateImage(image, true, new[] { certificate }));
                    }
                    else
                    {
                        Assert.Throws<SecurityException>(() => AuthenticodeVerifier.ValidateImage(image, false, new[] { certificate }));
                    }
                }
            }
        }

        private static byte[] ResignFixture(X509Certificate2 certificate, string digest)
        {
            var original = File.ReadAllBytes(Path.Combine(Items, "Signed.dll"));
            var directory = BitConverter.ToInt32(original, 60) + 24 + 128;
            var table = BitConverter.ToInt32(original, directory);
            var oldCms = new SignedCms();
            var signature = new byte[BitConverter.ToInt32(original, table) - 8];
            Buffer.BlockCopy(original, table + 8, signature, 0, signature.Length);
            oldCms.Decode(signature);
            var cms = new SignedCms(new ContentInfo(oldCms.ContentInfo.ContentType, oldCms.ContentInfo.Content));
            cms.ComputeSignature(new CmsSigner(certificate) { DigestAlgorithm = new Oid(digest == "SHA1" ? "1.3.14.3.2.26" : "2.16.840.1.101.3.4.2.1") });
            var encoded = cms.Encode();
            var size = (encoded.Length + 8 + 7) / 8 * 8;
            var result = new byte[table + size];
            Buffer.BlockCopy(original, 0, result, 0, table);
            Write32(result, directory + 4, size);
            Write32(result, table, size);
            Write16(result, table + 4, 0x200);
            Write16(result, table + 6, 2);
            Buffer.BlockCopy(encoded, 0, result, table + 8, encoded.Length);
            return result;
        }
#endif

        private static void Write16(byte[] image, int offset, ushort value) => Buffer.BlockCopy(BitConverter.GetBytes(value), 0, image, offset, 2);
        private static void Write32(byte[] image, int offset, int value) => Buffer.BlockCopy(BitConverter.GetBytes(value), 0, image, offset, 4);
    }
}

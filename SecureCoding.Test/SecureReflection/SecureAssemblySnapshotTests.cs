namespace SecureCoding.Test.SecureReflection
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Cryptography.Pkcs;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Skyline.DataMiner.Utils.SecureCoding.SecureReflection;

    [TestClass]
    public class SecureAssemblySnapshotTests
    {
        private static readonly string Items = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "SecureReflection", "TestItems"));

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ValidatedSnapshotSurvivesSourceReplacementOrDeletion(bool deleteSource)
        {
            var path = Path.GetTempFileName();
            try
            {
                File.Copy(Path.Combine(Items, "SelfSigned.dll"), path, true);
                using (var certificate = CertificateLoader.LoadCertificateFromFile(Path.Combine(Items, "SelfSigned.cer")))
                {
                    var platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? OSPlatform.Windows : OSPlatform.Linux;
                    var snapshot = SecureAssembly.ReadValidatedAssembly(path, false, platform, certificate);
                    File.Delete(path);
                    if (!deleteSource)
                    {
                        File.Copy(Path.Combine(Items, "Unsigned.dll"), path);
                        Assert.Throws<SecurityException>(() => SecureAssembly.ReadValidatedAssembly(path, false, platform, certificate));
                    }

                    // Exercise the exact bytes returned at the production validation-to-load boundary.
                    var assembly = Assembly.Load(snapshot);
                    Assert.AreEqual("SLThemeNewtecIntelsat", assembly.GetName().Name);
                    Assert.AreEqual(string.Empty, assembly.Location);
                }
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void PublicLoadersUseSnapshotAndReleaseSource(bool loadFile)
        {
            var path = Path.GetTempFileName();
            try
            {
                File.Copy(Path.Combine(Items, "SelfSigned.dll"), path, true);
                using (var certificate = CertificateLoader.LoadCertificateFromFile(Path.Combine(Items, "SelfSigned.cer")))
                {
                    var assembly = loadFile ? SecureAssembly.LoadFile(path, certificate, false) : SecureAssembly.LoadFrom(path, certificate, false);
                    Assert.AreEqual(string.Empty, assembly.Location);
                    Assert.AreEqual("SLThemeNewtecIntelsat", assembly.GetName().Name);
                    // A path loader would retain a source mapping on Windows; snapshot loading must not.
                    using (var writer = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None))
                    {
                        writer.SetLength(0);
                    }
                }
                File.Delete(path);
                Assert.IsFalse(File.Exists(path));
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        [OSCondition(OperatingSystems.Windows)]
        [DataRow(false)]
        [DataRow(true)]
        public void WindowsNativeVerificationUsesHandleNotReplacedPath(bool validHandle)
        {
            var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "original.dll");
            var movedPath = Path.Combine(directory, "moved.dll");
            try
            {
                File.Copy(Path.Combine(Items, validHandle ? "SelfSigned.dll" : "Unsigned.dll"), path);
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
                using (var certificate = CertificateLoader.LoadCertificateFromFile(Path.Combine(Items, "SelfSigned.cer")))
                {
                    // Deliberately weaker sharing than production, to deterministically redirect the pathname.
                    File.Move(path, movedPath);
                    File.Copy(Path.Combine(Items, validHandle ? "Unsigned.dll" : "SelfSigned.dll"), path);
                    if (validHandle)
                    {
                        SecureAssembly.ValidateWindowsSnapshot(file, false, new[] { certificate });
                    }
                    else
                    {
                        Assert.Throws<SecurityException>(() => SecureAssembly.ValidateWindowsSnapshot(file, false, new[] { certificate }));
                    }
                }
            }
            finally { Directory.Delete(directory, true); }
        }

        [TestMethod]
        [OSCondition(OperatingSystems.Windows)]
        [DataRow(false)]
        [DataRow(true)]
        public void WindowsSnapshotDeniesMutationAndIsDeletedAfterVerification(bool wrongSigner)
        {
            string path;
            using (var certificate = CertificateLoader.LoadCertificateFromFile(Path.Combine(Items, wrongSigner ? "Signed.cer" : "SelfSigned.cer")))
            using (var snapshot = SecureAssembly.CreateWindowsSnapshot(File.ReadAllBytes(Path.Combine(Items, "SelfSigned.dll"))))
            {
                path = snapshot.Name;
                Assert.IsTrue(File.Exists(path));
                Assert.Throws<IOException>(() =>
                {
                    using (var writer = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete)) { }
                });
                Assert.Throws<IOException>(() => File.Delete(path));
                Assert.Throws<IOException>(() => File.Move(path, path + ".moved"));
                if (wrongSigner)
                {
                    var exception = Assert.Throws<SecurityException>(() => SecureAssembly.ValidateWindowsSnapshot(snapshot, false, new[] { certificate }));
                    Assert.AreEqual("The certificate of the assembly does not match the provided certificate.", exception.Message);
                }
                else
                {
                    SecureAssembly.ValidateWindowsSnapshot(snapshot, false, new[] { certificate });
                }
            }
            Assert.IsFalse(File.Exists(path));
        }

        [TestMethod]
        [OSCondition(OperatingSystems.Windows)]
        [DataRow("Unsigned.dll", false)]
        [DataRow("Unsigned.dll", true)]
        [DataRow("Tampered.dll", false)]
        [DataRow("Tampered.dll", true)]
        [DataRow("SelfSigned.dll", true)]
        public void WindowsSnapshotFailsClosedAndReleasesInvalidSignature(string image, bool verifyCertificateChain)
        {
            string path;
            using (var certificate = CertificateLoader.LoadCertificateFromFile(Path.Combine(Items, image == "SelfSigned.dll" ? "SelfSigned.cer" : "Signed.cer")))
            using (var snapshot = SecureAssembly.CreateWindowsSnapshot(File.ReadAllBytes(Path.Combine(Items, image))))
            {
                path = snapshot.Name;
                var exception = Assert.Throws<SecurityException>(() => SecureAssembly.ValidateWindowsSnapshot(snapshot, verifyCertificateChain, new[] { certificate }));
                StringAssert.StartsWith(exception.Message, "Attempt to load an assembly with an invalid signature:");
            }
            Assert.IsFalse(File.Exists(path));
        }

        [TestMethod]
        [OSCondition(OperatingSystems.Windows)]
        [DataRow(false)]
        [DataRow(true)]
        public void WindowsPinsTheVerifiedSigner(bool verifyCertificateChain)
        {
            using (var certificate = CertificateLoader.LoadCertificateFromFile(Path.Combine(Items, "SelfSigned.cer")))
            {
                var exception = Assert.Throws<SecurityException>(() => SecureAssembly.ValidateAssemblySignature(Path.Combine(Items, "Signed.dll"), verifyCertificateChain, certificate));
                Assert.AreEqual("The certificate of the assembly does not match the provided certificate.", exception.Message);
            }
        }

        [TestMethod]
        [OSCondition(OperatingSystems.Windows)]
        [DataRow(false, "Validate")]
        [DataRow(true, "Validate")]
        [DataRow(false, "LoadFrom")]
        [DataRow(true, "LoadFrom")]
        [DataRow(false, "LoadFile")]
        [DataRow(true, "LoadFile")]
        public void WindowsRejectsCorruptCmsSignatureEvenWhenPeDigestIsUnchanged(bool verifyCertificateChain, string entryPoint)
        {
            var image = File.ReadAllBytes(Path.Combine(Items, "Signed.dll"));
            var optionalHeader = BitConverter.ToInt32(image, 60) + 24;
            var securityDirectory = optionalHeader + (BitConverter.ToUInt16(image, optionalHeader) == 0x20b ? 144 : 128);
            var table = BitConverter.ToInt32(image, securityDirectory);
            var encoded = new byte[BitConverter.ToInt32(image, table) - 8];
            Buffer.BlockCopy(image, table + 8, encoded, 0, encoded.Length);
            var cms = new SignedCms();
            cms.Decode(encoded);
            var signature = cms.SignerInfos[0].GetSignature();
            var signatureOffset = -1;
            for (var offset = 0; offset <= encoded.Length - signature.Length; offset++)
            {
                var index = 0;
                while (index < signature.Length && encoded[offset + index] == signature[index]) index++;
                if (index != signature.Length) continue;
                Assert.AreEqual(-1, signatureOffset, "The signature must occur exactly once in the fixture.");
                signatureOffset = offset;
            }
            Assert.IsTrue(signatureOffset >= 0);
            // The certificate table is excluded from the PE digest. Only the CMS signature is corrupted.
            image[table + 8 + signatureOffset] ^= 1;
            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(path, image);
                using (var certificate = CertificateLoader.LoadCertificateFromFile(Path.Combine(Items, "Signed.cer")))
                {
                    // Exercise the public boundary, not only the native verification helper.
                    var exception = Assert.Throws<SecurityException>(() =>
                    {
                        switch (entryPoint)
                        {
                            case "Validate": SecureAssembly.ValidateAssemblySignature(path, verifyCertificateChain, certificate); break;
                            case "LoadFrom": SecureAssembly.LoadFrom(path, certificate, verifyCertificateChain); break;
                            case "LoadFile": SecureAssembly.LoadFile(path, certificate, verifyCertificateChain); break;
                            default: throw new NotSupportedException($"Unknown test entry point '{entryPoint}'.");
                        }
                    });
                    StringAssert.StartsWith(exception.Message, "Attempt to load an assembly with an invalid signature:");
                    if (!verifyCertificateChain)
                    {
                        StringAssert.Contains(exception.Message, "CMS verification failed");
                    }
                }
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        [OSCondition(OperatingSystems.Windows)]
        public void WindowsNativeLayoutsMatchCurrentProcessAbi()
        {
            // These assertions exercise the running process ABI only; they do not simulate another architecture.
            var wide = IntPtr.Size == 8;
            var provider = typeof(SecureAssembly).GetNestedType("ProviderDataHeader", BindingFlags.NonPublic);
            var certificate = typeof(SecureAssembly).GetNestedType("ProviderCertificateHeader", BindingFlags.NonPublic);
            var verification = typeof(SecureAssembly).GetNestedType("MessageSignatureVerification", BindingFlags.NonPublic);
            Assert.IsNotNull(provider);
            Assert.IsNotNull(certificate);
            Assert.IsNotNull(verification);
            // In the documented prefix, dwEncoding is at 104 on x64; hMsg follows at 112.
            // The prefix ends after that pointer (120), not after the later native signer fields.
            Assert.AreEqual(wide ? 120 : 64, Marshal.SizeOf(provider));
            Assert.AreEqual(wide ? 112L : 60L, Marshal.OffsetOf(provider, "Message").ToInt64());
            Assert.AreEqual(wide ? 16 : 8, Marshal.SizeOf(certificate));
            Assert.AreEqual(wide ? 8L : 4L, Marshal.OffsetOf(certificate, "Certificate").ToInt64());
            Assert.AreEqual(wide ? 32 : 20, Marshal.SizeOf(verification));
            Assert.AreEqual(wide ? 24L : 16L, Marshal.OffsetOf(verification, "Signer").ToInt64());
        }

        [TestMethod]
        [OSCondition(OperatingSystems.Windows)]
        [DataRow(false)]
        [DataRow(true)]
        public void WindowsConcurrentSnapshotsRemainIndependent(bool wrongSigner)
        {
            var image = File.ReadAllBytes(Path.Combine(Items, "SelfSigned.dll"));
            System.Threading.Tasks.Parallel.For(0, 16, index =>
            {
                string path;
                // Each operation owns its certificate, file and native verification state.
                using (var certificate = CertificateLoader.LoadCertificateFromFile(Path.Combine(Items, wrongSigner ? "Signed.cer" : "SelfSigned.cer")))
                using (var snapshot = SecureAssembly.CreateWindowsSnapshot(image))
                {
                    path = snapshot.Name;
                    if (wrongSigner)
                    {
                        var exception = Assert.Throws<SecurityException>(() => SecureAssembly.ValidateWindowsSnapshot(snapshot, false, new[] { certificate }));
                        Assert.AreEqual("The certificate of the assembly does not match the provided certificate.", exception.Message);
                    }
                    else
                    {
                        SecureAssembly.ValidateWindowsSnapshot(snapshot, false, new[] { certificate });
                    }
                }
                Assert.IsFalse(File.Exists(path), "The snapshot must be deleted after success or rejection.");
            });
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void PublicLoadersRejectOversizedImages(bool loadFile)
        {
            var path = Path.GetTempFileName();
            try
            {
                using (var stream = File.OpenWrite(path)) stream.SetLength(256L * 1024 * 1024 + 1);
                using (var certificate = CertificateLoader.LoadCertificateFromFile(Path.Combine(Items, "SelfSigned.cer")))
                {
                    Assert.Throws<SecurityException>(() =>
                    {
                        if (loadFile) SecureAssembly.LoadFile(path, certificate, false);
                        else SecureAssembly.LoadFrom(path, certificate, false);
                    });
                }
            }
            finally { File.Delete(path); }
        }
    }
}

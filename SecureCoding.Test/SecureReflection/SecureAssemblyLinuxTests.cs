namespace SecureCoding.Test.SecureReflection
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Skyline.DataMiner.Utils.SecureCoding.SecureReflection;

    /// <summary>
    /// Exercises the managed (non-Windows) Authenticode verification path of <see cref="SecureAssembly"/>
    /// by explicitly selecting <see cref="OSPlatform.Linux"/>, so the rules are validated on every OS.
    /// </summary>
    [TestClass]
    public class SecureAssemblyLinuxTests
    {
        private static readonly string TestItemsPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "SecureReflection", "TestItems"));
        private static readonly string SignedTestDllPath = Path.Combine(TestItemsPath, "Signed.dll");
        private static readonly string SelfSignedTestDllPath = Path.Combine(TestItemsPath, "SelfSigned.dll");
        private static readonly string UnsignedTestDllPath = Path.Combine(TestItemsPath, "Unsigned.dll");
        private static readonly string TamperedTestDllPath = Path.Combine(TestItemsPath, "Tampered.dll");
        private static readonly string SignedCertificatePath = Path.Combine(TestItemsPath, "Signed.cer");
        private static readonly string SelfSignedCertificatePath = Path.Combine(TestItemsPath, "SelfSigned.cer");

        [TestMethod]
        public void GetSigningCertificate_Linux_SignedAssembly_ReturnsSigner()
        {
            using (var expected = new X509Certificate2(SignedCertificatePath))
            using (var actual = SecureAssembly.GetSigningCertificate(SignedTestDllPath, OSPlatform.Linux))
            {
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected.Thumbprint, actual.Thumbprint, ignoreCase: true);
            }
        }

        [TestMethod]
        public void GetSigningCertificate_Linux_UnsignedAssembly_ReturnsNull()
        {
            Assert.IsNull(SecureAssembly.GetSigningCertificate(UnsignedTestDllPath, OSPlatform.Linux));
        }

        [TestMethod]
        public void ValidateAssemblySignature_Linux_SignedWithTrustedCertificate_HashOnly_Success()
        {
            using (var certificate = new X509Certificate2(SignedCertificatePath))
            {
                SecureAssembly.ValidateAssemblySignature(SignedTestDllPath, false, OSPlatform.Linux, certificate);
            }
        }

        [TestMethod]
        public void ValidateAssemblySignature_Linux_SelfSigned_HashOnly_Success()
        {
            using (var certificate = new X509Certificate2(SelfSignedCertificatePath))
            {
                SecureAssembly.ValidateAssemblySignature(SelfSignedTestDllPath, false, OSPlatform.Linux, certificate);
            }
        }

        [TestMethod]
        public void ValidateAssemblySignature_Linux_SelfSigned_WithChain_Throws()
        {
            using (var certificate = new X509Certificate2(SelfSignedCertificatePath))
            {
                var exception = Assert.Throws<SecurityException>(() => SecureAssembly.ValidateAssemblySignature(SelfSignedTestDllPath, true, OSPlatform.Linux, certificate));
                StringAssert.Contains(exception.Message, "invalid signature");
            }
        }

        [TestMethod]
        public void ValidateAssemblySignature_Linux_WrongCertificate_Throws()
        {
            using (var certificate = new X509Certificate2(SelfSignedCertificatePath))
            {
                var exception = Assert.Throws<SecurityException>(() => SecureAssembly.ValidateAssemblySignature(SignedTestDllPath, false, OSPlatform.Linux, certificate));
                StringAssert.Contains(exception.Message, "does not match");
            }
        }

        [TestMethod]
        public void ValidateAssemblySignature_Linux_Unsigned_Throws()
        {
            using (var certificate = new X509Certificate2(SignedCertificatePath))
            {
                var exception = Assert.Throws<SecurityException>(() => SecureAssembly.ValidateAssemblySignature(UnsignedTestDllPath, false, OSPlatform.Linux, certificate));
                StringAssert.Contains(exception.Message, "FileNotSigned");
            }
        }

        [TestMethod]
        public void ValidateAssemblySignature_Linux_Tampered_Throws()
        {
            using (var certificate = new X509Certificate2(SignedCertificatePath))
            {
                var exception = Assert.Throws<SecurityException>(() => SecureAssembly.ValidateAssemblySignature(TamperedTestDllPath, false, OSPlatform.Linux, certificate));
                StringAssert.Contains(exception.Message, "invalid signature");
            }
        }

        [TestMethod]
        public void ValidateAssemblySignature_Linux_MissingFile_Throws()
        {
            using (var certificate = new X509Certificate2(SignedCertificatePath))
            {
                Assert.Throws<FileNotFoundException>(() => SecureAssembly.ValidateAssemblySignature(Path.Combine(TestItemsPath, "DoesNotExist.dll"), false, OSPlatform.Linux, certificate));
            }
        }

        [TestMethod]
        public void ValidateAssemblySignature_UnsupportedPlatform_Throws()
        {
            using (var certificate = new X509Certificate2(SignedCertificatePath))
            {
                Assert.Throws<NotSupportedException>(() => SecureAssembly.ValidateAssemblySignature(SignedTestDllPath, false, OSPlatform.OSX, certificate));
            }
        }

        [TestMethod]
        [OSCondition(OperatingSystems.Windows)]
        public void ValidateAssemblySignature_Linux_And_Windows_Agree()
        {
            using (var signed = new X509Certificate2(SignedCertificatePath))
            using (var selfSigned = new X509Certificate2(SelfSignedCertificatePath))
            {
                foreach (var platform in new[] { OSPlatform.Windows, OSPlatform.Linux })
                {
                    SecureAssembly.ValidateAssemblySignature(SignedTestDllPath, false, platform, signed);
                    SecureAssembly.ValidateAssemblySignature(SelfSignedTestDllPath, false, platform, selfSigned);
                    Assert.Throws<SecurityException>(() => SecureAssembly.ValidateAssemblySignature(SelfSignedTestDllPath, true, platform, selfSigned));
                    Assert.Throws<SecurityException>(() => SecureAssembly.ValidateAssemblySignature(TamperedTestDllPath, false, platform, signed));
                    Assert.Throws<SecurityException>(() => SecureAssembly.ValidateAssemblySignature(UnsignedTestDllPath, false, platform, signed));
                    Assert.Throws<SecurityException>(() => SecureAssembly.ValidateAssemblySignature(SignedTestDllPath, false, platform, selfSigned));
                }
            }
        }
    }
}

namespace SecureCoding.Test.SecureReflection
{
    using System;
    using System.Collections.Generic;
    using System.Security;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Skyline.DataMiner.CICD.FileSystem;
    using Skyline.DataMiner.Utils.SecureCoding.SecureReflection;

    [TestClass]
    public class SecureAssemblyLoaderTests
    {
        private static string testItemsPath = FileSystem.Instance.Path.GetFullPath($@"{Environment.CurrentDirectory}..\..\..\..\SecureReflection\TestItems");
        private static string signedTestDllPath = $"{testItemsPath}\\Signed.dll";
        private static string selfSignedTestDllPath = $"{testItemsPath}\\SelfSigned.dll";
        private static string unsignedTestDllPath = $"{testItemsPath}\\Unsigned.dll";
        private static string tamperedTestDllPath = $"{testItemsPath}\\Tampered.dll";

        private static string signedCertificatePath = $"{testItemsPath}\\Signed.cer";
        private static string selfSignedCertificateCerPath = $"{testItemsPath}\\SelfSigned.cer";
        private static string selfSignedCertificatePfxPath = $"{testItemsPath}\\SelfSigned.pfx";
        private static string selfSignedCertificatePassword = "SecureCodingNugetTest";

        [TestMethod]
        public void SecureAssemblyLoadFile_X509Certificate2Argument_Success()
        {
            var signedCertificate = new X509Certificate2(signedCertificatePath);
            Assert.IsTrue(SecureAssembly.Load(signedTestDllPath, signedCertificate) != default);
            Assert.IsTrue(SecureAssembly.Load(signedTestDllPath, new X509Certificate2[] { signedCertificate }) != default);

            var selfSignedCertificate = new X509Certificate2(selfSignedCertificateCerPath);
            Assert.IsTrue(SecureAssembly.Load(selfSignedTestDllPath, selfSignedCertificate, verifyCertificateChain: false) != default);
            Assert.IsTrue(SecureAssembly.Load(selfSignedTestDllPath, new X509Certificate2[] { selfSignedCertificate }, verifyCertificateChain: false) != default);
        }

        [TestMethod]
        public void SecureAssemblyLoadFile_CertificatePathArgument_Success()
        {
            Assert.IsTrue(SecureAssembly.Load(signedTestDllPath, signedCertificatePath) != default);
            Assert.IsTrue(SecureAssembly.Load(signedTestDllPath, new string[] { signedCertificatePath }) != default);

            Assert.IsTrue(SecureAssembly.Load(selfSignedTestDllPath, selfSignedCertificateCerPath, verifyCertificateChain: false) != default);
            Assert.IsTrue(SecureAssembly.Load(selfSignedTestDllPath, new string[] { selfSignedCertificateCerPath }, verifyCertificateChain: false) != default);
        }

        [TestMethod]
        public void SecureAssemblyLoadFile_CertificateInBytesArgument_Success()
        {
            var signedCertificateInBytes = System.IO.File.ReadAllBytes(signedCertificatePath);
            Assert.IsTrue(SecureAssembly.Load(signedTestDllPath, signedCertificateInBytes) != default);
            Assert.IsTrue(SecureAssembly.Load(signedTestDllPath, new List<byte[]> { signedCertificateInBytes }) != default);

            var selfSignedCertificateInBytes = System.IO.File.ReadAllBytes(selfSignedCertificateCerPath); ;
            Assert.IsTrue(SecureAssembly.Load(selfSignedTestDllPath, selfSignedCertificateInBytes, verifyCertificateChain: false) != default);
            Assert.IsTrue(SecureAssembly.Load(selfSignedTestDllPath, new List<byte[]> { selfSignedCertificateInBytes }, verifyCertificateChain: false) != default);
        }

        [TestMethod]
        public void SecureAssemblyLoadFile_X509Certificate2Argument_Failure()
        {
            // Arguments Test
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.Load(string.Empty, default(X509Certificate2)));
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.Load("ValidString", new X509Certificate2[0]));

            var selfSignedCertificate = new X509Certificate2(selfSignedCertificatePfxPath, selfSignedCertificatePassword);

            // Signed with different Certificate test
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.Load(signedTestDllPath, selfSignedCertificate));

            // Self Signed Extension (.pfx/p.12) Test
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.Load(selfSignedTestDllPath, selfSignedCertificate));

            // Unsigned Test
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.Load(unsignedTestDllPath, new X509Certificate2()));

            // Tampered Test
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.Load(tamperedTestDllPath, new X509Certificate2()));
        }

        [TestMethod]
        public void SecureAssemblyLoadFile_CertificatePathArgument_Failure()
        {
            // Arguments Test
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.Load(string.Empty, default(string)));
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.Load("ValidString", new string[0]));
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.Load(signedTestDllPath, selfSignedCertificatePfxPath));

            // Signed with different Certificate test
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.Load(selfSignedTestDllPath, signedCertificatePath));

            // Unsigned Test
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.Load(unsignedTestDllPath, signedCertificatePath));

            // Tampered Test
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.Load(tamperedTestDllPath, signedCertificatePath));
        }

        [TestMethod]
        public void SecureAssemblyLoadFile_CertificateInBytes_Failure()
        {
            // Arguments Test
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.Load(string.Empty, default(byte[])));
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.Load(string.Empty, default(List<byte[]>)));
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.Load("ValidString", new byte[0]));
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.Load("ValidString", new List<byte[]> { new byte[0] }));
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.Load(signedTestDllPath, selfSignedCertificatePfxPath));

            var signedCertificateInBytes = System.IO.File.ReadAllBytes(signedCertificatePath);

            // Signed with different Certificate test
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.Load(selfSignedTestDllPath, signedCertificateInBytes));
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.Load(selfSignedTestDllPath, new List<byte[]> { signedCertificateInBytes }));

            // Unsigned Test
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.Load(unsignedTestDllPath, signedCertificateInBytes));
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.Load(unsignedTestDllPath, new List<byte[]> { signedCertificateInBytes }));

            // Tampered Test
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.Load(tamperedTestDllPath, signedCertificateInBytes));
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.Load(tamperedTestDllPath, new List<byte[]> { signedCertificateInBytes }));
        }
    }
}
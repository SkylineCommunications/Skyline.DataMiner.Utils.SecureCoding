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
            Assert.IsTrue(SecureAssembly.LoadFrom(signedTestDllPath, signedCertificate) != default);
            Assert.IsTrue(SecureAssembly.LoadFrom(signedTestDllPath, new X509Certificate2[] { signedCertificate }) != default);
            Assert.IsTrue(SecureAssembly.LoadFile(signedTestDllPath, signedCertificate) != default);
            Assert.IsTrue(SecureAssembly.LoadFile(signedTestDllPath, new X509Certificate2[] { signedCertificate }) != default);

            var selfSignedCertificate = new X509Certificate2(selfSignedCertificateCerPath);
            Assert.IsTrue(SecureAssembly.LoadFrom(selfSignedTestDllPath, selfSignedCertificate, verifyCertificateChain: false) != default);
            Assert.IsTrue(SecureAssembly.LoadFrom(selfSignedTestDllPath, new X509Certificate2[] { selfSignedCertificate }, verifyCertificateChain: false) != default);
            Assert.IsTrue(SecureAssembly.LoadFile(selfSignedTestDllPath, selfSignedCertificate, verifyCertificateChain: false) != default);
            Assert.IsTrue(SecureAssembly.LoadFile(selfSignedTestDllPath, new X509Certificate2[] { selfSignedCertificate }, verifyCertificateChain: false) != default);
        }

        [TestMethod]
        public void SecureAssemblyLoadFile_CertificatePathArgument_Success()
        {
            Assert.IsTrue(SecureAssembly.LoadFrom(signedTestDllPath, signedCertificatePath) != default);
            Assert.IsTrue(SecureAssembly.LoadFrom(signedTestDllPath, new string[] { signedCertificatePath }) != default);
            Assert.IsTrue(SecureAssembly.LoadFile(signedTestDllPath, signedCertificatePath) != default);
            Assert.IsTrue(SecureAssembly.LoadFile(signedTestDllPath, new string[] { signedCertificatePath }) != default);

            Assert.IsTrue(SecureAssembly.LoadFrom(selfSignedTestDllPath, selfSignedCertificateCerPath, verifyCertificateChain: false) != default);
            Assert.IsTrue(SecureAssembly.LoadFrom(selfSignedTestDllPath, new string[] { selfSignedCertificateCerPath }, verifyCertificateChain: false) != default);
            Assert.IsTrue(SecureAssembly.LoadFile(selfSignedTestDllPath, selfSignedCertificateCerPath, verifyCertificateChain: false) != default);
            Assert.IsTrue(SecureAssembly.LoadFile(selfSignedTestDllPath, new string[] { selfSignedCertificateCerPath }, verifyCertificateChain: false) != default);
        }

        [TestMethod]
        public void SecureAssemblyLoadFile_CertificateInBytesArgument_Success()
        {
            var signedCertificateInBytes = System.IO.File.ReadAllBytes(signedCertificatePath);
            Assert.IsTrue(SecureAssembly.LoadFrom(signedTestDllPath, signedCertificateInBytes) != default);
            Assert.IsTrue(SecureAssembly.LoadFrom(signedTestDllPath, new List<byte[]> { signedCertificateInBytes }) != default);
            Assert.IsTrue(SecureAssembly.LoadFile(signedTestDllPath, signedCertificateInBytes) != default);
            Assert.IsTrue(SecureAssembly.LoadFile(signedTestDllPath, new List<byte[]> { signedCertificateInBytes }) != default);

            var selfSignedCertificateInBytes = System.IO.File.ReadAllBytes(selfSignedCertificateCerPath); ;
            Assert.IsTrue(SecureAssembly.LoadFrom(selfSignedTestDllPath, selfSignedCertificateInBytes, verifyCertificateChain: false) != default);
            Assert.IsTrue(SecureAssembly.LoadFrom(selfSignedTestDllPath, new List<byte[]> { selfSignedCertificateInBytes }, verifyCertificateChain: false) != default);
            Assert.IsTrue(SecureAssembly.LoadFile(selfSignedTestDllPath, selfSignedCertificateInBytes, verifyCertificateChain: false) != default);
            Assert.IsTrue(SecureAssembly.LoadFile(selfSignedTestDllPath, new List<byte[]> { selfSignedCertificateInBytes }, verifyCertificateChain: false) != default);
        }

        [TestMethod]
        public void SecureAssemblyLoadFile_X509Certificate2Argument_Failure()
        {
            // Arguments Test
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.LoadFrom(string.Empty, default(X509Certificate2)));
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.LoadFrom("ValidString", new X509Certificate2[0]));
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.LoadFile(string.Empty, default(X509Certificate2)));
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.LoadFile("ValidString", new X509Certificate2[0]));

            var selfSignedCertificate = new X509Certificate2(selfSignedCertificateCerPath);

            // Signed with different Certificate test
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFrom(signedTestDllPath, selfSignedCertificate));
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFile(signedTestDllPath, selfSignedCertificate));

            // Self Signed Extension (.pfx/p.12) Test
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFrom(selfSignedTestDllPath, selfSignedCertificate));
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFile(selfSignedTestDllPath, selfSignedCertificate));

            var emptyByteArr = Array.Empty<byte>();

            // Unsigned Test
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFrom(unsignedTestDllPath, new X509Certificate2(emptyByteArr)));
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFile(unsignedTestDllPath, new X509Certificate2(emptyByteArr)));

            // Tampered Test
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFrom(tamperedTestDllPath, new X509Certificate2(emptyByteArr)));
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFile(tamperedTestDllPath, new X509Certificate2(emptyByteArr)));
        }

        [TestMethod]
        public void SecureAssemblyLoadFile_CertificatePathArgument_Failure()
        {
            // Arguments Test
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.LoadFrom(string.Empty, default(string)));
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.LoadFrom("ValidString", new string[0]));
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.LoadFrom(signedTestDllPath, selfSignedCertificatePfxPath));

            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.LoadFile(string.Empty, default(string)));
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.LoadFile("ValidString", new string[0]));
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.LoadFile(signedTestDllPath, selfSignedCertificatePfxPath));

            // Signed with different Certificate test
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFrom(selfSignedTestDllPath, signedCertificatePath));
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFile(selfSignedTestDllPath, signedCertificatePath));

            // Unsigned Test
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFrom(unsignedTestDllPath, signedCertificatePath));
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFile(unsignedTestDllPath, signedCertificatePath));

            // Tampered Test
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFrom(tamperedTestDllPath, signedCertificatePath));
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFile(tamperedTestDllPath, signedCertificatePath));
        }

        [TestMethod]
        public void SecureAssemblyLoadFile_CertificateInBytes_Failure()
        {
            // Arguments Test
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.LoadFrom(string.Empty, default(byte[])));
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.LoadFrom(string.Empty, default(List<byte[]>)));
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.LoadFrom("ValidString", new byte[0]));
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.LoadFrom("ValidString", new List<byte[]> { new byte[0] }));
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.LoadFrom(signedTestDllPath, selfSignedCertificatePfxPath));

            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.LoadFile(string.Empty, default(byte[])));
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.LoadFile(string.Empty, default(List<byte[]>)));
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.LoadFile("ValidString", new byte[0]));
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.LoadFile("ValidString", new List<byte[]> { new byte[0] }));
            Assert.ThrowsException<ArgumentException>(() => SecureAssembly.LoadFile(signedTestDllPath, selfSignedCertificatePfxPath));

            var signedCertificateInBytes = System.IO.File.ReadAllBytes(signedCertificatePath);

            // Signed with different Certificate test
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFrom(selfSignedTestDllPath, signedCertificateInBytes));
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFrom(selfSignedTestDllPath, new List<byte[]> { signedCertificateInBytes }));
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFile(selfSignedTestDllPath, signedCertificateInBytes));
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFile(selfSignedTestDllPath, new List<byte[]> { signedCertificateInBytes }));

            // Unsigned Test
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFrom(unsignedTestDllPath, signedCertificateInBytes));
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFrom(unsignedTestDllPath, new List<byte[]> { signedCertificateInBytes }));
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFile(unsignedTestDllPath, signedCertificateInBytes));
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFile(unsignedTestDllPath, new List<byte[]> { signedCertificateInBytes }));

            // Tampered Test
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFrom(tamperedTestDllPath, signedCertificateInBytes));
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFrom(tamperedTestDllPath, new List<byte[]> { signedCertificateInBytes }));
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFile(tamperedTestDllPath, signedCertificateInBytes));
            Assert.ThrowsException<SecurityException>(() => SecureAssembly.LoadFile(tamperedTestDllPath, new List<byte[]> { signedCertificateInBytes }));
        }
    }
}
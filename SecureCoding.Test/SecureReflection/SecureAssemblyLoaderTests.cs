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
        private static string testItemsPath = FileSystem.Instance.Path.GetFullPath(System.IO.Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "SecureReflection", "TestItems"));
        private static string signedTestDllPath = System.IO.Path.Combine(testItemsPath, "Signed.dll");
        private static string selfSignedTestDllPath = System.IO.Path.Combine(testItemsPath, "SelfSigned.dll");
        private static string unsignedTestDllPath = System.IO.Path.Combine(testItemsPath, "Unsigned.dll");
        private static string tamperedTestDllPath = System.IO.Path.Combine(testItemsPath, "Tampered.dll");

        private static string signedCertificatePath = System.IO.Path.Combine(testItemsPath, "Signed.cer");
        private static string selfSignedCertificateCerPath = System.IO.Path.Combine(testItemsPath, "SelfSigned.cer");
        private static string selfSignedCertificatePfxPath = System.IO.Path.Combine(testItemsPath, "SelfSigned.pfx");
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
            Assert.Throws<ArgumentException>(() => SecureAssembly.LoadFrom(string.Empty, default(X509Certificate2)));
            Assert.Throws<ArgumentException>(() => SecureAssembly.LoadFrom("ValidString", new X509Certificate2[0]));
            Assert.Throws<ArgumentException>(() => SecureAssembly.LoadFile(string.Empty, default(X509Certificate2)));
            Assert.Throws<ArgumentException>(() => SecureAssembly.LoadFile("ValidString", new X509Certificate2[0]));

            var selfSignedCertificate = new X509Certificate2(selfSignedCertificatePfxPath, selfSignedCertificatePassword);

            // Signed with different Certificate test
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFrom(signedTestDllPath, selfSignedCertificate));
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFile(signedTestDllPath, selfSignedCertificate));

            // Self Signed Extension (.pfx/p.12) Test
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFrom(selfSignedTestDllPath, selfSignedCertificate));
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFile(selfSignedTestDllPath, selfSignedCertificate));

            var emptyByteArr = Array.Empty<byte>();

            // Unsigned Test
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFrom(unsignedTestDllPath, new X509Certificate2(emptyByteArr)));
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFile(unsignedTestDllPath, new X509Certificate2(emptyByteArr)));

            // Tampered Test
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFrom(tamperedTestDllPath, new X509Certificate2(emptyByteArr)));
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFile(tamperedTestDllPath, new X509Certificate2(emptyByteArr)));
        }

        [TestMethod]
        public void SecureAssemblyLoadFile_CertificatePathArgument_Failure()
        {
            // Arguments Test
            Assert.Throws<ArgumentException>(() => SecureAssembly.LoadFrom(string.Empty, default(string)));
            Assert.Throws<ArgumentException>(() => SecureAssembly.LoadFrom("ValidString", new string[0]));
            Assert.Throws<ArgumentException>(() => SecureAssembly.LoadFrom(signedTestDllPath, selfSignedCertificatePfxPath));

            Assert.Throws<ArgumentException>(() => SecureAssembly.LoadFile(string.Empty, default(string)));
            Assert.Throws<ArgumentException>(() => SecureAssembly.LoadFile("ValidString", new string[0]));
            Assert.Throws<ArgumentException>(() => SecureAssembly.LoadFile(signedTestDllPath, selfSignedCertificatePfxPath));

            // Signed with different Certificate test
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFrom(selfSignedTestDllPath, signedCertificatePath));
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFile(selfSignedTestDllPath, signedCertificatePath));

            // Unsigned Test
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFrom(unsignedTestDllPath, signedCertificatePath));
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFile(unsignedTestDllPath, signedCertificatePath));

            // Tampered Test
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFrom(tamperedTestDllPath, signedCertificatePath));
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFile(tamperedTestDllPath, signedCertificatePath));
        }

        [TestMethod]
        public void SecureAssemblyLoadFile_CertificateInBytes_Failure()
        {
            // Arguments Test
            Assert.Throws<ArgumentException>(() => SecureAssembly.LoadFrom(string.Empty, default(byte[])));
            Assert.Throws<ArgumentException>(() => SecureAssembly.LoadFrom(string.Empty, default(List<byte[]>)));
            Assert.Throws<ArgumentException>(() => SecureAssembly.LoadFrom("ValidString", new byte[0]));
            Assert.Throws<ArgumentException>(() => SecureAssembly.LoadFrom("ValidString", new List<byte[]> { new byte[0] }));
            Assert.Throws<ArgumentException>(() => SecureAssembly.LoadFrom(signedTestDllPath, selfSignedCertificatePfxPath));

            Assert.Throws<ArgumentException>(() => SecureAssembly.LoadFile(string.Empty, default(byte[])));
            Assert.Throws<ArgumentException>(() => SecureAssembly.LoadFile(string.Empty, default(List<byte[]>)));
            Assert.Throws<ArgumentException>(() => SecureAssembly.LoadFile("ValidString", new byte[0]));
            Assert.Throws<ArgumentException>(() => SecureAssembly.LoadFile("ValidString", new List<byte[]> { new byte[0] }));
            Assert.Throws<ArgumentException>(() => SecureAssembly.LoadFile(signedTestDllPath, selfSignedCertificatePfxPath));

            var signedCertificateInBytes = System.IO.File.ReadAllBytes(signedCertificatePath);

            // Signed with different Certificate test
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFrom(selfSignedTestDllPath, signedCertificateInBytes));
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFrom(selfSignedTestDllPath, new List<byte[]> { signedCertificateInBytes }));
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFile(selfSignedTestDllPath, signedCertificateInBytes));
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFile(selfSignedTestDllPath, new List<byte[]> { signedCertificateInBytes }));

            // Unsigned Test
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFrom(unsignedTestDllPath, signedCertificateInBytes));
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFrom(unsignedTestDllPath, new List<byte[]> { signedCertificateInBytes }));
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFile(unsignedTestDllPath, signedCertificateInBytes));
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFile(unsignedTestDllPath, new List<byte[]> { signedCertificateInBytes }));

            // Tampered Test
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFrom(tamperedTestDllPath, signedCertificateInBytes));
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFrom(tamperedTestDllPath, new List<byte[]> { signedCertificateInBytes }));
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFile(tamperedTestDllPath, signedCertificateInBytes));
            Assert.Throws<SecurityException>(() => SecureAssembly.LoadFile(tamperedTestDllPath, new List<byte[]> { signedCertificateInBytes }));
        }
    }
}
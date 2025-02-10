namespace SecureCoding.Test.SecureReflection
{
    using System;
    using System.IO;
    using System.Security;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Skyline.DataMiner.CICD.FileSystem;
    using Skyline.DataMiner.Utils.SecureCoding.SecureReflection;

    [TestClass]
    public class SecureAssemblyLoaderTests
    {
        private static string signedTestDllPath = FileSystem.Instance.Path.GetFullPath($@"{Environment.CurrentDirectory}..\..\..\..\SecureReflection\DLLs\Signed.dll");
        private static string unsignedTestDllPath = FileSystem.Instance.Path.GetFullPath($@"{Environment.CurrentDirectory}..\..\..\..\SecureReflection\DLLs\Unsigned.dll");

        [TestMethod]
        public void SecureAssemblyLoadFile_Success()
        {
            Assert.IsTrue(SecureAssembly.Load(signedTestDllPath) != default);
        }

        [TestMethod]
        public void SecureAssemblyLoadFile_Failure()
        {
            Assert.ThrowsException<FileNotFoundException>(() => SecureAssembly.Load(string.Empty));

            Assert.ThrowsException<InvalidOperationException>(() => SecureAssembly.Load($@"{Environment.CurrentDirectory}..\..\..\..\SecureReflection\DLLs\Unsigned.dll"));

            Assert.ThrowsException<SecurityException>(() => SecureAssembly.Load(unsignedTestDllPath));
        }
    }
}
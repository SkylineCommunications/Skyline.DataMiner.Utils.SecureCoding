namespace SecureCoding.Test.SecureIO
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Skyline.DataMiner.Utils.SecureCoding.SecureIO;

    [TestClass]
    public class StringExtensionsWindowsTests
    {
        [TestMethod]
        [DataRow(@"C:\skyline dataminer")]
        [DataRow(@"C:\skyline dataminer\filename.txt")]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer")]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\filename.txt")]
        public void IsPathValidSuccess(string path)
        {
            Assert.IsTrue(StringExtensions.IsPathValid(path, OSPlatform.Windows));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.IsTrue(path.IsPathValid());
                Assert.AreEqual(path, SecurePath.CreateSecurePath(path).ToString());
            }
        }

        [TestMethod]
        // invalid characters in path
        [DataRow(@"C:/skyline dataminer")]
        [DataRow(@"C:\skyline dataminer?")]
        [DataRow(@"C:\skyline dataminer|")]
        [DataRow(@"C:\skyline dataminer<")]
        [DataRow(@"C:\skyline dataminer>")]
        [DataRow("C:\\skyline dataminer\\..\\")]
        [DataRow("C:\\skyline dataminer\\\0\\")]
        [DataRow(@"C:\%programfiles%")]
        // invalid characters in filename
        [DataRow(@"C:\skyline dataminer\file:name.txt")]
        [DataRow(@"C:\skyline dataminer\file<name.txt")]
        [DataRow(@"C:\skyline dataminer\file>name.txt")]
        [DataRow(@"C:\skyline dataminer\file|name.txt")]
        [DataRow("C:\\skyline dataminer\\file\0name.txt")]
        public void IsPathValidFailure(string path)
        {
            Assert.IsFalse(StringExtensions.IsPathValid(path, OSPlatform.Windows));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.IsFalse(path.IsPathValid());
                Assert.Throws<InvalidOperationException>(() => SecurePath.CreateSecurePath(path));
            }
        }

        [TestMethod]
        [OSCondition(OperatingSystems.Windows)]
        [DataRow(@"C:\skyline dataminer\filename.txt", true)]
        [DataRow(@"C:/skyline dataminer", false)]
        public void IsPathValidUsesCurrentPlatformOnWindows(string path, bool expected)
        {
            Assert.AreEqual(expected, StringExtensions.IsPathValid(path));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(@"")]
        [DataRow(@" ")]
        [DataRow("\n")]
        public void IsPathValidException(string path)
        {
            Assert.Throws<ArgumentException>(() => { StringExtensions.IsPathValid(path, OSPlatform.Windows); });
        }
    }
}
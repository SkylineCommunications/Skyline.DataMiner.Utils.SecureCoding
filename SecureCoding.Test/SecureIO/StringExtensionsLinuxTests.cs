namespace SecureCoding.Test.SecureIO
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Skyline.DataMiner.Utils.SecureCoding.SecureIO;

    /// <summary>
    /// Validates the Linux path rules. These rules are pure string logic and do not depend on the
    /// runtime OS, so they run on every platform by passing <see cref="OSPlatform.Linux"/> explicitly.
    /// </summary>
    [TestClass]
    public class StringExtensionsLinuxTests
    {
        [TestMethod]
        [DataRow("/opt/skyline dataminer")]
        [DataRow("/opt/skyline dataminer/")]
        [DataRow("/opt/skyline dataminer/filename.txt")]
        [DataRow("relative/path/filename.txt")]
        [DataRow("/tmp/2a302e5a2c4d4fdcb22de4c038277b67")]
        // characters that are invalid on Windows but valid on Linux
        [DataRow("/opt/skyline dataminer/file:name.txt")]
        [DataRow("/opt/skyline dataminer/file<name>.txt")]
        [DataRow("/opt/skyline dataminer/file|name.txt")]
        [DataRow("/opt/skyline dataminer/file?name*.txt")]
        [DataRow("/opt/skyline dataminer/file\"name.txt")]
        [DataRow(@"/opt/skyline dataminer/file\name.txt")]
        [DataRow("/opt/skyline dataminer/program%files")]
        public void IsPathValidSuccess(string path)
        {
            Assert.IsTrue(StringExtensions.IsPathValid(path, OSPlatform.Linux));
        }

        [TestMethod]
        // invalid characters in path
        [DataRow("/opt/skyline dataminer/../")]
        [DataRow("/opt/skyline\0dataminer/")]
        [DataRow("/opt/%skyline%dataminer")]
        [DataRow("/opt/skyline dataminer/%programfiles%")]
        // invalid characters in filename
        [DataRow("/opt/skyline dataminer/file\0name.txt")]
        [DataRow("/opt/skyline dataminer/%file%name.txt")]
        public void IsPathValidFailure(string path)
        {
            Assert.IsFalse(StringExtensions.IsPathValid(path, OSPlatform.Linux));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("\n")]
        public void IsPathValidException(string path)
        {
            Assert.Throws<ArgumentException>(() => { StringExtensions.IsPathValid(path, OSPlatform.Linux); });
        }

        [TestMethod]
        public void IsPathValidUnsupportedPlatform()
        {
            Assert.Throws<NotSupportedException>(() => { StringExtensions.IsPathValid("/opt/skyline dataminer", OSPlatform.OSX); });
        }
    }
}
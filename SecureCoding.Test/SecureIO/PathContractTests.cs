namespace SecureCoding.Test.SecureIO
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Skyline.DataMiner.Utils.SecureCoding.SecureIO;

    [TestClass]
    public class PathContractTests
    {
        private static string BasePath => Path.GetFullPath(Path.Combine(Path.GetTempPath(), "secure-path-contract"));

        [TestMethod]
        [DataRow("report..txt")]
        [DataRow("..hidden")]
        public void ConstructionRejectsNamesRejectedByLexicalPolicy(string name)
        {
            Assert.IsFalse(name.IsPathValid());
            Assert.Throws<InvalidOperationException>(() => SecurePath.ConstructSecurePath(BasePath, name));
            Assert.Throws<InvalidOperationException>(() => SecurePath.ConstructSecurePath(new[] { BasePath, "child", name }));
            Assert.Throws<InvalidOperationException>(() => SecurePath.ConstructSecurePathWithSubDirectories(BasePath, name));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void ParamsOverloadRejectsMissingSegments(string segment)
        {
            Assert.Throws<ArgumentException>(() => SecurePath.ConstructSecurePath(new[] { segment, "file.txt" }));
            Assert.Throws<ArgumentException>(() => SecurePath.ConstructSecurePath(new[] { BasePath, segment, "file.txt" }));
            Assert.Throws<ArgumentException>(() => SecurePath.ConstructSecurePath(new[] { BasePath, segment }));
        }

        [TestMethod]
        public void ParamsOverloadValidatesArrayAndSingleSegment()
        {
            Assert.Throws<ArgumentNullException>(() => SecurePath.ConstructSecurePath((string[])null));
            Assert.Throws<ArgumentException>(() => SecurePath.ConstructSecurePath(Array.Empty<string>()));
            Assert.AreEqual(BasePath, SecurePath.ConstructSecurePath(new[] { BasePath }).ToString());
        }

        [TestMethod]
        [DataRow("safe.txt")]
        [DataRow("README")]
        [DataRow(".hidden")]
        [DataRow("one%percent.txt")]
        public void SuccessfulConstructionAgreesWithExtension(string name)
        {
            var expected = Path.Combine(BasePath, name);
            Assert.AreEqual(expected, SecurePath.ConstructSecurePath(BasePath, name).ToString());
            Assert.AreEqual(expected, SecurePath.ConstructSecurePath(new[] { BasePath, name }).ToString());
            Assert.AreEqual(expected, SecurePath.ConstructSecurePathWithSubDirectories(BasePath, name).ToString());
            Assert.IsTrue(expected.IsPathValid());
        }

        [TestMethod]
        [OSCondition(OperatingSystems.Linux)]
        [DataRow("te:st.txt")]
        [DataRow(@"te\st.txt")]
        public void LinuxLexicalValidityDoesNotImplySafeConstruction(string name)
        {
            Assert.IsTrue(name.IsPathValid(OSPlatform.Linux));
            Assert.AreEqual(name, SecurePath.CreateSecurePath(name).ToString());
            Assert.Throws<InvalidOperationException>(() => SecurePath.ConstructSecurePath(BasePath, name));
        }

        [TestMethod]
        public void RootedSiblingCannotReplaceBase()
        {
            var sibling = BasePath + "-sibling" + Path.DirectorySeparatorChar + "file.txt";
            Assert.Throws<InvalidOperationException>(() => SecurePath.ConstructSecurePathWithSubDirectories(BasePath, sibling));
            Assert.Throws<InvalidOperationException>(() => SecurePath.ConstructSecurePath(new[] { BasePath, "child", sibling }));
        }
    }
}

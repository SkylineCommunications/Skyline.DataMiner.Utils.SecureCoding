namespace Skyline.DataMiner.Utils.SecureCoding.Tests
{
    using Skyline.DataMiner.Utils.SecureCoding.SecureIO;

    [TestClass]
    public class StringExtensionsTests
    {
        [TestMethod]
        [DataRow(@"C:\skyline dataminer")]
        [DataRow(@"C:\skyline dataminer\filename.txt")]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer")]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\filename.txt")]
        public void IsPathValidSuccess(string path)
        {
            Assert.IsTrue(StringExtensions.IsPathValid(path));
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
            Assert.IsFalse(StringExtensions.IsPathValid(path));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(@"")]
        [DataRow(@" ")]
        [DataRow("\n")]
        public void IsPathValidException(string path)
        {
            Assert.ThrowsException<ArgumentException>(() => { StringExtensions.IsPathValid(path); });
        }
    }
}
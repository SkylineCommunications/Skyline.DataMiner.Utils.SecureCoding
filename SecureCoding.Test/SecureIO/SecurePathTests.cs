using Skyline.DataMiner.Utils.Security.SecureIO;

namespace Skyline.DataMiner.Utils.SecureCoding.Tests
{
    [TestClass]
    public class SecurePathUnitTests
    {
        [TestMethod]
        // straightforward cases
        [DataRow(@"C:\skyline dataminer\", @"test.txt", false, @"C:\skyline dataminer\test.txt")]
        [DataRow(@"C:\skyline dataminer", @"test.txt", false, @"C:\skyline dataminer\test.txt")]
        // straightforward cases with subdirectories
        [DataRow(@"C:\skyline dataminer\", @"subdir\test.txt", true, @"C:\skyline dataminer\subdir\test.txt")]
        [DataRow(@"C:\skyline dataminer", @"subdir\test.txt", true, @"C:\skyline dataminer\subdir\test.txt")]
        // edge case where we traverse out of a subdir
        [DataRow(@"C:\skyline dataminer\", @"subdir\..\test.txt", false, @"C:\skyline dataminer\test.txt")]
        [DataRow(@"C:\skyline dataminer", @"subdir\..\test.txt", false, @"C:\skyline dataminer\test.txt")]
        // edge case where a path with 2 % characters that results in the expected directory
        [DataRow(@"%programfiles%", @"test.txt", false, @"C:\program files\test.txt")]
        // case where wrong directory delimiter is used
        [DataRow(@"C:/skyline dataminer/", @"test.txt", false, @"C:\skyline dataminer\test.txt")]
        [DataRow(@"C:/skyline dataminer/", @"test.txt", false, @"C:\skyline dataminer\test.txt")]
        public void ConstructSecurePathSuccess(string basePath, string filename, bool subdirectoriesAllowed, string expectedResult)
        {
            string result = SecurePath.ConstructSecurePath(basePath, filename, subdirectoriesAllowed);
            Assert.AreEqual(result, expectedResult);
        }

        [TestMethod]
        // straightforward cases with subdirectories
        [DataRow(true, @"C:\skyline dataminer\subdir1\subdir2\test.txt", @"C:\skyline dataminer", @"subdir1", @"subdir2", @"test.txt")]
        [DataRow(true, @"C:\skyline dataminer\subdir1\subdir2\test.txt", @"C:\skyline dataminer\", @"subdir1", @"subdir2", @"test.txt")]
        // edge case where we traverse out of a subdir
        [DataRow(true, @"C:\skyline dataminer\subdir1\test.txt", @"C:\skyline dataminer", @"subdir1", @"subdir2", @"..", @"test.txt")]
        [DataRow(true, @"C:\skyline dataminer\subdir1\test.txt", @"C:\skyline dataminer\", @"subdir1", @"subdir2", @"..", @"test.txt")]
        // edge case with 2 % characters that results in the expected directory
        [DataRow(true, @"C:\program files\subdir1\test.txt", @"C:\skyline dataminer\", @"%programfiles%", @"subdir1", @"test.txt")]
        // case where wrong directory delimiter is used
        [DataRow(true, @"C:\skyline dataminer\subdir1\subdir2\test.txt", @"C:/skyline dataminer", @"subdir1", @"subdir2", @"test.txt")]
        [DataRow(true, @"C:\skyline dataminer\subdir1\subdir2\test.txt", @"C:/skyline dataminer/", @"subdir1", @"subdir2", @"test.txt")]
        public void ConstructSecurePathWithParamsSuccess(bool subdirectoriesAllowed, string expectedResult, params string[] paths)
        {
            string result = SecurePath.ConstructSecurePath(subdirectoriesAllowed, paths);
            Assert.AreEqual(result, expectedResult);
        }

        [TestMethod]
        // cases with invalid basedir
        [DataRow(null, @"\subdir\test.txt", false, typeof(ArgumentException))]
        [DataRow("", @"\subdir\test.txt", false, typeof(ArgumentException))]
        [DataRow(" ", @"\subdir\test.txt", false, typeof(ArgumentException))]
        [DataRow("\n", @"\subdir\test.txt", false, typeof(ArgumentException))]
        // cases with invalid filename
        [DataRow(@"C:\skyline dataminer\", null, false, typeof(ArgumentException))]
        [DataRow(@"C:\skyline dataminer\", "", false, typeof(ArgumentException))]
        [DataRow(@"C:\skyline dataminer\", " ", false, typeof(ArgumentException))]
        [DataRow(@"C:\skyline dataminer\", "\n", false, typeof(ArgumentException))]
        // cases with invalid characters
        [DataRow(@"C:\skyline dataminer>\", @"test.txt", false, typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer\", @"<test.txt", false, typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer|\", @"test.txt", false, typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer\", "\"test.txt", false, typeof(InvalidOperationException))]
        // cases with directory traversal
        [DataRow(@"C:\skyline dataminer", @"..\..\..\test.txt", false, typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer", @"C:\test.txt", false, typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer", @"\\127.0.0.1\c$\test.txt", false, typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer", @"%programfiles%\test.txt", false, typeof(InvalidOperationException))]
        [DataRow(@"%programfiles%", @"C:\skyline dataminer\test.txt", false, typeof(InvalidOperationException))]
        // cases with subdirectories while it's not allowed
        [DataRow(@"C:\skyline dataminer\", @"subdir\test.txt", false, typeof(InvalidOperationException))]
        public void ConstructSecurePathFailure(string basePath, string filename, bool subdirectoriesAllowed, Type expectedExceptionType)
        {
            typeof(Assert)
                .GetMethods()
                .Where(m => m.Name == "ThrowsException")
                .Where(m => m.GetParameters().Length == 1)
                .First(m => m.GetParameters()[0].ParameterType == typeof(Func<object?>))
                .MakeGenericMethod(expectedExceptionType)
                .Invoke(null, new object[] { () => SecurePath.ConstructSecurePath(basePath, filename, subdirectoriesAllowed) });
        }

        [TestMethod]
        public void ConstructSecurePathWithParamsFailure(bool subdirectoriesAllowed, Type expectedExceptionType, params string[] paths)
        {
            typeof(Assert)
                .GetMethods()
                .Where(m => m.Name == "ThrowsException")
                .Where(m => m.GetParameters().Length == 1)
                .First(m => m.GetParameters()[0].ParameterType == typeof(Func<object?>))
                .MakeGenericMethod(expectedExceptionType)
                .Invoke(null, new object[] { () => SecurePath.ConstructSecurePath(subdirectoriesAllowed, paths) });
        }

        [TestMethod]
        public void IsPathValidSuccess(string path)
        {
            Assert.IsTrue(SecurePath.IsPathValid(path));
        }

        [TestMethod]
        public void IsPathValidFailure(string path)
        {
            Assert.IsFalse(SecurePath.IsPathValid(path));
        }
    }
}
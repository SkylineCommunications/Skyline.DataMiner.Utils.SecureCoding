namespace SecureCoding.Test.SecureIO
{
    using System;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Skyline.DataMiner.Utils.SecureCoding.SecureIO;

    [TestClass]
    public class SecurePathUnitTests
    {
        [TestMethod]
        // straightforward cases
        [DataRow(@"C:\skyline dataminer\", @"test.txt", @"C:\skyline dataminer\test.txt")]
        [DataRow(@"C:\skyline dataminer", @"test.txt", @"C:\skyline dataminer\test.txt")]
        // network paths
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\", @"test.txt", @"\\127.0.0.1\c$\skyline dataminer\test.txt")]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer", @"test.txt", @"\\127.0.0.1\c$\skyline dataminer\test.txt")]
        public void ConstructSecureFilePathSuccess(string basePath, string filename, string expectedResult)
        {
            string result = SecurePath.ConstructSecurePath(basePath, filename);
            Assert.AreEqual(result, expectedResult);
        }

        [TestMethod]
        // straightforward cases
        [DataRow(@"C:\skyline dataminer\", @"subdir\test.txt", @"C:\skyline dataminer\subdir\test.txt")]
        [DataRow(@"C:\skyline dataminer", @"subdir\test.txt", @"C:\skyline dataminer\subdir\test.txt")]
        // network paths
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\", @"subdir\test.txt", @"\\127.0.0.1\c$\skyline dataminer\subdir\test.txt")]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer", @"subdir\test.txt", @"\\127.0.0.1\c$\skyline dataminer\subdir\test.txt")]
        public void ConstructSecureFilePathWithSubdirectoriesSuccess(string basePath, string filename, string expectedResult)
        {
            string result = SecurePath.ConstructSecurePathWithSubDirectories(basePath, filename);
            Assert.AreEqual(result, expectedResult);
        }

        [TestMethod]
        // straightforward cases
        [DataRow(@"C:\skyline dataminer\test.txt", @"C:\skyline dataminer", @"test.txt")]
        [DataRow(@"C:\skyline dataminer\test.txt", @"C:\skyline dataminer\", @"test.txt")]
        // straightforward cases with subdirectories
        [DataRow(@"C:\skyline dataminer\subdir1\subdir2\test.txt", @"C:\skyline dataminer", @"subdir1", @"subdir2", @"test.txt")]
        [DataRow(@"C:\skyline dataminer\subdir1\subdir2\test.txt", @"C:\skyline dataminer\", @"subdir1", @"subdir2", @"test.txt")]
        // edge case where one of the subdirs is absolute but points to the expected location
        [DataRow(@"C:\skyline dataminer\subdir1\test.txt", @"C:\", @"skyline dataminer", @"subdir1", @"test.txt")]
        // network paths
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\test.txt", @"\\127.0.0.1\c$\skyline dataminer", @"test.txt")]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\subdir1\subdir2\test.txt", @"\\127.0.0.1\c$\skyline dataminer", @"subdir1", @"subdir2", @"test.txt")]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\subdir1\test.txt", @"\\127.0.0.1\c$\", @"skyline dataminer", @"subdir1", @"test.txt")]
        public void ConstructSecureFilePathWithParamsSuccess(string expectedResult, params string[] paths)
        {
            string result = SecurePath.ConstructSecurePath(paths);
            Assert.AreEqual(result, expectedResult);
        }

        [TestMethod]
        // cases with invalid basedir
        [DataRow(null, @"\subdir\test.txt", typeof(ArgumentException))]
        [DataRow("", @"\subdir\test.txt", typeof(ArgumentException))]
        [DataRow(" ", @"\subdir\test.txt", typeof(ArgumentException))]
        [DataRow("\n", @"\subdir\test.txt", typeof(ArgumentException))]
        // cases with invalid filename
        [DataRow(@"C:\skyline dataminer\", null, typeof(ArgumentException))]
        [DataRow(@"C:\skyline dataminer\", "", typeof(ArgumentException))]
        [DataRow(@"C:\skyline dataminer\", " ", typeof(ArgumentException))]
        [DataRow(@"C:\skyline dataminer\", "\n", typeof(ArgumentException))]
        // cases with invalid characters in basedir
        [DataRow(@"C:\skyline dataminer>\", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer<\", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer|\", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer*\", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer?\", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:/skyline dataminer/", @"test.txt", typeof(InvalidOperationException))]
        [DataRow("C:\\skyline dataminer\"\\", @"test.txt", typeof(InvalidOperationException))]
        // cases with invalid characters in filename
        [DataRow(@"C:\skyline dataminer\", @">test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer\", @"<test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer\", @"*test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer\", @"?test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer\", @"|test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer\", "\"test.txt", typeof(InvalidOperationException))]
        // cases with directory traversal
        [DataRow(@"C:\skyline dataminer", @"subdir1\test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer", @"subdir1/test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer", @"..\..\..\test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer", @"C:\test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer", @"\\127.0.0.1\c$\test.txt", typeof(InvalidOperationException))]
        [DataRow(@"%programfiles%", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer", @"%programfiles%\test.txt", typeof(InvalidOperationException))]
        [DataRow(@"%programfiles%", @"C:\skyline dataminer\test.txt", typeof(InvalidOperationException))]
        // network paths
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer>\", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer<\", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer|\", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer*\", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer?\", @"test.txt", typeof(InvalidOperationException))]
        [DataRow("\\\\127.0.0.1\\c$\\skyline dataminer\"\\", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\", @">test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\", @"<test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\", @"*test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\", @"?test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\", @"|test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\", "\"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer", @"subdir1\test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer", @"subdir1/test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer", @"..\..\..\test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer", @"%programfiles%\test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\", @"\\127.0.0.1\c$\skyline dataminer\test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer", @"\\127.0.0.1\c$\skyline dataminer\subdir1\test.txt", typeof(InvalidOperationException))]
        public void ConstructSecureFilePathFailure(string basePath, string filename, Type expectedExceptionType)
        {
            Action action = () => SecurePath.ConstructSecurePath(basePath, filename);
            action.Should().Throw<Exception>().Which.Should().BeOfType(expectedExceptionType);
        }

        [TestMethod]
        // cases with invalid basedir
        [DataRow(null, @"\subdir\test.txt", typeof(ArgumentException))]
        [DataRow("", @"\subdir\test.txt", typeof(ArgumentException))]
        [DataRow(" ", @"\subdir\test.txt", typeof(ArgumentException))]
        [DataRow("\n", @"\subdir\test.txt", typeof(ArgumentException))]
        // cases with invalid relativePath
        [DataRow(@"C:\skyline dataminer\", null, typeof(ArgumentException))]
        [DataRow(@"C:\skyline dataminer\", "", typeof(ArgumentException))]
        [DataRow(@"C:\skyline dataminer\", " ", typeof(ArgumentException))]
        [DataRow(@"C:\skyline dataminer\", "\n", typeof(ArgumentException))]
        // cases with invalid characters
        [DataRow(@"C:\skyline dataminer>\", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer\", @"<test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer|\", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer*\", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer?\", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer\", "\"test.txt", typeof(InvalidOperationException))]
        // cases with directory traversal outside of the intended basedir
        [DataRow(@"C:\skyline dataminer", @"..\..\..\test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer", @"C:\test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer", @"\\127.0.0.1\c$\test.txt", typeof(InvalidOperationException))]
        [DataRow(@"%programfiles%", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer", @"%programfiles%\test.txt", typeof(InvalidOperationException))]
        [DataRow(@"%programfiles%", @"C:\skyline dataminer\test.txt", typeof(InvalidOperationException))]
        // case where wrong directory delimiter is used
        [DataRow(@"C:/skyline dataminer/", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:/skyline dataminer/", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"C:\skyline dataminer\", @"subdir/test.txt", typeof(InvalidOperationException))]
        // network paths
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer>\", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer<\", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer|\", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer*\", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer?\", @"test.txt", typeof(InvalidOperationException))]
        [DataRow("\\\\127.0.0.1\\c$\\skyline dataminer\"\\", @"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\", @">test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\", @"<test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\", @"*test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\", @"?test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\", @"|test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\", "\"test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer", @"subdir1/test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer", @"..\..\..\test.txt", typeof(InvalidOperationException))]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer", @"%programfiles%\test.txt", typeof(InvalidOperationException))]
        public void ConstructSecureFilePathWithSubDirectoriesFailure(string basePath, string relativePath, Type expectedExceptionType)
        {
            Action action = () => SecurePath.ConstructSecurePathWithSubDirectories(basePath, relativePath);
            action.Should().Throw<Exception>().Which.Should().BeOfType(expectedExceptionType);
        }

        [TestMethod]
        //case with invalid arguments
        [DataRow(typeof(ArgumentNullException), null)]
        [DataRow(typeof(ArgumentException), "")]
        // case with invalid characters in basepath
        [DataRow(typeof(InvalidOperationException), @"C:/skyline dataminer/", @"subdir1", @"test.txt")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer>\", @"subdir1", @"test.txt")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer<\", @"subdir1", @"test.txt")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer|\", @"subdir1", @"test.txt")]
        [DataRow(typeof(InvalidOperationException), "C:\\skyline dataminer\0\\", @"subdir1", @"test.txt")]
        [DataRow(typeof(InvalidOperationException), @"%programfiles%", @"subdir1", @"test.txt")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\..\", @"subdir1", @"test.txt")]
        // case with invalid characters in subdirectories
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer", @"subdir1/", @"subdir2", @"test.txt")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1>", @"subdir2", @"test.txt")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1<", @"subdir2", @"test.txt")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1|", @"subdir2", @"test.txt")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", "subdir1\0", @"subdir2", @"test.txt")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", "subdir1\"", @"subdir2", @"test.txt")]
        // case with invalid characters in filename
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer", @"subdir1", @"/test.txt")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1", @"<test.txt")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1", @">test.txt")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1", @"|test.txt")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1", "\0test.txt")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1", "\"test.txt")]
        // case with directory traversal
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1", @"..", "test.txt")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1", @"C:\", "test.txt")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1", @"\\127.0.0.1\c$\", "test.txt")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1", @"C:\test.txt")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1", @"\\127.0.0.1\c$\test.txt")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"%programdata%", @"test.txt")]
        // network path
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer>\", @"subdir1", @"test.txt")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer<\", @"subdir1", @"test.txt")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer|\", @"subdir1", @"test.txt")]
        [DataRow(typeof(InvalidOperationException), "\\\\127.0.0.1\\c$\\skyline dataminer\0\\", @"subdir1", @"test.txt")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\..\", @"subdir1", @"test.txt")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer", @"subdir1/", @"subdir2", @"test.txt")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"subdir1>", @"subdir2", @"test.txt")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"subdir1<", @"subdir2", @"test.txt")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"subdir1|", @"subdir2", @"test.txt")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", "subdir1\0", @"subdir2", @"test.txt")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", "subdir1\"", @"subdir2", @"test.txt")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer", @"subdir1", @"/test.txt")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"subdir1", @"<test.txt")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"subdir1", @">test.txt")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"subdir1", @"|test.txt")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"subdir1", "\0test.txt")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"subdir1", "\"test.txt")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"subdir1", @"..", "test.txt")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"subdir1", @"\\127.0.0.1\c$\", "test.txt")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"subdir1", @"\\127.0.0.1\c$\test.txt")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"%programdata%", @"test.txt")]
        public void ConstructSecureFilePathWithParamsFailure(Type expectedExceptionType, params string[] paths)
        {
            Action action = () => SecurePath.ConstructSecurePath(paths);
            action.Should().Throw<Exception>().Which.Should().BeOfType(expectedExceptionType);
        }

        [TestMethod]
        // straightforward cases
        [DataRow(@"C:\skyline dataminer\folder", @"C:\skyline dataminer", "folder")]
        [DataRow(@"C:\skyline dataminer\folder", @"C:\skyline dataminer", @"folder\")]
        [DataRow(@"C:\skyline dataminer\folder", @"C:\skyline dataminer\", "folder")]
        // straightforward cases with subdirectories
        [DataRow(@"C:\skyline dataminer\subdir1\subdir2", @"C:\skyline dataminer", @"subdir1", @"subdir2\")]
        [DataRow(@"C:\skyline dataminer\subdir1\subdir2", @"C:\skyline dataminer\", @"subdir1", @"subdir2")]
        // edge case where one of the subdirs is absolute but points to the expected location
        [DataRow(@"C:\skyline dataminer\subdir1", @"C:\", @"skyline dataminer", @"subdir1\")]
        // network paths
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\folder", @"\\127.0.0.1\c$\skyline dataminer", @"folder")]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\subdir1\subdir2", @"\\127.0.0.1\c$\skyline dataminer", @"subdir1", @"subdir2")]
        [DataRow(@"\\127.0.0.1\c$\skyline dataminer\subdir1", @"\\127.0.0.1\c$\", @"skyline dataminer", @"subdir1\")]
        public void ConstructSecureDirectoryPathWithParamsSuccess(string expectedResult, params string[] paths)
        {
            string result = SecurePath.ConstructSecurePath(paths);
            Assert.AreEqual(result, expectedResult);
        }

        [TestMethod]
        //case with invalid arguments
        [DataRow(typeof(ArgumentNullException), null)]
        [DataRow(typeof(ArgumentException), "")]
        // case with invalid characters in basepath
        [DataRow(typeof(InvalidOperationException), @"C:/skyline dataminer/", @"subdir1", @"subdir2")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer>\", @"subdir1", @"subdir2")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer<\", @"subdir1", @"subdir2")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer|\", @"subdir1", @"subdir2")]
        [DataRow(typeof(InvalidOperationException), "C:\\skyline dataminer\0\\", @"subdir1", @"subdir2")]
        [DataRow(typeof(InvalidOperationException), @"%programfiles%", @"subdir1", @"subdir2")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\..\", @"subdir1", @"subdir2")]
        // case with invalid characters in subdirectories
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer", @"subdir1/", @"subdir2")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1>", @"subdir2")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1<", @"subdir2")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1|", @"subdir2")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", "subdir1\0", @"subdir2")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", "subdir1\"", @"subdir2")]
        // case with invalid characters in last path segment
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer", @"subdir1", @"/subdir2")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1", @"<subdir2")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1", @">subdir2")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1", @"|subdir2")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1", "\0subdir2")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1", "\"subdir2")]
        // case with directory traversal
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1", @"..", "subdir2")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1", @"C:\", "subdir2")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1", @"\\127.0.0.1\c$\", "subdir2")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1", @"C:\subdir2")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"subdir1", @"\\127.0.0.1\c$\subdir2")]
        [DataRow(typeof(InvalidOperationException), @"C:\skyline dataminer\", @"%programdata%", @"subdir2")]
        // network path
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer>\", @"subdir1", @"subdir2")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer<\", @"subdir1", @"subdir2")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer|\", @"subdir1", @"subdir2")]
        [DataRow(typeof(InvalidOperationException), "\\\\127.0.0.1\\c$\\skyline dataminer\0\\", @"subdir1", @"subdir2")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\..\", @"subdir1", @"subdir2")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer", @"subdir1/", @"subdir2", @"subdir2")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"subdir1>", @"subdir2", @"subdir2")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"subdir1<", @"subdir2", @"subdir2")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"subdir1|", @"subdir2", @"subdir2")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", "subdir1\0", @"subdir2", @"subdir2")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", "subdir1\"", @"subdir2", @"subdir2")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer", @"subdir1", @"/subdir2")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"subdir1", @"<subdir2")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"subdir1", @">subdir2")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"subdir1", @"|subdir2")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"subdir1", "\0subdir2")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"subdir1", "\"subdir2")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"subdir1", @"..", "subdir2")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"subdir1", @"\\127.0.0.1\c$\", "subdir2")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"subdir1", @"\\127.0.0.1\c$\subdir2")]
        [DataRow(typeof(InvalidOperationException), @"\\127.0.0.1\c$\skyline dataminer\", @"%programdata%", @"subdir2")]
        public void ConstructSecureDirectoryPathWithParamsFailure(Type expectedException, params string[] paths)
        {
            Action action = () => SecurePath.ConstructSecurePath(paths);
            action.Should().Throw<Exception>().Which.Should().BeOfType(expectedException);
        }
    }
}
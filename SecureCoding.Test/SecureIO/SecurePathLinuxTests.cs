namespace SecureCoding.Test.SecureIO
{
    using System;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Skyline.DataMiner.Utils.SecureCoding.SecureIO;

    [TestClass]
    [OSCondition(OperatingSystems.Linux)]
    public class SecurePathLinuxUnitTests
    {
        [TestMethod]
        // straightforward cases
        [DataRow("/opt/skyline dataminer/", "test.txt", "/opt/skyline dataminer/test.txt")]
        [DataRow("/opt/skyline dataminer", "test.txt", "/opt/skyline dataminer/test.txt")]
        [DataRow("/", "test.txt", "/test.txt")]
        // temp directory with GUID-style filename without extension
        [DataRow("/tmp", "2a302e5a2c4d4fdcb22de4c038277b67", "/tmp/2a302e5a2c4d4fdcb22de4c038277b67")]
        [DataRow("/tmp/", "2a302e5a2c4d4fdcb22de4c038277b67", "/tmp/2a302e5a2c4d4fdcb22de4c038277b67")]
        // characters that are invalid on Windows but valid on Linux
        // NOTE: ':' and '\' are intentionally excluded here. The FileSystem abstraction (Skyline.DataMiner.CICD.FileSystem)
        // treats ':' as a drive separator and '\' as a directory separator on every OS, so they are rejected (see failure cases).
        [DataRow("/opt/skyline dataminer", "test?.txt", "/opt/skyline dataminer/test?.txt")]
        [DataRow("/opt/skyline dataminer", "te<st>.txt", "/opt/skyline dataminer/te<st>.txt")]
        [DataRow("/opt/skyline dataminer", "test|.txt", "/opt/skyline dataminer/test|.txt")]
        [DataRow("/opt/skyline dataminer", "test*.txt", "/opt/skyline dataminer/test*.txt")]
        [DataRow("/opt/skyline dataminer", "te\"st.txt", "/opt/skyline dataminer/te\"st.txt")]
        public void ConstructSecureFilePathSuccess(string basePath, string filename, string expectedResult)
        {
            string result = SecurePath.ConstructSecurePath(basePath, filename);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        // cases with invalid basedir
        [DataRow(null, "test.txt", typeof(ArgumentException))]
        [DataRow("", "test.txt", typeof(ArgumentException))]
        [DataRow(" ", "test.txt", typeof(ArgumentException))]
        // cases with invalid filename
        [DataRow("/opt/skyline dataminer", null, typeof(ArgumentException))]
        [DataRow("/opt/skyline dataminer", "", typeof(ArgumentException))]
        [DataRow("/opt/skyline dataminer", " ", typeof(ArgumentException))]
        // cases with invalid characters in basedir
        [DataRow("/opt/skyline\0dataminer", "test.txt", typeof(InvalidOperationException))]
        [DataRow("/opt/skyline dataminer/..", "test.txt", typeof(InvalidOperationException))]
        [DataRow("/opt/%skyline%dataminer", "test.txt", typeof(InvalidOperationException))]
        // cases with invalid characters in filename
        [DataRow("/opt/skyline dataminer", "te\0st.txt", typeof(InvalidOperationException))]
        [DataRow("/opt/skyline dataminer", "%te%st.txt", typeof(InvalidOperationException))]
        // cases with directory traversal
        [DataRow("/opt/skyline dataminer", "subdir/test.txt", typeof(InvalidOperationException))]
        [DataRow("/opt/skyline dataminer", "../test.txt", typeof(InvalidOperationException))]
        [DataRow("/opt/skyline dataminer", "..", typeof(InvalidOperationException))]
        [DataRow("/opt/skyline dataminer", ".", typeof(InvalidOperationException))]
        [DataRow("/opt/skyline dataminer", "/etc/passwd", typeof(InvalidOperationException))]
        // ':' and '\' are normalized as drive/directory separators by the FileSystem abstraction, so they are rejected
        [DataRow("/opt/skyline dataminer", "te:st.txt", typeof(InvalidOperationException))]
        [DataRow("/opt/skyline dataminer", @"te\st.txt", typeof(InvalidOperationException))]
        public void ConstructSecureFilePathFailure(string basePath, string filename, Type expectedExceptionType)
        {
            Action action = () => SecurePath.ConstructSecurePath(basePath, filename);
            action.Should().Throw<Exception>().Which.Should().BeOfType(expectedExceptionType);
        }

        [TestMethod]
        [DataRow("/opt/skyline dataminer/", "subdir/test.txt", "/opt/skyline dataminer/subdir/test.txt")]
        [DataRow("/opt/skyline dataminer", "subdir/test.txt", "/opt/skyline dataminer/subdir/test.txt")]
        [DataRow("/opt/skyline dataminer", "subdir1/subdir2/test.txt", "/opt/skyline dataminer/subdir1/subdir2/test.txt")]
        [DataRow("/opt/skyline dataminer", "subdir", "/opt/skyline dataminer/subdir")]
        [DataRow("/opt/skyline dataminer", "subdir/te|st.txt", "/opt/skyline dataminer/subdir/te|st.txt")]
        [DataRow("/tmp", "2a302e5a2c4d4fdcb22de4c038277b67", "/tmp/2a302e5a2c4d4fdcb22de4c038277b67")]
        public void ConstructSecureFilePathWithSubdirectoriesSuccess(string basePath, string relativePath, string expectedResult)
        {
            string result = SecurePath.ConstructSecurePathWithSubDirectories(basePath, relativePath);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        // cases with invalid basedir
        [DataRow(null, "subdir/test.txt", typeof(ArgumentException))]
        [DataRow("", "subdir/test.txt", typeof(ArgumentException))]
        [DataRow(" ", "subdir/test.txt", typeof(ArgumentException))]
        // cases with invalid relativePath
        [DataRow("/opt/skyline dataminer", null, typeof(ArgumentException))]
        [DataRow("/opt/skyline dataminer", "", typeof(ArgumentException))]
        [DataRow("/opt/skyline dataminer", " ", typeof(ArgumentException))]
        // cases with invalid characters
        [DataRow("/opt/skyline\0dataminer", "subdir/test.txt", typeof(InvalidOperationException))]
        [DataRow("/opt/skyline dataminer", "sub\0dir/test.txt", typeof(InvalidOperationException))]
        [DataRow("/opt/skyline dataminer", "subdir/te\0st.txt", typeof(InvalidOperationException))]
        [DataRow("/opt/skyline dataminer", "%sub%dir/test.txt", typeof(InvalidOperationException))]
        // cases with directory traversal outside of the intended basedir
        [DataRow("/opt/skyline dataminer", "../test.txt", typeof(InvalidOperationException))]
        [DataRow("/opt/skyline dataminer", "subdir/../../test.txt", typeof(InvalidOperationException))]
        [DataRow("/opt/skyline dataminer", "/etc/passwd", typeof(InvalidOperationException))]
        [DataRow("/opt/skyline dataminer/..", "subdir/test.txt", typeof(InvalidOperationException))]
        // ':' is treated as a drive separator by the FileSystem abstraction, making the relative path rooted
        [DataRow("/opt/skyline dataminer", "subdir/te:st.txt", typeof(InvalidOperationException))]
        public void ConstructSecureFilePathWithSubDirectoriesFailure(string basePath, string relativePath, Type expectedExceptionType)
        {
            Action action = () => SecurePath.ConstructSecurePathWithSubDirectories(basePath, relativePath);
            action.Should().Throw<Exception>().Which.Should().BeOfType(expectedExceptionType);
        }

        [TestMethod]
        // straightforward cases
        [DataRow("/opt/skyline dataminer/test.txt", "/opt/skyline dataminer", "test.txt")]
        [DataRow("/opt/skyline dataminer/test.txt", "/opt/skyline dataminer/", "test.txt")]
        // straightforward cases with subdirectories
        [DataRow("/opt/skyline dataminer/subdir1/subdir2/test.txt", "/opt/skyline dataminer", "subdir1", "subdir2", "test.txt")]
        [DataRow("/opt/skyline dataminer/subdir1/subdir2/test.txt", "/opt/skyline dataminer/", "subdir1", "subdir2", "test.txt")]
        // edge case where the base path is the root
        [DataRow("/opt/skyline dataminer/subdir1/test.txt", "/", "opt", "skyline dataminer", "subdir1", "test.txt")]
        // directory paths
        [DataRow("/opt/skyline dataminer/subdir1/subdir2", "/opt/skyline dataminer", "subdir1", "subdir2")]
        // characters that are invalid on Windows but valid on Linux
        [DataRow("/opt/skyline dataminer/sub<dir>/te|st.txt", "/opt/skyline dataminer", "sub<dir>", "te|st.txt")]
        public void ConstructSecurePathWithParamsSuccess(string expectedResult, params string[] paths)
        {
            string result = SecurePath.ConstructSecurePath(paths);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        // case with invalid arguments
        [DataRow(typeof(ArgumentNullException), null)]
        [DataRow(typeof(ArgumentException), "")]
        // case with invalid characters in basepath
        [DataRow(typeof(InvalidOperationException), "/opt/skyline\0dataminer", "subdir1", "test.txt")]
        [DataRow(typeof(InvalidOperationException), "/opt/skyline dataminer/..", "subdir1", "test.txt")]
        [DataRow(typeof(InvalidOperationException), "/opt/%skyline%dataminer", "subdir1", "test.txt")]
        // case with invalid characters in subdirectories
        [DataRow(typeof(InvalidOperationException), "/opt/skyline dataminer", "sub\0dir1", "subdir2", "test.txt")]
        [DataRow(typeof(InvalidOperationException), "/opt/skyline dataminer", "%sub%dir1", "subdir2", "test.txt")]
        // case with invalid characters in filename
        [DataRow(typeof(InvalidOperationException), "/opt/skyline dataminer", "subdir1", "te\0st.txt")]
        [DataRow(typeof(InvalidOperationException), "/opt/skyline dataminer", "subdir1", "%te%st.txt")]
        [DataRow(typeof(InvalidOperationException), "/opt/skyline dataminer", "subdir1", "subdir2/test.txt")]
        // case with directory traversal
        [DataRow(typeof(InvalidOperationException), "/opt/skyline dataminer", "subdir1", "..", "test.txt")]
        [DataRow(typeof(InvalidOperationException), "/opt/skyline dataminer", "..", "test.txt")]
        [DataRow(typeof(InvalidOperationException), "/opt/skyline dataminer", "subdir1", "/etc", "test.txt")]
        [DataRow(typeof(InvalidOperationException), "/opt/skyline dataminer", "subdir1", "/etc/passwd")]
        [DataRow(typeof(InvalidOperationException), "/opt/skyline dataminer", "subdir1", "/", "test.txt")]
        // ':' is treated as a drive separator by the FileSystem abstraction, making the segment rooted
        [DataRow(typeof(InvalidOperationException), "/opt/skyline dataminer", "sub:dir", "test.txt")]
        public void ConstructSecurePathWithParamsFailure(Type expectedExceptionType, params string[] paths)
        {
            Action action = () => SecurePath.ConstructSecurePath(paths);
            action.Should().Throw<Exception>().Which.Should().BeOfType(expectedExceptionType);
        }

        [TestMethod]
        [DataRow("/opt/skyline dataminer")]
        [DataRow("/opt/skyline dataminer/")]
        [DataRow("/opt/skyline dataminer/test.txt")]
        [DataRow("/opt/skyline dataminer/te:st?.txt")]
        [DataRow("relative/path/test.txt")]
        [DataRow("/tmp/2a302e5a2c4d4fdcb22de4c038277b67")]
        public void CreateSecurePathSuccess(string path)
        {
            string result = SecurePath.CreateSecurePath(path);
            Assert.AreEqual(path, result);
        }

        [TestMethod]
        [DataRow(null, typeof(ArgumentNullException))]
        [DataRow("", typeof(ArgumentException))]
        [DataRow(" ", typeof(ArgumentException))]
        [DataRow("/opt/skyline dataminer/../etc", typeof(InvalidOperationException))]
        [DataRow("/opt/skyline\0dataminer", typeof(InvalidOperationException))]
        [DataRow("/opt/skyline dataminer/te\0st.txt", typeof(InvalidOperationException))]
        [DataRow("/opt/%skyline%dataminer", typeof(InvalidOperationException))]
        public void CreateSecurePathFailure(string path, Type expectedExceptionType)
        {
            Action action = () => SecurePath.CreateSecurePath(path);
            action.Should().Throw<Exception>().Which.Should().BeOfType(expectedExceptionType);
        }
    }
}
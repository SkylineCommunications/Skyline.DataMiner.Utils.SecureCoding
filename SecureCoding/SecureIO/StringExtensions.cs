namespace Skyline.DataMiner.Utils.SecureCoding.SecureIO
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Skyline.DataMiner.CICD.FileSystem;

    /// <summary>
    /// Contains extensions specifically for strings that represent paths.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Checks whether the specified path is valid by ensuring it is not null, empty, or whitespace,
        /// and that it does not contain invalid characters for paths or filenames.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <returns>True if the path is valid; otherwise, false.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="path"/> is null, empty, or whitespace.
        /// </exception>
        /// <exception cref="NotSupportedException">Thrown if the current operating system is not supported.</exception>
        public static bool IsPathValid(this string path)
        {
            return path.IsPathValid(PlatformHelper.CurrentPlatform);
        }

        /// <summary>
        /// Checks whether the specified path is valid according to the path rules of the specified platform,
        /// by ensuring it is not null, empty, or whitespace, and that it does not contain invalid characters for paths or filenames.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <param name="platform">The platform whose path rules should be applied.</param>
        /// <returns>True if the path is valid; otherwise, false.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="path"/> is null, empty, or whitespace.
        /// </exception>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="platform"/> is not supported.</exception>
        public static bool IsPathValid(this string path, OSPlatform platform)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException($"'{nameof(path)}' cannot be null or whitespace.", nameof(path));
            }

            PlatformHelper.EnsureSupported(platform);

            var filename = path.Split(GetDirectorySeparator(platform)).LastOrDefault();

            if (!HasExtension(filename))
            {
                return !path.ContainsInvalidPathCharacters(platform);
            }

            return !path.ContainsInvalidPathCharacters(platform) && !filename.ContainsInvalidFilenameCharacters(platform);
        }

        internal static bool IsFile(this string path)
        {
            try
            {
                return FileSystem.Instance.Path.HasExtension(path);
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static bool ContainsInvalidPathCharacters(this string path, OSPlatform platform)
        {
            return path.IndexOfAny(GetInvalidPathChars(platform)) != -1
                   || path.IndexOf("..") != -1
                   || path.Count(c => c == '%') > 1;
        }

        internal static bool ContainsInvalidFilenameCharacters(this string filename, OSPlatform platform)
        {
            return filename.IndexOfAny(GetInvalidFileNameChars(platform)) != -1
                   || filename.Count(c => c == '%') > 1;
        }

        internal static char GetDirectorySeparator(OSPlatform platform)
        {
            return platform == OSPlatform.Windows ? '\\' : '/';
        }

        internal static StringComparison GetPathComparison(OSPlatform platform)
        {
            return platform == OSPlatform.Windows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        }

        private static bool HasExtension(string filename)
        {
            if (String.IsNullOrEmpty(filename))
            {
                return false;
            }

            var index = filename.LastIndexOf('.');
            return index >= 0 && index < filename.Length - 1;
        }

        private static char[] GetInvalidPathChars(OSPlatform platform)
        {
            PlatformHelper.EnsureSupported(platform);
            return platform == OSPlatform.Windows ? GetInvalidWindowsPathChars() : GetInvalidLinuxPathChars();
        }

        private static char[] GetInvalidFileNameChars(OSPlatform platform)
        {
            PlatformHelper.EnsureSupported(platform);
            return platform == OSPlatform.Windows ? GetInvalidWindowsFileNameChars() : GetInvalidLinuxFileNameChars();
        }

        private static char[] GetInvalidWindowsPathChars()
        {
            // This method replicates the behavior of FileSystem.Instance.Path.GetInvalidPathChars
            return new char[] {
                '\u0000', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\u0007', '\u0008', '\u0009',
                '\u000A', '\u000B', '\u000C', '\u000D', '\u000E', '\u000F', '\u0010', '\u0011', '\u0012', '\u0013',
                '\u0014', '\u0015', '\u0016', '\u0017', '\u0018', '\u0019', '\u001A', '\u001B', '\u001C', '\u001D',
                '\u001E', '\u001F', '\u0022', '\u002A', '\u002F', '\u003C', '\u003E', '\u003F', '\u007C',
            };
        }

        private static char[] GetInvalidWindowsFileNameChars()
        {
            // This method replicates the behavior of FileSystem.Instance.Path.GetInvalidFileNameChars
            return new char[] {
                '\u0000', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\u0007', '\u0008', '\u0009',
                '\u000A', '\u000B', '\u000C', '\u000D', '\u000E', '\u000F', '\u0010', '\u0011', '\u0012', '\u0013',
                '\u0014', '\u0015', '\u0016', '\u0017', '\u0018', '\u0019', '\u001A', '\u001B', '\u001C', '\u001D',
                '\u001E', '\u001F', '\u0022', '\u002A', '\u002F', '\u003A', '\u003C', '\u003E', '\u003F', '\u005C',
                '\u007C'
            };
        }

        private static char[] GetInvalidLinuxPathChars()
        {
            // This method replicates the behavior of System.IO.Path.GetInvalidPathChars on Linux
            return new char[] { '\u0000' };
        }

        private static char[] GetInvalidLinuxFileNameChars()
        {
            // This method replicates the behavior of System.IO.Path.GetInvalidFileNameChars on Linux
            return new char[] { '\u0000', '\u002F' };
        }
    }
}
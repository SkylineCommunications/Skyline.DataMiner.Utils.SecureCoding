namespace Skyline.DataMiner.Utils.SecureCoding.SecureIO
{
    using System;
    using System.IO;
    using System.Linq;
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
        public static bool IsPathValid(this string path)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException($"'{nameof(path)}' cannot be null or whitespace.", nameof(path));
            }

            try
            {
                // In .NET Framework it can already throw ArgumentException for invalid characters
                var filename = path.Split('\\').LastOrDefault();

                var extension = FileSystem.Instance.Path.GetExtension(filename);

                if (string.IsNullOrWhiteSpace(extension))
                {
                    return !path.ContainsInvalidPathCharacters();
                }

                return !path.ContainsInvalidPathCharacters() && !filename.ContainsInvalidFilenameCharacters();
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static bool ContainsInvalidPathCharacters(this string path)
        {
            return path.IndexOfAny(GetInvalidPathChars()) != -1
                   || path.IndexOf("..") != -1
                   || path.Count(c => c == '%') > 1;
        }

        internal static bool ContainsInvalidFilenameCharacters(this string filename)
        {
            return filename.IndexOfAny(GetInvalidFileNameChars()) != -1
                   || filename.Count(c => c == '%') > 1;
        }

        private static char[] GetInvalidPathChars()
        {
            // This method replicates the behavior of FileSystem.Instance.Path.GetInvalidPathChars
            return new char[] {
                '\u0000', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\u0007', '\u0008', '\u0009',
                '\u000A', '\u000B', '\u000C', '\u000D', '\u000E', '\u000F', '\u0010', '\u0011', '\u0012', '\u0013',
                '\u0014', '\u0015', '\u0016', '\u0017', '\u0018', '\u0019', '\u001A', '\u001B', '\u001C', '\u001D',
                '\u001E', '\u001F', '\u0022', '\u002A', '\u002F', '\u003C', '\u003E', '\u003F', '\u007C',
            };
        }

        private static char[] GetInvalidFileNameChars()
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
    }
}
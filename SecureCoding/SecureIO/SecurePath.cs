namespace Skyline.DataMiner.Utils.Security.SecureIO
{
    using System;
    using System.IO;
    using System.Linq;

    public static class SecurePath
    {
        /// <summary>
        /// Constructs a secure path by combining a base path and a filename, performing various validations to ensure the resulting path is secure. Note that is only secure as long as the base path cannot be manipulated.
        /// </summary>
        /// <param name="basePath">The base path to combine with the filename.</param>
        /// <param name="filename">The name of the file.</param>
        /// <returns>The full and validated secure path.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="basePath"/> or <paramref name="filename"/> is null, empty, or whitespace,
        /// or if the base path contains invalid characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the constructed full path is not a valid path or if it does not start with the specified base path.
        /// </exception>
        public static string ConstructSecurePath(string basePath, string filename)
        {
            if (string.IsNullOrWhiteSpace(basePath))
            {
                throw new ArgumentException($"'{nameof(basePath)}' cannot be null or whitespace.", nameof(basePath));
            }

            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException($"'{nameof(filename)}' cannot be null or whitespace.", nameof(filename));
            }

            if (basePath.ContainsInvalidPathCharacters())
            {
                throw new InvalidOperationException($"Base path '{basePath} contains invalid characters'");
            }

            if (filename.ContainsInvalidFilenameCharacters())
            {
                throw new InvalidOperationException($"Filename '{filename} contains invalid characters'");
            }

            var combinedPath = Path.Combine(basePath, filename);

            var fullPath = Path.GetFullPath(combinedPath);

            DirectoryTraversalValidation(false, basePath, fullPath);

            return fullPath;
        }

        /// <summary>
        /// Constructs a secure path by combining multiple path segments, performing various validations to ensure the resulting path is secure. Note that is only secure as long as the base path cannot be manipulated.
        /// </summary>
        /// <param name="paths">An array of path segments to combine, where the first position is considered the base path, and the last position is considered the filename, being all the segments in between considered as sub-directories relative to the base path.</param>
        /// <returns>The full and validated secure path.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="paths"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="paths"/> has a length less than 1, or if any path segment contains invalid characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the constructed full path is not a valid path or if it does not start with the specified base path.
        /// </exception>
        public static string ConstructSecurePath(params string[] paths)
        {
            if (paths.Length < 2)
            {
                throw new ArgumentException("Paths array should at least contain the base path and the filename");
            }

            var basePath = paths[0];

            if (basePath.ContainsInvalidPathCharacters())
            {
                throw new InvalidOperationException($"Base path '{basePath} contains invalid characters'");
            }

            for(int i = 1; i < paths.Length-1; i++)
            {
                if (paths[i].ContainsInvalidPathCharacters())
                {
                    throw new InvalidOperationException($"Path segment '{paths[i]} contains invalid characters'");
                }
            }

            var filename = paths[paths.Length - 1];
            if (filename.ContainsInvalidFilenameCharacters())
            {
                throw new InvalidOperationException($"Filename '{filename} contains invalid characters'");
            }

            var combinedPath = Path.Combine(paths);

            var fullPath = Path.GetFullPath(combinedPath);

            DirectoryTraversalValidation(allowSubDirectories: true, basePath, fullPath);

            return fullPath;
        }

        /// <summary>
        /// Constructs a secure path by combining a base path and a filename, performing various validations to ensure the resulting path is secure. Note that is only secure as long as the base path cannot be manipulated.
        /// </summary>
        /// <param name="basePath">The base path to combine with the filename.</param>
        /// <param name="relativePath">The path relative to the base path which may contain a filename.</param>
        /// <returns>The full and validated secure path.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="basePath"/> or <paramref name="relativePath"/> is null, empty, or whitespace,
        /// or if the base path contains invalid characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the constructed full path is not a valid path or if it does not start with the specified base path.
        /// </exception>
        public static string ConstructSecurePathWithSubDirectories(string basePath, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(basePath))
            {
                throw new ArgumentException($"'{nameof(basePath)}' cannot be null or whitespace.", nameof(basePath));
            }

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException($"'{nameof(relativePath)}' cannot be null or whitespace.", nameof(relativePath));
            }

            if (basePath.ContainsInvalidPathCharacters())
            {
                throw new InvalidOperationException($"Base path '{basePath} contains invalid characters'");
            }

            if (!relativePath.IsPathValid())
            {
                throw new InvalidOperationException($"Relative path '{relativePath} contains invalid characters'");
            }  

            var combinedPath = Path.Combine(basePath, relativePath);

            var fullPath = Path.GetFullPath(combinedPath);

            DirectoryTraversalValidation(true, basePath, fullPath);

            return fullPath;
        }

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
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException($"'{nameof(path)}' cannot be null or whitespace.", nameof(path));
            }

            var filename = Path.GetFileName(path);
            
            return !ContainsInvalidPathCharacters(path) && !ContainsInvalidFilenameCharacters(filename);
        }

        private static bool ContainsInvalidPathCharacters(this string path)
        {
            return path.IndexOfAny(GetInvalidPathChars()) != -1
                || path.IndexOf("..") != -1
                || path.Count(c => c == '%') > 1;
        }

        private static bool ContainsInvalidFilenameCharacters(this string filename)
        {
            return filename.IndexOfAny(GetInvalidFileNameChars()) != -1
            || filename.Count(c => c == '%') > 1;
        }

        private static void DirectoryTraversalValidation(bool allowSubDirectories, string basePath, string fullPath)
        {
            if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Invalid path '{fullPath}'");
            }

            if (!basePath.Equals(Path.GetPathRoot(basePath), StringComparison.OrdinalIgnoreCase))
            {
                basePath = basePath.TrimEnd('\\');
            }

            if (!allowSubDirectories && !Path.GetDirectoryName(fullPath).Equals(basePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Sub-directories flag should be set to true in order to construct a path with sub-directories in filename");
            }
        }

        private static char[] GetInvalidPathChars()
        {
            // This method replicates the behavior of Path.GetInvalidPathChars
            return new char[] {
                '\u0000', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\u0007', '\u0008', '\u0009',
                '\u000A', '\u000B', '\u000C', '\u000D', '\u000E', '\u000F', '\u0010', '\u0011', '\u0012', '\u0013',
                '\u0014', '\u0015', '\u0016', '\u0017', '\u0018', '\u0019', '\u001A', '\u001B', '\u001C', '\u001D',
                '\u001E', '\u001F', '\u0022', '\u002A', '\u003C', '\u003E', '\u003F', '\u007C'
            };
        }
        private static char[] GetInvalidFileNameChars()
        {         
            // This method replicates the behavior of Path.GetInvalidFileNameChars
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
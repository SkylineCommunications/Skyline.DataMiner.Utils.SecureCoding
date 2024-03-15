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
        /// <param name="filename">The filename to append to the base path.</param>
        /// <param name="allowSubDirectories">Boolean indicating whether the sub-directories of the base path are allowed in the filename argumnet. (Default = <see langword="false")/></param>
        /// <returns>The full and validated secure path.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="basePath"/> or <paramref name="filename"/> is null, empty, or whitespace,
        /// or if the base path contains invalid characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the constructed full path is not a valid path or if it does not start with the specified base path.
        /// </exception>
        public static string ConstructSecurePath(string basePath, string filename, bool allowSubDirectories = false)
        {
            if (string.IsNullOrWhiteSpace(basePath))
            {
                throw new ArgumentException($"'{nameof(basePath)}' cannot be null or whitespace.", nameof(basePath));
            }

            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException($"'{nameof(filename)}' cannot be null or whitespace.", nameof(filename));
            }

            if (!basePath.IsPathValid())
            {
                throw new InvalidOperationException($"Base path '{basePath} contains invalid characters'");
            }

            var combinedPath = Path.Combine(basePath, filename);

            var fullPath = Path.GetFullPath(combinedPath);

            InnerPathValidation(allowSubDirectories, basePath, fullPath);

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
            if (paths is null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            if (paths.Length < 2)
            {
                throw new ArgumentException("Paths array should at least contain the base path and the filename");
            }

            var basePath = paths[0];

            if (!basePath.IsPathValid())
            {
                throw new InvalidOperationException($"Base path '{basePath} contains invalid characters'");
            }

            var combinedPath = Path.Combine(paths);

            var fullPath = Path.GetFullPath(combinedPath);

            InnerPathValidation(allowSubDirectories: true, basePath, fullPath);

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

            if (filename.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                return false;
            }

            if (path.IndexOfAny(Path.GetInvalidPathChars()) != -1
                || path.IndexOf('\0') != -1
                || path.IndexOf('/') != -1
                || path.IndexOf("..") != -1
                || path.Count(c => c == '%') > 1)
            {
                return false;
            }

            return true;
        }

        private static void InnerPathValidation(bool allowSubDirectories, string basePath, string fullPath)
        {
            if (!fullPath.IsPathValid())
            {
                throw new InvalidOperationException($"Path '{fullPath} contains invalid characters'");
            }

            if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Invalid path '{fullPath}'");
            }

            if(!basePath.Equals(Path.GetPathRoot(basePath), StringComparison.OrdinalIgnoreCase))
            {
                basePath = basePath.TrimEnd('\\');
            }

            if (!allowSubDirectories && !Path.GetDirectoryName(fullPath).Equals(basePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Sub-directories flag should be set to true in order to construct a path with sub-directories in filename");
            }
        }
    }
}
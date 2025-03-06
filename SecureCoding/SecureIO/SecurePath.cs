namespace Skyline.DataMiner.Utils.SecureCoding.SecureIO
{
    using System;
    using Skyline.DataMiner.CICD.FileSystem;

    /// <summary>
    /// Represents a path that is secure.
    /// </summary>
    public class SecurePath
    {
        private readonly string path;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurePath"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        private SecurePath(string path)
        {
            this.path = path;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="SecurePath"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="securePath">The safe path.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator string(SecurePath securePath) => securePath.path;

        /// <summary>
        /// Performs an explicit conversion from <see cref="System.String"/> to <see cref="SecurePath"/>.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static explicit operator SecurePath(string path) => CreateSecurePath(path);

        /// <summary>
        /// Creates the secure path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">path</exception>
        /// <exception cref="System.InvalidOperationException">FileSystem.Instance.Path '{path}' is insecure!</exception>
        public static SecurePath CreateSecurePath(string path)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!path.IsPathValid())
            {
                throw new InvalidOperationException($"FileSystem.Instance.Path '{path}' is insecure!");
            }

            return new SecurePath(path);
        }

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
        public static SecurePath ConstructSecurePath(string basePath, string filename)
        {
            if (String.IsNullOrWhiteSpace(basePath))
            {
                throw new ArgumentException($"'{nameof(basePath)}' cannot be null or whitespace.", nameof(basePath));
            }

            if (String.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException($"'{nameof(filename)}' cannot be null or whitespace.", nameof(filename));
            }

            if (basePath.ContainsInvalidPathCharacters())
            {
                throw new InvalidOperationException($"Base path '{basePath}' contains invalid characters");
            }

            if (filename.ContainsInvalidFilenameCharacters())
            {
                throw new InvalidOperationException($"Filename '{filename}' contains invalid characters");
            }

            var combinedPath = FileSystem.Instance.Path.Combine(basePath, filename);

            var fullPath = FileSystem.Instance.Path.GetFullPath(combinedPath);

            DirectoryTraversalValidation(false, basePath, fullPath);

            return new SecurePath(fullPath);
        }

        /// <summary>
        /// Constructs a secure path by combining multiple path segments, performing various validations to ensure the resulting path is secure. Note that is only secure as long as the base path cannot be manipulated.
        /// </summary>
        /// <param name="paths">An array of path segments to combine, where the first position is considered the base path, and the last position is considered the filename, being all the segments in between considered as sub-directories relative to the base path.</param>
        /// <returns>The full and validated secure path.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="paths"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="paths"/> has a length less than 1, or if any path segment contains invalid characters.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the constructed full path is not a valid path, if it does not start with the specified base path,
        /// when any path segment contains invalid characters or when the base path is a rooted path.
        /// </exception>
        public static SecurePath ConstructSecurePath(params string[] paths)
        {
            if (paths.Length < 2)
            {
                throw new ArgumentException("Paths array should contain at least 2 path segments");
            }

            var basePath = paths[0];

            if (basePath.ContainsInvalidPathCharacters())
            {
                throw new InvalidOperationException($"Base path '{basePath}' contains invalid characters");
            }

            for (int i = 1; i < paths.Length - 1; i++)
            {
                PathSegmentValidation(paths[i]);
            }

            var lastPathSegment = paths[paths.Length - 1];
            if (lastPathSegment.IsFile())
            {
                if (lastPathSegment.ContainsInvalidFilenameCharacters())
                {
                    throw new InvalidOperationException($"Filename '{lastPathSegment}' contains invalid characters");
                }
            }
            else
            {
                PathSegmentValidation(lastPathSegment);
            }

            var combinedPath = FileSystem.Instance.Path.Combine(paths);

            var fullPath = FileSystem.Instance.Path.GetFullPath(combinedPath);

            DirectoryTraversalValidation(allowSubDirectories: true, basePath, fullPath);

            return new SecurePath(fullPath);
        }

        /// <summary>
        /// Constructs a secure path by combining a base path and a filename, performing various validations to ensure the resulting path is secure. Note that is only secure as long as the base path cannot be manipulated.
        /// </summary>
        /// <param name="basePath">The base path to combine with the filename.</param>
        /// <param name="relativePath">The path relative to the base path which may contain a filename.</param>
        /// <returns>The full and validated secure path.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="basePath"/> or <paramref name="relativePath"/> is null, empty, or whitespace,
        /// or if the base path contains invalid characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown if the constructed full path is not a valid path or if it does not start with the specified base path, 
        /// when the base path contains invalid characters, when the relative path is rooted or invalid, or when the relative path contains invalid characters.
        /// </exception>
        public static SecurePath ConstructSecurePathWithSubDirectories(string basePath, string relativePath)
        {
            if (String.IsNullOrWhiteSpace(basePath))
            {
                throw new ArgumentException($"'{nameof(basePath)}' cannot be null or whitespace.", nameof(basePath));
            }

            if (String.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException($"'{nameof(relativePath)}' cannot be null or whitespace.", nameof(relativePath));
            }

            if (basePath.ContainsInvalidPathCharacters())
            {
                throw new InvalidOperationException($"Base path '{basePath}' contains invalid characters");
            }

            if (!relativePath.IsPathValid())
            {
                throw new InvalidOperationException($"Relative path '{relativePath}' contains invalid characters");
            }

            if (FileSystem.Instance.Path.IsPathRooted(relativePath))
            {
                throw new InvalidOperationException($"Relative path '{relativePath}' cannot be a rooted path");
            }

            var combinedPath = FileSystem.Instance.Path.Combine(basePath, relativePath);

            var fullPath = FileSystem.Instance.Path.GetFullPath(combinedPath);

            DirectoryTraversalValidation(true, basePath, fullPath);

            return new SecurePath(fullPath);
        }

        /// <summary>
        /// Converts to string. Returns the path.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return path;
        }

        private static void DirectoryTraversalValidation(bool allowSubDirectories, string basePath, string fullPath)
        {
            if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Invalid path '{fullPath}'");
            }

            if (!basePath.Equals(FileSystem.Instance.Path.GetPathRoot(basePath), StringComparison.OrdinalIgnoreCase))
            {
                basePath = basePath.TrimEnd('\\');
            }

            if (!allowSubDirectories && !FileSystem.Instance.Path.GetDirectoryName(fullPath).Equals(basePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Sub-directories flag should be set to true in order to construct a path with sub-directories in filename");
            }
        }

        private static void PathSegmentValidation(string pathSegment)
        {
            if (pathSegment.ContainsInvalidPathCharacters())
            {
                throw new InvalidOperationException($"FileSystem.Instance.Path segment '{pathSegment}' contains invalid characters");
            }

            if (FileSystem.Instance.Path.IsPathRooted(pathSegment))
            {
                throw new InvalidOperationException($"FileSystem.Instance.Path segment '{pathSegment}' cannot be a rooted path");
            }
        }
    }
}
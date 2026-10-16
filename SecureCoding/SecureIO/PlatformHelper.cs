namespace Skyline.DataMiner.Utils.SecureCoding.SecureIO
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Resolves the platform whose path rules apply to the current process.
    /// </summary>
    internal static class PlatformHelper
    {
        /// <summary>
        /// Gets the platform of the current operating system.
        /// </summary>
        /// <exception cref="NotSupportedException">Thrown if the current operating system is not supported.</exception>
        internal static OSPlatform CurrentPlatform
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return OSPlatform.Windows;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return OSPlatform.Linux;
                }

                throw new NotSupportedException("The current operating system is not supported.");
            }
        }

        /// <summary>
        /// Ensures the specified platform is one for which path rules are defined.
        /// </summary>
        /// <exception cref="NotSupportedException">Thrown if the platform is not supported.</exception>
        internal static void EnsureSupported(OSPlatform platform)
        {
            if (platform != OSPlatform.Windows && platform != OSPlatform.Linux)
            {
                throw new NotSupportedException($"The platform '{platform}' is not supported.");
            }
        }
    }
}

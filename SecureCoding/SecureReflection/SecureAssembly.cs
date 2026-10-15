namespace Skyline.DataMiner.Utils.SecureCoding.SecureReflection
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using Skyline.DataMiner.Utils.SecureCoding.SecureIO;

    /// <summary>
    /// Provides methods to securely work with assemblies.
    /// </summary>
    /// <remarks>
    /// Linux validates and loads the same bounded byte snapshot. Both loader families use
    /// <see cref="Assembly.Load(byte[])"/> on Linux: Location is empty and path-based dependency resolution is not preserved.
    /// Linux chain verification fails closed when revocation cannot be established; RFC 3161 trust requires the net8.0 asset.
    /// Disabling chain verification still checks the signature, PE digest, signer policy and pinned certificate, but not dates or revocation.
    /// Windows retains WinVerifyTrust and path-loading behavior; callers must protect the file and parent directories from modification.
    /// Dependencies and native libraries are not validated. See SecureReflection/SECURITY.md for the supported security profile.
    /// </remarks>
    public class SecureAssembly
    {
        /// <summary>
        /// Loads an assembly from the specified path after verifying its digital signature against the given allowed certificates.
        /// </summary>
        /// <param name="assemblyPath">The file path to the assembly to be loaded.</param>
        /// <param name="allowedCertificate"><see cref="X509Certificate2"/> representing the allowed certificates for the assembly.</param>
        /// <param name="verifyCertificateChain">
        /// If <see langword="true"/> (default), performs full certificate chain verification;  
        /// if <see langword="false"/>, only the file hash is checked, bypassing certificate chain validation.
        /// </param>
        /// <returns>The loaded <see cref="Assembly"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="assemblyPath"/> is null or whitespace.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="allowedCertificate"/> is null.</exception>
        /// <exception cref="SecurityException">Thrown if the assembly's certificate does not match any of the allowed certificates, or if the assembly is unsigned or has an invalid signature.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the assembly file cannot be found at the specified path.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the assembly path contains invalid characters.</exception>
        /// <exception cref="NotSupportedException">Thrown if the current operating system is not supported.</exception>
        public static Assembly LoadFrom(string assemblyPath, X509Certificate2 allowedCertificate, bool verifyCertificateChain = true)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath))
            {
                throw new ArgumentException($"'{nameof(assemblyPath)}' cannot be null or whitespace.", nameof(assemblyPath));
            }

            if (allowedCertificate is null)
            {
                throw new ArgumentNullException(nameof(allowedCertificate));
            }

            return ValidateAndLoad(assemblyPath, nameof(Assembly.LoadFrom), verifyCertificateChain, allowedCertificate);
        }

        /// <summary>
        /// Loads an assembly from the specified path after verifying its digital signature against the given allowed certificates.
        /// </summary>
        /// <param name="assemblyPath">The file path to the assembly to be loaded.</param>
        /// <param name="allowedCertificates">An array of <see cref="X509Certificate2"/> representing the allowed certificates for the assembly.</param>
        /// <param name="verifyCertificateChain">
        /// If <see langword="true"/> (default), performs full certificate chain verification;  
        /// if <see langword="false"/>, only the file hash is checked, bypassing certificate chain validation.
        /// </param>
        /// <returns>The loaded <see cref="Assembly"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="assemblyPath"/> is null or whitespace.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="allowedCertificates"/> is null.</exception>
        /// <exception cref="SecurityException">Thrown if the assembly's certificate does not match any of the allowed certificates, or if the assembly is unsigned or has an invalid signature.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the assembly file cannot be found at the specified path.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the assembly path contains invalid characters.</exception>
        /// <exception cref="NotSupportedException">Thrown if the current operating system is not supported.</exception>
        public static Assembly LoadFrom(string assemblyPath, IEnumerable<X509Certificate2> allowedCertificates, bool verifyCertificateChain = true)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath))
            {
                throw new ArgumentException($"'{nameof(assemblyPath)}' cannot be null or whitespace.", nameof(assemblyPath));
            }

            if (allowedCertificates is null)
            {
                throw new ArgumentNullException(nameof(allowedCertificates));
            }

            var allowedCerticatesArr = allowedCertificates.ToArray();

            if (allowedCerticatesArr.Length < 1 || allowedCerticatesArr.All(certificate => certificate is null))
            {
                throw new ArgumentException($"{nameof(allowedCertificates)} must contain at least one certificate.");
            }

            return ValidateAndLoad(assemblyPath, nameof(Assembly.LoadFrom), verifyCertificateChain, allowedCerticatesArr);
        }

        /// <summary>
        /// Loads an assembly from the specified path after verifying its digital signature against the given allowed certificate paths.
        /// </summary>
        /// <param name="assemblyPath">The file path to the assembly to be loaded.</param>
        /// <param name="allowedCertificatePath">Certificate path representing the allowed certificate for the assembly.</param>
        /// <param name="verifyCertificateChain">
        /// If <see langword="true"/> (default), performs full certificate chain verification;  
        /// if <see langword="false"/>, only the file hash is checked, bypassing certificate chain validation.
        /// </param>
        /// <returns>The loaded <see cref="Assembly"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="assemblyPath"/> is null or whitespace, or if <paramref name="allowedCertificatePath"/> is empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="allowedCertificatePath"/> is null.</exception>
        /// <exception cref="SecurityException">Thrown if the assembly's certificate does not match any of the allowed certificates, or if the assembly is unsigned or has an invalid signature.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the assembly file cannot be found at the specified path.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the assembly path contains invalid characters.</exception>
        /// <exception cref="NotSupportedException">Thrown if the current operating system is not supported.</exception>
        public static Assembly LoadFrom(string assemblyPath, string allowedCertificatePath, bool verifyCertificateChain = true)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath))
            {
                throw new ArgumentException($"'{nameof(assemblyPath)}' cannot be null or whitespace.", nameof(assemblyPath));
            }

            if (allowedCertificatePath is null)
            {
                throw new ArgumentNullException(nameof(allowedCertificatePath));
            }

            if (allowedCertificatePath.EndsWith(".pfx") || allowedCertificatePath.EndsWith(".p12"))
            {
                throw new ArgumentException(
                   $"{nameof(allowedCertificatePath)} contains one or more '.pfx' or '.p12' files, which store private keys. " +
                   "To securely load these certificates, please use an overload that accepts an X509Certificate2 instance instead.");
            }

            using (var certificate = new X509Certificate2(allowedCertificatePath))
            {
                return ValidateAndLoad(assemblyPath, nameof(Assembly.LoadFrom), verifyCertificateChain, certificate);
            }
        }

        /// <summary>
        /// Loads an assembly from the specified path after verifying its digital signature against the given allowed certificate paths.
        /// </summary>
        /// <param name="assemblyPath">The file path to the assembly to be loaded.</param>
        /// <param name="allowedCertificatePaths">An array of certificate paths representing the allowed certificates for the assembly.</param>
        /// <param name="verifyCertificateChain">
        /// If <see langword="true"/> (default), performs full certificate chain verification;  
        /// if <see langword="false"/>, only the file hash is checked, bypassing certificate chain validation.
        /// </param>
        /// <returns>The loaded <see cref="Assembly"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="assemblyPath"/> is null or whitespace, or if <paramref name="allowedCertificatePaths"/> is empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="allowedCertificatePaths"/> is null.</exception>
        /// <exception cref="SecurityException">Thrown if the assembly's certificate does not match any of the allowed certificates, or if the assembly is unsigned or has an invalid signature.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the assembly file cannot be found at the specified path.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the assembly path contains invalid characters.</exception>
        /// <exception cref="NotSupportedException">Thrown if the current operating system is not supported.</exception>
        public static Assembly LoadFrom(string assemblyPath, IEnumerable<string> allowedCertificatePaths, bool verifyCertificateChain = true)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath))
            {
                throw new ArgumentException($"'{nameof(assemblyPath)}' cannot be null or whitespace.", nameof(assemblyPath));
            }

            if (allowedCertificatePaths is null)
            {
                throw new ArgumentNullException(nameof(allowedCertificatePaths));
            }

            if (!allowedCertificatePaths.Any() || allowedCertificatePaths.All(path => string.IsNullOrWhiteSpace(path)))
            {
                throw new ArgumentException($"{nameof(allowedCertificatePaths)} must contain at least one certificate.");
            }

            if (allowedCertificatePaths.Any(path => path.EndsWith(".pfx") || path.EndsWith(".p12")))
            {
                throw new ArgumentException(
                   $"{nameof(allowedCertificatePaths)} contains one or more '.pfx' or '.p12' files, which store private keys. " +
                   "To securely load these certificates, please use an overload that accepts an X509Certificate2 instance instead.");
            }

            var certificates = allowedCertificatePaths
                .Select(certificatePath => new X509Certificate2(certificatePath))
                .ToArray();

            try
            {
                return ValidateAndLoad(assemblyPath, nameof(Assembly.LoadFrom), verifyCertificateChain, certificates);
            }
            finally
            {
                foreach (var certificate in certificates)
                {
                    certificate.Dispose();
                }
            }
        }

        /// <summary>
        /// Loads an assembly from the specified path after verifying its digital signature against the given allowed certificate byte array.
        /// </summary>
        /// <remarks>
        /// If the <paramref name="allowedCertificate"/> were createad from '.pfx' or 'p12' file, the overload that accepts an X509Certificate2 instance should be used instead.
        /// </remarks>
        /// <param name="assemblyPath">The file path to the assembly to be loaded.</param>
        /// <param name="allowedCertificate">Byte array representing the allowed certificate for the assembly.</param>
        /// <param name="verifyCertificateChain">
        /// If <see langword="true"/> (default), performs full certificate chain verification;  
        /// if <see langword="false"/>, only the file hash is checked, bypassing certificate chain validation.
        /// </param>
        /// <returns>The loaded <see cref="Assembly"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="assemblyPath"/> is null or whitespace, or if <paramref name="allowedCertificate"/> is empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="allowedCertificate"/> is null.</exception>
        /// <exception cref="SecurityException">Thrown if the assembly's certificate does not match any of the allowed certificates, or if the assembly is unsigned or has an invalid signature.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the assembly file cannot be found at the specified path.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the assembly path contains invalid characters.</exception>
        /// <exception cref="NotSupportedException">Thrown if the current operating system is not supported.</exception>
        public static Assembly LoadFrom(string assemblyPath, byte[] allowedCertificate, bool verifyCertificateChain = true)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath))
            {
                throw new ArgumentException($"'{nameof(assemblyPath)}' cannot be null or whitespace.", nameof(assemblyPath));
            }

            if (allowedCertificate is null)
            {
                throw new ArgumentNullException(nameof(allowedCertificate));
            }

            if (allowedCertificate.Length < 1)
            {
                throw new ArgumentException($"{nameof(allowedCertificate)} cannot be empty.");
            }

            using (var certificate = new X509Certificate2(allowedCertificate))
            {
                return ValidateAndLoad(assemblyPath, nameof(Assembly.LoadFrom), verifyCertificateChain, certificate);
            }
        }

        /// <summary>
        /// Loads an assembly from the specified path after verifying its digital signature against the given allowed certificates byte arrays.
        /// </summary>
        /// <remarks>
        /// If the <paramref name="allowedCertificates"/> were createad from '.pfx' or 'p12' files, the overload that accepts an X509Certificate2 instance should be used instead.
        /// </remarks>
        /// <param name="assemblyPath">The file path to the assembly to be loaded.</param>
        /// <param name="allowedCertificates">An enumerable of byte arrays representing the allowed certificates for the assembly.</param>
        /// <param name="verifyCertificateChain">
        /// If <see langword="true"/> (default), performs full certificate chain verification;  
        /// if <see langword="false"/>, only the file hash is checked, bypassing certificate chain validation.
        /// </param>
        /// <returns>The loaded <see cref="Assembly"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="assemblyPath"/> is null or whitespace, or if <paramref name="allowedCertificates"/> is empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="allowedCertificates"/> is null.</exception>
        /// <exception cref="SecurityException">Thrown if the assembly's certificate does not match any of the allowed certificates, or if the assembly is unsigned or has an invalid signature.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the assembly file cannot be found at the specified path.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the assembly path contains invalid characters.</exception>
        /// <exception cref="NotSupportedException">Thrown if the current operating system is not supported.</exception>
        public static Assembly LoadFrom(string assemblyPath, IEnumerable<byte[]> allowedCertificates, bool verifyCertificateChain = true)
        {
            if (String.IsNullOrWhiteSpace(assemblyPath))
            {
                throw new ArgumentException($"'{nameof(assemblyPath)}' cannot be null or whitespace.", nameof(assemblyPath));
            }

            if (allowedCertificates is null)
            {
                throw new ArgumentNullException(nameof(allowedCertificates));
            }

            if (!allowedCertificates.Any() || allowedCertificates.All(certificate => certificate.Length < 1))
            {
                throw new ArgumentException($"{nameof(allowedCertificates)} must contain at least one certificate.");
            }

            var certificates = allowedCertificates
                .Select(certificate => new X509Certificate2(certificate))
                .ToArray();

            try
            {
                return ValidateAndLoad(assemblyPath, nameof(Assembly.LoadFrom), verifyCertificateChain, certificates);
            }
            finally
            {
                foreach (var certificate in certificates)
                {
                    certificate.Dispose();
                }
            }
        }

        /// <summary>
        /// Loads an assembly from the specified path after verifying its digital signature against the given allowed certificates.
        /// </summary>
        /// <param name="assemblyPath">The file path to the assembly to be loaded.</param>
        /// <param name="allowedCertificate"><see cref="X509Certificate2"/> representing the allowed certificates for the assembly.</param>
        /// <param name="verifyCertificateChain">
        /// If <see langword="true"/> (default), performs full certificate chain verification;  
        /// if <see langword="false"/>, only the file hash is checked, bypassing certificate chain validation.
        /// </param>
        /// <returns>The loaded <see cref="Assembly"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="assemblyPath"/> is null or whitespace.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="allowedCertificate"/> is null.</exception>
        /// <exception cref="SecurityException">Thrown if the assembly's certificate does not match any of the allowed certificates, or if the assembly is unsigned or has an invalid signature.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the assembly file cannot be found at the specified path.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the assembly path contains invalid characters.</exception>
        /// <exception cref="NotSupportedException">Thrown if the current operating system is not supported.</exception>
        public static Assembly LoadFile(string assemblyPath, X509Certificate2 allowedCertificate, bool verifyCertificateChain = true)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath))
            {
                throw new ArgumentException($"'{nameof(assemblyPath)}' cannot be null or whitespace.", nameof(assemblyPath));
            }

            if (allowedCertificate is null)
            {
                throw new ArgumentNullException(nameof(allowedCertificate));
            }

            return ValidateAndLoad(assemblyPath, nameof(Assembly.LoadFile), verifyCertificateChain, allowedCertificate);
        }

        /// <summary>
        /// Loads an assembly from the specified path after verifying its digital signature against the given allowed certificates.
        /// </summary>
        /// <param name="assemblyPath">The file path to the assembly to be loaded.</param>
        /// <param name="allowedCertificates">An array of <see cref="X509Certificate2"/> representing the allowed certificates for the assembly.</param>
        /// <param name="verifyCertificateChain">
        /// If <see langword="true"/> (default), performs full certificate chain verification;  
        /// if <see langword="false"/>, only the file hash is checked, bypassing certificate chain validation.
        /// </param>
        /// <returns>The loaded <see cref="Assembly"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="assemblyPath"/> is null or whitespace.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="allowedCertificates"/> is null.</exception>
        /// <exception cref="SecurityException">Thrown if the assembly's certificate does not match any of the allowed certificates, or if the assembly is unsigned or has an invalid signature.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the assembly file cannot be found at the specified path.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the assembly path contains invalid characters.</exception>
        /// <exception cref="NotSupportedException">Thrown if the current operating system is not supported.</exception>
        public static Assembly LoadFile(string assemblyPath, IEnumerable<X509Certificate2> allowedCertificates, bool verifyCertificateChain = true)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath))
            {
                throw new ArgumentException($"'{nameof(assemblyPath)}' cannot be null or whitespace.", nameof(assemblyPath));
            }

            if (allowedCertificates is null)
            {
                throw new ArgumentNullException(nameof(allowedCertificates));
            }

            var allowedCerticatesArr = allowedCertificates.ToArray();

            if (allowedCerticatesArr.Length < 1 || allowedCerticatesArr.All(certificate => certificate is null))
            {
                throw new ArgumentException($"{nameof(allowedCertificates)} must contain at least one certificate.");
            }

            return ValidateAndLoad(assemblyPath, nameof(Assembly.LoadFile), verifyCertificateChain, allowedCerticatesArr);
        }

        /// <summary>
        /// Loads an assembly from the specified path after verifying its digital signature against the given allowed certificate paths.
        /// </summary>
        /// <param name="assemblyPath">The file path to the assembly to be loaded.</param>
        /// <param name="allowedCertificatePath">Certificate path representing the allowed certificate for the assembly.</param>
        /// <param name="verifyCertificateChain">
        /// If <see langword="true"/> (default), performs full certificate chain verification;  
        /// if <see langword="false"/>, only the file hash is checked, bypassing certificate chain validation.
        /// </param>
        /// <returns>The loaded <see cref="Assembly"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="assemblyPath"/> is null or whitespace, or if <paramref name="allowedCertificatePath"/> is empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="allowedCertificatePath"/> is null.</exception>
        /// <exception cref="SecurityException">Thrown if the assembly's certificate does not match any of the allowed certificates, or if the assembly is unsigned or has an invalid signature.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the assembly file cannot be found at the specified path.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the assembly path contains invalid characters.</exception>
        /// <exception cref="NotSupportedException">Thrown if the current operating system is not supported.</exception>
        public static Assembly LoadFile(string assemblyPath, string allowedCertificatePath, bool verifyCertificateChain = true)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath))
            {
                throw new ArgumentException($"'{nameof(assemblyPath)}' cannot be null or whitespace.", nameof(assemblyPath));
            }

            if (allowedCertificatePath is null)
            {
                throw new ArgumentNullException(nameof(allowedCertificatePath));
            }

            if (allowedCertificatePath.EndsWith(".pfx") || allowedCertificatePath.EndsWith(".p12"))
            {
                throw new ArgumentException(
                   $"{nameof(allowedCertificatePath)} contains one or more '.pfx' or '.p12' files, which store private keys. " +
                   "To securely load these certificates, please use an overload that accepts an X509Certificate2 instance instead.");
            }

            using (var certificate = new X509Certificate2(allowedCertificatePath))
            {
                return ValidateAndLoad(assemblyPath, nameof(Assembly.LoadFile), verifyCertificateChain, certificate);
            }
        }

        /// <summary>
        /// Loads an assembly from the specified path after verifying its digital signature against the given allowed certificate paths.
        /// </summary>
        /// <param name="assemblyPath">The file path to the assembly to be loaded.</param>
        /// <param name="allowedCertificatePaths">An array of certificate paths representing the allowed certificates for the assembly.</param>
        /// <param name="verifyCertificateChain">
        /// If <see langword="true"/> (default), performs full certificate chain verification;  
        /// if <see langword="false"/>, only the file hash is checked, bypassing certificate chain validation.
        /// </param>
        /// <returns>The loaded <see cref="Assembly"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="assemblyPath"/> is null or whitespace, or if <paramref name="allowedCertificatePaths"/> is empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="allowedCertificatePaths"/> is null.</exception>
        /// <exception cref="SecurityException">Thrown if the assembly's certificate does not match any of the allowed certificates, or if the assembly is unsigned or has an invalid signature.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the assembly file cannot be found at the specified path.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the assembly path contains invalid characters.</exception>
        /// <exception cref="NotSupportedException">Thrown if the current operating system is not supported.</exception>
        public static Assembly LoadFile(string assemblyPath, IEnumerable<string> allowedCertificatePaths, bool verifyCertificateChain = true)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath))
            {
                throw new ArgumentException($"'{nameof(assemblyPath)}' cannot be null or whitespace.", nameof(assemblyPath));
            }

            if (allowedCertificatePaths is null)
            {
                throw new ArgumentNullException(nameof(allowedCertificatePaths));
            }

            if (!allowedCertificatePaths.Any() || allowedCertificatePaths.All(path => string.IsNullOrWhiteSpace(path)))
            {
                throw new ArgumentException($"{nameof(allowedCertificatePaths)} must contain at least one certificate.");
            }

            if (allowedCertificatePaths.Any(path => path.EndsWith(".pfx") || path.EndsWith(".p12")))
            {
                throw new ArgumentException(
                   $"{nameof(allowedCertificatePaths)} contains one or more '.pfx' or '.p12' files, which store private keys. " +
                   "To securely load these certificates, please use an overload that accepts an X509Certificate2 instance instead.");
            }

            var certificates = allowedCertificatePaths
                .Select(certificatePath => new X509Certificate2(certificatePath))
                .ToArray();

            try
            {
                return ValidateAndLoad(assemblyPath, nameof(Assembly.LoadFile), verifyCertificateChain, certificates);
            }
            finally
            {
                foreach (var certificate in certificates)
                {
                    certificate.Dispose();
                }
            }
        }

        /// <summary>
        /// Loads an assembly from the specified path after verifying its digital signature against the given allowed certificate byte array.
        /// </summary>
        /// <remarks>
        /// If the <paramref name="allowedCertificate"/> were createad from '.pfx' or 'p12' file, the overload that accepts an X509Certificate2 instance should be used instead.
        /// </remarks>
        /// <param name="assemblyPath">The file path to the assembly to be loaded.</param>
        /// <param name="allowedCertificate">Byte array representing the allowed certificate for the assembly.</param>
        /// <param name="verifyCertificateChain">
        /// If <see langword="true"/> (default), performs full certificate chain verification;  
        /// if <see langword="false"/>, only the file hash is checked, bypassing certificate chain validation.
        /// </param>
        /// <returns>The loaded <see cref="Assembly"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="assemblyPath"/> is null or whitespace, or if <paramref name="allowedCertificate"/> is empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="allowedCertificate"/> is null.</exception>
        /// <exception cref="SecurityException">Thrown if the assembly's certificate does not match any of the allowed certificates, or if the assembly is unsigned or has an invalid signature.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the assembly file cannot be found at the specified path.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the assembly path contains invalid characters.</exception>
        /// <exception cref="NotSupportedException">Thrown if the current operating system is not supported.</exception>
        public static Assembly LoadFile(string assemblyPath, byte[] allowedCertificate, bool verifyCertificateChain = true)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath))
            {
                throw new ArgumentException($"'{nameof(assemblyPath)}' cannot be null or whitespace.", nameof(assemblyPath));
            }

            if (allowedCertificate is null)
            {
                throw new ArgumentNullException(nameof(allowedCertificate));
            }

            if (allowedCertificate.Length < 1)
            {
                throw new ArgumentException($"{nameof(allowedCertificate)} cannot be empty.");
            }

            using (var certificate = new X509Certificate2(allowedCertificate))
            {
                return ValidateAndLoad(assemblyPath, nameof(Assembly.LoadFile), verifyCertificateChain, certificate);
            }
        }

        /// <summary>
        /// Loads an assembly from the specified path after verifying its digital signature against the given allowed certificates byte arrays.
        /// </summary>
        /// <remarks>
        /// If the <paramref name="allowedCertificates"/> were createad from '.pfx' or 'p12' files, the overload that accepts an X509Certificate2 instance should be used instead.
        /// </remarks>
        /// <param name="assemblyPath">The file path to the assembly to be loaded.</param>
        /// <param name="allowedCertificates">An enumerable of byte arrays representing the allowed certificates for the assembly.</param>
        /// <param name="verifyCertificateChain">
        /// If <see langword="true"/> (default), performs full certificate chain verification;  
        /// if <see langword="false"/>, only the file hash is checked, bypassing certificate chain validation.
        /// </param>
        /// <returns>The loaded <see cref="Assembly"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="assemblyPath"/> is null or whitespace, or if <paramref name="allowedCertificates"/> is empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="allowedCertificates"/> is null.</exception>
        /// <exception cref="SecurityException">Thrown if the assembly's certificate does not match any of the allowed certificates, or if the assembly is unsigned or has an invalid signature.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the assembly file cannot be found at the specified path.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the assembly path contains invalid characters.</exception>
        /// <exception cref="NotSupportedException">Thrown if the current operating system is not supported.</exception>
        public static Assembly LoadFile(string assemblyPath, IEnumerable<byte[]> allowedCertificates, bool verifyCertificateChain = true)
        {
            if (String.IsNullOrWhiteSpace(assemblyPath))
            {
                throw new ArgumentException($"'{nameof(assemblyPath)}' cannot be null or whitespace.", nameof(assemblyPath));
            }

            if (allowedCertificates is null)
            {
                throw new ArgumentNullException(nameof(allowedCertificates));
            }

            if (!allowedCertificates.Any() || allowedCertificates.All(certificate => certificate.Length < 1))
            {
                throw new ArgumentException($"{nameof(allowedCertificates)} must contain at least one certificate.");
            }

            var certificates = allowedCertificates
                .Select(certificate => new X509Certificate2(certificate))
                .ToArray();

            try
            {
                return ValidateAndLoad(assemblyPath, nameof(Assembly.LoadFile), verifyCertificateChain, certificates);
            }
            finally
            {
                foreach (var certificate in certificates)
                {
                    certificate.Dispose();
                }
            }
        }

        /// <summary>
        /// Validates the digital signature of an assembly and ensures it is signed by an allowed certificate.
        /// </summary>
        /// <remarks>This is a point-in-time check and does not protect a subsequent caller-managed load from file replacement.</remarks>
        /// <param name="assemblyPath">The full path to the assembly file to validate.</param>
        /// <param name="verifyCertificateChain">Indicates whether to verify the certificate chain as part of the signature validation.</param>
        /// <param name="allowedCertificates">A list of certificates that are considered trusted for signing the assembly.</param>
        /// <exception cref="FileNotFoundException">Thrown when <paramref name="assemblyPath"/> is null or the file does not exist.</exception>
        /// <exception cref="SecurityException">
        /// Thrown when the assembly is unsigned, has a tampered or invalid signature, or is not signed with a trusted certificate.
        /// </exception>
        /// <exception cref="NotSupportedException">Thrown if the current operating system is not supported.</exception>
        public static void ValidateAssemblySignature(string assemblyPath, bool verifyCertificateChain, params X509Certificate2[] allowedCertificates)
        {
            ValidateAssemblySignature(assemblyPath, verifyCertificateChain, PlatformHelper.CurrentPlatform, allowedCertificates);
        }

        /// <summary>
        /// Validates the digital signature of an assembly using the signature verification rules of the specified platform.
        /// </summary>
        /// <param name="assemblyPath">The full path to the assembly file to validate.</param>
        /// <param name="verifyCertificateChain">Indicates whether to verify the certificate chain as part of the signature validation.</param>
        /// <param name="platform">The platform whose signature verification implementation should be used.</param>
        /// <param name="allowedCertificates">A list of certificates that are considered trusted for signing the assembly.</param>
        /// <exception cref="NotSupportedException">Thrown if the platform is not supported.</exception>
        internal static void ValidateAssemblySignature(string assemblyPath, bool verifyCertificateChain, OSPlatform platform, params X509Certificate2[] allowedCertificates)
        {
            PlatformHelper.EnsureSupported(platform);

            if (allowedCertificates == null)
            {
                throw new ArgumentNullException(nameof(allowedCertificates));
            }

            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException($"The file '{assemblyPath}' does not exist.", assemblyPath);
            }

            if (platform == OSPlatform.Linux)
            {
                AuthenticodeVerifier.ValidateImage(AuthenticodeVerifier.ReadImage(assemblyPath), verifyCertificateChain, allowedCertificates);
                return;
            }

            X509Certificate2 assemblyCertificate;
            try
            {
                // Verify if the assembly is signed and retrieve the signing certificate
                assemblyCertificate = GetSigningCertificate(assemblyPath, platform);
            }
            catch (CryptographicException ex)
            {
                throw new SecurityException("The assembly's Authenticode signer certificate could not be decoded.", ex);
            }

            if (assemblyCertificate is null)
            {
                throw new SecurityException("Attempt to load an unsigned assembly.");
            }

            using (assemblyCertificate)
            {
                // Validate the signature before trusting any identity from the signed content.
                if (!TryValidateSignature(assemblyPath, verifyCertificateChain, platform, out var result))
                {
                    throw new SecurityException($"Attempt to load an assembly with an invalid signature: {result}");
                }

                // Verify if it's one of the allowed certificates
                if (!allowedCertificates.Any(certificate => certificate != null && string.Equals(certificate.Thumbprint, assemblyCertificate.Thumbprint, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new SecurityException("The certificate of the assembly does not match the provided certificate.");
                }
            }
        }

        /// <summary>
        /// Retrieves the certificate that was used to Authenticode sign the specified file.
        /// </summary>
        /// <param name="filePath">The full path to the signed file.</param>
        /// <param name="platform">The platform whose signature parsing implementation should be used.</param>
        /// <returns>The signing certificate, or <see langword="null"/> when the file is not signed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is empty or consists only of white-space characters.</exception>
        /// <exception cref="CryptographicException">Thrown when Authenticode content is detected but its signer certificate cannot be decoded.</exception>
        internal static X509Certificate2 GetSigningCertificate(string filePath, OSPlatform platform)
        {
            PlatformHelper.EnsureSupported(platform);
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException($"'{nameof(filePath)}' cannot be empty or whitespace.", nameof(filePath));
            }

            if (platform != OSPlatform.Windows)
            {
                // X509Certificate2.GetCertContentType and the Authenticode path constructor rely on CryptoAPI,
                // which is only available on Windows.
                return AuthenticodeVerifier.GetSigningCertificate(filePath);
            }

            X509ContentType contentType;
            try
            {
                contentType = X509Certificate2.GetCertContentType(filePath);
            }
            catch (CryptographicException)
            {
                return null;
            }

            if (contentType != X509ContentType.Authenticode)
            {
                return null;
            }

            // X509CertificateLoader intentionally does not support Authenticode containers. The .NET runtime team
            // recommends checking for Authenticode content and narrowly suppressing SYSLIB0057 around the path
            // constructor. This returns the first signer of the first signature; a different approach is required
            // if support for enumerating multiple Authenticode signatures is introduced.
            // https://github.com/dotnet/runtime/discussions/108740
            // https://github.com/dotnet/runtime/issues/111158
            // https://learn.microsoft.com/dotnet/fundamentals/syslib-diagnostics/syslib0057
#pragma warning disable SYSLIB0057
            return new X509Certificate2(filePath);
#pragma warning restore SYSLIB0057
        }

        private static Assembly ValidateAndLoad(string assemblyPath, string loadAssemblyMethod, bool verifyCertificateChain, params X509Certificate2[] certificates)
        {
            var platform = PlatformHelper.CurrentPlatform;
            if (platform == OSPlatform.Linux)
            {
                if (!assemblyPath.IsPathValid(platform))
                {
                    throw new InvalidOperationException($"Assembly path '{assemblyPath}' contains invalid characters");
                }

                var image = AuthenticodeVerifier.ReadImage(assemblyPath);
                AuthenticodeVerifier.ValidateImage(image, verifyCertificateChain, certificates);
                // Never reopen a caller-controlled pathname after verification.
                return Assembly.Load(image);
            }

            ValidateAssemblySignature(assemblyPath, verifyCertificateChain, platform, certificates);
            return LoadAssembly(assemblyPath, loadAssemblyMethod);
        }

        private static Assembly LoadAssembly(string assemblyPath, string loadAssemblyMethod)
        {
            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException(assemblyPath);
            }

            if (!assemblyPath.IsPathValid())
            {
                throw new InvalidOperationException($"Assembly path '{assemblyPath}' contains invalid characters");
            }

            if (loadAssemblyMethod == nameof(Assembly.LoadFrom))
            {
                return Assembly.LoadFrom(assemblyPath);
            }
            else if (loadAssemblyMethod == nameof(Assembly.LoadFile))
            {
                return Assembly.LoadFile(assemblyPath);
            }
            else
            {
                throw new NotSupportedException($"{loadAssemblyMethod} is not supported");
            }
        }

        private static bool TryValidateSignature(string fileName, bool verifyCertificateChain, OSPlatform platform, out WinVerifyTrustResult result)
        {
            if (platform != OSPlatform.Windows)
            {
                return AuthenticodeVerifier.TryValidateSignature(fileName, verifyCertificateChain, out result);
            }

            using (var fileInfo = new WINTRUST_FILE_INFO(fileName))
            using (var trustData = new WINTRUST_DATA(fileInfo, verifyCertificateChain))
            {
                var noInteractiveUser = new IntPtr(-1);
                result = (WinVerifyTrustResult)(uint)WinVerifyTrust(noInteractiveUser, in WINTRUST_ACTION_GENERIC_VERIFY_V2, trustData);

                return result == WinVerifyTrustResult.Success;
            }
        }

        #region WinTrust
#pragma warning disable S2933 // Add readonly modifier

        /// <summary>
        /// Represents possible results returned by the <c>WinVerifyTrust</c> function, 
        /// which verifies the authenticity and trustworthiness of a signed file.
        /// </summary>
        /// <remarks>
        /// These values indicate the status of the digital signature verification process.
        /// A return value of <see cref="Success"/> (0) means the signature is valid.
        /// Any other value represents an error or trust issue.
        /// </remarks>
        public enum WinVerifyTrustResult : uint
        {
            /// <summary>
            /// Valid signature.
            /// </summary>
            Success = 0,

            /// <summary>
            /// Trust provider is not recognized on this system.
            /// </summary>
            ProviderUnknown = 0x800b0001,

            /// <summary>
            /// Trust provider does not support the specified action.
            /// </summary>
            ActionUnknown = 0x800b0002,

            /// <summary>
            /// Trust provider does not support the form specified for the subject.
            /// </summary>
            SubjectFormUnknown = 0x800b0003,

            /// <summary>
            /// Subject failed the specified verification action.
            /// </summary>
            SubjectNotTrusted = 0x800b0004,

            /// <summary>
            /// TRUST_E_NOSIGNATURE - File was not signed.
            /// </summary>
            FileNotSigned = 0x800B0100,

            /// <summary>
            /// Signer's certificate is in the Untrusted Publishers store.
            /// </summary>
            SubjectExplicitlyDistrusted = 0x800B0111,

            /// <summary>
            /// TRUST_E_BAD_DIGEST - file was probably corrupt.
            /// </summary>
            SignatureOrFileCorrupt = 0x80096010,

            /// <summary>
            /// CERT_E_EXPIRED - Signer's certificate was expired.
            /// </summary>
            SubjectCertExpired = 0x800B0101,

            /// <summary>
            /// CERT_E_REVOKED Subject's certificate was revoked.
            /// </summary>
            SubjectCertificateRevoked = 0x800B010C,

            /// <summary>
            /// CERT_E_UNTRUSTEDROOT - A certification chain processed correctly but terminated in a root certificate that is not trusted by the trust provider.
            /// </summary>
            UntrustedRoot = 0x800B0109
        }

        private enum WinTrustDataUIChoice : uint
        {
            All = 1,
            None = 2,
            NoBad = 3,
            NoGood = 4
        }

        private enum WinTrustDataRevocationChecks : uint
        {
            None = 0x00000000,
            WholeChain = 0x00000001
        }

        private enum WinTrustDataChoice : uint
        {
            File = 1,
            Catalog = 2,
            Blob = 3,
            Signer = 4,
            Certificate = 5
        }

        private enum WinTrustDataStateAction : uint
        {
            Ignore = 0x00000000,
            Verify = 0x00000001,
            Close = 0x00000002,
            AutoCache = 0x00000003,
            AutoCacheFlush = 0x00000004
        }

        [FlagsAttribute]
        private enum WinTrustDataProvFlags : uint
        {
            UseIe4TrustFlag = 0x00000001,
            NoIe4ChainFlag = 0x00000002,
            NoPolicyUsageFlag = 0x00000004,
            RevocationCheckNone = 0x00000010,
            RevocationCheckEndCert = 0x00000020,
            RevocationCheckChain = 0x00000040,
            RevocationCheckChainExcludeRoot = 0x00000080,
            SaferFlag = 0x00000100,        // Used by software restriction policies. Should not be used.
            HashOnlyFlag = 0x00000200,
            UseDefaultOsverCheck = 0x00000400,
            LifetimeSigningFlag = 0x00000800,
            CacheOnlyUrlRetrieval = 0x00001000,      // affects CRL retrieval and AIA retrieval
            DisableMD2andMD4 = 0x00002000      // Win7 SP1+: Disallows use of MD2 or MD4 in the chain except for the root
        }

        private enum WinTrustDataUIContext : uint
        {
            Execute = 0,
            Install = 1
        }

        private static readonly Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 = new Guid("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");

        [DllImport("wintrust.dll", ExactSpelling = true)]
        private static extern int WinVerifyTrust(IntPtr hwnd, in Guid pgActionID, WINTRUST_DATA pWVTData);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class WINTRUST_FILE_INFO : IDisposable
        {
            public uint StructSize = (uint)Marshal.SizeOf(typeof(WINTRUST_FILE_INFO));
            IntPtr pszFilePath;                     // required, file name to be verified
            IntPtr hFile = IntPtr.Zero;             // optional, open handle to FilePath
            IntPtr pgKnownSubject = IntPtr.Zero;    // optional, subject type if it is known

            public WINTRUST_FILE_INFO(string filePath)
            {
                pszFilePath = Marshal.StringToCoTaskMemUni(filePath);
            }

            public void Dispose()
            {
                if (pszFilePath != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(pszFilePath);
                    pszFilePath = IntPtr.Zero;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class WINTRUST_DATA : IDisposable
        {
            UInt32 StructSize = (UInt32)Marshal.SizeOf(typeof(WINTRUST_DATA));
            IntPtr PolicyCallbackData = IntPtr.Zero;

            IntPtr SIPClientData = IntPtr.Zero;
            // required: UI choice
            WinTrustDataUIChoice UIChoice = WinTrustDataUIChoice.None;
            // required: certificate revocation check options
            WinTrustDataRevocationChecks RevocationChecks = WinTrustDataRevocationChecks.None;
            // required: which structure is being passed in?
            WinTrustDataChoice UnionChoice = WinTrustDataChoice.File;
            // individual file
            IntPtr FileInfoPtr;
            WinTrustDataStateAction StateAction = WinTrustDataStateAction.Ignore;
            IntPtr StateData = IntPtr.Zero;
            String URLReference = null;
            // Windows 7 and later 
            WinTrustDataProvFlags ProvFlags = WinTrustDataProvFlags.DisableMD2andMD4;
            WinTrustDataUIContext UIContext = WinTrustDataUIContext.Execute;

            // constructor for silent WinTrustDataChoice.File check
            public WINTRUST_DATA(WINTRUST_FILE_INFO _fileInfo, bool verifyCertificateChain)
            {
                if (!verifyCertificateChain)
                {
                    ProvFlags |= WinTrustDataProvFlags.HashOnlyFlag;
                }

                WINTRUST_FILE_INFO wtfiData = _fileInfo;
                FileInfoPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(WINTRUST_FILE_INFO)));
                Marshal.StructureToPtr(wtfiData, FileInfoPtr, false);
            }

            public void Dispose()
            {
                if (FileInfoPtr != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(FileInfoPtr);
                    FileInfoPtr = IntPtr.Zero;
                }
            }

#pragma warning restore S2933 // Add readonly modifier
            #endregion
        }
    }
}
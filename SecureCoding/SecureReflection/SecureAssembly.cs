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
    /// Both platforms validate and load the same bounded byte snapshot. Both loader families use
    /// <see cref="Assembly.Load(byte[])"/>: Location is empty and path-based dependency resolution is not preserved.
    /// Linux chain verification fails closed when revocation cannot be established; RFC 3161 trust requires the net10.0 asset.
    /// On Linux, disabling chain verification still checks the signature, PE digest, signer policy and pinned certificate, but not dates or revocation.
    /// Windows uses WinVerifyTrust on a handle-bound temporary copy of the snapshot, including its verified signer identity.
    /// Windows also verifies the CMS signature independently of chain trust; a file hash and certificate alone do not authenticate the publisher.
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
        /// if <see langword="false"/>, verifies the signature, file digest and allowed signer without requiring certificate chain trust.
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
        /// if <see langword="false"/>, verifies the signature, file digest and allowed signer without requiring certificate chain trust.
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
        /// if <see langword="false"/>, verifies the signature, file digest and allowed signer without requiring certificate chain trust.
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

            using (var certificate = CertificateLoader.LoadCertificateFromFile(allowedCertificatePath))
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
        /// if <see langword="false"/>, verifies the signature, file digest and allowed signer without requiring certificate chain trust.
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

            var certificates = new List<X509Certificate2>();

            try
            {
                foreach (var certificatePath in allowedCertificatePaths)
                {
                    certificates.Add(CertificateLoader.LoadCertificateFromFile(certificatePath));
                }

                return ValidateAndLoad(assemblyPath, nameof(Assembly.LoadFrom), verifyCertificateChain, certificates.ToArray());
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
        /// if <see langword="false"/>, verifies the signature, file digest and allowed signer without requiring certificate chain trust.
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

            using (var certificate = CertificateLoader.LoadCertificate(allowedCertificate))
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
        /// if <see langword="false"/>, verifies the signature, file digest and allowed signer without requiring certificate chain trust.
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
                .Select(certificate => CertificateLoader.LoadCertificate(certificate))
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
        /// if <see langword="false"/>, verifies the signature, file digest and allowed signer without requiring certificate chain trust.
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
        /// if <see langword="false"/>, verifies the signature, file digest and allowed signer without requiring certificate chain trust.
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
        /// if <see langword="false"/>, verifies the signature, file digest and allowed signer without requiring certificate chain trust.
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

            using (var certificate = CertificateLoader.LoadCertificateFromFile(allowedCertificatePath))
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
        /// if <see langword="false"/>, verifies the signature, file digest and allowed signer without requiring certificate chain trust.
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

            var certificates = new List<X509Certificate2>();

            try
            {
                foreach (var certificatePath in allowedCertificatePaths)
                {
                    certificates.Add(CertificateLoader.LoadCertificateFromFile(certificatePath));
                }

                return ValidateAndLoad(assemblyPath, nameof(Assembly.LoadFile), verifyCertificateChain, certificates.ToArray());
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
        /// if <see langword="false"/>, verifies the signature, file digest and allowed signer without requiring certificate chain trust.
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

            using (var certificate = CertificateLoader.LoadCertificate(allowedCertificate))
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
        /// if <see langword="false"/>, verifies the signature, file digest and allowed signer without requiring certificate chain trust.
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
                .Select(certificate => CertificateLoader.LoadCertificate(certificate))
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
            ReadValidatedAssembly(assemblyPath, verifyCertificateChain, platform, allowedCertificates);
        }

        // The returned bytes are the complete validation-to-load boundary. Never reopen the input path after this call.
        internal static byte[] ReadValidatedAssembly(string assemblyPath, bool verifyCertificateChain, OSPlatform platform, params X509Certificate2[] allowedCertificates)
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

            var snapshot = AuthenticodeVerifier.ReadPeImage(assemblyPath);
            switch (platform)
            {
                case var windows when windows == OSPlatform.Windows:
                    ValidateWindowsAssemblySignature(snapshot, verifyCertificateChain, allowedCertificates);
                    break;
                case var linux when linux == OSPlatform.Linux:
                    AuthenticodeVerifier.ValidatePeImage(snapshot, verifyCertificateChain, allowedCertificates);
                    break;
                default:
                    throw new NotSupportedException($"The platform '{platform}' is not supported.");
            }

            return snapshot;
        }

        private static void ValidateWindowsAssemblySignature(byte[] snapshot, bool verifyCertificateChain, X509Certificate2[] allowedCertificates)
        {
            // CreateNew prevents pre-created files/links from being followed. Keep this handle open with
            // no write/delete sharing through verification. A new file also has no pre-existing writable mappings.
            // WinVerifyTrust receives the handle, not just a name that could be redirected through a parent junction.
            using (var file = CreateWindowsSnapshot(snapshot))
            {
                ValidateWindowsSnapshot(file, verifyCertificateChain, allowedCertificates);
            }
        }

        // Internal for deterministic tests of sharing and cleanup. The caller must dispose the returned stream.
        internal static FileStream CreateWindowsSnapshot(byte[] snapshot)
        {
            var temporaryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".dll");
            var file = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.DeleteOnClose);
            try
            {
                file.Write(snapshot, 0, snapshot.Length);
                file.Flush();
                file.Position = 0;
                return file;
            }
            catch
            {
                file.Dispose();
                throw;
            }
        }

        // The production caller supplies a newly created, exclusively writable snapshot, kept open throughout
        // verification. This seam also lets tests prove the native verifier uses the handle instead of FileStream.Name.
        internal static void ValidateWindowsSnapshot(FileStream file, bool verifyCertificateChain, X509Certificate2[] allowedCertificates)
        {
            using (var fileInfo = new WINTRUST_FILE_INFO(file.Name, file.SafeFileHandle.DangerousGetHandle()))
            using (var trustData = new WINTRUST_DATA(fileInfo, verifyCertificateChain))
            {
                var result = (WinVerifyTrustResult)(uint)WinVerifyTrust(new IntPtr(-1), in WINTRUST_ACTION_GENERIC_VERIFY_V2, trustData);
                if (result != WinVerifyTrustResult.Success)
                {
                    throw new SecurityException($"Attempt to load an assembly with an invalid signature: {result}");
                }

                // Pin the signer from THIS successful verification, never from a second path-based parse.
                using (var assemblyCertificate = trustData.GetVerifiedSigner())
                {
                    if (!allowedCertificates.Any(certificate => certificate != null && certificate.RawData.SequenceEqual(assemblyCertificate.RawData)))
                    {
                        throw new SecurityException("The certificate of the assembly does not match the provided certificate.");
                    }
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
            PlatformHelper.EnsureSupported(platform);
            if (!assemblyPath.IsPathValid(platform))
            {
                throw new InvalidOperationException($"Assembly path '{assemblyPath}' contains invalid characters");
            }

            var snapshot = ReadValidatedAssembly(assemblyPath, verifyCertificateChain, platform, certificates);
            return LoadAssembly(snapshot, loadAssemblyMethod);
        }

        private static Assembly LoadAssembly(byte[] snapshot, string loadAssemblyMethod)
        {
            if (loadAssemblyMethod != nameof(Assembly.LoadFrom) && loadAssemblyMethod != nameof(Assembly.LoadFile))
            {
                throw new NotSupportedException($"{loadAssemblyMethod} is not supported");
            }

            // Both API families load only the verified snapshot; neither resolves the source pathname again.
            return Assembly.Load(snapshot);
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

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("wintrust.dll", ExactSpelling = true)]
        private static extern int WinVerifyTrust(IntPtr hwnd, in Guid pgActionID, [In, Out] WINTRUST_DATA pWVTData);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("wintrust.dll", ExactSpelling = true)]
        private static extern IntPtr WTHelperProvDataFromStateData(IntPtr stateData);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("wintrust.dll", ExactSpelling = true)]
        private static extern IntPtr WTHelperGetProvSignerFromChain(IntPtr providerData, uint signerIndex, [MarshalAs(UnmanagedType.Bool)] bool counterSigner, uint counterSignerIndex);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("wintrust.dll", ExactSpelling = true)]
        private static extern IntPtr WTHelperGetProvCertFromChain(IntPtr signer, uint certificateIndex);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("crypt32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptMsgControl(IntPtr message, uint flags, uint controlType, ref MessageSignatureVerification parameters);

        // Documented CRYPT_PROVIDER_DATA prefix through hMsg. Pointer-sized fields retain native x86/x64 alignment.
        [StructLayout(LayoutKind.Sequential)]
        private struct ProviderDataHeader
        {
            public uint StructSize;
            public IntPtr WintrustData;
            public int OpenedFile;
            public IntPtr ParentWindow;
            public IntPtr ActionId;
            public IntPtr CryptoProvider;
            public uint Error;
            public uint SecuritySettings;
            public uint PolicySettings;
            public IntPtr Functions;
            public uint TrustStepErrorCount;
            public IntPtr TrustStepErrors;
            public uint StoreCount;
            public IntPtr Stores;
            public uint Encoding;
            public IntPtr Message;
        }

        // CMSG_CTRL_VERIFY_SIGNATURE_EX_PARA with CMSG_VERIFY_SIGNER_CERT (2).
        [StructLayout(LayoutKind.Sequential)]
        private struct MessageSignatureVerification
        {
            public uint StructSize;
            public IntPtr CryptoProvider;
            public uint SignerIndex;
            public uint SignerType;
            public IntPtr Signer;
        }

        // Only the documented leading fields of CRYPT_PROVIDER_CERT are needed. Default native alignment
        // places the certificate pointer at offset 4 on x86 and offset 8 on x64.
        [StructLayout(LayoutKind.Sequential)]
        private struct ProviderCertificateHeader
        {
            public uint StructSize;
            public IntPtr Certificate;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class WINTRUST_FILE_INFO : IDisposable
        {
            public uint StructSize = (uint)Marshal.SizeOf(typeof(WINTRUST_FILE_INFO));
            IntPtr pszFilePath;                     // required, file name to be verified
            IntPtr hFile = IntPtr.Zero;             // optional, open handle to FilePath
            IntPtr pgKnownSubject = IntPtr.Zero;    // optional, subject type if it is known

            public WINTRUST_FILE_INFO(string filePath, IntPtr fileHandle)
            {
                pszFilePath = Marshal.StringToCoTaskMemUni(filePath);
                hFile = fileHandle; // Borrowed; the enclosing FileStream owns the handle through state closure.
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
            WinTrustDataStateAction StateAction = WinTrustDataStateAction.Verify;
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

            public X509Certificate2 GetVerifiedSigner()
            {
                var provider = WTHelperProvDataFromStateData(StateData);
                var signer = provider == IntPtr.Zero ? IntPtr.Zero : WTHelperGetProvSignerFromChain(provider, 0, false, 0);
                var certificate = signer == IntPtr.Zero ? IntPtr.Zero : WTHelperGetProvCertFromChain(signer, 0);
                if (certificate == IntPtr.Zero)
                {
                    throw new SecurityException("WinVerifyTrust did not return a verified signer certificate.");
                }

                var header = Marshal.PtrToStructure<ProviderCertificateHeader>(certificate);
                if (header.StructSize < Marshal.SizeOf<ProviderCertificateHeader>() || header.Certificate == IntPtr.Zero)
                {
                    throw new SecurityException("WinVerifyTrust returned an invalid signer certificate.");
                }

                // WTD_HASH_ONLY_FLAG checks the PE digest but does NOT authenticate the CMS signature.
                // Authenticate the same provider message with its selected signer even when chain checking is off.
                var data = Marshal.PtrToStructure<ProviderDataHeader>(provider);
                if (data.StructSize < Marshal.SizeOf<ProviderDataHeader>() || data.Message == IntPtr.Zero)
                {
                    throw new SecurityException("WinVerifyTrust did not return a signed message.");
                }

                var verification = new MessageSignatureVerification
                {
                    StructSize = (uint)Marshal.SizeOf<MessageSignatureVerification>(),
                    SignerIndex = 0,
                    SignerType = 2, // CMSG_VERIFY_SIGNER_CERT
                    Signer = header.Certificate,
                };
                if (!CryptMsgControl(data.Message, 0, 19, ref verification)) // CMSG_CTRL_VERIFY_SIGNATURE_EX
                {
                    throw new SecurityException($"Attempt to load an assembly with an invalid signature: CMS verification failed (0x{Marshal.GetLastWin32Error():X8}).");
                }

                // This constructor duplicates the native context; dispose it before closing provider state.
                return new X509Certificate2(header.Certificate);
            }

            public void Dispose()
            {
                // VERIFY may allocate state even on failure. CLOSE must happen before freeing the file info
                // or closing the borrowed file handle, including when signer pinning throws.
                if (StateData != IntPtr.Zero)
                {
                    StateAction = WinTrustDataStateAction.Close;
                    WinVerifyTrust(new IntPtr(-1), in WINTRUST_ACTION_GENERIC_VERIFY_V2, this);
                    StateData = IntPtr.Zero;
                }

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
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
        public static Assembly LoadFrom(string assemblyPath, X509Certificate2 allowedCertificate, bool verifyCertificateChain = true)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new InvalidOperationException($"{nameof(LoadFrom)} is only supported for Windows operating systems.");
            }

            if (string.IsNullOrWhiteSpace(assemblyPath))
            {
                throw new ArgumentException($"'{nameof(assemblyPath)}' cannot be null or whitespace.", nameof(assemblyPath));
            }

            if (allowedCertificate is null)
            {
                throw new ArgumentNullException(nameof(allowedCertificate));
            }

            ValidateCertificateAndSignature(assemblyPath, verifyCertificateChain, allowedCertificate);

            return LoadAssembly(assemblyPath, nameof(Assembly.LoadFrom));
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
        public static Assembly LoadFrom(string assemblyPath, IEnumerable<X509Certificate2> allowedCertificates, bool verifyCertificateChain = true)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new InvalidOperationException($"{nameof(LoadFrom)} is only supported for Windows operating systems.");
            }

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

            ValidateCertificateAndSignature(assemblyPath, verifyCertificateChain, allowedCerticatesArr);

            return LoadAssembly(assemblyPath, nameof(Assembly.LoadFrom));
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
        public static Assembly LoadFrom(string assemblyPath, string allowedCertificatePath, bool verifyCertificateChain = true)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new InvalidOperationException($"{nameof(LoadFrom)} is only supported for Windows operating systems.");
            }

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

            ValidateCertificateAndSignature(assemblyPath, verifyCertificateChain, new X509Certificate2(allowedCertificatePath));

            return LoadAssembly(assemblyPath, nameof(Assembly.LoadFrom));
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
        public static Assembly LoadFrom(string assemblyPath, IEnumerable<string> allowedCertificatePaths, bool verifyCertificateChain = true)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new InvalidOperationException($"{nameof(LoadFrom)} is only supported for Windows operating systems.");
            }

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

            ValidateCertificateAndSignature(assemblyPath, verifyCertificateChain, certificates);

            return LoadAssembly(assemblyPath, nameof(Assembly.LoadFrom));
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
        public static Assembly LoadFrom(string assemblyPath, byte[] allowedCertificate, bool verifyCertificateChain = true)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new InvalidOperationException($"{nameof(LoadFrom)} is only supported for Windows operating systems.");
            }

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

            ValidateCertificateAndSignature(assemblyPath, verifyCertificateChain, new X509Certificate2(allowedCertificate));

            return LoadAssembly(assemblyPath, nameof(Assembly.LoadFrom));
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
        public static Assembly LoadFrom(string assemblyPath, IEnumerable<byte[]> allowedCertificates, bool verifyCertificateChain = true)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new InvalidOperationException($"{nameof(LoadFrom)} is only supported for Windows operating systems.");
            }

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

            ValidateCertificateAndSignature(assemblyPath, verifyCertificateChain, certificates);

            return LoadAssembly(assemblyPath, nameof(Assembly.LoadFrom));
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
        public static Assembly LoadFile(string assemblyPath, X509Certificate2 allowedCertificate, bool verifyCertificateChain = true)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new InvalidOperationException($"{nameof(LoadFile)} is only supported for Windows operating systems.");
            }

            if (string.IsNullOrWhiteSpace(assemblyPath))
            {
                throw new ArgumentException($"'{nameof(assemblyPath)}' cannot be null or whitespace.", nameof(assemblyPath));
            }

            if (allowedCertificate is null)
            {
                throw new ArgumentNullException(nameof(allowedCertificate));
            }

            ValidateCertificateAndSignature(assemblyPath, verifyCertificateChain, allowedCertificate);

            return LoadAssembly(assemblyPath, nameof(Assembly.LoadFile));
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
        public static Assembly LoadFile(string assemblyPath, IEnumerable<X509Certificate2> allowedCertificates, bool verifyCertificateChain = true)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new InvalidOperationException($"{nameof(LoadFile)} is only supported for Windows operating systems.");
            }

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

            ValidateCertificateAndSignature(assemblyPath, verifyCertificateChain, allowedCerticatesArr);

            return LoadAssembly(assemblyPath, nameof(Assembly.LoadFile));
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
        public static Assembly LoadFile(string assemblyPath, string allowedCertificatePath, bool verifyCertificateChain = true)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new InvalidOperationException($"{nameof(LoadFile)} is only supported for Windows operating systems.");
            }

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

            ValidateCertificateAndSignature(assemblyPath, verifyCertificateChain, new X509Certificate2(allowedCertificatePath));

            return LoadAssembly(assemblyPath, nameof(Assembly.LoadFile));
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
        public static Assembly LoadFile(string assemblyPath, IEnumerable<string> allowedCertificatePaths, bool verifyCertificateChain = true)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new InvalidOperationException($"{nameof(LoadFile)} is only supported for Windows operating systems.");
            }

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

            ValidateCertificateAndSignature(assemblyPath, verifyCertificateChain, certificates);

            return LoadAssembly(assemblyPath, nameof(Assembly.LoadFile));
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
        public static Assembly LoadFile(string assemblyPath, byte[] allowedCertificate, bool verifyCertificateChain = true)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new InvalidOperationException($"{nameof(LoadFile)} is only supported for Windows operating systems.");
            }

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

            ValidateCertificateAndSignature(assemblyPath, verifyCertificateChain, new X509Certificate2(allowedCertificate));

            return LoadAssembly(assemblyPath, nameof(Assembly.LoadFile));
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
        public static Assembly LoadFile(string assemblyPath, IEnumerable<byte[]> allowedCertificates, bool verifyCertificateChain = true)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new InvalidOperationException($"{nameof(LoadFile)} is only supported for Windows operating systems.");
            }

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

            ValidateCertificateAndSignature(assemblyPath, verifyCertificateChain, certificates);

            return LoadAssembly(assemblyPath, nameof(Assembly.LoadFile));
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

        private static void ValidateCertificateAndSignature(string assemblyPath, bool verifyCertificateChain, params X509Certificate2[] allowedCertificates)
        {
            X509Certificate assemblyCertificate;

            try
            {
                assemblyCertificate = X509Certificate.CreateFromSignedFile(assemblyPath);
            }
            catch (CryptographicException)
            {
                throw new SecurityException("Attempt to load an unsigned assembly.");
            }

            if (assemblyCertificate is null)
            {
                throw new SecurityException("Attempt to load an unsigned assembly.");
            }

            // Needs to run first to cover tampered dll scenarios
            if (!TryValidateSignature(assemblyPath, verifyCertificateChain, out var result))
            {
                throw new SecurityException($"Attempt to load an assembly with an invalid signature: {result}");
            }

            if (!allowedCertificates.Any(certificate => certificate.GetCertHashString() == assemblyCertificate.GetCertHashString()))
            {
                throw new SecurityException("The certificate of the assembly does not match the provided certificate.");
            }
        }

        private static bool TryValidateSignature(string fileName, bool verifyCertificateChain, out WinVerifyTrustResult result)
        {
            var fileInfo = new WINTRUST_FILE_INFO(fileName);
            var trustData = new WINTRUST_DATA(fileInfo, verifyCertificateChain);

            var hWnd = IntPtr.Zero; // No UI
            result = (WinVerifyTrustResult)WinVerifyTrust(hWnd, WINTRUST_ACTION_GENERIC_VERIFY_V2, trustData);

            return result == 0; // 0 means signature is valid
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

        private static Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 = new Guid("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");

        [DllImport("wintrust.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint WinVerifyTrust(IntPtr hwnd, [MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID, WINTRUST_DATA pWVTData);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class WINTRUST_FILE_INFO : IDisposable
        {
            public uint StructSize = (uint)Marshal.SizeOf(typeof(WINTRUST_FILE_INFO));
            IntPtr pszFilePath;                     // required, file name to be verified
            IntPtr hFile = IntPtr.Zero;             // optional, open handle to FilePath
            IntPtr pgKnownSubject = IntPtr.Zero;    // optional, subject type if it is known

            public WINTRUST_FILE_INFO(string filePath)
            {
                pszFilePath = Marshal.StringToCoTaskMemAuto(filePath);
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
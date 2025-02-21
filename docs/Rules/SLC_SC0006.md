# SLC_SC0006 - Ensure secure loading of Assemblies

The SecureAssemblyAnalyzer is a C# diagnostic tool designed to detect and warn against the direct loading of assemblies, as it may introduce security vulnerabilities and unsafe loading practices.

> [!CAUTION]
> Disabling `verifyCertificateChain` (`true` by default) bypasses trust chain certificate validation, potentially exposing the system to untrusted or compromised files.

## Non-compliant
````csharp
private void InsecureAssemblyFileLoading(string assemblyPath)
{
    Assembly.LoadFrom(assemblyPath);

    Assembly.LoadFile(assemblyPath);
}

private void InsecureAssemblyLoading(string assemblyName)
{
    Assembly.Load(assemblyName); // When applicable should be replaced by SecureAssembly.LoadFile or SecureAssembly.LoadFrom
}
````

## Compliant
````csharp
private void SecureAssemblyLoading_X509Certificate2Argument(string assemblyPath, string certificatePath, string password = null)
{
    X509Certificate2 allowedCertificate = !string.IsNullOrWhiteSpace(password)
        ? new X509Certificate2(certificatePath)
        : new X509Certificate2(certificatePath, password); // Necessary when using certificates that store private keys (e.g. .p12/.pfx)

    SecureAssembly.LoadFrom(assemblyPath, allowedCertificate, verifyCertificateChain: true);
	
	SecureAssembly.LoadFrom(assemblyPath, allowedCertificate, verifyCertificateChain: true);
}

private void SecureAssemblyLoading_CertificatePathArgument(string assemblyPath, string certificatePath)
{
    SecureAssembly.LoadFrom(assemblyPath, certificatePath, verifyCertificateChain: true);
	
	SecureAssembly.LoadFile(assemblyPath, certificatePath, verifyCertificateChain: true);
}

private void SecureAssemblyLoading_CertificateInBytesArgument(string assemblyPath, string certificatePath)
{
    byte[] allowedCertificateInBytes = File.ReadAllBytes(certificatePath);

    SecureAssembly.LoadFrom(assemblyPath, allowedCertificateInBytes, verifyCertificateChain: true);
	
	SecureAssembly.LoadFile(assemblyPath, allowedCertificateInBytes, verifyCertificateChain: true);
}
````
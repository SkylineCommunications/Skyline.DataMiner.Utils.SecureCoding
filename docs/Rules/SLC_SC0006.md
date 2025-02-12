# SLC_SC0006 - Ensure secure loading of Assemblies

The SecureAssemblyAnalyzer is a C# diagnostic tool designed to detect and warn against the direct loading of assemblies, as it may introduce security vulnerabilities and unsafe loading practices.

## Non-compliant
````csharp
private void InsecureAssemblyFileLoading(string assemblyPath)
{
    Assembly.LoadFrom(assemblyPath);

    Assembly.LoadFile(assemblyPath);
}

private void InsecureAssemblyLoading(string assemblyName)
{
    Assembly.Load(assemblyName);
}
````

## Compliant
````csharp
private void SecureAssemblyLoading_X509Certificate2Argument(string assemblyPath, string certificatePath, string password = null)
{
    X509Certificate2 allowedCertificate = !string.IsNullOrWhiteSpace(password)
        ? new X509Certificate2(certificatePath)
        : new X509Certificate2(certificatePath, password); // Necessary when using certificates that store private keys (e.g. .p12/.pfx)

    SecureAssembly.Load(assemblyPath, allowedCertificate)
}

private void SecureAssemblyLoading_CertificatePathArgument(string assemblyPath, string certificatePath)
{
    SecureAssembly.Load(assemblyPath, certificatePath)
}

private void SecureAssemblyLoading_CertificateInBytesArgument(string assemblyPath, string certificatePath)
{
    byte[] allowedCertificateInBytes = File.ReadAllBytes(certificatePath);

    SecureAssembly.Load(assemblyPath, allowedCertificateInBytes)
}
````
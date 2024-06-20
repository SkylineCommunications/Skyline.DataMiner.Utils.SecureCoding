# SLC_SC0005 - Certificate callbacks should not always evaluate to true

The CertificateCallbackAnalyzer is a C# diagnostic analyzer that detects and warns against custom ServerCertificateCustomValidationCallback that always return true.

## Non-compliant
````csharp
private void InsecureCertificateCallbacks(string insecurePath)
{
    var httpClientHandler = new HttpClientHandler();

    httpClientHandler.ServerCertificateCustomValidationCallback =
    (HttpRequestMessage request, X509Certificate2 cert, X509Chain chain, SslPolicyErrors errors) =>
    {
        if (errors == SslPolicyErrors.None)
            return true; 
        return true;
    };
    
    httpClientHandler.ServerCertificateCustomValidationCallback = InsecureCustomValidationCallback;
}

private static bool InsecureCustomValidationCallback(
    HttpRequestMessage requestMessage,
    X509Certificate2? certificate,
    X509Chain? chain,
    SslPolicyErrors sslErrors)
{
    return true;
}
````

## Compliant
````csharp
private void SecureCertificateCallbacks()
{
    var httpClientHandler = new HttpClientHandler();

    httpClientHandler.ServerCertificateCustomValidationCallback =
        (HttpRequestMessage request, X509Certificate2 cert, X509Chain chain, SslPolicyErrors errors) =>
        {
            if (errors == SslPolicyErrors.None)
                return true; 
            return false;
        };
    
    httpClientHandler.ServerCertificateCustomValidationCallback = SecureCustomValidationCallback;
}

private static bool SecureCustomValidationCallback(
    HttpRequestMessage requestMessage,
    X509Certificate2? certificate,
    X509Chain? chain,
    SslPolicyErrors sslErrors)
    {
        return sslErrors == SslPolicyErrors.None;
    }
````

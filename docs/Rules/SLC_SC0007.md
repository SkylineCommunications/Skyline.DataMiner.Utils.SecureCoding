# SLC_SC0007 - Avoid insecure cryptographic algorithms

The CryptographicAnalyzer detects the usage of insecure or deprecated cryptographic algorithms in C# applications. It flags both hashing and encryption algorithms that are widely considered insecure and encourages the use of modern, secure alternatives.

Using outdated cryptographic primitives can leave your application vulnerable to a wide range of attacks, including collision attacks, preimage attacks, and key recovery.

## Triggered When

This analyzer triggers when your code:

-   Instantiates insecure cryptographic algorithm types (e.g., `new MD5CryptoServiceProvider()`)
    
-   Calls static `Create()` methods on insecure cryptographic algorithm classes (e.g., `MD5.Create()`)
    

## Insecure Hashing Algorithms Detected

The following hashing algorithms are considered insecure and will trigger this diagnostic:

-   MD5 (`System.Security.Cryptography.MD5`, `MD5CryptoServiceProvider`)
    
-   SHA1 (`System.Security.Cryptography.SHA1`, `SHA1Managed`)
    

**Suggested Alternatives:**

-   SHA256
    
-   SHA384
    
-   SHA512
    

## Insecure Encryption Algorithms Detected

The following encryption algorithms are considered insecure and will trigger this diagnostic:

-   DES
    
-   TripleDES
    
-   RC2
    

**Suggested Alternative:**

-   AES (`Aes`, `AesManaged`, `AesCryptoServiceProvider`)
````
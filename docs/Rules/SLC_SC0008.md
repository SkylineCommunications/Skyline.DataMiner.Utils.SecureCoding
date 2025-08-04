# SLC_SC0008 - Avoid usage of BinaryFormatter for serialization

The BinaryFormatterAnalyzer detects the usage of the `System.Runtime.Serialization.Formatters.Binary.BinaryFormatter` class in C# applications. This class is insecure and deprecated due to its potential to introduce remote code execution (RCE) vulnerabilities during deserialization.

Using BinaryFormatter can expose applications to serious security risks. Safer, modern alternatives should be used instead.

## Triggered When

This analyzer triggers when your code:

-   Instantiates a `BinaryFormatter` (e.g., `new BinaryFormatter()`)
    
-   Invokes methods on a `BinaryFormatter` instance (e.g., `formatter.Serialize(...)`, `formatter.Deserialize(...)`)
    

## Why BinaryFormatter is Insecure

`BinaryFormatter` is vulnerable to multiple deserialization attacks that can lead to arbitrary code execution. Attackers can exploit the deserialization process by injecting malicious payloads that execute code when deserialized.

Microsoft has deprecated `BinaryFormatter` and recommends against its usage in all .NET applications.  
[Read Microsoft’s official guidance here](https://learn.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-security-guide).

## Suggested Alternatives

Use the following secure and supported serializers instead of `BinaryFormatter`:

-   `System.Text.Json`
    
-   `Newtonsoft.Json` (Json.NET)
    
-   `System.Xml.Serialization.XmlSerializer`
    
-   `System.Runtime.Serialization.DataContractSerializer`
    
These alternatives are designed to safely serialize and deserialize objects without the security risks associated with `BinaryFormatter`.
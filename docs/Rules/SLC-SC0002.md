# SLC-SC0002 - Avoid using 'System.IO.Path.Combine'

The PathCombineAnalyzer is a C# diagnostic analyzer that identifies and warns against using the System.IO.Path.Combine method without considering security implications. 
It suggests using the safer SecureIO.ConstructSecurePath method instead, enhancing code security.

````csharp
// Non-compliant
string path = Path.Combine(@"C:\Folder", "Example.txt");

// Should be securely constructed
string securePath = SecurePath.ConstructSecurePath(@"C:\Folder", "Example.txt");
````

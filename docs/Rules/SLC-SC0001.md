# SLC-SC0001 - File operation usage detected without secure path construction or validation

The FileOperationAnalyzer is a diagnostic analyzer for C# code that detects insecure file operations by identifying instances where file paths are used without proper construction or validation. 
It ensures secure path handling practices, enhancing the security of C# codebases.

````csharp
// Non-compliant
string path = @"C:\Folder\Example.txt";
File.WriteAllText(path, "content"); 

// Should be securely constructed
string securePath = SecurePath.ConstructSecurePath(@"C:\Folder", "Example.txt");
File.WriteAllText(securePath,  "content");

// Or properly validated
string path = @"C:\Folder\Example.txt";
if(SecurePath.IsPathValid(path))
{
	File.WriteAllText(path,  "content");
}
````
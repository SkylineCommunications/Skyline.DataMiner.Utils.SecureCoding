# SLC_SC0001 - File operation usage detected without secure path construction or validation

The FileOperationAnalyzer is a diagnostic analyzer for C# code that detects insecure file operations by identifying instances where file paths are used without proper construction or validation. 
It ensures secure path handling practices, enhancing the security of C# codebases.

````csharp
// Non-compliant
string path = @"C:\Folder\Example.txt";
File.WriteAllText(path, "content"); 

// Construct secure path by passing the full file path
SecurePath secureFullPath = SecurePath.CreateSecurePath(@"C:\Folder\Example.txt");
File.WriteAllText(secureFullPath, "content");

// Construct secure path using multiple path segments, where the last should be the filename
string securePath = SecurePath.ConstructSecurePath(@"C:\Folder", "SubFolder", "Example.txt");
File.WriteAllText(securePath, "content");

// Construct a secure path without a filename by specifying a base path and a relative subdirectory path
string securePathWithoutFilename1 = SecurePath.ConstructSecurePathWithSubDirectories(@"C:\Folder", @"SubFolder\SubSubFolder");
Console.WriteLine($"Secure Path 1: {securePathWithoutFilename1}");
// 'C:\Folder' is the base path, while 'SubFolder\SubSubFolder' is the relative path

// Alternative way to construct a secure path with a base path and a single subdirectory
string securePathWithoutFilename2 = SecurePath.ConstructSecurePathWithSubDirectories(@"C:\Folder\SubFolder", "SubSubFolder");
Console.WriteLine($"Secure Path 2: {securePathWithoutFilename2}");
// Here, 'C:\Folder\SubFolder' is the base path, and 'SubSubFolder' is the relative path

// Path Validation
string path = @"C:\Folder\Example.txt";
if(path.IsPathValid())
{
	File.WriteAllText(path,  "content");
}
````

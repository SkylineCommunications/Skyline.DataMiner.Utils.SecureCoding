# SLC_SC0001 - File operation usage detected without secure path construction or validation

The FileOperationAnalyzer is a diagnostic analyzer for C# code that detects insecure file operations by identifying instances where file paths are used without proper construction or validation. 
It ensures secure path handling practices, enhancing the security of C# codebases.

````csharp
// Non-compliant
string path = @"C:\Folder\Example.txt";
File.WriteAllText(path, "content"); 

// Construct a secure path by passing the full file path
SecurePath secureFullPath = SecurePath.CreateSecurePath(@"C:\Folder\Example.txt");
File.WriteAllText(secureFullPath, "content");

// Construct a secure path with multiple path segments, where the last should be the filename
string securePathWithFilename = SecurePath.ConstructSecurePath(@"C:\Folder", "SubFolder", "Example.txt");
File.WriteAllText(securePathWithFilename, "content");

// Construct secure path with multiple path segments, where all are directories
string securePathWithDirectories = SecurePath.ConstructSecurePath(@"C:\Folder", "SubFolder", "SubSubFolder");
File.WriteAllText(securePathWithDirectories, "content");

// Construct a secure path without a filename by specifying a base path and a relative subdirectory path
string securePathWithRelativePath = SecurePath.ConstructSecurePathWithSubDirectories(@"C:\Folder", @"SubFolder\SubSubFolder");
// 'C:\Folder' is the base path, while 'SubFolder\SubSubFolder' is the relative path
File.WriteAllText(securePathWithRelativePath, "content");

// Alternative way to construct a secure path with a base path and a single subdirectory
string securePathWithRelativePathAlternative = SecurePath.ConstructSecurePathWithSubDirectories(@"C:\Folder\SubFolder", "SubSubFolder");
// 'C:\Folder\SubFolder' is the base path, and 'SubSubFolder' is the relative path
File.WriteAllText(securePathWithRelativePathAlternative, "content");

// Path Validation
string path = @"C:\Folder\Example.txt";
if(path.IsPathValid())
{
   File.WriteAllText(path,  "content");
}
````

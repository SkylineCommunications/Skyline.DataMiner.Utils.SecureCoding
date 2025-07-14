// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage
    ("Major Code Smell",
    "S3928:Parameter names used into ArgumentException constructors should match an existing one ",
    Justification = "ThrowIfNegative is not available in .NET Framework",
    Scope = "member",
    Target = "~M:Skyline.DataMiner.Utils.SecureCoding.Analyzers.Tests.AnalyzerVerifierHelper.BuildDiagnosticResult(System.String,Microsoft.CodeAnalysis.DiagnosticSeverity,System.Int32,System.Int32)~Microsoft.CodeAnalysis.Testing.DiagnosticResult")]

[assembly: SuppressMessage(
    "SonarQube", 
    "S2699:Tests should include assertions", 
    Justification = "Tests should include assertions is not a valid remark for Roslyn Analyzers Unit Tests.")]
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Testing;
using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.Tests
{
    public static class AnalyzerVerifierHelper
    {
        /// <summary>
        /// Builds an instance of CSharpAnalyzerTest with the specified diagnostic analyzer type and MSTest verifier,
        /// configured with the provided test case sources and optional package references.
        /// </summary>
        /// <typeparam name="T">The type of DiagnosticAnalyzer to use.</typeparam>
        /// <param name="testCaseSources">The source code to analyze.</param>
        /// <param name="packages">Optional package references for the test environment.</param>
        /// <returns>An instance of CSharpAnalyzerTest configured with the specified parameters.</returns>
        public static CSharpAnalyzerTest<T, MSTestVerifier> BuildAnalyzerVerifier<T>(
            string testCaseSources,
            ImmutableArray<PackageIdentity> packages = default)
            where T : DiagnosticAnalyzer, new()
        {
            if (string.IsNullOrEmpty(testCaseSources))
            {
                throw new ArgumentException($"'{nameof(testCaseSources)}' cannot be null or empty.", nameof(testCaseSources));
            }

            return new CSharpAnalyzerTest<T, MSTestVerifier>()
            {
                TestState =
                {
                    Sources = { testCaseSources },
                    ReferenceAssemblies = ReferenceAssemblies.Net.Net50.AddPackages(
                        packages == default
                        ? ImmutableArray<PackageIdentity>.Empty
                        : packages),
                }
            };
        }

        /// <summary>
        /// Builds an instance of CSharpCodeFixTest with the specified diagnostic analyzer type, code fix provider type,
        /// and MSTest verifier, configured with the provided test case sources, fixed state sources, and optional package references.
        /// </summary>
        /// <typeparam name="T1">The type of DiagnosticAnalyzer to use.</typeparam>
        /// <typeparam name="T2">The type of CodeFixProvider to use.</typeparam>
        /// <param name="testCaseSources">The source code to analyze.</param>
        /// <param name="fixedStateSources">The source code representing the expected fixed state after applying the code fix.</param>
        /// <param name="packages">Optional package references for the test environment.</param>
        /// <returns>An instance of CSharpCodeFixTest configured with the specified parameters.</returns>
        public static CSharpCodeFixTest<T1, T2, MSTestVerifier> BuildAnalyzerVerifierWithCodeFix<T1, T2>(
            string testCaseSources,
            string fixedStateSources,
            ImmutableArray<PackageIdentity> packages = default)
            where T1 : DiagnosticAnalyzer, new()
            where T2 : CodeFixProvider, new()
        {
            if (string.IsNullOrWhiteSpace(testCaseSources))
            {
                throw new ArgumentException($"'{nameof(testCaseSources)}' cannot be null or empty.", nameof(testCaseSources));
            }

            if (string.IsNullOrWhiteSpace(fixedStateSources))
            {
                throw new ArgumentException($"'{nameof(fixedStateSources)}' cannot be null or empty.", nameof(fixedStateSources));
            }

            var packageToAdd = packages == default
                ? ImmutableArray<PackageIdentity>.Empty
                : packages;

            return new CSharpCodeFixTest<T1, T2, MSTestVerifier>()
            {
                TestState =
                {
                    Sources = { testCaseSources },
                    ReferenceAssemblies = new ReferenceAssemblies(
                        "net8.0",
                        new PackageIdentity("Microsoft.NETCore.App.Ref", "8.0.0"),
                        Path.Combine("ref", "net8.0")
                        )
                    .AddPackages(packageToAdd)
                },
                
                FixedState =
                {
                    Sources = { fixedStateSources },
                    ReferenceAssemblies = new ReferenceAssemblies(
                        "net8.0",
                        new PackageIdentity("Microsoft.NETCore.App.Ref","8.0.0"),
                        Path.Combine("ref", "net8.0")
                        )
                    .AddPackages(packageToAdd)
                }
            };
        }

        /// <summary>
        /// Builds a DiagnosticResult representing a diagnostic with the specified ID, severity, and location.
        /// </summary>
        /// <param name="diagnosticId">The ID of the diagnostic.</param>
        /// <param name="severity">The severity of the diagnostic.</param>
        /// <param name="line">The line number of the diagnostic location.</param>
        /// <param name="column">The column number of the diagnostic location.</param>
        /// <returns>A DiagnosticResult instance representing the specified diagnostic.</returns>
        public static DiagnosticResult BuildDiagnosticResult(string diagnosticId, DiagnosticSeverity severity, int line, int column)
        {
            if (string.IsNullOrWhiteSpace(diagnosticId))
            {
                throw new ArgumentException($"'{nameof(diagnosticId)}' cannot be null or whitespace.", nameof(diagnosticId));
            }

            if (line < 0)
            {
                throw new ArgumentOutOfRangeException($"'{nameof(line)}' should be greater than 0.");
            }

            if (column < 0)
            {
                throw new ArgumentOutOfRangeException($"'{nameof(column)}' should be greater than 0.");
            }

            return new DiagnosticResult(diagnosticId, severity).WithLocation(line, column);
        }


        /// <summary>
        /// Builds a DiagnosticResult representing a diagnostic with the specified ID, severity, and location.
        /// </summary>
        /// <param name="diagnosticId">The ID of the diagnostic.</param>
        /// <param name="severity">The severity of the diagnostic.</param>
        /// <param name="line">The line number of the diagnostic location.</param>
        /// <param name="column">The column number of the diagnostic location.</param>
        /// <returns>A DiagnosticResult instance representing the specified diagnostic.</returns>
        public static DiagnosticResult BuildDiagnosticResult(DiagnosticDescriptor diagnosticDescriptor, int line, int column)
        {
            if (diagnosticDescriptor is null)
            {
                throw new ArgumentNullException(nameof(diagnosticDescriptor));
            }

            if (line < 0)
            {
                throw new ArgumentOutOfRangeException($"'{nameof(line)}' should be greater than 0.");
            }

            if (column < 0)
            {
                throw new ArgumentOutOfRangeException($"'{nameof(column)}' should be greater than 0.");
            }

            return new DiagnosticResult(diagnosticDescriptor).WithLocation(line, column);
        }
    }
}

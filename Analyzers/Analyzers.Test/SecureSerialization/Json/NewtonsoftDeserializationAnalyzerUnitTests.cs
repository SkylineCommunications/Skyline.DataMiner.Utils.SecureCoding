using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skyline.DataMiner.Utils.SecureCoding.CodeFixProviders.SecureSerialization.Json;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureSerialization.Json;
using System.IO;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.Tests
{
    [TestClass]
    public class NewtonsoftDeserializationAnalyzerUnitTests
    {
        private readonly string testCase = File.ReadAllText(@"..\..\..\SecureSerialization\Json\TestScenarios\JsonConvert.test");
        private readonly string expectedCodefix = File.ReadAllText(@"..\..\..\SecureSerialization\Json\TestScenarios\JsonConvert.fix");
        private readonly string knownTypesExpectedCodeFix = File.ReadAllText(@"..\..\..\SecureSerialization\Json\TestScenarios\JsonConvertKnownTypes.fix");

        [TestMethod]
        public async Task VerifyNewtonsoftDeserializationUsageDiagnosticAndCodeFix()
        {            
            ImmutableArray<PackageIdentity> packages = new PackageIdentity[]
            {
                new PackageIdentity("Newtonsoft.json","13.0.3"),
            }.ToImmutableArray();

            List<DiagnosticResult> expectedDiagnostics = new List<DiagnosticResult>()
            {
                BuildDiagnosticResult(10, 20),
                BuildDiagnosticResult(15, 20),
                BuildDiagnosticResult(20, 20),
                BuildDiagnosticResult(25, 20),
                BuildDiagnosticResult(30, 20),
            };

            CSharpCodeFixTest<Analyzers, NewtonsoftDeserializationCodeFixProvider, MSTestVerifier> analyzerFix
                = new CSharpCodeFixTest<Analyzers, NewtonsoftDeserializationCodeFixProvider, MSTestVerifier>()
                {
                    TestState =
                    {
                        Sources = { testCase },
                        ReferenceAssemblies = new ReferenceAssemblies(
                            "net8.0",
                            new PackageIdentity("Microsoft.NETCore.App.Ref", "8.0.0"),
                            Path.Combine("ref", "net8.0")
                            )
                        .AddPackages(packages),
                    },
                    
                    FixedState =
                    {
                        Sources = { expectedCodefix },
                        ReferenceAssemblies = new ReferenceAssemblies(
                            "net8.0",
                            new PackageIdentity("Microsoft.NETCore.App.Ref","8.0.0"),
                            Path.Combine("ref", "net8.0")
                            )
                        .AddPackages(packages)
                    }
                };
            analyzerFix.FixedState.AdditionalReferences.Add(typeof(SecureNewtonsoftDeserialization).Assembly);
            analyzerFix.CodeActionEquivalenceKey = NewtonsoftDeserializationCodeFixProvider.SecureDeserializationFixEquivalenceKey;
            analyzerFix.NumberOfFixAllIterations = 2;
            analyzerFix.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await analyzerFix.RunAsync();
        }
        
        [TestMethod]
        public async Task VerifyNewtonsoftDeserializationUsageDiagnosticAndKnownTypesCodeFix()
        {
            ImmutableArray<PackageIdentity> packages = new PackageIdentity[]
            {
                new PackageIdentity("Newtonsoft.json","13.0.3"),
            }.ToImmutableArray();

            List<DiagnosticResult> expectedDiagnostics = new List<DiagnosticResult>()
            {
                BuildDiagnosticResult(10, 20),
                BuildDiagnosticResult(15, 20),
                BuildDiagnosticResult(20, 20),
                BuildDiagnosticResult(25, 20),
                BuildDiagnosticResult(30, 20),
            };

            CSharpCodeFixTest<Analyzers, NewtonsoftDeserializationCodeFixProvider, MSTestVerifier> analyzerFix
                = new CSharpCodeFixTest<Analyzers, NewtonsoftDeserializationCodeFixProvider, MSTestVerifier>()
                {
                    TestState =
                    {
                        Sources = { testCase },
                        ReferenceAssemblies = new ReferenceAssemblies(
                            "net8.0",
                            new PackageIdentity("Microsoft.NETCore.App.Ref", "8.0.0"),
                            Path.Combine("ref", "net8.0")
                            )
                        .AddPackages(packages),
                    },
                    
                    FixedState =
                    {
                        Sources = { knownTypesExpectedCodeFix },
                        ReferenceAssemblies = new ReferenceAssemblies(
                            "net8.0",
                            new PackageIdentity("Microsoft.NETCore.App.Ref","8.0.0"),
                            Path.Combine("ref", "net8.0")
                            )
                        .AddPackages(packages)
                    }
                };
            analyzerFix.FixedState.AdditionalReferences.Add(typeof(SecureNewtonsoftDeserialization).Assembly);
            analyzerFix.CodeActionEquivalenceKey = NewtonsoftDeserializationCodeFixProvider.KnownTypesSecureDeserializationFixEquivalenceKey;
            analyzerFix.NumberOfFixAllIterations = 2;
            analyzerFix.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await analyzerFix.RunAsync();
        }

        private static DiagnosticResult BuildDiagnosticResult(int line, int column)
        {
            return new DiagnosticResult(
                new DiagnosticDescriptor(
                    NewtonsoftDeserializationAnalyzer.DiagnosticId,
                        "Avoid deserializing json strings by using Newtonsoft directly.",
                        "Avoid deserializing json strings by using Newtonsoft directly.\nConsider using SecureJsonDeserialization.DeserializeObject instead.",
                        "Usage",
                        DiagnosticSeverity.Warning,
                         isEnabledByDefault: true
                )
            ).WithLocation(line, column);
        }
    }
}

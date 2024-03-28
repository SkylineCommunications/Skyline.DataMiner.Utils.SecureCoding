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

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.Tests.SecureSerialization.Json
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
            var packages = new PackageIdentity[]
            {
                new PackageIdentity("Newtonsoft.json","13.0.3"),
            }
            .ToImmutableArray();

            var expectedDiagnostics = new List<DiagnosticResult>()
            {
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 10, 20),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 15, 20),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 20, 20),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 25, 20),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 30, 20),
            };

            var analyzerVerifier = AnalyzerVerifierHelper.
                BuildAnalyzerVerifierWithCodeFix<NewtonsoftDeserializationAnalyzer, NewtonsoftDeserializationCodeFixProvider>(
                    testCase,
                    expectedCodefix,
                    packages);

            analyzerVerifier.FixedState.AdditionalReferences.Add(typeof(SecureNewtonsoftDeserialization).Assembly);
            analyzerVerifier.CodeActionEquivalenceKey = NewtonsoftDeserializationCodeFixProvider.SecureDeserializationFixEquivalenceKey;
            analyzerVerifier.NumberOfFixAllIterations = 2;
            analyzerVerifier.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await analyzerVerifier.RunAsync();
        }

        [TestMethod]
        public async Task VerifyNewtonsoftDeserializationUsageDiagnosticAndKnownTypesCodeFix()
        {
            var packages = new PackageIdentity[]
            {
                new PackageIdentity("Newtonsoft.json","13.0.3"),
            }
            .ToImmutableArray();

            var expectedDiagnostics = new List<DiagnosticResult>()
            {
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 10, 20),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 15, 20),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 20, 20),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 25, 20),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 30, 20),
            };

            var analyzerVerifier = AnalyzerVerifierHelper.
                BuildAnalyzerVerifierWithCodeFix<NewtonsoftDeserializationAnalyzer, NewtonsoftDeserializationCodeFixProvider>(
                    testCase,
                    knownTypesExpectedCodeFix,
                    packages);

            analyzerVerifier.FixedState.AdditionalReferences.Add(typeof(SecureNewtonsoftDeserialization).Assembly);
            analyzerVerifier.CodeActionEquivalenceKey = NewtonsoftDeserializationCodeFixProvider.KnownTypesSecureDeserializationFixEquivalenceKey;
            analyzerVerifier.NumberOfFixAllIterations = 2;
            analyzerVerifier.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await analyzerVerifier.RunAsync();
        }
    }
}

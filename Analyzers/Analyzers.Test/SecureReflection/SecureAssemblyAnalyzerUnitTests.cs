using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureReflection;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.Tests.SecureReflection
{
    [TestClass]
    public class SecureAssemblyAnalyzerUnitTests
    {
        private readonly string testCase = File.ReadAllText(@"..\..\..\SecureReflection\TestScenarios\SecureAssembly.test");

        [TestMethod]
        public async Task VerifyUsageDiagnostic()
        {
            var expectedDiagnostics = new List<DiagnosticResult>()
            {
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 7, 46),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 8, 44),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 9, 44),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 13, 13),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 14, 4),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 15, 4),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 20, 13),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 21, 4),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 22, 4),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 29, 24),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 37, 24),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 45, 24),
            };

            // [NOTE]: SecureAssemblyCodeFix cannot be unit tested since it introduces a compile error on it's own.

            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifier<SecureAssemblyAnalyzer>(
                testCase);

            analyzerVerifier.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await analyzerVerifier.RunAsync();
        }
    }
}
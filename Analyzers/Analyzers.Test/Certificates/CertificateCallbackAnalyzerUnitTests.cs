using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.Certificates;

#pragma warning disable S2699 // Tests should include assertions is not valid for Roslyn Analyzers Unit Tests.  
namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.Tests.SecureIO
{
    [TestClass]
    public class CertificateCallbackAnalyzerUnitTests
    {
        private readonly string testCase = File.ReadAllText(@"..\..\..\Certificates\TestScenarios\CertificateCallbackAnalyzer.test");
        private readonly string testCaseValid = File.ReadAllText(@"..\..\..\Certificates\TestScenarios\CertificateCallbackAnalyzerNoDiagnostic.test");

        [TestMethod]
        public async Task VerifyUsageDiagnostic()
        {
            var expectedDiagnostics = new List<DiagnosticResult>()
            {
                AnalyzerVerifierHelper.BuildDiagnosticResult(CertificateCallbackAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 14, 22),
                AnalyzerVerifierHelper.BuildDiagnosticResult(CertificateCallbackAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 22, 66),
            };

            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifier<CertificateCallbackAnalyzer>(testCase);

            analyzerVerifier.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await analyzerVerifier.RunAsync();
        }

        [TestMethod]
        public async Task VerifyUsageDiagnostic_NoDiagnosticsThrown()
        {
            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifier<CertificateCallbackAnalyzer>(testCaseValid);

            await analyzerVerifier.RunAsync();
        }
    }
}
#pragma warning restore S2699 // Tests should include assertions is not valid for Roslyn Analyzers Unit Tests.
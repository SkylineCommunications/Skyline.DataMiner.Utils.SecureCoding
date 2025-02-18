using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureIO;

#pragma warning disable S2699 // Tests should include assertions is not valid for Roslyn Analyzers Unit Tests.  
namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.Tests.SecureIO
{
    using Skyline.DataMiner.Utils.SecureCoding.SecureIO;

    [TestClass]
    public class FileOperationAnalyzerUnitTests
    {
        private readonly string fileOperationAnalyzerTestCase = File.ReadAllText(@"..\..\..\SecureIO\TestScenarios\FileOperationAnalyzer.test");
        private readonly string fileOperationAnalyzerTestCaseValid = File.ReadAllText(@"..\..\..\SecureIO\TestScenarios\FileOperationAnalyzerNoDiagnostics.test");
        private readonly string fileOperationFlowsTestCase = File.ReadAllText(@"..\..\..\SecureIO\TestScenarios\FileOperationFlows.test");

        [TestMethod]
        public async Task VerifyUsageDiagnostic()
        {
            var expectedDiagnostics = new List<DiagnosticResult>()
            {
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,14,24),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,15,29),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,16,23),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,17,28),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,18,20),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,19,14),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,19,28),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,20,16),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,21,17),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,22,16),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,23,17),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,24,16),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,25,14),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,25,28),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,26,14),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,27,18),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,28,18),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,29,19),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,30,17),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,30,31),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,30,48),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,31,22),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,32,27),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,33,21),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,34,23),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,35,28),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,36,23),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,37,28),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,38,22),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,39,27),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,41,30),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,44,37),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,55,22),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,62,22),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,67,22),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,74,22),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,78,22),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,82,22),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,90,30),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,91,21),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,92,35),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,93,29),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,94,41),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,95,21),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,96,30),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,97,33),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,98,29),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,99,23),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,100,35),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,101,32),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,102,35),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,103,24),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,104,30),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,105,33),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,106,34),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,107,32),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,108,35),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,109,31),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,110,34),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,115,18),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,117,22),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,120,18),
            };

            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifier<FileOperationAnalyzer>(fileOperationAnalyzerTestCase);

            analyzerVerifier.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await analyzerVerifier.RunAsync();
        }

        [TestMethod]
        public async Task VerifyUsageDiagnostic_NoDiagnosticsThrown()
        {
            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifier<FileOperationAnalyzer>(fileOperationAnalyzerTestCaseValid);
            analyzerVerifier.TestState.AdditionalReferences.Add(typeof(SecurePath).Assembly);

            await analyzerVerifier.RunAsync();
        }

        [TestMethod]
        public async Task VerifyFileOperationFlows_NoDiagnosticsThrown()
        {
            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifier<FileOperationAnalyzer>(fileOperationFlowsTestCase);
            analyzerVerifier.TestState.AdditionalReferences.Add(typeof(SecurePath).Assembly);

            await analyzerVerifier.RunAsync();
        }
    }
}
#pragma warning restore S2699 // Tests should include assertions is not valid for Roslyn Analyzers Unit Tests.
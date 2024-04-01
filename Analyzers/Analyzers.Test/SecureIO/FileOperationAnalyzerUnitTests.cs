using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureIO;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.Tests.SecureIO
{
    [TestClass]
    public class FileOperationAnalyzerUnitTests
    {
        private readonly string testCase = File.ReadAllText(@"..\..\..\SecureIO\TestScenarios\FileOperationAnalyzer.test");

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
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,21,28),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,21,42),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,22,17),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,23,16),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,24,17),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,25,16),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,26,14),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,26,28),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,27,14),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,28,20),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,29,18),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,30,18),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,31,19),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,32,17),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,32,31),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,32,48),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,33,22),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,34,27),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,35,21),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,36,23),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,37,28),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,38,23),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,39,28),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,40,22),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,41,27),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,43,30),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,46,37),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,57,22),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,64,22),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,69,22),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,76,22),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,80,22),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,84,22),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,92,30),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,93,33),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,93,47),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,94,21),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,95,35),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,96,29),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,97,41),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,98,21),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,99,30),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,100,33),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,101,29),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,102,23),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,103,35),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,104,32),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,105,35),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,106,24),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,107,32),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,108,30),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,109,33),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,110,34),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,111,32),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,112,35),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,113,31),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,114,34),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,119,18),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,121,22),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,124,18),
            };

            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifier<FileOperationAnalyzer>(testCase);

            analyzerVerifier.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await analyzerVerifier.RunAsync();
        }
    }
}
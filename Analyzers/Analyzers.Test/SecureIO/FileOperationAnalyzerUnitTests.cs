using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureIO;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.Tests.SecureIO
{
    using Skyline.DataMiner.Utils.SecureCoding.SecureIO;

    [TestClass]
    public class FileOperationAnalyzerUnitTests
    {
        private readonly string fileOperationAnalyzerTestCaseWithDiagnostics = File.ReadAllText(@"..\..\..\SecureIO\TestScenarios\FileOperationAnalyzer.test");
        private readonly string fileOperationAnalyzerTestCaseWoDiagnostics = File.ReadAllText(@"..\..\..\SecureIO\TestScenarios\FileOperationAnalyzerNoDiagnostics.test");
        private readonly string fileOperationFlowsTestCaseWoDiagnostics = File.ReadAllText(@"..\..\..\SecureIO\TestScenarios\FileOperationFlows.test");

        [TestMethod]
        public async Task VerifyUsageDiagnostic_ReportedDiagnostics()
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
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,78,31),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,82,31),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,86,22),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,90,22),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,98,30),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,99,21),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,100,35),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,101,29),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,102,41),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,103,21),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,104,30),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,105,33),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,106,29),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,107,23),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,108,35),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,109,32),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,110,35),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,111,24),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,112,30),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,113,33),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,114,34),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,115,32),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,116,35),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,117,31),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,118,34),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,123,18),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,125,22),
                AnalyzerVerifierHelper.BuildDiagnosticResult(FileOperationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning,128,18),
            };

            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifier<FileOperationAnalyzer>(fileOperationAnalyzerTestCaseWithDiagnostics);
            analyzerVerifier.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await analyzerVerifier.RunAsync();
        }

        [TestMethod]
        public async Task VerifyUsageDiagnostic_NoDiagnosticsThrown()
        {
            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifier<FileOperationAnalyzer>(fileOperationAnalyzerTestCaseWoDiagnostics);
            analyzerVerifier.TestState.AdditionalReferences.Add(typeof(SecurePath).Assembly);

            await analyzerVerifier.RunAsync();
        }

        [TestMethod]
        public async Task VerifyFileOperationFlows_NoDiagnosticsThrown()
        {
            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifier<FileOperationAnalyzer>(fileOperationFlowsTestCaseWoDiagnostics);
            analyzerVerifier.TestState.AdditionalReferences.Add(typeof(SecurePath).Assembly);

            await analyzerVerifier.RunAsync();
        }

        [TestMethod]
        public async Task VerifyFileOperationDifferentClasses_NoDiagnosticsThrown()
        {
            // Static part of a class is a different code block than the instanced part of the class.
            var testCode = @"using System;
                using System.IO;
                using Skyline.DataMiner.Utils.SecureCoding.SecureIO;

                namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.Tests.SecureIO.TestScenarios
                {
                    internal class OwningSymbolClassFirst
                    {
                        public static string securePathFieldFirst = SecurePath.ConstructSecurePath(""LogDirectory"", ""file.txt"");

                        public OwningSymbolClassFirst()
                        {
                            File.ReadAllBytes(securePathFieldFirst);
                        }
                    }

                    internal class OwningSymbolClassSecond
                    {
                        public static string securePathFieldSecond = SecurePath.ConstructSecurePath(""LogDirectory"", ""file.txt"");

                        public OwningSymbolClassSecond()
                        {
                            File.ReadAllBytes(securePathFieldSecond);
                        }
                    }
                }";

            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifier<FileOperationAnalyzer>(testCode);
            analyzerVerifier.TestState.AdditionalReferences.Add(typeof(SecurePath).Assembly);

            await analyzerVerifier.RunAsync();
        }
    }
}
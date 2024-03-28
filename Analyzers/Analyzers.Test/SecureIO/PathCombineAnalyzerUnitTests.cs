using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureIO;
using Skyline.DataMiner.Utils.SecureCoding.CodeFixProviders.SecureIO;
using Skyline.DataMiner.Utils.SecureCoding.SecureIO;
using Microsoft.VisualBasic;
using System;
using System.Diagnostics;


namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.Tests.SecureIO
{
    [TestClass]
    public class PathCombineAnalyzerUnitTests
    {
        private readonly string testCase = File.ReadAllText(@"..\..\..\SecureIO\TestScenarios\PathCombine.test");
        private readonly string expectedCodeFix = File.ReadAllText(@"..\..\..\SecureIO\TestScenarios\PathCombine.fix");

        [TestMethod]
        public async Task VerifyUsageDiagnosticAndCodeFix()
        {
            var expectedDiagnostics = new List<DiagnosticResult>()
            {
                 AnalyzerVerifierHelper.BuildDiagnosticResult(PathCombineAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 7, 57),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(PathCombineAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 11, 13),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(PathCombineAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 16, 13),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(PathCombineAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 23, 24),
            };

            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifierWithCodeFix<PathCombineAnalyzer, PathCombineCodeFix>(
                testCase,
                expectedCodeFix);

            analyzerVerifier.FixedState.AdditionalReferences.Add(typeof(SecurePath).Assembly);
            analyzerVerifier.NumberOfFixAllIterations = 2;
            analyzerVerifier.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await analyzerVerifier.RunAsync();
        }
    }
}

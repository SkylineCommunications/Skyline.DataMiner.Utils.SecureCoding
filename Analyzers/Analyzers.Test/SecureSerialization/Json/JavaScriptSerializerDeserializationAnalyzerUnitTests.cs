using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureSerialization.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.Tests.SecureSerialization.Json
{
    [TestClass]
    public class JavaScriptSerializerDeserializationAnalyzerUnitTests
    {
        private readonly string testCase = File.ReadAllText(@"..\..\..\SecureSerialization\Json\TestScenarios\JavaScriptSerializer.test");

        [TestMethod]
        public async Task VerifyJavaScriptSerializerUsageDiagnostic()
        {
            var expectedDiagnostics = new List<DiagnosticResult>()
            {
                AnalyzerVerifierHelper.BuildDiagnosticResult(JavaScriptSerializerDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 9, 47),
                AnalyzerVerifierHelper.BuildDiagnosticResult(JavaScriptSerializerDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 10, 34),
                AnalyzerVerifierHelper.BuildDiagnosticResult(JavaScriptSerializerDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 11, 36),
                AnalyzerVerifierHelper.BuildDiagnosticResult(JavaScriptSerializerDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 12, 34),
                AnalyzerVerifierHelper.BuildDiagnosticResult(JavaScriptSerializerDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 13, 36),
                AnalyzerVerifierHelper.BuildDiagnosticResult(JavaScriptSerializerDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 14, 36),
                AnalyzerVerifierHelper.BuildDiagnosticResult(JavaScriptSerializerDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 15, 36),
            };

            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifier<JavaScriptSerializerDeserializationAnalyzer>(testCase);

            analyzerVerifier.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);
            analyzerVerifier.TestState.AdditionalReferences.Add(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.Web.Extensions.dll");

            await analyzerVerifier.RunAsync();
        }
    }
}

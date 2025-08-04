using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureSerialization.Binary;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.Tests.SecureSerialization.Binary
{
    [TestClass]
    public class BinaryFormatterAnalyzerUnitTests
    {
        [TestMethod]
        public async Task VerifyBinaryFormatterUsages()
        {
            var testCode = @"using System.Runtime.Serialization.Formatters.Binary;
                            using System.IO;

                            class TestClass
                            {
                                void TestMethod()
                                {
                                    var formatter = new BinaryFormatter();

                                    formatter.Serialize(Stream.Null, new object());

                                    formatter.Deserialize(Stream.Null);
                                }
                            }";

            var expectedDiagnostics = new List<DiagnosticResult>()
            {
                AnalyzerVerifierHelper.BuildDiagnosticResult(BinaryFormatterAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 8, 53),
                AnalyzerVerifierHelper.BuildDiagnosticResult(BinaryFormatterAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 10, 37),
                AnalyzerVerifierHelper.BuildDiagnosticResult(BinaryFormatterAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 12, 37),
            };

            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifier<BinaryFormatterAnalyzer>(testCode);
            analyzerVerifier.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await analyzerVerifier.RunAsync();
        }
    }
}

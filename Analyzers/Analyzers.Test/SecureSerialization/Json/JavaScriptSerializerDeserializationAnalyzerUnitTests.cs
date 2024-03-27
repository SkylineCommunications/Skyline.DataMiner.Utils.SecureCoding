using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureSerialization.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.Tests.SecureSerialization.Json
{
    [TestClass]
    public class JavaScriptSerializerDeserializationAnalyzerUnitTests
    {
        private readonly string testCase = File.ReadAllText(@"..\..\..\SecureSerialization\Json\TestScenarios\JavaScriptSerializer.test");

        [TestMethod]
        public async Task VerifyJavaScriptSerializerUsageDiagnostic()
        {
            List<DiagnosticResult> expectedDiagnostics = new List<DiagnosticResult>()
            {
                BuildDiagnosticResult(9, 47),
                BuildDiagnosticResult(10, 34),
                BuildDiagnosticResult(11, 36),
                BuildDiagnosticResult(12, 34),
                BuildDiagnosticResult(13, 36),
                BuildDiagnosticResult(14, 36),
                BuildDiagnosticResult(15, 36),
            };

            CSharpAnalyzerTest<JavaScriptSerializerDeserializationAnalyzer, MSTestVerifier> analyzerVerifier
                = new CSharpAnalyzerTest<JavaScriptSerializerDeserializationAnalyzer, MSTestVerifier>
                {
                    TestState =
                    {
                        Sources = { testCase },
                        ReferenceAssemblies = new ReferenceAssemblies(
                            "net8.0",
                            new PackageIdentity("Microsoft.NETCore.App.Ref", "8.0.0"),
                            Path.Combine("ref", "net8.0")
                        )
                    }
                };
            analyzerVerifier.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);
            analyzerVerifier.TestState.AdditionalReferences.Add(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.Web.Extensions.dll");

            await analyzerVerifier.RunAsync();
        }

        private static DiagnosticResult BuildDiagnosticResult(int line, int column)
        {
            return new DiagnosticResult(
               new DiagnosticDescriptor(
                JavaScriptSerializerDeserializationAnalyzer.DiagnosticId,
                "Avoid using the JavaScriptSerializer for (de)serialization.",
                "Avoid using the JavaScriptSerializer for (de)serialization.\nMicrosoft recommends to use System.Text.Json or Newtonsoft.Json instead.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true
            )
            ).WithLocation(line, column);
        }
    }
}

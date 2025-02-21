using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureReflection;
using Skyline.DataMiner.Utils.SecureCoding.SecureReflection;

#pragma warning disable S2699 // Tests should include assertions is not valid for Roslyn Analyzers Unit Tests.
namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.Tests.SecureReflection
{
    [TestClass]
    public class SecureAssemblyAnalyzerUnitTests
    {
        private readonly string insecureAssemblyTestCase = File.ReadAllText(
            @"..\..\..\SecureReflection\TestScenarios\InsecureAssembly.test");

        private readonly string insecureAssemblyCodeFix = File.ReadAllText(
            @"..\..\..\SecureReflection\TestScenarios\InsecureAssembly.fix");

        private readonly string defaultArgumentTestCase = File.ReadAllText(
            @"..\..\..\SecureReflection\TestScenarios\DefaultArgument.test");

        private readonly string bypassCertificateChainTestCase = File.ReadAllText(
            @"..\..\..\SecureReflection\TestScenarios\BypassCertificateChain.test");

        [TestMethod]
        public async Task VerifyDefaultArgumentDiagnostic()
        {
            var expectedDiagnostics = new List<DiagnosticResult>()
            {
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleDefaultArgument, 9, 84),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleDefaultArgument, 10, 84),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleDefaultArgument, 14, 44),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleDefaultArgument, 15, 44),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleDefaultArgument, 20, 44),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleDefaultArgument, 21, 44),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleDefaultArgument, 28, 64),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleDefaultArgument, 36, 64),
            };

            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifier<SecureAssemblyAnalyzer>(
                defaultArgumentTestCase);

            analyzerVerifier.TestState.AdditionalReferences.Add(typeof(SecureAssembly).Assembly);

            analyzerVerifier.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await analyzerVerifier.RunAsync();
        }

        [TestMethod]
        public void VerifyInsecureAssemblyCodeFix()
        {
            var expectedCodeFix = insecureAssemblyTestCase
                .Replace(
                    @"Assembly.LoadFile(""assemblyPath"");",
                    @"SecureAssembly.LoadFile(""assemblyPath"", default(X509Certificate2));")
                .Replace(
                    @"Assembly.LoadFrom(""assemblyPath"");",
                    @"SecureAssembly.LoadFrom(""assemblyPath"", default(X509Certificate2));")
                .Replace(
                    @"using System.Reflection;",
                    "using System.Reflection;"
                    + "\r\nusing System.Security.Cryptography.X509Certificates;"
                    + "\r\nusing Skyline.DataMiner.Utils.SecureCoding.SecureReflection;");

            Assert.AreEqual(expectedCodeFix, insecureAssemblyCodeFix);
        }

        [TestMethod]
        public async Task VerifyInsecureAssemblyUsageDiagnostic()
        {
            var expectedDiagnostics = new List<DiagnosticResult>()
            {
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadAssembly, 7, 46),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadFileAssembly, 8, 44),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadFromAssembly, 9, 44),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadAssembly, 13, 13),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadFileAssembly, 14, 4),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadFromAssembly, 15, 4),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadAssembly, 20, 13),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadFileAssembly, 21, 4),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadFromAssembly, 22, 4),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadAssembly, 29, 24),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadFileAssembly, 37, 24),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadFromAssembly, 45, 24),
            };

            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifier<SecureAssemblyAnalyzer>(
                insecureAssemblyTestCase);

            analyzerVerifier.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await analyzerVerifier.RunAsync();
        }

        [TestMethod]
        public async Task VerifyBypassCertificateChain()
        {
            var expectedDiagnostics = new List<DiagnosticResult>()
            {
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleBypassCertificateChain, 8, 52),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleBypassCertificateChain, 12, 13),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleBypassCertificateChain, 17, 13),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleBypassCertificateChain, 30, 24),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleBypassCertificateChain, 38, 24),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleBypassCertificateChain, 46, 24),
            };

            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifier<SecureAssemblyAnalyzer>(
                bypassCertificateChainTestCase);

            analyzerVerifier.TestState.AdditionalReferences.Add(typeof(SecureAssembly).Assembly);

            analyzerVerifier.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await analyzerVerifier.RunAsync();
        }
    }
}
#pragma warning restore S2699 // Tests should include assertions is not valid for Roslyn Analyzers Unit Tests.
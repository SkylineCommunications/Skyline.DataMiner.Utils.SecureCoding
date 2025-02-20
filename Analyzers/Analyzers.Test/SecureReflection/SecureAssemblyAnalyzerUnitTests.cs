using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureReflection;
using Skyline.DataMiner.Utils.SecureCoding.SecureReflection;
using Skyline.DataMiner.Utils.SecureCoding.CodeFixProviders.SecureReflection;

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

        private readonly string bypassCertificateChainTestCase = File.ReadAllText(
            @"..\..\..\SecureReflection\TestScenarios\BypassCertificateChain.test");

        [TestMethod]
        public async Task VerifyInsecureAssemblyUsageDiagnostic()
        {
            var expectedDiagnostics = new List<DiagnosticResult>()
            {
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadAssembly, 7, 46),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadFileAssembly, 7, 44),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadFromAssembly, 8, 44),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadAssembly, 13, 13),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadFileAssembly, 12, 4),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadFromAssembly, 13, 4),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadAssembly, 20, 13),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadFileAssembly, 18, 4),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadFromAssembly, 19, 4),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadAssembly, 29, 24),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadFileAssembly, 26, 24),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureLoadFromAssembly, 34, 24),
            };

            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifierWithCodeFix<SecureAssemblyAnalyzer, SecureAssemblyCodeFix>(
                insecureAssemblyTestCase, insecureAssemblyCodeFix);

            analyzerVerifier.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);
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
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureReflection;
using Skyline.DataMiner.Utils.SecureCoding.SecureReflection;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.Tests.SecureReflection
{
    [TestClass]
    public class SecureAssemblyAnalyzerUnitTests
    {
        private readonly string insecureAssemblyTestCase = File.ReadAllText(
            @"..\..\..\SecureReflection\TestScenarios\InsecureAssembly.test");

        private readonly string bypassCertificateChainTestCase = File.ReadAllText(
            @"..\..\..\SecureReflection\TestScenarios\BypassCertificateChain.test");

        [TestMethod]
        public async Task VerifyInsecureAssemblyUsageDiagnostic()
        {
            var expectedDiagnostics = new List<DiagnosticResult>()
            {
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureAssembly, 7, 46),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureAssembly, 8, 44),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureAssembly, 9, 44),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureAssembly, 13, 13),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureAssembly, 14, 4),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureAssembly, 15, 4),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureAssembly, 20, 13),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureAssembly, 21, 4),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureAssembly, 22, 4),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureAssembly, 29, 24),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureAssembly, 37, 24),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleInsecureAssembly, 45, 24),
            };

            // [NOTE]: SecureAssemblyCodeFix cannot be unit tested since it introduces a compile error on it's own.

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
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleBypassCertificateChain, 24, 24),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleBypassCertificateChain, 32, 24),
                 AnalyzerVerifierHelper.BuildDiagnosticResult(SecureAssemblyAnalyzer.RuleBypassCertificateChain, 40, 24),
            };

            // [NOTE]: SecureAssemblyCodeFix cannot be unit tested since it introduces a compile error on it's own.

            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifier<SecureAssemblyAnalyzer>(
                bypassCertificateChainTestCase);

            analyzerVerifier.TestState.AdditionalReferences.Add(typeof(SecureAssembly).Assembly);

            analyzerVerifier.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await analyzerVerifier.RunAsync();
        }
    }
}
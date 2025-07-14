using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureCryptography;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.Tests.SecureCryptography
{
    [TestClass]
    public class CryptographicAnalyzerUnitTests
    {
        [TestMethod]
        public async Task VerifyInsecureCryptographicInvocationUsages()
        {
            var testCode = @"using System;
                using System.Security.Cryptography;
                using System.Text;

                class InsecureFactoryUsage
                {
                    void MD5_Create()
                    {
                        using (var md5 = MD5.Create())
                        {
                            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(""example""));
                        }
                    }

                    void SHA1_Create()
                    {
                        using (var sha1 = SHA1.Create())
                        {
                            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(""example""));
                        }
                    }

                    void DES_Create()
                    {
                        using (var des = DES.Create())
                        {
                            des.GenerateIV();
                        }
                    }

                    void TripleDES_Create()
                    {
                        using (var tripleDes = TripleDES.Create())
                        {
                            tripleDes.GenerateIV();
                        }
                    }

                    void RC2_Create()
                    {
                        using (var rc2 = RC2.Create())
                        {
                            rc2.GenerateIV();
                        }
                    }
                }";

            var expectedDiagnostics = new List<DiagnosticResult>()
            {
                AnalyzerVerifierHelper.BuildDiagnosticResult(CryptographicAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 9, 42),
                AnalyzerVerifierHelper.BuildDiagnosticResult(CryptographicAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 17, 43),
                AnalyzerVerifierHelper.BuildDiagnosticResult(CryptographicAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 25, 42),
                AnalyzerVerifierHelper.BuildDiagnosticResult(CryptographicAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 33, 48),
                AnalyzerVerifierHelper.BuildDiagnosticResult(CryptographicAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 41, 42),
            };

            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifier<CryptographicAnalyzer>(testCode);
            analyzerVerifier.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await analyzerVerifier.RunAsync();
        }

        [TestMethod]
        public async Task VerifyInsecureCryptographicObjectCreationUsages()
        {
            var testCode = @"using System;
                using System.Security.Cryptography;
                using System.Text;

                class InsecureConstructorUsage
                {
                    void MD5_Ctor()
                    {
                        using (var md5 = new MD5CryptoServiceProvider())
                        {
                            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(""example""));
                        }
                    }

                    void SHA1_Ctor()
                    {
                        using (var sha1 = new SHA1Managed())
                        {
                            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(""example""));
                        }
                    }

                    void DES_Ctor()
                    {
                        using (var des = new DESCryptoServiceProvider())
                        {
                            des.GenerateKey();
                        }
                    }

                    void TripleDES_Ctor()
                    {
                        using (var tripleDes = new TripleDESCryptoServiceProvider())
                        {
                            tripleDes.GenerateKey();
                        }
                    }

                    void RC2_Ctor()
                    {
                        using (var rc2 = new RC2CryptoServiceProvider())
                        {
                            rc2.GenerateKey();
                        }
                    }
                }";

            var expectedDiagnostics = new List<DiagnosticResult>()
            {
                AnalyzerVerifierHelper.BuildDiagnosticResult(CryptographicAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 9, 42),
                AnalyzerVerifierHelper.BuildDiagnosticResult(CryptographicAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 17, 43),
                AnalyzerVerifierHelper.BuildDiagnosticResult(CryptographicAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 25, 42),
                AnalyzerVerifierHelper.BuildDiagnosticResult(CryptographicAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 33, 48),
                AnalyzerVerifierHelper.BuildDiagnosticResult(CryptographicAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 41, 42),
            };

            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifier<CryptographicAnalyzer>(testCode);
            analyzerVerifier.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await analyzerVerifier.RunAsync();
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skyline.DataMiner.Utils.SecureCoding.CodeFixProviders.SecureSerialization.Json;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureSerialization.Json;
using System.IO;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureIO;
using Skyline.DataMiner.Utils.SecureCoding.SecureIO;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.Tests.SecureSerialization.Json
{
    [TestClass]
    public class NewtonsoftDeserializationAnalyzerUnitTests
    {
        private readonly string testCase = File.ReadAllText(@"..\..\..\SecureSerialization\Json\TestScenarios\JsonConvert.test");
        private readonly string expectedCodefix = File.ReadAllText(@"..\..\..\SecureSerialization\Json\TestScenarios\JsonConvert.fix");
        private readonly string knownTypesExpectedCodeFix = File.ReadAllText(@"..\..\..\SecureSerialization\Json\TestScenarios\JsonConvertKnownTypes.fix");

        [TestMethod]
        public async Task VerifyNewtonsoftDeserializationUsageDiagnosticAndCodeFix()
        {
            var packages = new PackageIdentity[]
            {
                new PackageIdentity("Newtonsoft.json","13.0.3"),
            }
            .ToImmutableArray();

            var expectedDiagnostics = new List<DiagnosticResult>()
            {
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 11, 20),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 16, 20),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 21, 20),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 26, 20),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 31, 20),
            };

            var analyzerVerifier = AnalyzerVerifierHelper.
                BuildAnalyzerVerifierWithCodeFix<NewtonsoftDeserializationAnalyzer, NewtonsoftDeserializationCodeFixProvider>(
                    testCase,
                    expectedCodefix,
                    packages);

            analyzerVerifier.FixedState.AdditionalReferences.Add(typeof(SecureNewtonsoftDeserialization).Assembly);
            analyzerVerifier.CodeActionEquivalenceKey = NewtonsoftDeserializationCodeFixProvider.SecureDeserializationFixEquivalenceKey;
            analyzerVerifier.NumberOfFixAllIterations = 2;
            analyzerVerifier.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await analyzerVerifier.RunAsync();
        }

        [TestMethod]
        public async Task VerifyNewtonsoftDeserializationUsageDiagnosticAndKnownTypesCodeFix()
        {
            var packages = new PackageIdentity[]
            {
                new PackageIdentity("Newtonsoft.json","13.0.3"),
            }
            .ToImmutableArray();

            var expectedDiagnostics = new List<DiagnosticResult>()
            {
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 11, 20),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 16, 20),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 21, 20),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 26, 20),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 31, 20),
            };

            var analyzerVerifier = AnalyzerVerifierHelper.
                BuildAnalyzerVerifierWithCodeFix<NewtonsoftDeserializationAnalyzer, NewtonsoftDeserializationCodeFixProvider>(
                    testCase,
                    knownTypesExpectedCodeFix,
                    packages);

            analyzerVerifier.FixedState.AdditionalReferences.Add(typeof(SecureNewtonsoftDeserialization).Assembly);
            analyzerVerifier.CodeActionEquivalenceKey = NewtonsoftDeserializationCodeFixProvider.KnownTypesSecureDeserializationFixEquivalenceKey;
            analyzerVerifier.NumberOfFixAllIterations = 2;
            analyzerVerifier.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await analyzerVerifier.RunAsync();
        }

        [TestMethod]
        public async Task VerifyNewtonsoftDeserializationInsecureUsageDefaultSettings()
        {
            var packages = new PackageIdentity[]
            {
                new PackageIdentity("Newtonsoft.json","13.0.3"),
            }
            .ToImmutableArray();

            var testCode = @"using Newtonsoft.Json;
                using Newtonsoft.Json.Serialization;
                using System;
                using System.Collections.Generic;

                namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.Tests.SecureSerialization.TestScenarios
                {
                    internal class Insecure_JsonDefaultSettings
                    {
                        public void Insecure_TypeNameHandling_All()
                        {
                            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                            {
                                TypeNameHandling = TypeNameHandling.All
                            };

                            JsonConvert.DefaultSettings = () =>
                            {
                                return new JsonSerializerSettings
                                {
                                    TypeNameHandling = TypeNameHandling.All
                                };
                            };
                        }

                        public void Insecure_TypeNameHandling_Auto()
                        {
                            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                            {
                                TypeNameHandling = TypeNameHandling.Auto
                            };

                            JsonConvert.DefaultSettings = () =>
                            {
                                return new JsonSerializerSettings
                                {
                                    TypeNameHandling = TypeNameHandling.Auto
                                };
                            };
                        }


                        public void Insecure_TypeNameHandling_Objects()
                        {
                            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                            {
                                TypeNameHandling = TypeNameHandling.Objects
                            };

                            JsonConvert.DefaultSettings = () =>
                            {
                                return new JsonSerializerSettings
                                {
                                    TypeNameHandling = TypeNameHandling.Objects
                                };
                            };
                        }


                        public void Insecure_TypeNameHandling_Arrays()
                        {
                            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                            {
                                TypeNameHandling = TypeNameHandling.Arrays
                            };

                            JsonConvert.DefaultSettings = () =>
                            {
                                return new JsonSerializerSettings
                                {
                                    TypeNameHandling = TypeNameHandling.Arrays
                                };
                            };
                        }
                    }
                }";

            var expectedDiagnostics = new List<DiagnosticResult>()
            {
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 14, 33),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 21, 37),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 30, 33),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 37, 37),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 47, 33),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 54, 37),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 64, 33),
                AnalyzerVerifierHelper.BuildDiagnosticResult(NewtonsoftDeserializationAnalyzer.DiagnosticId, DiagnosticSeverity.Warning, 71, 37),
            };

            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifier<NewtonsoftDeserializationAnalyzer>(testCode, packages);
            analyzerVerifier.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await analyzerVerifier.RunAsync();
        }

        [TestMethod]
        public async Task VerifyNewtonsoftDeserializationSecureUsageDefaultSettings()
        {
            var packages = new PackageIdentity[]
            {
                new PackageIdentity("Newtonsoft.json","13.0.3"),
            }
            .ToImmutableArray();

            var testCode = @"using Newtonsoft.Json;
                using Newtonsoft.Json.Serialization;
                using System;
                using System.Collections.Generic;

                namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.Tests.SecureSerialization.TestScenarios
                {
                    internal class Secure_JsonDefaultSettings
                    {
                        public void Secure_TypeNameHandling_All()
                        {
                            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                            {
                                TypeNameHandling = TypeNameHandling.All,
                                SerializationBinder = new DefaultSerializationBinder()
                            };
                        }

                        public void Secure_TypeNameHandling_Auto()
                        {
                            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                            {
                                TypeNameHandling = TypeNameHandling.Auto,
                                SerializationBinder = new DefaultSerializationBinder()
                            };
                        }

                        public void Secure_TypeNameHandling_Objects()
                        {
                            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                            {
                                TypeNameHandling = TypeNameHandling.Objects,
                                SerializationBinder = new DefaultSerializationBinder()
                            };
                        }

                        public void Secure_TypeNameHandling_Arrays()
                        {
                            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                            {
                               TypeNameHandling = TypeNameHandling.Arrays,
                               SerializationBinder = new DefaultSerializationBinder()
                            };
                        }
                    }
                }";

            var analyzerVerifier = AnalyzerVerifierHelper.BuildAnalyzerVerifier<NewtonsoftDeserializationAnalyzer>(testCode, packages);

            await analyzerVerifier.RunAsync();
        }
    }
}

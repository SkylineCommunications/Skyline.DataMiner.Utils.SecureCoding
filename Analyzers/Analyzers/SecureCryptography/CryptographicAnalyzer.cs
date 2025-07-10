using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureCryptography
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CryptographicAnalyzer : DiagnosticAnalyzer
    {
        private static readonly ImmutableHashSet<string> insecureHashingAlgorithms = ImmutableHashSet.Create(
            "System.Security.Cryptography.MD5",
            "System.Security.Cryptography.MD5CryptoServiceProvider",
            "System.Security.Cryptography.SHA1",
            "System.Security.Cryptography.SHA1Managed"
        );

        private static readonly ImmutableHashSet<string> insecureEncryptionAlgorithms = ImmutableHashSet.Create(
            "System.Security.Cryptography.DES",
            "System.Security.Cryptography.DESCryptoServiceProvider",
            "System.Security.Cryptography.TripleDES",
            "System.Security.Cryptography.TripleDESCryptoServiceProvider",
            "System.Security.Cryptography.RC2",
            "System.Security.Cryptography.RC2CryptoServiceProvider"
        );

        public const string DiagnosticId = "SLC_SC0007";

        public static DiagnosticDescriptor HashingRule => new DiagnosticDescriptor(
            DiagnosticId,
            title: "Insecure Hashing Algorithm Used",
            messageFormat: "The hashing algorithm '{0}' is considered insecure. Use a secure alternative like SHA256, SHA384 or SHA512.",
            "Usage",
            DiagnosticSeverity.Warning,
            helpLinkUri: $"https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/{DiagnosticId}.md",
            isEnabledByDefault: true);

        public static DiagnosticDescriptor EncryptionRule => new DiagnosticDescriptor(
            DiagnosticId,
            title: "Insecure Encyrption Algorithm Used",
            messageFormat: "The encryption algorithm '{0}' is considered insecure. Use a secure alternative like AES.",
            "Usage",
            DiagnosticSeverity.Warning,
            helpLinkUri: $"https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/{DiagnosticId}.md",
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(HashingRule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeInsecureCryptographicCreation, SyntaxKind.ObjectCreationExpression);

            context.RegisterSyntaxNodeAction(AnalyzeInsecureCryptographicInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInsecureCryptographicInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

            if (symbol == null || symbol.Name != "Create")
            {
                return;
            }

            if (TryGetInsecureAlgorithmDescriptor(symbol.ContainingType.ToString(), out var descriptor))
            {
                var diagnostic = Diagnostic.Create(descriptor, invocation.GetLocation(), symbol.ContainingType.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzeInsecureCryptographicCreation(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
            var typeSymbol = context.SemanticModel.GetSymbolInfo(objectCreation.Type).Symbol as INamedTypeSymbol;

            if (typeSymbol == null)
            {
                return;
            }

            if (TryGetInsecureAlgorithmDescriptor(typeSymbol.ToString(), out var descriptor))
            {
                var diagnostic = Diagnostic.Create(descriptor, objectCreation.GetLocation(), typeSymbol.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool TryGetInsecureAlgorithmDescriptor(string fullTypeName, out DiagnosticDescriptor descriptor)
        {
            if (insecureHashingAlgorithms.Contains(fullTypeName))
            {
                descriptor = HashingRule;
                return true;
            }

            if (insecureEncryptionAlgorithms.Contains(fullTypeName))
            {
                descriptor = EncryptionRule;
                return true;
            }

            descriptor = null;
            return false;
        }
    }
}
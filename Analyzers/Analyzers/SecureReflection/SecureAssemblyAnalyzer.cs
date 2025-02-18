using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureReflection
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SecureAssemblyAnalyzer : DiagnosticAnalyzer
    {
        private static List<string> loadMethods = new List<string>
        {
            nameof(System.Reflection.Assembly.Load),
            nameof(System.Reflection.Assembly.LoadFile),
            nameof(System.Reflection.Assembly.LoadFrom),
        };

        public const string DiagnosticId = "SLC_SC0006";

        public static DiagnosticDescriptor RuleInsecureAssembly => new DiagnosticDescriptor(
            DiagnosticId,
            title: "Ensure secure loading of Assemblies",
            messageFormat: "Consider using 'SecureAssembly.Load' instead of 'Assembly.Load'",
            "Usage",
            DiagnosticSeverity.Warning,
            helpLinkUri: $"https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/{DiagnosticId}.md",
            isEnabledByDefault: true);

        public static DiagnosticDescriptor RuleBypassCertificateChain => new DiagnosticDescriptor(
            DiagnosticId,
            title: "Ensure secure loading of assemblies with certificate validation",
            messageFormat: "Caution: Disabling verifyCertificateChain (false) bypasses trust chain certificate validation",
            "Usage",
            DiagnosticSeverity.Warning,
            helpLinkUri: $"https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/{DiagnosticId}.md",
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(RuleInsecureAssembly, RuleBypassCertificateChain);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeUsages, SyntaxKind.InvocationExpression);
        }

        public static void AnalyzeUsages(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = context.Node as InvocationExpressionSyntax;
            if (invocationExpression == null)
            {
                return;
            }

            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocationExpression).Symbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                return;
            }

            if (loadMethods.Contains(methodSymbol.Name) && methodSymbol.ReceiverType.ToDisplayString() == "System.Reflection.Assembly")
            {
                var diagnostic = Diagnostic.Create(RuleInsecureAssembly, invocationExpression.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }

            if (methodSymbol.ReceiverType.ToDisplayString() == "Skyline.DataMiner.Utils.SecureCoding.SecureReflection.SecureAssembly")
            {
                if (invocationExpression.ArgumentList.Arguments.Count < 3)
                {
                    return;
                }

                var value = context.SemanticModel.GetConstantValue(invocationExpression.ArgumentList.Arguments.Last()?.Expression);
                if (value.HasValue && value.Value is bool && value.Value is false)
                {
                    var diagnostic = Diagnostic.Create(RuleBypassCertificateChain, invocationExpression.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
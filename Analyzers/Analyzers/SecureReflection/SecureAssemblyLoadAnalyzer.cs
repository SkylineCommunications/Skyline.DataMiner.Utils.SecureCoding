using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureReflection
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SecureAssemblyLoadAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SLC_SC0006";

        public static DiagnosticDescriptor Rule => new DiagnosticDescriptor(
            DiagnosticId,
            title: "Ensure secure loading of Assemblies",
            messageFormat: "Consider using 'SecureAssembly.Load' instead of 'Assembly.Load'",
            "Usage",
            DiagnosticSeverity.Warning,
            helpLinkUri: $"https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/{DiagnosticId}.md",
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

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

            if (methodSymbol.Name == nameof(System.Reflection.Assembly.Load))
            {
                var diagnostic = Diagnostic.Create(Rule, invocationExpression.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
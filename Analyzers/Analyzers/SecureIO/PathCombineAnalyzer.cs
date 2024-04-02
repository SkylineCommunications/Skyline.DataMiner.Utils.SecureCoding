using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureIO
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PathCombineAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SLC-SC0002";

        public static DiagnosticDescriptor Rule => new DiagnosticDescriptor(
            DiagnosticId,
            title: "Avoid using 'System.IO.Path.Combine'",
            messageFormat: "Consider using 'SecureIO.ConstructSecurePath' instead of 'System.IO.Path.Combine'",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.EnableConcurrentExecution();

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

            if (methodSymbol != null
                && methodSymbol.Name == "Combine"
                && methodSymbol.ReceiverType.ToDisplayString() == "System.IO.Path")
            {
                var diagnostic = Diagnostic.Create(Rule, invocationExpression.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
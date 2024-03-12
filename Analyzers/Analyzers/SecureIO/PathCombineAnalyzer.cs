using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Skyline.DataMiner.Utils.SecureCoding;
using System.Collections.Immutable;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureIO
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PathCombineAnalyzer : ACustomDiagnosticAnalyzer
    {
        public const string DiagnosticId = "PathCombineUsage";
        private const string Category = "Usage";

        public override DiagnosticDescriptor Rule => new DiagnosticDescriptor(
            DiagnosticId,
            "Avoid using System.IO.Path.Combine",
            "Consider using SecureIO.ConstructSecurePath instead of System.IO.Path.Combine",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "System.IO.Path.Combine can lead to insecure path construction. Consider using ConstructSecurePath.");

        public override void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = context.Node as InvocationExpressionSyntax;
            if (invocationExpression == null)
            {
                return;
            }

            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocationExpression).Symbol as IMethodSymbol;

            if (methodSymbol != null && methodSymbol.Name == "Combine" &&
                methodSymbol.ContainingType.ToDisplayString() == "System.IO.Path")
            {
                var diagnostic = Diagnostic.Create(Rule, invocationExpression.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Skyline.DataMiner.Utils.SecureCoding;
using System.Collections.Immutable;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureIO
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FileOperationAnalyzer : ACustomDiagnosticAnalyzer
    {
        public const string DiagnosticId = "FileOperationCheck";
        private const string Category = "Usage";

        public override DiagnosticDescriptor Rule => new DiagnosticDescriptor(
            DiagnosticId,
            "Potential file operation detected",
            "Potential file operation detected. Consider using a dedicated service or wrapper instead.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Direct file operations can lead to issues like security vulnerabilities and lack of abstraction.");

        public override void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InvocationExpressionSyntax)context.Node;

            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocationExpression).Symbol as IMethodSymbol;

            if (methodSymbol?.ContainingNamespace?.ToString() == "System.IO"
                && !IsSecureConstructPathUsed(invocationExpression)
                && IsFileOperationContext(invocationExpression))
            {
                var diagnostic = Diagnostic.Create(Rule, invocationExpression.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsSecureConstructPathUsed(InvocationExpressionSyntax invocationExpression)
        {
            return invocationExpression.Expression.ToString() == "ConstructPath";
        }

        private static bool IsFileOperationContext(InvocationExpressionSyntax invocationExpression)
        {
            return invocationExpression.Parent is AssignmentExpressionSyntax || invocationExpression.Parent is ExpressionStatementSyntax;
        }
    }
}

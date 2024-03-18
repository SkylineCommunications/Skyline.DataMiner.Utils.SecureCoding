using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureIO
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FileOperationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "FileOperationUsage";
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "File operation usage detected without secure path construction",
            "File operation used without path constructed by 'SecurePath.ConstructSecurePath'",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InvocationExpressionSyntax)context.Node;
            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocationExpression).Symbol as IMethodSymbol;

            if (methodSymbol == null || !IsFileOperation(methodSymbol))
            {
                return;
            }

            foreach (var argument in invocationExpression.ArgumentList.Arguments)
            {
                if (IsSecurePath(context, argument.Expression))
                {
                    var diagnostic = Diagnostic.Create(Rule, argument.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private bool IsSecurePath(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
        {
            if (expression is InvocationExpressionSyntax invocation)
            {
                var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                if (methodSymbol?.ContainingType?.Name == "SecurePath" && (methodSymbol?.Name == "ConstructSecurePath" || methodSymbol?.Name == "IsPathValid"))
                {
                    return true;
                }
            }

            if (expression is IdentifierNameSyntax identifier)
            {
                var symbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol;
                if (symbol is ILocalSymbol localSymbol)
                {
                    var variableDeclarations = localSymbol.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).OfType<VariableDeclaratorSyntax>();
                    foreach (var variableDeclaration in variableDeclarations)
                    {
                        if (IsSecurePath(context, variableDeclaration.Initializer?.Value))
                        {
                            return true;
                        }
                    }
                }

                //if (symbol is IPropertySymbol propertySymbol)
                //{
                //    var propertyDeclarations = propertySymbol.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).OfType<PropertyDeclarationSyntax>();
                //    foreach (var propertyDeclaration in propertyDeclarations)
                //    {
                //        if (IsSecurePathConstruction(context, propertyDeclaration.Initializer?.Value))
                //        {
                //            return true;
                //        }
                //    }
                //}
            }

            return false;
        }

        private static bool IsFileOperation(IMethodSymbol methodSymbol)
        {
            // Check if the method symbol represents a file operation method
            return methodSymbol?.ContainingNamespace?.ToDisplayString() == "System.IO" &&
                   methodSymbol?.ContainingType?.Name == "File";
        }
    }
}

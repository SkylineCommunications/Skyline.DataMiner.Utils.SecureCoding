using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace PropertyAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FileOperationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "FileOperationUsage";
        private const string Category = "Usage";

        private const string METHOD_COSTRUCTSECUREPATH = "SecurePath.ConstructSecurePath";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            title: "File operation usage detected without secure path construction",
            messageFormat: "File operation used with a path '{0}' not constructed by 'SecurePath.ConstructSecurePath' neither validated by 'IsPathValid'",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.EnableConcurrentExecution();

            context.RegisterCodeBlockAction(AnalyzeFileOperationUsages);
        }

        private static void AnalyzeFileOperationUsages(CodeBlockAnalysisContext context)
        {
            var descendantNodes = context.CodeBlock.DescendantNodes(descendIntoChildren: node => node.ChildNodes().Any());

            var invocations = descendantNodes.OfType<InvocationExpressionSyntax>();

            var isPathValidArguments = invocations
                .Where(invocation => IsPathValidMethod(context, invocation))
                .Select(isPathValidInvocation => isPathValidInvocation.ArgumentList.Arguments[0])
                .Distinct();

            var pathArguments = invocations
                .Where(invocation => IsFileOperationMethod(context, invocation))
                .Select(fileOperationInvocation => fileOperationInvocation.ArgumentList.Arguments[0])
                .Distinct();

            var variableDeclarators = descendantNodes.OfType<VariableDeclaratorSyntax>()
                .Where(variableDeclarator => variableDeclarator != null);

            var assigments = descendantNodes.OfType<AssignmentExpressionSyntax>()
                .Where(assignment => assignment != null);

            foreach (var pathArgument in pathArguments)
            {
                var pathArgumentName = pathArgument.Expression.ToString();

                var pathArgumentLocation = pathArgument.GetLocation();

                var matchingAssignments = assigments
                    .Where(assignment => pathArgumentName == GetAssignmentName(assignment))
                    .Distinct();

                foreach (var assignment in matchingAssignments)
                {
                    var assignmentLocation = assignment.GetLocation();

                    if (isPathValidArguments.Any(methodArgument =>
                        IsBetweenLocations(methodArgument.GetLocation(), assignmentLocation, pathArgumentLocation))
                        || assignment.Right.ToString().Contains(METHOD_COSTRUCTSECUREPATH))
                    {
                        continue;
                    }

                    context.ReportDiagnostic(Diagnostic.Create(Rule, pathArgumentLocation, assignmentLocation.GetLineSpan().StartLinePosition.ToString()));
                }

                // In case there's no assignments => check the variable declaration
                var matchingVariableDeclarator = variableDeclarators
                    .FirstOrDefault(variableDeclarator => variableDeclarator?.Identifier.Text == pathArgumentName);

                if (matchingVariableDeclarator != null
                    && !matchingVariableDeclarator.Initializer.ToString().Contains(METHOD_COSTRUCTSECUREPATH))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, pathArgumentLocation, pathArgumentName));
                }
            }
        }

        private static string GetAssignmentName(AssignmentExpressionSyntax assignment)
        {
            // Regular case => Variables/Properties assignments
            if (!assignment.Parent.IsKind(SyntaxKind.ObjectInitializerExpression))
            {
                return assignment.Left.ToString();
            }

            // Special case => Object Initializations
            var ancestorVariableDeclarator = assignment
                .FirstAncestorOrSelf<VariableDeclaratorSyntax>(ascendOutOfTrivia: true);

            if (ancestorVariableDeclarator != null)
            {
                return $"{ancestorVariableDeclarator.Identifier}.{assignment.Left}";
            }

            // Regular case => Variables/Properties assignments
            return assignment.Left.ToString();
        }

        private static bool IsPosteriorLocation(Location location, Location otherLocation)
        {
            // Check if location comes after other location
            return location.SourceSpan.Start > otherLocation.SourceSpan.End;
        }

        public static bool IsBetweenLocations(Location location, Location start, Location end)
        {
            // Check if the location is between start and end
            return location.SourceSpan.Start >= start.SourceSpan.Start &&
                   location.SourceSpan.End <= end.SourceSpan.End;
        }

        private static bool IsFileOperationMethod(CodeBlockAnalysisContext context, InvocationExpressionSyntax invocationExpression)
        {
            if (invocationExpression?.ArgumentList?.Arguments.Count < 1)
            {
                return false;
            }

            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocationExpression).Symbol as IMethodSymbol;

            if (methodSymbol != null
                && methodSymbol.ContainingNamespace?.ToDisplayString() == "System.IO"
                && methodSymbol.ContainingType?.Name == "File")
            {
                return true;
            }

            return false;
        }

        private static bool IsPathValidMethod(CodeBlockAnalysisContext context, InvocationExpressionSyntax invocationExpression)
        {
            if (invocationExpression?.ArgumentList?.Arguments.Count < 1)
            {
                return false;
            }

            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocationExpression).Symbol as IMethodSymbol;

            if (methodSymbol != null
                && methodSymbol.ContainingType?.Name == "SecurePath"
                && methodSymbol.Name == "IsPathValid")
            {
                return true;
            }

            return false;
        }
    }
}

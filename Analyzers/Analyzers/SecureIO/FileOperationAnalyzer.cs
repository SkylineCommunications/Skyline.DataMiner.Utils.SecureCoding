using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureIO
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FileOperationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "FileOperationUsage";
        private const string Category = "Usage";

        private static List<string> receiverTypes = new List<string>
        {
            "System.IO.File",
            "System.IO.StreamWritter",
        };

        private const string METHOD_COSTRUCTSECUREPATH = "SecurePath.ConstructSecurePath";
        private const string METHOD_ISPATHVALID = "SecurePath.IsPathValid";

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
            var objectCreations = descendantNodes.OfType<ObjectCreationExpressionSyntax>();
            var variableDeclarators = descendantNodes.OfType<VariableDeclaratorSyntax>();

            var fileOperationsPathArguments = new List<ArgumentSyntax>();
            var secureMethodsLocations = new List<Location>();

            foreach (var invocation in invocations)
            {
                if (invocation == null || invocation.ArgumentList?.Arguments.Count < 1)
                {
                    continue;
                }

                var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                if (methodSymbol == null)
                {
                    continue;
                }

                if (!methodSymbol.Name.Contains("Read") && !methodSymbol.Name.Contains("Write"))
                {
                    continue;
                }

                var pathArgument = invocation.ArgumentList.Arguments[0];

                var receiverType = methodSymbol.ReceiverType.ToDisplayString();

                switch (receiverType)
                {
                    case "System.IO.File":
                        fileOperationsPathArguments.Add(pathArgument);
                        break;

                    case "System.IO.StreamWritter":
                        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                        {
                            var variableDeclaratorMatchingMemberAccess = variableDeclarators
                                .FirstOrDefault(variableDeclarator => variableDeclarator.Identifier.ToString() == memberAccess.Expression.ToString());

                            if (variableDeclaratorMatchingMemberAccess != null
                                && variableDeclaratorMatchingMemberAccess.Initializer.Value.ToString().Contains(pathArgument.Expression.ToString()))
                            {

                            }




                        }
                        break;

                    default:
                        continue;
                }






                if (IsPathValidMethod(context, invocation)
                    || IsConstructSecurePathMethod(context, invocation))
                {
                    secureMethodsLocations.Add(invocation.GetLocation());
                }
            }

            var locationsToAnalyze = GetLocationsToAnalyze(context, descendantNodes, fileOperationsPathArguments);

            AnalyzeLocations(context, fileOperationsPathArguments, secureMethodsLocations, locationsToAnalyze);
        }

        private static void AnalyzeLocations(
            CodeBlockAnalysisContext context,
            List<ArgumentSyntax> fileOperationsPathArguments,
            List<Location> secureMethodsLocations,
            List<LocationToAnalyze> locationsToAnalyze)
        {
            var reportedLocations = new HashSet<Location>();

            foreach (var locationToAnalyze in locationsToAnalyze)
            {
                // Get the next file operations with the same path argument name
                var nextFileOperationArgument = fileOperationsPathArguments
                    .Find(pathArgument =>
                        pathArgument.Expression.ToString() == locationToAnalyze.PathArgumentName
                        && pathArgument.GetLocation().IsPosteriorLocation(locationToAnalyze.Location));

                var nextFileOperationArgumentLocation = nextFileOperationArgument.GetLocation();

                // Flag if there is no IsPathValid or ConstructSecurePath methods between the assignment and the file operation
                if (!secureMethodsLocations.Exists(methodLocation => methodLocation.IsBetweenLocations(locationToAnalyze.Location, nextFileOperationArgumentLocation)))
                {
                    if (reportedLocations.Contains(nextFileOperationArgumentLocation))
                    {
                        continue;
                    }

                    reportedLocations.Add(nextFileOperationArgumentLocation);

                    context.ReportDiagnostic(Diagnostic.Create(Rule, nextFileOperationArgumentLocation, nextFileOperationArgument.Expression.ToString()));
                }
            }
        }

        private static List<LocationToAnalyze> GetLocationsToAnalyze(
            CodeBlockAnalysisContext context,
            IEnumerable<SyntaxNode> descendantNodes,
            List<ArgumentSyntax> fileOperationsPathArguments)
        {
            var assignments = descendantNodes.OfType<AssignmentExpressionSyntax>();

            var variableDeclarators = descendantNodes.OfType<VariableDeclaratorSyntax>();

            var inputParameters = context.CodeBlock
                .FirstAncestorOrSelf<MethodDeclarationSyntax>().ParameterList
                .ChildNodes()
                .OfType<ParameterSyntax>();

            var locationsToAnalyze = new List<LocationToAnalyze>();

            foreach (var pathArgument in fileOperationsPathArguments)
            {
                var hasLocationToAnalyze = false;

                var pathArgumentName = pathArgument.Expression.ToString();

                // Assignments Locations
                var assignmentsLocations = assignments
                    .Where(assignment => assignment != null
                        && assignment.GetAssignmentName() == pathArgumentName
                        && !assignment.Right.ToString().Contains(METHOD_COSTRUCTSECUREPATH))
                    .Select(assignment => new LocationToAnalyze(assignment.GetLocation(), pathArgumentName));

                if (assignmentsLocations.Any())
                {
                    locationsToAnalyze.AddRange(assignmentsLocations);

                    hasLocationToAnalyze = true;
                }

                // VariableDeclarators Locations
                var variableDeclaratorsLocations = variableDeclarators
                    .Where(variableDeclarator => variableDeclarator != null
                        && (variableDeclarator.Identifier.Text == pathArgumentName || variableDeclarator.Initializer.ToString().Contains(pathArgumentName))
                        && !variableDeclarator.Initializer.ToString().Contains(METHOD_COSTRUCTSECUREPATH))
                    .Select(variableDeclarator => new LocationToAnalyze(variableDeclarator.GetLocation(), pathArgumentName));

                if (variableDeclaratorsLocations.Any())
                {
                    locationsToAnalyze.AddRange(variableDeclaratorsLocations);

                    hasLocationToAnalyze = true;
                }

                // Method Arguments
                var methodArgumentsLocations = inputParameters
                    .Where(inputArgument => inputArgument != null
                        && inputArgument.Identifier.ToString() == pathArgumentName)
                    .Select(inputArgument => new LocationToAnalyze(inputArgument.GetLocation(), pathArgumentName));

                if (methodArgumentsLocations.Any())
                {
                    locationsToAnalyze.AddRange(methodArgumentsLocations);

                    hasLocationToAnalyze = true;
                }

                // Other scenarios, such as foreachs
                if (!hasLocationToAnalyze)
                {
                    locationsToAnalyze.Add(new LocationToAnalyze(descendantNodes.First().GetLocation(), pathArgumentName));
                }
            }

            return locationsToAnalyze;
        }

        private static bool IsFileOperationMethod(
            CodeBlockAnalysisContext context,
            InvocationExpressionSyntax invocation)
        {
            if (invocation?.ArgumentList?.Arguments.Count < 1)
            {

                return false;
            }

            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                return false;
            }

            var receiverType = methodSymbol.ReceiverType.ToDisplayString();

            var isFileOperationMethod = receiverTypes.Contains(receiverType);

            var isReadWriteOperation = methodSymbol.Name.Contains("Read") || methodSymbol.Name.Contains("Write");

            return isFileOperationMethod && isReadWriteOperation;
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

        private static bool IsConstructSecurePathMethod(CodeBlockAnalysisContext context, InvocationExpressionSyntax invocationExpression)
        {
            if (invocationExpression?.ArgumentList?.Arguments.Count < 1)
            {
                return false;
            }

            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocationExpression).Symbol as IMethodSymbol;

            if (methodSymbol != null
                && methodSymbol.ContainingType?.Name == "SecurePath"
                && methodSymbol.Name == "ConstructSecurePath")
            {
                return true;
            }

            return false;
        }

        private sealed class LocationToAnalyze
        {
            public LocationToAnalyze(Location location, string pathArgumentName)
            {
                this.Location = location;

                this.PathArgumentName = pathArgumentName;
            }

            public string PathArgumentName { get; }

            public Location Location { get; }
        }
    }
}

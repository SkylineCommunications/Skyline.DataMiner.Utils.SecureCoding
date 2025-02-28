using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Skyline.DataMiner.Utils.SecureCoding.SecureIO;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureIO
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FileOperationAnalyzer : DiagnosticAnalyzer
    {
        #region Configuration
        private static List<string> containingTypes = new List<string>
        {
            "Skyline.DataMiner.Utils.SecureCoding.SecureIO.SecurePath",
            "Skyline.DataMiner.Utils.SecureCoding.SecureIO.StringExtensions",
        };

        private static List<string> constructSecurePathMethods = new List<string>
        {
            nameof(SecurePath.CreateSecurePath),
            nameof(SecurePath.ConstructSecurePath),
            nameof(SecurePath.ConstructSecurePathWithSubDirectories),
        };

        private static List<string> invocationReceiverTypes = new List<string>
        {
            "System.IO.File",
            "System.IO.Directory",
            "System.Reflection.Assembly",
            "System.Diagnostics.Process"
        };

        private static List<string> objectCreationReceiverTypes = new List<string>
        {
            "System.IO.StreamWriter",
            "System.IO.StreamReader",
            "System.IO.FileStream",
            "System.IO.TextReader",
            "System.IO.TextWritter",
            "System.IO.DirectoryInfo",
        };

        private static List<string> invocationParameterNamesToMatch = new List<string>
        {
            "destFileName",
            "destinationFileName",
            "destinationBackupFileName",
            "fileName",
            "linkPath",
            "path",
            "pathToTarget",
            "sourceFileName",
            "assemblyString",
        };
        #endregion

        public const string DiagnosticId = "SLC_SC0001";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            title: "File operation usage detected without secure path construction or validation",
            messageFormat: "File operation used with a path '{0}' not constructed by 'SecurePath.ConstructSecurePath' neither validated by 'IsPathValid'. These secure methods are available in the Skyline.DataMiner.Utils.SecureCoding NuGet package.",
            "Usage",
            DiagnosticSeverity.Warning,
            helpLinkUri: $"https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/{DiagnosticId}.md",
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCodeBlockAction(AnalyzeFileOperationUsages);
        }

        private static void AnalyzeFileOperationUsages(CodeBlockAnalysisContext context)
        {
            var descendantNodes = context.CodeBlock.DescendantNodes(descendIntoChildren: node => node.ChildNodes().Any());
            var invocations = descendantNodes.OfType<InvocationExpressionSyntax>();
            var variableDeclarators = descendantNodes.OfType<VariableDeclaratorSyntax>();
            var assignments = descendantNodes.OfType<AssignmentExpressionSyntax>();
            var forEachNodes = descendantNodes.OfType<ForEachStatementSyntax>();

            var fileOperationsPathArguments = new List<ArgumentSyntax>();
            var isPathValidLocations = new List<Location>();

            foreach (var invocation in invocations)
            {
                if (invocation == null)
                {
                    return;
                }

                var methodSymbol = context.SemanticModel?.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                if (methodSymbol == null)
                {
                    return;
                }

                if (TryGetFileOperationsPathArguments(context, variableDeclarators, invocation, methodSymbol, out var pathArguments))
                {
                    fileOperationsPathArguments.AddRange(pathArguments);
                }

                if (TryGetIsPathValidLocations(invocation, methodSymbol, out var isPathValidLocation))
                {
                    isPathValidLocations.Add(isPathValidLocation);
                }
            }

            var constructSecurePathResults = GetConstructSecurePathResults(variableDeclarators, assignments);

            var locationsToAnalyze = GetFileOperationLocationsToAnalyze(context, descendantNodes, variableDeclarators, assignments, forEachNodes, fileOperationsPathArguments);

            AnalyzeFileOperationLocations(context, fileOperationsPathArguments, isPathValidLocations, constructSecurePathResults, locationsToAnalyze);
        }

        private static List<string> GetConstructSecurePathResults(
            IEnumerable<VariableDeclaratorSyntax> variableDeclarators,
            IEnumerable<AssignmentExpressionSyntax> assignments)
        {
            var results = new List<string>();

            // Get ConstructSecurePath Results from Variable Declarators
            foreach (var variableDeclarator in variableDeclarators)
            {
                var initializer = variableDeclarator.Initializer?.ToString();

                if (!string.IsNullOrWhiteSpace(initializer) && constructSecurePathMethods.Any(method => initializer.Contains(method)))
                {
                    var identifier = variableDeclarator.Identifier.ToString();
                    if (string.IsNullOrWhiteSpace(identifier))
                    {
                        continue;
                    }

                    results.Add(identifier);
                }
            }

            // Get ConstructSecurePath Results from Assignments
            foreach (var assignment in assignments)
            {
                var rightAssignment = assignment.Right?.ToString();

                if (!string.IsNullOrWhiteSpace(rightAssignment) && constructSecurePathMethods.Any(method => rightAssignment.Contains(method)))
                {
                    var leftAssignemnt = assignment.Left.ToString();
                    if (string.IsNullOrWhiteSpace(leftAssignemnt))
                    {
                        continue;
                    }

                    results.Add(leftAssignemnt);
                }
            }

            return results;
        }

        private static bool TryGetIsPathValidLocations(
            InvocationExpressionSyntax invocation,
            IMethodSymbol methodSymbol,
            out Location isPathValidLocation)
        {
            if (containingTypes.Contains(methodSymbol.ContainingType.ToDisplayString())
                && methodSymbol.Name == nameof(StringExtensions.IsPathValid))
            {
                isPathValidLocation = invocation.GetLocation();
                return true;
            }

            isPathValidLocation = default;
            return false;
        }

        private static bool TryGetFileOperationsPathArguments(
            CodeBlockAnalysisContext context,
            IEnumerable<VariableDeclaratorSyntax> variableDeclarators,
            InvocationExpressionSyntax invocation,
            IMethodSymbol methodSymbol,
            out IEnumerable<ArgumentSyntax> fileOperationPathArguments)
        {
            var pathArguments = GetPathArguments(context, variableDeclarators, invocation, methodSymbol);

            fileOperationPathArguments = pathArguments;

            return pathArguments.Any();
        }

        private static IEnumerable<ArgumentSyntax> GetPathArguments(
            CodeBlockAnalysisContext context,
            IEnumerable<VariableDeclaratorSyntax> variableDeclarators,
            InvocationExpressionSyntax invocation,
            IMethodSymbol methodSymbol)
        {
            var receiverType = methodSymbol.ReceiverType?.ToDisplayString();

            if (invocationReceiverTypes.Contains(receiverType))
            {
                return GetPathArgumentsFromInvocation(invocation, methodSymbol);
            }

            if (objectCreationReceiverTypes.Contains(receiverType))
            {
                return GetPathArgumentsFromObjectCreation(context, variableDeclarators, invocation);
            }

            return Enumerable.Empty<ArgumentSyntax>();
        }

        private static IEnumerable<ArgumentSyntax> GetPathArgumentsFromObjectCreation(
            CodeBlockAnalysisContext context,
            IEnumerable<VariableDeclaratorSyntax> variableDeclarators,
            InvocationExpressionSyntax invocation)
        {
            if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
            {
                return Enumerable.Empty<ArgumentSyntax>();
            }

            var variableDeclaratorMatchingMemberAccess = variableDeclarators
                .FirstOrDefault(variableDeclarator => variableDeclarator.Identifier.ToString() == memberAccess.Expression.ToString());

            if (variableDeclaratorMatchingMemberAccess is null)
            {
                return Enumerable.Empty<ArgumentSyntax>();
            }

            var objectCreationWitinVariableDeclarator = variableDeclaratorMatchingMemberAccess
                .DescendantNodes(descendIntoChildren: node => node.ChildNodes().Any())
                .OfType<ObjectCreationExpressionSyntax>()
                .First();

            if (objectCreationWitinVariableDeclarator is null)
            {
                return Enumerable.Empty<ArgumentSyntax>();
            }

            var objectCreationSymbol = context.SemanticModel.GetSymbolInfo(objectCreationWitinVariableDeclarator).Symbol as IMethodSymbol;
            if (objectCreationSymbol == null)
            {
                return Enumerable.Empty<ArgumentSyntax>();
            }

            if (objectCreationWitinVariableDeclarator.ArgumentList.Arguments.Count < 1)
            {
                return Enumerable.Empty<ArgumentSyntax>();
            }

            var indexes = objectCreationSymbol.GetParametersSymbolIndexByName(invocationParameterNamesToMatch.ToArray());

            return indexes.Select(index => objectCreationWitinVariableDeclarator.ArgumentList.Arguments[index]);
        }

        private static IEnumerable<ArgumentSyntax> GetPathArgumentsFromInvocation(
            InvocationExpressionSyntax invocation,
            IMethodSymbol invocationSymbol)
        {
            if (invocation.ArgumentList?.Arguments.Count < 1)
            {
                return Enumerable.Empty<ArgumentSyntax>();
            }

            var indexes = invocationSymbol.GetParametersSymbolIndexByName(invocationParameterNamesToMatch.ToArray());

            return indexes.Select(index => invocation.ArgumentList.Arguments[index]);
        }

        private static void AnalyzeFileOperationLocations(
            CodeBlockAnalysisContext context,
            List<ArgumentSyntax> fileOperationsPathArguments,
            List<Location> isPathValidLocations,
            List<string> constructSecurePathResults,
            List<LocationToAnalyze> locationsToAnalyze)
        {
            var reportedLocations = new HashSet<Location>();

            foreach (var locationToAnalyze in locationsToAnalyze)
            {
                foreach (var pathArgument in fileOperationsPathArguments)
                {
                    AnalyzeLocations(context, isPathValidLocations, constructSecurePathResults, reportedLocations, locationToAnalyze, pathArgument);
                }
            }
        }

        private static void AnalyzeLocations(
            CodeBlockAnalysisContext context,
            List<Location> secureMethodsLocations,
            List<string> constructSecurePathResults,
            HashSet<Location> reportedLocations,
            LocationToAnalyze locationToAnalyze,
            ArgumentSyntax pathArgument)
        {
            var pathArgumentName = pathArgument.Expression.ToString();

            if (constructSecurePathMethods.Any(method => pathArgumentName.Contains(method)))
            {
                // ConstructSecurePath method is being invoked as input argument of the file operation.
                return;
            }

            var fqnArgument = GetFullyQualifiedName(context.SemanticModel, pathArgument.Expression);
            if (fqnArgument == "Skyline.DataMiner.Utils.SecureCoding.SecureIO.SecurePath")
            {
                // Secure path is already checked
                return;
            }

            if (constructSecurePathResults.Contains(pathArgumentName) || pathArgumentName != locationToAnalyze.PathArgumentName)
            {
                return;
            }

            // Gets the next file operation and checks if there's a secure method in betweeen
            var pathArgumentLocation = pathArgument.GetLocation();

            if (pathArgumentLocation.IsPosteriorLocation(locationToAnalyze.Location))
            {
                if (!secureMethodsLocations.Exists(methodLocation => methodLocation.IsBetweenLocations(locationToAnalyze.Location, pathArgumentLocation)))
                {
                    ReportDiagnostic(context, reportedLocations, pathArgumentLocation, pathArgumentName);
                }

                return;
            }

            // In case there's no next file operation => Gets the previous file operation and checks if there's a secure method in betweeen
            if (pathArgumentLocation.IsAnteriorLocation(locationToAnalyze.Location)
                && !secureMethodsLocations.Exists(methodLocation => methodLocation.IsBetweenLocations(pathArgumentLocation, locationToAnalyze.Location)))
            {
                ReportDiagnostic(context, reportedLocations, pathArgumentLocation, pathArgumentName);
            }
        }

        /// <summary>
        /// Gets the fully qualified name of the specified expression.
        /// </summary>
        /// <param name="semanticModel">The semantic model.</param>
        /// <param name="expression">The expression.</param>
        /// <returns>The fully qualified name.</returns>
        public static string GetFullyQualifiedName(
            SemanticModel semanticModel,
            ExpressionSyntax expression)
        {
            SymbolDisplayFormat symbolDisplayFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

            if (expression is MemberAccessExpressionSyntax maes)
            {
                ITypeSymbol argSymbol = semanticModel.GetTypeInfo(maes.Expression).Type;
                return argSymbol?.ToDisplayString(symbolDisplayFormat);
            }

            if (expression is InvocationExpressionSyntax ies)
            {
                ITypeSymbol argSymbol = semanticModel.GetTypeInfo(ies.Expression).Type;
                return argSymbol?.ToDisplayString(symbolDisplayFormat);
            }

            ITypeSymbol symbol = semanticModel.GetTypeInfo(expression).Type;
            return symbol?.ToDisplayString(symbolDisplayFormat);
        }

        private static void ReportDiagnostic(
            CodeBlockAnalysisContext context,
            HashSet<Location> reportedLocations,
            Location location,
            string description)
        {
            if (reportedLocations.Contains(location))
            {
                return;
            }

            reportedLocations.Add(location);

            context.ReportDiagnostic(Diagnostic.Create(Rule, location, description));
        }

        private static List<LocationToAnalyze> GetFileOperationLocationsToAnalyze(
            CodeBlockAnalysisContext context,
            IEnumerable<SyntaxNode> descendantNodes,
            IEnumerable<VariableDeclaratorSyntax> variableDeclarators,
            IEnumerable<AssignmentExpressionSyntax> assignments,
            IEnumerable<ForEachStatementSyntax> forEachNodes,
            List<ArgumentSyntax> fileOperationsPathArguments)
        {
            var inputParameters = context.CodeBlock?
                .FirstAncestorOrSelf<MethodDeclarationSyntax>()?.ParameterList?
                .ChildNodes()?
                .OfType<ParameterSyntax>();

            var locationsToAnalyze = new List<LocationToAnalyze>();

            foreach (var pathArgument in fileOperationsPathArguments)
            {
                var pathArgumentName = pathArgument.Expression?.ToString();
                if (string.IsNullOrWhiteSpace(pathArgumentName))
                {
                    continue;
                }

                // Assignments Locations - Ignore the ConstructSecurePath methods assignments
                locationsToAnalyze.AddRange(
                    assignments
                    .Where(assignment => assignment != null
                        && assignment.GetAssignmentName() == pathArgumentName
                        && !constructSecurePathMethods.Contains(assignment.Right?.ToString()))
                    .Select(assignment => new LocationToAnalyze(assignment.GetLocation(), pathArgumentName)));

                // VariableDeclarators Locations - Ignore the ConstructSecurePath methods declarators
                locationsToAnalyze.AddRange(
                   variableDeclarators
                   .Where(variableDeclarator => variableDeclarator != null && variableDeclarator.Initializer != null
                       && (variableDeclarator.Identifier.Text == pathArgumentName || variableDeclarator.Initializer.ToString().Contains(pathArgumentName))
                       && !constructSecurePathMethods.Contains(variableDeclarator.Initializer.ToString()))
                   .Select(variableDeclarator => new LocationToAnalyze(variableDeclarator.GetLocation(), pathArgumentName)));

                // Method Arguments
                locationsToAnalyze.AddRange(
                    inputParameters
                    .Where(inputArgument => inputArgument != null && inputArgument.Identifier.ToString() == pathArgumentName)
                    .Select(inputArgument => new LocationToAnalyze(inputArgument.GetLocation(), pathArgumentName)));

                // ForEach Nodes
                locationsToAnalyze.AddRange(
                    forEachNodes
                    .Where(node => node != null && pathArgument.GetLocation().IsLocationOverlapping(node.GetLocation()))
                    .Select(node => new LocationToAnalyze(node.GetLocation(), pathArgumentName)));

                // Cases where the path argument was not added to the locations to analyze => assume the first position of the block
                if (!locationsToAnalyze.Exists(locationToAnalyze => locationToAnalyze.PathArgumentName == pathArgumentName))
                {
                    var firstPosition = descendantNodes.First();
                    if (firstPosition == null)
                    {
                        continue;
                    }

                    locationsToAnalyze.Add(new LocationToAnalyze(descendantNodes.First().GetLocation(), pathArgumentName));
                }
            }

            return locationsToAnalyze;
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
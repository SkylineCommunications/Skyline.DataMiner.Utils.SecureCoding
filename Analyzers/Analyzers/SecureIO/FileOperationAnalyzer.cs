using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureIO
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FileOperationAnalyzer : DiagnosticAnalyzer
    {
        #region Configuration
        private static List<string> secureMethods = new List<string>
        {
            "ConstructSecurePath",
            "IsPathValid"
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
            messageFormat: "File operation used with a path '{0}' not constructed by 'SecurePath.ConstructSecurePath' neither validated by 'IsPathValid'",
            "Usage",
            DiagnosticSeverity.Warning,
            helpLinkUri: "https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/SLC-SC0001.md",
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

            var fileOperationsPathArguments = new List<ArgumentSyntax>();
            var secureMethodsLocations = new List<Location>();

            foreach (var invocation in invocations)
            {
                GetFileOperationsPathArguments(context, variableDeclarators, fileOperationsPathArguments, invocation);

                GetSecureMethodsLocations(context, secureMethodsLocations, invocation);
            }

            var locationsToAnalyze = GetLocationsToAnalyze(context, descendantNodes, variableDeclarators, fileOperationsPathArguments);

            AnalyzeLocations(context, fileOperationsPathArguments, secureMethodsLocations, locationsToAnalyze);
        }

        private static void GetSecureMethodsLocations(
            CodeBlockAnalysisContext context,
            List<Location> secureMethodsLocations,
            InvocationExpressionSyntax invocation)
        {
            if (invocation == null || invocation.ArgumentList?.Arguments.Count < 1)
            {
                return;
            }

            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (methodSymbol != null
                && methodSymbol.ContainingType.ToDisplayString() == "Skyline.DataMiner.Utils.SecureCoding.SecureIO.SecurePath"
                && secureMethods.Contains(methodSymbol.Name))
            {
                secureMethodsLocations.Add(invocation.GetLocation());
            }
        }

        private static void GetFileOperationsPathArguments(
            CodeBlockAnalysisContext context,
            IEnumerable<VariableDeclaratorSyntax> variableDeclarators,
            List<ArgumentSyntax> fileOperationsPathArguments,
            InvocationExpressionSyntax invocation)
        {
            if (invocation == null)
            {
                return;
            }

            var invocationSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (invocationSymbol == null)
            {
                return;
            }

            var pathArguments = GetPathArguments(context, variableDeclarators, invocation, invocationSymbol);

            fileOperationsPathArguments.AddRange(pathArguments);
        }

        private static IEnumerable<ArgumentSyntax> GetPathArguments(
            CodeBlockAnalysisContext context,
            IEnumerable<VariableDeclaratorSyntax> variableDeclarators,
            InvocationExpressionSyntax invocation,
            IMethodSymbol invocationSymbol)
        {
            var receiverType = invocationSymbol.ReceiverType.ToDisplayString();

            if (invocationReceiverTypes.Contains(receiverType))
            {
                return GetPathArgumentsFromInvocation(invocation, invocationSymbol);
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

        private static void AnalyzeLocations(
            CodeBlockAnalysisContext context,
            List<ArgumentSyntax> fileOperationsPathArguments,
            List<Location> secureMethodsLocations,
            List<LocationToAnalyze> locationsToAnalyze)
        {
            var reportedLocations = new HashSet<Location>();

            foreach (var locationToAnalyze in locationsToAnalyze)
            {
                foreach (var pathArgument in fileOperationsPathArguments)
                {
                    HandleReportDiagnosticsByPathArgument(context, secureMethodsLocations, reportedLocations, locationToAnalyze, pathArgument);
                }
            }
        }

        private static void HandleReportDiagnosticsByPathArgument(
            CodeBlockAnalysisContext context, 
            List<Location> secureMethodsLocations, 
            HashSet<Location> reportedLocations, 
            LocationToAnalyze locationToAnalyze, 
            ArgumentSyntax pathArgument)
        {
            var pathArgumentLocation = pathArgument.GetLocation();

            var pathArgumentName = pathArgument.Expression.ToString();

            if (pathArgumentName != locationToAnalyze.PathArgumentName)
            {
                return;
            }

            // Gets the next file operation and checks if there's a secure method in betweeen
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

        private static List<LocationToAnalyze> GetLocationsToAnalyze(
            CodeBlockAnalysisContext context,
            IEnumerable<SyntaxNode> descendantNodes,
            IEnumerable<VariableDeclaratorSyntax> variableDeclarators,
            List<ArgumentSyntax> fileOperationsPathArguments)
        {
            var assignments = descendantNodes.OfType<AssignmentExpressionSyntax>();

            var forEachNodes = descendantNodes.OfType<ForEachStatementSyntax>();

            var inputParameters = context.CodeBlock?
                .FirstAncestorOrSelf<MethodDeclarationSyntax>()?.ParameterList?
                .ChildNodes()?
                .OfType<ParameterSyntax>();

            var locationsToAnalyze = new List<LocationToAnalyze>();

            foreach (var pathArgument in fileOperationsPathArguments)
            {
                var pathArgumentName = pathArgument.Expression?.ToString();

                // Assignments Locations
                locationsToAnalyze.AddRange(
                    assignments
                    .Where(assignment => assignment != null
                        && assignment.GetAssignmentName() == pathArgumentName
                        && !assignment.Right.ToString().Contains("SecurePath.ConstructSecurePath"))
                    .Select(assignment => new LocationToAnalyze(assignment.GetLocation(), pathArgumentName)));

                // VariableDeclarators Locations
                locationsToAnalyze.AddRange(
                   variableDeclarators
                   .Where(variableDeclarator => variableDeclarator != null
                       && (variableDeclarator.Identifier.Text == pathArgumentName || variableDeclarator.Initializer.ToString().Contains(pathArgumentName))
                       && !variableDeclarator.Initializer.ToString().Contains("SecurePath.ConstructSecurePath"))
                   .Select(variableDeclarator => new LocationToAnalyze(variableDeclarator.GetLocation(), pathArgumentName)));

                // Method Arguments
                locationsToAnalyze.AddRange(
                    inputParameters
                    .Where(inputArgument => inputArgument != null
                        && inputArgument.Identifier.ToString() == pathArgumentName)
                    .Select(inputArgument => new LocationToAnalyze(inputArgument.GetLocation(), pathArgumentName)));

                // ForEach Nodes
                locationsToAnalyze.AddRange(
                    forEachNodes
                    .Where(node => pathArgument.GetLocation().IsLocationOverlapping(node.GetLocation()))
                    .Select(node => new LocationToAnalyze(node.GetLocation(), pathArgumentName)));

                // Cases where the path argument was not added to the locations to analyze => assume the first position of the block
                if (!locationsToAnalyze.Exists(locationToAnalyze => locationToAnalyze.PathArgumentName == pathArgumentName))
                {
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
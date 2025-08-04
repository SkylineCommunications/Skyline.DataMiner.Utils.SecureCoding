using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureSerialization.Binary
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BinaryFormatterAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SLC_SC0008";

        public static DiagnosticDescriptor Rule => new DiagnosticDescriptor(
            DiagnosticId,
            "Avoid deserializing data using BinaryFormatter.",
            "BinaryFormatter serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.",
            "Usage",
            DiagnosticSeverity.Warning,
            helpLinkUri: $"https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/{DiagnosticId}.md",
            isEnabledByDefault: true
         );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeBinaryFormatterObjectCreation, SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeBinaryFormatterInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeBinaryFormatterObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var node = (ObjectCreationExpressionSyntax)context.Node;

            var symbol = context.SemanticModel.GetSymbolInfo(node.Type).Symbol as INamedTypeSymbol;
            if (symbol?.ToString() == "System.Runtime.Serialization.Formatters.Binary.BinaryFormatter")
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation()));
            }
        }

        private static void AnalyzeBinaryFormatterInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (symbol?.ContainingType.ToString() == "System.Runtime.Serialization.Formatters.Binary.BinaryFormatter")
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
            }
        }
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureSerialization.Json
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NewtonsoftDeserializationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SLC_SC0004";

        public static DiagnosticDescriptor Rule => new DiagnosticDescriptor(
            DiagnosticId,
            "Avoid deserializing json strings by using Newtonsoft directly.",
            "Avoid deserializing json strings by using Newtonsoft directly.\nConsider using SecureJsonDeserialization.DeserializeObject instead.",
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

            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
        }

        public static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is InvocationExpressionSyntax node))
            {
                return;
            }

            if (!IsJsonConvertDeserialization(node, context))
            {
                return;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rule,
                    node.GetLocation()
                )
            );
        }

        private static bool IsJsonConvertDeserialization(InvocationExpressionSyntax node, SyntaxNodeAnalysisContext context)
        {
            if (!(node.Expression is MemberAccessExpressionSyntax deserializeObjectAccess))
            {
                return false;
            }

            if (!deserializeObjectAccess.Name.Identifier.Text.StartsWith("DeserializeObject"))
            {
                return false;
            }

            INamedTypeSymbol jsonConvertSymbol = context.SemanticModel.GetSymbolInfo(deserializeObjectAccess.Expression).Symbol as INamedTypeSymbol;
            if (jsonConvertSymbol == null || jsonConvertSymbol.Name != "JsonConvert")
            {
                return false;
            }

            string namespaceFullname = jsonConvertSymbol.ContainingNamespace.GetNamespaceFullname();
            if (string.IsNullOrWhiteSpace(namespaceFullname) || namespaceFullname != "Newtonsoft.Json")
            {
                return false;
            }

            return true;
        }
    }
}

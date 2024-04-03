using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers;
using System.Collections.Immutable;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureSerialization.Json
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class JavaScriptSerializerDeserializationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SLC_SC0003";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            new DiagnosticDescriptor(
                DiagnosticId,
                "Avoid using the JavaScriptSerializer for (de)serialization.",
                "Avoid using the JavaScriptSerializer for (de)serialization.\nMicrosoft recommends to use System.Text.Json or Newtonsoft.Json instead.",
                "Usage",
                DiagnosticSeverity.Warning,
                helpLinkUri: "https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/SLC-SC0003.md",
                isEnabledByDefault: true
            )
        );

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeInvoccationExpression, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreationExpression, SyntaxKind.ObjectCreationExpression);
        }

        public void AnalyzeInvoccationExpression(SyntaxNodeAnalysisContext context)
        {

            if (!(context.Node is InvocationExpressionSyntax node))
            {
                return;
            }

            if (!IsJavaScriptSerializerSerializationInvocation(node, context))
            {
                return;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    SupportedDiagnostics[0],
                    node.GetLocation()
                )
            );
        }

        public void AnalyzeObjectCreationExpression(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is ObjectCreationExpressionSyntax node))
            {
                return;
            }

            ISymbol javaScriptSerializerSymbol = context.SemanticModel.GetSymbolInfo(node).Symbol;
            if (javaScriptSerializerSymbol == null  || node.Type.ToString() != "JavaScriptSerializer")
            {
                return;
            }

            string namespaceFullName = javaScriptSerializerSymbol.ContainingNamespace.GetNamespaceFullname();
            if (string.IsNullOrWhiteSpace(namespaceFullName) || namespaceFullName != "System.Web.Script.Serialization")
            {
                return;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    SupportedDiagnostics[0],
                    node.GetLocation()
                )
            );
        }

        private bool IsJavaScriptSerializerSerializationInvocation(InvocationExpressionSyntax node, SyntaxNodeAnalysisContext context)
        {
            if (!(node.Expression is MemberAccessExpressionSyntax deserializeAccess))
            {
                return false;
            }

            string invokedMethod = deserializeAccess.Name.Identifier.Text;
            if (string.IsNullOrWhiteSpace(invokedMethod) || (!invokedMethod.Contains("Serialize") && !invokedMethod.Contains("Deserialize")) )
            {
                return false;
            }

            ISymbol javaScriptSerializerSymbol = context.SemanticModel.GetSymbolInfo(deserializeAccess).Symbol;
            if (javaScriptSerializerSymbol == null)
            {
                return false;
            }

            string namespaceFullName = javaScriptSerializerSymbol.ContainingNamespace.GetNamespaceFullname();
            if (string.IsNullOrWhiteSpace(namespaceFullName) || namespaceFullName != "System.Web.Script.Serialization")
            {
                return false;
            }

            return true;
        }
    }
}

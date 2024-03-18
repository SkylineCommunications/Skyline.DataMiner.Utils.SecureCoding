using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureSerialization.Json
{
    public class NewtonsoftDeserializationAnalyzer : ACustomDiagnosticAnalyzer
    {
        public const string DiagnosticId = "NewtonsoftDeserializationUsage";

        public override DiagnosticDescriptor Rule => new DiagnosticDescriptor(
            DiagnosticId,
            "Avoid deserializing json strings by using Newtonsoft directly.",
            "Avoid deserializing json strings by using Newtonsoft directly.\nConsider using SecureJsonDeserialization.DeserializeObject instead.",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true
         );

        public override void Analyze(SyntaxNodeAnalysisContext context)
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

        private bool IsJsonConvertDeserialization(InvocationExpressionSyntax node, SyntaxNodeAnalysisContext context)
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

            string namespaceFullname = GetNamespaceFullname(jsonConvertSymbol.ContainingNamespace);
            if (namespaceFullname == null || namespaceFullname != "Newtonsoft.Json")
            {
                return false;
            }

            return true;
        }
    }
}

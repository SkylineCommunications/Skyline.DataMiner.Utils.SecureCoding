using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Text;

namespace Analyzers.SecureSerialization.Json
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class JavaScriptSerializerDesializationAnalyzer : DiagnosticAnalyzer
    {

        public const string DiagnosticId = "JavaScriptSerializerUsage";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            new DiagnosticDescriptor(
                DiagnosticId,
                "Avoid using the JavaScriptSerializer for (de)serialization.",
                "Avoid using the JavaScriptSerializer for (de)serialization.\nMicrosoft recommends to use System.Text.Json or Newtonsoft.Json instead.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true
            )
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeInvoccationExpression, SyntaxKind.InvocationExpression);
            //context.RegisterSyntaxNodeAction(AnalyzeObjectCreationExpression, SyntaxKind.ObjectCreationExpression);
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

            ITypeSymbol javaScriptSerializerSymbol = context.SemanticModel.GetSymbolInfo(node).Symbol as ITypeSymbol;
            if (javaScriptSerializerSymbol == null || javaScriptSerializerSymbol.Name != "JavaScriptSerializer")
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
                return true;
            }

            string invokedMethod = deserializeAccess.Name.Identifier.Text;
            if (string.IsNullOrWhiteSpace(invokedMethod) || !invokedMethod.StartsWith("Serialize") || !invokedMethod.StartsWith("Deserialize"))
            {
                return true;
            }

            ISymbol javaScriptSerializerSymbol = context.SemanticModel.GetSymbolInfo(deserializeAccess.Expression).Symbol;
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

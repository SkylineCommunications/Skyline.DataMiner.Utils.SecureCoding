using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureSerialization.Json
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NewtonsoftDeserializationAnalyzer : DiagnosticAnalyzer
    {
        private static readonly HashSet<string> insecureTypeNames = new HashSet<string>()
        {
            "All", "Auto", "Objects", "Arrays"
        };

        public const string DiagnosticId = "SLC_SC0004";

        public static DiagnosticDescriptor DeserializationRule => new DiagnosticDescriptor(
            DiagnosticId,
            "Avoid deserializing json strings by using Newtonsoft directly.",
            "Consider using SecureNewtonsoftDeserialization.DeserializeObject instead. These secure methods are available in the Skyline.DataMiner.Utils.SecureCoding NuGet package.",
            "Usage",
            DiagnosticSeverity.Warning,
            helpLinkUri: $"https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/{DiagnosticId}.md",
            isEnabledByDefault: true
         );

        public static DiagnosticDescriptor DefaultSettingsRule => new DiagnosticDescriptor(
            DiagnosticId,
            "Insecure TypeNameHandling Usage Without SerializationBinder",
            "Setting TypeNameHandling to '{0}' at the global level is insecure and may lead to remote code execution during deserialization",
            "Usage",
            DiagnosticSeverity.Warning,
            helpLinkUri: $"https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/{DiagnosticId}.md",
            isEnabledByDefault: true
         );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(DeserializationRule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeJsonConvertInvocation, SyntaxKind.InvocationExpression);

            context.RegisterSyntaxNodeAction(AnalyzeJsonConvertAssignment, SyntaxKind.SimpleAssignmentExpression);
        }

        private static void AnalyzeJsonConvertAssignment(SyntaxNodeAnalysisContext context)
        {
            var assignment = (AssignmentExpressionSyntax)context.Node;
            if (assignment == null)
            {
                return;
            }

            // Match JsonConvert.DefaultSettings = ...
            if (!assignment.Left.ToString().Contains("JsonConvert.DefaultSettings"))
            {
                return;
            }

            // Look for lambda or method returning JsonSerializerSettings
            if (!(assignment.Right is ParenthesizedLambdaExpressionSyntax lambda))
            {
                return;
            }

            var defaultSettings = GetDefaultSettings(lambda);
            if (defaultSettings?.Initializer == null)
            {
                return;
            }

            foreach (var expr in defaultSettings.Initializer.Expressions.OfType<AssignmentExpressionSyntax>())
            {
                if (expr.Left is IdentifierNameSyntax leftName &&
                    leftName.Identifier.Text == "TypeNameHandling" &&
                    expr.Right is MemberAccessExpressionSyntax rightMember)
                {
                    var typeName = rightMember.Name.Identifier.Text;

                    if (insecureTypeNames.Contains(typeName))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(DefaultSettingsRule, expr.GetLocation(), typeName));
                    }
                }
            }
        }

        private static void AnalyzeJsonConvertInvocation(SyntaxNodeAnalysisContext context)
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
                    DeserializationRule,
                    node.GetLocation()
                )
            );
        }

        private static ObjectCreationExpressionSyntax GetDefaultSettings(ParenthesizedLambdaExpressionSyntax lambda)
        {
            if (lambda.Body is ObjectCreationExpressionSyntax creation)  // Handle: () => new JsonSerializerSettings { ... }
            {
                return creation;
            }
            else if (lambda.Body is BlockSyntax block) // Handle: () => { return new JsonSerializerSettings { ... }; }
            {
                return block.DescendantNodes()
                    .OfType<ObjectCreationExpressionSyntax>()
                    .FirstOrDefault(expr => expr.Type.ToString().Contains("JsonSerializerSettings"));
            }
            else
            {
                return null;
            }
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.Certificates
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CertificateCallbackAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SLC_SC0005";

        public static DiagnosticDescriptor Rule => new DiagnosticDescriptor(
            DiagnosticId,
            title: "Certificate callbacks should not always evaluate to true",
            messageFormat: "Certificate callbacks should be overridden with specific validation criteria",
            "Usage",
            DiagnosticSeverity.Warning,
            helpLinkUri: $"https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/{DiagnosticId}.md",
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeUsages, SyntaxKind.SimpleAssignmentExpression);
        }

        public static void AnalyzeUsages(SyntaxNodeAnalysisContext context)
        {
            var assignmentExpression = (AssignmentExpressionSyntax)context.Node;

            if (assignmentExpression.Left is MemberAccessExpressionSyntax memberAccess
                && memberAccess.Name.Identifier.Text == "ServerCertificateCustomValidationCallback")
            {
                AnalyzeParenthesizedLambdaExpression(context, assignmentExpression, memberAccess);

                if (assignmentExpression.Right is IdentifierNameSyntax identifier)
                {
                    AnalyzeIdentifierName(context, identifier);
                }
            }
        }

        private static void AnalyzeIdentifierName(SyntaxNodeAnalysisContext context, IdentifierNameSyntax identifier)
        {
            var methodSymbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                return;
            }

            var methodSyntax = methodSymbol.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken) as MethodDeclarationSyntax;
            if (methodSyntax == null)
            {
                return;
            }

            var alwaysReturnsTrue = methodSyntax.Body != null
                && methodSyntax.Body.Statements.OfType<ReturnStatementSyntax>()
                .All(r => r.Expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.TrueLiteralExpression));

            if (alwaysReturnsTrue)
            {
                var diagnostic = Diagnostic.Create(Rule, identifier.GetLocation(), methodSymbol.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzeParenthesizedLambdaExpression(
            SyntaxNodeAnalysisContext context,
            AssignmentExpressionSyntax assignmentExpression,
            MemberAccessExpressionSyntax memberAccess)
        {
            if (!(assignmentExpression.Right is ParenthesizedLambdaExpressionSyntax lambda))
            {
                return;
            }

            var alwaysReturnsTrue = lambda.Body is BlockSyntax block
                && block.Statements.OfType<ReturnStatementSyntax>()
                .All(r => r.Expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.TrueLiteralExpression));

            if (alwaysReturnsTrue)
            {
                var diagnostic = Diagnostic.Create(Rule, memberAccess.Name.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
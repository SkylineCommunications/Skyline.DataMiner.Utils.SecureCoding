using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureIO;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureReflection;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Skyline.DataMiner.Utils.SecureCoding.CodeFixProviders.SecureReflection
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SecureAssemblyLoaderCodeFix)), Shared]
    public class SecureAssemblyLoaderCodeFix : CodeFixProvider
    {
        private const string SECURE_REFLECTION_NAMESPACE = "Skyline.DataMiner.Utils.SecureCoding.SecureReflection";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(SecureAssemblyLoadAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var invocation = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();
            if (invocation == null)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Replace Assembly.Load with SecureAssembly.Load",
                    createChangedDocument: c => ReplaceAssemblyLoadWithSecureAssemblyLoad(context.Document, invocation, c),
                    equivalenceKey: nameof(SecureAssemblyLoaderCodeFix)),
                diagnostic);
        }

        private async Task<Document> ReplaceAssemblyLoadWithSecureAssemblyLoad(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            InvocationExpressionSyntax newInvocation = GetNewInvocation(invocation);

            var newRoot = root.ReplaceNode(invocation, newInvocation.WithTriviaFrom(invocation)); // Keep original form

            var compilationUnit = newRoot as CompilationUnitSyntax ?? (CompilationUnitSyntax)root;

            if (!compilationUnit.Usings.Any(@using => @using.Name.ToString() == SECURE_REFLECTION_NAMESPACE))
            {
                compilationUnit = compilationUnit.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(SECURE_REFLECTION_NAMESPACE)));
            }

            var newDocument = document.WithSyntaxRoot(compilationUnit);

            return newDocument;
        }

        private static InvocationExpressionSyntax GetNewInvocation(InvocationExpressionSyntax invocation)
        {
            return invocation
                   .WithExpression(SyntaxFactory.ParseExpression("SecureAssembly.Load"))
                   .WithArgumentList(invocation.ArgumentList);
        }
    }
}

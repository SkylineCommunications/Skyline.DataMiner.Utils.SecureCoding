using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureIO;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Skyline.DataMiner.Utils.SecureCoding.CodeFixProviders.SecureIO
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PathCombineCodeFixProvider)), Shared]
    public class PathCombineCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(PathCombineAnalyzer.DiagnosticId); }
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

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Replace SecurePath.ConstructSecurePath by System.IO.Path.Combine",
                    createChangedDocument: c => ReplacePathCombineWithSecurePath(context.Document, invocation, c),
                    equivalenceKey: nameof(PathCombineCodeFixProvider)),
                diagnostic);
        }

        private async Task<Document> ReplacePathCombineWithSecurePath(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Replace System.IO.Path.Combine with SecurePath.ConstructSecurePath
            var newInvocation = SyntaxFactory.ParseExpression("Skyline.DataMiner.Utils.Security.SecureIO.SecurePath.ConstructSecurePath")
                .WithTriviaFrom(
                    invocation.WithArgumentList(invocation.ArgumentList));

            var newRoot = root.ReplaceNode(invocation, newInvocation);
            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument;
        }
    }
}

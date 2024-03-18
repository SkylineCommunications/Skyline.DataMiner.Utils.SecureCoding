using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureIO;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Skyline.DataMiner.Utils.SecureCoding.CodeFixProviders.SecureIO
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PathCombineCodeFix)), Shared]
    public class PathCombineCodeFix : CodeFixProvider
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
                    title: "Replace Path.Combine into SecurePath.ConstructSecurePath",
                    createChangedDocument: c => ReplacePathCombineWithConstructSecurePath(context.Document, invocation, c),
                    equivalenceKey: nameof(PathCombineCodeFix)),
                diagnostic);
        }

        private async Task<Document> ReplacePathCombineWithConstructSecurePath(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            InvocationExpressionSyntax newInvocation = GetNewInvocation(invocation);

            var newRoot = root.ReplaceNode(invocation, newInvocation.WithTriviaFrom(invocation)); // Keep original form

            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument;
        }

        private static InvocationExpressionSyntax GetNewInvocation(InvocationExpressionSyntax invocation)
        {
            if (invocation.ArgumentList.Arguments.Count < 2)
            {
                return invocation;
            }
            else if (invocation.ArgumentList.Arguments.Count > 2)
            {
                return invocation
                    .WithExpression(SyntaxFactory.ParseExpression("Skyline.DataMiner.Utils.Security.SecureIO.SecurePath.ConstructSecurePath"))
                    .WithArgumentList(invocation.ArgumentList);
            }
            else
            {
                var lastArgument = invocation.ArgumentList.Arguments.Last().ToString();

                if (lastArgument.Contains("/") || lastArgument.Contains("\\"))
                {
                    return invocation
                        .WithExpression(SyntaxFactory.ParseExpression("Skyline.DataMiner.Utils.Security.SecureIO.SecurePath.ConstructSecurePathWithSubDirectories"))
                        .WithArgumentList(invocation.ArgumentList);
                }

                return invocation
                    .WithExpression(SyntaxFactory.ParseExpression("Skyline.DataMiner.Utils.Security.SecureIO.SecurePath.ConstructSecurePath"))
                    .WithArgumentList(invocation.ArgumentList);
            }
        }
    }
}

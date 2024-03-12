namespace Skyline.DataMiner.Utils.SecureCoding.CodeFixProviders
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureIO;
    using Skyline.DataMiner.Utils.SecureCoding.SecureIO.CodeFixProviders;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading.Tasks;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CodeFixProviders)), Shared]
    public class CodeFixProviders : CodeFixProvider
    {
        private static readonly List<ACustomCodeFixProvider> codeFixes =
            new List<ACustomCodeFixProvider>
            {
                new PathCombineCodeFixProvider(),
            };

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.CreateRange(codeFixes.Select(codeFix => codeFix.DiagnosticId)); }
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

            var invocationExpression = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

            codeFixes.ForEach(
                codeFix => context.RegisterCodeFix(
                    CodeAction.Create(
                        title: codeFix.Title,
                        createChangedDocument: c => codeFix.ReplaceMethod(context.Document, invocationExpression, c),
                        equivalenceKey: codeFix.Title),
                    diagnostic));
        }
    }
}

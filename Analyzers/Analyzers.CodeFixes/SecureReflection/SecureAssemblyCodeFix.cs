using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureReflection;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Skyline.DataMiner.Utils.SecureCoding.CodeFixProviders.SecureReflection
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SecureAssemblyCodeFix)), Shared]
    public class SecureAssemblyCodeFix : CodeFixProvider
    {
        private const string SECURE_REFLECTION_NAMESPACE = "Skyline.DataMiner.Utils.SecureCoding.SecureReflection";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(SecureAssemblyAnalyzer.DiagnosticId); }
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

            if (TryGetCodeAction(context, invocation, out CodeAction codeAction))
            {
                context.RegisterCodeFix(codeAction, diagnostic);
            }
        }

        private bool TryGetCodeAction(CodeFixContext context, InvocationExpressionSyntax invocation, out CodeAction codeAction)
        {
            var methodSymbol = GetAssemblyMethodSymbol(context, invocation);
            if (methodSymbol == null)
            {
                codeAction = default;
                return false;
            }

            if (nameof(System.Reflection.Assembly.LoadFrom).Contains(methodSymbol.Name))
            {
                codeAction = CodeAction.Create(
                    title: "Replace 'Assembly.LoadFrom' with 'SecureAssembly.LoadFrom'",
                    createChangedDocument: c => ReplaceAssemblyLoadSecureAssemblyLoad(context.Document, invocation, "SecureAssembly.LoadFrom", c),
                    equivalenceKey: nameof(SecureAssemblyCodeFix));

                return true;
            }
            else if (nameof(System.Reflection.Assembly.LoadFile).Contains(methodSymbol.Name))
            {
                codeAction = CodeAction.Create(
                    title: "Replace 'Assembly.LoadFile' with 'SecureAssembly.LoadFile'",
                    createChangedDocument: c => ReplaceAssemblyLoadSecureAssemblyLoad(context.Document, invocation, "SecureAssembly.LoadFile", c),
                    equivalenceKey: nameof(SecureAssemblyCodeFix));

                return true;
            }
            else
            {
                codeAction = default;
                return false;
            }
        }

        private static IMethodSymbol GetAssemblyMethodSymbol(CodeFixContext context, InvocationExpressionSyntax invocation)
        {
            var semanticModel = context.Document.GetSemanticModelAsync(context.CancellationToken)?.Result;
            if (semanticModel == null)
            {
                return null;
            }

            var methodSymbol = semanticModel?.GetSymbolInfo(invocation.Expression, context.CancellationToken).Symbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                return null;
            }

            if (!methodSymbol.ReceiverType.ToDisplayString().Contains("System.Reflection.Assembly"))
            {
                return null;
            }

            return methodSymbol;
        }

        private async Task<Document> ReplaceAssemblyLoadSecureAssemblyLoad(
            Document document,
            InvocationExpressionSyntax invocation,
            string secureAssemblyMethod,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            InvocationExpressionSyntax newInvocation = GetNewInvocation(invocation, secureAssemblyMethod);

            var newRoot = root.ReplaceNode(invocation, newInvocation.WithTriviaFrom(invocation)); // Keep original form

            var compilationUnit = newRoot as CompilationUnitSyntax ?? (CompilationUnitSyntax)root;

            if (!compilationUnit.Usings.Any(@using => @using.Name.ToString() == SECURE_REFLECTION_NAMESPACE))
            {
                compilationUnit = compilationUnit.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(SECURE_REFLECTION_NAMESPACE)));
            }

            var newDocument = document.WithSyntaxRoot(compilationUnit);

            return newDocument;
        }

        private static InvocationExpressionSyntax GetNewInvocation(InvocationExpressionSyntax invocation, string secureAssemblyMethod)
        {
            var arguments = invocation.ArgumentList
                .AddArguments(SyntaxFactory.Argument(SyntaxFactory.ParseExpression("allowedCertificates")));

            return invocation
                   .WithExpression(SyntaxFactory.ParseExpression(secureAssemblyMethod))
                   .WithArgumentList(arguments);
        }
    }
}
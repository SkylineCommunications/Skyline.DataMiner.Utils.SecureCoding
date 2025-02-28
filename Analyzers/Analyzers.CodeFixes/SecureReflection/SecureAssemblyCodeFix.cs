using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureReflection;
using Skyline.DataMiner.Utils.SecureCoding.SecureReflection;
using System;
using System.Collections.Generic;
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
        private static string[] requiredNamespaces = new string[]
        {
            "System.Security.Cryptography.X509Certificates",
            "Skyline.DataMiner.Utils.SecureCoding.SecureReflection",
        };

        private static Dictionary<string, string> defaultCertificateArguments = new Dictionary<string, string>
        {
            { "X509Certificate2", "default(X509Certificate2)" },
            { "IEnumerable<X509Certificate2>", "default(IEnumerable<X509Certificate2>)" },
            { "Certificate path", "default(string)" },
            { "Certificate paths", "default(string[])" },
            { "byte[]", "default(byte[])" },
            { "IEnumerable<byte[]>", "default(IEnumerable<byte[]>)" }
        };


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

            if (!TryGetNestedCodeAction(context, invocation, out CodeAction[] nestedCodeActions))
            {
                return;
            }

            var codeAction = CodeActionWithOptions.Create(
                title: "Replace with secure assembly loading",
                nestedActions: ImmutableArray.Create<CodeAction>(nestedCodeActions),
                isInlinable: false);

            context.RegisterCodeFix(codeAction, diagnostic);
        }

        private bool TryGetNestedCodeAction(CodeFixContext context, InvocationExpressionSyntax invocation, out CodeAction[] nestedCodeActionsArr)
        {
            var nestedCodeActions = new List<CodeAction>();

            var methodSymbol = GetAssemblyMethodSymbol(context, invocation);
            if (methodSymbol == null)
            {
                nestedCodeActionsArr = new CodeAction[0];
                return false;
            }

            if (methodSymbol.Name == nameof(System.Reflection.Assembly.LoadFrom))
            {
                AddNestedLoadFromCodeActions(context, invocation, nameof(SecureAssembly.LoadFrom), nestedCodeActions);
            }

            if (methodSymbol.Name == nameof(System.Reflection.Assembly.LoadFile))
            {
                AddNestedLoadFromCodeActions(context, invocation, nameof(SecureAssembly.LoadFile), nestedCodeActions);
            }

            nestedCodeActionsArr = nestedCodeActions.ToArray();
            return nestedCodeActionsArr.Length > 0;
        }

        private void AddNestedLoadFromCodeActions(
            CodeFixContext context,
            InvocationExpressionSyntax invocation,
            string method,
            List<CodeAction> nestedCodeActions)
        {
            var secureAssemblyMethod = $"SecureAssembly.{method}";

            foreach (var defaultCertificateArg in defaultCertificateArguments)
            {
                var title = $"'{secureAssemblyMethod}' ({defaultCertificateArg.Key} load option)";
                var argument = defaultCertificateArg.Value;

                nestedCodeActions.Add(CodeAction.Create(
                    title,
                    createChangedDocument: c => ReplaceAssemblyLoadSecureAssemblyLoad(context.Document, invocation, secureAssemblyMethod, argument, c),
                    equivalenceKey: nameof(SecureAssemblyCodeFix)));
            }
        }

        private static IMethodSymbol GetAssemblyMethodSymbol(CodeFixContext context, InvocationExpressionSyntax invocation)
        {
            var semanticModel = context.Document.GetSemanticModelAsync(context.CancellationToken)?.Result;
            if (semanticModel == null)
            {
                return null;
            }

            var methodSymbol = semanticModel.GetSymbolInfo(invocation.Expression, context.CancellationToken).Symbol as IMethodSymbol;
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
            string defaultCertificateArgument,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            InvocationExpressionSyntax newInvocation = GetNewInvocation(invocation, secureAssemblyMethod, defaultCertificateArgument);

            var newRoot = root.ReplaceNode(invocation, newInvocation.WithTriviaFrom(invocation)); // Keep original form

            var compilationUnit = newRoot as CompilationUnitSyntax ?? (CompilationUnitSyntax)root;

            var missingNamespaces = requiredNamespaces
                .Where(ns => !compilationUnit.Usings.Any(u => u.Name.ToString() == ns))
                .Select(ns => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns)))
                .ToArray();

            if (missingNamespaces.Length > 0)
            {
                compilationUnit = compilationUnit.AddUsings(missingNamespaces);
            }

            var newDocument = document.WithSyntaxRoot(compilationUnit);

            return newDocument;
        }

        private static InvocationExpressionSyntax GetNewInvocation(InvocationExpressionSyntax invocation, string secureAssemblyMethod, string defaultCertificateArgument)
        {
            var arguments = invocation.ArgumentList
                .AddArguments(SyntaxFactory.Argument(SyntaxFactory.ParseExpression(defaultCertificateArgument)));

            return invocation
                   .WithExpression(SyntaxFactory.ParseExpression(secureAssemblyMethod))
                   .WithArgumentList(arguments);
        }
    }
}
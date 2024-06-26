﻿using Microsoft.CodeAnalysis.CodeFixes;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureSerialization.Json;
using Microsoft.CodeAnalysis;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CodeActions;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;

namespace Skyline.DataMiner.Utils.SecureCoding.CodeFixProviders.SecureSerialization.Json
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NewtonsoftDeserializationCodeFixProvider)), Shared]
    public class NewtonsoftDeserializationCodeFixProvider : CodeFixProvider
    {
        private const string secureDeserializationNamespace = "Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft";

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(NewtonsoftDeserializationAnalyzer.DiagnosticId); }
        }

        public const string SecureDeserializationFixEquivalenceKey = "SecureDeserializationFix";
        public const string KnownTypesSecureDeserializationFixEquivalenceKey = "KnownTypesSecureDeserializationFix";

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            Diagnostic diagnostic = context.Diagnostics.First();
            TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

            InvocationExpressionSyntax invocation = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Replace by SecureNewtonsoftDeserialization.DeserializeObject",
                    createChangedDocument: c => ReplaceDeserializeObject(context.Document, invocation, false, c),
                    SecureDeserializationFixEquivalenceKey),
                diagnostic);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Replace by SecureNewtonsoftDeserialization.DeserializeObject with a list of known types",
                    createChangedDocument: c => ReplaceDeserializeObject(context.Document, invocation, true, c),
                    KnownTypesSecureDeserializationFixEquivalenceKey),
                diagnostic);
        }

        private async Task<Document> ReplaceDeserializeObject(Document document, InvocationExpressionSyntax invocation, bool withKnoownTypesList, CancellationToken cancellationToken)
        {
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            InvocationExpressionSyntax newInvocation = await GetNewDeserializeObjectInvocation(document, invocation, withKnoownTypesList, cancellationToken);
            SyntaxNode codeFixRoot = root.ReplaceNode(invocation, newInvocation.WithTriviaFrom(invocation)); // Keep original form
            codeFixRoot = AddUsingDirectiveIfNecessary(codeFixRoot);
     
            Document newDocument = document.WithSyntaxRoot(codeFixRoot);
            return newDocument;
        }

        private async Task<InvocationExpressionSyntax> GetNewDeserializeObjectInvocation(Document document, InvocationExpressionSyntax invocation, bool withKnownTypesList, CancellationToken cancellationToken)
        {
            SemanticModel model = await document.GetSemanticModelAsync(cancellationToken);
            SeparatedSyntaxList<ArgumentSyntax> args = invocation.ArgumentList.Arguments;
            ArgumentSyntax jsonStringArgument = null;
            ArgumentSyntax settingsArgument = null;

            foreach (var arg in args)
            {
                ITypeSymbol typeSymbol = model.GetTypeInfo(arg.ChildNodes().First(), cancellationToken).Type;
                string typeFullName = $"{GetNamespaceFullname(typeSymbol.ContainingNamespace)}.{typeSymbol.Name}";
                if (typeFullName == "System.String")
                {
                    jsonStringArgument = arg;
                }
                else if (typeFullName == "Newtonsoft.Json.JsonSerializerSettings")
                {
                    settingsArgument = arg;
                }
            }

            ArgumentListSyntax arguments = SyntaxFactory.ArgumentList();
            arguments = arguments.AddArguments(jsonStringArgument);

            if (withKnownTypesList)
            {
                arguments = arguments.AddArguments(SyntaxFactory.Argument(SyntaxFactory.ParseExpression("new List<Type>() {}")));
            }

            if (settingsArgument != null)
            {
                arguments = arguments.AddArguments(settingsArgument);
            }

            string genericType = "T";
            if (model.GetSymbolInfo(invocation).Symbol is IMethodSymbol methodSymbol && methodSymbol.TypeArguments.Length > 0)
            {
                genericType = methodSymbol.TypeArguments.First().ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            }

            return invocation
                .WithExpression(SyntaxFactory.ParseExpression($"SecureNewtonsoftDeserialization.DeserializeObject<{genericType}>"))
                .WithArgumentList(arguments);
        }

        private SyntaxNode AddUsingDirectiveIfNecessary(SyntaxNode root)
        {
            CompilationUnitSyntax newRoot = root as CompilationUnitSyntax;
            if(newRoot != null && !newRoot.Usings.Any(u => u.Name.ToString() == secureDeserializationNamespace))
            {
                newRoot = newRoot.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(secureDeserializationNamespace)));
            }

            if(newRoot != null && !newRoot.Usings.Any(u => u.Name.ToString() == "System.Collections.Generic")) 
            {
                newRoot = newRoot.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Collections.Generic")));
            }

            return newRoot;
        }

        private string GetNamespaceFullname(INamespaceSymbol ns)
        {
            if (ns == null)
            {
                return null;
            }

            if (ns.IsGlobalNamespace)
            {
                return "";
            }

            if (ns.ContainingNamespace.IsGlobalNamespace)
            {
                return ns.Name;
            }

            return GetNamespaceFullname(ns.ContainingNamespace) + "." + ns.Name;
        }
    }
}

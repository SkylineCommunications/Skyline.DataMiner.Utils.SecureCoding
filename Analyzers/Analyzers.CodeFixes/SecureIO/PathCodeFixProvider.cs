namespace Skyline.DataMiner.Utils.SecureCoding.SecureIO.CodeFixProviders
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;
    using Microsoft.CodeAnalysis.Simplification;
    using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureIO;
    using Skyline.DataMiner.Utils.SecureCoding.CodeFixProviders;
    using System;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PathCombineCodeFixProvider)), Shared]
    public class PathCombineCodeFixProvider : ACustomCodeFixProvider
    {
        public override string Title => "Replace with SecureIO.ConstructSecurePath";
        private const string AssemblyName = "SecureCodingAnalyzers"; // Adjust to your assembly name
        private const string SecureIOTypeName = "Skyline.DataMiner.Utils.Security.IO.SecureIO";

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

            var invocationExpression = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: c => ReplaceMethod(context.Document, invocationExpression, c),
                    equivalenceKey: Title),
                diagnostic);
        }

        public override async Task<Document> ReplaceMethod(
            Document document,
            InvocationExpressionSyntax invocationExpression,
            CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            var assembly = LoadAssembly(AssemblyName);

            if (assembly != null)
            {
                var secureIOType = assembly.GetType(SecureIOTypeName);

                if (secureIOType != null)
                {
                    var secureIOConstructSecurePath = invocationExpression.ArgumentList.Arguments.Count > 2
                        ? secureIOType.GetMethod("ConstructSecurePath", new Type[] { typeof(bool), typeof(string[]) })
                        : secureIOType.GetMethod("ConstructSecurePath", new Type[] { typeof(string), typeof(string), typeof(bool) });

                    if (secureIOConstructSecurePath != null)
                    {
                        var syntaxGenerator = SyntaxGenerator.GetGenerator(document);

                        var secureIOTypeSyntax = SyntaxFactory.QualifiedName(
                            SyntaxFactory.QualifiedName(
                                SyntaxFactory.QualifiedName(
                                    SyntaxFactory.IdentifierName("Skyline"),
                                    SyntaxFactory.IdentifierName("DataMiner")),
                                SyntaxFactory.IdentifierName("Utils")),
                            SyntaxFactory.IdentifierName("Security"));

                        var newExpression = syntaxGenerator
                            .InvocationExpression(
                                syntaxGenerator.MemberAccessExpression(
                                    secureIOTypeSyntax,
                                    syntaxGenerator.IdentifierName("ConstructSecurePath")),
                                invocationExpression.ArgumentList.Arguments.Select(arg => arg.Expression))
                            .WithAdditionalAnnotations(Simplifier.Annotation);

                        editor.ReplaceNode(invocationExpression, newExpression);
                    }
                }
            }

            return editor.GetChangedDocument();
        }

        private Assembly LoadAssembly(string assemblyName)
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                return assemblies.FirstOrDefault(assembly => assembly.GetName().Name == assemblyName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading assembly {assemblyName}: {ex.Message}");
                return null;
            }
        }
    }
}

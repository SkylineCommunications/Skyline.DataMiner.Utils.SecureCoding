using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Skyline.DataMiner.Utils.SecureCoding.CodeFixProviders
{
    public abstract class ACustomCodeFixProvider
    {
        public abstract string Title { get; }

        public abstract string DiagnosticId { get; }

        public abstract FixAllProvider GetFixAllProvider();

        public abstract Task RegisterCodeFixesAsync(CodeFixContext context);

        public abstract Task<Document> ReplaceMethod(
            Document document,
            InvocationExpressionSyntax invocationExpression,
            CancellationToken cancellationToken);
    }
}

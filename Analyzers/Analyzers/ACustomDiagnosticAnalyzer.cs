namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    public abstract class ACustomDiagnosticAnalyzer
    {
        public abstract DiagnosticDescriptor Rule { get; }

        public abstract void AnalyzeNode(SyntaxNodeAnalysisContext context);

        private protected string GetNamespaceFullname(INamespaceSymbol ns)
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

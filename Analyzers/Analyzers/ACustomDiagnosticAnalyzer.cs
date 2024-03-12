namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    public abstract class ACustomDiagnosticAnalyzer
    {
        public abstract DiagnosticDescriptor Rule { get; }

        public abstract void AnalyzeNode(SyntaxNodeAnalysisContext context);
    }
}

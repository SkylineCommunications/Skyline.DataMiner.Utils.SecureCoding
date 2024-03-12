using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureIO;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Analyzers : DiagnosticAnalyzer
    {
        private static readonly List<ACustomDiagnosticAnalyzer> analyzers = 
            new List<ACustomDiagnosticAnalyzer>
            {
                new PathCombineAnalyzer(),
                new FileOperationAnalyzer(),
            };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.CreateRange(analyzers.Select(analyzer => analyzer.Rule));
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            foreach (var analyzer in analyzers)
            {
                context.RegisterSyntaxNodeAction(analyzer.AnalyzeNode, SyntaxKind.InvocationExpression);
            }
        }
    }
}

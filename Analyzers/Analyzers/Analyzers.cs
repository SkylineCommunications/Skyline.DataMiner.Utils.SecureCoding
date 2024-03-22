using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureIO;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureSerialization.Json;
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
                // IO
                new PathCombineAnalyzer(),
                //new FileOperationAnalyzer(),

                // Serialization
                new NewtonsoftDeserializationAnalyzer(),
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
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            foreach (var analyzer in analyzers)
            {
                context.RegisterSyntaxNodeAction(analyzer.Analyze, SyntaxKind.InvocationExpression);
            }
        }
    }
}

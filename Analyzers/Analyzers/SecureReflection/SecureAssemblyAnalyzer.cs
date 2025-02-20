using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureReflection
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SecureAssemblyAnalyzer : DiagnosticAnalyzer
    {
        private static List<string> loadMethods = new List<string>
        {
            nameof(System.Reflection.Assembly.Load),
            nameof(System.Reflection.Assembly.LoadFile),
            nameof(System.Reflection.Assembly.LoadFrom),
        };

        public const string DiagnosticId = "SLC_SC0006";

        public static DiagnosticDescriptor RuleInsecureLoadFromAssembly => new DiagnosticDescriptor(
            DiagnosticId,
            title: "Ensure secure loading of Assemblies",
            messageFormat: "Consider using 'SecureAssembly.LoadFrom' instead of 'Assembly.LoadFrom'",
            "Usage",
            DiagnosticSeverity.Warning,
            helpLinkUri: $"https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/{DiagnosticId}.md",
            isEnabledByDefault: true);

        public static DiagnosticDescriptor RuleInsecureLoadFileAssembly => new DiagnosticDescriptor(
            DiagnosticId,
            title: "Ensure secure loading of Assemblies",
            messageFormat: "Consider using 'SecureAssembly.LoadFile' instead of 'Assembly.LoadFile'",
            "Usage",
            DiagnosticSeverity.Warning,
            helpLinkUri: $"https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/{DiagnosticId}.md",
            isEnabledByDefault: true);

        public static DiagnosticDescriptor RuleInsecureLoadAssembly => new DiagnosticDescriptor(
            DiagnosticId,
            title: "Ensure secure loading of Assemblies",
            messageFormat: "Consider using either 'SecureAssembly.LoadFile' or 'SecureAssembly.LoadFrom' instead of 'Assembly.Load'",
            "Usage",
            DiagnosticSeverity.Warning,
            helpLinkUri: $"https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/{DiagnosticId}.md",
            isEnabledByDefault: true);

        public static DiagnosticDescriptor RuleBypassCertificateChain => new DiagnosticDescriptor(
            DiagnosticId,
            title: "Ensure secure loading of assemblies with certificate validation",
            messageFormat: "Caution: Disabling verifyCertificateChain (false) bypasses trust chain certificate validation",
            "Usage",
            DiagnosticSeverity.Warning,
            helpLinkUri: $"https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/{DiagnosticId}.md",
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(RuleInsecureLoadFromAssembly, RuleBypassCertificateChain);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeUsages, SyntaxKind.InvocationExpression);
        }

        public static void AnalyzeUsages(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = context.Node as InvocationExpressionSyntax;
            if (invocationExpression == null)
            {
                return;
            }

            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocationExpression).Symbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                return;
            }

            if (methodSymbol.ReceiverType.ToDisplayString() == "System.Reflection.Assembly")
            {
                HandleAssemblyDiagnostics(context, methodSymbol, invocationExpression);
            }

            if (methodSymbol.ReceiverType.ToDisplayString() == "Skyline.DataMiner.Utils.SecureCoding.SecureReflection.SecureAssembly")
            {
                HandleSecureAssemblyDiagnostics(context, invocationExpression);
            }
        }

        private static void HandleSecureAssemblyDiagnostics(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpression)
        {
            if (invocationExpression.ArgumentList.Arguments.Count < 3)
            {
                return;
            }

            var value = context.SemanticModel.GetConstantValue(invocationExpression.ArgumentList.Arguments.Last()?.Expression);
            if (value.HasValue && value.Value is bool && value.Value is false)
            {
                var diagnostic = Diagnostic.Create(RuleBypassCertificateChain, invocationExpression.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void HandleAssemblyDiagnostics(
            SyntaxNodeAnalysisContext context,
            IMethodSymbol methodSymbol,
            InvocationExpressionSyntax invocation)
        {
            if (methodSymbol.Name == nameof(System.Reflection.Assembly.Load))
            {
                context.ReportDiagnostic(Diagnostic.Create(RuleInsecureLoadAssembly, invocation.GetLocation()));
            }
            if (methodSymbol.Name == nameof(System.Reflection.Assembly.LoadFrom))
            {
                context.ReportDiagnostic(Diagnostic.Create(RuleInsecureLoadFromAssembly, invocation.GetLocation()));
            }
            else if (methodSymbol.Name == nameof(System.Reflection.Assembly.LoadFile))
            {
                context.ReportDiagnostic(Diagnostic.Create(RuleInsecureLoadFileAssembly, invocation.GetLocation()));
            }
            else
            {
                return;
            }
        }
    }
}
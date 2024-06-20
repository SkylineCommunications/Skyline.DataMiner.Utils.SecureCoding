using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureIO;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureSerialization.Json;
using Skyline.DataMiner.Utils.SecureCoding.CodeFixProviders.SecureIO;
using Skyline.DataMiner.Utils.SecureCoding.CodeFixProviders.SecureSerialization.Json;

namespace Skyline.DataMiner.Utils.SecureCoding.DocumentationGenerator
{
    public static partial class Program
    {
        public static void Main()
        {
            var outputFolder = Path.GetFullPath($"{Environment.CurrentDirectory}..\\..\\..\\..\\..");

            var assemblies = new[]
            { 
            // Analyzers
            typeof(FileOperationAnalyzer).Assembly,
            typeof(PathCombineAnalyzer).Assembly,
            typeof(JavaScriptSerializerDeserializationAnalyzer).Assembly,
            typeof(NewtonsoftDeserializationAnalyzer).Assembly,

            // Code Fixes
            typeof(PathCombineCodeFix).Assembly,
            typeof(NewtonsoftDeserializationCodeFixProvider).Assembly,
        };

            var diagnosticAnalyzers = assemblies.SelectMany(assembly => assembly.GetExportedTypes())
                .Where(type => !type.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(type))
                .Select(type => (DiagnosticAnalyzer)Activator.CreateInstance(type)!)
                .ToList();

            var codeFixProviders = assemblies.SelectMany(assembly => assembly.GetExportedTypes())
                .Where(type => !type.IsAbstract && typeof(CodeFixProvider).IsAssignableFrom(type))
                .Select(type => (CodeFixProvider)Activator.CreateInstance(type)!)
                .ToList();

            var sb = new StringBuilder();
            sb.Append("# ").Append(assemblies[0].GetName().Name).Append("'s rules\n");
            var rulesTable = GenerateRulesTable(diagnosticAnalyzers, codeFixProviders);
            sb.Append(rulesTable);

            sb.Append("\n\n# .editorconfig - default values\n\n");
            GenerateEditorConfig(sb, diagnosticAnalyzers);

            sb.Append("\n# .editorconfig - all rules disabled\n\n");
            GenerateEditorConfig(sb, diagnosticAnalyzers, overrideSeverity: "none");

            Console.WriteLine(sb.ToString());

            UpdateHomeReadme(outputFolder, diagnosticAnalyzers, codeFixProviders);

            UpdateRulesReadmeFile(outputFolder, sb);

            UpdateTitleInRulePages(outputFolder, diagnosticAnalyzers);
        }

        private static void UpdateHomeReadme(string outputFolder, List<DiagnosticAnalyzer> diagnosticAnalyzers, List<CodeFixProvider> codeFixProviders)
        {
            // The main readme is embedded into the NuGet package and rendered by nuget.org.
            // nuget.org's markdown support is limited. Raw html in table is not supported.
            var readmePath = Path.GetFullPath(Path.Combine(outputFolder, "README.md"));
            var readmeContent = File.ReadAllText(readmePath);
            var newContent = ReadME().Replace(readmeContent, "\n" + GenerateRulesTable(diagnosticAnalyzers, codeFixProviders, addTitle: false) + "\n");

            File.WriteAllText(readmePath, newContent);
        }

        private static void UpdateRulesReadmeFile(string outputFolder, StringBuilder sb)
        {
            var path = Path.GetFullPath(Path.Combine(outputFolder, "docs", "README.md"));
            Console.WriteLine(path);
            File.WriteAllText(path, sb.ToString());
        }

        private static void UpdateTitleInRulePages(string outputFolder, List<DiagnosticAnalyzer> diagnosticAnalyzers)
        {
            foreach (var diagnostic in diagnosticAnalyzers.SelectMany(diagnosticAnalyzer => diagnosticAnalyzer.SupportedDiagnostics).DistinctBy(diag => diag.Id).OrderBy(diag => diag.Id, StringComparer.Ordinal))
            {
                var title = $"# {diagnostic.Id} - {EscapeMarkdown(diagnostic.Title.ToString(CultureInfo.InvariantCulture))}";
                var detailPath = Path.GetFullPath(Path.Combine(outputFolder, "docs", "Rules", diagnostic.Id + ".md"));
                if (File.Exists(detailPath))
                {
                    var lines = File.ReadAllLines(detailPath);
                    lines[0] = title;
                    File.WriteAllLines(detailPath, lines);
                }
                else
                {
                    File.WriteAllText(detailPath, title);
                }
            }
        }

        private static string GenerateRulesTable(List<DiagnosticAnalyzer> diagnosticAnalyzers, List<CodeFixProvider> codeFixProviders, bool addTitle = true)
        {
            var sb = new StringBuilder();
            sb.Append("|Id|Category|Description|Severity|Is enabled|Code fix|\n");
            sb.Append("|--|--------|-----------|:------:|:--------:|:------:|\n");

            foreach (var diagnostic in diagnosticAnalyzers.SelectMany(diagnosticAnalyzer => diagnosticAnalyzer.SupportedDiagnostics).DistinctBy(diag => diag.Id).OrderBy(diag => diag.Id, StringComparer.Ordinal))
            {
                var hasCodeFix = codeFixProviders.Exists(codeFixProvider => codeFixProvider.FixableDiagnosticIds.Contains(diagnostic.Id, StringComparer.Ordinal));
                sb.Append("|[")
                  .Append(diagnostic.Id)
                  .Append("](")
                  .Append(diagnostic.HelpLinkUri)
                  .Append(")|")
                  .Append(diagnostic.Category)
                  .Append('|')
                  .Append(EscapeMarkdown(diagnostic.Title.ToString(CultureInfo.InvariantCulture)))
                  .Append('|');
                if (addTitle)
                {
                    sb.Append("<span title='")
                      .Append(HtmlEncoder.Default.Encode(diagnostic.DefaultSeverity.ToString()))
                      .Append("'>")
                      .Append(GetSeverity(diagnostic.DefaultSeverity))
                      .Append("</span>");
                }
                else
                {
                    sb.Append(GetSeverity(diagnostic.DefaultSeverity));
                }

                sb.Append('|')
                  .Append(GetBoolean(diagnostic.IsEnabledByDefault))
                  .Append('|')
                  .Append(GetBoolean(hasCodeFix))
                  .Append('|')
                  .Append('\n');
            }

            return sb.ToString();
        }

        private static void GenerateEditorConfig(StringBuilder sb, List<DiagnosticAnalyzer> analyzers, string? overrideSeverity = null)
        {
            sb.Append("```editorconfig\n");
            var first = true;
            foreach (var diagnostic in analyzers.SelectMany(diagnosticAnalyzer => diagnosticAnalyzer.SupportedDiagnostics).DistinctBy(diag => diag.Id).OrderBy(diag => diag.Id, StringComparer.Ordinal))
            {
                if (!first)
                {
                    sb.Append('\n');
                }

                var severity = overrideSeverity;
                if (severity is null)
                {
                    if (diagnostic.IsEnabledByDefault)
                    {
                        severity = diagnostic.DefaultSeverity switch
                        {
                            DiagnosticSeverity.Hidden => "silent",
                            DiagnosticSeverity.Info => "suggestion",
                            DiagnosticSeverity.Warning => "warning",
                            DiagnosticSeverity.Error => "error",
                            _ => throw new InvalidOperationException($"{diagnostic.DefaultSeverity} not supported"),
                        };
                    }
                    else
                    {
                        severity = "none";
                    }
                }

                sb.Append("# ").Append(diagnostic.Id).Append(": ").Append(diagnostic.Title).Append('\n')
                  .Append("dotnet_diagnostic.").Append(diagnostic.Id).Append(".severity = ").Append(severity).Append('\n');

                first = false;
            }

            sb.Append("```\n");
        }

        private static string GetSeverity(DiagnosticSeverity severity)
        {
            return severity switch
            {
                DiagnosticSeverity.Hidden => "👻",
                DiagnosticSeverity.Info => "ℹ️",
                DiagnosticSeverity.Warning => "⚠️",
                DiagnosticSeverity.Error => "❌",
                _ => throw new ArgumentOutOfRangeException(nameof(severity)),
            };
        }

        private static string EscapeMarkdown(string text)
        {
            return text
                .Replace("[", "\\[", StringComparison.Ordinal)
                .Replace("]", "\\]", StringComparison.Ordinal)
                .Replace("<", "\\<", StringComparison.Ordinal)
                .Replace(">", "\\>", StringComparison.Ordinal);
        }

        private static string GetBoolean(bool value)
        {
            return value ? "✔️" : "❌";
        }

        [GeneratedRegex("(?<=<!-- rules -->\\r?\\n).*(?=<!-- rules -->)", RegexOptions.Singleline)]
        private static partial Regex ReadME();
    } 
}
using System;
using System.IO;
using System.Linq;

namespace XafApiConverter.Converter {
    /// <summary>
    /// Command-line interface for type migration
    /// </summary>
    internal class TypeMigrationCli {
        /// <summary>
        /// Run type migration from command line
        /// </summary>
        public static int Run(string[] args) {
            if (args.Length == 0 || args.Contains("--help") || args.Contains("-h")) {
                PrintHelp();
                return 0;
            }

            try {
                var options = ParseArguments(args);
                return ExecuteMigration(options);
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        private static MigrationOptions ParseArguments(string[] args) {
            var options = new MigrationOptions();

            for (int i = 0; i < args.Length; i++) {
                var arg = args[i];

                switch (arg.ToLowerInvariant()) {
                    case "--solution":
                    case "-s":
                        if (i + 1 < args.Length) {
                            options.SolutionPath = args[++i];
                        }
                        break;

                    case "--report-only":
                    case "-r":
                        options.ReportOnly = true;
                        break;

                    case "--output":
                    case "-o":
                        if (i + 1 < args.Length) {
                            options.OutputPath = args[++i];
                        }
                        break;

                    case "--show-mappings":
                    case "-m":
                        options.ShowMappings = true;
                        break;

                    default:
                        if (!arg.StartsWith("-") && string.IsNullOrEmpty(options.SolutionPath)) {
                            options.SolutionPath = arg;
                        }
                        break;
                }
            }

            return options;
        }

        private static int ExecuteMigration(MigrationOptions options) {
            // Show mappings mode
            if (options.ShowMappings) {
                ShowMappings();
                return 0;
            }

            // Validate solution path
            if (string.IsNullOrEmpty(options.SolutionPath)) {
                Console.WriteLine("Error: Solution path is required");
                PrintHelp();
                return 1;
            }

            if (!File.Exists(options.SolutionPath)) {
                Console.WriteLine($"Error: Solution file not found: {options.SolutionPath}");
                return 1;
            }

            // Print header
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("  XAF Type Migration Tool - Hybrid Approach");
            Console.WriteLine("  Automatic Replacements + LLM Analysis");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine($"Solution: {Path.GetFileName(options.SolutionPath)}");
            Console.WriteLine($"Mode: {(options.ReportOnly ? "Report Only" : "Full Migration")}");
            Console.WriteLine();

            try {
                // Create migration tool
                var tool = new TypeMigrationTool(options.SolutionPath);

                // Run migration
                var report = tool.RunMigration();

                // Summary already printed by tool
                
                // Show report path
                var reportPath = options.OutputPath ?? Path.Combine(
                    Path.GetDirectoryName(options.SolutionPath),
                    "type-migration-report.md");

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"📄 Full report saved to:");
                Console.WriteLine($"   {reportPath}");
                Console.ResetColor();
                Console.WriteLine();

                // Next steps
                PrintNextSteps(report);

                return report.BuildSuccessful ? 0 : 1;
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nMigration failed: {ex.Message}");
                Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
                Console.ResetColor();
                return 1;
            }
        }

        private static void ShowMappings() {
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.WriteLine("  Type and Namespace Replacement Mappings");
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.WriteLine();

            // Namespace mappings
            Console.WriteLine("## Namespace Replacements (TRANS-006, TRANS-007)");
            Console.WriteLine();
            foreach (var ns in TypeReplacementMap.NamespaceReplacements.Values) {
                Console.WriteLine($"  {ns.OldNamespace}");
                Console.WriteLine($"  → {ns.NewNamespace}");
                Console.WriteLine($"  ({ns.Description})");
                Console.WriteLine();
            }

            // Type mappings
            Console.WriteLine("## Type Replacements (TRANS-008)");
            Console.WriteLine();
            foreach (var type in TypeReplacementMap.TypeReplacements.Values) {
                Console.WriteLine($"  {type.OldType}");
                Console.WriteLine($"  → {type.NewType}");
                Console.WriteLine($"  ({type.Description})");
                Console.WriteLine();
            }

            // NO_EQUIVALENT namespaces
            Console.WriteLine("## No Equivalent Namespaces (Require Commenting Out)");
            Console.WriteLine();
            foreach (var ns in TypeReplacementMap.NoEquivalentNamespaces.Values) {
                Console.WriteLine($"  ❌ {ns.OldNamespace}");
                Console.WriteLine($"  ({ns.Description})");
                Console.WriteLine();
            }

            // NO_EQUIVALENT types
            Console.WriteLine("## No Equivalent Types (Require Commenting Out)");
            Console.WriteLine();
            foreach (var type in TypeReplacementMap.NoEquivalentTypes.Values) {
                Console.WriteLine($"  ❌ {type.OldType}");
                Console.WriteLine($"  ({type.Description})");
                if (type.CommentOutEntireClass) {
                    Console.WriteLine($"  ⚠️  Requires commenting out ENTIRE CLASS");
                }
                Console.WriteLine();
            }

            // Problematic enums
            Console.WriteLine("## Problematic Enums (TRANS-009)");
            Console.WriteLine();
            foreach (var enumEntry in TypeReplacementMap.ProblematicEnums.Values) {
                Console.WriteLine($"  ⚠️ {enumEntry.EnumName}");
                Console.WriteLine($"  Values: {string.Join(", ", enumEntry.ProblematicValues)}");
                Console.WriteLine($"  ({enumEntry.Description})");
                Console.WriteLine();
            }
        }

        private static void PrintNextSteps(MigrationReport report) {
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.WriteLine("  Next Steps");
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.WriteLine();

            if (report.ProblematicClasses.Any()) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠️  LLM Analysis Required:");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("1. Review the generated report (type-migration-report.md)");
                Console.WriteLine("2. Share the report with LLM (GitHub Copilot, ChatGPT, etc.)");
                Console.WriteLine("3. LLM will analyze and provide:");
                Console.WriteLine("   - Which classes to comment out");
                Console.WriteLine("   - Dependency cascade analysis");
                Console.WriteLine("   - Alternative Blazor implementations");
                Console.WriteLine("   - Code fixes for build errors");
                Console.WriteLine();
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ Automatic Changes:");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine($"• {report.NamespacesReplaced} namespaces migrated");
            Console.WriteLine($"• {report.TypesReplaced} types replaced");
            Console.WriteLine($"• {report.FilesProcessed} files processed");
            Console.WriteLine();

            if (!report.BuildSuccessful) {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("🔧 Build Fixes:");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("After LLM analysis, you can:");
                Console.WriteLine("1. Build the project: dotnet build");
                Console.WriteLine("2. Review build errors");
                Console.WriteLine("3. Apply LLM-suggested fixes");
                Console.WriteLine("4. Iterate until build succeeds");
                Console.WriteLine();
            }
        }

        private static void PrintHelp() {
            Console.WriteLine(@"
XAF Type Migration Tool - Hybrid Approach
==========================================

Usage:
  XafApiConverter migrate-types --solution <path> [options]
  XafApiConverter migrate-types <path> [options]

Arguments:
  <path>                    Path to the .sln file

Options:
  -s, --solution <path>     Path to the solution file
  -o, --output <path>       Output path for report (default: type-migration-report.md)
  -r, --report-only         Generate report only, don't modify files
  -m, --show-mappings       Show all type and namespace mappings
  -h, --help                Show this help message

Examples:
  # Run full migration
  XafApiConverter migrate-types MySolution.sln

  # Generate report only (no file changes)
  XafApiConverter migrate-types MySolution.sln --report-only

  # Show all mappings
  XafApiConverter migrate-types --show-mappings

  # Custom output path
  XafApiConverter migrate-types MySolution.sln --output custom-report.md

What This Tool Does:
====================

✅ AUTOMATIC (No LLM needed):
  • TRANS-006: Migrate System.Data.SqlClient → Microsoft.Data.SqlClient
  • TRANS-007: Migrate DevExpress.ExpressApp.Web.* → Blazor.*
  • TRANS-008: Replace types (WebApplication → BlazorApplication, etc.)
  • Process both .cs and .xafml files
  • Generate detailed report

⚠️  REQUIRES LLM ANALYSIS:
  • TRANS-009: Classes using NO_EQUIVALENT types (Page, TemplateType, etc.)
  • TRANS-010: Iterative build-fix-comment process
  • Dependency cascade analysis
  • Commenting out problematic classes
  • Manual code reviews

Workflow:
=========

1. Run this tool → Automatic replacements + problem detection
2. Review generated report (type-migration-report.md)
3. Share report with LLM (Copilot, ChatGPT)
4. LLM analyzes and suggests fixes
5. Apply LLM fixes
6. Build and test

For more information, see UpdateTypes.md
");
        }

        private class MigrationOptions {
            public string SolutionPath { get; set; }
            public string OutputPath { get; set; }
            public bool ReportOnly { get; set; }
            public bool ShowMappings { get; set; }
        }
    }
}

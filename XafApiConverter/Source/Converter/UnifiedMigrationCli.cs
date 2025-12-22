using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace XafApiConverter.Converter {
    /// <summary>
    /// Unified options for XAF migration workflow
    /// Supports project conversion, security updates, and type migration
    /// </summary>
    public class MigrationOptions {
        public string SolutionPath { get; set; }
        public string TargetFramework { get; set; }
        public string DxVersion { get; set; }
        public string OutputPath { get; set; }

        // Step control
        public bool SkipProjectConversion { get; set; }
        public bool SkipTypeMigration { get; set; }
        public bool SkipSecurityUpdate { get; set; }

        // Type migration specific options
        public bool CommentIssuesOnly { get; set; }
        public bool ShowMappings { get; set; }

        // Other options
        public bool CreateBackup { get; set; } = false;
        public bool UseDirectoryPackages { get; set; }
        public bool ValidateOnly { get; set; }
        public bool ReportOnly { get; set; }
    }

    /// <summary>
    /// Unified CLI for complete XAF migration workflow
    /// Executes: 1) Type Migration, 2) Security Types Update, 3) Project Conversion
    /// </summary>
    public class UnifiedMigrationCli {
        /// <summary>
        /// Run unified migration from command line
        /// </summary>
        public static int Run(string[] args) {
            if (args.Length == 0 || args.Contains("--help") || args.Contains("-h")) {
                PrintHelp();
                return 0;
            }

            try {
                var options = ParseArguments(args);
                
                // Show mappings mode
                if (options.ShowMappings) {
                    ShowMappings();
                    return 0;
                }
                
                if (!ValidateOptions(options)) {
                    return 1;
                }

                return ExecuteMigration(options);
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
                Console.ResetColor();
                return 1;
            }
        }

        private static MigrationOptions ParseArguments(string[] args) {
            var options = new MigrationOptions();

            for (int i = 0; i < args.Length; i++) {
                var arg = args[i];

                switch (arg.ToLowerInvariant()) {
                    // Solution/Directory path
                    case "--solution":
                    case "-s":
                    case "--path":
                    case "-p":
                        if (i + 1 < args.Length) {
                            options.SolutionPath = args[++i];
                        }
                        break;

                    // Target framework
                    case "--target-framework":
                    case "-tf":
                        if (i + 1 < args.Length) {
                            options.TargetFramework = args[++i];
                        }
                        break;

                    // DevExpress version
                    case "--dx-version":
                    case "-dx":
                        if (i + 1 < args.Length) {
                            options.DxVersion = args[++i];
                        }
                        break;

                    // Output path for reports
                    case "--output":
                    case "-o":
                        if (i + 1 < args.Length) {
                            options.OutputPath = args[++i];
                        }
                        break;

                    // Step control
                    case "--skip-conversion":
                        options.SkipProjectConversion = true;
                        break;

                    case "--skip-type-migration":
                        options.SkipTypeMigration = true;
                        break;

                    case "--skip-security-update":
                        options.SkipSecurityUpdate = true;
                        break;

                    case "--only-conversion":
                        options.SkipTypeMigration = true;
                        options.SkipSecurityUpdate = true;
                        break;

                    case "--only-type-migration":
                        options.SkipProjectConversion = true;
                        options.SkipSecurityUpdate = true;
                        break;

                    case "--only-security-update":
                        options.SkipProjectConversion = true;
                        options.SkipTypeMigration = true;
                        break;

                    // Type migration specific options
                    case "--comment-issues-only":
                    case "-c":
                        options.CommentIssuesOnly = true;
                        break;

                    case "--show-mappings":
                    case "-m":
                        options.ShowMappings = true;
                        break;

                    // Other options
                    case "--backup":
                    case "-b":
                        options.CreateBackup = true;
                        break;

                    case "--directory-packages":
                    case "-dp":
                        options.UseDirectoryPackages = true;
                        break;

                    case "--validate":
                    case "-v":
                        options.ValidateOnly = true;
                        break;

                    case "--report-only":
                    case "-r":
                        options.ReportOnly = true;
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

        private static bool ValidateOptions(MigrationOptions options) {
            if (string.IsNullOrEmpty(options.SolutionPath)) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Solution or directory path is required");
                Console.ResetColor();
                Console.WriteLine("\nUsage: XafApiConverter <path> [options]");
                Console.WriteLine("Run with --help for more information");
                return false;
            }

            if (!File.Exists(options.SolutionPath) && !Directory.Exists(options.SolutionPath)) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Path not found: {options.SolutionPath}");
                Console.ResetColor();
                return false;
            }

            return true;
        }

        private static void ShowMappings() {
            Console.WriteLine("===============================================");
            Console.WriteLine("  Type and Namespace Replacement Mappings");
            Console.WriteLine("===============================================");
            Console.WriteLine();

            // Namespace mappings
            Console.WriteLine("## Namespace Replacements (TRANS-006, TRANS-007)");
            Console.WriteLine();
            foreach (var ns in TypeReplacementMap.NamespaceReplacements.Values) {
                Console.WriteLine($"  {ns.OldNamespace}");
                Console.WriteLine($"  -> {ns.NewNamespace}");
                Console.WriteLine($"  ({ns.Description})");
                Console.WriteLine();
            }

            // Type mappings
            Console.WriteLine("## Type Replacements (TRANS-008)");
            Console.WriteLine();
            foreach (var type in TypeReplacementMap.TypeReplacements.Values) {
                Console.WriteLine($"  {type.OldType}");
                Console.WriteLine($"  -> {type.NewType}");
                Console.WriteLine($"  ({type.Description})");
                Console.WriteLine();
            }

            // NO_EQUIVALENT namespaces
            Console.WriteLine("## No Equivalent Namespaces (Require Commenting Out)");
            Console.WriteLine();
            foreach (var ns in TypeReplacementMap.NoEquivalentNamespaces.Values) {
                Console.WriteLine($"  [X] {ns.OldNamespace}");
                Console.WriteLine($"  ({ns.Description})");
                Console.WriteLine();
            }

            // NO_EQUIVALENT types
            Console.WriteLine("## No Equivalent Types (Require Commenting Out)");
            Console.WriteLine();
            foreach (var type in TypeReplacementMap.NoEquivalentTypes.Values) {
                Console.WriteLine($"  [X] {type.OldType}");
                Console.WriteLine($"  ({type.Description})");
                if (type.CommentOutEntireClass) {
                    Console.WriteLine($"  [WARNING] Requires commenting out ENTIRE CLASS");
                }
                Console.WriteLine();
            }

            // Problematic enums
            Console.WriteLine("## Problematic Enums (TRANS-009)");
            Console.WriteLine();
            foreach (var enumEntry in TypeReplacementMap.ProblematicEnums.Values) {
                Console.WriteLine($"  [!] {enumEntry.EnumName}");
                Console.WriteLine($"  Values: {string.Join(", ", enumEntry.ProblematicValues)}");
                Console.WriteLine($"  ({enumEntry.Description})");
                Console.WriteLine();
            }
        }

        private static int ExecuteMigration(MigrationOptions options) {
            PrintHeader();
            PrintOptions(options);

            var solutions = DiscoverSolutions(options.SolutionPath);
            if (!solutions.Any()) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No solution files found.");
                Console.ResetColor();
                return 1;
            }

            Console.WriteLine($"\nFound {solutions.Count} solution(s):");
            foreach (var sln in solutions) {
                Console.WriteLine($"  - {Path.GetFileName(sln)}");
            }
            Console.WriteLine();

            int totalErrors = 0;

            foreach (var solutionPath in solutions) {
                Console.WriteLine("===============================================================");
                Console.WriteLine($"Processing: {Path.GetFileName(solutionPath)}");
                Console.WriteLine("===============================================================");
                Console.WriteLine();

                try {
                    // Step 1: Type Migration (TRANS-006 to TRANS-008)
                    if (!options.SkipTypeMigration) {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(">> Step 1/3: Type Migration (Web -> Blazor)");
                        Console.ResetColor();
                        Console.WriteLine();

                        var typeMigrationResult = RunTypeMigration(solutionPath, options);
                        if (typeMigrationResult != 0) {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("[WARNING] Step 1 completed with warnings");
                            Console.ResetColor();
                        }
                        else {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[OK] Step 1 completed successfully");
                            Console.ResetColor();
                        }
                        Console.WriteLine();
                    }

                    // Step 2: Security Types Update
                    if (!options.SkipSecurityUpdate) {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(">> Step 2/3: Security Types Update");
                        Console.ResetColor();
                        Console.WriteLine();

                        var securityResult = RunSecurityUpdate(solutionPath, options);
                        if (securityResult != 0) {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("[WARNING] Step 2 completed with warnings");
                            Console.ResetColor();
                        }
                        else {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[OK] Step 2 completed successfully");
                            Console.ResetColor();
                        }
                        Console.WriteLine();
                    }

                    // Step 3: Project Conversion (TRANS-001 to TRANS-005)
                    if (!options.SkipProjectConversion) {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(">> Step 3/3: Project Conversion (.NET Framework -> .NET)");
                        Console.ResetColor();
                        Console.WriteLine();

                        var conversionResult = RunProjectConversion(solutionPath, options);
                        if (conversionResult != 0) {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("[WARNING] Step 3 completed with warnings");
                            Console.ResetColor();
                        }
                        else {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[OK] Step 3 completed successfully");
                            Console.ResetColor();
                        }
                        Console.WriteLine();
                    }
                }
                catch (Exception ex) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[ERROR] Error processing solution: {ex.Message}");
                    Console.ResetColor();
                    totalErrors++;
                }
            }

            // Final summary
            PrintFinalSummary(solutions.Count, totalErrors);

            return totalErrors > 0 ? 1 : 0;
        }

        private static List<string> DiscoverSolutions(string path) {
            var solutions = new List<string>();

            if (File.Exists(path) && (path.EndsWith(".sln") || path.EndsWith(".slnx"))) {
                solutions.Add(path);
            }
            else if (Directory.Exists(path)) {
                solutions.AddRange(Directory.GetFiles(path, "*.sln", SearchOption.AllDirectories));
                solutions.AddRange(Directory.GetFiles(path, "*.slnx", SearchOption.AllDirectories));
            }

            return solutions;
        }

        private static int RunProjectConversion(string solutionPath, MigrationOptions options) {
            try {
                // Parse solution to get actual projects
                var solutionDir = Path.GetDirectoryName(solutionPath);
                var projectFiles = ParseSolutionForProjects(solutionPath);

                if (!projectFiles.Any()) {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("No projects found in solution");
                    Console.ResetColor();
                    return 0;
                }

                Console.WriteLine($"Found {projectFiles.Count} project(s) in solution");
                Console.WriteLine();

                // Create conversion config
                var config = CreateConversionConfig(options);
                var converter = new CSprojConverter(config);

                int converted = 0;
                int skipped = 0;
                int failed = 0;

                foreach (var projectPath in projectFiles) {
                    try {
                        var projectName = Path.GetFileNameWithoutExtension(projectPath);
                        Console.Write($"  Converting {projectName}...");

                        converter.ConvertProject(projectPath, options.CreateBackup);

                        // Validate
                        var validation = ProjectValidator.Validate(projectPath, config);
                        if (validation.IsValid) {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine(" [OK]");
                            Console.ResetColor();
                            converted++;
                        }
                        else {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine(" [WARN]");
                            Console.ResetColor();
                            skipped++;
                        }
                    }
                    catch (Exception ex) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($" [ERROR] ({ex.Message})");
                        Console.ResetColor();
                        failed++;
                    }
                }

                Console.WriteLine();
                Console.WriteLine($"Summary: {converted} converted, {skipped} skipped, {failed} failed");

                return failed > 0 ? 1 : 0;
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Project conversion failed: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        /// <summary>
        /// Parse solution file to get list of projects
        /// </summary>
        private static List<string> ParseSolutionForProjects(string solutionPath) {
            var projects = new List<string>();
            var solutionDir = Path.GetDirectoryName(solutionPath);

            try {
                var solutionContent = File.ReadAllText(solutionPath);
                
                // Parse .sln file for project entries
                // Format: Project("{...}") = "ProjectName", "Path\To\Project.csproj", "{...}"
                var projectPattern = @"Project\(""\{[^}]+\}""\)\s*=\s*""[^""]+"",\s*""([^""]+\.csproj)""";
                var matches = System.Text.RegularExpressions.Regex.Matches(solutionContent, projectPattern);

                foreach (System.Text.RegularExpressions.Match match in matches) {
                    if (match.Groups.Count > 1) {
                        var projectRelativePath = match.Groups[1].Value;
                        // Convert to absolute path
                        var projectAbsolutePath = Path.GetFullPath(Path.Combine(solutionDir, projectRelativePath));
                        
                        if (File.Exists(projectAbsolutePath)) {
                            projects.Add(projectAbsolutePath);
                        }
                    }
                }
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Warning: Could not parse solution file: {ex.Message}");
                Console.WriteLine("Falling back to directory search...");
                Console.ResetColor();
                
                // Fallback: search for .csproj files in solution directory
                projects.AddRange(Directory.GetFiles(solutionDir, "*.csproj", SearchOption.AllDirectories));
            }

            return projects;
        }

        private static int RunTypeMigration(string solutionPath, MigrationOptions options) {
            try {
                // Create migration tool with merged options
                var tool = new TypeMigrationTool(options) {
                    CommentIssuesOnly = options.CommentIssuesOnly
                };

                var report = tool.RunMigration();

                // Save report
                var reportPath = options.OutputPath ?? Path.Combine(
                    Path.GetDirectoryName(solutionPath),
                    "type-migration-report.md");
                report.SaveToFile(reportPath);

                Console.WriteLine($"Report saved to: {reportPath}");
                Console.WriteLine();

                // Print next steps if there are problematic classes
                if (report.ProblematicClasses.Any()) {
                    PrintNextSteps(report);
                }

                return report.ProblematicClasses.Any() ? 1 : 0;
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Type migration failed: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        private static void PrintNextSteps(MigrationReport report) {
            Console.WriteLine("===============================================");
            Console.WriteLine("  Next Steps");
            Console.WriteLine("===============================================");
            Console.WriteLine();

            if (report.ProblematicClasses.Any()) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[!] LLM Analysis Required:");
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
            Console.WriteLine("[OK] Automatic Changes:");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine($"  * {report.NamespacesReplaced} namespaces migrated");
            Console.WriteLine($"  * {report.TypesReplaced} types replaced");
            Console.WriteLine($"  * {report.FilesProcessed} files processed");
            Console.WriteLine();

            if (!report.BuildSuccessful) {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("[BUILD] Build Fixes:");
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

        private static int RunSecurityUpdate(string solutionPath, MigrationOptions options) {
            try {
                Console.WriteLine("Processing security types...");

                var result = SecurityTypesUpdater.ProcessSolution(solutionPath);

                Console.WriteLine($"Security types updated: {result.FilesChanged} file(s) changed");

                return 0;
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Security update failed: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        private static ConversionConfig CreateConversionConfig(MigrationOptions options) {
            var config = ConversionConfig.Default;

            if (!string.IsNullOrEmpty(options.TargetFramework)) {
                config.TargetFramework = options.TargetFramework;
                config.TargetFrameworkWindows = $"{options.TargetFramework}-windows";
            }

            if (!string.IsNullOrEmpty(options.DxVersion)) {
                config.DxPackageVersion = options.DxVersion;
                var parts = options.DxVersion.Split('.');
                if (parts.Length >= 2) {
                    config.DxAssemblyVersion = $"v{parts[0]}.{parts[1]}";
                }
            }

            config.UseDirectoryPackages = options.UseDirectoryPackages;

            return config;
        }

        private static void PrintHeader() {
            Console.WriteLine("===============================================================");
            Console.WriteLine("     XAF Migration Tool - Complete Workflow");
            Console.WriteLine("     .NET Framework -> .NET + Web -> Blazor Migration");
            Console.WriteLine("===============================================================");
            Console.WriteLine();
        }

        private static void PrintOptions(MigrationOptions options) {
            Console.WriteLine("Configuration:");
            Console.WriteLine($"  Path: {options.SolutionPath}");
            if (!string.IsNullOrEmpty(options.TargetFramework)) {
                Console.WriteLine($"  Target Framework: {options.TargetFramework}");
            }
            if (!string.IsNullOrEmpty(options.DxVersion)) {
                Console.WriteLine($"  DevExpress Version: {options.DxVersion}");
            }

            Console.WriteLine();
            Console.WriteLine("Steps to execute:");
            Console.WriteLine($"  {(options.SkipProjectConversion ? "[ ]" : "[X]")} Step 1: Project Conversion");
            Console.WriteLine($"  {(options.SkipTypeMigration ? "[ ]" : "[X]")} Step 2: Type Migration");
            Console.WriteLine($"  {(options.SkipSecurityUpdate ? "[ ]" : "[X]")} Step 3: Security Update");
            Console.WriteLine();
        }

        private static void PrintFinalSummary(int totalSolutions, int errors) {
            Console.WriteLine("===============================================================");
            Console.WriteLine("                    Final Summary");
            Console.WriteLine("===============================================================");
            Console.WriteLine();
            Console.WriteLine($"Solutions processed: {totalSolutions}");

            if (errors == 0) {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[OK] All migrations completed successfully!");
                Console.ResetColor();
            }
            else {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[WARNING] Completed with {errors} error(s)");
                Console.WriteLine("Review the output above for details");
                Console.ResetColor();
            }
            Console.WriteLine();
        }

        private static void PrintHelp() {
            Console.WriteLine(@"
XAF Migration Tool - Complete Workflow
======================================

Executes complete migration workflow:
  1. Project Conversion (.NET Framework -> .NET)
  2. Security Types Update (SecuritySystem* -> PermissionPolicy*)
  3. Type Migration (Web -> Blazor)

Usage:
  XafApiConverter <path> [options]

Arguments:
  <path>                    Path to solution file (.sln) or directory

Common Options:
  -s, --solution <path>     Solution file or directory path
  -p, --path <path>         Solution file or directory path (alias)
  -tf, --target-framework   Target .NET version (default: net9.0)
                            Examples: net8.0, net9.0, net10.0
  -dx, --dx-version         DevExpress version (default: 25.1.6)
                            Example: 25.1.6, 26.1.6
  -o, --output <path>       Output directory for reports
  -b, --backup              Create backup files
  -dp, --directory-packages Use Directory.Packages.props

Step Control:
  --skip-conversion         Skip step 1 (project conversion)
  --skip-security-update    Skip step 2 (security update)
  --skip-type-migration     Skip step 3 (type migration)
  
  --only-conversion         Execute only step 1
  --only-security-update    Execute only step 2
  --only-type-migration     Execute only step 3

Type Migration Options:
  -c, --comment-issues-only Add warning comments to ALL problematic classes 
                            without commenting them out
                            (Treats all classes as protected - useful for manual review)
  -m, --show-mappings       Show all type and namespace mappings and exit

Other Options:
  -v, --validate            Validation mode only (not implemented yet)
  -r, --report-only         Generate reports without modifications
  -h, --help                Show this help message

Examples:
=========

# Run complete migration (all 3 steps)
XafApiConverter MySolution.sln

# Run with custom .NET version
XafApiConverter MySolution.sln --target-framework net10.0

# Run with custom DevExpress version
XafApiConverter MySolution.sln --dx-version 26.1.6

# Skip security update step
XafApiConverter MySolution.sln --skip-security-update

# Run only project conversion
XafApiConverter MySolution.sln --only-conversion

# Run only type migration with warning comments only (no auto-commenting)
XafApiConverter MySolution.sln --only-type-migration --comment-issues-only

# Process all solutions in directory
XafApiConverter C:\Projects\MyXafApp

# Use Directory.Packages.props
XafApiConverter MySolution.sln --directory-packages

# Show type and namespace mappings
XafApiConverter --show-mappings

# Generate report only (no file changes)
XafApiConverter MySolution.sln --report-only

Steps Details:
==============

Step 1: Project Conversion
  - Converts .csproj to SDK-style format
  - Updates target framework to .NET 9/10
  - Adds NuGet packages (BASE/WINDOWS/BLAZOR_WEB)
  - Removes legacy assembly references
  - Validates converted projects

Step 2: Security Types Update
  - Updates security types (SecuritySystem* -> PermissionPolicy*)
  - Removes obsolete feature toggles
  - Adds PermissionPolicyRoleExtensions
  - Updates permission state setters

Step 3: Type Migration (Hybrid Approach)
  
  [OK] AUTOMATIC (No LLM needed):
    * TRANS-006: Migrate System.Data.SqlClient -> Microsoft.Data.SqlClient
    * TRANS-007: Migrate DevExpress.ExpressApp.Web.* -> Blazor.*
    * TRANS-008: Replace types (WebApplication -> BlazorApplication, etc.)
    * Process both .cs and .xafml files
    * Generate detailed report

  [!] REQUIRES LLM ANALYSIS:
    * TRANS-009: Classes using NO_EQUIVALENT types (Page, TemplateType, etc.)
    * TRANS-010: Iterative build-fix-comment process
    * Dependency cascade analysis
    * Commenting out problematic classes
    * Manual code reviews

Type Migration Modes:
=====================

1. Full Migration (default):
   - Applies automatic replacements
   - Comments out problematic classes automatically
   - Protected classes (ModuleBase, BaseObject) only get warnings

2. Comment Issues Only (--comment-issues-only):
   - Applies automatic replacements
   - ALL problematic classes only get warning comments (no auto-commenting)
   - Useful for manual review and controlled migration
   - Developer decides which classes to comment out

3. Report Only (--report-only):
   - No file modifications
   - Only generates analysis report

Type Migration Workflow:
========================

1. Run this tool -> Automatic replacements + problem detection
2. Review generated report (type-migration-report.md)
3. Share report with LLM (Copilot, ChatGPT)
4. LLM analyzes and suggests fixes
5. Apply LLM fixes
6. Build and test

Default Behavior:
  By default, ALL THREE STEPS are executed sequentially.
  Use --skip-* or --only-* flags to control step execution.

Legacy Commands:
================

The following legacy commands are still supported for backward compatibility:
  XafApiConverter convert <solution>      -> XafApiConverter <solution> --only-conversion
  XafApiConverter migrate-types <solution> -> XafApiConverter <solution> --only-type-migration
  XafApiConverter security-update <solution> -> XafApiConverter <solution> --only-security-update

For more information, see documentation files:
  - README.md
  - TYPE_MIGRATION_README.md
  - UpdateTypes.md
  - QUICK_START.md
  - Convert_to_NET.md
");
        }
    }

    /// <summary>
    /// Result from SecurityTypesUpdater
    /// </summary>
    public class SecurityUpdateResult {
        public int FilesChanged { get; set; }
        public List<string> ChangedFiles { get; set; } = new();
        public bool Success { get; set; }
    }
}

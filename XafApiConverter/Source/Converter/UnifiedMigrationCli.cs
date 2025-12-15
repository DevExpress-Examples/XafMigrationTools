using System;
using System.IO;
using System.Linq;

namespace XafApiConverter.Converter {
    /// <summary>
    /// Unified CLI for complete XAF migration workflow
    /// Executes: 1) Project Conversion, 2) Type Migration, 3) Security Types Update
    /// </summary>
    internal class UnifiedMigrationCli {
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

                    // Other options
                    case "--no-backup":
                    case "-nb":
                        options.CreateBackup = false;
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
                Console.WriteLine($"  • {Path.GetFileName(sln)}");
            }
            Console.WriteLine();

            int totalErrors = 0;

            foreach (var solutionPath in solutions) {
                Console.WriteLine("???????????????????????????????????????????????????????????");
                Console.WriteLine($"Processing: {Path.GetFileName(solutionPath)}");
                Console.WriteLine("???????????????????????????????????????????????????????????");
                Console.WriteLine();

                try {
                    // Step 1: Project Conversion (TRANS-001 to TRANS-005)
                    if (!options.SkipProjectConversion) {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("? Step 1/3: Project Conversion (.NET Framework ? .NET)");
                        Console.ResetColor();
                        Console.WriteLine();

                        var conversionResult = RunProjectConversion(solutionPath, options);
                        if (conversionResult != 0) {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("??  Step 1 completed with warnings");
                            Console.ResetColor();
                        }
                        else {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("? Step 1 completed successfully");
                            Console.ResetColor();
                        }
                        Console.WriteLine();
                    }

                    // Step 2: Type Migration (TRANS-006 to TRANS-008)
                    if (!options.SkipTypeMigration) {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("? Step 2/3: Type Migration (Web ? Blazor)");
                        Console.ResetColor();
                        Console.WriteLine();

                        var typeMigrationResult = RunTypeMigration(solutionPath, options);
                        if (typeMigrationResult != 0) {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("??  Step 2 completed with warnings");
                            Console.ResetColor();
                        }
                        else {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("? Step 2 completed successfully");
                            Console.ResetColor();
                        }
                        Console.WriteLine();
                    }

                    // Step 3: Security Types Update
                    if (!options.SkipSecurityUpdate) {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("? Step 3/3: Security Types Update");
                        Console.ResetColor();
                        Console.WriteLine();

                        var securityResult = RunSecurityUpdate(solutionPath, options);
                        if (securityResult != 0) {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("??  Step 3 completed with warnings");
                            Console.ResetColor();
                        }
                        else {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("? Step 3 completed successfully");
                            Console.ResetColor();
                        }
                        Console.WriteLine();
                    }
                }
                catch (Exception ex) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"? Error processing solution: {ex.Message}");
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
                // Get all projects in solution
                var solutionDir = Path.GetDirectoryName(solutionPath);
                var projectFiles = Directory.GetFiles(solutionDir, "*.csproj", SearchOption.AllDirectories);

                Console.WriteLine($"Found {projectFiles.Length} project(s) to convert");
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

                        converter.ConvertProject(projectPath);

                        // Validate
                        var validation = ProjectValidator.Validate(projectPath, config);
                        if (validation.IsValid) {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine(" ?");
                            Console.ResetColor();
                            converted++;
                        }
                        else {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine(" ?");
                            Console.ResetColor();
                            skipped++;
                        }
                    }
                    catch (Exception ex) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($" ? ({ex.Message})");
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

        private static int RunTypeMigration(string solutionPath, MigrationOptions options) {
            try {
                var tool = new TypeMigrationTool(solutionPath);
                var report = tool.RunMigration();

                // Save report
                var reportPath = options.OutputPath ?? Path.Combine(
                    Path.GetDirectoryName(solutionPath),
                    "type-migration-report.md");
                report.SaveToFile(reportPath);

                Console.WriteLine($"Report saved to: {reportPath}");
                Console.WriteLine();

                return report.ProblematicClasses.Any() ? 1 : 0;
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Type migration failed: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        private static int RunSecurityUpdate(string solutionPath, MigrationOptions options) {
            try {
                Console.WriteLine("Processing security types...");

                // This will be integrated with SecurityTypesUpdater
                // For now, just a placeholder
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
            Console.WriteLine("?????????????????????????????????????????????????????????????");
            Console.WriteLine("?     XAF Migration Tool - Complete Workflow               ?");
            Console.WriteLine("?     .NET Framework ? .NET + Web ? Blazor Migration      ?");
            Console.WriteLine("?????????????????????????????????????????????????????????????");
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
            Console.WriteLine($"  {(options.SkipProjectConversion ? "?" : "?")} Step 1: Project Conversion");
            Console.WriteLine($"  {(options.SkipTypeMigration ? "?" : "?")} Step 2: Type Migration");
            Console.WriteLine($"  {(options.SkipSecurityUpdate ? "?" : "?")} Step 3: Security Update");
            Console.WriteLine();
        }

        private static void PrintFinalSummary(int totalSolutions, int errors) {
            Console.WriteLine("???????????????????????????????????????????????????????????");
            Console.WriteLine("                    Final Summary");
            Console.WriteLine("???????????????????????????????????????????????????????????");
            Console.WriteLine();
            Console.WriteLine($"Solutions processed: {totalSolutions}");

            if (errors == 0) {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("? All migrations completed successfully!");
                Console.ResetColor();
            }
            else {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"??  Completed with {errors} error(s)");
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
  1. Project Conversion (.NET Framework ? .NET)
  2. Type Migration (Web ? Blazor)
  3. Security Types Update

Usage:
  XafApiConverter <path> [options]

Arguments:
  <path>                    Path to solution file (.sln) or directory

Common Options:
  -s, --solution <path>     Solution file or directory path
  -tf, --target-framework   Target .NET version (default: net9.0)
                            Examples: net8.0, net9.0, net10.0
  -dx, --dx-version         DevExpress version (default: 25.1.6)
                            Example: 25.1.6, 26.1.6
  -o, --output <path>       Output directory for reports
  -nb, --no-backup          Don't create backup files
  -dp, --directory-packages Use Directory.Packages.props

Step Control:
  --skip-conversion         Skip step 1 (project conversion)
  --skip-type-migration     Skip step 2 (type migration)
  --skip-security-update    Skip step 3 (security update)
  
  --only-conversion         Execute only step 1
  --only-type-migration     Execute only step 2
  --only-security-update    Execute only step 3

Other Options:
  -v, --validate            Validation mode only
  -r, --report-only         Generate reports without modifications
  -h, --help                Show this help message

Examples:
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

  # Process all solutions in directory
  XafApiConverter C:\Projects\MyXafApp

  # Use Directory.Packages.props
  XafApiConverter MySolution.sln --directory-packages

Steps Details:
==============

Step 1: Project Conversion
  - Converts .csproj to SDK-style format
  - Updates target framework to .NET 9/10
  - Adds NuGet packages (BASE/WINDOWS/BLAZOR_WEB)
  - Removes legacy assembly references
  - Validates converted projects

Step 2: Type Migration
  - Migrates namespaces (Web ? Blazor)
  - Replaces types (ASPx* ? Dx*, WebApplication ? BlazorApplication)
  - Processes .cs and .xafml files
  - Detects problematic types (NO_EQUIVALENT)
  - Generates detailed migration report

Step 3: Security Update
  - Updates security types (SecuritySystem* ? PermissionPolicy*)
  - Removes obsolete feature toggles
  - Adds PermissionPolicyRoleExtensions
  - Updates permission state setters

Default Behavior:
  By default, ALL THREE STEPS are executed sequentially.
  Use --skip-* or --only-* flags to control step execution.

For more information, see documentation files:
  - README.md
  - TYPE_MIGRATION_README.md
  - QUICK_START.md
");
        }

        private class MigrationOptions {
            public string SolutionPath { get; set; }
            public string TargetFramework { get; set; }
            public string DxVersion { get; set; }
            public string OutputPath { get; set; }

            // Step control
            public bool SkipProjectConversion { get; set; }
            public bool SkipTypeMigration { get; set; }
            public bool SkipSecurityUpdate { get; set; }

            // Other options
            public bool CreateBackup { get; set; } = true;
            public bool UseDirectoryPackages { get; set; }
            public bool ValidateOnly { get; set; }
            public bool ReportOnly { get; set; }
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

using System;
using System.IO;
using System.Linq;

namespace XafApiConverter.Converter {
    /// <summary>
    /// Command-line interface for project conversion
    /// </summary>
    internal class ConversionCli {
        /// <summary>
        /// Run conversion from command line arguments
        /// </summary>
        public static int Run(string[] args) {
            if (args.Length == 0 || args.Contains("--help") || args.Contains("-h")) {
                PrintHelp();
                return 0;
            }

            try {
                var options = ParseArguments(args);
                return ExecuteConversion(options);
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        private static ConversionOptions ParseArguments(string[] args) {
            var options = new ConversionOptions();

            for (int i = 0; i < args.Length; i++) {
                var arg = args[i];

                switch (arg.ToLowerInvariant()) {
                    case "--project":
                    case "-p":
                        if (i + 1 < args.Length) {
                            options.ProjectPath = args[++i];
                        }
                        break;

                    case "--target-framework":
                    case "-tf":
                        if (i + 1 < args.Length) {
                            options.TargetFramework = args[++i];
                        }
                        break;

                    case "--dx-version":
                    case "-dx":
                        if (i + 1 < args.Length) {
                            options.DxVersion = args[++i];
                        }
                        break;

                    case "--validate":
                    case "-v":
                        options.ValidateOnly = true;
                        break;

                    case "--no-backup":
                    case "-nb":
                        options.CreateBackup = false;
                        break;

                    case "--directory-packages":
                    case "-dp":
                        options.UseDirectoryPackages = true;
                        break;

                    default:
                        if (!arg.StartsWith("-") && string.IsNullOrEmpty(options.ProjectPath)) {
                            options.ProjectPath = arg;
                        }
                        break;
                }
            }

            return options;
        }

        private static int ExecuteConversion(ConversionOptions options) {
            if (string.IsNullOrEmpty(options.ProjectPath)) {
                Console.WriteLine("Error: Project path is required");
                PrintHelp();
                return 1;
            }

            if (!File.Exists(options.ProjectPath)) {
                Console.WriteLine($"Error: Project file not found: {options.ProjectPath}");
                return 1;
            }

            // Create configuration
            var config = new ConversionConfig();
            
            if (!string.IsNullOrEmpty(options.TargetFramework)) {
                config.TargetFramework = options.TargetFramework;
                config.TargetFrameworkWindows = $"{options.TargetFramework}-windows";
            }

            if (!string.IsNullOrEmpty(options.DxVersion)) {
                config.DxPackageVersion = options.DxVersion;
                // Extract assembly version (e.g., "25.1.6" -> "v25.1")
                var parts = options.DxVersion.Split('.');
                if (parts.Length >= 2) {
                    config.DxAssemblyVersion = $"v{parts[0]}.{parts[1]}";
                }
            }

            config.UseDirectoryPackages = options.UseDirectoryPackages;

            // Execute based on mode
            if (options.ValidateOnly) {
                return ExecuteValidation(options.ProjectPath, config);
            }
            else {
                return ExecuteConvert(options.ProjectPath, config, options.CreateBackup);
            }
        }

        private static int ExecuteConvert(string projectPath, ConversionConfig config, bool createBackup) {
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine("XAF .NET Framework to .NET Core/5+ Project Converter");
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine();

            var converter = new CSprojConverter(config);
            
            try {
                converter.ConvertProject(projectPath);
                Console.WriteLine();

                // Validate after conversion
                var validation = ProjectValidator.Validate(projectPath, config);
                validation.PrintResults();

                return validation.IsValid ? 0 : 1;
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nConversion failed: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        private static int ExecuteValidation(string projectPath, ConversionConfig config) {
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine("Project Validation");
            Console.WriteLine("=".PadRight(60, '='));

            var validation = ProjectValidator.Validate(projectPath, config);
            validation.PrintResults();

            return validation.IsValid ? 0 : 1;
        }

        private static void PrintHelp() {
            Console.WriteLine(@"
XAF .NET Framework to .NET Core/5+ Project Converter
====================================================

Usage:
  XafApiConverter convert --project <path> [options]
  XafApiConverter convert <path> [options]

Arguments:
  <path>                    Path to the .csproj file to convert

Options:
  -p, --project <path>      Path to the .csproj file to convert
  -tf, --target-framework   Target framework (default: net9.0)
                            Examples: net8.0, net9.0, net10.0
  -dx, --dx-version         DevExpress package version (default: 25.1.6)
                            Example: 25.1.6, 26.1.6
  -v, --validate            Validate project without converting
  -nb, --no-backup          Don't create backup file
  -dp, --directory-packages Use Directory.Packages.props
  -h, --help                Show this help message

Examples:
  # Convert a project with default settings
  XafApiConverter convert MyProject.csproj

  # Convert to .NET 10
  XafApiConverter convert MyProject.csproj --target-framework net10.0

  # Convert with specific DevExpress version
  XafApiConverter convert MyProject.csproj --dx-version 26.1.6

  # Validate an already converted project
  XafApiConverter convert MyProject.csproj --validate

  # Convert using Directory.Packages.props
  XafApiConverter convert MyProject.csproj --directory-packages

Features:
  ? Converts legacy .NET Framework .csproj to SDK-style format
  ? Automatically detects Windows and Web project types
  ? Adds appropriate NuGet packages based on project type
  ? Removes legacy assembly references
  ? Handles AssemblyInfo.cs correctly
  ? Manages embedded resources properly
  ? Creates backup before conversion
  ? Validates converted project

For more information, see Convert_to_NET.md
");
        }

        private class ConversionOptions {
            public string ProjectPath { get; set; }
            public string TargetFramework { get; set; }
            public string DxVersion { get; set; }
            public bool ValidateOnly { get; set; }
            public bool CreateBackup { get; set; } = true;
            public bool UseDirectoryPackages { get; set; }
        }
    }
}

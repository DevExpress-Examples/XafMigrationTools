using Microsoft.Build.Locator;
using XafApiConverter.Converter;

namespace XafApiConverter {
    static class Program {
        static void Main(string[] args) {
            args = new[] { "d:\\Work\\Temp_Convert_NET\\FeatureCenter.NETFramework.XPO.sln" };
            // Register MSBuild
            MSBuildLocator.RegisterDefaults();

            // Check for specific CLI commands
            if (args.Length > 0) {
                var command = args[0].ToLowerInvariant();

                // explicit "convert" command
                if (command == "convert") {
                    var cliArgs = args.Skip(1).ToArray();
                    Environment.Exit(ConversionCli.Run(cliArgs));
                    return;
                }

                // explicit "migrate-types" command
                if (command == "migrate-types") {
                    var cliArgs = args.Skip(1).ToArray();
                    Environment.Exit(TypeMigrationCli.Run(cliArgs));
                    return;
                }

                // explicit "security-update" command
                if (command == "security-update") {
                    var cliArgs = args.Skip(1).ToArray();
                    Environment.Exit(SecurityUpdateCli.Run(cliArgs));
                    return;
                }
            }

            // Default: Run unified migration workflow
            // Executes all 3 steps by default
            Environment.Exit(UnifiedMigrationCli.Run(args));
        }
    }

    /// <summary>
    /// CLI wrapper for SecurityTypesUpdater (for legacy support)
    /// </summary>
    internal static class SecurityUpdateCli {
        public static int Run(string[] args) {
            if (args.Length == 0 || args.Contains("--help") || args.Contains("-h")) {
                PrintHelp();
                return 0;
            }

            try {
                var solutionPath = args[0];
                
                if (!File.Exists(solutionPath)) {
                    Console.WriteLine($"Error: Solution file not found: {solutionPath}");
                    return 1;
                }

                Console.WriteLine($"Processing solution: {solutionPath}");
                Console.WriteLine();

                var result = SecurityTypesUpdater.ProcessSolution(solutionPath);

                Console.WriteLine();
                Console.WriteLine($"✅ Security update complete");
                Console.WriteLine($"   Files changed: {result.FilesChanged}");

                return result.Success ? 0 : 1;
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        private static void PrintHelp() {
            Console.WriteLine(@"
Security Types Updater
======================

Updates security types from SecuritySystem* to PermissionPolicy*

Usage:
  XafApiConverter security-update <solution.sln>

Arguments:
  <solution.sln>   Path to solution file

Example:
  XafApiConverter security-update MySolution.sln

Note: This is a legacy command. Consider using the unified workflow:
  XafApiConverter <solution.sln>  (runs all steps including security update)
");
        }
    }
}
using Microsoft.Build.Locator;
using XafApiConverter.Converter;

namespace XafApiConverter {
    static class Program {
        static void Main(string[] args) {
            // Register MSBuild
            MSBuildLocator.RegisterDefaults();

            // Check for specific CLI commands
            if (args.Length > 0) {
                var command = args[0].ToLowerInvariant();

                // Legacy commands for backward compatibility

                // explicit "migrate-types" command - redirect to unified CLI with --only-type-migration
                if (command == "migrate-types") {
                    var cliArgs = args.Skip(1).ToList();
                    cliArgs.Add("--only-type-migration");
                    Environment.Exit(UnifiedMigrationCli.Run(cliArgs.ToArray()));
                    return;
                }

                // explicit "security-update" command - redirect to unified CLI with --only-security-update
                if (command == "security-update") {
                    var cliArgs = args.Skip(1).ToList();
                    cliArgs.Add("--only-security-update");
                    Environment.Exit(UnifiedMigrationCli.Run(cliArgs.ToArray()));
                    return;
                }
            }

            // Default: Run unified migration workflow
            // Executes all 3 steps by default
            Environment.Exit(UnifiedMigrationCli.Run(args));
        }
    }
}
using Microsoft.Build.Locator;
using XafApiConverter.Converter;

namespace XafApiConverter {
    static class Program {
        static void Main(string[] args) {
            // Register MSBuild
            MSBuildLocator.RegisterDefaults();

            Environment.Exit(UnifiedMigrationCli.Run(args));
        }
    }
}
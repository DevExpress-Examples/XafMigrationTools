using System;
using System.IO;
using XafApiConverter.Converter;

namespace XafApiConverter.Converter.Examples {
    /// <summary>
    /// Example usage scenarios for CSprojConverter
    /// </summary>
    internal class UsageExamples {
        /// <summary>
        /// Example 1: Simple conversion with default settings
        /// </summary>
        public static void Example1_SimpleConversion() {
            Console.WriteLine("=== Example 1: Simple Conversion ===\n");

            var projectPath = @"C:\Projects\MyProject\MyProject.csproj";
            
            var converter = new CSprojConverter();
            converter.ConvertProject(projectPath);

            // Validate the result
            var result = ProjectValidator.Validate(projectPath);
            result.PrintResults();
        }


        /// <summary>
        /// Example 2: Conversion to .NET 10 with custom DevExpress version
        /// </summary>
        public static void Example2_CustomConfiguration() {
            Console.WriteLine("=== Example 2: Custom Configuration ===\n");

            var projectPath = @"C:\Projects\MyProject\MyProject.csproj";

            // Create custom configuration
            var config = new ConversionConfig {
                TargetFramework = "net10.0",
                TargetFrameworkWindows = "net10.0-windows",
                DxPackageVersion = "26.1.6",
                DxAssemblyVersion = "v26.1"
            };

            var converter = new CSprojConverter(config);
            converter.ConvertProject(projectPath);
        }

        /// <summary>
        /// Example 3: Batch conversion of multiple projects
        /// </summary>
        public static void Example3_BatchConversion() {
            Console.WriteLine("=== Example 3: Batch Conversion ===\n");

            var projectPaths = new[] {
                @"C:\Projects\MyProject\Module\Module.csproj",
                @"C:\Projects\MyProject\Module.Win\Module.Win.csproj",
                @"C:\Projects\MyProject\Module.Blazor\Module.Blazor.csproj",
                @"C:\Projects\MyProject\Win\Win.csproj",
                @"C:\Projects\MyProject\Blazor\Blazor.csproj"
            };

            var converter = new CSprojConverter();
            int successCount = 0;
            int failCount = 0;

            foreach (var projectPath in projectPaths) {
                try {
                    if (File.Exists(projectPath)) {
                        Console.WriteLine($"\nConverting: {Path.GetFileName(projectPath)}");
                        converter.ConvertProject(projectPath);
                        
                        // Quick validation
                        var result = ProjectValidator.Validate(projectPath);
                        if (result.IsValid) {
                            successCount++;
                        } else {
                            failCount++;
                        }
                    }
                }
                catch (Exception ex) {
                    Console.WriteLine($"Failed: {ex.Message}");
                    failCount++;
                }
            }

            Console.WriteLine($"\n=== Summary ===");
            Console.WriteLine($"Success: {successCount}");
            Console.WriteLine($"Failed: {failCount}");
        }

        /// <summary>
        /// Example 4: Validation only mode
        /// </summary>
        public static void Example4_ValidationOnly() {
            Console.WriteLine("=== Example 4: Validation Only ===\n");

            var projectPath = @"C:\Projects\MyProject\MyProject.csproj";

            // Validate without converting
            var result = ProjectValidator.Validate(projectPath);
            result.PrintResults();

            if (!result.IsValid) {
                Console.WriteLine("\nProject needs conversion or has issues.");
            }
        }

        /// <summary>
        /// Example 5: Using Directory.Packages.props
        /// </summary>
        public static void Example5_DirectoryPackages() {
            Console.WriteLine("=== Example 5: Directory.Packages.props ===\n");

            var projectPath = @"C:\Projects\MyProject\MyProject.csproj";

            var config = new ConversionConfig {
                UseDirectoryPackages = true
            };

            var converter = new CSprojConverter(config);
            converter.ConvertProject(projectPath);

            // Note: You'll need to manually create Directory.Packages.props
            Console.WriteLine("\nIMPORTANT: Create Directory.Packages.props in solution root:");
            Console.WriteLine(@"
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include=""DevExpress.ExpressApp"" Version=""25.1.6"" />
    <!-- Add other package versions here -->
  </ItemGroup>
</Project>
");
        }

        /// <summary>
        /// Example 6: Get package list for manual verification
        /// </summary>
        public static void Example6_InspectPackages() {
            Console.WriteLine("=== Example 6: Inspect Packages ===\n");

            var packageManager = new PackageManager();

            // Get packages for Windows project
            var windowsPackages = packageManager.GetPackages(isWindowsProject: true, isWebProject: false);
            Console.WriteLine($"Windows project packages: {windowsPackages.Count}");
            foreach (var pkg in windowsPackages.Take(5)) {
                Console.WriteLine($"  - {pkg.Name} v{pkg.Version}");
            }

            Console.WriteLine($"  ... and {windowsPackages.Count - 5} more\n");

            // Get packages for Web project
            var webPackages = packageManager.GetPackages(isWindowsProject: false, isWebProject: true);
            Console.WriteLine($"Web project packages: {webPackages.Count}");
            foreach (var pkg in webPackages.Take(5)) {
                Console.WriteLine($"  - {pkg.Name} v{pkg.Version}");
            }

            Console.WriteLine($"  ... and {webPackages.Count - 5} more");
        }

        /// <summary>
        /// Example 7: Process entire solution
        /// </summary>
        public static void Example7_ConvertSolution() {
            Console.WriteLine("=== Example 7: Convert Entire Solution ===\n");

            var solutionPath = @"C:\Projects\MyProject\MyProject.sln";

            // Find all .csproj files in solution directory
            var solutionDir = Path.GetDirectoryName(solutionPath);
            var projectFiles = Directory.GetFiles(solutionDir, "*.csproj", SearchOption.AllDirectories);

            Console.WriteLine($"Found {projectFiles.Length} projects\n");

            var converter = new CSprojConverter();
            var results = new List<(string ProjectName, bool Success)>();

            foreach (var projectPath in projectFiles) {
                try {
                    Console.WriteLine($"Converting: {Path.GetFileName(projectPath)}");
                    converter.ConvertProject(projectPath);
                    
                    var validation = ProjectValidator.Validate(projectPath);
                    results.Add((Path.GetFileNameWithoutExtension(projectPath), validation.IsValid));
                }
                catch (Exception ex) {
                    Console.WriteLine($"Error: {ex.Message}");
                    results.Add((Path.GetFileNameWithoutExtension(projectPath), false));
                }
            }

            // Summary
            Console.WriteLine("\n=== Conversion Summary ===");
            foreach (var (projectName, success) in results) {
                var icon = success ? "?" : "?";
                Console.WriteLine($"{icon} {projectName}");
            }
        }

        /// <summary>
        /// Example 8: Compare before and after
        /// </summary>
        public static void Example8_CompareBeforeAfter() {
            Console.WriteLine("=== Example 8: Compare Before and After ===\n");

            var projectPath = @"C:\Projects\MyProject\MyProject.csproj";
            var backupPath = projectPath + ".backup";

            if (!File.Exists(backupPath)) {
                Console.WriteLine("No backup file found. Convert the project first.");
                return;
            }

            var originalSize = new FileInfo(backupPath).Length;
            var newSize = new FileInfo(projectPath).Length;
            var reduction = (1.0 - (double)newSize / originalSize) * 100;

            Console.WriteLine($"Original size: {originalSize:N0} bytes");
            Console.WriteLine($"New size: {newSize:N0} bytes");
            Console.WriteLine($"Reduction: {reduction:F1}%");

            // Count lines
            var originalLines = File.ReadAllLines(backupPath).Length;
            var newLines = File.ReadAllLines(projectPath).Length;
            var lineReduction = (1.0 - (double)newLines / originalLines) * 100;

            Console.WriteLine($"\nOriginal lines: {originalLines}");
            Console.WriteLine($"New lines: {newLines}");
            Console.WriteLine($"Line reduction: {lineReduction:F1}%");
        }

        /// <summary>
        /// Example 9: Restore from backup
        /// </summary>
        public static void Example9_RestoreFromBackup() {
            Console.WriteLine("=== Example 9: Restore from Backup ===\n");

            var projectPath = @"C:\Projects\MyProject\MyProject.csproj";
            var backupPath = projectPath + ".backup";

            if (!File.Exists(backupPath)) {
                Console.WriteLine("No backup file found.");
                return;
            }

            File.Copy(backupPath, projectPath, overwrite: true);
            Console.WriteLine($"Restored project from backup: {backupPath}");
        }

        /// <summary>
        /// Example 10: Check if project needs conversion
        /// </summary>
        public static void Example10_CheckNeedsConversion() {
            Console.WriteLine("=== Example 10: Check if Conversion Needed ===\n");

            var projectPath = @"C:\Projects\MyProject\MyProject.csproj";

            if (!File.Exists(projectPath)) {
                Console.WriteLine("Project file not found.");
                return;
            }

            var content = File.ReadAllText(projectPath);
            var isSdkStyle = content.Contains("<Project Sdk=", StringComparison.OrdinalIgnoreCase);

            if (isSdkStyle) {
                Console.WriteLine("? Project is already SDK-style format");
                Console.WriteLine("  No conversion needed.");
            }
            else {
                Console.WriteLine("? Project is legacy format");
                Console.WriteLine("  Conversion recommended.");
            }
        }

        /// <summary>
        /// Run all examples (for testing)
        /// </summary>
        public static void RunAllExamples() {
            var examples = new Action[] {
                // Example1_SimpleConversion,           // Uncomment to run
                // Example2_CustomConfiguration,
                // Example3_BatchConversion,
                // Example4_ValidationOnly,
                // Example5_DirectoryPackages,
                Example6_InspectPackages,
                // Example7_ConvertSolution,
                // Example8_CompareBeforeAfter,
                // Example9_RestoreFromBackup,
                Example10_CheckNeedsConversion
            };

            foreach (var example in examples) {
                try {
                    example();
                    Console.WriteLine("\nPress Enter to continue to next example...");
                    Console.ReadLine();
                }
                catch (Exception ex) {
                    Console.WriteLine($"Example failed: {ex.Message}");
                }
            }
        }
    }
}

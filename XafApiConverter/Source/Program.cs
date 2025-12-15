using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using XafApiConverter.Converter;

namespace XafApiConverter {
    static class Program {
        static void Main(string[] args) {
            // Check if user wants to run conversion CLI
            if(args.Length > 0 && args[0].Equals("convert", StringComparison.OrdinalIgnoreCase)) {
                // Remove "convert" from args and run CLI
                var cliArgs = args.Skip(1).ToArray();
                Environment.Exit(ConversionCli.Run(cliArgs));
                return;
            }

            // Check if user wants to run type migration
            if(args.Length > 0 && args[0].Equals("migrate-types", StringComparison.OrdinalIgnoreCase)) {
                // Remove "migrate-types" from args and run type migration CLI
                var cliArgs = args.Skip(1).ToArray();
                Environment.Exit(TypeMigrationCli.Run(cliArgs));
                return;
            }

            string solutionPath;
            if(args.Length == 0) {
                Console.WriteLine($"Usage: {typeof(Program).Assembly.GetName().Name}.exe <PathToSolution | PathToDirectory>");
                return;
            } else {
                solutionPath = args[0];
            }

            var solutions = new List<string>();
            if(File.Exists(solutionPath)) {
                solutions.Add(solutionPath);
            } else {
                solutions.AddRange(Directory.GetFiles(solutionPath, "*.sln", SearchOption.AllDirectories));
                solutions.AddRange(Directory.GetFiles(solutionPath, "*.slnx", SearchOption.AllDirectories));
            }

            try {
                MSBuildLocator.RegisterDefaults();
                foreach(string solution in solutions) {
                    Console.WriteLine(solution);
                    ProcessSolution(solution);
                }
            } catch(Exception ex) {
                Console.WriteLine(ex.ToString());
            }
        }

        static void ProcessSolution(string solutionPath) {
            using(var workspace = MSBuildWorkspace.Create()) {
                Solution solution = workspace.OpenSolutionAsync(solutionPath).Result;
                foreach(Project project in solution.Projects) {
                    Console.WriteLine(project.FilePath);

                    // Convert project to SDK-style if needed
                    // Uncomment the line below to enable automatic conversion
                    // CSprojConverter.Convert(project);

                    foreach(var document in project.Documents) {
                        if(!document.FilePath.EndsWith(".cs")) {
                            continue;
                        }
                        SecurityTypesUpdater.ProcessDocument(document);
                    }
                }
            }
        }
    }
}
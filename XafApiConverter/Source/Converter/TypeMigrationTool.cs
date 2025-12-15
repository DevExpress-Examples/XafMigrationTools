using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace XafApiConverter.Converter {
    /// <summary>
    /// Main type migration tool - hybrid approach (programmatic + LLM)
    /// Implements TRANS-006, TRANS-007, TRANS-008 automatically
    /// Detects TRANS-009, TRANS-010 problems for LLM analysis
    /// </summary>
    internal class TypeMigrationTool {
        private readonly string _solutionPath;
        private Solution _solution;
        private MigrationReport _report;

        public TypeMigrationTool(string solutionPath) {
            _solutionPath = solutionPath;
            _report = new MigrationReport { SolutionPath = solutionPath };
        }

        /// <summary>
        /// Run complete migration workflow
        /// </summary>
        public MigrationReport RunMigration() {
            Console.WriteLine("Starting Type Migration...");
            Console.WriteLine();

            try {
                // Phase 1: Load solution
                Console.WriteLine("Phase 1: Loading solution...");
                LoadSolution();

                // Phase 2: Apply automatic replacements
                Console.WriteLine("Phase 2: Applying automatic replacements...");
                ApplyAutomaticReplacements();

                // Phase 3: Detect problems
                Console.WriteLine("Phase 3: Detecting problems for LLM analysis...");
                DetectProblems();

                // Phase 4: Build project
                Console.WriteLine("Phase 4: Building project...");
                BuildAndAnalyzeErrors();

                // Phase 5: Generate report
                Console.WriteLine("Phase 5: Generating report...");
                SaveReport();

                Console.WriteLine();
                Console.WriteLine("? Migration analysis complete!");
                _report.PrintSummary();

                return _report;
            }
            catch (Exception ex) {
                Console.WriteLine($"? Migration failed: {ex.Message}");
                throw;
            }
        }

        private void LoadSolution() {
            var workspace = MSBuildWorkspace.Create();
            _solution = workspace.OpenSolutionAsync(_solutionPath).Result;
            Console.WriteLine($"  Loaded solution: {Path.GetFileName(_solutionPath)}");
            Console.WriteLine($"  Projects: {_solution.Projects.Count()}");
        }

        /// <summary>
        /// Phase 2: Apply automatic namespace and type replacements
        /// TRANS-006, TRANS-007, TRANS-008
        /// </summary>
        private void ApplyAutomaticReplacements() {
            foreach (var project in _solution.Projects) {
                Console.WriteLine($"  Processing project: {project.Name}");

                // Process C# files
                foreach (var document in project.Documents) {
                    var filePath = document.FilePath;
                    var extension = Path.GetExtension(filePath);

                    if (extension == ".cs") {
                        ProcessCSharpFile(document);
                        _report.FilesProcessed++;
                    }
                    else if (extension == ".xafml") {
                        ProcessXafmlFile(filePath);
                        _report.XafmlFilesProcessed++;
                        _report.FilesProcessed++;
                    }
                }
            }
        }

        private void ProcessCSharpFile(Document document) {
            var syntaxTree = document.GetSyntaxTreeAsync().Result;
            if (syntaxTree == null) return;

            var root = syntaxTree.GetRoot();
            var originalRoot = root;

            // TRANS-006: SqlClient namespace
            var sqlClientReplacements = new Dictionary<string, string> {
                { "System.Data.SqlClient", "Microsoft.Data.SqlClient" }
            };

            foreach (var replacement in sqlClientReplacements) {
                var oldRoot = root;
                root = ReplaceUsingNamespace(root, replacement.Key, replacement.Value);
                if (root != oldRoot) {
                    _report.NamespacesReplaced++;
                }
            }

            // TRANS-007: DevExpress namespace migrations
            foreach (var nsReplacement in TypeReplacementMap.NamespaceReplacements.Values) {
                if (!nsReplacement.AppliesToFileType(".cs")) continue;
                if (!nsReplacement.HasEquivalent) continue;

                var oldRoot = root;
                root = ReplaceUsingNamespace(root, nsReplacement.OldNamespace, nsReplacement.NewNamespace);
                if (root != oldRoot) {
                    _report.NamespacesReplaced++;
                }
            }

            // TRANS-008: Type replacements
            foreach (var typeReplacement in TypeReplacementMap.TypeReplacements.Values) {
                if (!typeReplacement.AppliesToFileType(".cs")) continue;
                if (!typeReplacement.HasEquivalent) continue;

                var oldRoot = root;
                var rewriter = new TypeReplaceRewriter(
                    typeReplacement.GetFullOldTypeName(),
                    typeReplacement.GetFullNewTypeName());
                root = rewriter.Visit(root);

                if (root != oldRoot) {
                    _report.TypesReplaced++;
                }
            }

            // Save if changed
            if (root != originalRoot) {
                File.WriteAllText(document.FilePath, root.ToFullString());
            }
        }

        /// <summary>
        /// Replace using namespace directive
        /// </summary>
        private SyntaxNode ReplaceUsingNamespace(SyntaxNode root, string oldNamespace, string newNamespace) {
            var compilationUnit = root as CompilationUnitSyntax;
            if (compilationUnit == null) return root;

            var newUsings = new List<UsingDirectiveSyntax>();
            bool replaced = false;

            foreach (var usingDirective in compilationUnit.Usings) {
                var namespaceName = usingDirective.Name.ToString();
                
                if (namespaceName == oldNamespace) {
                    // Replace with new namespace
                    var newUsing = SyntaxFactory.UsingDirective(
                        SyntaxFactory.IdentifierName(newNamespace)
                            .WithLeadingTrivia(SyntaxFactory.Space))
                        .WithTrailingTrivia(usingDirective.GetTrailingTrivia());
                    newUsings.Add(newUsing);
                    replaced = true;
                }
                else {
                    newUsings.Add(usingDirective);
                }
            }

            if (replaced) {
                return compilationUnit.WithUsings(SyntaxFactory.List(newUsings));
            }

            return root;
        }

        private void ProcessXafmlFile(string filePath) {
            var content = File.ReadAllText(filePath);
            var originalContent = content;

            // TRANS-007: Namespace replacements in XAFML
            foreach (var nsReplacement in TypeReplacementMap.NamespaceReplacements.Values) {
                if (!nsReplacement.AppliesToFileType(".xafml")) continue;
                if (!nsReplacement.HasEquivalent) continue;

                var oldContent = content;
                content = content.Replace(nsReplacement.OldNamespace, nsReplacement.NewNamespace);
                if (content != oldContent) {
                    _report.NamespacesReplaced++;
                }
            }

            // TRANS-008: Type replacements in XAFML (use full type names)
            foreach (var typeReplacement in TypeReplacementMap.TypeReplacements.Values) {
                if (!typeReplacement.AppliesToFileType(".xafml")) continue;
                if (!typeReplacement.HasEquivalent) continue;

                var oldTypeName = typeReplacement.GetFullOldTypeName();
                var newTypeName = typeReplacement.GetFullNewTypeName();

                var oldContent = content;
                content = content.Replace(oldTypeName, newTypeName);
                if (content != oldContent) {
                    _report.TypesReplaced++;
                }
            }

            // Save if changed
            if (content != originalContent) {
                File.WriteAllText(filePath, content);
            }
        }

        /// <summary>
        /// Phase 3: Detect problems for LLM analysis
        /// TRANS-009: Classes using NO_EQUIVALENT types
        /// </summary>
        private void DetectProblems() {
            foreach (var project in _solution.Projects) {
                var detector = new ProblemDetector(_solution);

                // Detect problematic C# classes
                var problematicClasses = detector.FindClassesWithNoEquivalentTypes(project);
                
                // Find dependencies for each problematic class
                foreach (var problematicClass in problematicClasses) {
                    var dependents = detector.FindDependentClasses(project, problematicClass.ClassName);
                    problematicClass.DependentClasses = dependents;
                }

                _report.ProblematicClasses.AddRange(problematicClasses);

                // Detect XAFML problems
                var xafmlProblems = detector.AnalyzeXafmlFiles(project);
                _report.XafmlProblems.AddRange(xafmlProblems);
            }

            Console.WriteLine($"  Found {_report.ProblematicClasses.Count} problematic classes");
            Console.WriteLine($"  Found {_report.XafmlProblems.Count} XAFML problems");
        }

        /// <summary>
        /// Phase 4: Build project and categorize errors
        /// </summary>
        private void BuildAndAnalyzeErrors() {
            // Note: This is a simplified version
            // In real implementation, you would use:
            // - upgrade_build_project() from upgrade tools
            // - upgrade_get_current_dotnet_build_errors() to get errors
            
            // For now, just mark as not built yet
            _report.BuildSuccessful = false;
            
            // Placeholder for build errors
            // In real implementation:
            // var buildResult = BuildProject(_solutionPath);
            // var errors = GetBuildErrors(buildResult);
            // var (fixable, unfixable) = detector.CategorizeErrors(errors);
            // _report.FixableErrors = fixable;
            // _report.UnfixableErrors = unfixable;

            Console.WriteLine("  Build analysis skipped (requires integration with upgrade tools)");
        }

        /// <summary>
        /// Phase 5: Generate and save report
        /// </summary>
        private void SaveReport() {
            var reportPath = Path.Combine(
                Path.GetDirectoryName(_solutionPath),
                "type-migration-report.md");

            _report.SaveToFile(reportPath);
            Console.WriteLine($"  Report saved to: {reportPath}");
        }

        /// <summary>
        /// Get migration statistics
        /// </summary>
        public MigrationReport GetReport() => _report;
    }

    /// <summary>
    /// Extension methods for easier usage
    /// </summary>
    internal static class TypeMigrationExtensions {
        /// <summary>
        /// Run type migration on a solution
        /// </summary>
        public static MigrationReport MigrateTypes(this string solutionPath) {
            var tool = new TypeMigrationTool(solutionPath);
            return tool.RunMigration();
        }
    }
}

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

                // Phase 6: Comment out problematic classes (NEW)
                Console.WriteLine("Phase 6: Commenting out problematic classes...");
                CommentOutProblematicClasses();

                Console.WriteLine();
                Console.WriteLine("[OK] Migration analysis complete!");
                _report.PrintSummary();

                return _report;
            }
            catch (Exception ex) {
                Console.WriteLine($"[ERROR] Migration failed: {ex.Message}");
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

            // NEW: Remove using directives for NO_EQUIVALENT namespaces
            foreach (var nsReplacement in TypeReplacementMap.NoEquivalentNamespaces.Values) {
                if (!nsReplacement.AppliesToFileType(".cs")) continue;

                var oldRoot = root;
                root = RemoveUsingNamespace(root, nsReplacement.OldNamespace);
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

        /// <summary>
        /// Remove using namespace directive for NO_EQUIVALENT namespaces
        /// </summary>
        private SyntaxNode RemoveUsingNamespace(SyntaxNode root, string namespaceToRemove) {
            var compilationUnit = root as CompilationUnitSyntax;
            if (compilationUnit == null) return root;

            var newUsings = new List<UsingDirectiveSyntax>();
            bool removed = false;

            foreach (var usingDirective in compilationUnit.Usings) {
                var namespaceName = usingDirective.Name.ToString();
                
                // Check for exact match or if it starts with the namespace
                if (namespaceName == namespaceToRemove || 
                    namespaceName.StartsWith(namespaceToRemove + ".")) {
                    // Skip this using directive (remove it)
                    removed = true;
                }
                else {
                    newUsings.Add(usingDirective);
                }
            }

            if (removed) {
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
            Console.WriteLine("  Building solution...");
            
            try {
                var buildResult = BuildSolution(_solutionPath);
                
                if (buildResult.Success) {
                    _report.BuildSuccessful = true;
                    Console.WriteLine("  Build succeeded!");
                }
                else {
                    _report.BuildSuccessful = false;
                    Console.WriteLine($"  Build failed with {buildResult.Errors.Count} error(s)");
                    
                    // Categorize errors
                    CategorizeErrors(buildResult.Errors);
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"  Build analysis failed: {ex.Message}");
                _report.BuildSuccessful = false;
            }
        }

        /// <summary>
        /// Build solution using dotnet CLI
        /// </summary>
        private BuildResult BuildSolution(string solutionPath) {
            var result = new BuildResult();
            
            var processInfo = new System.Diagnostics.ProcessStartInfo {
                FileName = "dotnet",
                Arguments = $"build \"{solutionPath}\" --no-restore",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(solutionPath)
            };

            using (var process = System.Diagnostics.Process.Start(processInfo)) {
                var output = process.StandardOutput.ReadToEnd();
                var errors = process.StandardError.ReadToEnd();
                
                process.WaitForExit();
                
                result.Success = process.ExitCode == 0;
                result.ExitCode = process.ExitCode;
                
                // Parse errors from output
                if (!result.Success) {
                    result.Errors = ParseBuildErrors(output + errors);
                }
            }
            
            return result;
        }

        /// <summary>
        /// Parse build errors from dotnet build output
        /// </summary>
        private List<BuildError> ParseBuildErrors(string buildOutput) {
            var errors = new List<BuildError>();
            
            // Pattern: FilePath(Line,Col): error CS0000: Message
            var errorPattern = @"(.+?)\((\d+),(\d+)\):\s+(error|warning)\s+(\w+):\s+(.+)";
            var matches = Regex.Matches(buildOutput, errorPattern);
            
            foreach (Match match in matches) {
                if (match.Groups.Count >= 7) {
                    var error = new BuildError {
                        FilePath = match.Groups[1].Value.Trim(),
                        Line = int.Parse(match.Groups[2].Value),
                        Column = int.Parse(match.Groups[3].Value),
                        Severity = match.Groups[4].Value,
                        Code = match.Groups[5].Value,
                        Message = match.Groups[6].Value.Trim()
                    };
                    
                    errors.Add(error);
                }
            }
            
            return errors;
        }

        /// <summary>
        /// Categorize errors into fixable and unfixable
        /// </summary>
        private void CategorizeErrors(List<BuildError> errors) {
            var detector = new ProblemDetector(_solution);
            
            foreach (var error in errors.Where(e => e.Severity == "error")) {
                // Check if error is related to NO_EQUIVALENT types
                var isNoEquivalent = detector.IsNoEquivalentError(error);
                
                if (isNoEquivalent) {
                    // Unfixable - requires commenting out
                    _report.UnfixableErrors.Add(new UnfixableError {
                        Code = error.Code,
                        Message = error.Message,
                        FilePath = error.FilePath,
                        Line = error.Line,
                        Column = error.Column,
                        Reason = "Type has no .NET equivalent - requires commenting out or manual replacement"
                    });
                }
                else {
                    // Potentially fixable
                    var suggestedFix = detector.SuggestFix(error);
                    
                    if (!string.IsNullOrEmpty(suggestedFix)) {
                        _report.FixableErrors.Add(new FixableError {
                            Code = error.Code,
                            Message = error.Message,
                            FilePath = error.FilePath,
                            Line = error.Line,
                            Column = error.Column,
                            SuggestedFix = suggestedFix
                        });
                    }
                    else {
                        _report.UnfixableErrors.Add(new UnfixableError {
                            Code = error.Code,
                            Message = error.Message,
                            FilePath = error.FilePath,
                            Line = error.Line,
                            Column = error.Column,
                            Reason = "Requires manual review"
                        });
                    }
                }
            }
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
        /// Phase 6: Comment out problematic classes automatically
        /// Implements TRANS-010 lightweight version
        /// </summary>
        private void CommentOutProblematicClasses() {
            var commenter = new ClassCommenter(_report);
            var commentedCount = commenter.CommentOutProblematicClasses();

            if (commentedCount > 0) {
                Console.WriteLine($"  Commented out {commentedCount} classes");
                
                // Update report with commented classes
                _report.ClassesCommented = commentedCount;
                _report.CommentedClassNames = commenter.GetCommentedClasses().ToList();
            }
            else {
                Console.WriteLine("  No classes needed commenting");
            }
        }

        /// <summary>
        /// Get migration statistics
        /// </summary>
        public MigrationReport GetReport() => _report;
    }

    /// <summary>
    /// Build result container
    /// </summary>
    internal class BuildResult {
        public bool Success { get; set; }
        public int ExitCode { get; set; }
        public List<BuildError> Errors { get; set; } = new();
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

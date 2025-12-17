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
        
        /// <summary>
        /// Cache of original semantic models and syntax trees before any modifications.
        /// Key: Document.FilePath
        /// Value: (SemanticModel, SyntaxTree, Document)
        /// This cache is populated once after LoadSolution and never updated.
        /// Use this for dependency analysis and type resolution on original code.
        /// </summary>
        private Dictionary<string, (SemanticModel SemanticModel, SyntaxTree SyntaxTree, Microsoft.CodeAnalysis.Document Document)> _semanticCache;

        public TypeMigrationTool(string solutionPath) {
            _solutionPath = solutionPath;
            _report = new MigrationReport { SolutionPath = solutionPath };
            _semanticCache = new Dictionary<string, (SemanticModel, SyntaxTree, Microsoft.CodeAnalysis.Document)>();
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
                
                // Phase 1.5: Build semantic cache of original state
                Console.WriteLine("Phase 1.5: Building semantic cache...");
                BuildSemanticCache();

                // Phase 2: Detect and comment out problematic classes FIRST (before changing usings!)
                // This allows using directives analysis to work correctly for namespace resolution
                Console.WriteLine("Phase 2: Detecting and commenting out problematic classes...");
                DetectProblems();
                CommentOutProblematicClasses();

                // Phase 3: Apply automatic replacements (usings + types)
                // Now it's safe to change usings since problematic classes are already commented
                Console.WriteLine("Phase 3: Applying automatic replacements...");
                ApplyAutomaticReplacements();

                // Phase 4: Build project
                Console.WriteLine("Phase 4: Building project...");
                BuildAndAnalyzeErrors();

                // Phase 5: Generate report
                Console.WriteLine("Phase 5: Generating report...");
                SaveReport();

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
        /// Build semantic cache for all C# documents in the solution.
        /// This cache represents the ORIGINAL state before any modifications.
        /// Use this cache for dependency analysis and type resolution.
        /// </summary>
        private void BuildSemanticCache() {
            int totalDocuments = 0;
            int cachedDocuments = 0;
            
            foreach (var project in _solution.Projects) {
                foreach (var document in project.Documents) {
                    if (!document.FilePath.EndsWith(".cs")) {
                        continue;
                    }
                    
                    totalDocuments++;
                    
                    try {
                        var syntaxTree = document.GetSyntaxTreeAsync().Result;
                        var semanticModel = document.GetSemanticModelAsync().Result;
                        
                        if (syntaxTree != null && semanticModel != null) {
                            _semanticCache[document.FilePath] = (semanticModel, syntaxTree, document);
                            cachedDocuments++;
                        }
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"    [WARNING] Failed to cache document {Path.GetFileName(document.FilePath)}: {ex.Message}");
                    }
                }
            }
            
            Console.WriteLine($"  Cached {cachedDocuments}/{totalDocuments} documents");
        }
        
        /// <summary>
        /// Get semantic model from cache for a given file path.
        /// Returns null if not found in cache.
        /// </summary>
        internal (SemanticModel SemanticModel, SyntaxTree SyntaxTree, Microsoft.CodeAnalysis.Document Document)? GetCachedSemanticModel(string filePath) {
            if (_semanticCache.TryGetValue(filePath, out var cached)) {
                return cached;
            }
            return null;
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
            // CRITICAL: Read file from DISK, not from Roslyn document cache!
            // Phase 2 (ClassCommenter) may have modified files on disk,
            // but Roslyn workspace still has old cached syntax trees.
            // We must read the current state from disk to preserve Phase 2 changes.
            
            var filePath = document.FilePath;
            if (!File.Exists(filePath)) {
                Console.WriteLine($"    [WARNING] File not found: {filePath}");
                return;
            }
            
            // Read current content from disk (may contain Phase 2 modifications)
            var fileContent = File.ReadAllText(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(fileContent);
            var root = syntaxTree.GetRoot();
            var originalRoot = root;

            // Use unified static methods for processing
            int namespacesReplaced = 0;
            int typesReplaced = 0;
            
            root = ProcessUsingsInRoot(root, ref namespacesReplaced);
            root = ProcessTypesInRoot(root, ref typesReplaced);

            _report.NamespacesReplaced += namespacesReplaced;
            _report.TypesReplaced += typesReplaced;

            // Save if changed
            if (root != originalRoot) {
                File.WriteAllText(filePath, root.ToFullString());
            }
        }

        /// <summary>
        /// Process using directives in syntax root.
        /// Internal static helper for tests and production use.
        /// Handles SqlClient namespace, DevExpress namespaces, and NO_EQUIVALENT namespace removal.
        /// </summary>
        internal static SyntaxNode ProcessUsingsInRoot(SyntaxNode root) {
            int dummy = 0;
            return ProcessUsingsInRoot(root, ref dummy);
        }

        /// <summary>
        /// Process using directives with counter update.
        /// </summary>
        private static SyntaxNode ProcessUsingsInRoot(SyntaxNode root, ref int replacedCount) {
            var compilationUnit = root as CompilationUnitSyntax;
            if (compilationUnit == null) return root;

            var newUsings = new List<UsingDirectiveSyntax>();
            bool modified = false;

            foreach (var usingDirective in compilationUnit.Usings) {
                var namespaceName = usingDirective.Name?.ToString();
                
                // Check if should be removed (NO_EQUIVALENT namespaces)
                if (ShouldRemoveNamespace(namespaceName)) {
                    modified = true;
                    replacedCount++;
                    continue; // Skip (remove)
                }

                // Check if should be replaced
                var newNamespace = GetNamespaceReplacement(namespaceName);
                if (newNamespace != null) {
                    // Create new using with proper whitespace: "using Namespace;"
                    // We need to copy all trivia from original
                    var newName = SyntaxFactory.ParseName(newNamespace);
                    var newUsing = SyntaxFactory.UsingDirective(newName)
                        .WithUsingKeyword(usingDirective.UsingKeyword) // Keep "using" keyword with its trivia
                        .WithSemicolonToken(usingDirective.SemicolonToken); // Keep semicolon with its trivia
                    
                    newUsings.Add(newUsing);
                    modified = true;
                    replacedCount++;
                    continue;
                }

                // Keep unchanged
                newUsings.Add(usingDirective);
            }

            return modified ? compilationUnit.WithUsings(SyntaxFactory.List(newUsings)) : root;
        }

        /// <summary>
        /// Check if namespace should be removed (NO_EQUIVALENT).
        /// </summary>
        private static bool ShouldRemoveNamespace(string namespaceName) {
            return TypeReplacementMap.NoEquivalentNamespaces.ContainsKey(namespaceName) ||
                   TypeReplacementMap.NoEquivalentNamespaces.Values.Any(ns => 
                       namespaceName.StartsWith(ns.OldNamespace + "."));
        }

        /// <summary>
        /// Get replacement for namespace or null if no replacement needed.
        /// Handles SqlClient and DevExpress namespace migrations.
        /// </summary>
        private static string GetNamespaceReplacement(string namespaceName) {
            // TRANS-006: SqlClient namespace
            if (namespaceName == "System.Data.SqlClient") {
                return "Microsoft.Data.SqlClient";
            }

            // TRANS-007: DevExpress namespaces
            if (TypeReplacementMap.NamespaceReplacements.TryGetValue(namespaceName, out var replacement)) {
                if (replacement.HasEquivalent && replacement.AppliesToFileType(".cs")) {
                    return replacement.NewNamespace;
                }
            }

            return null;
        }

        /// <summary>
        /// Process type replacements in syntax root.
        /// Internal static helper for tests and production use.
        /// Applies all type replacements from TypeReplacementMap.
        /// </summary>
        internal static SyntaxNode ProcessTypesInRoot(SyntaxNode root) {
            int dummy = 0;
            return ProcessTypesInRoot(root, ref dummy);
        }

        /// <summary>
        /// Process type replacements with counter update.
        /// </summary>
        private static SyntaxNode ProcessTypesInRoot(SyntaxNode root, ref int replacedCount) {
            var originalRoot = root;

            // TRANS-008: Type replacements
            foreach (var typeReplacement in TypeReplacementMap.TypeReplacements.Values) {
                if (!typeReplacement.AppliesToFileType(".cs") || !typeReplacement.HasEquivalent) {
                    continue;
                }

                var oldRoot = root;
                var rewriter = new TypeReplaceRewriter(
                    typeReplacement.GetFullOldTypeName(),
                    typeReplacement.GetFullNewTypeName());
                root = rewriter.Visit(root);

                if (root != oldRoot) {
                    replacedCount++;
                }
            }

            return root;
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
        /// Uses cached semantic models from Phase 1.5 to analyze ORIGINAL code before any modifications.
        /// </summary>
        private void DetectProblems() {
            foreach (var project in _solution.Projects) {
                var detector = new ProblemDetector(_solution);

                // NEW: Analyze using cached semantic models (original state before modifications)
                var problematicClasses = new List<ProblematicClass>();
                
                foreach (var document in project.Documents) {
                    if (!document.FilePath.EndsWith(".cs")) continue;
                    
                    // Get cached semantic model and syntax tree (ORIGINAL state)
                    var cached = GetCachedSemanticModel(document.FilePath);
                    if (!cached.HasValue) {
                        Console.WriteLine($"    [WARNING] No cached semantic model for {Path.GetFileName(document.FilePath)}, skipping");
                        continue;
                    }
                    
                    var semanticModel = cached.Value.SemanticModel;
                    var syntaxTree = cached.Value.SyntaxTree;
                    var root = syntaxTree.GetRoot();
                    
                    // Extract using directives from ORIGINAL syntax tree
                    var usingDirectives = root.DescendantNodes()
                        .OfType<UsingDirectiveSyntax>()
                        .Select(u => u.Name?.ToString())
                        .Where(n => !string.IsNullOrEmpty(n))
                        .ToHashSet();
                    
                    // Analyze classes using ORIGINAL semantic model and syntax tree
                    var classesInFile = ProblemDetector.AnalyzeClassesInSyntaxTree(
                        document.FilePath,
                        root,
                        semanticModel,
                        usingDirectives);
                    
                    problematicClasses.AddRange(classesInFile);
                }
                
                // Find dependencies for each problematic class using semantic analysis
                foreach (var problematicClass in problematicClasses) {
                    // Use semantic cache and namespace for accurate dependency detection
                    var dependents = detector.FindDependentClasses(
                        project, 
                        problematicClass.ClassName,
                        problematicClass.Namespace,
                        _semanticCache);
                    
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
}

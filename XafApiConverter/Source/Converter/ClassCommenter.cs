using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace XafApiConverter.Converter {
    /// <summary>
    /// Automatically comments out classes with NO_EQUIVALENT types and their dependencies
    /// Implements TRANS-010: Build-Fix-Comment Iteration (lightweight version)
    /// </summary>
    internal class ClassCommenter {
        private readonly MigrationReport _report;
        private readonly HashSet<string> _commentedClasses = new();

        public ClassCommenter(MigrationReport report) {
            _report = report;
        }

        /// <summary>
        /// Comment out all problematic classes and their dependencies
        /// </summary>
        public int CommentOutProblematicClasses() {
            int totalCommented = 0;

            // Group classes by file to process each file only once
            var classesByFile = _report.ProblematicClasses
                .Where(c => c.Problems.Any(p => p.RequiresCommentOut))
                .GroupBy(c => c.FilePath)
                .ToList();

            if (!classesByFile.Any()) {
                Console.WriteLine("  No classes require commenting out.");
                return 0;
            }

            Console.WriteLine($"  Found {classesByFile.Sum(g => g.Count())} classes in {classesByFile.Count} files to comment out...");

            foreach (var fileGroup in classesByFile) {
                var filePath = fileGroup.Key;
                var classesToComment = fileGroup.ToList();

                // Process all classes in this file at once
                if (CommentOutClassesInFile(filePath, classesToComment)) {
                    foreach (var problematicClass in classesToComment) {
                        totalCommented++;
                        _commentedClasses.Add(problematicClass.ClassName);
                        Console.WriteLine($"    [COMMENTED] {problematicClass.ClassName} in {Path.GetFileName(filePath)}");

                        // Comment out dependent classes
                        foreach (var dependent in problematicClass.DependentClasses) {
                            if (_commentedClasses.Contains(dependent)) {
                                continue;
                            }

                            if (CommentOutDependentClass(dependent, problematicClass.ClassName)) {
                                totalCommented++;
                                _commentedClasses.Add(dependent);
                                Console.WriteLine($"    [COMMENTED] {dependent} (dependency)");
                            }
                        }
                    }
                }
            }

            return totalCommented;
        }

        /// <summary>
        /// Comment out multiple classes in a single file
        /// </summary>
        private bool CommentOutClassesInFile(string filePath, List<ProblematicClass> classesToComment) {
            try {
                if (!File.Exists(filePath)) {
                    Console.WriteLine($"      [ERROR] File not found: {filePath}");
                    return false;
                }

                var content = File.ReadAllText(filePath);
                var syntaxTree = CSharpSyntaxTree.ParseText(content);
                var root = syntaxTree.GetRoot() as CompilationUnitSyntax;

                if (root == null) {
                    Console.WriteLine($"      [ERROR] Could not parse file: {filePath}");
                    return false;
                }

                // Find all classes to comment
                var classNamesToComment = classesToComment.Select(c => c.ClassName).ToHashSet();
                var classDeclarations = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(c => classNamesToComment.Contains(c.Identifier.Text))
                    .ToList();

                if (!classDeclarations.Any()) {
                    Console.WriteLine($"      [ERROR] No classes found in file: {filePath}");
                    return false;
                }

                // Replace each class with commented version using Roslyn
                var newRoot = root;
                foreach (var originalClassDecl in classDeclarations) {
                    var problematicClass = classesToComment.First(c => c.ClassName == originalClassDecl.Identifier.Text);
                    
                    // CRITICAL FIX: Find the class in the CURRENT tree (newRoot), not the original
                    var currentClassDecl = newRoot.DescendantNodes()
                        .OfType<ClassDeclarationSyntax>()
                        .FirstOrDefault(c => c.Identifier.Text == originalClassDecl.Identifier.Text);
                    
                    if (currentClassDecl == null) {
                        Console.WriteLine($"      [WARNING] Class {originalClassDecl.Identifier.Text} not found in current tree, skipping");
                        continue;
                    }
                    
                    // Build comment
                    var comment = BuildClassComment(problematicClass);
                    
                    // Create commented trivia
                    var commentTrivia = CreateCommentedClassTrivia(comment, currentClassDecl);
                    
                    // Create empty node with comment trivia
                    var emptyStatement = SyntaxFactory.EmptyStatement()
                        .WithLeadingTrivia(commentTrivia);
                    
                    // Replace class with commented version (using current node from current tree)
                    newRoot = newRoot.ReplaceNode(currentClassDecl, emptyStatement);
                }

                // Save file
                File.WriteAllText(filePath, newRoot.ToFullString(), Encoding.UTF8);
                return true;
            }
            catch (Exception ex) {
                Console.WriteLine($"      [ERROR] Failed to comment out classes in {filePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Build comment header for class
        /// </summary>
        private string BuildClassComment(ProblematicClass problematicClass) {
            var sb = new StringBuilder();
            sb.AppendLine("// NOTE: Class commented out due to types having no XAF .NET equivalent");
            
            var reasons = problematicClass.Problems
                .Where(p => p.RequiresCommentOut)
                .Select(p => $"//   - {p.Reason}")
                .Distinct();
            
            foreach (var reason in reasons) {
                sb.AppendLine(reason);
            }
            
            sb.AppendLine("// TODO: Application behavior verification required and new solution if necessary");
            
            return sb.ToString();
        }

        /// <summary>
        /// Create trivia list for commented class
        /// </summary>
        private SyntaxTriviaList CreateCommentedClassTrivia(string comment, ClassDeclarationSyntax classDecl) {
            var triviaList = new List<SyntaxTrivia>();
            
            // Add comment lines
            foreach (var line in comment.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
                triviaList.Add(SyntaxFactory.Comment(line));
                triviaList.Add(SyntaxFactory.CarriageReturnLineFeed);
            }
            
            // Add /* opener
            triviaList.Add(SyntaxFactory.Comment("/*"));
            triviaList.Add(SyntaxFactory.CarriageReturnLineFeed);
            
            // Add class code
            var classText = classDecl.ToFullString();
            foreach (var line in classText.Split(new[] { '\r', '\n' }, StringSplitOptions.None)) {
                if (!string.IsNullOrEmpty(line)) {
                    triviaList.Add(SyntaxFactory.Whitespace(line));
                }
                triviaList.Add(SyntaxFactory.CarriageReturnLineFeed);
            }
            
            // Add */ closer
            triviaList.Add(SyntaxFactory.Comment("*/"));
            triviaList.Add(SyntaxFactory.CarriageReturnLineFeed);
            
            return SyntaxFactory.TriviaList(triviaList);
        }

        /// <summary>
        /// Comment out a dependent class
        /// </summary>
        private bool CommentOutDependentClass(string className, string dependencyName) {
            try {
                // Find the file containing this class
                var classInfo = _report.ProblematicClasses
                    .FirstOrDefault(c => c.ClassName == className);

                if (classInfo == null) {
                    Console.WriteLine($"      [WARNING] Class {className} not in report, skipping");
                    return false;
                }

                var filePath = classInfo.FilePath;
                if (!File.Exists(filePath)) {
                    Console.WriteLine($"      [ERROR] File not found: {filePath}");
                    return false;
                }

                var content = File.ReadAllText(filePath);
                var syntaxTree = CSharpSyntaxTree.ParseText(content);
                var root = syntaxTree.GetRoot() as CompilationUnitSyntax;

                if (root == null) {
                    Console.WriteLine($"      [ERROR] Could not parse file: {filePath}");
                    return false;
                }

                // Find the class declaration
                var classDecl = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault(c => c.Identifier.Text == className);

                if (classDecl == null) {
                    Console.WriteLine($"      [ERROR] Class not found in file: {className}");
                    return false;
                }

                // Build comment
                var comment = $@"// NOTE: Class commented out because it depends on '{dependencyName}' which has no XAF .NET equivalent
// TODO: Application behavior verification required and new solution if necessary
";

                // Create commented trivia
                var commentTrivia = CreateCommentedClassTrivia(comment, classDecl);
                
                // Create empty node with comment trivia
                var emptyStatement = SyntaxFactory.EmptyStatement()
                    .WithLeadingTrivia(commentTrivia);
                
                // Replace class with commented version
                var newRoot = root.ReplaceNode(classDecl, emptyStatement);

                // Save file
                File.WriteAllText(filePath, newRoot.ToFullString(), Encoding.UTF8);
                return true;
            }
            catch (Exception ex) {
                Console.WriteLine($"      [ERROR] Failed to comment out dependent class {className}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get list of all commented classes
        /// </summary>
        public IReadOnlyCollection<string> GetCommentedClasses() => _commentedClasses;
    }
}

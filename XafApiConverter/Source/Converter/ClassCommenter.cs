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

            // Get all classes that need to be commented out
            var classesToComment = _report.ProblematicClasses
                .Where(c => c.Problems.Any(p => p.RequiresCommentOut))
                .ToList();

            if (!classesToComment.Any()) {
                Console.WriteLine("  No classes require commenting out.");
                return 0;
            }

            Console.WriteLine($"  Found {classesToComment.Count} classes to comment out...");

            // Process each class individually with full reload after each change
            foreach (var problematicClass in classesToComment) {
                // Skip if already commented
                if (_commentedClasses.Contains(problematicClass.ClassName)) {
                    continue;
                }

                // Comment out this single class
                if (CommentOutSingleClass(problematicClass.FilePath, problematicClass.ClassName, problematicClass)) {
                    totalCommented++;
                    _commentedClasses.Add(problematicClass.ClassName);
                    Console.WriteLine($"    [COMMENTED] {problematicClass.ClassName} in {Path.GetFileName(problematicClass.FilePath)}");

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

            return totalCommented;
        }

        /// <summary>
        /// Comment out a single class in a file with full syntax tree reload
        /// </summary>
        private bool CommentOutSingleClass(string filePath, string className, ProblematicClass problematicClass) {
            try {
                if (!File.Exists(filePath)) {
                    Console.WriteLine($"      [ERROR] File not found: {filePath}");
                    return false;
                }

                // STEP 1: Load fresh content from disk
                var content = File.ReadAllText(filePath);
                
                // STEP 2: Check if class is already commented out (BEFORE parsing!)
                if (IsClassAlreadyCommented(content, className)) {
                    Console.WriteLine($"      [WARNING] Class {className} appears to be already commented out, skipping");
                    return false;
                }
                
                // STEP 3: Parse syntax tree
                var syntaxTree = CSharpSyntaxTree.ParseText(content);
                var root = syntaxTree.GetRoot() as CompilationUnitSyntax;

                if (root == null) {
                    Console.WriteLine($"      [ERROR] Could not parse file: {filePath}");
                    return false;
                }

                // STEP 4: Find the specific class by name (only active, uncommented classes!)
                var allClasses = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(c => c.Identifier.Text == className)
                    .Where(c => !IsClassInsideComment(c, content))  // NEW: Filter out classes inside comments!
                    .ToList();
                
                if (!allClasses.Any()) {
                    Console.WriteLine($"      [WARNING] Class {className} not found as active code in file: {filePath}");
                    return false;
                }
                
                // If multiple classes with same name found (shouldn't happen in valid C#), take the first
                var classDecl = allClasses.First();

                // STEP 5: Additional check - verify the class is not inside a comment
                // by checking if it's part of a trivia (comment)
                var classPosition = classDecl.SpanStart;
                var textBeforeClass = content.Substring(Math.Max(0, classPosition - 100), Math.Min(100, classPosition));
                
                // If there's a // ========== marker shortly before, it's already commented
                if (textBeforeClass.Contains("// ========== COMMENTED OUT CLASS", StringComparison.Ordinal)) {
                    Console.WriteLine($"      [WARNING] Class {className} is inside a commented block, skipping");
                    return false;
                }

                // STEP 6: Build comment header
                var comment = BuildClassComment(problematicClass);

                // STEP 7: Get ONLY the class code directly from file content
                // Use direct substring instead of GetText() to avoid Roslyn capturing commented code
                var classStartPosition = classDecl.SpanStart;
                var classLength = classDecl.Span.Length;
                var classText = content.Substring(classStartPosition, classLength);
                
                // Get leading trivia separately for indentation calculation
                var leadingTrivia = classDecl.GetLeadingTrivia();

                // STEP 8: Build commented version
                var commentedClass = BuildCommentedClassText(comment, classText, leadingTrivia);

                // STEP 9: Replace in content using SPAN positions
                var beforeClass = content.Substring(0, classStartPosition);
                var afterClass = content.Length > classStartPosition + classLength
                    ? content.Substring(classStartPosition + classLength)
                    : string.Empty;

                var newContent = beforeClass + commentedClass + afterClass;

                // STEP 10: Save file (this invalidates all syntax nodes)
                File.WriteAllText(filePath, newContent, Encoding.UTF8);
                
                Console.WriteLine($"      [SUCCESS] Class {className} commented out successfully");
                
                // STEP 11: Syntax tree is now stale - will be reloaded on next iteration
                return true;
            }
            catch (Exception ex) {
                Console.WriteLine($"      [ERROR] Failed to comment out class {className} in {filePath}: {ex.Message}");
                Console.WriteLine($"      Stack trace: {ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// Check if class is already commented out
        /// </summary>
        private bool IsClassAlreadyCommented(string content, string className) {
            // Look for multiple patterns to catch already commented classes:
            // Pattern 1: // ========== COMMENTED OUT CLASS ==========
            // Pattern 2: // public class ClassName (with any indentation)
            // Pattern 3: //     public class ClassName
            // Pattern 4: // internal class ClassName
            
            var commentMarker = "// ========== COMMENTED OUT CLASS ==========";
            var commentIndex = content.IndexOf(commentMarker, StringComparison.Ordinal);
            
            // Start from beginning if no marker found yet
            var searchStart = commentIndex >= 0 ? commentIndex : 0;
            
            // Search for various patterns of commented class declarations
            var patterns = new[] {
                $"// public class {className}",           // Standard pattern
                $"//     public class {className}",       // With standard indent
                $"// internal class {className}",        // Internal modifier
                $"//     internal class {className}",    // Internal with indent
                $"//\tpublic class {className}",         // Tab indentation
                $"//  public class {className}",         // Double space
                $"//    public class {className}",       // 4 spaces indent
                $"//private class {className}",          // Private
                $"//protected class {className}",        // Protected
            };
            
            // Check if any pattern matches in the search region (next 1000 characters)
            var searchLength = Math.Min(1000, content.Length - searchStart);
            if (searchLength <= 0) return false;
            
            var searchRegion = content.Substring(searchStart, searchLength);
            
            foreach (var pattern in patterns) {
                if (searchRegion.Contains(pattern, StringComparison.Ordinal)) {
                    return true;
                }
            }
            
            // Also check if the entire class declaration appears in comments
            // by searching for // followed by [PropertyEditor or [<any attribute related to this class>
            var attributePattern = $"// [PropertyEditor";
            if (searchRegion.Contains(attributePattern, StringComparison.Ordinal)) {
                // Check if class name appears nearby
                var attributeIndex = searchRegion.IndexOf(attributePattern, StringComparison.Ordinal);
                var nearbySearch = searchRegion.Substring(attributeIndex, Math.Min(300, searchRegion.Length - attributeIndex));
                if (nearbySearch.Contains(className, StringComparison.Ordinal)) {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Check if a class declaration is inside commented code
        /// by analyzing the actual file content at the class position
        /// </summary>
        private bool IsClassInsideComment(ClassDeclarationSyntax classDecl, string fileContent) {
            // Get the actual position of the class in the file
            var classPosition = classDecl.SpanStart;
            
            // Look backwards from the class position to find the start of the line
            var lineStart = classPosition;
            while (lineStart > 0 && fileContent[lineStart - 1] != '\n' && fileContent[lineStart - 1] != '\r') {
                lineStart--;
            }
            
            // Get the text from line start to class start
            var linePrefix = fileContent.Substring(lineStart, classPosition - lineStart);
            
            // If the line starts with //, the class is commented out
            var trimmedPrefix = linePrefix.TrimStart(' ', '\t');
            if (trimmedPrefix.StartsWith("//", StringComparison.Ordinal)) {
                return true;
            }
            
            // Additional check: look for comment marker nearby (within 200 chars before)
            var checkStart = Math.Max(0, classPosition - 200);
            var checkRegion = fileContent.Substring(checkStart, classPosition - checkStart);
            
            // If we find the comment marker recently, this class is inside commented block
            if (checkRegion.Contains("// ========== COMMENTED OUT CLASS ==========", StringComparison.Ordinal)) {
                // Check if there's a closing marker between the comment start and this class
                var closingMarker = "// ========================================";
                var commentStartIndex = checkRegion.LastIndexOf("// ========== COMMENTED OUT CLASS ==========", StringComparison.Ordinal);
                var afterCommentStart = checkRegion.Substring(commentStartIndex);
                
                // If no closing marker found after comment start, we're still inside the comment
                if (!afterCommentStart.Contains(closingMarker, StringComparison.Ordinal)) {
                    return true;
                }
            }
            
            return false;
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
        /// Build commented class text from original class
        /// </summary>
        private string BuildCommentedClassText(string commentHeader, string classText, SyntaxTriviaList leadingTrivia) {
            var sb = new StringBuilder();
            
            // Extract base indentation from the leading trivia
            var baseIndent = string.Empty;
            var lastWhitespace = leadingTrivia.LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia));
            if (lastWhitespace != default(SyntaxTrivia)) {
                baseIndent = lastWhitespace.ToString();
            }
            
            // Add comment header
            foreach (var line in commentHeader.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)) {
                sb.Append(baseIndent);
                sb.AppendLine(line);
            }
            
            // Add separator
            sb.Append(baseIndent);
            sb.AppendLine("// ========== COMMENTED OUT CLASS ==========");
            
            // Comment out each line of the class
            var lines = classText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            for (int i = 0; i < lines.Length; i++) {
                var line = lines[i];
                
                // Skip the last empty line if it exists
                if (i == lines.Length - 1 && string.IsNullOrWhiteSpace(line)) {
                    continue;
                }
                
                if (string.IsNullOrWhiteSpace(line)) {
                    sb.Append(baseIndent);
                    sb.AppendLine("//");
                } else {
                    sb.Append(baseIndent);
                    sb.Append("// ");
                    sb.AppendLine(line);
                }
            }
            
            // Add closing separator
            sb.Append(baseIndent);
            sb.AppendLine("// ========================================");
            
            return sb.ToString();
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

                // STEP 1: Load fresh content from disk
                var content = File.ReadAllText(filePath);
                
                // STEP 2: Check if class is already commented out (BEFORE parsing!)
                if (IsClassAlreadyCommented(content, className)) {
                    Console.WriteLine($"      [WARNING] Class {className} appears to be already commented out, skipping");
                    return false;
                }
                
                // STEP 3: Parse syntax tree
                var syntaxTree = CSharpSyntaxTree.ParseText(content);
                var root = syntaxTree.GetRoot() as CompilationUnitSyntax;

                if (root == null) {
                    Console.WriteLine($"      [ERROR] Could not parse file: {filePath}");
                    return false;
                }

                // STEP 4: Find the class declaration (only active classes!)
                var allClasses = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(c => c.Identifier.Text == className)
                    .Where(c => !IsClassInsideComment(c, content))  // NEW: Filter out classes inside comments!
                    .ToList();

                if (!allClasses.Any()) {
                    Console.WriteLine($"      [WARNING] Class {className} not found as active code in file: {filePath}");
                    return false;
                }
                
                var classDecl = allClasses.First();
                
                // STEP 5: Additional check - verify the class is not inside a comment
                var classPosition = classDecl.SpanStart;
                var textBeforeClass = content.Substring(Math.Max(0, classPosition - 100), Math.Min(100, classPosition));
                
                if (textBeforeClass.Contains("// ========== COMMENTED OUT CLASS", StringComparison.Ordinal)) {
                    Console.WriteLine($"      [WARNING] Class {className} is inside a commented block, skipping");
                    return false;
                }

                // STEP 6: Build comment
                var comment = $@"// NOTE: Class commented out because it depends on '{dependencyName}' which has no XAF .NET equivalent
// TODO: Application behavior verification required and new solution if necessary
";

                // STEP 7: Get ONLY the class code directly from file content
                // Use direct substring instead of GetText() to avoid Roslyn capturing commented code
                var classStartPosition = classDecl.SpanStart;
                var classLength = classDecl.Span.Length;
                var classText = content.Substring(classStartPosition, classLength);
                var leadingTrivia = classDecl.GetLeadingTrivia();
                
                // STEP 8: Build commented version
                var commentedClass = BuildCommentedClassText(comment, classText, leadingTrivia);
                
                // STEP 9: Replace in content using SPAN positions
                var beforeClass = content.Substring(0, classStartPosition);
                var afterClass = content.Length > classStartPosition + classLength 
                    ? content.Substring(classStartPosition + classLength) 
                    : string.Empty;
                
                var newContent = beforeClass + commentedClass + afterClass;

                // STEP 10: Save file
                File.WriteAllText(filePath, newContent, Encoding.UTF8);
                
                Console.WriteLine($"      [SUCCESS] Dependent class {className} commented out successfully");
                
                return true;
            }
            catch (Exception ex) {
                Console.WriteLine($"      [ERROR] Failed to comment out dependent class {className}: {ex.Message}");
                Console.WriteLine($"      Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Get list of all commented classes
        /// </summary>
        public IReadOnlyCollection<string> GetCommentedClasses() => _commentedClasses;
    }
}

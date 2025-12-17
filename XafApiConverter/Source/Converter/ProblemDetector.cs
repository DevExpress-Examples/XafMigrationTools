using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace XafApiConverter.Converter {
    /// <summary>
    /// Detects problematic code patterns that require LLM intervention
    /// </summary>
    internal class ProblemDetector {
        private readonly Solution _solution;

        public ProblemDetector(Solution solution) {
            _solution = solution;
        }

        /// <summary>
        /// Find all classes that use types with no Blazor equivalent
        /// </summary>
        public List<ProblematicClass> FindClassesWithNoEquivalentTypes(Project project) {
            var problematicClasses = new List<ProblematicClass>();

            foreach (var document in project.Documents) {
                if (!document.FilePath.EndsWith(".cs")) continue;

                var syntaxTree = document.GetSyntaxTreeAsync().Result;
                if (syntaxTree == null) continue;

                var root = syntaxTree.GetRoot();
                var semanticModel = document.GetSemanticModelAsync().Result;
                if (semanticModel == null) continue;

                // Find class declarations
                var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var classDecl in classes) {
                    var problems = AnalyzeClass(classDecl, semanticModel, document.FilePath);
                    if (problems.Any()) {
                        problematicClasses.Add(new ProblematicClass {
                            ClassName = classDecl.Identifier.Text,
                            FilePath = document.FilePath,
                            Problems = problems
                        });
                    }
                }
            }

            return problematicClasses;
        }

        /// <summary>
        /// Analyze classes in a syntax tree for problematic types.
        /// This is a stateless helper method that can be used by tests.
        /// </summary>
        /// <param name="filePath">Path to the file being analyzed</param>
        /// <param name="root">Syntax tree root</param>
        /// <param name="semanticModel">Semantic model for type resolution</param>
        /// <param name="usingDirectives">Using directives for fallback namespace resolution</param>
        /// <returns>List of problematic classes found</returns>
        internal static List<ProblematicClass> AnalyzeClassesInSyntaxTree(
            string filePath,
            SyntaxNode root,
            SemanticModel semanticModel,
            HashSet<string> usingDirectives) {
            
            var problematicClasses = new List<ProblematicClass>();
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var classDecl in classes) {
                // Use unified analysis logic
                var problems = AnalyzeSingleClass(classDecl, semanticModel, usingDirectives);
                
                if (problems.Any(p => p.RequiresCommentOut)) {
                    problematicClasses.Add(new ProblematicClass {
                        ClassName = classDecl.Identifier.Text,
                        FilePath = filePath,
                        Problems = problems
                    });
                }
            }

            return problematicClasses;
        }

        /// <summary>
        /// Core analysis logic for a single class.
        /// Used by both production code and tests to ensure consistency.
        /// </summary>
        /// <param name="classDecl">Class declaration to analyze</param>
        /// <param name="semanticModel">Semantic model for type resolution</param>
        /// <param name="usingDirectives">Using directives for fallback namespace resolution (can be null)</param>
        /// <returns>List of type problems found in the class</returns>
        internal static List<TypeProblem> AnalyzeSingleClass(
            ClassDeclarationSyntax classDecl,
            SemanticModel semanticModel,
            HashSet<string> usingDirectives) {
            
            var problems = new List<TypeProblem>();

            // 1. Check base classes
            if (classDecl.BaseList != null) {
                foreach (var baseType in classDecl.BaseList.Types) {
                    var typeName = baseType.Type.ToString();
                    var typeSymbol = semanticModel.GetSymbolInfo(baseType.Type).Symbol as INamedTypeSymbol;

                    if (typeSymbol != null && !typeSymbol.ToDisplayString().StartsWith("?")) {
                        // Semantic model resolved - use full type name
                        var fullTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                            .Replace("global::", "");
                        
                        CheckTypeAgainstMaps(typeName, fullTypeName, problems);
                    }
                    else if (usingDirectives != null) {
                        // Fallback to using directives
                        CheckTypeUsingDirectives(typeName, usingDirectives, problems);
                    }
                }
            }

            // 2. Check for problematic enum usages (e.g., TemplateType)
            var memberAccess = classDecl.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
            foreach (var access in memberAccess) {
                var text = access.ToString();
                
                if (text.Contains("TemplateType.Horizontal") || text.Contains("TemplateType.Vertical")) {
                    problems.Add(new TypeProblem {
                        TypeName = "TemplateType",
                        FullTypeName = "DevExpress.ExpressApp.Web.Templates.TemplateType",
                        Reason = "Uses TemplateType enum which has no Blazor equivalent",
                        Description = "TemplateType enum has no Blazor equivalent",
                        Severity = ProblemSeverity.Critical,
                        RequiresCommentOut = true
                    });
                    break; // Only add once
                }
            }

            // 3. Check for NO_EQUIVALENT and MANUAL_CONVERSION_REQUIRED types used in code
            var identifiers = classDecl.DescendantNodes().OfType<IdentifierNameSyntax>();
            foreach (var identifier in identifiers) {
                var symbol = semanticModel.GetSymbolInfo(identifier).Symbol as INamedTypeSymbol;
                
                if (symbol != null && !symbol.ToDisplayString().StartsWith("?")) {
                    // Semantic model resolved
                    var fullTypeName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                        .Replace("global::", "");
                    
                    CheckTypeAgainstMaps(identifier.Identifier.Text, fullTypeName, problems);
                }
                else if (usingDirectives != null) {
                    // Fallback to using directives for identifiers
                    var typeName = identifier.Identifier.Text;
                    if (TypeReplacementMap.NoEquivalentTypes.ContainsKey(typeName) ||
                        TypeReplacementMap.ManualConversionRequiredTypes.ContainsKey(typeName)) {
                        CheckTypeUsingDirectives(typeName, usingDirectives, problems);
                    }
                }
            }

            // Deduplicate problems by FullTypeName
            return problems
                .GroupBy(p => p.FullTypeName)
                .Select(g => g.First())
                .ToList();
        }

        /// <summary>
        /// Check type against TypeReplacementMap using full type name
        /// </summary>
        private static void CheckTypeAgainstMaps(string typeName, string fullTypeName, List<TypeProblem> problems) {
            // Check NoEquivalentTypes
            var matchingNoEquiv = TypeReplacementMap.NoEquivalentTypes.Values
                .FirstOrDefault(t => {
                    var expectedFullName = t.GetFullOldTypeName();
                    return fullTypeName.Equals(expectedFullName, StringComparison.Ordinal) ||
                           fullTypeName.EndsWith($".{expectedFullName}", StringComparison.Ordinal);
                });

            if (matchingNoEquiv != null) {
                problems.Add(new TypeProblem {
                    TypeName = typeName,
                    FullTypeName = fullTypeName,
                    Reason = $"Base class '{typeName}' has no equivalent in XAF .NET",
                    Description = matchingNoEquiv.Description,
                    Severity = ProblemSeverity.Critical,
                    RequiresCommentOut = matchingNoEquiv.CommentOutEntireClass
                });
                return;
            }

            // Check ManualConversionRequiredTypes
            var matchingManual = TypeReplacementMap.ManualConversionRequiredTypes.Values
                .FirstOrDefault(t => {
                    var expectedFullName = t.GetFullOldTypeName();
                    return fullTypeName.Equals(expectedFullName, StringComparison.Ordinal) ||
                           fullTypeName.EndsWith($".{expectedFullName}", StringComparison.Ordinal);
                });

            if (matchingManual != null) {
                problems.Add(new TypeProblem {
                    TypeName = typeName,
                    FullTypeName = fullTypeName,
                    Reason = $"Base class '{typeName}' has equivalent but requires manual conversion",
                    Description = matchingManual.Description,
                    Severity = ProblemSeverity.High,
                    RequiresCommentOut = matchingManual.CommentOutEntireClass
                });
            }
        }

        /// <summary>
        /// Check type using directives fallback for namespace resolution
        /// </summary>
        private static void CheckTypeUsingDirectives(string typeName, HashSet<string> usingDirectives, List<TypeProblem> problems) {
            var candidateTypes = TypeReplacementMap.NoEquivalentTypes.Values
                .Concat(TypeReplacementMap.ManualConversionRequiredTypes.Values)
                .Where(t => t.OldType == typeName)
                .ToList();

            foreach (var candidateType in candidateTypes) {
                if (!string.IsNullOrEmpty(candidateType.OldNamespace) && 
                    usingDirectives.Contains(candidateType.OldNamespace)) {
                    var isNoEquiv = TypeReplacementMap.NoEquivalentTypes.ContainsKey(typeName);
                    
                    problems.Add(new TypeProblem {
                        TypeName = typeName,
                        FullTypeName = candidateType.GetFullOldTypeName(),
                        Reason = isNoEquiv 
                            ? $"Base class '{typeName}' has no equivalent (inferred from using {candidateType.OldNamespace})"
                            : $"Base class '{typeName}' requires manual conversion (inferred from using {candidateType.OldNamespace})",
                        Description = candidateType.Description,
                        Severity = isNoEquiv ? ProblemSeverity.Critical : ProblemSeverity.High,
                        RequiresCommentOut = candidateType.CommentOutEntireClass
                    });
                    break;
                }
            }
        }

        /// <summary>
        /// Private wrapper for production use with Solution/Project.
        /// Extracts using directives and delegates to unified analysis logic.
        /// </summary>
        private List<TypeProblem> AnalyzeClass(
            ClassDeclarationSyntax classDecl,
            SemanticModel semanticModel,
            string filePath) {
            
            // Extract using directives from the file
            var root = classDecl.SyntaxTree.GetRoot();
            var usingDirectives = root.DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(u => u.Name?.ToString())
                .Where(n => !string.IsNullOrEmpty(n))
                .ToHashSet();
            
            // Use unified analysis logic
            return AnalyzeSingleClass(classDecl, semanticModel, usingDirectives);
        }

        /// <summary>
        /// Find all classes that depend on a specific class
        /// </summary>
        public List<string> FindDependentClasses(Project project, string targetClassName) {
            var dependents = new List<string>();

            foreach (var document in project.Documents) {
                if (!document.FilePath.EndsWith(".cs")) continue;

                var content = File.ReadAllText(document.FilePath);
                
                // Simple text search for class name usage
                if (content.Contains(targetClassName)) {
                    var syntaxTree = document.GetSyntaxTreeAsync().Result;
                    if (syntaxTree == null) continue;

                    var root = syntaxTree.GetRoot();
                    var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                    foreach (var classDecl in classes) {
                        var className = classDecl.Identifier.Text;
                        if (className != targetClassName && !dependents.Contains(className)) {
                            // Check if this class actually uses the target class
                            var classText = classDecl.ToString();
                            if (classText.Contains(targetClassName)) {
                                dependents.Add(className);
                            }
                        }
                    }
                }
            }

            return dependents;
        }

        /// <summary>
        /// Categorize build errors into fixable and unfixable
        /// </summary>
        public (List<FixableError>, List<UnfixableError>) CategorizeErrors(
            string projectPath,
            List<BuildError> errors) {
            var fixableErrors = new List<FixableError>();
            var unfixableErrors = new List<UnfixableError>();

            foreach (var error in errors) {
                if (IsFixableError(error)) {
                    fixableErrors.Add(new FixableError {
                        Code = error.Code,
                        Message = error.Message,
                        FilePath = error.FilePath,
                        Line = error.Line,
                        Column = error.Column,
                        SuggestedFix = GetSuggestedFix(error)
                    });
                }
                else {
                    unfixableErrors.Add(new UnfixableError {
                        Code = error.Code,
                        Message = error.Message,
                        FilePath = error.FilePath,
                        Line = error.Line,
                        Column = error.Column,
                        Reason = GetUnfixableReason(error)
                    });
                }
            }

            return (fixableErrors, unfixableErrors);
        }

        /// <summary>
        /// Check if error is related to NO_EQUIVALENT types
        /// </summary>
        public bool IsNoEquivalentError(BuildError error) {
            // Check error message for NO_EQUIVALENT type names
            foreach (var typeEntry in TypeReplacementMap.NoEquivalentTypes) {
                var typeName = typeEntry.Key;
                if (error.Message.Contains(typeName)) {
                    return true;
                }
            }

            // Check for NO_EQUIVALENT namespace references
            foreach (var nsEntry in TypeReplacementMap.NoEquivalentNamespaces) {
                var ns = nsEntry.Value.OldNamespace;
                if (error.Message.Contains(ns)) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Suggest a fix for the error
        /// </summary>
        public string SuggestFix(BuildError error) {
            var fix = GetSuggestedFix(error);
            if (fix != "Unknown") {
                return fix;
            }

            // Try to find more specific fixes based on message content
            if (error.Message.Contains("does not contain a definition")) {
                return "Member may have been renamed or removed in Blazor version";
            }

            if (error.Message.Contains("obsolete")) {
                return "Replace with recommended alternative shown in warning";
            }

            if (error.Message.Contains("ambiguous")) {
                return "Add explicit namespace or type qualifier";
            }

            return null;
        }

        private bool IsFixableError(BuildError error) {
            // CS0246: Type or namespace not found (might be fixable with using)
            if (error.Code == "CS0246") {
                return error.Message.Contains("using directive");
            }

            // CS0234: Namespace does not exist (might be namespace migration)
            if (error.Code == "CS0234") {
                return true;
            }

            // CS1061: Does not contain a definition (might be type migration)
            if (error.Code == "CS1061") {
                return false; // Usually unfixable - API changes
            }

            // Default: unfixable
            return false;
        }

        private string GetSuggestedFix(BuildError error) {
            if (error.Code == "CS0246") {
                return "Add missing using statement or migrate namespace";
            }

            if (error.Code == "CS0234") {
                return "Migrate namespace from Web to .NET";
            }

            return "Unknown";
        }

        private string GetUnfixableReason(BuildError error) {
            if (error.Code == "CS1061") {
                return "API member not available in .NET";
            }

            if (error.Message.Contains("no Blazor equivalent")) {
                return "Type has no .NET equivalent";
            }

            return "Requires manual review";
        }

        /// <summary>
        /// Analyze XAFML files for problematic types
        /// </summary>
        public List<XafmlProblem> AnalyzeXafmlFiles(Project project) {
            var problems = new List<XafmlProblem>();

            foreach (var document in project.Documents) {
                if (!document.FilePath.EndsWith(".xafml")) continue;

                var content = File.ReadAllText(document.FilePath);

                // Check for NO_EQUIVALENT types in XAFML
                foreach (var typeEntry in TypeReplacementMap.NoEquivalentTypes) {
                    var typeName = typeEntry.Key;
                    var replacement = typeEntry.Value;

                    // XAFML uses full type names
                    var fullTypeName = replacement.GetFullOldTypeName();
                    if (content.Contains(fullTypeName)) {
                        problems.Add(new XafmlProblem {
                            FilePath = document.FilePath,
                            TypeName = typeName,
                            FullTypeName = fullTypeName,
                            Reason = replacement.Description,
                            RequiresCommentOut = true
                        });
                    }
                }
            }

            return problems;
        }
    }

    /// <summary>
    /// Represents a class with problematic code
    /// </summary>
    internal class ProblematicClass {
        public string ClassName { get; set; }
        public string FilePath { get; set; }
        public List<TypeProblem> Problems { get; set; }
        public List<string> DependentClasses { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents a type-related problem
    /// </summary>
    internal class TypeProblem {
        public string TypeName { get; set; }
        public string FullTypeName { get; set; }
        public string Reason { get; set; }
        public string Description { get; set; }
        public ProblemSeverity Severity { get; set; }
        public bool RequiresCommentOut { get; set; }
    }

    internal enum ProblemSeverity {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Represents a build error
    /// </summary>
    internal class BuildError {
        public string Code { get; set; }
        public string Message { get; set; }
        public string FilePath { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public string Severity { get; set; }
    }

    /// <summary>
    /// Represents a fixable build error
    /// </summary>
    internal class FixableError : BuildError {
        public string SuggestedFix { get; set; }
    }

    /// <summary>
    /// Represents an unfixable build error
    /// </summary>
    internal class UnfixableError : BuildError {
        public string Reason { get; set; }
    }

    /// <summary>
    /// Represents a problem in XAFML file
    /// </summary>
    internal class XafmlProblem {
        public string FilePath { get; set; }
        public string TypeName { get; set; }
        public string FullTypeName { get; set; }
        public string Reason { get; set; }
        public bool RequiresCommentOut { get; set; }
    }
}

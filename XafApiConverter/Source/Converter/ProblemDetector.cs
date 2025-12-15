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
                            // REMOVED: ClassSyntax - will be reloaded fresh each time
                        });
                    }
                }
            }

            return problematicClasses;
        }


        private List<TypeProblem> AnalyzeClass(
            ClassDeclarationSyntax classDecl,
            SemanticModel semanticModel,
            string filePath) {
            var problems = new List<TypeProblem>();

            // Check base class (e.g., : Page)
            if (classDecl.BaseList != null) {
                foreach (var baseType in classDecl.BaseList.Types) {
                    var typeInfo = semanticModel.GetTypeInfo(baseType.Type);
                    if (typeInfo.Type != null) {
                        var typeName = typeInfo.Type.Name;
                        var typeNamespace = typeInfo.Type.ContainingNamespace?.ToDisplayString();

                        if (TypeReplacementMap.NoEquivalentTypes.ContainsKey(typeName)) {
                            problems.Add(new TypeProblem {
                                TypeName = typeName,
                                FullTypeName = $"{typeNamespace}.{typeName}",
                                Reason = $"Base class '{typeName}' has no equivalent in XAF .NET",
                                Severity = ProblemSeverity.Critical,
                                RequiresCommentOut = true
                            });
                        }
                        else if (TypeReplacementMap.ManualConversionRequiredTypes.ContainsKey(typeName)) {
                            var replacement = TypeReplacementMap.ManualConversionRequiredTypes[typeName];
                            problems.Add(new TypeProblem {
                                TypeName = typeName,
                                FullTypeName = $"{typeNamespace}.{typeName}",
                                Reason = $"Base class '{typeName}' has equivalent in XAF .NET ({replacement.NewType}) but automatic conversion is not possible. See: {replacement.GetFullNewTypeName()}",
                                Severity = ProblemSeverity.High,
                                RequiresCommentOut = false
                            });
                        }
                    }
                }
            }

            // Check for problematic enum usages
            var memberAccess = classDecl.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
            foreach (var access in memberAccess) {
                var text = access.ToString();
                
                // Check for TemplateType.Horizontal/Vertical
                if (text.Contains("TemplateType.Horizontal") || text.Contains("TemplateType.Vertical")) {
                    problems.Add(new TypeProblem {
                        TypeName = "TemplateType",
                        FullTypeName = "DevExpress.ExpressApp.Web.Templates.TemplateType",
                        Reason = "Uses TemplateType enum which has no Blazor equivalent",
                        Severity = ProblemSeverity.Critical,
                        RequiresCommentOut = true
                    });
                    break; // Only add once
                }
            }

            // Check for NO_EQUIVALENT and MANUAL_CONVERSION_REQUIRED types in code
            var identifiers = classDecl.DescendantNodes().OfType<IdentifierNameSyntax>();
            foreach (var identifier in identifiers) {
                var typeInfo = semanticModel.GetTypeInfo(identifier);
                if (typeInfo.Type != null) {
                    var typeName = typeInfo.Type.Name;
                    var typeNamespace = typeInfo.Type.ContainingNamespace?.ToDisplayString();

                    if (TypeReplacementMap.NoEquivalentTypes.ContainsKey(typeName)) {
                        problems.Add(new TypeProblem {
                            TypeName = typeName,
                            FullTypeName = $"{typeNamespace}.{typeName}",
                            Reason = $"Type '{typeName}' has no equivalent in XAF .NET",
                            Severity = ProblemSeverity.High,
                            RequiresCommentOut = true
                        });
                    }
                    else if (TypeReplacementMap.ManualConversionRequiredTypes.ContainsKey(typeName)) {
                        var replacement = TypeReplacementMap.ManualConversionRequiredTypes[typeName];
                        problems.Add(new TypeProblem {
                            TypeName = typeName,
                            FullTypeName = $"{typeNamespace}.{typeName}",
                            Reason = $"Type '{typeName}' has equivalent in XAF .NET ({replacement.NewType}) but automatic conversion is not possible. See: {replacement.GetFullNewTypeName()}",
                            Severity = ProblemSeverity.Medium,
                            RequiresCommentOut = false
                        });
                    }
                }
            }

            // Deduplicate problems by FullTypeName
            var uniqueProblems = problems
                .GroupBy(p => p.FullTypeName)
                .Select(g => g.First())
                .ToList();

            return uniqueProblems;
        }

        private bool IsNoEquivalentType(string typeName, string typeNamespace) {
            // Check NO_EQUIVALENT types
            if (TypeReplacementMap.NoEquivalentTypes.ContainsKey(typeName)) {
                return true;
            }

            // Check MANUAL_CONVERSION_REQUIRED types
            if (TypeReplacementMap.ManualConversionRequiredTypes.ContainsKey(typeName)) {
                return true;
            }

            // Check NO_EQUIVALENT namespaces
            if (!string.IsNullOrEmpty(typeNamespace)) {
                if (TypeReplacementMap.NoEquivalentNamespaces.Values.Any(
                    ns => typeNamespace.StartsWith(ns.OldNamespace))) {
                    return true;
                }
            }

            return false;
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
        public string Severity { get; set; }  // "error" or "warning"
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

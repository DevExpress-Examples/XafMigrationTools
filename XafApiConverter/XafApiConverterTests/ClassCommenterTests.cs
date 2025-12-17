using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using XafApiConverter.Converter;

namespace XafApiConverterTests.ClassCommenterTests {
    /// <summary>
    /// Integration tests for the complete migration pipeline:
    /// 1. Replace usings (UsingsRewriter)
    /// 2. Replace types (TypeReplaceRewriter)
    /// 3. Comment out problematic classes (ClassCommenter)
    /// </summary>
    public class ClassCommenterTests {
        private readonly string _testFilesPath;

        public ClassCommenterTests() {
            // Get the path to TestFiles folder relative to the test assembly
            var assemblyLocation = Path.GetDirectoryName(typeof(ClassCommenterTests).Assembly.Location);
            _testFilesPath = Path.Combine(assemblyLocation!, "TestFiles", "ClassCommenterTests");
            
            // Ensure test files exist
            if (!Directory.Exists(_testFilesPath)) {
                throw new DirectoryNotFoundException($"Test files directory not found: {_testFilesPath}");
            }
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void TestFullPipeline_SingleClass_ASPxPropertyEditor() {
            // Arrange
            var inputFile = Path.Combine(_testFilesPath, "CustomStringEditor.cs");
            var expectedFile = Path.Combine(_testFilesPath, "CustomStringEditor_commented.cs");

            // Act
            var result = RunFullMigrationPipeline(inputFile);

            // Assert
            var expected = NormalizeWhitespace(File.ReadAllText(expectedFile));
            var actual = NormalizeWhitespace(result);

            Assert.Equal(expected, actual);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void TestFullPipeline_MultipleClasses_AllCommented() {
            // Arrange
            var inputFile = Path.Combine(_testFilesPath, "CustomLayoutTemplates.cs");
            var expectedFile = Path.Combine(_testFilesPath, "CustomLayoutTemplates_commented.cs");

            // Act
            var result = RunFullMigrationPipeline(inputFile);

            // Assert
            var expected = NormalizeWhitespace(File.ReadAllText(expectedFile));
            var actual = NormalizeWhitespace(result);

            Assert.Equal(expected, actual);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void TestFullPipeline_MixedClasses_SelectiveCommented() {
            // Arrange
            var inputFile = Path.Combine(_testFilesPath, "CustomIntegerEditor.cs");
            var expectedFile = Path.Combine(_testFilesPath, "CustomIntegerEditor_commented.cs");

            // Act
            var result = RunFullMigrationPipeline(inputFile);

            // Assert
            var expected = NormalizeWhitespace(File.ReadAllText(expectedFile));
            var actual = NormalizeWhitespace(result);

            Assert.Equal(expected, actual);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void TestFullPipeline_PartialClass_ProtectedBaseClass_WarningOnly() {
            // Arrange - Process both partial class files
            var inputFileMain = Path.Combine(_testFilesPath, "WebModule.cs");
            var inputFileDesigner = Path.Combine(_testFilesPath, "WebModule.Designer.cs");
            var expectedFile = Path.Combine(_testFilesPath, "WebModule_commented.cs");

            // Act - Run pipeline on both files (Designer file contains problematic type)
            var resultMain = RunFullMigrationPipelineForPartialClass(inputFileMain, inputFileDesigner);

            // Assert
            var expected = NormalizeWhitespace(File.ReadAllText(expectedFile));
            var actual = NormalizeWhitespace(resultMain);

            Assert.Equal(expected, actual);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void TestUsingsReplacement_WebToBlazor() {
            // Arrange
            var inputFile = Path.Combine(_testFilesPath, "CustomLayoutTemplates.cs");
            var content = File.ReadAllText(inputFile);

            // Act
            var result = RunFullMigrationPipeline(inputFile);

            // Assert - Old usings should be removed/replaced
            Assert.DoesNotContain("using System.Web.UI.WebControls;", result);
            Assert.DoesNotContain("using DevExpress.ExpressApp.Web.Layout;", result);
            
            // Assert - New usings should be present
            Assert.Contains("using DevExpress.ExpressApp.Blazor", result);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void TestProtectedBaseClass_NotCommented_WithWarning() {
            // Arrange - Process both partial class files
            var inputFileMain = Path.Combine(_testFilesPath, "WebModule.cs");
            var inputFileDesigner = Path.Combine(_testFilesPath, "WebModule.Designer.cs");

            // Act - Run pipeline on both files (Designer file contains MapsAspNetModule)
            var result = RunFullMigrationPipelineForPartialClass(inputFileMain, inputFileDesigner);

            // Assert - Class should NOT be inside "COMMENTED OUT CLASS" block
            Assert.DoesNotContain("// ========== COMMENTED OUT CLASS ==========", result);
            Assert.DoesNotContain("// public sealed partial class FeatureCenterAspNetModule", result);
            
            // Assert - But should have warning comment
            Assert.Contains("// NOTE:", result);
            Assert.Contains("// TODO: It is necessary to test the application's behavior", result);
            
            // Assert - Class declaration should be active
            Assert.Contains("public sealed partial class FeatureCenterAspNetModule : ModuleBase", result);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void TestSafeClass_RemainsUncommented() {
            // Arrange
            var inputFile = Path.Combine(_testFilesPath, "CustomIntegerEditor.cs");

            // Act
            var result = RunFullMigrationPipeline(inputFile);
            
            // Assert - WelcomeObject should remain uncommented
            Assert.Contains("public class WelcomeObject {", result);
            Assert.DoesNotContain("// public class WelcomeObject", result);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void TestNamespaceAmbiguity_ShouldNotCommentOutWrongType() {
            // Arrange
            // SchedulerNotifications inherits from Event (DevExpress.Persistent.BaseImpl.Event from XPO)
            // But removed-api.txt contains DevExpress.Persistent.BaseImpl.EF.Event (from EF Core)
            // These are DIFFERENT types in different namespaces - class should NOT be commented
            var inputFile = Path.Combine(_testFilesPath, "SchedulerNotificationsSimple.cs");

            // Act
            var result = RunFullMigrationPipeline(inputFile);
            
            // Assert - SchedulerNotifications should remain ACTIVE (not commented out)
            // It inherits from Event which could be XPO or EF, but using directive shows DevExpress.Persistent.BaseImpl (XPO)
            // The removed type is DevExpress.Persistent.BaseImpl.EF.Event (different namespace!)
            Assert.Contains("public class SchedulerNotifications : Event {", result);
            Assert.DoesNotContain("// public class SchedulerNotifications", result);
            Assert.DoesNotContain("// NOTE: Class commented out", result);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void TestDescriptionIncluded_InComments() {
            // Arrange
            var inputFile = Path.Combine(_testFilesPath, "CustomStringEditor.cs");

            // Act
            var result = RunFullMigrationPipeline(inputFile);

            // Assert - Description should be present
            Assert.Contains("ASPxPropertyEditor has Blazor equivalent (BlazorPropertyEditorBase) but automatic conversion is not possible. Manual refactoring required.", result);
        }

        [Theory]
        [InlineData("CustomStringEditor.cs", "CustomStringEditor")]
        [InlineData("CustomLayoutTemplates.cs", "CustomLayoutItemTemplate")]
        [Trait("Category", "Integration")]
        public void TestInputFileExists_BeforeProcessing(string fileName, string expectedClassName) {
            // Arrange
            var filePath = Path.Combine(_testFilesPath, fileName);

            // Assert
            Assert.True(File.Exists(filePath), $"Test file should exist: {filePath}");
            
            var content = File.ReadAllText(filePath);
            Assert.Contains(expectedClassName, content);
        }

        #region Helper Methods - Full Migration Pipeline

        /// <summary>
        /// Run the complete migration pipeline on a file:
        /// 1. Replace usings (remove no-equivalent, replace Web->Blazor)
        /// 2. Replace types (Web->Blazor types)
        /// 3. Detect problematic classes
        /// 4. Comment out problematic classes
        /// </summary>
        private string RunFullMigrationPipeline(string filePath) {
            if (!File.Exists(filePath)) {
                throw new FileNotFoundException($"Test file not found: {filePath}");
            }

            var content = File.ReadAllText(filePath);
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, content);

            try {
                // STEP 1: Replace usings
                content = ReplaceUsings(tempFile);
                File.WriteAllText(tempFile, content);

                // STEP 2: Replace types
                content = ReplaceTypes(tempFile);
                File.WriteAllText(tempFile, content);

                // STEP 3: Analyze and comment out problematic classes
                content = CommentOutProblematicClasses(tempFile);

                return content;
            }
            finally {
                if (File.Exists(tempFile)) {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Run the complete migration pipeline for partial classes (requires both files)
        /// The problematic type might be in either part of the partial class
        /// </summary>
        private string RunFullMigrationPipelineForPartialClass(string mainFilePath, string designerFilePath) {
            if (!File.Exists(mainFilePath)) {
                throw new FileNotFoundException($"Test file not found: {mainFilePath}");
            }
            if (!File.Exists(designerFilePath)) {
                throw new FileNotFoundException($"Test file not found: {designerFilePath}");
            }

            var tempMainFile = Path.GetTempFileName();
            var tempDesignerFile = Path.GetTempFileName();

            try {
                // Copy both files to temp location
                File.Copy(mainFilePath, tempMainFile, true);
                File.Copy(designerFilePath, tempDesignerFile, true);

                // STEP 1: Replace usings in both files
                var mainContent = ReplaceUsings(tempMainFile);
                File.WriteAllText(tempMainFile, mainContent);

                var designerContent = ReplaceUsings(tempDesignerFile);
                File.WriteAllText(tempDesignerFile, designerContent);

                // STEP 2: Replace types in both files
                mainContent = ReplaceTypes(tempMainFile);
                File.WriteAllText(tempMainFile, mainContent);

                designerContent = ReplaceTypes(tempDesignerFile);
                File.WriteAllText(tempDesignerFile, designerContent);

                // STEP 3: Analyze BOTH files for problematic classes
                // We need to analyze the Designer file too, as it contains MapsAspNetModule
                mainContent = CommentOutProblematicClassesForPartialClass(
                    tempMainFile, 
                    tempDesignerFile);

                return mainContent;
            }
            finally {
                if (File.Exists(tempMainFile)) File.Delete(tempMainFile);
                if (File.Exists(tempDesignerFile)) File.Delete(tempDesignerFile);
            }
        }

        /// <summary>
        /// Step 1: Replace usings using UsingsRewriter
        /// </summary>
        private string ReplaceUsings(string filePath) {
            var content = File.ReadAllText(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(content);
            var root = syntaxTree.GetRoot();

            // Apply UsingsRewriter logic
            var rewriter = new TestUsingsRewriter();
            var newRoot = rewriter.Visit(root);

            return newRoot.ToFullString();
        }

        /// <summary>
        /// Step 2: Replace types using TypeReplaceRewriter logic
        /// </summary>
        private string ReplaceTypes(string filePath) {
            var content = File.ReadAllText(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(content);
            var root = syntaxTree.GetRoot();

            // Apply TypeReplaceRewriter logic - replace type names
            var rewriter = new TestTypeReplaceRewriter();
            var newRoot = rewriter.Visit(root);

            return newRoot.ToFullString();
        }

        /// <summary>
        /// Step 3: Detect and comment out problematic classes
        /// </summary>
        private string CommentOutProblematicClasses(string filePath) {
            var content = File.ReadAllText(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(content);
            var root = syntaxTree.GetRoot();

            // Create compilation to get semantic model
            var compilation = CreateCompilation(syntaxTree);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            // Analyze classes for problems
            var problematicClasses = new List<ProblematicClass>();
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var classDecl in classes) {
                var className = classDecl.Identifier.Text;
                var problems = AnalyzeClassForProblems(classDecl, className, semanticModel);
                
                if (problems.Any(p => p.RequiresCommentOut)) {
                    problematicClasses.Add(new ProblematicClass {
                        ClassName = className,
                        FilePath = filePath,
                        Problems = problems
                    });
                }
            }

            if (!problematicClasses.Any()) {
                return content;
            }

            // Create report and run ClassCommenter
            var report = new MigrationReport {
                ProblematicClasses = problematicClasses
            };

            var commenter = new ClassCommenter(report);
            commenter.CommentOutProblematicClasses();

            return File.ReadAllText(filePath);
        }

        /// <summary>
        /// Step 3: Detect and comment out problematic classes for partial classes
        /// Analyzes BOTH files to find all problematic types
        /// </summary>
        private string CommentOutProblematicClassesForPartialClass(string mainFilePath, string designerFilePath) {
            var mainContent = File.ReadAllText(mainFilePath);
            var designerContent = File.ReadAllText(designerFilePath);

            var mainTree = CSharpSyntaxTree.ParseText(mainContent);
            var designerTree = CSharpSyntaxTree.ParseText(designerContent);

            var mainRoot = mainTree.GetRoot();
            var designerRoot = designerTree.GetRoot();

            // Create compilation with both syntax trees to get semantic models
            var compilation = CreateCompilation(mainTree, designerTree);
            var mainSemanticModel = compilation.GetSemanticModel(mainTree);
            var designerSemanticModel = compilation.GetSemanticModel(designerTree);

            // Find all classes in both files
            var allClasses = new Dictionary<string, List<(ClassDeclarationSyntax, SemanticModel)>>();

            // Collect classes from main file
            foreach (var classDecl in mainRoot.DescendantNodes().OfType<ClassDeclarationSyntax>()) {
                var className = classDecl.Identifier.Text;
                if (!allClasses.ContainsKey(className)) {
                    allClasses[className] = new List<(ClassDeclarationSyntax, SemanticModel)>();
                }
                allClasses[className].Add((classDecl, mainSemanticModel));
            }

            // Collect classes from designer file
            foreach (var classDecl in designerRoot.DescendantNodes().OfType<ClassDeclarationSyntax>()) {
                var className = classDecl.Identifier.Text;
                if (!allClasses.ContainsKey(className)) {
                    allClasses[className] = new List<(ClassDeclarationSyntax, SemanticModel)>();
                }
                allClasses[className].Add((classDecl, designerSemanticModel));
            }

            // Analyze ALL parts of each partial class
            var problematicClasses = new List<ProblematicClass>();

            foreach (var kvp in allClasses) {
                var className = kvp.Key;
                var classParts = kvp.Value;

                // Collect problems from ALL parts
                var allProblems = new List<TypeProblem>();
                foreach (var (classDecl, semanticModel) in classParts) {
                    var problems = AnalyzeClassForProblems(classDecl, className, semanticModel);
                    allProblems.AddRange(problems);
                }

                // Deduplicate problems
                var uniqueProblems = allProblems
                    .GroupBy(p => p.TypeName)
                    .Select(g => g.First())
                    .ToList();

                if (uniqueProblems.Any(p => p.RequiresCommentOut)) {
                    problematicClasses.Add(new ProblematicClass {
                        ClassName = className,
                        FilePath = mainFilePath,  // Main file will get the comment
                        Problems = uniqueProblems
                    });
                }
            }

            if (!problematicClasses.Any()) {
                return mainContent;
            }

            // Create report and run ClassCommenter
            var report = new MigrationReport {
                ProblematicClasses = problematicClasses
            };

            var commenter = new ClassCommenter(report);
            commenter.CommentOutProblematicClasses();

            return File.ReadAllText(mainFilePath);
        }

        /// <summary>
        /// Create a minimal compilation for semantic analysis
        /// </summary>
        private CSharpCompilation CreateCompilation(params SyntaxTree[] syntaxTrees) {
            var references = new[] {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            };

            return CSharpCompilation.Create(
                "TestAssembly",
                syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        /// <summary>
        /// Analyze class for problems based on base types and used types
        /// Uses semantic model to resolve FULL type names with namespaces
        /// Falls back to using directives when semantic model cannot resolve types
        /// </summary>
        private List<TypeProblem> AnalyzeClassForProblems(
            ClassDeclarationSyntax classDecl, 
            string className,
            SemanticModel semanticModel) {
            var problems = new List<TypeProblem>();

            // Get using directives for fallback namespace resolution
            var root = classDecl.SyntaxTree.GetRoot();
            var usingDirectives = root.DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(u => u.Name?.ToString())
                .Where(n => !string.IsNullOrEmpty(n))
                .ToHashSet();

            // Check base classes
            if (classDecl.BaseList != null) {
                foreach (var baseType in classDecl.BaseList.Types) {
                    var typeName = baseType.Type.ToString();
                    
                    // Try to get the full type name using semantic model
                    var typeSymbol = semanticModel.GetSymbolInfo(baseType.Type).Symbol as INamedTypeSymbol;
                    string fullTypeName = null;
                    
                    if (typeSymbol != null && !typeSymbol.ToDisplayString().StartsWith("?")) {
                        // Get full type name including namespace
                        fullTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                            .Replace("global::", ""); // Remove global:: prefix
                    }

                    // If we have semantic info, use it for precise matching
                    if (fullTypeName != null) {
                        // Check against NoEquivalentTypes using FULL type name comparison
                        var matchingNoEquiv = TypeReplacementMap.NoEquivalentTypes.Values
                            .FirstOrDefault(t => {
                                var expectedFullName = t.GetFullOldTypeName();
                                // Match if full names are equal
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
                            continue; // Move to next base type
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
                                Reason = $"Base class '{typeName}' has equivalent in XAF .NET ({matchingManual.NewType}) but automatic conversion is not possible. See: {matchingManual.GetFullNewTypeName()}",
                                Description = matchingManual.Description,
                                Severity = ProblemSeverity.High,
                                RequiresCommentOut = matchingManual.CommentOutEntireClass
                            });
                        }
                    }
                    else {
                        // Semantic model couldn't resolve type - use USING DIRECTIVES for namespace inference
                        // This is more reliable than simple name matching
                        
                        // Find all problematic types with this simple name
                        var candidateTypes = TypeReplacementMap.NoEquivalentTypes.Values
                            .Concat(TypeReplacementMap.ManualConversionRequiredTypes.Values)
                            .Where(t => t.OldType == typeName)
                            .ToList();

                        foreach (var candidateType in candidateTypes) {
                            // Check if the namespace of this candidate type is in the using directives
                            if (!string.IsNullOrEmpty(candidateType.OldNamespace) && 
                                usingDirectives.Contains(candidateType.OldNamespace)) {
                                // Found a match based on using directives!
                                var isNoEquiv = TypeReplacementMap.NoEquivalentTypes.ContainsKey(typeName);
                                
                                problems.Add(new TypeProblem {
                                    TypeName = typeName,
                                    FullTypeName = candidateType.GetFullOldTypeName(),
                                    Reason = isNoEquiv 
                                        ? $"Base class '{typeName}' has no equivalent in XAF .NET (inferred from using {candidateType.OldNamespace})"
                                        : $"Base class '{typeName}' has equivalent in XAF .NET ({candidateType.NewType}) but automatic conversion is not possible (inferred from using {candidateType.OldNamespace})",
                                    Description = candidateType.Description,
                                    Severity = isNoEquiv ? ProblemSeverity.Critical : ProblemSeverity.High,
                                    RequiresCommentOut = candidateType.CommentOutEntireClass
                                });
                                break; // Found the match, don't check other candidates
                            }
                        }
                        
                        // If no match found via using directives, the type is likely safe
                        // (e.g., Event from DevExpress.Persistent.BaseImpl - XPO, which is fine)
                    }
                }
            }

            // Check for problematic types used in code (including typeof() expressions)
            var identifiers = classDecl.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Select(i => i.Identifier.Text)
                .Distinct();

            foreach (var identifier in identifiers) {
                // Get the identifier node to check semantic info
                var identifierNode = classDecl.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .FirstOrDefault(i => i.Identifier.Text == identifier);

                if (identifierNode != null) {
                    var symbol = semanticModel.GetSymbolInfo(identifierNode).Symbol as INamedTypeSymbol;
                    if (symbol != null && !symbol.ToDisplayString().StartsWith("?")) {
                        var fullTypeName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                            .Replace("global::", "");

                        // Check with full type name
                        var matchingNoEquiv = TypeReplacementMap.NoEquivalentTypes.Values
                            .FirstOrDefault(t => fullTypeName.Equals(t.GetFullOldTypeName(), StringComparison.Ordinal));

                        if (matchingNoEquiv != null) {
                            problems.Add(new TypeProblem {
                                TypeName = identifier,
                                FullTypeName = fullTypeName,
                                Reason = $"Type '{identifier}' has no equivalent in XAF .NET",
                                Description = matchingNoEquiv.Description,
                                Severity = ProblemSeverity.High,
                                RequiresCommentOut = matchingNoEquiv.CommentOutEntireClass
                            });
                        }
                    }
                    else {
                        // Fallback to using directives for identifiers too
                        if (TypeReplacementMap.NoEquivalentTypes.TryGetValue(identifier, out var noEquivType)) {
                            if (!string.IsNullOrEmpty(noEquivType.OldNamespace) && 
                                usingDirectives.Contains(noEquivType.OldNamespace)) {
                                problems.Add(new TypeProblem {
                                    TypeName = identifier,
                                    FullTypeName = noEquivType.GetFullOldTypeName(),
                                    Reason = $"Type '{identifier}' has no equivalent in XAF .NET (inferred from using {noEquivType.OldNamespace})",
                                    Description = noEquivType.Description,
                                    Severity = ProblemSeverity.High,
                                    RequiresCommentOut = noEquivType.CommentOutEntireClass
                                });
                            }
                        }
                    }
                }
            }

            return problems.GroupBy(p => p.TypeName).Select(g => g.First()).ToList();
        }

        /// <summary>
        /// Normalize whitespace for comparison
        /// </summary>
        private string NormalizeWhitespace(string text) {
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = text.Split('\n').Select(line => line.TrimEnd()).ToArray();
            return string.Join("\n", lines).Trim();
        }

        #endregion

        #region Test Rewriters - Simplified versions for testing

        /// <summary>
        /// Simplified UsingsRewriter for tests
        /// Removes no-equivalent usings and replaces Web->Blazor usings
        /// </summary>
        private class TestUsingsRewriter : CSharpSyntaxRewriter {
            public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node) {
                var usingName = node.Name?.ToString();
                if (string.IsNullOrEmpty(usingName)) {
                    return base.VisitUsingDirective(node);
                }

                // Remove no-equivalent namespaces
                if (TypeReplacementMap.NoEquivalentNamespaces.ContainsKey(usingName)) {
                    return null; // Remove this using
                }

                // Replace Web->Blazor namespaces
                if (TypeReplacementMap.NamespaceReplacements.TryGetValue(usingName, out var replacement)) {
                    if (replacement.HasEquivalent) {
                        var newName = SyntaxFactory.ParseName(replacement.NewNamespace);
                        return node.WithName(newName);
                    }
                }

                return base.VisitUsingDirective(node);
            }
        }

        /// <summary>
        /// Simplified TypeReplaceRewriter for tests
        /// Replaces Web types with Blazor equivalents
        /// </summary>
        private class TestTypeReplaceRewriter : CSharpSyntaxRewriter {
            public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node) {
                var typeName = node.Identifier.Text;

                // Replace types that have equivalents
                if (TypeReplacementMap.TypeReplacements.TryGetValue(typeName, out var replacement)) {
                    if (replacement.HasEquivalent) {
                        return SyntaxFactory.IdentifierName(replacement.NewType)
                            .WithTriviaFrom(node);
                    }
                }

                return base.VisitIdentifierName(node);
            }
        }

        #endregion
    }
}

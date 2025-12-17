using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using XafApiConverter;
using XafApiConverter.Converter;

namespace XafApiConverterTests.ClassCommenterTests {
    /// <summary>
    /// Integration tests for the complete migration pipeline.
    /// Uses REAL production code - no duplicated logic!
    /// </summary>
    public class ClassCommenterTests {
        private readonly string _testFilesPath;

        public ClassCommenterTests() {
            var assemblyLocation = Path.GetDirectoryName(typeof(ClassCommenterTests).Assembly.Location);
            _testFilesPath = Path.Combine(assemblyLocation!, "TestFiles", "ClassCommenterTests");
            
            if (!Directory.Exists(_testFilesPath)) {
                throw new DirectoryNotFoundException($"Test files directory not found: {_testFilesPath}");
            }
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void TestFullPipeline_SingleClass_ASPxPropertyEditor() {
            var inputFile = Path.Combine(_testFilesPath, "CustomStringEditor.cs");
            var expectedFile = Path.Combine(_testFilesPath, "CustomStringEditor_commented.cs");

            var result = RunFullMigrationPipeline(inputFile);
            var expected = NormalizeWhitespace(File.ReadAllText(expectedFile));
            var actual = NormalizeWhitespace(result);

            Assert.Equal(expected, actual);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void TestFullPipeline_MultipleClasses_AllCommented() {
            var inputFile = Path.Combine(_testFilesPath, "CustomLayoutTemplates.cs");
            var expectedFile = Path.Combine(_testFilesPath, "CustomLayoutTemplates_commented.cs");

            var result = RunFullMigrationPipeline(inputFile);
            var expected = NormalizeWhitespace(File.ReadAllText(expectedFile));
            var actual = NormalizeWhitespace(result);

            Assert.Equal(expected, actual);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void TestFullPipeline_MixedClasses_SelectiveCommented() {
            var inputFile = Path.Combine(_testFilesPath, "CustomIntegerEditor.cs");
            var expectedFile = Path.Combine(_testFilesPath, "CustomIntegerEditor_commented.cs");

            var result = RunFullMigrationPipeline(inputFile);
            var expected = NormalizeWhitespace(File.ReadAllText(expectedFile));
            var actual = NormalizeWhitespace(result);

            Assert.Equal(expected, actual);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void TestFullPipeline_PartialClass_ProtectedBaseClass_WarningOnly() {
            var inputFileMain = Path.Combine(_testFilesPath, "WebModule.cs");
            var inputFileDesigner = Path.Combine(_testFilesPath, "WebModule.Designer.cs");
            var expectedFile = Path.Combine(_testFilesPath, "WebModule_commented.cs");

            var resultMain = RunFullMigrationPipelineForPartialClass(inputFileMain, inputFileDesigner);
            var expected = NormalizeWhitespace(File.ReadAllText(expectedFile));
            var actual = NormalizeWhitespace(resultMain);

            Assert.Equal(expected, actual);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void TestUsingsReplacement_WebToBlazor() {
            var inputFile = Path.Combine(_testFilesPath, "CustomLayoutTemplates.cs");

            var result = RunFullMigrationPipeline(inputFile);

            Assert.DoesNotContain("using System.Web.UI.WebControls;", result);
            Assert.DoesNotContain("using DevExpress.ExpressApp.Web.Layout;", result);
            Assert.Contains("using DevExpress.ExpressApp.Blazor", result);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void TestProtectedBaseClass_NotCommented_WithWarning() {
            var inputFileMain = Path.Combine(_testFilesPath, "WebModule.cs");
            var inputFileDesigner = Path.Combine(_testFilesPath, "WebModule.Designer.cs");

            var result = RunFullMigrationPipelineForPartialClass(inputFileMain, inputFileDesigner);

            Assert.DoesNotContain("// ========== COMMENTED OUT CLASS ==========", result);
            Assert.DoesNotContain("// public sealed partial class FeatureCenterAspNetModule", result);
            Assert.Contains("// NOTE:", result);
            Assert.Contains("// TODO: It is necessary to test the application's behavior", result);
            Assert.Contains("public sealed partial class FeatureCenterAspNetModule : ModuleBase", result);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void TestNamespaceAmbiguity_ShouldNotCommentOutWrongType() {
            // CustomLogger inherits from Logger (DevExpress.ExpressApp.MiddleTier.Logger)
            // But removed-api.txt contains DevExpress.ExpressApp.ScriptRecorder.Logger
            // These are DIFFERENT types - class should NOT be commented
            var inputFile = Path.Combine(_testFilesPath, "CustomLogger.cs");

            var result = RunFullMigrationPipeline(inputFile);
            var expected = NormalizeWhitespace(File.ReadAllText(inputFile));
            var actual = NormalizeWhitespace(result);

            Assert.Equal(expected, actual);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void TestNamespaceAmbiguity_GeneralBOClasses() {
            // Input file uses protected base classes - should not be commented out, only warnings added
            var inputFile = Path.Combine(_testFilesPath, "SchedulerNotifications.cs");
            var expectedFile = Path.Combine(_testFilesPath, "SchedulerNotifications_commented.cs");

            var result = RunFullMigrationPipeline(inputFile);
            var expected = NormalizeWhitespace(File.ReadAllText(expectedFile));
            var actual = NormalizeWhitespace(result);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("CustomStringEditor.cs", "CustomStringEditor")]
        [InlineData("CustomLayoutTemplates.cs", "CustomLayoutItemTemplate")]
        [Trait("Category", "Integration")]
        public void TestInputFileExists_BeforeProcessing(string fileName, string expectedClassName) {
            var filePath = Path.Combine(_testFilesPath, fileName);

            Assert.True(File.Exists(filePath), $"Test file should exist: {filePath}");
            
            var content = File.ReadAllText(filePath);
            Assert.Contains(expectedClassName, content);
        }

        #region Helper Methods - Uses REAL Production Code

        /// <summary>
        /// Run complete migration pipeline using REAL production code
        /// </summary>
        private string RunFullMigrationPipeline(string filePath) {
            if (!File.Exists(filePath)) {
                throw new FileNotFoundException($"Test file not found: {filePath}");
            }

            var tempFile = Path.GetTempFileName();
            File.Copy(filePath, tempFile, true);

            try {
                // STEP 1: Detect and comment out problematic classes using REAL ProblemDetector
                DetectAndCommentProblematicClasses(tempFile);

                // STEP 2 & 3: Apply replacements using REAL production logic
                ApplyReplacements(tempFile);

                return File.ReadAllText(tempFile);
            }
            finally {
                if (File.Exists(tempFile)) {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Run pipeline for partial classes
        /// </summary>
        private string RunFullMigrationPipelineForPartialClass(string mainFilePath, string designerFilePath) {
            if (!File.Exists(mainFilePath) || !File.Exists(designerFilePath)) {
                throw new FileNotFoundException("Test files not found");
            }

            var tempMainFile = Path.GetTempFileName();
            var tempDesignerFile = Path.GetTempFileName();

            try {
                File.Copy(mainFilePath, tempMainFile, true);
                File.Copy(designerFilePath, tempDesignerFile, true);

                // STEP 1: Detect and comment (analyzes BOTH files)
                DetectAndCommentProblematicClassesForPartialClass(tempMainFile, tempDesignerFile);

                // STEP 2 & 3: Apply replacements
                ApplyReplacements(tempMainFile);
                ApplyReplacements(tempDesignerFile);

                return File.ReadAllText(tempMainFile);
            }
            finally {
                if (File.Exists(tempMainFile)) File.Delete(tempMainFile);
                if (File.Exists(tempDesignerFile)) File.Delete(tempDesignerFile);
            }
        }

        /// <summary>
        /// Detect and comment problematic classes using REAL ProblemDetector logic
        /// </summary>
        private void DetectAndCommentProblematicClasses(string filePath) {
            var content = File.ReadAllText(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(content);
            var root = syntaxTree.GetRoot();

            // Use minimal compilation (no DevExpress refs in tests)
            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                new[] {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            // Get using directives for namespace resolution
            var usingDirectives = root.DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(u => u.Name?.ToString())
                .Where(n => !string.IsNullOrEmpty(n))
                .ToHashSet();

            // ✅ Use REAL ProblemDetector static method!
            var problematicClasses = ProblemDetector.AnalyzeClassesInSyntaxTree(
                filePath,
                root,
                semanticModel,
                usingDirectives);

            if (problematicClasses.Any()) {
                var report = new MigrationReport {
                    ProblematicClasses = problematicClasses
                };

                // Use REAL ClassCommenter
                var commenter = new ClassCommenter(report);
                commenter.CommentOutProblematicClasses();
            }
        }

        /// <summary>
        /// Detect for partial classes (analyzes both parts)
        /// </summary>
        private void DetectAndCommentProblematicClassesForPartialClass(string mainFilePath, string designerFilePath) {
            var mainContent = File.ReadAllText(mainFilePath);
            var designerContent = File.ReadAllText(designerFilePath);

            var mainTree = CSharpSyntaxTree.ParseText(mainContent);
            var designerTree = CSharpSyntaxTree.ParseText(designerContent);

            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { mainTree, designerTree },
                new[] {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var mainSemanticModel = compilation.GetSemanticModel(mainTree);
            var designerSemanticModel = compilation.GetSemanticModel(designerTree);

            // Get using directives for both files
            var mainUsingDirectives = mainTree.GetRoot().DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(u => u.Name?.ToString())
                .Where(n => !string.IsNullOrEmpty(n))
                .ToHashSet();

            var designerUsingDirectives = designerTree.GetRoot().DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(u => u.Name?.ToString())
                .Where(n => !string.IsNullOrEmpty(n))
                .ToHashSet();

            // ✅ Use REAL ProblemDetector static method for both files!
            var mainProblems = ProblemDetector.AnalyzeClassesInSyntaxTree(
                mainFilePath,
                mainTree.GetRoot(),
                mainSemanticModel,
                mainUsingDirectives);

            var designerProblems = ProblemDetector.AnalyzeClassesInSyntaxTree(
                designerFilePath,
                designerTree.GetRoot(),
                designerSemanticModel,
                designerUsingDirectives);

            // Merge problems for partial classes
            var allProblems = mainProblems.Concat(designerProblems)
                .GroupBy(pc => pc.ClassName)
                .Select(g => new ProblematicClass {
                    ClassName = g.Key,
                    FilePath = mainFilePath,
                    Problems = g.SelectMany(pc => pc.Problems)
                        .GroupBy(p => p.TypeName)
                        .Select(pg => pg.First())
                        .ToList()
                })
                .Where(pc => pc.Problems.Any(p => p.RequiresCommentOut))
                .ToList();

            if (allProblems.Any()) {
                var report = new MigrationReport {
                    ProblematicClasses = allProblems
                };

                var commenter = new ClassCommenter(report);
                commenter.CommentOutProblematicClasses();
            }
        }

        /// <summary>
        /// Apply replacements using REAL production logic
        /// </summary>
        private void ApplyReplacements(string filePath) {
            var content = File.ReadAllText(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(content);
            var root = syntaxTree.GetRoot();
            var originalRoot = root;

            // ✅ Use REAL TypeMigrationTool static methods!
            root = TypeMigrationTool.ProcessUsingsInRoot(root);
            root = TypeMigrationTool.ProcessTypesInRoot(root);

            if (root != originalRoot) {
                File.WriteAllText(filePath, root.ToFullString());
            }
        }

        private string NormalizeWhitespace(string text) {
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = text.Split('\n').Select(line => line.TrimEnd()).ToArray();
            return string.Join("\n", lines).Trim();
        }

        #endregion
    }
}

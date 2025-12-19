using Microsoft.CodeAnalysis;
using System.Text.RegularExpressions;

namespace XafApiConverter.Converter.CodeAnalysis {
    internal class BuildErrorAnalysis {

        /// <summary>
        /// Phase 4: Build project and categorize errors
        /// </summary>
        public static void BuildAndAnalyzeErrors(string solutionPath, MigrationReport report, Solution solution) {
            Console.WriteLine("  Building solution...");

            try {
                var buildResult = BuildSolution(solutionPath);

                if(buildResult.Success) {
                    report.BuildSuccessful = true;
                    Console.WriteLine("  Build succeeded!");
                } else {
                    report.BuildSuccessful = false;
                    Console.WriteLine($"  Build failed with {buildResult.Errors.Count} error(s)");

                    // Categorize errors
                    BuildErrorAnalysis.CategorizeErrors(buildResult.Errors, report, solution);
                }
            } catch(Exception ex) {
                Console.WriteLine($"  Build analysis failed: {ex.Message}");
                report.BuildSuccessful = false;
            }
        }

        /// <summary>
        /// Build solution using dotnet CLI
        /// </summary>
        private static BuildResult BuildSolution(string solutionPath) {
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

            using(var process = System.Diagnostics.Process.Start(processInfo)) {
                var output = process.StandardOutput.ReadToEnd();
                var errors = process.StandardError.ReadToEnd();

                process.WaitForExit();

                result.Success = process.ExitCode == 0;
                result.ExitCode = process.ExitCode;

                // Parse errors from output
                if(!result.Success) {
                    result.Errors = ParseBuildErrors(output + errors);
                }
            }

            return result;
        }

        /// <summary>
        /// Parse build errors from dotnet build output
        /// </summary>
        private static List<BuildError> ParseBuildErrors(string buildOutput) {
            var errors = new List<BuildError>();

            // Pattern: FilePath(Line,Col): error CS0000: Message
            var errorPattern = @"(.+?)\((\d+),(\d+)\):\s+(error|warning)\s+(\w+):\s+(.+)";
            var matches = Regex.Matches(buildOutput, errorPattern);

            foreach(Match match in matches) {
                if(match.Groups.Count >= 7) {
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
        /// Check if error is related to NO_EQUIVALENT types
        /// </summary>
        public static bool IsNoEquivalentError(BuildError error) {
            // Check error message for NO_EQUIVALENT type names
            foreach(var typeEntry in TypeReplacementMap.NoEquivalentTypes) {
                var typeName = typeEntry.Key;
                if(error.Message.Contains(typeName)) {
                    return true;
                }
            }

            // Check for NO_EQUIVALENT namespace references
            foreach(var nsEntry in TypeReplacementMap.NoEquivalentNamespaces) {
                var ns = nsEntry.Value.OldNamespace;
                if(error.Message.Contains(ns)) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Categorize errors into fixable and unfixable
        /// </summary>
        public static void CategorizeErrors(List<BuildError> errors, MigrationReport report, Solution solution) {
            var detector = new ProblemDetector(solution);

            foreach(var error in errors.Where(e => e.Severity == "error")) {
                // Check if error is related to NO_EQUIVALENT types
                var isNoEquivalent = IsNoEquivalentError(error);

                if(isNoEquivalent) {
                    // Unfixable - requires commenting out
                    report.UnfixableErrors.Add(new UnfixableError {
                        Code = error.Code,
                        Message = error.Message,
                        FilePath = error.FilePath,
                        Line = error.Line,
                        Column = error.Column,
                        Reason = "Type has no .NET equivalent - requires commenting out or manual replacement"
                    });
                } else {
                    // Potentially fixable
                    var suggestedFix = SuggestFix(error);

                    if(!string.IsNullOrEmpty(suggestedFix)) {
                        report.FixableErrors.Add(new FixableError {
                            Code = error.Code,
                            Message = error.Message,
                            FilePath = error.FilePath,
                            Line = error.Line,
                            Column = error.Column,
                            SuggestedFix = suggestedFix
                        });
                    } else {
                        report.UnfixableErrors.Add(new UnfixableError {
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
        /// Categorize build errors into fixable and unfixable
        /// </summary>
        public static (List<FixableError>, List<UnfixableError>) CategorizeErrors(
            string projectPath,
            List<BuildError> errors) {
            var fixableErrors = new List<FixableError>();
            var unfixableErrors = new List<UnfixableError>();

            foreach(var error in errors) {
                if(IsFixableError(error)) {
                    fixableErrors.Add(new FixableError {
                        Code = error.Code,
                        Message = error.Message,
                        FilePath = error.FilePath,
                        Line = error.Line,
                        Column = error.Column,
                        SuggestedFix = GetSuggestedFix(error)
                    });
                } else {
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
        /// Suggest a fix for the error
        /// </summary>
        public static string SuggestFix(BuildError error) {
            var fix = GetSuggestedFix(error);
            if(fix != "Unknown") {
                return fix;
            }

            // Try to find more specific fixes based on message content
            if(error.Message.Contains("does not contain a definition")) {
                return "Member may have been renamed or removed in Blazor version";
            }

            if(error.Message.Contains("obsolete")) {
                return "Replace with recommended alternative shown in warning";
            }

            if(error.Message.Contains("ambiguous")) {
                return "Add explicit namespace or type qualifier";
            }

            return null;
        }

        private static bool IsFixableError(BuildError error) {
            // CS0246: Type or namespace not found (might be fixable with using)
            if(error.Code == "CS0246") {
                return error.Message.Contains("using directive");
            }

            // CS0234: Namespace does not exist (might be namespace migration)
            if(error.Code == "CS0234") {
                return true;
            }

            // CS1061: Does not contain a definition (might be type migration)
            if(error.Code == "CS1061") {
                return false; // Usually unfixable - API changes
            }

            // Default: unfixable
            return false;
        }

        private static string GetSuggestedFix(BuildError error) {
            if(error.Code == "CS0246") {
                return "Add missing using statement or migrate namespace";
            }

            if(error.Code == "CS0234") {
                return "Migrate namespace from Web to .NET";
            }

            return "Unknown";
        }

        private static string GetUnfixableReason(BuildError error) {
            if(error.Code == "CS1061") {
                return "API member not available in .NET";
            }

            if(error.Message.Contains("no Blazor equivalent")) {
                return "Type has no .NET equivalent";
            }

            return "Requires manual review";
        }
    }
}

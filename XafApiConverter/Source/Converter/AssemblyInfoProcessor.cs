using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace XafApiConverter.Converter {
    /// <summary>
    /// Processes AssemblyInfo.cs to comment out Web-specific attributes
    /// </summary>
    internal class AssemblyInfoProcessor {
        private static readonly string[] WebSpecificAttributes = new[] {
            "WebResourceAssembly",
            "WebResource"
        };

        /// <summary>
        /// Process AssemblyInfo.cs file and comment out Web-specific attributes
        /// </summary>
        public static bool ProcessAssemblyInfo(string projectDir) {
            var assemblyInfoPaths = new[] {
                Path.Combine(projectDir, "Properties", "AssemblyInfo.cs"),
                Path.Combine(projectDir, "AssemblyInfo.cs")
            };

            bool changed = false;
            foreach (var assemblyInfoPath in assemblyInfoPaths) {
                if (File.Exists(assemblyInfoPath)) {
                    if (ProcessFile(assemblyInfoPath)) {
                        changed = true;
                        Console.WriteLine($"[PROCESSED] {assemblyInfoPath}");
                    }
                }
            }

            return changed;
        }

        private static bool ProcessFile(string filePath) {
            var content = File.ReadAllText(filePath);
            var originalContent = content;
            
            // Process each Web-specific attribute
            foreach (var attribute in WebSpecificAttributes) {
                content = CommentOutAttribute(content, attribute);
            }

            // Save if changed
            if (content != originalContent) {
                File.WriteAllText(filePath, content, Encoding.UTF8);
                return true;
            }

            return false;
        }

        private static string CommentOutAttribute(string content, string attributeName) {
            // Pattern to match attribute lines (with or without 'Attribute' suffix)
            // Handles both:
            // [assembly: WebResource(...)]
            // [assembly: WebResourceAttribute(...)]
            var patterns = new[] {
                $@"(\[assembly:\s*{attributeName}Attribute\s*\([^\]]*\)\])",
                $@"(\[assembly:\s*{attributeName}\s*\([^\]]*\)\])"
            };

            foreach (var pattern in patterns) {
                var regex = new Regex(pattern, RegexOptions.Multiline);
                var matches = regex.Matches(content);

                if (matches.Count > 0) {
                    // Process matches in reverse order to maintain correct positions
                    for (int i = matches.Count - 1; i >= 0; i--) {
                        var match = matches[i];
                        var attributeLine = match.Groups[1].Value;
                        
                        // Create commented version with note
                        var commented = CreateCommentedAttribute(attributeLine, attributeName);
                        
                        // Replace in content
                        content = content.Substring(0, match.Index) + 
                                 commented + 
                                 content.Substring(match.Index + match.Length);
                    }
                }
            }

            return content;
        }

        private static string CreateCommentedAttribute(string attributeLine, string attributeName) {
            var sb = new StringBuilder();
            
            // Add note
            sb.AppendLine($"// NOTE: {attributeName} attribute has no equivalent in .NET (non-ASP.NET Web Forms)");
            sb.AppendLine("// This attribute was used in ASP.NET Web Forms applications.");
            sb.AppendLine("// For Blazor applications, web resources are handled differently.");
            
            // Comment out the attribute
            var lines = attributeLine.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines) {
                sb.AppendLine($"// {line}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Check if AssemblyInfo.cs contains Web-specific attributes
        /// </summary>
        public static bool HasWebSpecificAttributes(string projectDir) {
            var assemblyInfoPaths = new[] {
                Path.Combine(projectDir, "Properties", "AssemblyInfo.cs"),
                Path.Combine(projectDir, "AssemblyInfo.cs")
            };

            foreach (var assemblyInfoPath in assemblyInfoPaths) {
                if (File.Exists(assemblyInfoPath)) {
                    var content = File.ReadAllText(assemblyInfoPath);
                    
                    foreach (var attribute in WebSpecificAttributes) {
                        if (content.Contains($"[assembly: {attribute}", StringComparison.OrdinalIgnoreCase) ||
                            content.Contains($"[assembly:{attribute}", StringComparison.OrdinalIgnoreCase)) {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}

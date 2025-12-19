using Microsoft.CodeAnalysis;

namespace XafApiConverter.Converter.CodeAnalysis {
    internal class XafmlAnalysis {
        /// <summary>
        /// Analyze XAFML files for problematic types
        /// </summary>
        public static List<XafmlProblem> AnalyzeXafmlFiles(Project project) {
            var problems = new List<XafmlProblem>();

            foreach(var document in project.Documents) {
                if(!document.FilePath.EndsWith(".xafml")) continue;

                var content = File.ReadAllText(document.FilePath);

                // Check for NO_EQUIVALENT types in XAFML
                foreach(var typeEntry in TypeReplacementMap.NoEquivalentTypes) {
                    var typeName = typeEntry.Key;
                    var replacement = typeEntry.Value;

                    // XAFML uses full type names
                    var fullTypeName = replacement.GetFullOldTypeName();
                    if(content.Contains(fullTypeName)) {
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

using System;
using System.Collections.Generic;
using System.Text;

namespace XafApiConverter.Converter.CodeAnalysis {
    /// <summary>
    /// Build result container
    /// </summary>
    internal class BuildResult {
        public bool Success { get; set; }
        public int ExitCode { get; set; }
        public List<BuildError> Errors { get; set; } = new();
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
}

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace XafApiConverter {
    static class SyntaxTreeHelper {
        public static SyntaxNode CommentLine(SyntaxNode line) {
            var leadingTrivias = line.GetLeadingTrivia();
            leadingTrivias = leadingTrivias.Add(SyntaxFactory.Comment("// "));
            return line.WithLeadingTrivia(leadingTrivias);
        }
    }
}

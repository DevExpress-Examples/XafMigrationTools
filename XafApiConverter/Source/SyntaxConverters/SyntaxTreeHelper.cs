using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace XafApiConverter {
    static class SyntaxTreeHelper {
        public static SyntaxNode CommentLine(SyntaxNode line, string addComment = null) {
            var leadingTrivias = line.GetLeadingTrivia();
            if (!string.IsNullOrEmpty(addComment)) {
                leadingTrivias = leadingTrivias.Add(SyntaxFactory.Comment($"// {addComment}"));
                leadingTrivias = leadingTrivias.Add(SyntaxFactory.CarriageReturnLineFeed);
                leadingTrivias = leadingTrivias.AddRange(line.GetLeadingTrivia());
            }
            leadingTrivias = leadingTrivias.Add(SyntaxFactory.Comment("// "));
            return line.WithLeadingTrivia(leadingTrivias);
        }
    }
}

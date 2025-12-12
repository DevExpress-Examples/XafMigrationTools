using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XafApiConverter {
    class UsingsFinder : CSharpSyntaxWalker {
        public readonly List<string> Usings = new List<string>();
        public override void VisitUsingDirective(UsingDirectiveSyntax node) {
            Usings.Add(node.Name.ToString());
            base.VisitUsingDirective(node);
        }
    }

    static class UsingsRewriter {
        public static SyntaxNode AddUsingNamespaces(SyntaxNode syntaxRoot, string[] namespaces) {
            var usingsFinder = new UsingsFinder();
            usingsFinder.Visit(syntaxRoot);
            foreach (string nameSpace in namespaces) {
                if (!usingsFinder.Usings.Contains(nameSpace)) {
                    var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(nameSpace).WithLeadingTrivia(SyntaxFactory.Space))
                        .WithTrailingTrivia(SyntaxFactory.CarriageReturn, SyntaxFactory.LineFeed);
                    syntaxRoot = ((CompilationUnitSyntax)syntaxRoot).AddUsings(usingDirective);
                }
            }
            return syntaxRoot;
        }
    }
}

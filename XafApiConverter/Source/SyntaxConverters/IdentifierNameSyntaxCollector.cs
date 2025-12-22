using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XafApiConverter.SyntaxConverters {
    class IdentifierNameSyntaxCollector : CSharpSyntaxWalker {
        public readonly HashSet<IdentifierNameSyntax> Identifiers = new HashSet<IdentifierNameSyntax>();
        public override void VisitIdentifierName(IdentifierNameSyntax node) {
            base.VisitIdentifierName(node);
            Identifiers.Add(node);
        }
    }
}

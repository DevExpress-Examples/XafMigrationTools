using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XafApiConverter {
    class MemberInvocationFinder : CSharpSyntaxWalker {
        readonly HashSet<string> memberNames;
        public MemberInvocationFinder(string[] memberNames) {
            this.memberNames = new HashSet<string>(memberNames);
        }
        public bool HasInvocation { get; private set; }
        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node) {
            if (!HasInvocation && memberNames.Contains(node.Name.Identifier.Text)) {
                HasInvocation = true;
            }
            base.VisitMemberAccessExpression(node);
        }
    }
}

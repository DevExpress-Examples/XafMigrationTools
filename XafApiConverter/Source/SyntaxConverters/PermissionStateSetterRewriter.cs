using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XafApiConverter {
    class PermissionStateSetterRewriter : CSharpSyntaxRewriter {
        static readonly string[] allowParts = new string[] { "DevExpress", "Persistent", "Base", "SecurityPermissionState", "Allow" };
        static readonly string[] denyParts = new string[] { "DevExpress", "Persistent", "Base", "SecurityPermissionState", "Deny" };
        static readonly HashSet<string> typesToMemberReplacement = new HashSet<string>() {
            "PermissionPolicyMemberPermissionsObject",
            "PermissionPolicyObjectPermissionsObject",
            "PermissionPolicyTypePermissionObject"
        };
        static readonly Dictionary<string, string> memberReplacements = new Dictionary<string, string>() {
            { "AllowCreate", "CreateState" },
            { "AllowRead", "ReadState" },
            { "AllowWrite", "WriteState" },
            { "AllowDelete", "DeleteState" },
            { "AllowNavigate", "NavigateState" }
        };
        readonly SemanticModel semanticModel;

        public PermissionStateSetterRewriter(SemanticModel semanticModel) {
            this.semanticModel = semanticModel;
        }

        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node) {
            if (node.Left is MemberAccessExpressionSyntax memberAccess) {
                string newName = memberReplacements.GetValueOrDefault(memberAccess.Name.Identifier.Text);
                if (newName != null) {
                    if (IsValidTypeToMemberReplacement(memberAccess.Expression)) {
                        var newLeft = memberAccess
                            .WithName(SyntaxFactory.IdentifierName(newName))
                            .WithTriviaFrom(memberAccess);
                        node = node = node.WithLeft(newLeft).WithTriviaFrom(node);
                        if (node.Right is LiteralExpressionSyntax literalExpression) {
                            string rightConstant = literalExpression.Token.Text;
                            if (rightConstant == "true") {
                                var allowValue = CreateNameSyntax(allowParts, allowParts.Length);
                                node = node.WithRight(allowValue).WithTriviaFrom(node);
                            }
                            else if (rightConstant == "false") {
                                var denyValue = CreateNameSyntax(denyParts, denyParts.Length);
                                node = node.WithRight(denyValue).WithTriviaFrom(node);
                            }
                        }
                    }
                }
            }
            return base.VisitAssignmentExpression(node);
        }

        bool IsValidTypeToMemberReplacement(ExpressionSyntax expression) {
            var typeInfo = semanticModel.GetTypeInfo(expression);
            if (typeInfo.Type != null) {
                return typesToMemberReplacement.Contains(typeInfo.Type.Name);
            }
            return false;
        }

        static NameSyntax CreateNameSyntax(string[] nameParts, int length) {
            if (length == 1) {
                return SyntaxFactory.IdentifierName(nameParts[0]);
            }
            else {
                return SyntaxFactory.QualifiedName(
                    CreateNameSyntax(nameParts, length - 1),
                    SyntaxFactory.IdentifierName(nameParts[length - 1]));
            }
        }
    }
}

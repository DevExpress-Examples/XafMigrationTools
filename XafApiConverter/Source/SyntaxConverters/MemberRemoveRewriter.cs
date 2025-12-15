using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XafApiConverter {
    class MemberRemoveRewriter : CSharpSyntaxRewriter {
        readonly string[] membersToRemove;
        readonly string typeToMembersRemove;
        readonly SemanticModel semanticModel;

        public MemberRemoveRewriter(SemanticModel semanticModel, string typeName, string[] membersToRemove) {
            this.membersToRemove = membersToRemove;
            this.typeToMembersRemove = typeName;
            this.semanticModel = semanticModel;
        }

        bool insideInvocation = false;
        bool hasAccessToMember = false;

        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node) {
            if (!insideInvocation) {
                insideInvocation = true;
                var newNode = base.VisitAssignmentExpression(node);
                insideInvocation = false;
                if (hasAccessToMember) {
                    hasAccessToMember = false;
                    newNode = SyntaxTreeHelper.CommentLine(newNode);
                }
                return newNode;
            }
            else {
                return base.VisitAssignmentExpression(node);
            }
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node) {
            if (!insideInvocation) {
                insideInvocation = true;
                var newNode = base.VisitInvocationExpression(node);
                insideInvocation = false;
                if (hasAccessToMember) {
                    hasAccessToMember = false;
                    return SyntaxTreeHelper.CommentLine(newNode);
                }
                return newNode;
            }
            else {
                return base.VisitInvocationExpression(node);
            }
        }

        public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node) {
            string name = node.Name.Identifier.Text;
            if (membersToRemove.Contains(name)) {
                if (IsValidTypeToMemberRemove(node.Expression)) {
                    hasAccessToMember = true;
                }
            }
            return base.VisitMemberAccessExpression(node);
        }

        bool IsValidTypeToMemberRemove(ExpressionSyntax expression) {
            var typeInfo = semanticModel.GetTypeInfo(expression);
            if (typeInfo.Type != null) {
                return typeToMembersRemove == typeInfo.Type.Name;
            }
            return false;
        }
    }
}

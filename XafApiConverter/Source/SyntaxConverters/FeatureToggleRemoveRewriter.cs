using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XafApiConverter {
    class FeatureToggleRemoveRewriter : CSharpSyntaxRewriter {
        readonly string[] featureTogglesToRemove;

        public FeatureToggleRemoveRewriter(string[] featureTogglesToRemove) {
            this.featureTogglesToRemove = featureTogglesToRemove;
        }

        bool isInsideAssignmentExpression = false;
        bool hasFeatureToggleAccess = false;

        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node) {
            if (!isInsideAssignmentExpression) {
                isInsideAssignmentExpression = true;
                SyntaxNode newNode = base.VisitAssignmentExpression(node);
                isInsideAssignmentExpression = false;
                if (hasFeatureToggleAccess) {
                    hasFeatureToggleAccess = false;
                    newNode = SyntaxTreeHelper.CommentLine(newNode, "https://supportcenter.devexpress.com/ticket/details/T1312589");
                }
                return newNode;
            }
            else {
                return base.VisitAssignmentExpression(node);
            }
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node) {
            string identifier = node.Identifier.Text;
            if (featureTogglesToRemove.Any(t => t == identifier || t.EndsWith("." + identifier))) {
                hasFeatureToggleAccess = true;
            }
            return base.VisitIdentifierName(node);
        }
    }
}

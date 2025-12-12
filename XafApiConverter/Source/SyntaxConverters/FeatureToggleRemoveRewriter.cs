using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XafApiConverter {
    class FeatureToggleRemoveRewriter : CSharpSyntaxRewriter {
        readonly SemanticModel semanticModel;
        readonly SyntaxNode syntaxRoot;
        readonly string[] featureTogglesToRemove;

        public FeatureToggleRemoveRewriter(SemanticModel semanticModel, SyntaxNode syntaxRoot, string[] featureTogglesToRemove) {
            this.semanticModel = semanticModel;
            this.syntaxRoot = syntaxRoot;
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
                    newNode = CommentLine(newNode);
                }
                return newNode;
            }
            else {
                return base.VisitAssignmentExpression(node);
            }
        }

        static SyntaxNode CommentLine(SyntaxNode line) {
            var trivias = line.GetLeadingTrivia();
            trivias = trivias.Add(SyntaxFactory.Comment("// "));
            return line.WithLeadingTrivia(trivias);
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

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XafApiConverter {
    class TypeReplaceRewriter : CSharpSyntaxRewriter {
        readonly string replaceFromFullName;
        readonly string replaceFromShortName;
        readonly string[] replaceFromNameParts;
        readonly string replaceToFullName;
        readonly string replaceToShortName;
        readonly string[] replaceToNameParts;

        public TypeReplaceRewriter(string replaceFromFullName,
            string replaceToFullName) {
            replaceFromNameParts = replaceFromFullName.Split('.');
            replaceToNameParts = replaceToFullName.Split('.');
            this.replaceFromFullName = replaceFromFullName;
            this.replaceFromShortName = replaceFromNameParts[replaceFromNameParts.Length - 1];
            this.replaceToFullName = replaceToFullName;
            this.replaceToShortName = replaceToNameParts[replaceToNameParts.Length - 1];
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node) {
            if (node.Identifier.Text == replaceFromShortName) {
                if (node.Parent is TypeArgumentListSyntax
                    || node.Parent is VariableDeclarationSyntax
                    || node.Parent is MemberDeclarationSyntax
                    || node.Parent is BaseTypeSyntax
                    || node.Parent is BaseParameterSyntax
                    || node.Parent is BaseObjectCreationExpressionSyntax
                    || node.Parent is TypeOfExpressionSyntax
                    || node.Parent is DefaultExpressionSyntax
                    || node.Parent is TypeSyntax
                    || node.Parent is CastExpressionSyntax) {
                    node = node.ReplaceToken(node.Identifier, SyntaxFactory.Identifier(replaceToFullName)).WithTriviaFrom(node);
                }
            }
            return base.VisitIdentifierName(node);
        }

        public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node) {
            if (node.Right.Identifier.Text == replaceFromShortName) {
                string fullName = GetFullName(node);
                if (replaceFromFullName == fullName || replaceFromFullName.EndsWith("." + fullName)) {
                    var newLeft = CreateNameSyntax(replaceToNameParts, replaceToNameParts.Length - 1);
                    var newRight = SyntaxFactory.IdentifierName(replaceToShortName);
                    node = node.WithLeft(newLeft).WithRight(newRight).WithTriviaFrom(node);
                }
            }
            return base.VisitQualifiedName(node);
        }

        static string GetFullName(QualifiedNameSyntax qualifiedName) {
            var leftQualified = qualifiedName.Left as QualifiedNameSyntax;
            if (leftQualified != null) {
                return GetFullName(leftQualified) + "." + qualifiedName.Right.Identifier.Text;
            }
            else {
                var leftIdentifier = qualifiedName.Left as IdentifierNameSyntax;
                if (leftIdentifier != null) {
                    return leftIdentifier.Identifier.Text + "." + qualifiedName.Right.Identifier.Text;
                }
            }
            return qualifiedName.Right.Identifier.Text;
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

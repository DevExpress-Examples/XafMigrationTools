using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics.CodeAnalysis;

namespace XafApiConverter {
    class TypeSyntaxCollector : CSharpSyntaxWalker {
        public readonly HashSet<TypeSyntax> Types = new HashSet<TypeSyntax>(new TypeSyntaxEqualityComparer());
        class TypeSyntaxEqualityComparer : IEqualityComparer<TypeSyntax> {
            public bool Equals(TypeSyntax x, TypeSyntax y) {
                return x.ToString().Equals(y.ToString());
            }
            public int GetHashCode([DisallowNull] TypeSyntax obj) {
                return obj.ToString().GetHashCode();
            }
        }
        void AddType(TypeSyntax type) {
            if (type != null && !(type is PredefinedTypeSyntax)) {
                Types.Add(type);
            }
        }
        public override void VisitIdentifierName(IdentifierNameSyntax node) {
            base.VisitIdentifierName(node);
            if (node.Parent is TypeSyntax typeSyntax && !(node.Parent is QualifiedNameSyntax)) {
                AddType(typeSyntax);
            }
        }
        public override void VisitQualifiedName(QualifiedNameSyntax node) {
            if (!(node.Parent is QualifiedNameSyntax)) {
                AddType(node);
            }
            base.VisitQualifiedName(node);
        }
        public override void VisitTypeArgumentList(TypeArgumentListSyntax node) {
            base.VisitTypeArgumentList(node);
            foreach (var type in node.Arguments) {
                AddType(type);
            }
        }
        public override void VisitArrayType(ArrayTypeSyntax node) {
            base.VisitArrayType(node);
            AddType(node.ElementType);
        }
        public override void VisitPointerType(PointerTypeSyntax node) {
            base.VisitPointerType(node);
            AddType(node.ElementType);
        }
        public override void VisitNullableType(NullableTypeSyntax node) {
            base.VisitNullableType(node);
            AddType(node.ElementType);
        }
        public override void VisitTupleElement(TupleElementSyntax node) {
            base.VisitTupleElement(node);
            AddType(node.Type);
        }
        public override void VisitRefType(RefTypeSyntax node) {
            base.VisitRefType(node);
            AddType(node.Type);
        }
        public override void VisitScopedType(ScopedTypeSyntax node) {
            base.VisitScopedType(node);
            AddType(node.Type);
        }
        public override void VisitDefaultExpression(DefaultExpressionSyntax node) {
            base.VisitDefaultExpression(node);
            AddType(node.Type);
        }
        public override void VisitTypeOfExpression(TypeOfExpressionSyntax node) {
            base.VisitTypeOfExpression(node);
            AddType(node.Type);
        }
        public override void VisitSizeOfExpression(SizeOfExpressionSyntax node) {
            base.VisitSizeOfExpression(node);
            AddType(node.Type);
        }
        public override void VisitDeclarationExpression(DeclarationExpressionSyntax node) {
            base.VisitDeclarationExpression(node);
            AddType(node.Type);
        }
        public override void VisitCastExpression(CastExpressionSyntax node) {
            base.VisitCastExpression(node);
            AddType(node.Type);
        }
        public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) {
            base.VisitParenthesizedLambdaExpression(node);
            AddType(node.ReturnType);
        }
        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node) {
            base.VisitObjectCreationExpression(node);
            AddType(node.Type);
        }
        public override void VisitArrayCreationExpression(ArrayCreationExpressionSyntax node) {
            base.VisitArrayCreationExpression(node);
            AddType(node.Type);
        }
        public override void VisitStackAllocArrayCreationExpression(StackAllocArrayCreationExpressionSyntax node) {
            base.VisitStackAllocArrayCreationExpression(node);
            AddType(node.Type);
        }
        public override void VisitFromClause(FromClauseSyntax node) {
            base.VisitFromClause(node);
            AddType(node.Type);
        }
        public override void VisitJoinClause(JoinClauseSyntax node) {
            base.VisitJoinClause(node);
            AddType(node.Type);
        }
        public override void VisitDeclarationPattern(DeclarationPatternSyntax node) {
            base.VisitDeclarationPattern(node);
            AddType(node.Type);
        }
        public override void VisitRecursivePattern(RecursivePatternSyntax node) {
            base.VisitRecursivePattern(node);
            AddType(node.Type);
        }
        public override void VisitTypePattern(TypePatternSyntax node) {
            base.VisitTypePattern(node);
            AddType(node.Type);
        }
        public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node) {
            base.VisitLocalFunctionStatement(node);
            AddType(node.ReturnType);
        }
        public override void VisitVariableDeclaration(VariableDeclarationSyntax node) {
            base.VisitVariableDeclaration(node);
            AddType(node.Type);
        }
        public override void VisitCatchDeclaration(CatchDeclarationSyntax node) {
            base.VisitCatchDeclaration(node);
            AddType(node.Type);
        }
        public override void VisitUsingDirective(UsingDirectiveSyntax node) {
            base.VisitUsingDirective(node);
            AddType(node.NamespaceOrType);
        }
        public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node) {
            base.VisitDelegateDeclaration(node);
            AddType(node.ReturnType);
        }
        public override void VisitSimpleBaseType(SimpleBaseTypeSyntax node) {
            base.VisitSimpleBaseType(node);
            AddType(node.Type);
        }
        public override void VisitPrimaryConstructorBaseType(PrimaryConstructorBaseTypeSyntax node) {
            base.VisitPrimaryConstructorBaseType(node);
            AddType(node.Type);
        }
        public override void VisitTypeConstraint(TypeConstraintSyntax node) {
            base.VisitTypeConstraint(node);
            AddType(node.Type);
        }
        public override void VisitMethodDeclaration(MethodDeclarationSyntax node) {
            base.VisitMethodDeclaration(node);
            AddType(node.ReturnType);
        }
        public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node) {
            base.VisitOperatorDeclaration(node);
            AddType(node.ReturnType);
        }
        public override void VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node) {
            base.VisitConversionOperatorDeclaration(node);
            AddType(node.Type);
        }
        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node) {
            base.VisitPropertyDeclaration(node);
            AddType(node.Type);
        }
        public override void VisitEventDeclaration(EventDeclarationSyntax node) {
            base.VisitEventDeclaration(node);
            AddType(node.Type);
        }
        public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node) {
            base.VisitIndexerDeclaration(node);
            AddType(node.Type);
        }
        public override void VisitParameter(ParameterSyntax node) {
            base.VisitParameter(node);
            AddType(node.Type);
        }
        public override void VisitFunctionPointerParameter(FunctionPointerParameterSyntax node) {
            base.VisitFunctionPointerParameter(node);
            AddType(node.Type);
        }
        public override void VisitIncompleteMember(IncompleteMemberSyntax node) {
            base.VisitIncompleteMember(node);
            AddType(node.Type);
        }
    }
}

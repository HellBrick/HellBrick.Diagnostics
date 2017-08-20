using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace HellBrick.Diagnostics.StructDeclarations.EquatabilityRules
{
	internal abstract class ImplementEqualityOperatorRule : IEquatabilityRule
	{
		private readonly TypeSyntax _boolTypeName = ParseTypeName( "bool" );
		private readonly IdentifierNameSyntax _equalsMethodName = IdentifierName( "Equals" );
		private const string _xArg = "x";
		private const string _yArg = "y";

		public string ID => $"ImplementOperator{OperatorToken.Kind()}";
		public string RuleText => $"should implement operator {OperatorToken.ValueText}";

		protected abstract SyntaxToken OperatorToken { get; }
		protected abstract ExpressionSyntax BuildOperatorBody( ExpressionSyntax equalsCall );

		public bool IsViolatedBy( StructDeclarationSyntax structDeclaration, ITypeSymbol structType, SemanticModel semanticModel )
		{
			IEnumerable<OperatorDeclarationSyntax> operatorQuery =
				from op in structDeclaration.Members.OfType<OperatorDeclarationSyntax>()
				where op.OperatorToken.IsKind( OperatorToken.Kind() )
				let parameters = op.ParameterList.Parameters
				where parameters.Count == 2
				where parameters.All( p => semanticModel.GetTypeInfo( p.Type ).Type?.Equals( structType ) == true )
				select op;

			bool implementsOperator = operatorQuery.Any();
			return !implementsOperator;
		}

		public StructDeclarationSyntax Enforce( StructDeclarationSyntax structDeclaration, INamedTypeSymbol structType, SemanticModel semanticModel, ISymbol[] fieldsAndProperties )
		{
			OperatorDeclarationSyntax operatorDeclaration = BuildOperatorDeclaration( structDeclaration, structType );
			return structDeclaration.AddMembers( operatorDeclaration );
		}

		private OperatorDeclarationSyntax BuildOperatorDeclaration( StructDeclarationSyntax structDeclaration, INamedTypeSymbol structType )
		{
			OperatorDeclarationSyntax operatorDeclaration = OperatorDeclaration( _boolTypeName, OperatorToken );
			TypeSyntax structTypeName = ParseTypeName( structType.ToDisplayString() );
			operatorDeclaration = operatorDeclaration
				.AddParameterListParameters
				(
					Parameter( ParseToken( _xArg ) ).WithType( structTypeName ),
					Parameter( ParseToken( _yArg ) ).WithType( structTypeName )
				);
			operatorDeclaration = operatorDeclaration.AddModifiers( Token( SyntaxKind.PublicKeyword ), Token( SyntaxKind.StaticKeyword ) );
			operatorDeclaration = operatorDeclaration.WithExpressionBody( ArrowExpressionClause( BuildOperatorBody( BuildEqualsCall( structDeclaration, structTypeName ) ) ) );
			operatorDeclaration = operatorDeclaration.WithSemicolonToken( Token( SyntaxKind.SemicolonToken ) );
			operatorDeclaration = operatorDeclaration.WithAdditionalAnnotations( Simplifier.Annotation );

			return operatorDeclaration;
		}

		private ExpressionSyntax BuildEqualsCall( StructDeclarationSyntax structDeclaration, TypeSyntax structTypeName )
		{
			MemberAccessExpressionSyntax equalsMethod = MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, IdentifierName( _xArg ), _equalsMethodName );
			InvocationExpressionSyntax call = InvocationExpression( equalsMethod ).AddArgumentListArguments( Argument( IdentifierName( _yArg ) ) );
			return call;
		}
	}

	internal sealed class ImplementEqualsOperatorRule : ImplementEqualityOperatorRule
	{
		protected override SyntaxToken OperatorToken => Token( SyntaxKind.EqualsEqualsToken );
		protected override ExpressionSyntax BuildOperatorBody( ExpressionSyntax equalsCall ) => equalsCall;
	}

	internal sealed class ImplementNotEqualsOperatorRule : ImplementEqualityOperatorRule
	{
		protected override SyntaxToken OperatorToken => Token( SyntaxKind.ExclamationEqualsToken );
		protected override ExpressionSyntax BuildOperatorBody( ExpressionSyntax equalsCall ) => PrefixUnaryExpression( SyntaxKind.LogicalNotExpression, equalsCall );
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using HellBrick.Diagnostics.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace HellBrick.Diagnostics.StructDeclarations.EquatabilityRules
{
	internal class OverrideGetHashCodeRule : IEquatabilityRule
	{
		private static readonly TypeSyntax _intTypeName = PredefinedType( Token( SyntaxKind.IntKeyword ) );
		private static readonly TypeSyntax _varTypeName = IdentifierName( "var" );

		private const string _primeName = "prime";
		private const string _hashName = "hash";
		private const int _initialHashValue = 12345701;

		private static readonly LocalDeclarationStatementSyntax _primeDeclaration = IntDeclaration( _intTypeName, _primeName, -1521134295 ).AddModifiers( Token( SyntaxKind.ConstKeyword ) );
		private static readonly LocalDeclarationStatementSyntax _hashDeclaration = IntDeclaration( _intTypeName, _hashName, _initialHashValue );
		private static readonly LocalDeclarationStatementSyntax _hashVarDeclaration = IntDeclaration( _varTypeName, _hashName, _initialHashValue );

		private static LocalDeclarationStatementSyntax IntDeclaration( TypeSyntax typeName, string localName, int value )
			=> LocalDeclarationStatement( VariableDeclaration( typeName ).AddVariables( Declarator( localName, value ) ) );

		private static VariableDeclaratorSyntax Declarator( string localName, int value )
			=> VariableDeclarator( localName ).WithInitializer( EqualsValueClause( LiteralExpression( SyntaxKind.NumericLiteralExpression, Literal( value ) ) ) );

		private static readonly IdentifierNameSyntax _getHashCodeName = IdentifierName( "GetHashCode" );

		public string ID => "OverrideGetHashCode";
		public string RuleText => "override GetHashCode()";

		public bool IsViolatedBy( StructDeclarationSyntax structDeclaration, ITypeSymbol structType, SemanticModel semanticModel )
		{
			bool overridesGetHashCode = structDeclaration.Members
				.OfType<MethodDeclarationSyntax>()
				.Where( m => m.Modifiers.Any( SyntaxKind.OverrideKeyword ) )
				.Where( m => m.Identifier.ValueText == "GetHashCode" )
				.Any();

			return !overridesGetHashCode;
		}

		public StructDeclarationSyntax Enforce( StructDeclarationSyntax structDeclaration, INamedTypeSymbol structType, TypeSyntax structTypeName, SemanticModel semanticModel, ISymbol[] fieldsAndProperties, DocumentOptionSet options )
		{
			MethodDeclarationSyntax equalsOverrideDeclaration = BuldGetHashCodeOverrideDeclaration( fieldsAndProperties, options );
			return structDeclaration.AddMembers( equalsOverrideDeclaration );
		}

		private MethodDeclarationSyntax BuldGetHashCodeOverrideDeclaration( ISymbol[] fieldsAndProperties, DocumentOptionSet options )
		{
			MethodDeclarationSyntax method = MethodDeclaration( _intTypeName, "GetHashCode" );
			method = method.WithModifiers( TokenList( Token( SyntaxKind.PublicKeyword ), Token( SyntaxKind.OverrideKeyword ) ) );

			ExpressionSyntax body = BuildExpressionBody( fieldsAndProperties );
			method = method.WithExpressionBody( ArrowExpressionClause( body ) );
			method = method.WithSemicolonToken( Token( SyntaxKind.SemicolonToken ) );
			method = method.WithAdditionalAnnotations( Simplifier.Annotation );
			return method;
		}

		private ExpressionSyntax BuildExpressionBody( ISymbol[] fieldsAndProperties )
		{
			if ( fieldsAndProperties.Length == 0 )
				return LiteralExpression( SyntaxKind.NumericLiteralExpression, Literal( 0 ) );

			if ( fieldsAndProperties.Length == 1 )
				return BuildFieldHashCodeCall( fieldsAndProperties[ 0 ] );

			return BuildTupleHashCodeCall( fieldsAndProperties );
		}

		private ExpressionSyntax BuildFieldHashCodeCall( ISymbol fieldSymbol )
		{
			ITypeSymbol fieldType = ( fieldSymbol as IFieldSymbol )?.Type ?? ( fieldSymbol as IPropertySymbol ).Type;
			return fieldType.IsValueType ? BuildValueTypeHashCodeCall() : BuildReferenceTypeHashCodeCall();

			ExpressionSyntax BuildValueTypeHashCodeCall()
				=> InvocationExpression
				(
					MemberAccessExpression
					(
						SyntaxKind.SimpleMemberAccessExpression,
						IdentifierName( fieldSymbol.Name ),
						_getHashCodeName
					)
				);

			ExpressionSyntax BuildReferenceTypeHashCodeCall()
				=> ParenthesizedExpression
				(
					BinaryExpression
					(
						SyntaxKind.CoalesceExpression,
						ConditionalAccessExpression
						(
							IdentifierName( fieldSymbol.Name ),
							InvocationExpression( MemberBindingExpression( _getHashCodeName ) )
						),
						LiteralExpression( SyntaxKind.NumericLiteralExpression, Literal( 0 ) )
					)
				);
		}

		private ExpressionSyntax BuildTupleHashCodeCall( ISymbol[] fieldsAndProperties )
			=> InvocationExpression
			(
				MemberAccessExpression
				(
					SyntaxKind.SimpleMemberAccessExpression,
					TupleExpression
					(
						SeparatedList( fieldsAndProperties.Select( f => Argument( IdentifierName( f.Name ) ) ) )
					),
					_getHashCodeName
				)
			);
	}
}

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

		public StructDeclarationSyntax Enforce( StructDeclarationSyntax structDeclaration, INamedTypeSymbol structType, SemanticModel semanticModel, ISymbol[] fieldsAndProperties, DocumentOptionSet options )
		{
			MethodDeclarationSyntax equalsOverrideDeclaration = BuldGetHashCodeOverrideDeclaration( fieldsAndProperties, options );
			return structDeclaration.AddMembers( equalsOverrideDeclaration );
		}

		private MethodDeclarationSyntax BuldGetHashCodeOverrideDeclaration( ISymbol[] fieldsAndProperties, DocumentOptionSet options )
		{
			MethodDeclarationSyntax method = MethodDeclaration( _intTypeName, "GetHashCode" );
			method = method.WithModifiers( TokenList( Token( SyntaxKind.PublicKeyword ), Token( SyntaxKind.OverrideKeyword ) ) );

			if ( fieldsAndProperties.Length <= 1 )
			{
				//	If there's 0 or 1 fields, GetHashCode() can be implemented as an expression-bodied method.
				ExpressionSyntax body = BuildExpressionBody( fieldsAndProperties );
				method = method.WithExpressionBody( ArrowExpressionClause( body ) );
				method = method.WithSemicolonToken( Token( SyntaxKind.SemicolonToken ) );
			}
			else
			{
				BlockSyntax body = BuildBlockBody( fieldsAndProperties, options );
				method = method.WithBody( body );
			}

			method = method.WithAdditionalAnnotations( Simplifier.Annotation );
			return method;
		}

		private ExpressionSyntax BuildExpressionBody( ISymbol[] fieldsAndProperties )
		{
			if ( fieldsAndProperties.Length == 0 )
				return LiteralExpression( SyntaxKind.NumericLiteralExpression, Literal( 0 ) );

			if ( fieldsAndProperties.Length == 1 )
				return BuildFieldHashCodeCall( fieldsAndProperties[ 0 ] );

			throw new InvalidOperationException( $"{nameof( BuildExpressionBody )} should never be called if there's more than 1 field." );
		}

		private BlockSyntax BuildBlockBody( ISymbol[] fieldsAndProperties, DocumentOptionSet options ) =>
			Block( CheckedStatement( SyntaxKind.UncheckedStatement, Block( EnumerateHashCombinerStatements( fieldsAndProperties, options ) ) ) );

		private IEnumerable<StatementSyntax> EnumerateHashCombinerStatements( ISymbol[] fieldsAndProperties, DocumentOptionSet options )
		{
			yield return _primeDeclaration;
			yield return
				options.GetOption( CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes ).Value
				? _hashVarDeclaration
				: _hashDeclaration;

			foreach ( ISymbol field in fieldsAndProperties )
			{
				AssignmentExpressionSyntax assignment =
					AssignmentExpression
					(
						SyntaxKind.SimpleAssignmentExpression,
						IdentifierName( _hashName ),
						BinaryExpression
						(
							SyntaxKind.AddExpression,
							BinaryExpression( SyntaxKind.MultiplyExpression, IdentifierName( _hashName ), IdentifierName( _primeName ) ),
							BuildFieldHashCodeCall( field )
						)
					);

				ExpressionStatementSyntax statement = ExpressionStatement( assignment, Token( SyntaxKind.SemicolonToken ) );
				yield return statement;
			}

			yield return ReturnStatement( IdentifierName( _hashName ) );
		}

		private ExpressionSyntax BuildFieldHashCodeCall( ISymbol fieldSymbol )
		{
			ITypeSymbol fieldType = ( fieldSymbol as IFieldSymbol )?.Type ?? ( fieldSymbol as IPropertySymbol ).Type;
			MemberAccessExpressionSyntax defaultComparer = DefaultEqualityComparer.AccessExpression( fieldType );
			MemberAccessExpressionSyntax defaultGetHashCodeMethod = MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, defaultComparer, _getHashCodeName );
			MemberAccessExpressionSyntax fieldAccess = MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName( fieldSymbol.Name ) );
			InvocationExpressionSyntax getHashCodeCall = InvocationExpression( defaultGetHashCodeMethod ).AddArgumentListArguments( Argument( fieldAccess ) );
			return getHashCodeCall;
		}
	}
}

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace HellBrick.Diagnostics.StructDeclarations.EquatabilityRules
{
	internal class ImplementEquatableRule : IEquatabilityRule
	{
		private SyntaxTriviaList _endlineTriviaList = TriviaList( EndOfLine( Environment.NewLine ) );
		private readonly TypeSyntax _boolTypeName = ParseTypeName( "bool" );
		private const string _otherArg = "other";

		public string ID => StructIDPrefix.Value + "ImplementIEquatable";
		public string RuleText => "implement IEquatable<T>";

		public bool IsViolatedBy( StructDeclarationSyntax structDeclaration, ITypeSymbol structType, SemanticModel semanticModel )
		{
			bool implementsEquatable = structType.AllInterfaces
				.Where( i => i.ContainingNamespace.Name == "System" && i.Name == "IEquatable" )
				.Where( i => i.IsGenericType && i.TypeArguments.Length == 1 && i.TypeArguments[ 0 ] == structType )
				.Any();

			return !implementsEquatable;
		}

		public StructDeclarationSyntax Enforce( StructDeclarationSyntax structDeclaration, INamedTypeSymbol structType, TypeSyntax structTypeName, SemanticModel semanticModel, ISymbol[] fieldsAndProperties, DocumentOptionSet options )
		{
			structDeclaration = RemoveEndlineTriviaFromIdentifierIfHasNoInterfaces( structDeclaration );
			structDeclaration = AddInterfaceToBaseList( structDeclaration, structType );
			structDeclaration = AddEndlineTriviaToBaseListIfHadNoInterfaces( structDeclaration );
			structDeclaration = ImplementInterface( structDeclaration, structType, structTypeName, semanticModel, fieldsAndProperties );

			return structDeclaration;
		}

		private static StructDeclarationSyntax RemoveEndlineTriviaFromIdentifierIfHasNoInterfaces( StructDeclarationSyntax structDeclaration )
		{
			//	If the struct implements no interfaces, we have to move the endline trivia from identifier to the base list.
			if ( structDeclaration.BaseList == null )
			{
				//	lastTokenBeforeBrace may be either identifier name or > if the struct is generic.
				//	So it's easier to just get the token before the {.
				SyntaxToken lastTokenBeforeBrace = structDeclaration.OpenBraceToken.GetPreviousToken();
				SyntaxToken withoutTrivia = lastTokenBeforeBrace.WithTrailingTrivia();
				structDeclaration = structDeclaration.ReplaceToken( lastTokenBeforeBrace, withoutTrivia );
			}

			return structDeclaration;
		}

		private static StructDeclarationSyntax AddInterfaceToBaseList( StructDeclarationSyntax structDeclaration, INamedTypeSymbol structType )
		{
			TypeSyntax interfaceTypeNode = ParseTypeName( $"System.IEquatable<{structType.ToDisplayString()}>" ).WithAdditionalAnnotations( Simplifier.Annotation );
			SimpleBaseTypeSyntax interfaceImplementationNode = SimpleBaseType( interfaceTypeNode );
			structDeclaration = structDeclaration.AddBaseListTypes( interfaceImplementationNode );
			return structDeclaration;
		}

		private StructDeclarationSyntax AddEndlineTriviaToBaseListIfHadNoInterfaces( StructDeclarationSyntax structDeclaration )
		{
			//	This happens if the struct hasn't implemented any interfaces before.
			//	If this is the case, we have to add an endline trivia after our new base list.
			if ( !structDeclaration.BaseList.HasTrailingTrivia )
			{
				BaseListSyntax newBaseList = structDeclaration.BaseList.WithTrailingTrivia( _endlineTriviaList );
				structDeclaration = structDeclaration.WithBaseList( newBaseList );
			}

			return structDeclaration;
		}

		private StructDeclarationSyntax ImplementInterface( StructDeclarationSyntax structDeclaration, INamedTypeSymbol structType, TypeSyntax structTypeName, SemanticModel semanticModel, ISymbol[] fieldsAndProperties )
		{
			MethodDeclarationSyntax equalsMethodDeclaration = BuldEqualsMethodDeclaration( structDeclaration, structType, structTypeName, semanticModel, fieldsAndProperties );
			return structDeclaration.AddMembers( equalsMethodDeclaration );
		}

		private MethodDeclarationSyntax BuldEqualsMethodDeclaration( StructDeclarationSyntax structDeclaration, INamedTypeSymbol structType, TypeSyntax structTypeName, SemanticModel semanticModel, ISymbol[] fieldsAndProperties )
		{
			MethodDeclarationSyntax method = MethodDeclaration( _boolTypeName, "Equals" );
			ParameterSyntax parameter = Parameter( ParseToken( _otherArg ).WithLeadingTrivia( Whitespace( " " ) ) ).WithType( structTypeName );

			method = method.WithModifiers( TokenList( Token( SyntaxKind.PublicKeyword ) ) );
			method = method.AddParameterListParameters( parameter );
			method = method.WithExpressionBody( ArrowExpressionClause( BuildEqualsBodyExpression( structDeclaration, semanticModel, fieldsAndProperties ) ) );
			method = method.WithSemicolonToken( Token( SyntaxKind.SemicolonToken ) );
			method = method.WithAdditionalAnnotations( Simplifier.Annotation );

			return method;
		}

		/// <summary>
		/// Builds the expression that's going to form the method body. The result is going to be attached to the method via an arrow expression.
		/// </summary>
		private ExpressionSyntax BuildEqualsBodyExpression( StructDeclarationSyntax structDeclaration, SemanticModel semanticModel, ISymbol[] fieldsAndProperties )
		{
			//	If there are no fields, the method is as simple as 'bool Equals( T other ) => true;'
			if ( fieldsAndProperties.Length == 0 )
				return LiteralExpression( SyntaxKind.TrueLiteralExpression );

			//	If there's only 1 field, its comparison is actually the full method body.
			if ( fieldsAndProperties.Length == 1 )
				return BuildFieldEqualityCall( fieldsAndProperties[ 0 ] );

			//	Otherwise we have to invoke tuple comparison.
			return BinaryExpression( SyntaxKind.EqualsExpression, FieldTuple( ThisExpression() ), FieldTuple( IdentifierName( _otherArg ) ) );

			TupleExpressionSyntax FieldTuple( ExpressionSyntax instance )
				=> TupleExpression
				(
					SeparatedList
					(
						fieldsAndProperties.Select
						(
							f => Argument
							(
								MemberAccessExpression
								(
									SyntaxKind.SimpleMemberAccessExpression,
									instance,
									IdentifierName( f.Name )
								)
							)
						)
					)
				);
		}

		private static ExpressionSyntax BuildFieldEqualityCall( ISymbol fieldSymbol )
		{
			ITypeSymbol fieldType = ( fieldSymbol as IFieldSymbol )?.Type ?? ( fieldSymbol as IPropertySymbol ).Type;
			bool declaresEqualityOperator = fieldType
				.GetMembers()
				.OfType<IMethodSymbol>()
				.Where( m => m.MethodKind == MethodKind.BuiltinOperator || m.MethodKind == MethodKind.UserDefinedOperator )
				.Where( m => m.Name == "op_Equality" )
				.Any();

			//	If the field type declares == operator, we can safely use it.
			// This provides better readability in a lot of cases.
			if ( declaresEqualityOperator )
			{
				return BinaryExpression
				(
					SyntaxKind.EqualsExpression,
					MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName( fieldSymbol.Name ) ),
					MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, IdentifierName( _otherArg ), IdentifierName( fieldSymbol.Name ) )
				);
			}

			//	Otherwise, we have to resort to EqualityComparer<T>.Default.Equals( this._field, other._field )
			MemberAccessExpressionSyntax defaultProperty = DefaultEqualityComparer.AccessExpression( fieldType );
			MemberAccessExpressionSyntax equalsMethod = MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, defaultProperty, IdentifierName( "Equals" ) );
			InvocationExpressionSyntax invocation = InvocationExpression( equalsMethod );
			invocation = invocation
				.AddArgumentListArguments
				(
					Argument( MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName( fieldSymbol.Name ) ) ),
					Argument( MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, IdentifierName( _otherArg ), IdentifierName( fieldSymbol.Name ) ) )
				);

			return invocation;
		}
	}
}

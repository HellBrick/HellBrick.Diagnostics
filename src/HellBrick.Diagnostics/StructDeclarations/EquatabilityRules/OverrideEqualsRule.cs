using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace HellBrick.Diagnostics.StructDeclarations.EquatabilityRules
{
	internal class OverrideEqualsRule : IEquatabilityRule
	{
		private readonly TypeSyntax _boolTypeName = ParseTypeName( "bool" );
		private readonly TypeSyntax _objectTypeName = ParseTypeName( "object" );
		private readonly IdentifierNameSyntax _equalsMethodName = IdentifierName( "Equals" );
		private const string _objArg = "obj";
		private const string _otherPatternVar = "other";
		private static readonly SyntaxToken _otherIdentitiferToken = Identifier( _otherPatternVar );
		private static readonly IdentifierNameSyntax _otherIdentifierSyntax = IdentifierName( _otherIdentitiferToken );

		public string ID => "OverrideEquals";
		public string RuleText => "should override Equals()";

		public bool IsViolatedBy( StructDeclarationSyntax structDeclaration, ITypeSymbol structType, SemanticModel semanticModel )
		{
			bool overridesEquals = structDeclaration.Members
				.OfType<MethodDeclarationSyntax>()
				.Where( m => m.Modifiers.Any( SyntaxKind.OverrideKeyword ) )
				.Where( m => m.Identifier.ValueText == "Equals" )
				.Any();

			return !overridesEquals;
		}

		public StructDeclarationSyntax Enforce( StructDeclarationSyntax structDeclaration, INamedTypeSymbol structType, SemanticModel semanticModel, ISymbol[] fieldsAndProperties )
		{
			MethodDeclarationSyntax equalsOverrideDeclaration = BuldEqualsOverrideDeclaration( structDeclaration, structType );
			return structDeclaration.AddMembers( equalsOverrideDeclaration );
		}

		private MethodDeclarationSyntax BuldEqualsOverrideDeclaration( StructDeclarationSyntax structDeclaration, INamedTypeSymbol structType )
		{
			MethodDeclarationSyntax method = MethodDeclaration( _boolTypeName, "Equals" );
			TypeSyntax structTypeName = ParseTypeName( structType.ToDisplayString() );
			ParameterSyntax parameter = Parameter( ParseToken( _objArg ) ).WithType( _objectTypeName );
			method = method.WithModifiers( TokenList( Token( SyntaxKind.PublicKeyword ), Token( SyntaxKind.OverrideKeyword ) ) );
			method = method.AddParameterListParameters( parameter );
			method = method.WithExpressionBody( ArrowExpressionClause( BuildEqualsOverrideBodyExpression( structDeclaration, structTypeName ) ) );
			method = method.WithSemicolonToken( Token( SyntaxKind.SemicolonToken ) );
			method = method.WithAdditionalAnnotations( Simplifier.Annotation );

			return method;
		}

		private ExpressionSyntax BuildEqualsOverrideBodyExpression( StructDeclarationSyntax structDeclaration, TypeSyntax structTypeName )
		{
			IsPatternExpressionSyntax typeCheck = IsPatternExpression( IdentifierName( _objArg ), DeclarationPattern( structTypeName, SingleVariableDesignation( _otherIdentitiferToken ) ) );
			InvocationExpressionSyntax call = InvocationExpression( _equalsMethodName ).AddArgumentListArguments( Argument( _otherIdentifierSyntax ) );
			BinaryExpressionSyntax body = BinaryExpression( SyntaxKind.LogicalAndExpression, typeCheck, call );
			return body;
		}
	}
}

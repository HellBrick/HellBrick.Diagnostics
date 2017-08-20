using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace HellBrick.Diagnostics.StructDeclarations
{
	internal static class DefaultEqualityComparer
	{
		public static MemberAccessExpressionSyntax AccessExpression( ITypeSymbol fieldType )
		{
			TypeSyntax comparerTypeNode = ParseTypeName( $"System.Collections.Generic.EqualityComparer<{fieldType.ToDisplayString()}>" );
			MemberAccessExpressionSyntax defaultProperty = MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, comparerTypeNode, IdentifierName( "Default" ) );
			return defaultProperty;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HellBrick.Diagnostics.StructDeclarations
{
	internal static class StructDataFinder
	{
		public static IEnumerable<FieldDeclarationSyntax> EnumerateDataFields( this StructDeclarationSyntax structNode ) =>
			structNode.Members
				.Where( node => node.IsKind( SyntaxKind.FieldDeclaration ) )
				.Cast<FieldDeclarationSyntax>()
				.Where( field => !field.Modifiers.Any( SyntaxKind.StaticKeyword ) )
				.Where( field => !field.Modifiers.Any( SyntaxKind.ConstKeyword ) );

		public static IEnumerable<PropertyDeclarationSyntax> EnumerateDataProperties( this StructDeclarationSyntax structNode ) =>
			structNode.Members
				.Where( node => node.IsKind( SyntaxKind.PropertyDeclaration ) )
				.Cast<PropertyDeclarationSyntax>()
				.Where( property => !property.Modifiers.Any( SyntaxKind.StaticKeyword ) )
				.Where( property => property.HasAutoGetter() );

		private static bool HasAutoGetter( this PropertyDeclarationSyntax property )
		{
			AccessorDeclarationSyntax getter = property.AccessorList?.Accessors.FirstOrDefault( accessor => accessor.Keyword.IsKind( SyntaxKind.GetKeyword ) );
			return getter != null && getter.Body == null;
		}
	};
}

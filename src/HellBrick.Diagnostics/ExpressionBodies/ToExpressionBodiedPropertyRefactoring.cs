using System;
using System.Composition;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using HellBrick.Diagnostics.Utils;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace HellBrick.Diagnostics.ExpressionBodies
{
	[ExportCodeRefactoringProvider( LanguageNames.CSharp, Name = nameof( ToExpressionBodiedPropertyRefactoring ) ), Shared]
	internal class ToExpressionBodiedPropertyRefactoring : AbstractExpressionBodyRefactoring<PropertyDeclarationSyntax>
	{
		protected override bool CanConvertToExpression( PropertyDeclarationSyntax declaration )
		{
			AccessorDeclarationSyntax getter = GetAccessor( declaration, SyntaxKind.GetAccessorDeclaration );
			AccessorDeclarationSyntax setter = GetAccessor( declaration, SyntaxKind.SetAccessorDeclaration );

			return getter != null && setter == null;
		}

		protected override BlockSyntax GetBody( PropertyDeclarationSyntax declaration ) => GetAccessor( declaration, SyntaxKind.GetAccessorDeclaration ).Body;
		protected override string GetIdentifierName( PropertyDeclarationSyntax declaration ) => declaration.Identifier.Text;
		protected override SyntaxNode GetRemovedNode( PropertyDeclarationSyntax declaration ) => declaration.AccessorList;

		protected override PropertyDeclarationSyntax ReplaceBodyWithExpressionClause( PropertyDeclarationSyntax declaration, ArrowExpressionClauseSyntax arrow ) =>
			declaration
				.WithAccessorList( null )
				.WithExpressionBody( arrow )
				.WithSemicolonToken( Token( SyntaxKind.SemicolonToken ) );

		private AccessorDeclarationSyntax GetAccessor( PropertyDeclarationSyntax declaration, SyntaxKind accessorKind )
		{
			return declaration.AccessorList.Accessors.FirstOrDefault( a => a.IsKind( accessorKind ) );
		}
	}
}
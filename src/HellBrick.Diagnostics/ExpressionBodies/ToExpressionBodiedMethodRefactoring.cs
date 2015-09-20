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
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using HellBrick.Diagnostics.Utils;

namespace HellBrick.Diagnostics.ExpressionBodies
{
	[ExportCodeRefactoringProvider( LanguageNames.CSharp, Name = nameof( ToExpressionBodiedMethodRefactoring ) ), Shared]
	internal class ToExpressionBodiedMethodRefactoring : AbstractExpressionBodyRefactoring<MethodDeclarationSyntax>
	{
		protected override bool CanConvertToExpression( MethodDeclarationSyntax declaration ) => true;
		protected override BlockSyntax GetBody( MethodDeclarationSyntax declaration ) => declaration.Body;
		protected override string GetIdentifierName( MethodDeclarationSyntax declaration ) => declaration.Identifier.Text;
		protected override SyntaxNode GetRemovedNode( MethodDeclarationSyntax declaration ) => declaration.Body;

		protected override MethodDeclarationSyntax ReplaceBodyWithExpressionClause( MethodDeclarationSyntax declaration, ArrowExpressionClauseSyntax arrow ) =>
			declaration
				.WithBody( null )
				.WithExpressionBody( arrow )
				.WithSemicolonToken( Token( SyntaxKind.SemicolonToken ) );
	}
}
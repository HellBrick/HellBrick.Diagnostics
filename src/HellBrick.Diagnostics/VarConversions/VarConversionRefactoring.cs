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
using System.Diagnostics;

namespace HellBrick.Diagnostics.VarConversions
{
	[ExportCodeRefactoringProvider( LanguageNames.CSharp, Name = nameof( VarConversionRefactoring ) ), Shared]
	internal class VarConversionRefactoring : CodeRefactoringProvider
	{
		public sealed override async Task ComputeRefactoringsAsync( CodeRefactoringContext context )
		{
			var root = await context.Document.GetSyntaxRootAsync( context.CancellationToken ).ConfigureAwait( false );
			var spanNode = root.FindNode( context.Span );
			var declaration = spanNode.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().FirstOrDefault();

			if ( declaration == null )
				return;

			if ( !declaration.Declaration.Type.IsVar )
				return;

			CodeAction refactoring = CodeAction.Create( "Convert explicit type to 'var'", cancelToken => ConvertExplicitTypeToVarAsync( context.Document, root, declaration, cancelToken ) );
		}

		private Task<Document> ConvertExplicitTypeToVarAsync( Document document, SyntaxNode root, LocalDeclarationStatementSyntax enclosingDeclaration, CancellationToken cancellationToken )
		{
			var newType = SyntaxFactory.IdentifierName( "var" )
				.WithLeadingTrivia( enclosingDeclaration.Declaration.Type.GetLeadingTrivia() )
				.WithTrailingTrivia( enclosingDeclaration.Declaration.Type.GetTrailingTrivia() );

			var newDeclaration = enclosingDeclaration.Declaration.WithType( newType );
			var newRoot = root.ReplaceNode( enclosingDeclaration.Declaration, newDeclaration );
			var newDocument = document.WithSyntaxRoot( newRoot );
			return Task.FromResult( newDocument );
		}
	}
}
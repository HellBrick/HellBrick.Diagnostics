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
	internal class ToExpressionBodiedMethodRefactoring : CodeRefactoringProvider
	{
		public async sealed override Task ComputeRefactoringsAsync( CodeRefactoringContext context )
		{
			var root = await context.Document.GetSyntaxRootAsync( context.CancellationToken ).ConfigureAwait( false );
			var semanticModel = await context.Document.GetSemanticModelAsync( context.CancellationToken ).ConfigureAwait( false );

			IEnumerable<OneLiner> oneLiners = EnumerateSelectedOneLiners( context, root, semanticModel );

			foreach ( var oneLiner in oneLiners )
			{
				var methodName = semanticModel.GetDeclaredSymbol( oneLiner.Declaration, context.CancellationToken )?.Name ?? oneLiner.Declaration.ToString();
				var codeFix = CodeAction.Create( $"Convert '{methodName}' to an expression-bodied method", c => ConvertToExpressionBodiedMethodAsync( oneLiner, context, root, c ) );
				context.RegisterRefactoring( codeFix );
			}
		}

		private static IEnumerable<OneLiner> EnumerateSelectedOneLiners( CodeRefactoringContext context, SyntaxNode root, SemanticModel semanticModel ) =>
			from method in root.EnumerateSelectedNodes<MethodDeclarationSyntax>( context.Span )
			where method.Body?.Statements.Count == 1
			let returnStatement = method.Body.Statements[ 0 ] as ReturnStatementSyntax
			where ( semanticModel.GetDeclaredSymbol( method ) as IMethodSymbol )?.ReturnsVoid != true
			select new OneLiner( method, returnStatement );

		private Task<Document> ConvertToExpressionBodiedMethodAsync( OneLiner oneLiner, CodeRefactoringContext context, SyntaxNode root, CancellationToken cancellationToken )
		{
			MethodDeclarationSyntax newMethod = BuildNewMethod( oneLiner );
			var newDocument = context.Document.WithSyntaxRoot( root.ReplaceNode( oneLiner.Declaration, newMethod ) );
			return Task.FromResult( newDocument );
		}

		private static MethodDeclarationSyntax BuildNewMethod( OneLiner oneLiner )
		{
			var oldBody = oneLiner.Declaration.Body;
			var newMethod = oneLiner.Declaration;

			//	Remove the \r\n if it's the only trailing trivia
			var beforeBody = oneLiner.Declaration.FindNode( TextSpan.FromBounds( oldBody.FullSpan.Start - 1, oldBody.FullSpan.Start - 1 ) );
			var beforeBodyTrivia = beforeBody.GetTrailingTrivia();
			if ( beforeBodyTrivia.Count == 1 && beforeBodyTrivia[ 0 ].IsKind( SyntaxKind.EndOfLineTrivia ) )
			{
				newMethod = newMethod.ReplaceNode(
					beforeBody,
					beforeBody.ReplaceTrivia( beforeBodyTrivia[ 0 ], SyntaxTrivia( SyntaxKind.WhitespaceTrivia, " " ) ) );
			}

			var expression = oneLiner.ReturnStatement.Expression.WithLeadingTrivia( SyntaxTrivia( SyntaxKind.WhitespaceTrivia, " " ) );

			var arrow = ArrowExpressionClause( expression );
			return newMethod
				.WithBody( null )
				.WithExpressionBody( arrow )
				.WithSemicolonToken( Token( SyntaxKind.SemicolonToken ) )
				.WithTrailingTrivia( oldBody.GetTrailingTrivia() );
		}
	}
}
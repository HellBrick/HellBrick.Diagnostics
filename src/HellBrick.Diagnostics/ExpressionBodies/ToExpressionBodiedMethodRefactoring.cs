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

			IEnumerable<MethodDeclarationSyntax> oneLiners = EnumerateSelectedOneLiners( context, root, semanticModel );

			foreach ( var method in oneLiners )
			{
				var methodName = semanticModel.GetDeclaredSymbol( method, context.CancellationToken )?.Name ?? method.ToString();
				var codeFix = CodeAction.Create( $"Convert '{methodName}' to an expression-bodied method", c => ConvertToExpressionBodiedMethodAsync( method, context, root, c ) );
				context.RegisterRefactoring( codeFix );
			}
		}

		private static IEnumerable<MethodDeclarationSyntax> EnumerateSelectedOneLiners( CodeRefactoringContext context, SyntaxNode root, SemanticModel semanticModel )
		{
			return root
				.EnumerateSelectedNodes<MethodDeclarationSyntax>( context.Span )
				.Where( m => m.Body?.Statements.Count == 1 )
				.Where( m => ( semanticModel.GetDeclaredSymbol( m ) as IMethodSymbol )?.ReturnsVoid != true );
		}

		private Task<Document> ConvertToExpressionBodiedMethodAsync( MethodDeclarationSyntax method, CodeRefactoringContext context, SyntaxNode root, CancellationToken cancellationToken )
		{
			MethodDeclarationSyntax newMethod = BuildNewMethod( method );
			var newDocument = context.Document.WithSyntaxRoot( root.ReplaceNode( method, newMethod ) );
			return Task.FromResult( newDocument );
		}

		private static MethodDeclarationSyntax BuildNewMethod( MethodDeclarationSyntax method )
		{
			var oldBody = method.Body;
			var newMethod = method;

			//	Remove the \r\n if it's the only trailing trivia
			var beforeBody = method.FindNode( TextSpan.FromBounds( oldBody.FullSpan.Start - 1, oldBody.FullSpan.Start - 1 ) );
			var beforeBodyTrivia = beforeBody.GetTrailingTrivia();
			if ( beforeBodyTrivia.Count == 1 && beforeBodyTrivia[ 0 ].IsKind( SyntaxKind.EndOfLineTrivia ) )
			{
				newMethod = newMethod.ReplaceNode(
					beforeBody,
					beforeBody.ReplaceTrivia( beforeBodyTrivia[ 0 ], SyntaxTrivia( SyntaxKind.WhitespaceTrivia, " " ) ) );
			}

			var expression = oldBody.Statements[ 0 ].ChildNodes()
				.OfType<ExpressionSyntax>()
				.FirstOrDefault()
				.WithLeadingTrivia( SyntaxTrivia( SyntaxKind.WhitespaceTrivia, " " ) );

			var arrow = ArrowExpressionClause( expression );
			return newMethod
				.WithBody( null )
				.WithExpressionBody( arrow )
				.WithSemicolonToken( Token( SyntaxKind.SemicolonToken ) )
				.WithTrailingTrivia( oldBody.GetTrailingTrivia() );
		}
	}
}
using System;
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
using Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace HellBrick.Diagnostics
{
	[ExportCodeRefactoringProvider(ToLambdaMethodRefactoring.RefactoringID, LanguageNames.CSharp)]
	internal class ToLambdaMethodRefactoring: CodeRefactoringProvider
	{
		public const string RefactoringID = Common.RulePrefix + "RefactorToLambdaMethod";

		public async sealed override Task ComputeRefactoringsAsync( CodeRefactoringContext context )
		{
			var root = await context.Document.GetSyntaxRootAsync( context.CancellationToken ).ConfigureAwait( false );
			var semanticModel = await context.Document.GetSemanticModelAsync( context.CancellationToken ).ConfigureAwait( false );

			IEnumerable<MethodDeclarationSyntax> selectedMethods;
			if ( context.Span.Length > 0 )
				selectedMethods = root.DescendantNodes( context.Span ).OfType<MethodDeclarationSyntax>();
			else
			{
				var node = root.FindNode( context.Span ).FirstAncestorOrSelf<MethodDeclarationSyntax>();
				selectedMethods = node != null ? Enumerable.Repeat( node, 1 ) : Enumerable.Empty<MethodDeclarationSyntax>();
			}

			var oneLiners = selectedMethods
				.Where( m => m.Body?.Statements.Count == 1 )
				.Where( m => ( semanticModel.GetDeclaredSymbol( m ) as IMethodSymbol )?.ReturnsVoid != true );

			foreach ( var method in oneLiners )
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
				newMethod = newMethod
					.WithBody( null )
					.WithExpressionBody( arrow )
					.WithSemicolonToken( Token( SyntaxKind.SemicolonToken ) )
					.WithTrailingTrivia( oldBody.GetTrailingTrivia() );

				var newDocument = context.Document.WithSyntaxRoot( root.ReplaceNode( method, newMethod ) );
				var methodName = semanticModel.GetDeclaredSymbol( method, context.CancellationToken )?.Name ?? method.ToString();
				context.RegisterRefactoring( CodeAction.Create( "Convert '\{methodName}' to expression method", newDocument, RefactoringID ) );
			}
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using HellBrick.Diagnostics.Utils;
using Microsoft.CodeAnalysis.CodeActions;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace HellBrick.Diagnostics.ExpressionBodies
{
	internal abstract class AbstractExpressionBodyRefactoring<TDeclarationSyntax> : CodeRefactoringProvider
		where TDeclarationSyntax : MemberDeclarationSyntax
	{
		public async sealed override Task ComputeRefactoringsAsync( CodeRefactoringContext context )
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync( context.CancellationToken ).ConfigureAwait( false );
			SemanticModel semanticModel = await context.Document.GetSemanticModelAsync( context.CancellationToken ).ConfigureAwait( false );

			IEnumerable<OneLiner> oneLiners = EnumerateOneLiners( context, root );

			foreach ( OneLiner oneLiner in oneLiners )
			{
				string memberName = semanticModel.GetDeclaredSymbol( oneLiner.Declaration, context.CancellationToken )?.Name ?? GetIdentifierName( oneLiner.Declaration );
				CodeAction codeFix = CodeAction.Create( $"Convert '{memberName}' to an expression-bodied member", c => ConvertToExpressionBodiedMemberAsync( oneLiner, context, root, c ) );
				context.RegisterRefactoring( codeFix );
			}
		}

		private IEnumerable<OneLiner> EnumerateOneLiners( CodeRefactoringContext context, SyntaxNode root ) =>
			from TDeclarationSyntax declaration in root.EnumerateSelectedNodes<TDeclarationSyntax>( context.Span )
			where CanConvertToExpression( declaration )
			let body = GetBody( declaration )
			where body?.Statements.Count == 1
			let returnStatement = body.Statements[ 0 ] as ReturnStatementSyntax
			where returnStatement != null && returnStatement.Expression != null
			select new OneLiner( declaration, returnStatement );

		private Task<Document> ConvertToExpressionBodiedMemberAsync( OneLiner oneLiner, CodeRefactoringContext context, SyntaxNode root, CancellationToken cancellationToken )
		{
			TDeclarationSyntax newMember = BuildNewMember( oneLiner );
			var newDocument = context.Document.WithSyntaxRoot( root.ReplaceNode( oneLiner.Declaration, newMember ) );
			return Task.FromResult( newDocument );
		}

		private TDeclarationSyntax BuildNewMember( OneLiner oneLiner )
		{
			TDeclarationSyntax newMember = oneLiner.Declaration;

			//	Remove the \r\n if it's the only trailing trivia
			SyntaxNode removedNode = GetRemovedNode( oneLiner.Declaration );
			SyntaxNode lastMaintainedNode = oneLiner.Declaration.FindNode( new TextSpan( removedNode.FullSpan.Start - 1, 0 ) );
			SyntaxTriviaList lastMaintainedNodeTrivia = lastMaintainedNode.GetTrailingTrivia();
			if ( lastMaintainedNodeTrivia.Count == 1 && lastMaintainedNodeTrivia[ 0 ].IsKind( SyntaxKind.EndOfLineTrivia ) )
			{
				newMember = newMember.ReplaceNode(
					removedNode,
					removedNode.ReplaceTrivia( lastMaintainedNodeTrivia[ 0 ], SyntaxTrivia( SyntaxKind.WhitespaceTrivia, " " ) ) );
			}

			ExpressionSyntax returnExpression = oneLiner.ReturnStatement.Expression.WithLeadingTrivia( SyntaxTrivia( SyntaxKind.WhitespaceTrivia, " " ) );
			ArrowExpressionClauseSyntax arrow = ArrowExpressionClause( returnExpression );

			return ReplaceBodyWithExpressionClause( newMember, arrow );
		}

		protected abstract bool CanConvertToExpression( TDeclarationSyntax declaration );
		protected abstract BlockSyntax GetBody( TDeclarationSyntax declaration );
		protected abstract string GetIdentifierName( TDeclarationSyntax declaration );
		protected abstract SyntaxNode GetRemovedNode( TDeclarationSyntax declaration );
		protected abstract TDeclarationSyntax ReplaceBodyWithExpressionClause( TDeclarationSyntax declaration, ArrowExpressionClauseSyntax arrow );

		private class OneLiner
		{
			public OneLiner( TDeclarationSyntax declaration, ReturnStatementSyntax returnStatement )
			{
				Declaration = declaration;
				ReturnStatement = returnStatement;
			}

			public TDeclarationSyntax Declaration { get; }
			public ReturnStatementSyntax ReturnStatement { get; }
		}
	}
}

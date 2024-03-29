﻿using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace HellBrick.Diagnostics.ConfigureAwait
{
	[ExportCodeFixProvider( LanguageNames.CSharp, Name = nameof( TaskAwaiterCodeFixProvider ) ), Shared]
	public class TaskAwaiterCodeFixProvider : CodeFixProvider
	{
		private static readonly ArgumentSyntax _falseArgument = Argument( LiteralExpression( SyntaxKind.FalseLiteralExpression ) );
		private static readonly ArgumentSyntax _trueArgument = Argument( LiteralExpression( SyntaxKind.TrueLiteralExpression ) );

		public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create( TaskAwaiterAnalyzer.DiagnosticID );
		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override Task RegisterCodeFixesAsync( CodeFixContext context )
		{
			RegisterConfigureAwaitCodeFix( context, false );
			RegisterConfigureAwaitCodeFix( context, true );
			return Task.CompletedTask;
		}

		private static void RegisterConfigureAwaitCodeFix( CodeFixContext context, bool captureContext )
		{
			CodeAction codeFix = CodeAction.Create
			(
				$"Add 'ConfigureAwait( {captureContext} )'",
				c => AddConfigureAwaitAsync( context, captureContext, c ),
				$"{nameof( TaskAwaiterCodeFixProvider )}.{captureContext}"
			);
			context.RegisterCodeFix( codeFix, context.Diagnostics[ 0 ] );
		}

		private static async Task<Document> AddConfigureAwaitAsync( CodeFixContext context, bool captureContext, CancellationToken cancellationToken )
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync( context.CancellationToken ).ConfigureAwait( false );
			ExpressionSyntax awaitedExpression = ((AwaitExpressionSyntax)root.FindNode( context.Span )).Expression;
			MemberAccessExpressionSyntax configureAwaitMember = MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, awaitedExpression, IdentifierName( "ConfigureAwait" ) );
			ArgumentSyntax argument = captureContext ? _trueArgument : _falseArgument;
			InvocationExpressionSyntax configureAwaitInvocation = InvocationExpression( configureAwaitMember ).AddArgumentListArguments( argument );

			SyntaxNode newRoot = root.ReplaceNode( awaitedExpression, configureAwaitInvocation );
			return context.Document.WithSyntaxRoot( newRoot );
		}
	}
}

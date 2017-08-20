using System.Composition;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using HellBrick.Diagnostics.Utils;

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
			return TaskHelper.CompletedTask;
		}

		private void RegisterConfigureAwaitCodeFix( CodeFixContext context, bool captureContext )
		{
			CodeAction codeFix = CodeAction.Create
			(
				$"Add 'ConfigureAwait( {captureContext} )'",
				c => AddConfigureAwaitAsync( context, captureContext, c ),
				$"{nameof( TaskAwaiterCodeFixProvider )}.{captureContext}"
			);
			context.RegisterCodeFix( codeFix, context.Diagnostics[ 0 ] );
		}

		private async Task<Document> AddConfigureAwaitAsync( CodeFixContext context, bool captureContext, CancellationToken cancellationToken )
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync( context.CancellationToken ).ConfigureAwait( false );
			ExpressionSyntax awaitedExpression = root.FindNode( context.Span ) as ExpressionSyntax;
			MemberAccessExpressionSyntax configureAwaitMember = MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, awaitedExpression, IdentifierName( "ConfigureAwait" ) );
			ArgumentSyntax argument = captureContext ? _trueArgument : _falseArgument;
			InvocationExpressionSyntax configureAwaitInvocation = InvocationExpression( configureAwaitMember ).AddArgumentListArguments( argument );

			SyntaxNode newRoot = root.ReplaceNode( awaitedExpression, configureAwaitInvocation );
			return context.Document.WithSyntaxRoot( newRoot );
		}
	}
}
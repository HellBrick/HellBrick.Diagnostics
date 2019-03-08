using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace HellBrick.Diagnostics.CommentedCode
{
	[ExportCodeFixProvider( LanguageNames.CSharp, Name = nameof( CommentedCodeCodeFixProvider ) ), Shared]
	public class CommentedCodeCodeFixProvider : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create( CommentedCodeAnalyzer.DiagnosticId );

		public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public override Task RegisterCodeFixesAsync( CodeFixContext context )
		{
			CodeAction codeFix = CodeAction.Create( "Remove commented out code", ct => UpdateDocumentAsync( context, ct ) );
			context.RegisterCodeFix( codeFix, context.Diagnostics[ 0 ] );
			return Task.CompletedTask;
		}

		private static async Task<Document> UpdateDocumentAsync( CodeFixContext context, CancellationToken cancellationToken )
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync( cancellationToken ).ConfigureAwait( false );
			SemanticModel semanticModel = await context.Document.GetSemanticModelAsync( cancellationToken ).ConfigureAwait( false );

			SyntaxNode newRoot = root.ReplaceTrivia( EnumerateCommentBlockTrivia().ToArray(), ( original, rewritten ) => default );

			return context.Document.WithSyntaxRoot( newRoot );

			IEnumerable<SyntaxTrivia> EnumerateCommentBlockTrivia()
			{
				SyntaxTrivia someTrivia = root.FindTrivia( context.Span.Start );
				return someTrivia.Token.GetAllTrivia().Where( trivia => context.Span.Contains( trivia.Span ) );
			}
		}
	}
}

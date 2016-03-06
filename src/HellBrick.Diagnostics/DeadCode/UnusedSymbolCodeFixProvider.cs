using System;
using System.Composition;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using HellBrick.Diagnostics.Utils;

namespace HellBrick.Diagnostics.DeadCode
{
	[ExportCodeFixProvider( LanguageNames.CSharp, Name = nameof( UnusedSymbolCodeFixProvider ) ), Shared]
	public class UnusedSymbolCodeFixProvider : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create( UnusedSymbolAnalyzer.DiagnosticID );
		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override Task RegisterCodeFixesAsync( CodeFixContext context )
		{
			CodeAction codeAction = CodeAction.Create( "Remove unused code", ct => UpdateDocumentAsync( context, ct ), nameof( UnusedSymbolCodeFixProvider ) );
			context.RegisterCodeFix( codeAction, context.Diagnostics[ 0 ] );
			return TaskHelper.CompletedTask;
		}

		private async Task<Document> UpdateDocumentAsync( CodeFixContext context, CancellationToken cancellationToken )
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync( cancellationToken ).ConfigureAwait( false );
			SyntaxNode removedNode = root.FindNode( context.Span );
			SyntaxNode newRoot = root.RemoveNode( removedNode, SyntaxRemoveOptions.KeepDirectives | SyntaxRemoveOptions.AddElasticMarker );
			Document newDocument = context.Document.WithSyntaxRoot( newRoot );
			return newDocument;
		}
	}
}
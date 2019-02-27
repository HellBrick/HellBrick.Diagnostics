using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace HellBrick.Diagnostics.DeadCode
{
	[ExportCodeFixProvider( LanguageNames.CSharp, Name = nameof( UnusedSymbolCodeFixProvider ) ), Shared]
	public class UnusedSymbolCodeFixProvider : CodeFixProvider
	{
		private const string _codeActionTitle = "Remove unused code";
		private const SyntaxRemoveOptions _nodeRemovalOptions = SyntaxRemoveOptions.KeepDirectives | SyntaxRemoveOptions.AddElasticMarker;

		public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create( UnusedSymbolAnalyzer.DiagnosticID );

		private static readonly BatchFixer _batchFixer = new BatchFixer();
		public sealed override FixAllProvider GetFixAllProvider() => _batchFixer;

		public sealed override Task RegisterCodeFixesAsync( CodeFixContext context )
		{
			CodeAction codeAction = CodeAction.Create( _codeActionTitle, ct => UpdateDocumentAsync( context, ct ), nameof( UnusedSymbolCodeFixProvider ) );
			context.RegisterCodeFix( codeAction, context.Diagnostics[ 0 ] );
			return Task.CompletedTask;
		}

		private async Task<Document> UpdateDocumentAsync( CodeFixContext context, CancellationToken cancellationToken )
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync( cancellationToken ).ConfigureAwait( false );
			SyntaxNode removedNode = root.FindNode( context.Span );
			SyntaxNode newRoot = root.RemoveNode( removedNode, _nodeRemovalOptions );
			Document newDocument = context.Document.WithSyntaxRoot( newRoot );
			return newDocument;
		}

		private class BatchFixer : FixAllProvider
		{
			public override Task<CodeAction> GetFixAsync( FixAllContext fixAllContext )
			{
				switch ( fixAllContext.Scope )
				{
					case FixAllScope.Document:
						return Task.FromResult( CodeAction.Create( _codeActionTitle, ct => UpdateDocumentAsync( fixAllContext, ct ) ) );

					case FixAllScope.Project:
						return Task.FromResult( CodeAction.Create( _codeActionTitle, ct => UpdateProjectAsync( fixAllContext, ct ) ) );

					case FixAllScope.Solution:
						return Task.FromResult( CodeAction.Create( _codeActionTitle, ct => UpdateSolutionAsync( fixAllContext, ct ) ) );

					default:
						throw new NotSupportedException( $"Scope {fixAllContext.Scope} is not supported." );
				}
			}

			private async Task<Document> UpdateDocumentAsync( FixAllContext fixAllContext, CancellationToken cancellationToken )
			{
				ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync( fixAllContext.Document ).ConfigureAwait( false );
				SyntaxNode newRoot = await GetNewDocumentRootAsync( fixAllContext.Document, diagnostics, cancellationToken ).ConfigureAwait( false );
				return fixAllContext.Document.WithSyntaxRoot( newRoot );
			}

			private static async Task<SyntaxNode> GetNewDocumentRootAsync( Document document, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken )
			{
				SyntaxNode root = await document.GetSyntaxRootAsync( cancellationToken ).ConfigureAwait( false );
				SyntaxNode[] nodesToRemove = diagnostics.Select( d => root.FindNode( d.Location.SourceSpan ) ).ToArray();
				SyntaxNode newRoot = root.RemoveNodes( nodesToRemove, _nodeRemovalOptions );
				return newRoot;
			}

			private async Task<Solution> UpdateProjectAsync( FixAllContext fixAllContext, CancellationToken cancellationToken )
			{
				ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetAllDiagnosticsAsync( fixAllContext.Project ).ConfigureAwait( false );
				Solution newSolution = await UpdateProjectAsync( fixAllContext.Solution, fixAllContext.Project, diagnostics, cancellationToken ).ConfigureAwait( false );

				return newSolution;
			}

			private static async Task<Solution> UpdateProjectAsync( Solution newSolution, Project project, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken )
			{
				foreach ( IGrouping<SyntaxTree, Diagnostic> sourceTreeGroup in diagnostics.GroupBy( d => d.Location.SourceTree ) )
				{
					Document document = project.GetDocument( sourceTreeGroup.Key );
					SyntaxNode newDocumentRoot = await GetNewDocumentRootAsync( document, sourceTreeGroup, cancellationToken ).ConfigureAwait( false );
					newSolution = newSolution.WithDocumentSyntaxRoot( document.Id, newDocumentRoot );
				}

				return newSolution;
			}

			private async Task<Solution> UpdateSolutionAsync( FixAllContext fixAllContext, CancellationToken cancellationToken )
			{
				Solution newSolution = fixAllContext.Solution;
				foreach ( Project project in fixAllContext.Solution.Projects )
				{
					ImmutableArray<Diagnostic> projectDiags = await fixAllContext.GetAllDiagnosticsAsync( project ).ConfigureAwait( false );
					newSolution = await UpdateProjectAsync( newSolution, project, projectDiags, cancellationToken ).ConfigureAwait( false );
				}

				return newSolution;
			}
		}
	}
}

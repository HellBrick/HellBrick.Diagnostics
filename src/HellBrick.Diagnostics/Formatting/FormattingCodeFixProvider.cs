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
using Microsoft.CodeAnalysis.Formatting;
using HellBrick.Diagnostics.Utils;

namespace HellBrick.Diagnostics.Formatting
{
	[ExportCodeFixProvider( LanguageNames.CSharp, Name = nameof( FormattingCodeFixProvider ) ), Shared]
	public class FormattingCodeFixProvider : CodeFixProvider
	{
		private const string _fixTitle = "Format the code";
		private const string _key = nameof( FormattingCodeFixProvider );
		private static readonly MultiFormatterProvider _fixAllProvider = new MultiFormatterProvider();

		public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create( FormattingAnalyzer.DiagnosticID );
		public sealed override FixAllProvider GetFixAllProvider() => _fixAllProvider;

		public sealed override Task RegisterCodeFixesAsync( CodeFixContext context )
		{
			CodeAction fix = CodeAction.Create( _fixTitle, c => FormatAsync( context.Document, context.Span, c ), _key );
			context.RegisterCodeFix( fix, context.Diagnostics[ 0 ] );
			return TaskHelper.CompletedTask;
		}

		private Task<Document> FormatAsync( Document document, TextSpan span, CancellationToken cancellationToken )
		{
			return Formatter.FormatAsync( document, span, ProperFormattingOptions.Instance, cancellationToken );
		}

		private static Task<Document> FormatAsync( Document document, CancellationToken cancellationToken )
		{
			return Formatter.FormatAsync( document, ProperFormattingOptions.Instance, cancellationToken );
		}

		private class MultiFormatterProvider : FixAllProvider
		{
			public override Task<CodeAction> GetFixAsync( FixAllContext fixAllContext ) => Task.FromResult( GetFix( fixAllContext ) );

			private static CodeAction GetFix( FixAllContext fixAllContext )
			{
				switch ( fixAllContext.Scope )
				{
					case FixAllScope.Document:
						return CodeAction.Create( _fixTitle, c => FormatAsync( fixAllContext.Document, c ) );

					case FixAllScope.Project:
						return CodeAction.Create( _fixTitle, c => FormatProjectAsync( fixAllContext, fixAllContext.Solution, fixAllContext.Project, c ) );

					case FixAllScope.Solution:
						return CodeAction.Create( _fixTitle, c => FormatSolutionAsync( fixAllContext, c ) );

					default:
						throw new NotSupportedException( $"{fixAllContext.Scope} is not supported by {nameof( FormattingCodeFixProvider )}" );
				}
			}

			private static async Task<Solution> FormatSolutionAsync( FixAllContext fixAllContext, CancellationToken cancellationToken )
			{
				Solution newSolution = fixAllContext.Solution;
				using ( var projectIterator = fixAllContext.Solution.Projects.GetEnumerator() )
				{
					while ( !cancellationToken.IsCancellationRequested && projectIterator.MoveNext() )
					{
						Project project = projectIterator.Current;
						newSolution = await FormatProjectAsync( fixAllContext, newSolution, project, cancellationToken );
					}
				}

				return newSolution;
			}

			private static async Task<Solution> FormatProjectAsync( FixAllContext fixAllContext, Solution solution, Project project, CancellationToken cancellationToken )
			{
				ImmutableArray<Diagnostic> projectDiagnostics = await fixAllContext.GetAllDiagnosticsAsync( project );

				Document[] documents = projectDiagnostics
					.Select( d => d.Location.SourceTree )
					.Distinct()
					.Select( st => project.GetDocument( st ) )
					.ToArray();

				Solution newSolution = solution;
				foreach ( Document document in documents )
				{
					var newDocument = await FormatAsync( document, cancellationToken );
					newSolution = newSolution.WithDocumentText( document.Id, await newDocument.GetTextAsync() );
				}

				return newSolution;
			}
		}
	}
}
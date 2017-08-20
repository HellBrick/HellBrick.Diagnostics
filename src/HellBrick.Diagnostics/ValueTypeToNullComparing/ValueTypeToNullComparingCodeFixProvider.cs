using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HellBrick.Diagnostics.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HellBrick.Diagnostics.ValueTypeToNullComparing
{
	[ExportCodeFixProvider( LanguageNames.CSharp, Name = nameof( ValueTypeToNullComparingCodeFixProvider ) ), Shared]
	public class ValueTypeToNullComparingCodeFixProvider : CodeFixProvider
	{
		private const string _title = "Replace with `default` literal";

		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create( ValueTypeToNullComparingAnalyzer.DiagnosticId );
		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override Task RegisterCodeFixesAsync( CodeFixContext context )
		{
			Diagnostic diagnostic = context.Diagnostics.First();

			context.RegisterCodeFix
			(
				CodeAction.Create
				(
					title: _title,
					createChangedDocument: c => ReplaceNullWithDefalutSyntaxAsync( context.Document, diagnostic, c ),
					equivalenceKey: _title
				),
					diagnostic
			);
			return TaskHelper.CompletedTask;
		}

		private async Task<Document> ReplaceNullWithDefalutSyntaxAsync( Document document, Diagnostic diagnostic, CancellationToken token )
		{
			SyntaxNode documentRoot = await document
				.GetSyntaxRootAsync( token )
				.ConfigureAwait( false );

			BinaryExpressionSyntax equalStatement = documentRoot
				.FindNode( diagnostic.Location.SourceSpan ) as BinaryExpressionSyntax;

			return document.WithSyntaxRoot
			(
				equalStatement.Right.IsKind( SyntaxKind.NullLiteralExpression )
					? ReplaceNodeWithDefaultSyntax( diagnostic, documentRoot, equalStatement.Right )
					: ReplaceNodeWithDefaultSyntax( diagnostic, documentRoot, equalStatement.Left )
			);
		}

		private static SyntaxNode ReplaceNodeWithDefaultSyntax( Diagnostic diagnostic, SyntaxNode documentRoot, ExpressionSyntax nodeToReplace )
			=> documentRoot.ReplaceNode( nodeToReplace, CreateDefaultSyntax( diagnostic.Location, nodeToReplace ) );

		private static ExpressionSyntax CreateDefaultSyntax( Location location, ExpressionSyntax nodeToReplace )
			=> SyntaxFactory
			.LiteralExpression( SyntaxKind.DefaultLiteralExpression )
			.WithLeadingTrivia( nodeToReplace.GetLeadingTrivia() )
			.WithTrailingTrivia( nodeToReplace.GetTrailingTrivia() );
	}
}
